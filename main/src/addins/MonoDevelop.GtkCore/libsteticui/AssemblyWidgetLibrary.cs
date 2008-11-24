using System;
using System.Collections;
using System.Reflection;
using System.Xml;
using System.IO;

namespace Stetic
{
	internal class AssemblyWidgetLibrary: WidgetLibrary
	{
		static LibraryCache Cache = LibraryCache.Cache;

		Assembly assembly;
		string name;
		AssemblyResolver resolver;
		LibraryCache.LibraryInfo cache_info;
		
		public AssemblyWidgetLibrary (string name, Assembly assembly)
		{
			this.name = name;
			this.assembly = assembly;
			UpdateCache ();
		}
		
		public AssemblyWidgetLibrary (AssemblyResolver resolver, string assemblyPath)
		{
			this.name = assemblyPath;
			
			string ares = resolver.Resolve (assemblyPath, null);
			if (ares != null)
				assemblyPath = ares;
			
			this.resolver = resolver;
			if (assemblyPath.EndsWith (".dll") || assemblyPath.EndsWith (".exe")) {
				if (File.Exists (assemblyPath))
					assembly = Assembly.LoadFrom (assemblyPath);
			} else
				assembly = Assembly.Load (assemblyPath);
				
			if (assembly == null)
				throw new InvalidOperationException ("Couldn't load assembly at " + assemblyPath);

			UpdateCache ();
		}
		
		void UpdateCache ()
		{
			Cache.Refresh (resolver, assembly.Location);
			cache_info = Cache [assembly.Location];
		}
		
		public override string Name {
			get { return name; }
		}
		
		public override bool NeedsReload {
			get {
				return File.Exists (assembly.Location) && !Cache.IsCurrent (assembly.Location);
			}
		}
		
		public override bool CanReload {
			get { return false; }
		}
		
		public override void Load ()
		{
			Load (cache_info.ObjectsDocument);
		}

		protected override ClassDescriptor LoadClassDescriptor (XmlElement element)
		{
			return new TypedClassDescriptor (assembly, element);
		}
		
		public override Type GetType (string typeName)
		{
			Type t = assembly.GetType (typeName, false);
			if (t != null) return t;
			
			// Look in referenced assemblies
/*
			Disabled. The problem is that Assembly.Load tries to load the exact version
			of the assembly, and loaded references may not have the same exact version.
			
			foreach (AssemblyName an in assembly.GetReferencedAssemblies ()) {
				Assembly a = Assembly.Load (an);
				t = a.GetType (typeName);
				if (t != null) return t;
			}
*/
			return null;
		}
		
		public override System.IO.Stream GetResource (string name)
		{
			return assembly.GetManifestResourceStream (name);
		}
		
		public override string[] GetLibraryDependencies ()
		{
			if (!cache_info.HasWidgets)
				return new string [0];
				
			ArrayList list = new ArrayList ();
			ScanLibraries (list, assembly);
			return (string[]) list.ToArray (typeof(string));
		}
		
		void ScanLibraries (ArrayList list, Assembly asm)
		{
			foreach (AssemblyName aname in asm.GetReferencedAssemblies ()) {
				Assembly depasm = null;
				try {
					depasm = Assembly.Load (aname);
				} catch {
				}
				
				if (depasm == null) {
					string file = resolver.Resolve (aname.FullName, Path.GetDirectoryName (asm.Location));
					if (file != null)
						depasm = Assembly.LoadFrom (file);
					else
						throw new InvalidOperationException ("Assembly not found: " + aname.FullName);
				}
				
				ManifestResourceInfo res = depasm.GetManifestResourceInfo ("objects.xml");
				if (res != null)
					list.Add (depasm.FullName);
			}
		}
	}
	
}
