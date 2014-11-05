// MSBuildProject.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//

using System;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using System.Text;

using MonoDevelop.Core;
using MonoDevelop.Projects.Utility;
using Microsoft.Build.BuildEngine;
using MSProject = Microsoft.Build.BuildEngine.Project;
using System.Linq;
using MonoDevelop.Projects.Text;
using System.Threading.Tasks;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	public class MSBuildProject
	{
		XmlDocument doc;
		FilePath file;
		Dictionary<XmlElement,MSBuildObject> elemCache = new Dictionary<XmlElement,MSBuildObject> ();
		Dictionary<string, MSBuildItemGroup> bestGroups;
		List<MSBuildItemEvaluated> evaluatedItems = new List<MSBuildItemEvaluated> ();
		List<MSBuildItemEvaluated> evaluatedItemsIgnoringCondition;
		MSBuildEvaluatedPropertyCollection evaluatedProperties;

		public const string Schema = "http://schemas.microsoft.com/developer/msbuild/2003";
		static XmlNamespaceManager manager;
		
		bool endsWithEmptyLine;
		string newLine = Environment.NewLine;
		ByteOrderMark bom;

		public static XmlNamespaceManager XmlNamespaceManager {
			get {
				if (manager == null) {
					manager = new XmlNamespaceManager (new NameTable ());
					manager.AddNamespace ("tns", Schema);
				}
				return manager;
			}
		}

		public FilePath FileName {
			get { return file; }
			set { file = value; }
		}

		public FilePath BaseDirectory {
			get { return file.ParentDirectory; }
		}

		public MSBuildFileFormat Format {
			get;
			set;
		}

		public bool IsNewProject { get; private set; }
		
		public XmlDocument Document {
			get { return doc; }
		}

		public MSBuildProject ()
		{
			evaluatedProperties = new MSBuildEvaluatedPropertyCollection (this);
			evaluatedItemsIgnoringCondition = new List<MSBuildItemEvaluated> ();
			doc = new XmlDocument ();
			doc.PreserveWhitespace = false;
			doc.AppendChild (doc.CreateElement (null, "Project", Schema));
			IsNewProject = true;
		}

		public static Task<MSBuildProject> LoadAsync (string file)
		{
			return Task<MSBuildProject>.Factory.StartNew (delegate {
				var p = new MSBuildProject ();
				p.Load (file);
				return p;
			});
		}

		public void Load (string file)
		{
			this.file = file;
			IsNewProject = false;
			using (FileStream fs = File.OpenRead (file)) {
				byte[] buf = new byte [1024];
				int nread, i;
				
				if ((nread = fs.Read (buf, 0, buf.Length)) <= 0)
					return;
				
				if (ByteOrderMark.TryParse (buf, nread, out bom))
					i = bom.Length;
				else
					i = 0;
				
				do {
					// Read to the first newline to figure out which line endings this file is using
					while (i < nread) {
						if (buf[i] == '\r') {
							newLine = "\r\n";
							break;
						} else if (buf[i] == '\n') {
							newLine = "\n";
							break;
						}
						
						i++;
					}
					
					if (newLine == null) {
						if ((nread = fs.Read (buf, 0, buf.Length)) <= 0) {
							newLine = "\n";
							break;
						}
						
						i = 0;
					}
				} while (newLine == null);
				
				// Check for a blank line at the end
				endsWithEmptyLine = fs.Seek (-1, SeekOrigin.End) > 0 && fs.ReadByte () == (int) '\n';
			}
			
			// Load the XML document
			doc = new XmlDocument ();
			doc.PreserveWhitespace = false;
			
			// HACK: XmlStreamReader will fail if the file is encoded in UTF-8 but has <?xml version="1.0" encoding="utf-16"?>
			// To work around this, we load the XML content into a string and use XmlDocument.LoadXml() instead.
			string xml = File.ReadAllText (file);
			
			doc.LoadXml (xml);
		}
		
		class ProjectWriter : StringWriter
		{
			Encoding encoding;
			
			public ProjectWriter (ByteOrderMark bom)
			{
				encoding = bom != null ? Encoding.GetEncoding (bom.Name) : null;
				ByteOrderMark = bom;
			}
			
			public ByteOrderMark ByteOrderMark {
				get; private set;
			}
			
			public override Encoding Encoding {
				get { return encoding ?? Encoding.UTF8; }
			}
		}
		
		public void Save (string fileName)
		{
			string content = SaveToString ();
			TextFile.WriteFile (fileName, content, bom, true);
		}
		
		public string SaveToString ()
		{
			// StringWriter.Encoding always returns UTF16. We need it to return UTF8, so the
			// XmlDocument will write the UTF8 header.
			ProjectWriter sw = new ProjectWriter (bom);
			sw.NewLine = newLine;
			doc.Save (sw);

			string content = sw.ToString ();
			if (endsWithEmptyLine && !content.EndsWith (newLine))
				content += newLine;

			return content;
		}

		void WriteDataObjects ()
		{
			foreach (var ob in elemCache.Values) {
				if (ob is MSBuildPropertyGroup)
					((MSBuildPropertyGroup)ob).WriteDataObjects ();
				else if (ob is MSBuildItem)
					((MSBuildItem)ob).WriteDataObjects ();
			}
		}

		public void Evaluate ()
		{
			try {
				Engine e = new Engine ();
				MSProject project = new MSProject (e);
				project.Load (FileName);
				SyncBuildProject (project);
			} catch (Exception ex) {
				// If the project can't be evaluated don't crash
				LoggingService.LogError ("MSBuild project could not be evaluated", ex);
			}
		}

		void SyncBuildProject (MSProject project)
		{
			var xmlGroups = PropertyGroups.ToArray ();
			var buildGroups = project.PropertyGroups.Cast<BuildPropertyGroup> ().ToArray ();
			for (int n=0; n<xmlGroups.Length && n<buildGroups.Length; n++)
				SyncBuildPropertyGroup (xmlGroups [n], buildGroups [n]);

			var xmlItems = ItemGroups.ToArray ();
			var buildItems = project.ItemGroups.Cast<BuildItemGroup> ().Where (g => !g.IsImported).ToArray ();
			for (int n=0; n<xmlItems.Length && n<buildItems.Length; n++)
				SyncBuildItemGroup (xmlItems [n], buildItems [n]);

			var xmlImports = Imports.ToArray ();
			var buildImports = project.Imports.Cast<Import> ().ToArray ();
			for (int n = 0; n < xmlImports.Length && n < buildImports.Length; n++)
				xmlImports [n].SetEvalResult (buildImports [n].EvaluatedProjectPath);

			foreach (BuildItem it in project.EvaluatedItems) {
				var xit = new MSBuildItemEvaluated (this, it.Name, it.Include, it.FinalItemSpec);
				xit.IsImported = it.IsImported;
				((MSBuildPropertyGroupEvaluated)xit.Metadata).Sync (it);
				evaluatedItems.Add (xit);
			}

			foreach (BuildItem it in project.EvaluatedItemsIgnoringCondition) {
				var xit = new MSBuildItemEvaluated (this, it.Name, it.Include, it.FinalItemSpec);
				xit.IsImported = it.IsImported;
				((MSBuildPropertyGroupEvaluated)xit.Metadata).Sync (it);
				evaluatedItemsIgnoringCondition.Add (xit);
			}

			evaluatedProperties.Sync (project.EvaluatedProperties);
		}

		void SyncBuildPropertyGroup (MSBuildPropertyGroup xmlGroup, BuildPropertyGroup buildGroup)
		{
			var xmlProps = xmlGroup.Properties.ToArray ();
			var buildProps = buildGroup.Cast<BuildProperty> ().ToArray ();
			for (int n = 0; n < xmlProps.Length && n < buildProps.Length; n++)
				SyncBuildProperty (xmlProps [n], buildProps [n]);
		}

		void SyncBuildProperty (MSBuildProperty xmlProp, BuildProperty buildProp)
		{
		}

		void SyncBuildItemGroup (MSBuildItemGroup xmlGroup, BuildItemGroup buildGroup)
		{
			var xmlItems = xmlGroup.Items.ToArray ();
			var buildItems = buildGroup.Cast<BuildItem> ().ToArray ();
			for (int n = 0; n < xmlItems.Length && n < buildItems.Length; n++)
				SyncBuildItem (xmlItems [n], buildItems [n]);
		}

		void SyncBuildItem (MSBuildItem xmlItem, BuildItem buildItem)
		{
			xmlItem.SetEvalResult (buildItem.FinalItemSpec);
			((MSBuildPropertyGroupEvaluated)xmlItem.EvaluatedMetadata).Sync (buildItem);
		}

		public string DefaultTargets {
			get { return doc.DocumentElement.GetAttribute ("DefaultTargets"); }
			set { doc.DocumentElement.SetAttribute ("DefaultTargets", value); }
		}
		
		public string ToolsVersion {
			get { return doc.DocumentElement.GetAttribute ("ToolsVersion"); }
			set {
				if (!string.IsNullOrEmpty (value))
					doc.DocumentElement.SetAttribute ("ToolsVersion", value);
				else
					doc.DocumentElement.RemoveAttribute ("ToolsVersion");
			}
		}

		public string[] ProjectTypeGuids {
			get { return GetGlobalPropertyGroup ().GetValue ("ProjectTypeGuids", "").Split (new []{';'}, StringSplitOptions.RemoveEmptyEntries).Select (t => t.Trim()).ToArray (); }
			set { GetGlobalPropertyGroup ().SetValue ("ProjectTypeGuids", string.Join (";", value), preserveExistingCase:true); }
		}

		public bool AddProjectTypeGuid (string guid)
		{
			var guids = GetGlobalPropertyGroup ().GetValue ("ProjectTypeGuids", "").Trim ();
			if (guids.IndexOf (guid, StringComparison.OrdinalIgnoreCase) == -1) {
				if (!string.IsNullOrEmpty (guids))
					guids += ";" + guid;
				else
					guids = guid;
				GetGlobalPropertyGroup ().SetValue ("ProjectTypeGuids", guids, preserveExistingCase: true);
				return true;
			}
			return false;
		}
		
		public bool RemoveProjectTypeGuid (string guid)
		{
			var guids = ProjectTypeGuids;
			var newGuids = guids.Where (g => !g.Equals (guid, StringComparison.OrdinalIgnoreCase)).ToArray ();
			if (newGuids.Length != guids.Length) {
				ProjectTypeGuids = newGuids;
				return true;
			} else
				return false;
		}

		public MSBuildImport AddNewImport (string name, string condition = null, MSBuildImport beforeImport = null)
		{
			XmlElement elem = doc.CreateElement (null, "Import", MSBuildProject.Schema);
			elem.SetAttribute ("Project", name);
			if (condition != null)
				elem.SetAttribute ("Condition", condition);

			if (beforeImport != null) {
				doc.DocumentElement.InsertBefore (elem, beforeImport.Element);
			} else {
				XmlElement last = doc.DocumentElement.SelectSingleNode ("tns:Import[last()]", XmlNamespaceManager) as XmlElement;
				if (last != null)
					doc.DocumentElement.InsertAfter (elem, last);
				else
					doc.DocumentElement.AppendChild (elem);
			}
			return GetImport (elem);
		}

		public MSBuildImport GetImport (string name, string condition = null)
		{
			return Imports.FirstOrDefault (i => i.Project == name && i.Condition == condition);
		}
		
		public void RemoveImport (string name)
		{
			XmlElement elem = (XmlElement) doc.DocumentElement.SelectSingleNode ("tns:Import[@Project='" + name + "']", XmlNamespaceManager);
			if (elem != null)
				elem.ParentNode.RemoveChild (elem);
			else
				//FIXME: should this actually log an error?
				Console.WriteLine ("ppnf:");
		}
		
		public void RemoveImport (MSBuildImport import)
		{
			import.Element.ParentNode.RemoveChild (import.Element);
		}

		public IEnumerable<MSBuildImport> Imports {
			get {
				foreach (XmlElement elem in doc.DocumentElement.SelectNodes ("tns:Import", XmlNamespaceManager))
					yield return GetImport (elem);
			}
		}

		public IMSBuildEvaluatedPropertyCollection EvaluatedProperties {
			get { return evaluatedProperties; }
		}

		public IEnumerable<IMSBuildItemEvaluated> EvaluatedItems {
			get { return evaluatedItems; }
		}

		public IEnumerable<IMSBuildItemEvaluated> EvaluatedItemsIgnoringCondition {
			get { return evaluatedItemsIgnoringCondition; }
		}

		public IMSBuildPropertySet GetGlobalPropertyGroup ()
		{
			foreach (MSBuildPropertyGroup grp in PropertyGroups) {
				if (grp.Condition.Length == 0)
					return grp;
			}
			return null;
		}
		
		public MSBuildPropertyGroup AddNewPropertyGroup (bool insertAtEnd)
		{
			XmlElement elem = doc.CreateElement (null, "PropertyGroup", MSBuildProject.Schema);
			
			if (insertAtEnd) {
				XmlElement last = doc.DocumentElement.SelectSingleNode ("tns:PropertyGroup[last()]", XmlNamespaceManager) as XmlElement;
				if (last != null)
					doc.DocumentElement.InsertAfter (elem, last);
			} else {
				XmlElement first = doc.DocumentElement.SelectSingleNode ("tns:PropertyGroup", XmlNamespaceManager) as XmlElement;
				if (first != null)
					doc.DocumentElement.InsertBefore (elem, first);
			}
			
			if (elem.ParentNode == null) {
				XmlElement first = doc.DocumentElement.SelectSingleNode ("tns:ItemGroup", XmlNamespaceManager) as XmlElement;
				if (first != null)
					doc.DocumentElement.InsertBefore (elem, first);
				else
					doc.DocumentElement.AppendChild (elem);
			}
			
			return GetGroup (elem);
		}
		
		public IEnumerable<MSBuildItem> GetAllItems ()
		{
			foreach (XmlElement elem in doc.DocumentElement.SelectNodes ("tns:ItemGroup/*", XmlNamespaceManager)) {
				yield return GetItem (elem);
			}
		}
		
		public IEnumerable<MSBuildItem> GetAllItems (params string[] names)
		{
			string name = string.Join ("|tns:ItemGroup/tns:", names);
			foreach (XmlElement elem in doc.DocumentElement.SelectNodes ("tns:ItemGroup/tns:" + name, XmlNamespaceManager)) {
				yield return GetItem (elem);
			}
		}
		
		public IEnumerable<MSBuildPropertyGroup> PropertyGroups {
			get {
				foreach (XmlElement elem in doc.DocumentElement.SelectNodes ("tns:PropertyGroup", XmlNamespaceManager))
					yield return GetGroup (elem);
			}
		}
		
		public IEnumerable<MSBuildItemGroup> ItemGroups {
			get {
				foreach (XmlElement elem in doc.DocumentElement.SelectNodes ("tns:ItemGroup", XmlNamespaceManager))
					yield return GetItemGroup (elem);
			}
		}
		
		public MSBuildItemGroup AddNewItemGroup ()
		{
			XmlElement elem = doc.CreateElement (null, "ItemGroup", MSBuildProject.Schema);
			doc.DocumentElement.AppendChild (elem);
			return GetItemGroup (elem);
		}
		
		public MSBuildItem AddNewItem (string name, string include)
		{
			MSBuildItemGroup grp = FindBestGroupForItem (name);
			return grp.AddNewItem (name, include);
		}
		
		MSBuildItemGroup FindBestGroupForItem (string itemName)
		{
			MSBuildItemGroup group;
			
			if (bestGroups == null)
			    bestGroups = new Dictionary<string, MSBuildItemGroup> ();
			else {
				if (bestGroups.TryGetValue (itemName, out group))
					return group;
			}
			
			foreach (MSBuildItemGroup grp in ItemGroups) {
				foreach (MSBuildItem it in grp.Items) {
					if (it.Name == itemName) {
						bestGroups [itemName] = grp;
						return grp;
					}
				}
			}
			group = AddNewItemGroup ();
			bestGroups [itemName] = group;
			return group;
		}
		
		public XmlElement GetProjectExtension (string section)
		{
			return doc.DocumentElement.SelectSingleNode ("tns:ProjectExtensions/tns:" + section, XmlNamespaceManager) as XmlElement;
		}
		
		public XmlElement GetMonoDevelopProjectExtension (string section)
		{
			return doc.DocumentElement.SelectSingleNode ("tns:ProjectExtensions/tns:MonoDevelop/tns:Properties/tns:" + section, XmlNamespaceManager) as XmlElement;
		}

		public void SetProjectExtension (string section, XmlElement value)
		{
			if (value.OwnerDocument != doc)
				value = (XmlElement)doc.ImportNode (value, true);

			XmlElement elem = doc.DocumentElement ["ProjectExtensions", MSBuildProject.Schema];
			if (elem == null) {
				elem = doc.CreateElement (null, "ProjectExtensions", MSBuildProject.Schema);
				doc.DocumentElement.AppendChild (elem);
			}
			XmlElement sec = elem [section];
			if (sec == null)
				elem.AppendChild (value);
			else {
				elem.InsertAfter (value, sec);
				elem.RemoveChild (sec);
			}
		}

		public void SetMonoDevelopProjectExtension (string section, XmlElement value)
		{
			if (value.OwnerDocument != doc)
				value = (XmlElement)doc.ImportNode (value, true);

			XmlElement elem = doc.DocumentElement ["ProjectExtensions", MSBuildProject.Schema];
			if (elem == null) {
				elem = doc.CreateElement (null, "ProjectExtensions", MSBuildProject.Schema);
				doc.DocumentElement.AppendChild (elem);
			}
			var parent = elem;
			elem = parent ["MonoDevelop", MSBuildProject.Schema];
			if (elem == null) {
				elem = doc.CreateElement (null, "MonoDevelop", MSBuildProject.Schema);
				parent.AppendChild (elem);
			}
			parent = elem;
			elem = parent ["Properties", MSBuildProject.Schema];
			if (elem == null) {
				elem = doc.CreateElement (null, "Properties", MSBuildProject.Schema);
				parent.AppendChild (elem);
			}
			XmlElement sec = elem [section];
			if (sec == null)
				elem.AppendChild (value);
			else {
				elem.InsertAfter (value, sec);
				elem.RemoveChild (sec);
			}
		}

		public void RemoveProjectExtension (string section)
		{
			XmlElement elem = doc.DocumentElement.SelectSingleNode ("tns:ProjectExtensions/tns:" + section, XmlNamespaceManager) as XmlElement;
			if (elem != null) {
				XmlElement parent = (XmlElement) elem.ParentNode;
				parent.RemoveChild (elem);
				if (!parent.HasChildNodes)
					parent.ParentNode.RemoveChild (parent);
			}
		}
		
		public void RemoveMonoDevelopProjectExtension (string section)
		{
			XmlElement elem = doc.DocumentElement.SelectSingleNode ("tns:ProjectExtensions/tns:MonoDevelop/tns:Properties/tns:" + section, XmlNamespaceManager) as XmlElement;
			if (elem != null) {
				do {
					XmlElement parent = (XmlElement) elem.ParentNode;
					parent.RemoveChild (elem);
					elem = parent;
				}
				while (!elem.HasChildNodes);
			}
		}

		public void RemoveItem (MSBuildItem item)
		{
			elemCache.Remove (item.Element);
			XmlElement parent = (XmlElement) item.Element.ParentNode;
			item.Element.ParentNode.RemoveChild (item.Element);
			if (parent.ChildNodes.Count == 0) {
				elemCache.Remove (parent);
				parent.ParentNode.RemoveChild (parent);
				bestGroups = null;
			}
		}
		
		internal MSBuildImport GetImport (XmlElement elem)
		{
			MSBuildObject ob;
			if (elemCache.TryGetValue (elem, out ob))
				return (MSBuildImport) ob;
			MSBuildImport it = new MSBuildImport (elem);
			elemCache [elem] = it;
			return it;
		}

		internal MSBuildItem GetItem (XmlElement elem)
		{
			MSBuildObject ob;
			if (elemCache.TryGetValue (elem, out ob))
				return (MSBuildItem) ob;
			MSBuildItem it = new MSBuildItem (this, elem);
			elemCache [elem] = it;
			return it;
		}
		
		MSBuildPropertyGroup GetGroup (XmlElement elem)
		{
			MSBuildObject ob;
			if (elemCache.TryGetValue (elem, out ob))
				return (MSBuildPropertyGroup) ob;
			MSBuildPropertyGroup it = new MSBuildPropertyGroup (this, elem);
			elemCache [elem] = it;
			return it;
		}
		
		MSBuildItemGroup GetItemGroup (XmlElement elem)
		{
			MSBuildObject ob;
			if (elemCache.TryGetValue (elem, out ob))
				return (MSBuildItemGroup) ob;
			MSBuildItemGroup it = new MSBuildItemGroup (this, elem);
			elemCache [elem] = it;
			return it;
		}
		
		public void RemoveGroup (MSBuildPropertyGroup grp)
		{
			elemCache.Remove (grp.Element);
			grp.Element.ParentNode.RemoveChild (grp.Element);
		}
	}
	

	
	
	
	
	
	

}
