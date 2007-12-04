using System;
using System.Collections;
using System.Xml;

namespace Stetic
{
	public abstract class WidgetLibrary: IDisposable
	{
		Hashtable classes_by_cname = new Hashtable ();
		Hashtable classes_by_csname = new Hashtable ();
		Hashtable enums = new Hashtable ();
		string targetGtkVersion;

		XmlElement[] importElems = new XmlElement [0];
		XmlElement[] exportElems = new XmlElement [0];
		
		public event EventHandler Changed;
		
		public abstract string Name { get; }
		
		public virtual bool NeedsReload {
			get { return false; }
		}
		
		public virtual bool CanReload {
			get { return false; }
		}
		
		// Returns true if it is possible to generate code using this widget library.
		// Not all widget libraries can generate code. For example, when a widget 
		// depends on a wrapper class, and the wrapper class can't be loaded in memory,
		// then it is not possible to generate code.
		public virtual bool CanGenerateCode {
			get { return true; }
		}
		
		public virtual string[] GetLibraryDependencies ()
		{
			return new string [0];
		}
		
		public string TargetGtkVersion {
			get { return targetGtkVersion != null && targetGtkVersion.Length > 0 ? targetGtkVersion : "2.4"; }
		}
		
		public bool SupportsGtkVersion (string targetVersion)
		{
			return WidgetUtils.CompareVersions (TargetGtkVersion, targetVersion) >= 0;
		}
		
		public virtual void Reload ()
		{
			Load ();
		}

		public virtual void Load ()
		{
		}

		protected virtual void Load (XmlDocument objects)
		{
			classes_by_cname.Clear ();
			classes_by_csname.Clear ();
			enums.Clear ();
			
			if (objects == null || objects.DocumentElement == null)
				return;
			
			targetGtkVersion = objects.DocumentElement.GetAttribute ("gtk-version");
			if (targetGtkVersion.Length == 0)
				targetGtkVersion = "2.4";
			
			foreach (XmlElement element in objects.SelectNodes ("/objects/enum")) {
				EnumDescriptor enm = new EnumDescriptor (element);
				enums[enm.Name] = enm;
			}

			foreach (XmlElement element in objects.SelectNodes ("/objects/object")) {
				ClassDescriptor klass = LoadClassDescriptor (element);
				if (klass == null) continue;
				klass.SetLibrary (this);
				classes_by_cname[klass.CName] = klass;
				classes_by_csname[klass.WrappedTypeName] = klass;
			}

			XmlNamespaceManager nsm = new XmlNamespaceManager (objects.NameTable);
			nsm.AddNamespace ("xsl", "http://www.w3.org/1999/XSL/Transform");
			
			XmlNodeList nodes = objects.SelectNodes ("/objects/object/glade-transform/import/xsl:*", nsm);
			importElems = new XmlElement [nodes.Count];
			for (int n=0; n<nodes.Count; n++)
				importElems [n] = (XmlElement) nodes[n];
				
			nodes = objects.SelectNodes ("/objects/object/glade-transform/export/xsl:*", nsm);
			exportElems = new XmlElement [nodes.Count];
			for (int n=0; n<nodes.Count; n++)
				exportElems [n] = (XmlElement) nodes[n];
		}
		
		public virtual void Dispose ()
		{
		}
		
		protected abstract ClassDescriptor LoadClassDescriptor (XmlElement element);
		
		
		public virtual XmlElement[] GetGladeImportTransformElements ()
		{
			return importElems;
		}

		public virtual XmlElement[] GetGladeExportTransformElements ()
		{
			return exportElems;
		}

		public virtual ICollection AllClasses {
			get {
				return classes_by_csname.Values;
			}
		}

		public virtual EnumDescriptor LookupEnum (string typeName)
		{
			return (EnumDescriptor)enums[typeName];
		}

		public virtual ClassDescriptor LookupClassByCName (string cname)
		{
			return (ClassDescriptor)classes_by_cname[cname];
		}
		
		public virtual ClassDescriptor LookupClassByName (string csname)
		{
			return (ClassDescriptor)classes_by_csname[csname];
		}
		
		public virtual Type GetType (string typeName)
		{
			return null;
		}
		
		public virtual System.IO.Stream GetResource (string name)
		{
			return null;
		}
		
		protected virtual void OnChanged ()
		{
			if (Changed != null)
				Changed (this, EventArgs.Empty);
		}
	}
}
