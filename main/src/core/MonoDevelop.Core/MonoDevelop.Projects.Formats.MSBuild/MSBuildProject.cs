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
		List<IMSBuildItemEvaluated> evaluatedItems = new List<IMSBuildItemEvaluated> ();
		List<IMSBuildItemEvaluated> evaluatedItemsIgnoringCondition;
		MSBuildEvaluatedPropertyCollection evaluatedProperties;

		public const string Schema = "http://schemas.microsoft.com/developer/msbuild/2003";
		static XmlNamespaceManager manager;

		TextFormatInfo format = new TextFormatInfo ();

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

		public bool IsNewProject { get; internal set; }
		
		public XmlDocument Document {
			get { return doc; }
		}

		public MSBuildProject ()
		{
			evaluatedItemsIgnoringCondition = new List<IMSBuildItemEvaluated> ();
			doc = new XmlDocument ();
			doc.PreserveWhitespace = false;
			doc.AppendChild (doc.CreateElement (null, "Project", Schema));
			IsNewProject = true;
			AddNewPropertyGroup (false);
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
			format = FileUtil.GetTextFormatInfo (file);

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
			TextFile.WriteFile (fileName, content, format.ByteOrderMark, true);
		}
		
		public Task SaveAsync (string fileName)
		{
			return Task.Run (() => {
				string content = SaveToString ();
				TextFile.WriteFile (fileName, content, format.ByteOrderMark, true);
			});
		}

		public string SaveToString ()
		{
			IsNewProject = false;

			// StringWriter.Encoding always returns UTF16. We need it to return UTF8, so the
			// XmlDocument will write the UTF8 header.
			ProjectWriter sw = new ProjectWriter (format.ByteOrderMark);
			sw.NewLine = format.NewLine;
			doc.Save (sw);

			string content = sw.ToString ();
			if (format.EndsWithEmptyLine && !content.EndsWith (format.NewLine))
				content += format.NewLine;

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
				// Use a private metadata property to assign an id to each item. This id is used to match
				// evaluated items with the items that generated them.
				int id = 0;
				List<XmlElement> idElems = new List<XmlElement> ();

				var currentItems = new Dictionary<string,MSBuildItem> ();
				foreach (var it in GetAllItems ()) {
					var c = doc.CreateElement (NodeIdPropertyName, Schema);
					string nid = (id++).ToString ();
					c.InnerXml = nid;
					it.Element.AppendChild (c);
					currentItems [nid] = it;
					idElems.Add (c);
					it.EvaluatedItemCount = 0;
				}

				var prop = GetGlobalPropertyGroup ().GetProperty ("UseMSBuildEngine");
				if (prop != null && !prop.GetValue<bool> ()) {
					// If msbuild engine is disabled don't evaluate the msbuild file since it is likely to fail.
					SyncBuildProject (currentItems);
					return;
				}
				MSBuildEngine e = MSBuildEngine.Create ();

				OnEvaluationStarting ();

				var project = e.LoadProjectFromXml (FileName, doc.OuterXml);

				// Now remove the item id property
				foreach (var el in idElems)
					el.ParentNode.RemoveChild (el);

				SyncBuildProject (currentItems, e, project);
			}
			catch (Exception ex) {
				// If the project can't be evaluated don't crash
				LoggingService.LogError ("MSBuild project could not be evaluated", ex);
				throw new ProjectEvaluationException (this, ex.Message);
			}
			finally {
				OnEvaluationFinished ();
			}
		}

		internal const string NodeIdPropertyName = "__MD_NodeId";

		void SyncBuildProject (Dictionary<string,MSBuildItem> currentItems, MSBuildEngine e, object project)
		{
			evaluatedItemsIgnoringCondition.Clear ();
			evaluatedItems.Clear ();

			foreach (var it in e.GetAllItems (project, false)) {
				string name, include, finalItemSpec;
				bool imported;
				e.GetItemInfo (it, out name, out include, out finalItemSpec, out imported);
				var iid = e.GetItemMetadata (it, NodeIdPropertyName);
				MSBuildItem xit;
				if (currentItems.TryGetValue (iid, out xit)) {
					xit.SetEvalResult (finalItemSpec);
					((MSBuildPropertyGroupEvaluated)xit.EvaluatedMetadata).Sync (e, it);
				}
			}

			var xmlImports = Imports.ToArray ();
			var buildImports = e.GetImports (project).ToArray ();
			for (int n = 0; n < xmlImports.Length && n < buildImports.Length; n++)
				xmlImports [n].SetEvalResult (e.GetImportEvaluatedProjectPath (buildImports [n]));

			var evalItems = new Dictionary<string,MSBuildItemEvaluated> ();
			foreach (var it in e.GetEvaluatedItems (project)) {
				var xit = CreateEvaluatedItem (e, it);
				var itemId = e.GetItemMetadata (it, NodeIdPropertyName);
				var key = itemId + " " + xit.Include;
				if (evalItems.ContainsKey (key))
					continue; // xbuild seems to return duplicate items when using wildcards. This is a workaround to avoid the duplicates.
				MSBuildItem pit;
				if (!string.IsNullOrEmpty (itemId) && currentItems.TryGetValue (itemId, out pit)) {
					xit.SourceItem = pit;
					xit.Condition = pit.Condition;
					pit.EvaluatedItemCount++;
					evalItems [key] = xit;
				}
				evaluatedItems.Add (xit);
			}

			var evalItemsNoCond = new Dictionary<string,MSBuildItemEvaluated> ();
			foreach (var it in e.GetEvaluatedItemsIgnoringCondition (project)) {
				var itemId = e.GetItemMetadata (it, NodeIdPropertyName);
				MSBuildItemEvaluated evItem;
				var xit = CreateEvaluatedItem (e, it);
				var key = itemId + " " + xit.Include;
				if (evalItemsNoCond.ContainsKey (key))
					continue; // xbuild seems to return duplicate items when using wildcards. This is a workaround to avoid the duplicates.
				if (!string.IsNullOrEmpty (itemId) && evalItems.TryGetValue (key, out evItem)) {
					evaluatedItemsIgnoringCondition.Add (evItem);
					evalItemsNoCond [key] = evItem;
					continue;
				}
				MSBuildItem pit;
				if (!string.IsNullOrEmpty (itemId) && currentItems.TryGetValue (itemId, out pit)) {
					xit.SourceItem = pit;
					xit.Condition = pit.Condition;
					pit.EvaluatedItemCount++;
					evalItemsNoCond [key] = xit;
				}
				evaluatedItemsIgnoringCondition.Add (xit);
			}

			var props = new MSBuildEvaluatedPropertyCollection (this);
			evaluatedProperties = props;
			props.Sync (e, project);
		}

		MSBuildItemEvaluated CreateEvaluatedItem (MSBuildEngine e, object it)
		{
			string name, include, finalItemSpec;
			bool imported;
			e.GetEvaluatedItemInfo (it, out name, out include, out finalItemSpec, out imported);
			var xit = new MSBuildItemEvaluated (this, name, include, finalItemSpec);
			xit.IsImported = imported;
			((MSBuildPropertyGroupEvaluated)xit.Metadata).Sync (e, it);
			return xit;
		}

		void SyncBuildProject (Dictionary<string,MSBuildItem> currentItems)
		{
			evaluatedItemsIgnoringCondition.Clear ();
			evaluatedItems.Clear ();
			evaluatedItems.AddRange (GetAllItems ());
			evaluatedItemsIgnoringCondition.AddRange (GetAllItems ());
			evaluatedProperties = null;
		}

		void OnEvaluationStarting ()
		{
			foreach (var g in PropertyGroups)
				g.OnEvaluationStarting ();
			foreach (var g in ItemGroups)
				g.OnEvaluationStarting ();
			foreach (var i in Imports)
				i.OnEvaluationStarting ();
		}

		void OnEvaluationFinished ()
		{
			foreach (var g in PropertyGroups)
				g.OnEvaluationFinished ();
			foreach (var g in ItemGroups)
				g.OnEvaluationFinished ();
			foreach (var i in Imports)
				i.OnEvaluationFinished ();
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
			get { return (IMSBuildEvaluatedPropertyCollection) evaluatedProperties ?? (IMSBuildEvaluatedPropertyCollection) GetGlobalPropertyGroup (); }
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

		public MSBuildItem CreateItem (string name, string include)
		{
			var elem = Document.CreateElement (name, MSBuildProject.Schema);
			var bitem = new MSBuildItem (this, elem);
			bitem.Include = include;
			return bitem;
		}

		public void AddItem (MSBuildItem it)
		{
			MSBuildItemGroup grp = FindBestGroupForItem (it.Name);
			grp.AddItem (it);
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
		
		internal void AddToItemCache (MSBuildObject it)
		{
			elemCache [it.Element] = it;
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

		public IEnumerable<MSBuildTarget> Targets {
			get {
				foreach (XmlElement elem in doc.DocumentElement.SelectNodes ("tns:Target", XmlNamespaceManager))
					yield return new MSBuildTarget (elem);
			}
		}
	}
	
	public class MSBuildTarget: MSBuildObject
	{
		public MSBuildTarget (XmlElement elem): base (elem)
		{
		}

		public string Name {
			get { return Element.GetAttribute ("Name"); }
			set { Element.SetAttribute ("Name", value); }
		}

		public IEnumerable<MSBuildTask> Tasks {
			get {
				foreach (XmlNode node in Element.ChildNodes) {
					var elem = node as XmlElement;
					if (MSBuildTask.IsTask (elem))
						yield return new MSBuildTask (elem);
				}
			}
		}
	}

	public class MSBuildTask: MSBuildObject
	{
		public static bool IsTask (XmlElement elem)
		{
			if (elem == null)
				return false;

			return elem.LocalName == "Error";
		}

		public MSBuildTask (XmlElement elem): base (elem)
		{
		}

		public string Name {
			get { return Element.GetAttribute ("Name"); }
			set { Element.SetAttribute ("Name", value); }
		}
	}
}
