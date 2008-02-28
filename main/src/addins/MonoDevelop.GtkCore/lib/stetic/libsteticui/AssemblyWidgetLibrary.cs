using System;
using System.Collections;
using System.Reflection;
using System.Xml;
using System.IO;

namespace Stetic
{
	internal class AssemblyWidgetLibrary: WidgetLibrary
	{
		Assembly assembly;
		DateTime timestamp;
		string name;
		bool isWidgetLibrary;
		XmlDocument objectsDoc;
		ImportContext importContext;
		
		public AssemblyWidgetLibrary (string name, Assembly assembly)
		{
			this.name = name;
			this.assembly = assembly;
			timestamp = System.IO.File.GetLastWriteTime (assembly.Location);
			Init ();
		}
		
		public AssemblyWidgetLibrary (ImportContext importContext, string assemblyPath)
		{
			this.name = assemblyPath;
			
			string ares = importContext.App.ResolveAssembly (assemblyPath);
			if (ares != null)
				assemblyPath = ares;
			
			this.importContext = importContext;
			if (assemblyPath.EndsWith (".dll") || assemblyPath.EndsWith (".exe")) {
				if (File.Exists (assemblyPath))
					assembly = Assembly.LoadFrom (assemblyPath);
			} else
				assembly = Assembly.Load (assemblyPath);
				
			if (assembly != null)
				timestamp = System.IO.File.GetLastWriteTime (assembly.Location);
			else
				timestamp = DateTime.MinValue;

			Init ();
		}
		
		void Init ()
		{
			objectsDoc = new XmlDocument ();
			System.IO.Stream stream = null;
			
			if (assembly != null)
				stream = assembly.GetManifestResourceStream ("objects.xml");
				
			if (stream == null) {
				isWidgetLibrary = false;
			}
			else {
				using (stream) {
					objectsDoc.Load (stream);
				}
				isWidgetLibrary = true;
			}
		}
		
		public override string Name {
			get { return name; }
		}
		
		public override bool NeedsReload {
			get {
				if (!System.IO.File.Exists (assembly.Location))
					return false;
				return System.IO.File.GetLastWriteTime (assembly.Location) != timestamp;
			}
		}
		
		public override bool CanReload {
			get { return false; }
		}
		
		public Assembly Assembly {
			get { return assembly; }
		}
		
		public DateTime TimeStamp {
			get { return timestamp; }
		}
		
		public override void Load ()
		{
			if (objectsDoc == null)
				Init ();
			Load (objectsDoc);
			objectsDoc = null;
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
			if (objectsDoc == null)
				Init ();
				
			if (!isWidgetLibrary)
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
					string file = CecilWidgetLibrary.FindAssembly (importContext, aname.FullName, Path.GetDirectoryName (asm.Location));
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
