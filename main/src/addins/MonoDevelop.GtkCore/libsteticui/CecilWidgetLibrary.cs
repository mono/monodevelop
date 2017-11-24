// CecilWidgetLibrary.cs
//
// Authors: 
//	Lluis Sanchez Gual  <lluis@novell.com>
//	Mike Kestner  <mkestner@novell.com>
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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Xml;
using Mono.Cecil;

namespace Stetic
{
	internal class CecilWidgetLibrary: WidgetLibrary
	{
		static LibraryCache cache = LibraryCache.Cache;

		string name;
		string filename;
		string[] dependencies;
		AssemblyResolver resolver;
		bool canGenerateCode;
		bool info_changed;
		bool objects_dirty;
		AssemblyDefinition assembly;
		LibraryCache.LibraryInfo cache_info;
		
		public CecilWidgetLibrary (AssemblyResolver resolver, string path)
		{
			name = path;
			this.resolver = resolver;

			if (System.IO.File.Exists (path))
				filename = path;
			else if (resolver != null)
				filename = resolver.Resolve (path, null);
			
			if (filename == null)
				filename = path;
			else
				filename = System.IO.Path.GetFullPath (filename);

			RefreshFromCache ();
		}

		public override string Name {
			get { return name; }
		}
		
		public override bool NeedsReload {
			get {
				cache.Refresh (resolver, filename);
				return info_changed; 
			}
		}
		
		public override bool CanReload {
			get { return true; }
		}
		
		public override bool CanGenerateCode {
			get { return canGenerateCode; }
		}
		
		public override void Load ()
		{
			canGenerateCode = true;
			
			RefreshFromCache ();
			if (cache_info == null || !File.Exists (filename))
				return;

			using (assembly = AssemblyDefinition.ReadAssembly (filename)) {
				objects_dirty = false;
				Load (cache_info.ObjectsDocument);
				if (objects_dirty)
					cache_info.WriteObjectsFile ();

				foreach (string dep in GetLibraryDependencies ()) {
					WidgetLibrary lib = Registry.GetWidgetLibrary (dep);
					if (lib != null && !lib.CanGenerateCode)
						canGenerateCode = false;
				}
			}
			assembly = null;
			info_changed = false;
		}
		
		void RefreshFromCache ()
		{
			LibraryCache.LibraryInfo newInfo = cache.Refresh (resolver, filename);
			if (newInfo != cache_info) {
				if (cache_info != null)
					cache_info.Changed -= OnCacheInfoChanged;
				cache_info = newInfo;
				cache_info.Changed += OnCacheInfoChanged;
			}
		}
		
		void OnCacheInfoChanged (object o, EventArgs a)
		{
			info_changed = true;
		}
		
		public override void Dispose ()
		{
			base.Dispose ();
			if (cache_info != null) {
				cache_info.Changed -= OnCacheInfoChanged;
				cache_info = null;
			}
		}

		
		protected override ClassDescriptor LoadClassDescriptor (XmlElement element)
		{
			string name = element.GetAttribute ("type");
			
			TypeDefinition cls = null;
			Stetic.ClassDescriptor typeClassDescriptor = null;
			string tname;
			
			if (element.HasAttribute ("baseClassType")) {
				tname = element.GetAttribute ("baseClassType");
				typeClassDescriptor = Stetic.Registry.LookupClassByName (tname);
			} else {
				cls = assembly.MainModule.GetType (name);
				if (cls != null) {
					// Find the nearest type that can be loaded
					typeClassDescriptor = FindType (cls);
					tname = cls.Name;
					if (typeClassDescriptor != null) {
						element.SetAttribute ("baseClassType", typeClassDescriptor.Name);
						objects_dirty = true;
					}
				}
			}
			
			if (typeClassDescriptor == null) {
				Console.WriteLine ("Descriptor not found: " + name);
				return null;
			}
			
			XmlElement steticDefinition = null;
			
			XmlDocument gui = cache [filename].GuiDocument;
			if (gui != null) {
				string wrappedTypeName = element.GetAttribute ("type");
				steticDefinition = (XmlElement) gui.DocumentElement.SelectSingleNode ("widget[@id='" + wrappedTypeName + "']");
			}
			
			CecilClassDescriptor cd = new CecilClassDescriptor (this, element, typeClassDescriptor, steticDefinition, cls);
			
			if (canGenerateCode && !cd.CanGenerateCode)
				canGenerateCode = false;
			return cd;
		}
		
		Stetic.ClassDescriptor FindType (TypeDefinition cls)
		{
			if (cls.BaseType == null)
				return null;
			Stetic.ClassDescriptor klass = Stetic.Registry.LookupClassByName (cls.BaseType.FullName);
			if (klass != null) return klass;
			
			TypeDefinition bcls = FindTypeDefinition (cls.BaseType.FullName);
			if (bcls == null)
				return null;

			return FindType (bcls);
		}
		
		AssemblyDefinition ResolveAssembly (AssemblyNameReference aref)
		{
			return resolver.Resolve (aref, Path.GetDirectoryName (filename));
		}
		
		internal TypeDefinition FindTypeDefinition (string fullName)
		{
			return FindTypeDefinition (assembly, fullName);
		}
		
		TypeDefinition FindTypeDefinition (AssemblyDefinition asm, string fullName)
		{
			TypeDefinition cls = asm.MainModule.GetType (fullName);
			if (cls != null)
				return cls;
			
			foreach (AssemblyNameReference aref in asm.MainModule.AssemblyReferences) {
				AssemblyDefinition basm = ResolveAssembly (aref);
				if (basm != null) {
					cls = basm.MainModule.GetType (fullName);
					if (cls != null)
						return cls;
				}
			}
			return null;
		}
		
		public override string[] GetLibraryDependencies ()
		{
			if (NeedsReload || dependencies == null)
				LoadDependencies ();
			return dependencies;
		}
		
		void LoadDependencies ()
		{
			RefreshFromCache ();
			if (cache_info == null || cache_info.ObjectsDocument == null) {
				dependencies = new string[0];
				return;
			}
			XmlElement elem = cache_info.ObjectsDocument.DocumentElement ["dependencies"];
			if (elem != null) {
				ArrayList list = new ArrayList ();
				foreach (XmlElement dep in elem.SelectNodes ("dependency"))
					list.Add (dep.InnerText);
				dependencies = (string[]) list.ToArray (typeof(string));
			} else
				dependencies = new string[0];
		}
		
		public static bool IsWidgetLibrary (string path)
		{
			// Info can be null if the library could not be scanned for some reason
			var info = cache [path];
			return info != null && info.HasWidgets;
		}
		
		public static string GetInstanceType (TypeDefinition td, TypeReference sourceType, TypeReference tref)
		{
			string tn = null;
			if (sourceType is GenericInstanceType) {
				GenericInstanceType it = (GenericInstanceType) sourceType;
				foreach (GenericParameter gc in td.GenericParameters) {
					if (gc.Name == tref.FullName) {
						tn = it.GenericArguments [gc.Position].FullName;
						break;
					}
				}
			}
			if (tn == null)
				tn = tref.FullName;
			tn = tn.Replace ('<', '[');
			tn = tn.Replace ('>', ']');
			return tn;
		}
		
		public static List<ComponentType> GetComponentTypes (Application app, string filename)
		{
			List<ComponentType> list = new List<ComponentType> ();

			LibraryCache.LibraryInfo info = cache.Refresh (new AssemblyResolver (app.Backend), filename);
			if (info.ObjectsDocument == null)
				return list;

			string defTargetGtkVersion = info.ObjectsDocument.DocumentElement.GetAttribute ("gtk-version");
			if (defTargetGtkVersion.Length == 0)
				defTargetGtkVersion = "2.4";

			using (AssemblyDefinition adef = AssemblyDefinition.ReadAssembly (filename)) {
				foreach (XmlElement elem in info.ObjectsDocument.SelectNodes ("objects/object")) {
					if (elem.GetAttribute ("internal") == "true" || elem.HasAttribute ("deprecated") || !elem.HasAttribute ("palette-category"))
						continue;

					string iconname = elem.GetAttribute ("icon");
					Gdk.Pixbuf icon = GetEmbeddedIcon (adef, iconname);

					string targetGtkVersion = elem.GetAttribute ("gtk-version");
					if (targetGtkVersion.Length == 0)
						targetGtkVersion = defTargetGtkVersion;

					ComponentType ct = new ComponentType (app,
						elem.GetAttribute ("type"),
						elem.GetAttribute ("label"),
						elem.GetAttribute ("type"),
						elem.GetAttribute ("palette-category"),
						targetGtkVersion,
						filename,
						icon);

					list.Add (ct);
				}
			}
			
			return list;
		}
		
		public Gdk.Pixbuf GetEmbeddedIcon (string iconname)
		{
			return GetEmbeddedIcon (assembly, iconname);
		}
		
		static Gdk.Pixbuf GetEmbeddedIcon (AssemblyDefinition asm, string iconname)
		{
			Gdk.Pixbuf icon = null;
			if (iconname != null && iconname.Length > 0) {
				try {
					// Using the pixbuf resource constructor generates a gdk warning.
					EmbeddedResource res = GetResource (asm, iconname);
					Gdk.PixbufLoader loader = new Gdk.PixbufLoader (res.GetResourceData ());
					icon = loader.Pixbuf;
				} catch {
					// Ignore
				}
			}
			
			if (icon == null) {
				ClassDescriptor cc = Registry.LookupClassByName ("Gtk.Bin");
				icon = cc.Icon;
			}
			return icon;
		}
		
		static EmbeddedResource GetResource (AssemblyDefinition asm, string name)
		{
			foreach (Resource res in asm.MainModule.Resources) {
				EmbeddedResource eres = res as EmbeddedResource;
				if (eres != null && eres.Name == name)
					return eres;
			}
			return null;
		}
		
		public override void Flush ()
		{
			base.Flush ();
			if (resolver != null)
				resolver.ClearCache ();
		}

	}
}
