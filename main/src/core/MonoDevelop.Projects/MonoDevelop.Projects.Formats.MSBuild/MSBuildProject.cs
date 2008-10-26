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

namespace MonoDevelop.Projects.Formats.MSBuild
{
	class MSBuildProject
	{
		public XmlDocument doc;
		Dictionary<XmlElement,MSBuildObject> elemCache = new Dictionary<XmlElement,MSBuildObject> ();
		
		public const string Schema = "http://schemas.microsoft.com/developer/msbuild/2003";
		static XmlNamespaceManager manager;
		
		bool useCrLf;
		
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
			doc = new XmlDocument ();
			doc.PreserveWhitespace = false;
			doc.Load (file);
		}
		
		public void LoadXml (string xml)
		{
			doc = new XmlDocument ();
			doc.PreserveWhitespace = false;
			doc.LoadXml (xml);
			useCrLf = CountNewLines ("\r\n", xml) > (CountNewLines ("\n", xml) / 2);
		}
		
		public void Save (string file)
		{
			StreamWriter sw = new StreamWriter (file);
			if (useCrLf)
				sw.NewLine = "\r\n";
			else
				sw.NewLine = "\n";
			using (sw) {
				doc.Save (sw);
			}
		}
		
		int CountNewLines (string nl, string text)
		{
			int i = -1;
			int c = -1;
			do {
				c++;
				i++;
				i = text.IndexOf (nl, i);
			}
			while (i != -1);
			return c;
		}
		
		public string DefaultTargets {
			get { return doc.DocumentElement.GetAttribute ("DefaultTargets"); }
			set { doc.DocumentElement.SetAttribute ("DefaultTargets", value); }
		}
		
		public void AddNewImport (string name, string condition)
		{
			XmlElement elem = doc.CreateElement (null, "Import", MSBuildProject.Schema);
			doc.DocumentElement.AppendChild (elem);
			elem.SetAttribute ("Project", name);
		}
		
		public MSBuildPropertyGroup GetGlobalPropertyGroup ()
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
			
			XmlElement last = doc.DocumentElement.SelectSingleNode ("tns:PropertyGroup[last()]", XmlNamespaceManager) as XmlElement;
			if (last != null)
				doc.DocumentElement.InsertAfter (elem, last);
			else
				doc.DocumentElement.AppendChild (elem);
			
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
			XmlElement elem = (XmlElement) doc.DocumentElement.SelectSingleNode ("tns:ItemGroup/" + name, XmlNamespaceManager);
			if (elem != null) {
				MSBuildItemGroup grp = GetItemGroup ((XmlElement) elem.ParentNode);
				return grp.AddNewItem (name, include);
			} else {
				MSBuildItemGroup grp = AddNewItemGroup ();
				return grp.AddNewItem (name, include);
			}
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
		
		public void RemoveItem (MSBuildItem item)
		{
			elemCache.Remove (item.Element);
			XmlElement parent = (XmlElement) item.Element.ParentNode;
			item.Element.ParentNode.RemoveChild (item.Element);
			if (parent.ChildNodes.Count == 0) {
				elemCache.Remove (parent);
				parent.ParentNode.RemoveChild (parent);
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
			MSBuildPropertyGroup it = new MSBuildPropertyGroup (elem);
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
	
	class MSBuildProperty: MSBuildObject
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
	
	class MSBuildPropertyGroup: MSBuildObject
	{
		Dictionary<string,MSBuildProperty> properties = new Dictionary<string,MSBuildProperty> ();
		
		public MSBuildPropertyGroup (XmlElement elem): base (elem)
		{
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
		
		public void SetPropertyValue (string name, string value)
		{
			MSBuildProperty prop = GetProperty (name);
			if (prop == null) {
				XmlElement pelem = AddChildElement (name);
				prop = new MSBuildProperty (pelem);
				properties [name] = prop;
			}
			prop.Value = value;
		}
		
		public string GetPropertyValue (string name)
		{
			MSBuildProperty prop = GetProperty (name);
			if (prop == null)
				return null;
			else
				return prop.Value;
		}
		
		public void RemoveProperty (string name)
		{
			MSBuildProperty prop = GetProperty (name);
			if (prop != null) {
				properties.Remove (name);
				Element.RemoveChild (prop.Element);
			}
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

		public static MSBuildPropertyGroup Merge (MSBuildPropertyGroup g1, MSBuildPropertyGroup g2)
		{
			XmlElement elem = g1.Element.OwnerDocument.CreateElement (null, "PropertyGroup", MSBuildProject.Schema);
			MSBuildPropertyGroup grp = new MSBuildPropertyGroup (elem);
			foreach (MSBuildProperty prop in g1.Properties)
				grp.SetPropertyValue (prop.Name, prop.Value);
			foreach (MSBuildProperty prop in g2.Properties)
				grp.SetPropertyValue (prop.Name, prop.Value);
			return grp;
		}

		public void UnMerge (MSBuildPropertyGroup baseGrp)
		{
			foreach (MSBuildProperty prop in baseGrp.Properties) {
				MSBuildProperty thisProp = GetProperty (prop.Name);
				if (thisProp != null && prop.Value == thisProp.Value)
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
	
	class MSBuildItem: MSBuildObject
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
			if (elem != null)
				Element.RemoveChild (elem);
		}
		
		public string GetMetadata (string name)
		{
			XmlElement elem = Element [name, MSBuildProject.Schema];
			if (elem != null)
				return elem.InnerXml;
			else
				return null;
		}
	}
	
	class MSBuildItemGroup: MSBuildObject
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
