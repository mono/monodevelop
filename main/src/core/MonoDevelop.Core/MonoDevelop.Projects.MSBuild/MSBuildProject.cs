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
using MonoDevelop.Core.Assemblies;

namespace MonoDevelop.Projects.MSBuild
{
	public sealed partial class MSBuildProject : MSBuildObject, IDisposable
	{
		FilePath file;
		Dictionary<string, MSBuildItemGroup> bestGroups;
		MSBuildProjectInstance mainProjectInstance;
		int changeStamp;
		bool hadXmlDeclaration;
		bool isShared;
		ConditionedPropertyCollection conditionedProperties = new ConditionedPropertyCollection ();
		Dictionary<string, string[]> knownItemAttributes;
		Dictionary<string,string> globalProperties = new Dictionary<string, string> ();

		MSBuildEngineManager engineManager;
		bool engineManagerIsLocal;

		MSBuildProjectInstanceInfo nativeProjectInfo;

		public const string Schema = "http://schemas.microsoft.com/developer/msbuild/2003";
		static XmlNamespaceManager manager;

		TextFormatInfo format = new TextFormatInfo { NewLine = "\r\n" };

		static readonly string [] knownAttributes = { "Sdk", "DefaultTargets", "ToolsVersion", "xmlns" };

		public static XmlNamespaceManager XmlNamespaceManager
		{
			get
			{
				if (manager == null) {
					manager = new XmlNamespaceManager (new NameTable ());
					manager.AddNamespace ("tns", Schema);
				}
				return manager;
			}
		}

		public FilePath FileName
		{
			get { return file; }
			set { AssertCanModify (); file = value; }
		}

		public FilePath BaseDirectory
		{
			get { return file.ParentDirectory; }
		}

		FilePath solutionDirectory;

		public FilePath SolutionDirectory {
			get { return solutionDirectory; }
			set {
				solutionDirectory = value;
				if (!solutionDirectory.IsNullOrEmpty)
					SetGlobalProperty ("SolutionDir", solutionDirectory.ToString () + Path.DirectorySeparatorChar);
				else
					RemoveGlobalProperty ("SolutionDir");
			}
		}

		public MSBuildFileFormat Format
		{
			get;
			set;
		}

		internal TextFormatInfo TextFormat
		{
			get { return format; }
		}

		public bool IsNewProject { get; internal set; }

		internal int ChangeStamp
		{
			get { return changeStamp; }
		}

		internal object ReadLock
		{
			get { return readLock; }
		}

		public MSBuildProject ()
		{
			ParentProject = this;
			hadXmlDeclaration = true;
			mainProjectInstance = new MSBuildProjectInstance (this);
			UseMSBuildEngine = true;
			IsNewProject = true;
			initialWhitespace = format.NewLine;
			StartInnerWhitespace = format.NewLine;
			AddNewPropertyGroup (false);
			EnableChangeTracking ();
		}

		internal MSBuildProject (MSBuildEngineManager manager) : this ()
		{
			engineManager = manager;
		}

		public void Dispose ()
		{
			DisposeMainInstance ();
		}

		void DisposeMainInstance ()
		{
			if (nativeProjectInfo != null) {
				if (mainProjectInstance != null)
					mainProjectInstance.Dispose ();
				nativeProjectInfo.Engine.UnloadProject (nativeProjectInfo.Project);
				if (engineManagerIsLocal)
					nativeProjectInfo.Engine.Dispose ();
				nativeProjectInfo = null;
				mainProjectInstance = null;
			}
		}

		internal override void AssertCanModify ()
		{
			if (isShared)
				Runtime.AssertMainThread ();
		}

		/// <summary>
		/// Gets a value indicating whether this instance is shared.
		/// </summary>
		/// <remarks>Shared objects can only be modified in the main thread</remarks>
		public bool IsShared {
			get { return isShared; }
		}

		/// <summary>
		/// Sets this object as shared, which means that it is accessible from several threads for reading,
		/// but it can only be modified in the main thread
		/// </summary>
		public void SetShared ()
		{
			isShared = true;
		}


		void EnableChangeTracking ()
		{
		}

		void DisableChangeTracking ()
		{
		}

		internal MSBuildEngineManager EngineManager
		{
			get
			{
				return engineManager;
			}
			set
			{
				engineManager = value;
			}
		}

		public TargetRuntime TargetRuntime { get; set; } = Runtime.SystemAssemblyService.DefaultRuntime;

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
			Load (file, new MSBuildXmlReader ());
		}

		internal void Load (string file, MSBuildXmlReader reader)
		{
			AssertCanModify ();
			try {
				this.file = file;
				IsNewProject = false;
				format = FileUtil.GetTextFormatInfo (file);

				// HACK: XmlStreamReader will fail if the file is encoded in UTF-8 but has <?xml version="1.0" encoding="utf-16"?>
				// To work around this, we load the XML content into a string and use XmlDocument.LoadXml() instead.
				string xml = File.ReadAllText (file);

				LoadXml (xml, reader);

			} finally {
				EnableChangeTracking ();
			}
		}

		public void LoadXml (string xml)
		{
			LoadXml (xml, new MSBuildXmlReader ());
		}

		internal void LoadXml (string xml, MSBuildXmlReader reader)
		{
			AssertCanModify ();
			try {
				DisableChangeTracking ();
				var xr = new XmlTextReader (new StringReader (xml));
				xr.WhitespaceHandling = WhitespaceHandling.All;
				xr.Normalization = false;
				reader.XmlReader = xr;
				LoadFromXml (reader);
			} finally {
				EnableChangeTracking ();
			}
		}

		object initialWhitespace;

		void LoadFromXml (MSBuildXmlReader reader)
		{
			AssertCanModify ();
			DisposeMainInstance ();
			ChildNodes = ChildNodes.Clear ();
			conditionedProperties = new ConditionedPropertyCollection ();
			bestGroups = null;
			hadXmlDeclaration = false;
			initialWhitespace = null;
			StartInnerWhitespace = null;

			while (!reader.EOF && reader.NodeType != XmlNodeType.Element) {
				if (reader.NodeType == XmlNodeType.XmlDeclaration) {
					initialWhitespace = reader.ConsumeWhitespace ();
					hadXmlDeclaration = true;
					reader.Read ();
				}
				else if (reader.IsWhitespace)
					reader.ReadAndStoreWhitespace ();
				else
					reader.Read ();
			}

			if (reader.EOF)
				return;

			Read (reader);

			while (!reader.EOF) {
				if (reader.IsWhitespace)
					reader.ReadAndStoreWhitespace ();
				else
					reader.Read ();
			}
		}

		internal override string [] GetKnownAttributes ()
		{
			return knownAttributes;
		}

		internal override string GetElementName ()
		{
			return "Project";
		}

		internal override void ReadAttribute (string name, string value)
		{
			switch (name) {
				case "DefaultTargets": defaultTargets = value; return;
				case "ToolsVersion": toolsVersion = value; return;
				case "Sdk": Sdk = value; return;
			}
			base.ReadAttribute (name, value);
		}

		internal override string WriteAttribute (string name)
		{
			switch (name) {
				case "DefaultTargets": return defaultTargets;
				case "ToolsVersion": return toolsVersion;
				case "xmlns": return string.IsNullOrEmpty (Namespace) ? null : Namespace;
				case "Sdk": return Sdk;
			}
			return base.WriteAttribute (name);
		}

		internal override void ReadChildElement (MSBuildXmlReader reader)
		{
			MSBuildObject ob = null;
			switch (reader.LocalName) {
				case "ItemGroup": ob = new MSBuildItemGroup (); break;
				case "PropertyGroup": ob = new MSBuildPropertyGroup (); break;
				case "ImportGroup": ob = new MSBuildImportGroup (); break;
				case "Import": ob = new MSBuildImport (); break;
				case "Target": ob = new MSBuildTarget (); break;
				case "Choose": ob = new MSBuildChoose (); break;
				case "ProjectExtensions": ob = new MSBuildProjectExtensions (); break;
				default: ob = new MSBuildXmlElement (); break;
			}
			if (ob != null) {
				ob.ParentNode = this;
				ob.Read (reader);
				ChildNodes = ChildNodes.Add (ob);
			} else
				base.ReadChildElement (reader);
		}

		class ProjectWriter : StringWriter
		{
			Encoding encoding;

			public ProjectWriter (ByteOrderMark bom)
			{
				encoding = bom != null ? Encoding.GetEncoding (bom.Name) : null;
				ByteOrderMark = bom;
			}

			public ByteOrderMark ByteOrderMark
			{
				get; private set;
			}

			public override Encoding Encoding
			{
				get { return encoding ?? Encoding.UTF8; }
			}
		}

		public bool Save (string fileName)
		{
			string content = SaveToString ();
			return TextFile.WriteFile (fileName, content, format.ByteOrderMark, true);
		}

		public Task<bool> SaveAsync (string fileName)
		{
			return Task.Run (() => {
				string content = SaveToString ();
				return TextFile.WriteFile (fileName, content, format.ByteOrderMark, true);
			});
		}

		internal Task<bool> SaveAsync (string fileName, string content)
		{
			return Task.Run (() => {
				return TextFile.WriteFile (fileName, content, format.ByteOrderMark, true);
			});
		}

		public string SaveToString ()
		{
			IsNewProject = false;
			return SaveToString (new WriteContext ());
		}

		string SaveToString (WriteContext ctx)
		{
			// StringWriter.Encoding always returns UTF16. We need it to return UTF8, so the
			// XmlDocument will write the UTF8 header.
			ProjectWriter sw = new ProjectWriter (format.ByteOrderMark);
			sw.NewLine = format.NewLine;
			var xw = XmlWriter.Create (sw, new XmlWriterSettings {
				OmitXmlDeclaration = !hadXmlDeclaration,
				NewLineChars = format.NewLine,
				NewLineHandling = NewLineHandling.Replace
			});

			MSBuildWhitespace.Write (initialWhitespace, xw);

			Save (xw);

			xw.Dispose ();

			return sw.ToString ();
		}

		public void Save (XmlWriter writer)
		{
			Save (writer, new WriteContext ());
		}

		void Save (XmlWriter writer, WriteContext ctx)
		{
			Write (writer, ctx);
		}

		internal new void NotifyChanged ()
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
			if (mainProjectInstance != null)
				mainProjectInstance.Dispose ();
			mainProjectInstance = new MSBuildProjectInstance (this);
			mainProjectInstance.Evaluate ();
			conditionedProperties = mainProjectInstance.GetConditionedProperties ();
		}

		public Task EvaluateAsync ()
		{
			if (mainProjectInstance != null)
				mainProjectInstance.Dispose ();
			mainProjectInstance = new MSBuildProjectInstance (this);
			return mainProjectInstance.EvaluateAsync ().ContinueWith (t => {
				conditionedProperties = mainProjectInstance.GetConditionedProperties ();
			});
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

					try {
						DisableChangeTracking ();

						var ctx = new WriteContext {
							Evaluating = true,
							ItemMap = new Dictionary<string, MSBuildItem> ()
						};
						var xml = SaveToString (ctx);

						foreach (var it in GetAllItems ())
							it.EvaluatedItemCount = 0;

						nativeProjectInfo.Project = e.LoadProject (this, xml, FileName);
					} catch (Exception ex) {
						// If the project can't be evaluated don't crash
						LoggingService.LogError ("MSBuild project could not be evaluated", ex);
						throw new ProjectEvaluationException (this, ex.Message);
					} finally {
						EnableChangeTracking ();
					}
				}
				return nativeProjectInfo;
			}
		}

		string defaultTargets;

		public string DefaultTargets
		{
			get { return defaultTargets; }
			set { AssertCanModify (); defaultTargets = value; NotifyChanged (); }
		}

		string toolsVersion;

		public string ToolsVersion
		{
			get { return toolsVersion; }
			set
			{
				AssertCanModify ();
				toolsVersion = value;
				NotifyChanged ();
			}
		}

		string sdk;
		string[] sdkArray;
		public string Sdk {
			get => sdk;
			set {
				sdk = value;
				sdkArray = null;
			}
		}

		public override string Namespace {
			get {
				if (Sdk != null)
					return string.Empty;
				return Schema;
			}
		}

		public string [] ProjectTypeGuids
		{
			get { return GetGlobalPropertyGroup ().GetValue ("ProjectTypeGuids", "").Split (new [] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select (t => t.Trim ()).ToArray (); }
			set { GetGlobalPropertyGroup ().SetValue ("ProjectTypeGuids", string.Join (";", value), preserveExistingCase: true); }
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

		public MSBuildImport AddNewImport (string name, string condition = null, MSBuildObject beforeObject = null)
		{
			AssertCanModify ();
			var import = new MSBuildImport {
				Project = name,
				Condition = condition
			};

			int index = -1;
			if (beforeObject != null)
				index = ChildNodes.IndexOf (beforeObject);
			else {
				index = ChildNodes.FindLastIndex (ob => ob is MSBuildImport);
				if (index != -1)
					index++;
			}

			import.ParentNode = this;

			if (index != -1)
				ChildNodes = ChildNodes.Insert (index, import);
			else
				ChildNodes = ChildNodes.Add (import);

			import.ResetIndent (false);
			NotifyChanged ();
			return import;
		}

		public MSBuildImport GetImport (string name, string condition = null)
		{
			return Imports.FirstOrDefault (i => string.Equals (i.Project, name, StringComparison.OrdinalIgnoreCase) && (condition == null || i.Condition == condition));
		}

		public void RemoveImport (string name, string condition = null)
		{
			AssertCanModify ();
			var i = GetImport (name, condition);
			if (i != null) {
				i.RemoveIndent ();
				ChildNodes = ChildNodes.Remove (i);
				NotifyChanged ();
			}
		}

		public void RemoveImport (MSBuildImport import)
		{
			AssertCanModify ();
			if (import.ParentProject != this)
				throw new InvalidOperationException ("Import object does not belong to this project");

			if (import.ParentObject == this) {
				import.RemoveIndent ();
				ChildNodes = ChildNodes.Remove (import);
				NotifyChanged ();
			} else
				((MSBuildImportGroup)import.ParentObject).RemoveImport (import);
		}

		public ConditionedPropertyCollection ConditionedProperties {
			get {
				return conditionedProperties;
			}
		}

		public IMSBuildEvaluatedPropertyCollection EvaluatedProperties
		{
			get { return mainProjectInstance != null ? mainProjectInstance.EvaluatedProperties : (IMSBuildEvaluatedPropertyCollection)GetGlobalPropertyGroup (); }
		}

		public IEnumerable<IMSBuildItemEvaluated> EvaluatedItems
		{
			get { return mainProjectInstance.EvaluatedItems; }
		}

		public IEnumerable<IMSBuildItemEvaluated> EvaluatedItemsIgnoringCondition
		{
			get { return mainProjectInstance.EvaluatedItemsIgnoringCondition; }
		}

		public IEnumerable<IMSBuildTargetEvaluated> EvaluatedTargets
		{
			get { return mainProjectInstance.Targets; }
		}

		public IEnumerable<IMSBuildTargetEvaluated> EvaluatedTargetsIgnoringCondition
		{
			get { return mainProjectInstance.TargetsIgnoringCondition; }
		}

		public IEnumerable<MSBuildItem> FindGlobItemsIncludingFile (string include)
		{
			return mainProjectInstance.FindGlobItemsIncludingFile (include);
		}

		internal IEnumerable<MSBuildItem> FindUpdateGlobItemsIncludingFile (string include, MSBuildItem globItem)
		{
			return mainProjectInstance.FindUpdateGlobItemsIncludingFile (include, globItem);
		}

		public MSBuildPropertyGroup GetGlobalPropertyGroup ()
		{
			return PropertyGroups.FirstOrDefault (g => g.Condition.Length == 0);
		}

		public MSBuildPropertyGroup CreatePropertyGroup ()
		{
			return new MSBuildPropertyGroup ();
		}

		public MSBuildPropertyGroup AddNewPropertyGroup (bool insertAtEnd = true, MSBuildObject beforeObject = null)
		{
			var group = new MSBuildPropertyGroup ();
			AddPropertyGroup (group, insertAtEnd, beforeObject);
			return group;
		}

		public void AddPropertyGroup (MSBuildPropertyGroup group, bool insertAtEnd = true, MSBuildObject beforeObject = null)
		{
			AssertCanModify ();
			if (group.ParentProject != null)
				throw new InvalidOperationException ("Group already belongs to a project");

			group.ParentNode = this;

			bool added = false;
			if (beforeObject != null) {
				var index = ChildNodes.IndexOf (beforeObject);
				if (index != -1) {
					ChildNodes = ChildNodes.Insert (index, group);
					added = true;
				}
			}
			if (!added) {
				if (insertAtEnd) {
					var last = ChildNodes.FindLastIndex (g => g is MSBuildPropertyGroup);
					if (last != -1) {
						ChildNodes = ChildNodes.Insert (last + 1, group);
						added = true;
					}
				} else {
					var first = ChildNodes.FindIndex (g => g is MSBuildPropertyGroup);
					if (first != -1) {
						ChildNodes = ChildNodes.Insert (first, group);
						added = true;
					}
				}
				if (!added) {
					var first = ChildNodes.FindIndex (g => g is MSBuildItemGroup);
					if (first != -1)
						ChildNodes = ChildNodes.Insert (first, group);
					else
						ChildNodes = ChildNodes.Add (group);
				}
			}

			group.ResetIndent (true);
			NotifyChanged ();
		}

		public IEnumerable<MSBuildObject> GetAllObjects ()
		{
			return ChildNodes.OfType<MSBuildObject> ();
		}

		public IEnumerable<MSBuildItem> GetAllItems ()
		{
			return GetAllItems (ChildNodes.OfType<MSBuildObject> ());
		}

		IEnumerable<MSBuildItem> GetAllItems (IEnumerable<MSBuildObject> list)
		{
			foreach (var ob in list) {
				if (ob is MSBuildItemGroup) {
					foreach (var it in ((MSBuildItemGroup)ob).Items)
						yield return it;
				} else if (ob is MSBuildChoose) {
					foreach (var op in ((MSBuildChoose)ob).GetOptions ()) {
						foreach (var c in GetAllItems (op.GetAllObjects ()))
							yield return c;
					}
				}
			}
		}

		public IEnumerable<MSBuildPropertyGroup> PropertyGroups
		{
			get { return ChildNodes.OfType<MSBuildPropertyGroup> (); }
		}

		public IEnumerable<MSBuildItemGroup> ItemGroups
		{
			get { return ChildNodes.OfType<MSBuildItemGroup> (); }
		}

		public IEnumerable<MSBuildImportGroup> ImportGroups
		{
			get { return ChildNodes.OfType<MSBuildImportGroup> (); }
		}

		public IEnumerable<MSBuildTarget> Targets
		{
			get { return ChildNodes.OfType<MSBuildTarget> (); }
		}

		public IEnumerable<MSBuildImport> Imports
		{
			get { return ChildNodes.OfType<MSBuildImport> (); }
		}

		public MSBuildItemGroup AddNewItemGroup ()
		{
			return AddNewItemGroup (null);
		}

		public MSBuildItemGroup AddNewItemGroup (MSBuildObject beforeObject)
		{
			AssertCanModify ();
			var group = new MSBuildItemGroup ();

			MSBuildObject refNode = null;
			var lastGroup = ItemGroups.LastOrDefault ();
			if (lastGroup != null)
				refNode = lastGroup;
			else {
				var g = PropertyGroups.LastOrDefault ();
				if (g != null)
					refNode = g;
			}

			group.ParentNode = this;
			if (beforeObject != null)
				ChildNodes = ChildNodes.Insert (ChildNodes.IndexOf (beforeObject), group);
			else if (refNode != null)
				ChildNodes = ChildNodes.Insert (ChildNodes.IndexOf (refNode) + 1, group);
			else
				ChildNodes = ChildNodes.Add (group);

			group.ResetIndent (true);
			NotifyChanged ();
			return group;
		}

		public MSBuildItem AddNewItem (string name, string include)
		{
			return AddNewItem (name, include, null);
		}

		public MSBuildItem AddNewItem (string name, string include, MSBuildItem beforeItem)
		{
			if (beforeItem != null) {
				var group = beforeItem.ParentNode as MSBuildItemGroup;
				if (group != null)
					return group.AddNewItem (name, include, beforeItem);
			}
			MSBuildItem it = CreateItem (name, include);
			AddItem (it);
			return it;
		}

		public MSBuildItem CreateItem (string name, string include)
		{
			var bitem = new MSBuildItem (name);
			bitem.Include = include;
			return bitem;
		}

		public void AddItem (MSBuildItem it)
		{
			AddItem (it, null);
		}

		public void AddItem (MSBuildItem it, MSBuildItem beforeItem)
		{
			if (string.IsNullOrEmpty (it.Name))
				throw new InvalidOperationException ("Item doesn't have a name");

			if (beforeItem != null) {
				var group = beforeItem.ParentNode as MSBuildItemGroup;
				if (group != null) {
					group.AddItem (it, beforeItem);
					return;
				}
			}
			MSBuildItemGroup grp = FindBestGroupForItem (it);
			grp.AddItem (it);
		}

		MSBuildItemGroup FindBestGroupForItem (MSBuildItem newItem)
		{
			string groupId = GetBestGroupId (newItem);
			MSBuildItemGroup group;

			if (bestGroups == null)
				bestGroups = new Dictionary<string, MSBuildItemGroup> ();
			else {
				if (bestGroups.TryGetValue (groupId, out group))
					return group;
			}

			MSBuildItemGroup insertBefore = null;
			foreach (MSBuildItemGroup grp in ItemGroups) {
				foreach (MSBuildItem it in grp.Items) {
					if (ShouldAddItemToGroup (it, newItem)) {
						bestGroups [groupId] = grp;
						return grp;
					} else if (insertBefore == null && ShouldInsertItemGroupBefore (it, newItem)) {
						insertBefore = grp;
					}
				}
			}
			group = AddNewItemGroup (insertBefore);
			bestGroups [groupId] = group;
			return group;
		}

		static string GetBestGroupId (MSBuildItem it)
		{
			if (it.IsRemove)
				return it.Name + ":Remove";
			else if (it.IsUpdate)
				return it.Name + ":Update";
			return it.Name;
		}

		static bool ShouldAddItemToGroup (MSBuildItem existingItem, MSBuildItem newItem)
		{
			return existingItem.Name == newItem.Name &&
				existingItem.IsRemove == newItem.IsRemove &&
				existingItem.IsUpdate == newItem.IsUpdate;
		}

		static bool ShouldInsertItemGroupBefore (MSBuildItem existing, MSBuildItem newItem)
		{
			if (existing.Name != newItem.Name)
				return false;
			if (newItem.IsInclusion)
				return existing.IsUpdate;
			if (newItem.IsRemove)
				return existing.IsUpdate || (existing.IsInclusion && !existing.IsWildcardItem);
			return false;
		}

		public XmlElement GetProjectExtension (string section)
		{
			var ext = (MSBuildProjectExtensions)ChildNodes.FirstOrDefault (ob => ob is MSBuildProjectExtensions);
			if (ext != null)
				return ext.GetProjectExtension (section);
			return null;
		}

		public XmlElement GetMonoDevelopProjectExtension (string section)
		{
			var elem = GetProjectExtension ("MonoDevelop");
			if (elem != null)
				return elem.SelectSingleNode ("tns:Properties/tns:" + section, GetNamespaceManagerForProject ()) as XmlElement;
			else
				return null;
		}

		/// <summary>
		/// Returns a list of SDKs referenced by this project
		/// </summary>
		public string[] GetReferencedSDKs ()
		{
			if (!string.IsNullOrEmpty (Sdk)) {
				if (sdkArray == null)
					sdkArray = Sdk.Split (new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
				return sdkArray;
			}
			else
				return Array.Empty<string> ();
		}

		XmlNamespaceManager GetNamespaceManagerForProject ()
		{
			if (Namespace == Schema)
				return XmlNamespaceManager;

			var namespaceManager = new XmlNamespaceManager (new NameTable ());
			namespaceManager.AddNamespace ("tns", Namespace);
			return namespaceManager;
		}

		public void SetProjectExtension (XmlElement value)
		{
			AssertCanModify ();
			var ext = (MSBuildProjectExtensions)ChildNodes.FirstOrDefault (ob => ob is MSBuildProjectExtensions);
			if (ext == null) {
				ext = new MSBuildProjectExtensions ();
				ext.ParentNode = this;
				ChildNodes = ChildNodes.Add (ext);
				ext.ResetIndent (false);
			}
			ext.SetProjectExtension (value);
			NotifyChanged ();
		}

		public void SetMonoDevelopProjectExtension (string section, XmlElement value)
		{
			AssertCanModify ();
			var elem = GetProjectExtension ("MonoDevelop");
			if (elem == null) {
				XmlDocument doc = new XmlDocument ();
				elem = doc.CreateElement (null, "MonoDevelop", Namespace);
			}
			value = (XmlElement) elem.OwnerDocument.ImportNode (value, true);
			var parent = elem;
			elem = parent ["Properties", Namespace];
			if (elem == null) {
				elem = parent.OwnerDocument.CreateElement (null, "Properties", Namespace);
				parent.AppendChild (elem);
				XmlUtil.Indent (format, elem, true);
			}
			XmlElement sec = elem [value.LocalName];
			if (sec == null)
				elem.AppendChild (value);
			else {
				elem.InsertAfter (value, sec);
				XmlUtil.RemoveElementAndIndenting (sec);
			}
			XmlUtil.Indent (format, value, false);
			var xmlns = value.GetAttribute ("xmlns");
			if (xmlns == Namespace)
				value.RemoveAttribute ("xmlns");
			SetProjectExtension (parent);
			NotifyChanged ();
		}

		public void RemoveProjectExtension (string section)
		{
			AssertCanModify ();
			var ext = (MSBuildProjectExtensions)ChildNodes.FirstOrDefault (ob => ob is MSBuildProjectExtensions);
			if (ext != null) {
				ext.RemoveProjectExtension (section);
				if (ext.IsEmpty)
					Remove (ext);
			}
		}

		public void RemoveMonoDevelopProjectExtension (string section)
		{
			AssertCanModify ();
			var md = GetProjectExtension ("MonoDevelop");
			if (md == null)
				return;
			XmlElement elem = md.SelectSingleNode ("tns:Properties/tns:" + section, GetNamespaceManagerForProject ()) as XmlElement;
			if (elem != null) {
				var parent = (XmlElement)elem.ParentNode;
				XmlUtil.RemoveElementAndIndenting (elem);
				if (parent.ChildNodes.OfType<XmlNode> ().All (n => n is XmlWhitespace))
					RemoveProjectExtension ("MonoDevelop");
				else
					SetProjectExtension (md);
				NotifyChanged ();
			}
		}

		public void Remove (MSBuildObject ob)
		{
			AssertCanModify ();
			if (ob.ParentObject == this) {
				ob.RemoveIndent ();
				ChildNodes = ChildNodes.Remove (ob);
				ob.ParentNode = null;
			}
		}

		public void RemoveItem (MSBuildItem item, bool removeEmptyParentGroup = true)
		{
			AssertCanModify ();
			if (item.ParentGroup != null) {
				item.RemoveIndent ();
				var g = item.ParentGroup;
				g.RemoveItem (item);
				if (removeEmptyParentGroup && !g.Items.Any ()) {
					Remove (g);
					if (bestGroups != null) {
						string groupId = GetBestGroupId (item);
						bestGroups.Remove (groupId);
					}
				}
			}
		}

		public void AddKnownItemAttribute (string itemName, params string[] attributes)
		{
			AssertCanModify ();

			if (knownItemAttributes == null)
				knownItemAttributes = new Dictionary<string, string[]> ();

			var mergedAttributes = MSBuildItem.KnownAttributes.Union (attributes).ToArray ();
			knownItemAttributes [itemName] = mergedAttributes;
		}

		internal string[] GetKnownItemAttributes (string itemName)
		{
			if (knownItemAttributes == null)
				return MSBuildItem.KnownAttributes;

			string[] attributes = null;
			if (knownItemAttributes.TryGetValue (itemName, out attributes))
				return attributes;

			return MSBuildItem.KnownAttributes;
		}

		internal void SetGlobalProperty (string property, string value)
		{
			globalProperties [property] = value;
		}

		internal void RemoveGlobalProperty (string property)
		{
			globalProperties.Remove (property);
		}

		internal Dictionary<string, string> GlobalProperties {
			get { return globalProperties; }
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
				bool hasNewLine = t.Length > 0 && (t [t.Length - 1] == '\r' || t [t.Length - 1] == '\n');
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
			if (s [s.Length - 1] == '\n') {
				if (s.Length > 1 && s [s.Length - 2] == '\r')
					return s.Substring (0, s.Length - 2);
			}
			return s.Substring (0, s.Length - 1);
		}

		static string GetIndentString (XmlNode elem)
		{
			if (elem == null)
				return "";
			var node = elem.PreviousSibling;
			StringBuilder res = StringBuilderCache.Allocate ();

			while (node != null) {
				var ws = node as XmlWhitespace;
				if (ws != null) {
					var t = ws.InnerText;
					int i = t.LastIndexOfAny (new [] { '\r', '\n' });
					if (i == -1) {
						res.Append (t);
					} else {
						res.Append (t, i + 1, t.Length - i - 1);
						return StringBuilderCache.ReturnAndFree (res);
					}
				} else
					res.Clear ();
				node = node.PreviousSibling;
			}
			return StringBuilderCache.ReturnAndFree (res);
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

			if (elem.NextSibling == null && elem != elem.OwnerDocument.DocumentElement && elem.ParentNode != null) {
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
			if (string.IsNullOrEmpty (indent) || elem.ParentNode == null)
				return;
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
				int i = t.LastIndexOfAny (new [] { '\r', '\n' });
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

	class WriteContext
	{
		public bool Evaluating;
		public Dictionary<string, MSBuildItem> ItemMap;
	}
}
