// LibraryCache.cs : Assembly file caching class
//
// Author: Mike Kestner  <mkestner@novell.com>
//
// Copyright (c) 2008 Novell, Inc
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


using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using ENV = System.Environment;
using Mono.Cecil;

namespace Stetic {

	public class LibraryCache {

		public class LibraryInfo {

			public LibraryInfo () {}

			string file;
			Guid guid;
			DateTime timestamp;
			XmlDocument gui;
			XmlDocument objects;

			string CacheDirectory {
				get { return Path.Combine (dir, Guid.ToString ()); }
			}

			[XmlAttribute]
			public string File {
				get { return file; }
				set { file = value; }
			}

			[XmlAttribute]
			public Guid Guid {
				get { return guid; }
				set { 
					string path = Path.Combine (dir, guid.ToString ());
					if (Directory.Exists (path))
						Directory.Delete (path, true);
					guid = value; 
				}
			}
			
			public bool IsCurrent {
				get {
					return Timestamp == System.IO.File.GetLastWriteTime (file).ToUniversalTime ();
				}
			}

			[XmlIgnore]
			public XmlDocument GuiDocument {
				get {
					if (gui == null) {
						if (System.IO.File.Exists (GuiPath)) {
							try {
								gui = new XmlDocument ();
								using (Stream stream = System.IO.File.Open (GuiPath, FileMode.Open))
									gui.Load (stream);
							} catch (Exception) {
								gui = null;
							}
						}
					}
					return gui;
				}
				set {
					gui = value;
					WriteGuiFile ();
				}
			}

			string GuiPath {
				get { return Path.Combine (CacheDirectory, "steticGui"); }
			}

			public bool HasWidgets {
				get { return System.IO.File.Exists (ObjectsPath); }
			}

			[XmlIgnore]
			public XmlDocument ObjectsDocument {
				get {
					if (objects == null) {
						if (System.IO.File.Exists (ObjectsPath)) {
							try {
								objects = new XmlDocument ();
								using (Stream stream = System.IO.File.Open (ObjectsPath, FileMode.Open))
									objects.Load (stream);
							} catch (Exception) {
								objects = null;
							}
						}
					}
					return objects;
				}
				set {
					objects = value;
					WriteObjectsFile ();
					if (objects == null && gui != null)
						GuiDocument = null;
				}
			}
			
			internal ToolboxItemInfo GetToolboxItem (string name, string asmName)
			{
				XmlDocument doc = ObjectsDocument;
				if (doc != null) {
					XmlElement elem = (XmlElement) doc.SelectSingleNode ("/objects/object[@type='" + name + "']");
					if (elem == null)
						elem = (XmlElement) doc.SelectSingleNode ("/objects/object[@type='" + name + "," + asmName + "']");
					if (elem != null) {
						ToolboxItemInfo info = new ToolboxItemInfo (elem.GetAttribute ("base-type"));
						info.PaletteCategory = elem.GetAttribute ("palette-category");
						return info;
					}
					
				}
				return null;
			}

			string ObjectsPath {
				get { return Path.Combine (CacheDirectory, "objects"); }
			}

			[XmlAttribute]
			public DateTime Timestamp {
				get { return timestamp; }
				set { timestamp = value; }
			}

			void WriteGuiFile ()
			{
				if (gui == null) {
					if (System.IO.File.Exists (GuiPath))
						System.IO.File.Delete (GuiPath);
					return;
				}
				if (!Directory.Exists (CacheDirectory))
					Directory.CreateDirectory (CacheDirectory);

				using (Stream stream = System.IO.File.Create (GuiPath))
					gui.Save (stream);
			}

			public void WriteObjectsFile ()
			{
				if (objects == null) {
					if (System.IO.File.Exists (ObjectsPath))
						System.IO.File.Delete (ObjectsPath);
					return;
				}

				if (!Directory.Exists (CacheDirectory))
					Directory.CreateDirectory (CacheDirectory);

				using (Stream stream = System.IO.File.Create (ObjectsPath))
					objects.Save (stream);
			}

			public event EventHandler Changed;

			public void OnChanged ()
			{
				if (Changed != null)
					Changed (this, EventArgs.Empty);
			}
		}

		static string dir = Path.Combine (Path.Combine (ENV.GetFolderPath (ENV.SpecialFolder.ApplicationData), "stetic"), "library-cache");

		public class LibraryInfoCollection : IEnumerable {

			Dictionary<string, LibraryInfo> libs = new Dictionary<string, LibraryInfo> ();

			public LibraryInfo this [string path] {
				get {
					path = Path.GetFullPath (path);
					if (libs.ContainsKey (path))
						return libs [path];
					return null;
				}
			}

			public void Add (object obj)
			{
				Add (obj as LibraryInfo);
			}
				
			public void Add (LibraryInfo info)
			{
				libs [info.File] = info;
			}

			public IEnumerator GetEnumerator ()
			{
				return libs.Values.GetEnumerator ();
			}

			public void Remove (string file)
			{
				file = Path.GetFullPath (file);
				libs.Remove (file);
			}
		}

		public static LibraryCache Cache = Load ();

		[XmlArray]
		[XmlArrayItem (ElementName="LibraryInfo", Type=typeof(LibraryInfo))]
		public LibraryInfoCollection Members = new LibraryInfoCollection ();

		public LibraryCache () {}

		public LibraryInfo this [string file] {
			get {
				file = Path.GetFullPath (file);
				if (IsCurrent (file))
					return Members [file];

				Refresh (null, file);
				return Members [file];
			}
		}

		public bool IsCurrent (string file)
		{
			file = Path.GetFullPath (file);
			LibraryInfo info = Members [file];
			return info != null && info.Timestamp == File.GetLastWriteTime (file).ToUniversalTime ();
		}

		EmbeddedResource GetResource (AssemblyDefinition asm, string name)
		{
			foreach (Resource res in asm.MainModule.Resources) {
				EmbeddedResource eres = res as EmbeddedResource;
				if (eres != null && eres.Name == name)
					return eres;
			}
			return null;
		}
 
		void AddDependencies (XmlElement elem, AssemblyResolver resolver, string filename, AssemblyDefinition asm)
		{
			string dir = Path.GetDirectoryName (filename);
			foreach (AssemblyNameReference aref in asm.MainModule.AssemblyReferences) {
				LibraryInfo info = GetInfo (resolver, aref.FullName, dir);
				if (info != null && info.HasWidgets) {
					XmlElement edep = elem.OwnerDocument.CreateElement ("dependency");
					edep.InnerText = info.File;
					elem.AppendChild (edep);
				}
			}
		}

		XmlDocument GetGuiDoc (AssemblyDefinition adef)
		{
			XmlDocument doc = null;
			try {
				EmbeddedResource res = GetResource (adef, "gui.stetic");
				if (res != null) {
					MemoryStream stream = new MemoryStream (res.GetResourceData ());
					doc = new XmlDocument ();
					using (stream)
						doc.Load (stream);
				}
			} catch {
				doc = null;
			}

			return doc;
		}
		
		bool ReferenceChainContainsGtk (AssemblyResolver resolver, AssemblyNameReference aref, Hashtable visited)
		{
			if (aref.Name == "gtk-sharp")
				return true;
			else if (visited.Contains (aref.Name))
				return false;

			visited [aref.Name] = aref;

			AssemblyDefinition adef = resolver.Resolve (aref);
			if (adef == null)
				return false;

			foreach (AssemblyNameReference child in adef.MainModule.AssemblyReferences)
				if (ReferenceChainContainsGtk (resolver, child, visited))
					return true;

			return false;
		}

		internal class ToolboxItemInfo {

			public ToolboxItemInfo (string base_type)
			{
				BaseType = base_type;
			}

			public string BaseType;
			public string PaletteCategory;
		}

		ToolboxItemInfo GetToolboxItemInfo (AssemblyResolver resolver, string baseDirectory, AssemblyDefinition asm, TypeDefinition tdef, bool checkBaseType)
		{
			if (tdef == null)
				return null;

			ToolboxItemInfo info = null;
			string category = "General";
			
			foreach (CustomAttribute attr in tdef.CustomAttributes) {
				switch (attr.AttributeType.FullName) {
				case "System.ComponentModel.ToolboxItemAttribute":
					if (attr.ConstructorArguments.Count > 0) {
						object param = attr.ConstructorArguments [0].Value;
						if (param == null)
							return null;
						else if (param.GetType () == typeof (bool)) {
							if ((bool) param)
								info = new ToolboxItemInfo ("Gtk.Widget");
							else 
								return null;
						} else if (param.GetType () == typeof (TypeReference))
							info = new ToolboxItemInfo ("Gtk.Widget");
						else
							return null;
					}
					break;
				case "System.ComponentModel.CategoryAttribute":
					if (attr.ConstructorArguments.Count > 0) {
						object param = attr.ConstructorArguments [0].Value;
						if (param.GetType () == typeof (string))
							category = (string) param;
					}
					break;
				default:
					continue;
				}
			}

			if (info == null && checkBaseType && tdef.BaseType != null) {
				string baseName = tdef.BaseType.FullName;

				foreach (AssemblyNameReference aref in asm.MainModule.AssemblyReferences) {
					LibraryInfo libInfo = GetInfo (resolver, aref.FullName, baseDirectory);
					if (libInfo != null && libInfo.HasWidgets) {
						ToolboxItemInfo binfo = libInfo.GetToolboxItem (baseName, aref.Name);
						if (binfo != null) {
							info = new ToolboxItemInfo (baseName);
							category = binfo.PaletteCategory;
							break;
						}
					}
				}
			}

			if (info != null)
				info.PaletteCategory = category;

			return info;
		}

		XmlElement GetItemGroup (XmlElement groups, string cat, string default_label)
		{
			XmlElement group;
			
			if (String.IsNullOrEmpty (cat))
				group = (XmlElement) groups.SelectSingleNode ("itemgroup[(not(@name) or @name='') and not(@ref)]");
			else
				group = (XmlElement) groups.SelectSingleNode ("itemgroup[@name='" + cat + "']");
			
			if (group == null) {
				group = groups.OwnerDocument.CreateElement ("itemgroup");
				if (String.IsNullOrEmpty (cat))
					group.SetAttribute ("label", default_label);
				else {
					group.SetAttribute ("name", cat);
					group.SetAttribute ("label", cat);
				}
				groups.AppendChild (group);
			}
			return group;
		}
		
		void AddProperty (PropertyDefinition prop, string cat, bool translatable, XmlElement obj)
		{
			XmlElement groups = obj ["itemgroups"];
			if (groups == null) {
				groups = obj.OwnerDocument.CreateElement ("itemgroups");
				obj.AppendChild (groups);
			}
			
			XmlElement group = GetItemGroup (groups, cat, prop.DeclaringType.Name + " Properties");
			XmlElement elem = group.OwnerDocument.CreateElement ("property");
			if (translatable)
				elem.SetAttribute ("translatable", "yes");
			elem.SetAttribute ("name", prop.Name);
			group.AppendChild (elem);
		}

		static string[] supported_types = new string[] {
			"System.Boolean",
			"System.Char",
			"System.SByte",
			"System.Byte",
			"System.Int16",
			"System.UInt16",
			"System.Int32",
			"System.UInt32",
			"System.Int64",
			"System.UInt64",
			"System.Decimal",
			"System.Single",
			"System.Double",
			"System.DateTime",
			"System.String",
			"System.String[]",
			"System.TimeSpan",
			"Gtk.Adjustment",
		};

		void AddProperties (TypeDefinition tdef, XmlElement obj)
		{
			foreach (PropertyDefinition prop in tdef.Properties) {
				if (prop.GetMethod == null || !prop.GetMethod.IsPublic || prop.SetMethod == null || !prop.SetMethod.IsPublic)
					continue;
				
				else if (Array.IndexOf (supported_types, prop.PropertyType.FullName) < 0)
					continue;
				
				bool browsable = true;
				bool translatable = false;
				string category = String.Empty;
				foreach (CustomAttribute attr in prop.CustomAttributes) {
					switch (attr.Constructor.DeclaringType.FullName) {
					case "System.ComponentModel.BrowsableAttribute":
						if (attr.ConstructorArguments.Count > 0) {
							object param = attr.ConstructorArguments [0].Value;
							if (param.GetType () == typeof (bool))
								browsable = (bool) param;
						}
						break;
					case "System.ComponentModel.CategoryAttribute":
						if (attr.ConstructorArguments.Count > 0) {
							object param = attr.ConstructorArguments [0].Value;
							if (param.GetType () == typeof (string))
								category = (string) param;
						}
						break;
					case "System.ComponentModel.LocalizableAttribute":
						if (attr.ConstructorArguments.Count > 0) {
							object param = attr.ConstructorArguments [0].Value;
							if (param.GetType () == typeof (bool))
								translatable = (bool) param;
						}
						break;
					default:
						continue;
					}
					if (!browsable)
						break;
				}
				if (browsable)
					AddProperty (prop, category, translatable, obj);
			}
		}
	
		void AddEvent (EventDefinition ev, string cat, XmlElement obj)
		{
			XmlElement groups = obj ["signals"];
			if (groups == null) {
				groups = obj.OwnerDocument.CreateElement ("signals");
				obj.AppendChild (groups);
			}
			
			XmlElement group = GetItemGroup (groups, cat, ev.DeclaringType.Name + " Signals");
			XmlElement elem = group.OwnerDocument.CreateElement ("signal");
			elem.SetAttribute ("name", ev.Name);
			group.AppendChild (elem);
		}

		void AddEvents (TypeDefinition tdef, XmlElement obj)
		{
			foreach (EventDefinition ev in tdef.Events) {
				if (ev.AddMethod == null || !ev.AddMethod.IsPublic)
					continue;
				bool browsable = true;
				string category = String.Empty;
				foreach (CustomAttribute attr in ev.CustomAttributes) {
					switch (attr.Constructor.DeclaringType.FullName) {
					case "System.ComponentModel.BrowsableAttribute":
						if (attr.ConstructorArguments.Count > 0) {
							object param = attr.ConstructorArguments [0].Value;
							if (param.GetType () == typeof (bool))
								browsable = (bool) param;
						}
						break;
					case "System.ComponentModel.CategoryAttribute":
						if (attr.ConstructorArguments.Count > 0) {
							object param = attr.ConstructorArguments [0].Value;
							if (param.GetType () == typeof (string))
								category = (string) param;
						}
						break;
					default:
						continue;
					}
					if (!browsable)
						break;
				}
				if (browsable)
					AddEvent (ev, category, obj);
			}
		}

		void AddObject (TypeDefinition tdef, Dictionary<TypeDefinition,ToolboxItemInfo> localObjects, AssemblyResolver resolver, string basePath, AssemblyDefinition adef)
		{
			if (tdef.IsAbstract || !tdef.IsClass) 
				return;

			ToolboxItemInfo tbinfo = GetToolboxItemInfo (resolver, basePath, adef, tdef, true);
			if (tbinfo == null)
				return;
			
			localObjects [tdef] = tbinfo;

			foreach (var nestedType in tdef.NestedTypes)
				AddObject (nestedType, localObjects, resolver, basePath, adef);
		}
	
		void AddObjects (XmlDocument doc, AssemblyResolver resolver, string basePath, AssemblyDefinition adef)
		{
			Dictionary<TypeDefinition,ToolboxItemInfo> localObjects = new Dictionary<TypeDefinition, ToolboxItemInfo> ();
			
			foreach (TypeDefinition tdef in adef.MainModule.Types) {
				AddObject (tdef, localObjects, resolver, basePath, adef);
			}
			
			foreach (KeyValuePair<TypeDefinition,ToolboxItemInfo> item in localObjects) {
				TypeDefinition tdef = item.Key;
				ToolboxItemInfo tbinfo = item.Value;
				XmlElement elem = doc.CreateElement ("object");
				elem.SetAttribute ("type", tdef.FullName);
				elem.SetAttribute ("allow-children", "false");
				elem.SetAttribute ("palette-category", tbinfo.PaletteCategory);
				if (tdef.IsNotPublic)
					elem.SetAttribute ("internal", "true");
				doc.DocumentElement.AppendChild (elem);
				
				TypeDefinition curDef = tdef;
				while (curDef != null && curDef.FullName != tbinfo.BaseType) {
					if (curDef != tdef && localObjects.ContainsKey (curDef)) {
						tbinfo.BaseType = curDef.FullName;
						break;
					}
					else if (curDef.Module.Assembly.Name.Name == "gtk-sharp") {
						tbinfo.BaseType = curDef.FullName;
						break;
					}
					else if (curDef != tdef && GetToolboxItemInfo (resolver, basePath, curDef.Module.Assembly, curDef, false) != null) {
						tbinfo.BaseType = curDef.FullName;
						break;
					}
					if (curDef.Module.Assembly != adef) {
						
						LibraryInfo li = Refresh (resolver, curDef.Module.FullyQualifiedName, basePath);
						if (li.HasWidgets && li.GetToolboxItem (curDef.FullName, curDef.Module.Assembly.Name.Name) != null) {
							tbinfo.BaseType = curDef.FullName;
							break;
						}
					}
					AddProperties (curDef, elem);
					AddEvents (curDef, elem);
					if (curDef.BaseType != null && curDef.BaseType.FullName != tbinfo.BaseType)
						curDef = FindTypeDefinition (resolver, adef, basePath, curDef.BaseType.FullName);
					else
						curDef = null;
				}
				
				elem.SetAttribute ("base-type", tbinfo.BaseType);
			}
		}

		XmlDocument GetObjectsDoc (AssemblyResolver resolver, AssemblyDefinition adef, string path, string baseDirectory)
		{
			XmlDocument doc = null;
			bool isMainLib = Path.GetFileName (path) == "libstetic.dll";
			
			try {
				EmbeddedResource res = GetResource (adef, "objects.xml");
				if (res != null) {
					MemoryStream stream = new MemoryStream (res.GetResourceData ());
					doc = new XmlDocument ();
					using (stream)
						doc.Load (stream);
				}

				if (resolver == null)
					resolver = new AssemblyResolver (null);

				baseDirectory = baseDirectory ?? Path.GetDirectoryName (path);
				
				if (!isMainLib) {
					// Make sure all referenced assemblies are up to date.
					foreach (AssemblyNameReference aref in adef.MainModule.AssemblyReferences) {
						Refresh (resolver, aref.FullName, baseDirectory);
					}
				}
			
				if (doc == null) {
//					Hashtable visited = new Hashtable ();
					foreach (AssemblyNameReference aref in adef.MainModule.AssemblyReferences) {
						if (aref.Name != "gtk-sharp") {
							LibraryInfo info = GetInfo (resolver, aref.FullName, baseDirectory);
							if (info == null || !info.HasWidgets)
								continue;
						}

						if (doc == null) {
							doc = new XmlDocument ();
							doc.AppendChild (doc.CreateElement ("objects"));
						}
						AddObjects (doc, resolver, baseDirectory, adef);
						break;
					}
				}

				if (doc != null && !isMainLib) {
					XmlElement elem = doc.CreateElement ("dependencies");
					doc.DocumentElement.AppendChild (elem);
					AddDependencies (elem, resolver, path, adef);
				}
			} catch (Exception e) {
				Console.WriteLine ("Got exception loading objects: " + e);
				doc = null;
			}

			return doc;
		}
		
		LibraryInfo GetInfo (AssemblyResolver resolver, string assembly, string baseDirectory)
		{
			string file = assembly;
			if (File.Exists (assembly))
				file = assembly;
			else {
				if (resolver == null)
					resolver = new AssemblyResolver (null);
				try {
					string path = resolver.Resolve (assembly, baseDirectory);
					if (path != null)
						file = path;
					else
						return null;
				} catch (Exception) {
					return null;
				}
			}

			file = Path.GetFullPath (file);
			
			LibraryInfo info = Members [file];
			if (info == null) {
				info = new LibraryInfo ();
				info.File = file ?? assembly;
				Members.Add (info);
			}
			return info;
		}
		
		internal LibraryInfo Refresh (AssemblyResolver resolver, string assembly)
		{
			return Refresh (resolver, assembly, null);
		}
		
		LibraryInfo Refresh (AssemblyResolver resolver, string assembly, string baseDirectory)
		{
			LibraryInfo info = GetInfo (resolver, assembly, baseDirectory);

			if (info == null || info.IsCurrent || !File.Exists (info.File))
				return info;

			info.Timestamp = File.GetLastWriteTime (info.File).ToUniversalTime ();
			info.Guid = Guid.NewGuid ();
			Save ();
			using (AssemblyDefinition adef = AssemblyDefinition.ReadAssembly (info.File)) {
				XmlDocument objects = GetObjectsDoc (resolver, adef, info.File, baseDirectory);
				if (objects != null) {
					info.ObjectsDocument = objects;
					XmlDocument gui = GetGuiDoc (adef);
					if (gui != null)
						info.GuiDocument = gui;
				}
			}
			info.OnChanged ();
			return info;
		}

		void Save ()
		{
			if (!Directory.Exists (dir))
				Directory.CreateDirectory (dir);

			// remove any dead assemblies from the cache
			StringCollection zombies = new StringCollection ();
			foreach (LibraryInfo info in Members) {
				if (File.Exists (info.File))
					continue;
				zombies.Add (info.File);
				string zombie_dir = Path.Combine (dir, info.Guid.ToString ());
				if (Directory.Exists (zombie_dir))
					Directory.Delete (Path.Combine (dir, info.Guid.ToString ()), true);
			}
			
			foreach (string file in zombies)
				Members.Remove (file);
			
			XmlSerializer serializer = new XmlSerializer (typeof (LibraryCache));
			using (FileStream fs = File.Create (Path.Combine (dir, "index.xml")))
				serializer.Serialize (fs, this);
		}
		
		static LibraryCache Load ()
		{
			string index_path = Path.Combine (dir, "index.xml");
			if (File.Exists (index_path)) {
				try {
					LibraryCache result;
					XmlSerializer serializer = new XmlSerializer (typeof (LibraryCache));
					using (XmlTextReader rdr = new XmlTextReader (index_path))
						result = (LibraryCache) serializer.Deserialize (rdr);
					return result;
				} catch (Exception e) {
					Console.WriteLine ("Cache index serialization failed " + e);
				}
			}

			return new LibraryCache ();
		}

		internal TypeDefinition FindTypeDefinition (AssemblyResolver resolver, AssemblyDefinition assembly, string basePath, string fullName)
		{
			TypeDefinition t = FindTypeDefinition (new Hashtable (), resolver, basePath, assembly, fullName);
			return t;
		}
		
		TypeDefinition FindTypeDefinition (Hashtable visited, AssemblyResolver resolver, string basePath, AssemblyDefinition asm, string fullName)
		{
			if (visited.Contains (asm))
				return null;
				
			visited [asm] = asm;
			
			TypeDefinition cls = asm.MainModule.GetType (fullName);
			if (cls != null)
				return cls;
			
			foreach (AssemblyNameReference aref in asm.MainModule.AssemblyReferences) {
				AssemblyDefinition basm = resolver.Resolve (aref, basePath);
				if (basm != null) {
					cls = basm.MainModule.GetType (fullName);
					if (cls != null)
						return cls;
				}
			}
			return null;
		}
	}
}
