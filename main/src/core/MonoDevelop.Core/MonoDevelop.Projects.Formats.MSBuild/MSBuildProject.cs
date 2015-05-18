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
	public sealed class MSBuildProject: IDisposable
	{
		XmlDocument doc;
		FilePath file;
		Dictionary<XmlElement,MSBuildObject> elemCache = new Dictionary<XmlElement,MSBuildObject> ();
		Dictionary<string, MSBuildItemGroup> bestGroups;
		MSBuildProjectInstance mainProjectInstance;
		int changeStamp;

		MSBuildEngineManager engineManager;
		bool engineManagerIsLocal;

		MSBuildProjectInstanceInfo nativeProjectInfo;

		public const string Schema = "http://schemas.microsoft.com/developer/msbuild/2003";
		static XmlNamespaceManager manager;

		TextFormatInfo format = new TextFormatInfo { NewLine = "\r\n" };

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

		internal TextFormatInfo TextFormat {
			get { return format; }
		}

		public bool IsNewProject { get; internal set; }
		
		public XmlDocument Document {
			get { return doc; }
		}

		internal int ChangeStamp {
			get { return changeStamp; }
		}

		internal object ReadLock {
			get { return readLock; }
		}

		public MSBuildProject ()
		{
			mainProjectInstance = new MSBuildProjectInstance (this);
			doc = new XmlDocument ();
			doc.AppendChild (doc.CreateXmlDeclaration ("1.0","utf-8", null));
			doc.AppendChild (doc.CreateElement (null, "Project", Schema));
			XmlUtil.Indent (format, doc.DocumentElement, true);
			doc.PreserveWhitespace = true;
			UseMSBuildEngine = true;
			IsNewProject = true;
			AddNewPropertyGroup (false);
			EnableChangeTracking ();
		}

		internal MSBuildProject (MSBuildEngineManager manager): this ()
		{
			engineManager = manager;
		}

		public void Dispose ()
		{
			if (nativeProjectInfo != null) {
				mainProjectInstance.Dispose ();
				nativeProjectInfo.Engine.UnloadProject (nativeProjectInfo.Project);
				if (engineManagerIsLocal)
					nativeProjectInfo.Engine.Dispose ();
			}
		}

		void EnableChangeTracking ()
		{
			doc.NodeRemoving += OnNodeAddedRemoved;
			doc.NodeInserted += OnNodeAddedRemoved;
		}

		void DisableChangeTracking ()
		{
			doc.NodeRemoving -= OnNodeAddedRemoved;
			doc.NodeInserted -= OnNodeAddedRemoved;
		}

		internal MSBuildEngineManager EngineManager {
			get {
				return engineManager;
			}
			set {
				engineManager = value;
			}
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
			try {
				DisableChangeTracking ();
				this.file = file;
				IsNewProject = false;
				format = FileUtil.GetTextFormatInfo (file);

				// Load the XML document
				doc = new XmlDocument ();
				doc.PreserveWhitespace = true;
				
				// HACK: XmlStreamReader will fail if the file is encoded in UTF-8 but has <?xml version="1.0" encoding="utf-16"?>
				// To work around this, we load the XML content into a string and use XmlDocument.LoadXml() instead.
				string xml = File.ReadAllText (file);
				
				doc.LoadXml (xml);
				ClearCachedData ();

			} finally {
				EnableChangeTracking ();
			}
		}

		void ClearCachedData ()
		{
			lock (readLock) {
				itemGroups = null;
				allItems = null;
				propertyGroups = null;
				targets = null;
			}
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
			var xw = XmlWriter.Create (sw, new XmlWriterSettings {
				OmitXmlDeclaration = !doc.ChildNodes.OfType<XmlDeclaration> ().Any (),
				NewLineChars = format.NewLine
			});
			doc.Save (xw);

			string content = sw.ToString ();
			if (format.EndsWithEmptyLine && !content.EndsWith (format.NewLine))
				content += format.NewLine;

			return content;
		}

		internal void NotifyChanged ()
		{
			changeStamp++;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this project uses the msbuild engine for evaluation.
		/// </summary>
		/// <remarks>When set to false, evaluation support is limited but it allows loading projects
		/// which are not fully compliant with MSBuild (old MD projects).</remarks>
		public bool UseMSBuildEngine { get; set; }

		public void Evaluate ()
		{
			mainProjectInstance = new MSBuildProjectInstance (this);
			mainProjectInstance.Evaluate ();
		}

		public MSBuildProjectInstance CreateInstance ()
		{
			return new MSBuildProjectInstance (this);
		}

		object readLock = new object ();

		internal MSBuildProjectInstanceInfo LoadNativeInstance ()
		{
			lock (readLock) {
				var supportsMSBuild = UseMSBuildEngine && GetGlobalPropertyGroup ().GetValue ("UseMSBuildEngine", true);

				if (engineManager == null) {
					engineManager = new MSBuildEngineManager ();
					engineManagerIsLocal = true;
				}

				MSBuildEngine e = engineManager.GetEngine (supportsMSBuild);

				if (nativeProjectInfo != null && nativeProjectInfo.Engine != null && (nativeProjectInfo.Engine != e || nativeProjectInfo.ProjectStamp != ChangeStamp)) {
					nativeProjectInfo.Engine.UnloadProject (nativeProjectInfo.Project);
					nativeProjectInfo = null;
				}

				if (nativeProjectInfo == null) {
					nativeProjectInfo = new MSBuildProjectInstanceInfo {
						Engine = e,
						ProjectStamp = ChangeStamp
					};
				}

				if (nativeProjectInfo.Project == null) {
					// Use a private metadata property to assign an id to each item. This id is used to match
					// evaluated items with the items that generated them.
					int id = 0;
					List<XmlElement> idElems = new List<XmlElement> ();

					try {
						DisableChangeTracking ();
						nativeProjectInfo.ItemMap = new Dictionary<string,MSBuildItem> ();

						var docClone = (XmlDocument) doc.Clone ();

						var items = GetAllItems ().ToArray ();
						var clonedElements = docClone.DocumentElement.SelectNodes ("tns:ItemGroup/*", XmlNamespaceManager).Cast<XmlElement> ().ToArray ();

						if (items.Length != clonedElements.Length)
							throw new InvalidOperationException ("Items collection out of sync"); // Should never happen

						for (int n=0; n<items.Length; n++) {
							var it = items [n];
							var elem = clonedElements [n];
							var c = docClone.CreateElement (MSBuildProjectInstance.NodeIdPropertyName, MSBuildProject.Schema);
							string nid = (id++).ToString ();
							c.InnerXml = nid;
							elem.AppendChild (c);
							nativeProjectInfo.ItemMap [nid] = it;
							idElems.Add (c);
						}
						foreach (var it in elemCache.Values.OfType<MSBuildItem> ())
							it.EvaluatedItemCount = 0;

						PrepareForEvaluation (docClone);

						nativeProjectInfo.Project = e.LoadProject (this, docClone, FileName);
					}
					catch (Exception ex) {
						// If the project can't be evaluated don't crash
						LoggingService.LogError ("MSBuild project could not be evaluated", ex);
						throw new ProjectEvaluationException (this, ex.Message);
					}
					finally {
						EnableChangeTracking ();
					}
				}
				return nativeProjectInfo;
			}
		}

		internal void PrepareForEvaluation (XmlDocument doc)
		{
			foreach (var import in doc.DocumentElement.SelectNodes ("tns:Import", XmlNamespaceManager).Cast<XmlElement> ())
				PatchImport (import);
		}

		void PatchImport (XmlElement import)
		{
			var target = import.GetAttribute ("Project");
			var newTarget = MSBuildProjectService.GetImportRedirect (target);
			if (newTarget == null)
				return;
			
			var originalCondition = import.GetAttribute ("Condition");

			/* If an import redirect exists, add a fake import to the project which will be used only
			   if the original import doesn't exist. That is, the following import:

			   <Import Project = "PathToReplace" />

			   will be converted into:

			   <Import Project = "PathToReplace" Condition = "Exists('PathToReplace')"/>
			   <Import Project = "ReplacementPath" Condition = "!Exists('PathToReplace')" />
			*/

			// Modify the original import by adding a condition, so that this import will be used only
			// if the targets file exists.

			string cond = "Exists('" + target + "')";
			if (!string.IsNullOrEmpty (originalCondition))
				cond = "( " + originalCondition + " ) AND " + cond;
			import.SetAttribute ("Condition", cond);

			// Now add the fake import, with a condition so that it will be used only if the original
			// import does not exist.

			var fakeImport = import.OwnerDocument.CreateElement ("Import", MSBuildProject.Schema);

			cond = "!Exists('" + target + "')";
			if (!string.IsNullOrEmpty (originalCondition))
				cond = "( " + originalCondition + " ) AND " + cond;

			fakeImport.SetAttribute ("Project", MSBuildProjectService.ToMSBuildPath (null, newTarget));
			fakeImport.SetAttribute ("Condition", cond);
			import.ParentNode.InsertAfter (fakeImport, import);
		}

		public string DefaultTargets {
			get { return doc.DocumentElement.GetAttribute ("DefaultTargets"); }
			set { doc.DocumentElement.SetAttribute ("DefaultTargets", value); NotifyChanged (); }
		}
		
		public string ToolsVersion {
			get { return doc.DocumentElement.GetAttribute ("ToolsVersion"); }
			set {
				if (!string.IsNullOrEmpty (value))
					doc.DocumentElement.SetAttribute ("ToolsVersion", value);
				else
					doc.DocumentElement.RemoveAttribute ("ToolsVersion");
				NotifyChanged ();
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
			XmlUtil.Indent (format, elem, false);
			NotifyChanged ();
			return GetImport (elem);
		}

		public MSBuildImport GetImport (string name, string condition = null)
		{
			return Imports.FirstOrDefault (i => i.Project == name && i.Condition == condition);
		}
		
		public void RemoveImport (string name)
		{
			XmlElement elem = (XmlElement) doc.DocumentElement.SelectSingleNode ("tns:Import[@Project='" + name + "']", XmlNamespaceManager);
			if (elem != null) {
				XmlUtil.RemoveElementAndIndenting (elem);
				NotifyChanged ();
			}
		}
		
		public void RemoveImport (MSBuildImport import)
		{
			XmlUtil.RemoveElementAndIndenting (import.Element);
			NotifyChanged ();
		}

		public IMSBuildEvaluatedPropertyCollection EvaluatedProperties {
			get { return mainProjectInstance != null ? mainProjectInstance.EvaluatedProperties : (IMSBuildEvaluatedPropertyCollection) GetGlobalPropertyGroup (); }
		}

		public IEnumerable<IMSBuildItemEvaluated> EvaluatedItems {
			get { return mainProjectInstance.EvaluatedItems; }
		}

		public IEnumerable<IMSBuildItemEvaluated> EvaluatedItemsIgnoringCondition {
			get { return mainProjectInstance.EvaluatedItemsIgnoringCondition; }
		}

		public IEnumerable<MSBuildTarget> EvaluatedTargets {
			get { return mainProjectInstance.Targets; }
		}

		public IMSBuildPropertySet GetGlobalPropertyGroup ()
		{
			foreach (MSBuildPropertyGroup grp in PropertyGroups) {
				if (grp.Condition.Length == 0)
					return grp;
			}
			return null;
		}

		public MSBuildPropertyGroup CreatePropertyGroup ()
		{
			XmlElement elem = doc.CreateElement (null, "PropertyGroup", MSBuildProject.Schema);
			return new MSBuildPropertyGroup (this, elem);
		}

		public void AddPropertyGroup (MSBuildPropertyGroup group, bool insertAtEnd)
		{
			if (group.Project == null)
				group.SetProject (this);
			AddPropertyGroupElement (group.Element, insertAtEnd);
			AddToItemCache (group);
		}

		public MSBuildPropertyGroup AddNewPropertyGroup (bool insertAtEnd)
		{
			XmlElement elem = doc.CreateElement (null, "PropertyGroup", MSBuildProject.Schema);
			AddPropertyGroupElement (elem, insertAtEnd);
			return GetGroup (elem);
		}

		void AddPropertyGroupElement (XmlElement elem, bool insertAtEnd)
		{
			if (elem.ParentNode != null)
				throw new InvalidOperationException ("Group already belongs to a project");

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

			XmlUtil.Indent (format, elem, true);
			NotifyChanged ();
		}

		void OnNodeAddedRemoved (object sender, XmlNodeChangedEventArgs e)
		{
			if (!(e.Node is XmlElement))
				return;

			if (e.Node.ParentNode != null && e.Node.ParentNode.LocalName == "ItemGroup") {
				lock (readLock) allItems = null;
				var g = GetItemGroup ((XmlElement)e.Node.ParentNode);
				if (g != null)
					g.ResetItemCache ();
			}
			if (e.Node.ParentNode != null && e.Node.ParentNode.LocalName == "ImportGroup") {
				lock (readLock) imports = null;
				var g = GetImportGroup ((XmlElement)e.Node.ParentNode);
				if (g != null)
					g.ResetItemCache ();
			}
			if (e.Node.ParentNode == doc.DocumentElement) {
				if (e.Node.LocalName == "ItemGroup")
					lock (readLock) {
						itemGroups = null;
						allItems = null;
						allObjects = null;
					}
				else if (e.Node.LocalName == "PropertyGroup")
					lock (readLock) { propertyGroups = null; allObjects = null; }
				else if (e.Node.LocalName == "Target")
					lock (readLock) { targets = null; allObjects = null; }
				else if (e.Node.LocalName == "Import")
					lock (readLock) { imports = null; allObjects = null; }
				else if (e.Node.LocalName == "ImportGroup")
					lock (readLock) {
						importGroups = null;
						imports = null;
						allObjects = null;
					}
			}
		}

		MSBuildObject[] allObjects;
		internal IEnumerable<MSBuildObject> GetAllObjects ()
		{
			lock (readLock) {
				if (allObjects == null) {
					List<MSBuildObject> list = new List<MSBuildObject> ();
					foreach (XmlElement elem in doc.DocumentElement.ChildNodes.OfType<XmlElement> ()) {
						switch (elem.LocalName) {
						case "ItemGroup": list.Add (GetItemGroup (elem)); break;
						case "PropertyGroup": list.Add (GetGroup (elem)); break;
						case "ImportGroup": list.Add (GetImportGroup (elem)); break;
						case "Import": list.Add (GetImport (elem)); break;
						case "Target": list.Add (GetTarget (elem)); break;
						case "Choose": list.Add (GetChoose (elem)); break;
						}
					}
					allObjects = list.ToArray ();
				}
				return allObjects;
			}
		}

		MSBuildItem[] allItems;
		public IEnumerable<MSBuildItem> GetAllItems ()
		{
			lock (readLock) {
				if (allItems == null)
					allItems = doc.DocumentElement.SelectNodes ("tns:ItemGroup/*", XmlNamespaceManager).Cast<XmlElement> ().Select (e => GetItem (e)).ToArray ();
				return allItems;
			}
		}

		MSBuildPropertyGroup[] propertyGroups;
		public IEnumerable<MSBuildPropertyGroup> PropertyGroups {
			get {
				lock (readLock) {
					if (propertyGroups == null)
						propertyGroups = doc.DocumentElement.SelectNodes ("tns:PropertyGroup", XmlNamespaceManager).Cast<XmlElement> ().Select (e => GetGroup (e)).ToArray ();
					return propertyGroups;
				}
			}
		}

		MSBuildItemGroup[] itemGroups;
		public IEnumerable<MSBuildItemGroup> ItemGroups {
			get {
				lock (readLock) {
					if (itemGroups == null)
						itemGroups = doc.DocumentElement.SelectNodes ("tns:ItemGroup", XmlNamespaceManager).Cast<XmlElement> ().Select (e => GetItemGroup (e)).ToArray ();
					return itemGroups;
				}
			}
		}

		MSBuildImportGroup[] importGroups;
		public IEnumerable<MSBuildImportGroup> ImportGroups {
			get {
				lock (readLock) {
					if (importGroups == null)
						importGroups = doc.DocumentElement.SelectNodes ("tns:ImportGroup", XmlNamespaceManager).Cast<XmlElement> ().Select (e => GetImportGroup (e)).ToArray ();
					return importGroups;
				}
			}
		}

		MSBuildTarget[] targets;
		public IEnumerable<MSBuildTarget> Targets {
			get {
				lock (readLock) {
					if (targets == null)
						targets = doc.DocumentElement.SelectNodes ("tns:Target", XmlNamespaceManager).Cast<XmlElement> ().Select (t => GetTarget (t)).ToArray ();
					return targets;
				}
			}
		}

		MSBuildImport[] imports;
		public IEnumerable<MSBuildImport> Imports {
			get {
				lock (readLock) {
					if (imports == null)
						imports = doc.DocumentElement.SelectNodes ("tns:Import", XmlNamespaceManager).Cast<XmlElement> ().Select (e => GetImport (e)).ToArray ();
					return imports;
				}
			}
		}

		public MSBuildItemGroup AddNewItemGroup ()
		{
			XmlElement elem = doc.CreateElement (null, "ItemGroup", MSBuildProject.Schema);

			XmlNode refNode = null;
			var lastGroup = ItemGroups.LastOrDefault ();
			if (lastGroup != null)
				refNode = lastGroup.Element;
			else {
				var g = PropertyGroups.LastOrDefault ();
				if (g != null)
					refNode = g.Element;
			}
			if (refNode != null)
				doc.DocumentElement.InsertAfter (elem, refNode);
			else
				doc.DocumentElement.AppendChild (elem);
			XmlUtil.Indent (format, elem, true);
			NotifyChanged ();
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
				XmlUtil.RemoveElementAndIndenting (sec);
			}
			XmlUtil.Indent (format, value, true);
			NotifyChanged ();
		}

		public void SetMonoDevelopProjectExtension (string section, XmlElement value)
		{
			if (value.OwnerDocument != doc)
				value = (XmlElement)doc.ImportNode (value, true);

			XmlElement elem = doc.DocumentElement ["ProjectExtensions", MSBuildProject.Schema];
			if (elem == null) {
				elem = doc.CreateElement (null, "ProjectExtensions", MSBuildProject.Schema);
				doc.DocumentElement.AppendChild (elem);
				XmlUtil.Indent (format, elem, true);
			}
			var parent = elem;
			elem = parent ["MonoDevelop", MSBuildProject.Schema];
			if (elem == null) {
				elem = doc.CreateElement (null, "MonoDevelop", MSBuildProject.Schema);
				parent.AppendChild (elem);
				XmlUtil.Indent (format, elem, true);
			}
			parent = elem;
			elem = parent ["Properties", MSBuildProject.Schema];
			if (elem == null) {
				elem = doc.CreateElement (null, "Properties", MSBuildProject.Schema);
				parent.AppendChild (elem);
				XmlUtil.Indent (format, elem, true);
			}
			XmlElement sec = elem [section];
			if (sec == null)
				elem.AppendChild (value);
			else {
				elem.InsertAfter (value, sec);
				XmlUtil.RemoveElementAndIndenting (sec);
			}
			XmlUtil.Indent (format, value, false);
			var xmlns = value.GetAttribute ("xmlns");
			if (xmlns == Schema)
				value.RemoveAttribute ("xmlns");
			NotifyChanged ();
		}

		public void RemoveProjectExtension (string section)
		{
			XmlElement elem = doc.DocumentElement.SelectSingleNode ("tns:ProjectExtensions/tns:" + section, XmlNamespaceManager) as XmlElement;
			if (elem != null) {
				XmlElement parent = (XmlElement) elem.ParentNode;
				XmlUtil.RemoveElementAndIndenting (elem);
				if (parent.ChildNodes.OfType<XmlNode>().All (n => n is XmlWhitespace))
					XmlUtil.RemoveElementAndIndenting (parent);
				NotifyChanged ();
			}
		}
		
		public void RemoveMonoDevelopProjectExtension (string section)
		{
			XmlElement elem = doc.DocumentElement.SelectSingleNode ("tns:ProjectExtensions/tns:MonoDevelop/tns:Properties/tns:" + section, XmlNamespaceManager) as XmlElement;
			if (elem != null) {
				do {
					XmlElement parent = (XmlElement) elem.ParentNode;
					XmlUtil.RemoveElementAndIndenting (elem);
					elem = parent;
				}
				while (elem.ChildNodes.OfType<XmlNode>().All (n => n is XmlWhitespace));
				NotifyChanged ();
			}
		}

		public void RemoveItem (MSBuildItem item)
		{
			elemCache.Remove (item.Element);
			XmlElement parent = (XmlElement) item.Element.ParentNode;
			XmlUtil.RemoveElementAndIndenting (item.Element);
			if (parent.ChildNodes.OfType<XmlNode>().All (n => n is XmlWhitespace)) {
				elemCache.Remove (parent);
				XmlUtil.RemoveElementAndIndenting (parent);
				lock (readLock)
					bestGroups = null;
			}
			NotifyChanged ();
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

		internal MSBuildPropertyGroup GetGroup (XmlElement elem)
		{
			MSBuildObject ob;
			if (elemCache.TryGetValue (elem, out ob))
				return (MSBuildPropertyGroup) ob;
			MSBuildPropertyGroup it = new MSBuildPropertyGroup (this, elem);
			elemCache [elem] = it;
			return it;
		}
		
		internal MSBuildItemGroup GetItemGroup (XmlElement elem)
		{
			MSBuildObject ob;
			if (elemCache.TryGetValue (elem, out ob))
				return (MSBuildItemGroup) ob;
			MSBuildItemGroup it = new MSBuildItemGroup (this, elem);
			elemCache [elem] = it;
			return it;
		}
		
		internal MSBuildImportGroup GetImportGroup (XmlElement elem)
		{
			MSBuildObject ob;
			if (elemCache.TryGetValue (elem, out ob))
				return (MSBuildImportGroup) ob;
			MSBuildImportGroup it = new MSBuildImportGroup (this, elem);
			elemCache [elem] = it;
			return it;
		}

		public void RemoveGroup (MSBuildPropertyGroup grp)
		{
			elemCache.Remove (grp.Element);
			XmlUtil.RemoveElementAndIndenting (grp.Element);
			NotifyChanged ();
		}

		internal MSBuildTarget GetTarget (XmlElement elem)
		{
			MSBuildObject ob;
			if (elemCache.TryGetValue (elem, out ob))
				return (MSBuildTarget) ob;
			MSBuildTarget it = new MSBuildTarget (elem);
			elemCache [elem] = it;
			return it;
		}

		internal MSBuildChoose GetChoose (XmlElement elem)
		{
			MSBuildObject ob;
			if (elemCache.TryGetValue (elem, out ob))
				return (MSBuildChoose) ob;
			MSBuildChoose it = new MSBuildChoose (this, elem);
			elemCache [elem] = it;
			return it;
		}
	}
	
	public class MSBuildTarget: MSBuildObject
	{
		public MSBuildTarget (XmlElement elem): base (elem)
		{
		}

		public string Name {
			get { return Element.GetAttribute ("Name"); }
		}

		public bool IsImported { get; internal set; }

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
		}
	}

	static class XmlUtil
	{
		public static void RemoveElementAndIndenting (XmlElement elem)
		{
			var ws = elem.PreviousSibling as XmlWhitespace;
			elem.ParentNode.RemoveChild (elem);

			while (ws != null) {
				var t = ws.InnerText;
				t = t.TrimEnd (' ');
				bool hasNewLine = t.Length > 0 && (t[t.Length - 1] == '\r' || t[t.Length - 1] == '\n');
				if (hasNewLine)
					t = RemoveLineEnd (t);

				if (t.Length == 0) {
					var nextws = ws.PreviousSibling as XmlWhitespace;
					ws.ParentNode.RemoveChild (ws);
					ws = nextws;
					if (hasNewLine)
						break;
				} else {
					ws.InnerText = t;
					break;
				}
			}
		}

		static string RemoveLineEnd (string s)
		{
			if (s[s.Length - 1] == '\n') {
				if (s.Length > 1 && s[s.Length - 2] == '\r')
					return s.Substring (0, s.Length - 2);
			}
			return s.Substring (0, s.Length - 1);
		}

		static string GetIndentString (XmlNode elem)
		{
			var node = elem.PreviousSibling;
			StringBuilder res = new StringBuilder ();

			while (node != null) {
				var ws = node as XmlWhitespace;
				if (ws != null) {
					var t = ws.InnerText;
					int i = t.LastIndexOfAny (new [] { '\r','\n' });
					if (i == -1) {
						res.Append (t);
					} else {
						res.Append (t.Substring (i + 1));
						return res.ToString ();
					}
				} else
					res.Clear ();
				node = node.PreviousSibling;
			}
			return res.ToString ();
		}

		public static void FormatElement (TextFormatInfo format, XmlElement elem)
		{
			// Remove duplicate namespace declarations
			var nsa = elem.Attributes["xmlns"];
			if (nsa != null && nsa.Value == MSBuildProject.Schema)
				elem.Attributes.Remove (nsa);
			
			foreach (var e in elem.ChildNodes.OfType<XmlElement> ().ToArray ()) {
				Indent (format, e, false);
				FormatElement (format, e);
			}
		}

		public static void Indent (TextFormatInfo format, XmlElement elem, bool closeInNewLine)
		{
			var prev = FindPreviousSibling (elem);

			string indent;
			if (prev != null)
				indent = GetIndentString (prev);
			else if (elem != elem.OwnerDocument.DocumentElement)
				indent = GetIndentString (elem.ParentNode) + "  ";
			else
				indent = "";
			
			Indent (format, elem, indent);
			if (elem.ChildNodes.Count == 0 && closeInNewLine) {
				var ws = elem.OwnerDocument.CreateWhitespace (format.NewLine + indent);
				elem.AppendChild (ws);
			}

			if (elem.NextSibling is XmlElement)
				SetIndent (format, (XmlElement)elem.NextSibling, indent);
			
			if (elem.NextSibling == null && elem != elem.OwnerDocument.DocumentElement) {
				var parentIndent = GetIndentString (elem.ParentNode);
				var ws = elem.OwnerDocument.CreateWhitespace (format.NewLine + parentIndent);
				elem.ParentNode.InsertAfter (ws, elem);
			}
		}

		static XmlElement FindPreviousSibling (XmlElement elem)
		{
			XmlNode node = elem;
			do {
				node = node.PreviousSibling;
			} while (node != null && !(node is XmlElement));
			return node as XmlElement;
		}

		static void Indent (TextFormatInfo format, XmlElement elem, string indent)
		{
			SetIndent (format, elem, indent);
			foreach (var c in elem.ChildNodes.OfType<XmlElement> ())
				Indent (format, c, indent + "  ");
		}

		static void SetIndent (TextFormatInfo format, XmlElement elem, string indent)
		{
			bool foundLineBreak = RemoveIndent (elem.PreviousSibling);
			string newIndent = foundLineBreak ? indent : format.NewLine + indent;

			var ws = elem.OwnerDocument.CreateWhitespace (newIndent);
			if (elem.ParentNode is XmlDocument) {
				// MS.NET doesn't allow inserting whitespace if there is a document element. The workaround
				// is to remove the element, add the space, then add back the element
				var doc = elem.OwnerDocument;
				doc.RemoveChild (elem);
				doc.AppendChild (ws);
				doc.AppendChild (elem);
			} else
				elem.ParentNode.InsertBefore (ws, elem);

			if (elem.ChildNodes.OfType<XmlElement> ().Any ()) {
				foundLineBreak = RemoveIndent (elem.LastChild);
				newIndent = foundLineBreak ? indent : format.NewLine + indent;
				elem.AppendChild (elem.OwnerDocument.CreateWhitespace (newIndent));
			}
		}

		static bool RemoveIndent (XmlNode node)
		{
			List<XmlNode> toDelete = new List<XmlNode> ();
			bool foundLineBreak = false;

			var ws = node as XmlWhitespace;
			while (ws != null) {
				var t = ws.InnerText;
				int i = t.LastIndexOfAny (new [] { '\r','\n' });
				if (i == -1) {
					toDelete.Add (ws);
				} else {
					ws.InnerText = t.Substring (0, i + 1);
					foundLineBreak = true;
					break;
				}
				ws = ws.PreviousSibling as XmlWhitespace;
			}
			foreach (var n in toDelete)
				n.ParentNode.RemoveChild (n);
			return foundLineBreak;
		}
	}
}
