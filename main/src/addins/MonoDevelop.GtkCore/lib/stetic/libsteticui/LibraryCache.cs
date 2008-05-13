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
			bool has_widgets;
			DateTime timestamp;

			string CacheDirectory {
				get { 
					string path = Path.Combine (dir, Guid.ToString ()); 
					if (!Directory.Exists (path))
						Directory.CreateDirectory (path);
					return path;
				}
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
						Directory.Delete (path);
					guid = value; 
				}
			}

			public string GuiPath {
				get { return Path.Combine (CacheDirectory, "steticGui"); }
			}

			[XmlAttribute]
			public bool HasWidgets {
				get { return has_widgets; }
				set { has_widgets = value; }
			}

			public string ObjectsPath {
				get { return Path.Combine (CacheDirectory, "objects"); }
			}

			[XmlAttribute]
			public DateTime Timestamp {
				get { return timestamp; }
				set { timestamp = value; }
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

		[XmlArray]
		[XmlArrayItem (ElementName="LibraryInfo", Type=typeof(LibraryInfo))]
		public LibraryInfoCollection Members = new LibraryInfoCollection ();

		public LibraryCache () {}

		public LibraryInfo this [string file] {
			get {
				file = Path.GetFullPath (file);
				if (IsCurrent (file))
					return Members [file];

				RefreshFile (file);
				return Members [file];
			}
		}

		public bool IsCurrent (string file)
		{
			file = Path.GetFullPath (file);
			LibraryInfo info = Members [file];
			return info != null && info.Timestamp == File.GetLastWriteTime (file).ToUniversalTime ();
		}

		AssemblyResolver resolver = new AssemblyResolver ();

		EmbeddedResource GetResource (AssemblyDefinition asm, string name)
		{
			foreach (Resource res in asm.MainModule.Resources) {
				EmbeddedResource eres = res as EmbeddedResource;
				if (eres != null && eres.Name == name)
					return eres;
			}
			return null;
		}
 
		bool HasGtkReference (AssemblyDefinition assm, StringCollection visited)
		{
			visited.Add (assm.Name.Name);

			foreach (AssemblyNameReference nameRef in assm.MainModule.AssemblyReferences) {
				if (visited.Contains (nameRef.Name))
					continue;
				else if (nameRef.Name == "gtk-sharp" || HasGtkReference (resolver.Resolve (nameRef), visited))
					return true;
			}

			return false;
		}

		bool CheckForWidgets (string path)
		{
			try {
				AssemblyDefinition adef = AssemblyFactory.GetAssembly (path);
				if (GetResource (adef, "objects.xml") != null)
					return true;
				
				if (adef.Name.Name == "gtk-sharp")
					return false;  // Gtk is special-cased, so ignore it.

				if (!HasGtkReference (adef, new StringCollection ()))
					return false;

				foreach (TypeDefinition type in adef.MainModule.Types) {
					TypeReference tref = type.BaseType;
					while (tref != null) {
						if (tref.FullName == "Gtk.Window") {
							break;
						} else if (tref.FullName == "Gtk.Widget") {
							return true;
						}
						tref = resolver.Resolve (tref).BaseType;
					}
				}
			} catch {
			}
			return false;
		}
		
		void RefreshFile (string assembly)
		{
			assembly = Path.GetFullPath (assembly);
			LibraryInfo info = Members [assembly];
			if (info == null) {
				info = new LibraryInfo ();
				info.File = assembly;
				Members.Add (info);
			}
			info.Timestamp = File.GetLastWriteTime (assembly).ToUniversalTime ();
			info.Guid = Guid.NewGuid ();
			info.HasWidgets = CheckForWidgets (assembly);
			Save ();
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
		
		public static LibraryCache Load ()
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
	}
}
