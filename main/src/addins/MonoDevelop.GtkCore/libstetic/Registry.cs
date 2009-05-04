using System;
using System.Collections;
using System.Reflection;
using System.Xml;
using System.Xml.Xsl;

namespace Stetic {
	public static class Registry {

		static ArrayList libraries = new ArrayList ();
		static ArrayList classes = new ArrayList ();
		
		static XslTransform gladeImport, gladeExport;
		static WidgetLibrary coreLib;
		
		public static event EventHandler RegistryChanging;
		public static event EventHandler RegistryChanged;
		
		static int changing;
		static bool changed;

		public static void Initialize (WidgetLibrary coreLibrary)
		{
			RegisterWidgetLibrary (coreLibrary);
			coreLib = coreLibrary;
		}
		
		public static WidgetLibrary CoreWidgetLibrary {
			get { return coreLib; }
		}
		
		public static void BeginChangeSet ()
		{
			if (changing == 0)
				changed = false;
			changing++;
		}
		
		public static void EndChangeSet ()
		{
			if (--changing == 0) {
				if (changed) {
					foreach (WidgetLibrary lib in libraries)
						lib.Flush ();
					NotifyChanged ();
				}
				changed = false;
			}
		}
		
		public static void RegisterWidgetLibrary (WidgetLibrary library)
		{
			NotifyChanging ();
			
			try {
				if (coreLib != null && library.Name == coreLib.Name) {
					libraries.Remove (coreLib);
					InternalUpdate ();
					coreLib = library;
				}
				libraries.Add (library);
				library.Load ();
				classes.AddRange (library.AllClasses);
				UpdateGladeTransform ();
			} catch (Exception ex) {
				Console.WriteLine (ex);
				throw;
			} finally {
				NotifyChanged ();
			}
		}

		public static void UnregisterWidgetLibrary (WidgetLibrary library)
		{
			if (library == coreLib)
				return;

			NotifyChanging ();

			libraries.Remove (library);
			library.Dispose ();
			InternalUpdate ();

			NotifyChanged ();
		}
		
		
		// Returns true if all libraries that need reloading
		// could be reloaded
		
		public static bool ReloadWidgetLibraries ()
		{
			bool needsReload = false;

			// If there is a lib which can't be reloaded, 
			// there is no need to start the reloading process
			
			foreach (WidgetLibrary lib in libraries) {
				if (lib != coreLib && lib.NeedsReload) {
					if (!lib.CanReload)
						return false;
					needsReload = true;
				}
			}
			
			if (!needsReload)
				return true;

			try {
				NotifyChanging ();
				
				foreach (WidgetLibrary lib in libraries)
					if (lib != coreLib && lib.NeedsReload)
						lib.Reload ();

				InternalUpdate ();
			} finally {
				NotifyChanged ();
			}
			
			return true;
		}
		
		public static bool IsRegistered (WidgetLibrary library)
		{
			return libraries.Contains (library);
		}
		
		public static WidgetLibrary GetWidgetLibrary (string name)
		{
			foreach (WidgetLibrary lib in libraries)
				if (lib.Name == name)
					return lib;
			return null;
		}
		
		public static bool IsRegistered (string name)
		{
			foreach (WidgetLibrary lib in libraries)
				if (lib.Name == name)
					return true;
			return false;
		}
		
		public static WidgetLibrary[] RegisteredWidgetLibraries {
			get { return (WidgetLibrary[]) libraries.ToArray (typeof(WidgetLibrary)); }
		}
		
		static void NotifyChanging ()
		{
			if (changing > 0) {
				if (changed)
					return;
				else
					changed = true;
			}
			if (RegistryChanging != null)
				RegistryChanging (null, EventArgs.Empty);
		}
		
		static void NotifyChanged ()
		{
			if (changing == 0 && RegistryChanged != null)
				RegistryChanged (null, EventArgs.Empty);
		}
		
		static void InternalUpdate ()
		{
			classes.Clear ();
			foreach (WidgetLibrary lib in libraries)
				classes.AddRange (lib.AllClasses);
			UpdateGladeTransform ();
		}

		static void UpdateGladeTransform ()
		{
			XmlDocument doc = CreateGladeTransformBase ();
			XmlNamespaceManager nsm = new XmlNamespaceManager (doc.NameTable);
			nsm.AddNamespace ("xsl", "http://www.w3.org/1999/XSL/Transform");
			
			foreach (WidgetLibrary lib in libraries) {
				foreach (XmlElement elem in lib.GetGladeImportTransformElements ())
					doc.FirstChild.PrependChild (doc.ImportNode (elem, true));
			}
			
			gladeImport = new XslTransform ();
			gladeImport.Load (doc, null, null);
				
			doc = CreateGladeTransformBase ();
			
			foreach (WidgetLibrary lib in libraries) {
				foreach (XmlElement elem in lib.GetGladeExportTransformElements ())
					doc.FirstChild.PrependChild (doc.ImportNode (elem, true));
			}
			
			gladeExport = new XslTransform ();
			gladeExport.Load (doc, null, null);
		}
			
		static XmlDocument CreateGladeTransformBase ()
		{
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (
				"<xsl:stylesheet version='1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform'>" +
				"  <xsl:template match='@*|node()'>" +
				"    <xsl:copy>" +
				"      <xsl:apply-templates select='@*|node()' />" +
				"    </xsl:copy>" +
				"  </xsl:template>" +
				"</xsl:stylesheet>"
				);
			return doc;
		}

		public static IEnumerable AllClasses {
			get {
				return classes;
			}
		}

		public static XslTransform GladeImportXsl {
			get {
				return gladeImport;
			}
		}

		public static XslTransform GladeExportXsl {
			get {
				return gladeExport;
			}
		}

		public static EnumDescriptor LookupEnum (string typeName)
		{
			foreach (WidgetLibrary lib in libraries) {
				EnumDescriptor desc = lib.LookupEnum (typeName);
				if (desc != null)
					return desc;
			}
			return null;
		}

		public static ClassDescriptor LookupClassByCName (string cname)
		{
			foreach (WidgetLibrary lib in libraries) {
				ClassDescriptor desc = lib.LookupClassByCName (cname);
				if (desc != null)
					return desc;
			}
			return null;
		}
		
		public static ClassDescriptor LookupClassByName (string cname)
		{
			foreach (WidgetLibrary lib in libraries) {
				ClassDescriptor desc = lib.LookupClassByName (cname);
				if (desc != null)
					return desc;
			}
			return null;
		}
		
		static ClassDescriptor FindGroupClass (string name, out string groupname)
		{
			int sep = name.LastIndexOf ('.');
			string classname = name.Substring (0, sep);
			groupname = name.Substring (sep + 1);
			ClassDescriptor klass = LookupClassByName (classname);
			if (klass == null) {
				klass = LookupClassByName (name);
				if (klass == null)
					throw new ArgumentException ("No class for itemgroup " + name);
				classname = name;
				groupname = "";
			}
			return klass;
		}

		public static ItemGroup LookupItemGroup (string name)
		{
			string groupname;
			ClassDescriptor klass = FindGroupClass (name, out groupname);
			
			foreach (ItemGroup grp in klass.ItemGroups)
				if (grp.Name == groupname && grp.DeclaringType == klass)
					return grp;

			throw new ArgumentException ("No itemgroup '" + groupname + "' in class " + klass.WrappedTypeName);
		}

		public static ItemGroup LookupSignalGroup (string name)
		{
			string groupname;
			ClassDescriptor klass = FindGroupClass (name, out groupname);
			
			foreach (ItemGroup grp in klass.SignalGroups)
				if (grp.Name == groupname && grp.DeclaringType == klass)
					return grp;
			throw new ArgumentException ("No itemgroup '" + groupname + "' in class " + klass.WrappedTypeName);
		}

		public static ItemDescriptor LookupItem (string name)
		{
			int sep = name.LastIndexOf ('.');
			string classname = name.Substring (0, sep);
			string propname = name.Substring (sep + 1);
			ClassDescriptor klass = LookupClassByName (classname);
			if (klass == null)
				throw new ArgumentException ("No class " + classname + " for property " + propname);
			ItemDescriptor idesc = klass[propname];
			if (idesc == null)
				throw new ArgumentException ("Property '" + propname + "' not found in class '" + classname + "'");
			return idesc;
		}

		public static ItemGroup LookupContextMenu (string classname)
		{
			ClassDescriptor klass = LookupClassByName (classname);
			if (klass == null)
				throw new ArgumentException ("No class for contextmenu " + classname);
			return klass.ContextMenu;
		}

		public static object NewInstance (string typeName, IProject proj)
		{
			return LookupClassByName (typeName).NewInstance (proj);
		}
		
		public static Type GetType (string typeName, bool throwOnError)
		{
			Type t = Type.GetType (typeName, false);
			if (t != null) return t;
			
			foreach (WidgetLibrary lib in libraries) {
				t = lib.GetType (typeName);
				if (t != null) return t;
			}
			
			string tname, aname;
			int i = typeName.IndexOf (',');
			if (i != -1) {
				tname = typeName.Substring (0, i).Trim ();
				aname = typeName.Substring (i + 1).Trim ();
			}
			else {
				tname = typeName;
				aname = null;
			}
			foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies ()) {
				if (aname == null || asm.GetName ().Name == aname) {
					t = asm.GetType (tname);
					if (t != null)
						return t;
				}
			}
			
			if (throwOnError)
				throw new TypeLoadException ("Could not load type '" + typeName + "'");
			
			return null;
		}
	}
}
