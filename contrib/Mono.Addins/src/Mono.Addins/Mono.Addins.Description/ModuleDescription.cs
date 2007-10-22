//
// ModuleDescription.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Specialized;
using Mono.Addins.Serialization;

namespace Mono.Addins.Description
{
	public class ModuleDescription: ObjectDescription
	{
		StringCollection assemblies;
		StringCollection dataFiles;
		DependencyCollection dependencies;
		ExtensionCollection extensions;
		
		internal ModuleDescription (XmlElement element)
		{
			Element = element;
		}

		public ModuleDescription ()
		{
		}

		public bool DependsOnAddin (string addinId)
		{
			AddinDescription desc = Parent as AddinDescription;
			if (desc == null)
				throw new InvalidOperationException ();
			
			foreach (Dependency dep in Dependencies) {
				AddinDependency adep = dep as AddinDependency;
				if (adep == null) continue;
				if (Addin.GetFullId (desc.Namespace, adep.AddinId, adep.Version) == addinId)
					return true;
			}
			return false;
		}
		
		public StringCollection AllFiles {
			get {
				StringCollection col = new StringCollection ();
				foreach (string s in Assemblies)
					col.Add (s);

				foreach (string d in DataFiles)
					col.Add (d);
					
				return col;
			}
		}
		
		public StringCollection Assemblies {
			get {
				if (assemblies == null) {
					if (Element != null)
						InitCollections ();
					else
						assemblies = new StringCollection ();
				}
				return assemblies;
			}
		}
		
		public StringCollection DataFiles {
			get {
				if (dataFiles == null) {
					if (Element != null)
						InitCollections ();
					else
						dataFiles = new StringCollection ();
				}
				return dataFiles;
			}
		}
		
		public DependencyCollection Dependencies {
			get {
				if (dependencies == null) {
					dependencies = new DependencyCollection (this);
					if (Element != null) {
						XmlNodeList elems = Element.SelectNodes ("Dependencies/*");

						foreach (XmlNode node in elems) {
							XmlElement elem = node as XmlElement;
							if (elem == null) continue;
							
							if (elem.Name == "Addin") {
								AddinDependency dep = new AddinDependency (elem);
								dependencies.Add (dep);
							} else if (elem.Name == "Assembly") {
								AssemblyDependency dep = new AssemblyDependency (elem);
								dependencies.Add (dep);
							}
						}
					}
				}
				return dependencies;
			}
		}
		
		public ExtensionCollection Extensions {
			get {
				if (extensions == null) {
					extensions = new ExtensionCollection (this);
					if (Element != null) {
						foreach (XmlElement elem in Element.SelectNodes ("Extension"))
							extensions.Add (new Extension (elem));
					}
				}
				return extensions;
			}
		}
		
		public ExtensionNodeDescription AddExtensionNode (string path, string nodeName)
		{
			ExtensionNodeDescription node = new ExtensionNodeDescription (nodeName);
			GetExtension (path).ExtensionNodes.Add (node);
			return node;
		}
		
		public Extension GetExtension (string path)
		{
			foreach (Extension e in Extensions) {
				if (e.Path == path)
					return e;
			}
			Extension ex = new Extension (path);
			Extensions.Add (ex);
			return ex;
		}
		
		internal override void SaveXml (XmlElement parent)
		{
			CreateElement (parent, "Module");
			
			if (assemblies != null || dataFiles != null) {
				XmlElement runtime = GetRuntimeElement ();
				
				while (runtime.FirstChild != null)
					runtime.RemoveChild (runtime.FirstChild);
					
				foreach (string s in assemblies) {
					XmlElement asm = Element.OwnerDocument.CreateElement ("Import");
					asm.SetAttribute ("assembly", s);
					runtime.AppendChild (asm);
				}
				foreach (string s in dataFiles) {
					XmlElement asm = Element.OwnerDocument.CreateElement ("Import");
					asm.SetAttribute ("file", s);
					runtime.AppendChild (asm);
				}
				runtime.AppendChild (Element.OwnerDocument.CreateTextNode ("\n"));
			}
			
			// Save dependency information
			
			if (dependencies != null) {
				XmlElement deps = GetDependenciesElement ();
				dependencies.SaveXml (deps);
				deps.AppendChild (Element.OwnerDocument.CreateTextNode ("\n"));
				
				if (extensions != null)
					extensions.SaveXml (Element);
			}
		}
		
		public void AddAssemblyReference (string id, string version)
		{
			XmlElement deps = GetDependenciesElement ();
			if (deps.SelectSingleNode ("Addin[@id='" + id + "']") != null)
				return;
			
			XmlElement dep = Element.OwnerDocument.CreateElement ("Addin");
			dep.SetAttribute ("id", id);
			dep.SetAttribute ("version", version);
			deps.AppendChild (dep);
		}
		
		XmlElement GetDependenciesElement ()
		{
			XmlElement de = Element ["Dependencies"];
			if (de != null)
				return de;

			de = Element.OwnerDocument.CreateElement ("Dependencies");
			Element.AppendChild (de);
			return de;
		}
		
		XmlElement GetRuntimeElement ()
		{
			XmlElement de = Element ["Runtime"];
			if (de != null)
				return de;

			de = Element.OwnerDocument.CreateElement ("Runtime");
			Element.AppendChild (de);
			return de;
		}
		
		void InitCollections ()
		{
			dataFiles = new StringCollection ();
			assemblies = new StringCollection ();
			
			XmlNodeList elems = Element.SelectNodes ("Runtime/Import");
			foreach (XmlElement elem in elems) {
				string asm = elem.GetAttribute ("assembly");
				if (asm != "") {
					assemblies.Add (asm);
				} else {
					string file = elem.GetAttribute ("file");
					if (file != "") {
						dataFiles.Add (file);
					}
				}
			}
		}
		
		internal override void Verify (string location, StringCollection errors)
		{
			Dependencies.Verify (location + "Module/", errors);
			Extensions.Verify (location + "Module/", errors);
		}

		internal override void Write (BinaryXmlWriter writer)
		{
			writer.WriteValue ("Assemblies", Assemblies);
			writer.WriteValue ("DataFiles", DataFiles);
			writer.WriteValue ("Dependencies", Dependencies);
			writer.WriteValue ("Extensions", Extensions);
		}
		
		internal override void Read (BinaryXmlReader reader)
		{
			assemblies = (StringCollection) reader.ReadValue ("Assemblies", new StringCollection ());
			dataFiles = (StringCollection) reader.ReadValue ("DataFiles", new StringCollection ());
			dependencies = (DependencyCollection) reader.ReadValue ("Dependencies", new DependencyCollection (this));
			extensions = (ExtensionCollection) reader.ReadValue ("Extensions", new ExtensionCollection (this));
		}
	}
}
