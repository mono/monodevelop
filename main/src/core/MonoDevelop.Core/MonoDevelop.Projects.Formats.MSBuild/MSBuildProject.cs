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
		MSBuildProjectInstance mainProjectInstance;

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
			doc.PreserveWhitespace = true;
			
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
			var xw = XmlWriter.Create (sw, new XmlWriterSettings {
				OmitXmlDeclaration = !doc.ChildNodes.OfType<XmlDeclaration> ().Any ()
			});
			doc.Save (xw);

			string content = sw.ToString ();
			if (format.EndsWithEmptyLine && !content.EndsWith (format.NewLine))
				content += format.NewLine;

			return content;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this project uses the msbuild engine for evaluation.
		/// </summary>
		/// <remarks>When set to false, evaluation support is limited but it allows loading projects
		/// which are not fully compliant with MSBuild (old MD projects).</remarks>
		public bool UseMSBuildEngine { get; set; }

		public void Evaluate ()
		{
			DateTime t = DateTime.Now;
			mainProjectInstance = new MSBuildProjectInstance (this);
			mainProjectInstance.Evaluate ();
			Console.WriteLine ("ET: " + (DateTime.Now - t).TotalMilliseconds + " " + FileName.FileName);
		}

		public MSBuildProjectInstance CreateInstance ()
		{
			return new MSBuildProjectInstance (this);
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
			XmlUtil.Indent (format, elem, false);
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
				XmlUtil.RemoveElementAndIndenting (elem);
			else
				//FIXME: should this actually log an error?
				Console.WriteLine ("ppnf:");
		}
		
		public void RemoveImport (MSBuildImport import)
		{
			XmlUtil.RemoveElementAndIndenting (import.Element);
		}

		public IEnumerable<MSBuildImport> Imports {
			get {
				foreach (XmlElement elem in doc.DocumentElement.SelectNodes ("tns:Import", XmlNamespaceManager))
					yield return GetImport (elem);
			}
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
			
			XmlUtil.Indent (format, elem, true);
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
		}

		public void RemoveProjectExtension (string section)
		{
			XmlElement elem = doc.DocumentElement.SelectSingleNode ("tns:ProjectExtensions/tns:" + section, XmlNamespaceManager) as XmlElement;
			if (elem != null) {
				XmlElement parent = (XmlElement) elem.ParentNode;
				XmlUtil.RemoveElementAndIndenting (elem);
				if (parent.ChildNodes.OfType<XmlNode>().All (n => n is XmlWhitespace))
					XmlUtil.RemoveElementAndIndenting (parent);
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
			XmlUtil.RemoveElementAndIndenting (grp.Element);
		}

		public IEnumerable<MSBuildTarget> Targets {
			get {
				return mainProjectInstance.Targets;
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
			elem.ParentNode.InsertBefore (elem.OwnerDocument.CreateWhitespace (newIndent), elem);

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
