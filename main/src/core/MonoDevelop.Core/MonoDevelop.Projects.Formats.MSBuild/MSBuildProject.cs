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
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using System.Text;

using MonoDevelop.Projects.Utility;
using MonoDevelop.Projects.Text;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	public class MSBuildProject
	{
		public XmlDocument doc;
		Dictionary<XmlElement,MSBuildObject> elemCache = new Dictionary<XmlElement,MSBuildObject> ();
		Dictionary<string, MSBuildItemGroup> bestGroups;
		
		public const string Schema = "http://schemas.microsoft.com/developer/msbuild/2003";
		static XmlNamespaceManager manager;
		
		bool endsWithEmptyLine;
		string newLine = Environment.NewLine;
		ByteOrderMark bom;
		
		internal static XmlNamespaceManager XmlNamespaceManager {
			get {
				if (manager == null) {
					manager = new XmlNamespaceManager (new NameTable ());
					manager.AddNamespace ("tns", Schema);
				}
				return manager;
			}
		}
		
		public MSBuildProject ()
		{
			doc = new XmlDocument ();
			doc.PreserveWhitespace = false;
			doc.AppendChild (doc.CreateElement (null, "Project", Schema));
		}
		
		public void Load (string file)
		{
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
			// StringWriter.Encoding always returns UTF16. We need it to return UTF8, so the
			// XmlDocument will write the UTF8 header.
			ProjectWriter sw = new ProjectWriter (bom);
			sw.NewLine = newLine;
			doc.Save (sw);
			
			string content = sw.ToString ();
			if (endsWithEmptyLine && !content.EndsWith (newLine))
				content += newLine;
			
			TextFile.WriteFile (fileName, content, bom, true);
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
		
		public void AddNewImport (string name, string condition)
		{
			XmlElement elem = doc.CreateElement (null, "Import", MSBuildProject.Schema);
			elem.SetAttribute ("Project", name);
			
			XmlElement last = doc.DocumentElement.SelectSingleNode ("tns:Import[last()]", XmlNamespaceManager) as XmlElement;
			if (last != null)
				doc.DocumentElement.InsertAfter (elem, last);
			else
				doc.DocumentElement.AppendChild (elem);
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
		
		public List<string> Imports {
			get {
				List<string> ims = new List<string> ();
				foreach (XmlElement elem in doc.DocumentElement.SelectNodes ("tns:Import", XmlNamespaceManager)) {
					ims.Add (elem.GetAttribute ("Project"));
				}
				return ims;
			}
		}
		
		public MSBuildPropertySet GetGlobalPropertyGroup ()
		{
			MSBuildPropertyGroupMerged res = new MSBuildPropertyGroupMerged ();
			foreach (MSBuildPropertyGroup grp in PropertyGroups) {
				if (grp.Condition.Length == 0)
					res.Add (grp);
			}
			return res.GroupCount > 0 ? res : null;
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
		
		public string GetProjectExtensions (string section)
		{
			XmlElement elem = doc.DocumentElement.SelectSingleNode ("tns:ProjectExtensions/tns:" + section, XmlNamespaceManager) as XmlElement;
			if (elem != null)
				return elem.InnerXml;
			else
				return string.Empty;
		}
		
		public void SetProjectExtensions (string section, string value)
		{
			XmlElement elem = doc.DocumentElement ["ProjectExtensions", MSBuildProject.Schema];
			if (elem == null) {
				elem = doc.CreateElement (null, "ProjectExtensions", MSBuildProject.Schema);
				doc.DocumentElement.AppendChild (elem);
			}
			XmlElement sec = elem [section];
			if (sec == null) {
				sec = doc.CreateElement (null, section, MSBuildProject.Schema);
				elem.AppendChild (sec);
			}
			sec.InnerXml = value;
		}

		public void RemoveProjectExtensions (string section)
		{
			XmlElement elem = doc.DocumentElement.SelectSingleNode ("tns:ProjectExtensions/tns:" + section, XmlNamespaceManager) as XmlElement;
			if (elem != null) {
				XmlElement parent = (XmlElement) elem.ParentNode;
				parent.RemoveChild (elem);
				if (!parent.HasChildNodes)
					parent.ParentNode.RemoveChild (parent);
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
		
		internal MSBuildItem GetItem (XmlElement elem)
		{
			MSBuildObject ob;
			if (elemCache.TryGetValue (elem, out ob))
				return (MSBuildItem) ob;
			MSBuildItem it = new MSBuildItem (elem);
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
	
	public class MSBuildObject
	{
		XmlElement elem;
		
		public MSBuildObject (XmlElement elem)
		{
			this.elem = elem;
		}
		
		public XmlElement Element {
			get { return elem; }
		}
		
		protected XmlElement AddChildElement (string name)
		{
			XmlElement e = elem.OwnerDocument.CreateElement (null, name, MSBuildProject.Schema);
			elem.AppendChild (e);
			return e;
		}
		
		public string Condition {
			get {
				return Element.GetAttribute ("Condition");
			}
			set {
				if (string.IsNullOrEmpty (value))
					Element.RemoveAttribute ("Condition");
				else
					Element.SetAttribute ("Condition", value);
			}
		}
	}
	
	public class MSBuildProperty: MSBuildObject
	{
		public MSBuildProperty (XmlElement elem): base (elem)
		{
		}
		
		public string Name {
			get { return Element.Name; }
		}
		
		public string Value {
			get {
				return Element.InnerXml; 
			}
			set {
				Element.InnerXml = value;
			}
		}
	}
	
	public interface MSBuildPropertySet
	{
		MSBuildProperty GetProperty (string name);
		IEnumerable<MSBuildProperty> Properties { get; }
		MSBuildProperty SetPropertyValue (string name, string value, bool preserveExistingCase);
		string GetPropertyValue (string name);
		bool RemoveProperty (string name);
		void RemoveAllProperties ();
		void UnMerge (MSBuildPropertySet baseGrp, ISet<string> propertiesToExclude);
	}
	
	class MSBuildPropertyGroupMerged: MSBuildPropertySet
	{
		List<MSBuildPropertyGroup> groups = new List<MSBuildPropertyGroup> ();
		
		public void Add (MSBuildPropertyGroup g)
		{
			groups.Add (g);
		}
		
		public int GroupCount {
			get { return groups.Count; }
		}
		
		public MSBuildProperty GetProperty (string name)
		{
			// Find property in reverse order, since the last set
			// value is the good one
			for (int n=groups.Count - 1; n >= 0; n--) {
				var g = groups [n];
				MSBuildProperty p = g.GetProperty (name);
				if (p != null)
					return p;
			}
			return null;
		}

		public MSBuildProperty SetPropertyValue (string name, string value, bool preserveExistingCase)
		{
			MSBuildProperty p = GetProperty (name);
			if (p != null) {
				if (!preserveExistingCase || !string.Equals (value, p.Value, StringComparison.OrdinalIgnoreCase)) {
					p.Value = value;
				}
				return p;
			}
			return groups [0].SetPropertyValue (name, value, preserveExistingCase);
		}

		public string GetPropertyValue (string name)
		{
			MSBuildProperty prop = GetProperty (name);
			return prop != null ? prop.Value : null;
		}

		public bool RemoveProperty (string name)
		{
			bool found = false;
			foreach (var g in groups) {
				if (g.RemoveProperty (name)) {
					Prune (g);
					found = true;
				}
			}
			return found;
		}

		public void RemoveAllProperties ()
		{
			foreach (var g in groups) {
				g.RemoveAllProperties ();
				Prune (g);
			}
		}

		public void UnMerge (MSBuildPropertySet baseGrp, ISet<string> propertiesToExclude)
		{
			foreach (var g in groups) {
				g.UnMerge (baseGrp, propertiesToExclude);
			}
		}

		public IEnumerable<MSBuildProperty> Properties {
			get {
				foreach (var g in groups) {
					foreach (var p in g.Properties)
						yield return p;
				}
			}
		}
		
		void Prune (MSBuildPropertyGroup g)
		{
			if (g != groups [0] && !g.Properties.Any()) {
				// Remove this group since it's now empty
				g.Parent.RemoveGroup (g);
			}
		}
	}
	
	public class MSBuildPropertyGroup: MSBuildObject, MSBuildPropertySet
	{
		Dictionary<string,MSBuildProperty> properties = new Dictionary<string,MSBuildProperty> ();
		MSBuildProject parent;
		
		public MSBuildPropertyGroup (MSBuildProject parent, XmlElement elem): base (elem)
		{
			this.parent = parent;
		}
		
		public MSBuildProject Parent {
			get {
				return this.parent;
			}
		}
		
		public MSBuildProperty GetProperty (string name)
		{
			MSBuildProperty prop;
			if (properties.TryGetValue (name, out prop))
				return prop;
			XmlElement propElem = Element [name, MSBuildProject.Schema];
			if (propElem != null) {
				prop = new MSBuildProperty (propElem);
				properties [name] = prop;
				return prop;
			}
			else
				return null;
		}
		
		public IEnumerable<MSBuildProperty> Properties {
			get {
				foreach (XmlNode node in Element.ChildNodes) {
					XmlElement pelem = node as XmlElement;
					if (pelem == null)
						continue;
					MSBuildProperty prop;
					if (properties.TryGetValue (pelem.Name, out prop))
						yield return prop;
					else {
						prop = new MSBuildProperty (pelem);
						properties [pelem.Name] = prop;
						yield return prop;
					}
				}
			}
		}
		
		public MSBuildProperty SetPropertyValue (string name, string value, bool preserveExistingCase)
		{
			MSBuildProperty prop = GetProperty (name);
			if (prop == null) {
				XmlElement pelem = AddChildElement (name);
				prop = new MSBuildProperty (pelem);
				properties [name] = prop;
				prop.Value = value;
			} else if (!preserveExistingCase || !string.Equals (value, prop.Value, StringComparison.OrdinalIgnoreCase)) {
				prop.Value = value;
			}
			return prop;
		}
		
		public string GetPropertyValue (string name)
		{
			MSBuildProperty prop = GetProperty (name);
			if (prop == null)
				return null;
			else
				return prop.Value;
		}
		
		public bool RemoveProperty (string name)
		{
			MSBuildProperty prop = GetProperty (name);
			if (prop != null) {
				properties.Remove (name);
				Element.RemoveChild (prop.Element);
				return true;
			}
			return false;
		}

		public void RemoveAllProperties ()
		{
			List<XmlNode> toDelete = new List<XmlNode> ();
			foreach (XmlNode node in Element.ChildNodes) {
				if (node is XmlElement)
					toDelete.Add (node);
			}
			foreach (XmlNode node in toDelete)
				Element.RemoveChild (node);
			properties.Clear ();
		}

		public void UnMerge (MSBuildPropertySet baseGrp, ISet<string> propsToExclude)
		{
			foreach (MSBuildProperty prop in baseGrp.Properties) {
				if (propsToExclude != null && propsToExclude.Contains (prop.Name))
					continue;
				MSBuildProperty thisProp = GetProperty (prop.Name);
				if (thisProp != null && prop.Value.Equals (thisProp.Value, StringComparison.CurrentCultureIgnoreCase))
					RemoveProperty (prop.Name);
			}
		}

		public override string ToString()
		{
			string s = "[MSBuildPropertyGroup:";
			foreach (MSBuildProperty prop in Properties)
				s += " " + prop.Name + "=" + prop.Value;
			return s + "]";
		}

	}
	
	public class MSBuildItem: MSBuildObject
	{
		public MSBuildItem (XmlElement elem): base (elem)
		{
		}
		
		public string Include {
			get { return Element.GetAttribute ("Include"); }
			set { Element.SetAttribute ("Include", value); }
		}
		
		public string Name {
			get { return Element.Name; }
		}
		
		public bool HasMetadata (string name)
		{
			return Element [name, MSBuildProject.Schema] != null;
		}
		
		public void SetMetadata (string name, string value)
		{
			SetMetadata (name, value, true);
		}
		
		public void SetMetadata (string name, string value, bool isLiteral)
		{
			XmlElement elem = Element [name, MSBuildProject.Schema];
			if (elem == null) {
				elem = AddChildElement (name);
				Element.AppendChild (elem);
			}
			elem.InnerXml = value;
		}
		
		public void UnsetMetadata (string name)
		{
			XmlElement elem = Element [name, MSBuildProject.Schema];
			if (elem != null) {
				Element.RemoveChild (elem);
				if (!Element.HasChildNodes)
					Element.IsEmpty = true;
			}
		}
		
		public string GetMetadata (string name)
		{
			XmlElement elem = Element [name, MSBuildProject.Schema];
			if (elem != null)
				return elem.InnerXml;
			else
				return null;
		}
		
		public bool GetMetadataIsFalse (string name)
		{
			return String.Compare (GetMetadata (name), "False", StringComparison.OrdinalIgnoreCase) == 0;
		}
		
		public void MergeFrom (MSBuildItem other)
		{
			foreach (XmlNode node in Element.ChildNodes) {
				if (node is XmlElement)
					SetMetadata (node.LocalName, node.InnerXml);
			}
		}
	}
	
	public class MSBuildItemGroup: MSBuildObject
	{
		MSBuildProject parent;
		
		internal MSBuildItemGroup (MSBuildProject parent, XmlElement elem): base (elem)
		{
			this.parent = parent;
		}
		
		public MSBuildItem AddNewItem (string name, string include)
		{
			XmlElement elem = AddChildElement (name);
			MSBuildItem it = parent.GetItem (elem);
			it.Include = include;
			return it;
		}
		
		public IEnumerable<MSBuildItem> Items {
			get {
				foreach (XmlNode node in Element.ChildNodes) {
					XmlElement elem = node as XmlElement;
					if (elem != null)
						yield return parent.GetItem (elem);
				}
			}
		}
	}
}
