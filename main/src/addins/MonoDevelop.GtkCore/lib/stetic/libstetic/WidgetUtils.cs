using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Xml;
using Stetic.Wrapper;

namespace Stetic
{
	public static class WidgetUtils
	{
		static Gdk.Atom steticAtom;
		static int undoIdCount;
		static Gdk.Pixbuf missingIcon;
		static Gtk.Widget renderer;
		
		public static Gdk.Atom ApplicationXSteticAtom {
			get {
				if (steticAtom == null)
					steticAtom = Gdk.Atom.Intern ("application/x-stetic", false);
				return steticAtom;
			}
		}

		public static XmlElement ExportWidget (Gtk.Widget widget)
		{
			XmlDocument doc = new XmlDocument ();
			Stetic.Wrapper.Widget wrapper = Stetic.Wrapper.Widget.Lookup (widget);
			if (wrapper == null)
				throw new InvalidOperationException ();
				
			XmlElement elem = wrapper.Write (new ObjectWriter (doc, FileFormat.Native));
			doc.AppendChild (elem);
			return doc.DocumentElement;
		}
		
		public static Gtk.Widget ImportWidget (IProject project, XmlElement element)
		{
			ObjectReader reader = new ObjectReader (project, FileFormat.Native);
			ObjectWrapper wrapper = Stetic.ObjectWrapper.ReadObject (reader, element);
			return wrapper.Wrapped as Gtk.Widget;
		}
		
		public static XmlElement Write (ObjectWrapper wrapper, XmlDocument doc)
		{
			ClassDescriptor klass = wrapper.ClassDescriptor;

			XmlElement elem = doc.CreateElement ("widget");
			elem.SetAttribute ("class", klass.Name);
			elem.SetAttribute ("id", ((Gtk.Widget)wrapper.Wrapped).Name);

			GetProps (wrapper, elem);
			GetSignals (wrapper, elem);
			return elem;
		}

		public static void GetProps (ObjectWrapper wrapper, XmlElement parent_elem)
		{
			ClassDescriptor klass = wrapper.ClassDescriptor;

			foreach (ItemGroup group in klass.ItemGroups) {
				foreach (ItemDescriptor item in group) {
					PropertyDescriptor prop = item as PropertyDescriptor;
					if (prop == null)
						continue;
					if (!prop.VisibleFor (wrapper.Wrapped) || !prop.CanWrite || prop.Name == "Name")	// Name is written in the id attribute
						continue;

					object value = prop.GetValue (wrapper.Wrapped);
					
					// If the property has its default value, we don't need to write it
					if (value == null || (prop.HasDefault && prop.IsDefaultValue (value)))
						continue;
				
					string val = prop.ValueToString (value);
					if (val == null)
						continue;

					XmlElement prop_elem = parent_elem.OwnerDocument.CreateElement ("property");
					prop_elem.SetAttribute ("name", prop.Name);
					if (val.Length > 0)
						prop_elem.InnerText = val;

					if (prop.Translatable && prop.IsTranslated (wrapper.Wrapped)) {
						prop_elem.SetAttribute ("translatable", "yes");
						string tcx = prop.TranslationContext (wrapper.Wrapped);
						if (tcx != null && tcx.Length > 0) {
							prop_elem.SetAttribute ("context", "yes");
							prop_elem.InnerText = tcx + "|" + prop_elem.InnerText;
						}
						string tcm = prop.TranslationComment (wrapper.Wrapped);
						if (tcm != null && tcm.Length > 0)
							prop_elem.SetAttribute ("comments", prop.TranslationComment (wrapper.Wrapped));
					}

					parent_elem.AppendChild (prop_elem);
				}
			}
		}

		public static void GetSignals (ObjectWrapper ob, XmlElement parent_elem)
		{
			foreach (Signal signal in ob.Signals) {
				if (!signal.SignalDescriptor.VisibleFor (ob.Wrapped))
					continue;

				XmlElement signal_elem = parent_elem.OwnerDocument.CreateElement ("signal");
				signal_elem.SetAttribute ("name", signal.SignalDescriptor.Name);
				signal_elem.SetAttribute ("handler", signal.Handler);
				if (signal.After)
					signal_elem.SetAttribute ("after", "yes");
				parent_elem.AppendChild (signal_elem);
			}
		}
		
		static public void Read (ObjectWrapper wrapper, XmlElement elem)
		{
			string className = elem.GetAttribute ("class");
			if (className == null)
				throw new GladeException ("<widget> node with no class name");

			ClassDescriptor klass = Registry.LookupClassByName (className);
			if (klass == null)
				throw new GladeException ("No stetic ClassDescriptor for " + className);

			Gtk.Widget widget = (Gtk.Widget) wrapper.Wrapped;
			if (widget == null) {
				widget = (Gtk.Widget) klass.CreateInstance (wrapper.Project);
				ObjectWrapper.Bind (wrapper.Project, klass, wrapper, widget, true);
			}
			
			widget.Name = elem.GetAttribute ("id");
			
			ReadMembers (klass, wrapper, widget, elem);
			
			if (!(widget is Gtk.Window))
				widget.ShowAll ();
		}
		
		public static void ReadMembers (ClassDescriptor klass, ObjectWrapper wrapper, object wrapped, XmlElement elem)
		{
			foreach (XmlNode node in elem.ChildNodes) {
				XmlElement child = node as XmlElement;
				if (child == null)
					continue;
					
				if (child.LocalName == "signal")
					ReadSignal (klass, wrapper, child);
				else if (child.LocalName == "property")
					ReadProperty (klass, wrapper, wrapped, child);
			}
		}
		
		public static void ReadSignal (ClassDescriptor klass, ObjectWrapper ob, XmlElement elem)
		{
			string name = elem.GetAttribute ("name");
			SignalDescriptor signal = klass.SignalGroups.GetItem (name) as SignalDescriptor;
			if (signal != null) {
				string handler = elem.GetAttribute ("handler");
				bool after = elem.GetAttribute ("after") == "yes";
				ob.Signals.Add (new Signal (signal, handler, after));
			}
		}

		public static void ReadProperty (ClassDescriptor klass, ObjectWrapper wrapper, object wrapped, XmlElement prop_node)
		{
			string name = prop_node.GetAttribute ("name");
			PropertyDescriptor prop = klass [name] as PropertyDescriptor;
			if (prop == null || !prop.CanWrite)
				return;

			string strval = prop_node.InnerText;
			
			// Skip translation context
			if (prop_node.GetAttribute ("context") == "yes" && strval.IndexOf ('|') != -1)
				strval = strval.Substring (strval.IndexOf ('|') + 1);
				
			object value = prop.StringToValue (strval);
			prop.SetValue (wrapped, value);
			
			if (prop.Translatable) {
				if (prop_node.GetAttribute ("translatable") != "yes") {
					prop.SetTranslated (wrapped, false);
				}
				else {
					prop.SetTranslated (wrapped, true);
					if (prop_node.GetAttribute ("context") == "yes") {
						strval = prop_node.InnerText;
						int bar = strval.IndexOf ('|');
						if (bar != -1)
							prop.SetTranslationContext (wrapped, strval.Substring (0, bar));
					}

					if (prop_node.HasAttribute ("comments"))
						prop.SetTranslationComment (wrapped, prop_node.GetAttribute ("comments"));
				}
			}
		}
		
		static public void SetPacking (Stetic.Wrapper.Container.ContainerChild wrapper, XmlElement child_elem)
		{
			XmlElement packing = child_elem["packing"];
			if (packing == null)
				return;

			Gtk.Container.ContainerChild cc = wrapper.Wrapped as Gtk.Container.ContainerChild;
			ClassDescriptor klass = wrapper.ClassDescriptor;
			ReadMembers (klass, wrapper, cc, packing);
		}
		
		internal static XmlElement CreatePacking (XmlDocument doc, Stetic.Wrapper.Container.ContainerChild childwrapper)
		{
			XmlElement packing_elem = doc.CreateElement ("packing");
			WidgetUtils.GetProps (childwrapper, packing_elem);
			return packing_elem;
		}
		
		public static void Copy (Gtk.Widget widget, Gtk.SelectionData seldata, bool copyAsText)
		{
			XmlElement elem = ExportWidget (widget);
			if (elem == null)
				return;

			if (copyAsText)
				seldata.Text = elem.OuterXml;
			else
				seldata.Set (ApplicationXSteticAtom, 8, System.Text.Encoding.UTF8.GetBytes (elem.OuterXml));
		}

		public static Stetic.Wrapper.Widget Paste (IProject project, Gtk.SelectionData seldata)
		{
			if (seldata == null || seldata.Type == null || seldata.Type.Name != ApplicationXSteticAtom.Name)
				return null;
				
			string data = System.Text.Encoding.UTF8.GetString (seldata.Data);
			XmlDocument doc = new XmlDocument ();
			doc.PreserveWhitespace = true;
			try {
				doc.LoadXml (data);
			} catch {
				return null;
			}
			
			Gtk.Widget w = ImportWidget (project, doc.DocumentElement);
			return Wrapper.Widget.Lookup (w);
		}
		
		public static IDesignArea GetDesignArea (Gtk.Widget w)
		{
			while (w != null && !(w is IDesignArea))
				w = w.Parent;
			return w as IDesignArea;
		}
		
		internal static void ParseWidgetName (string name, out string baseName, out int idx)
		{
			// Extract a numerical suffix from the name
			// If suffix has more than 4 digits, only the last 4 digits are considered
			// a numerical suffix.
			
			int n;
			for (n = name.Length - 1; n >= name.Length-4 && n >= 0 && char.IsDigit (name [n]); n--)
				;
				
			if (n < name.Length - 1) {
				baseName = name.Substring (0, n + 1);
				idx = int.Parse (name.Substring (n + 1));
			} else {
				baseName = name;
				idx = 0;
			}
		}
		
		internal static string GetUndoId ()
		{
			return (undoIdCount++).ToString ();
		}
		
		public static Gdk.Pixbuf MissingIcon {
			get {
				if (missingIcon == null) {
					try {
						missingIcon = Gtk.IconTheme.Default.LoadIcon ("gtk-missing-image", 16, 0);
					} catch {}
					if (missingIcon == null)
						missingIcon = Gdk.Pixbuf.LoadFromResource ("missing.png");
				}
				return missingIcon;
			}
		}
		
		public static string AbsoluteToRelativePath (string baseDirectoryPath, string absPath)
		{
			if (! Path.IsPathRooted (absPath))
				return absPath;
			
			absPath = Path.GetFullPath (absPath);
			baseDirectoryPath = Path.GetFullPath (baseDirectoryPath);
			
			char[] separators = { Path.DirectorySeparatorChar, Path.VolumeSeparatorChar, Path.AltDirectorySeparatorChar };
			baseDirectoryPath = baseDirectoryPath.TrimEnd (separators);
			string[] bPath = baseDirectoryPath.Split (separators);
			string[] aPath = absPath.Split (separators);
			int indx = 0;
			for(; indx < Math.Min(bPath.Length, aPath.Length); ++indx){
				if(!bPath[indx].Equals(aPath[indx]))
					break;
			}
			
			if (indx == 0) {
				return absPath;
			}
			
			string erg = "";
			
			if(indx == bPath.Length) {
				erg += "." + Path.DirectorySeparatorChar;
			} else {
				for (int i = indx; i < bPath.Length; ++i) {
					erg += ".." + Path.DirectorySeparatorChar;
				}
			}
			erg += String.Join(Path.DirectorySeparatorChar.ToString(), aPath, indx, aPath.Length-indx);
			
			return erg;
		}
		
		public static int CompareVersions (string v1, string v2)
		{
			string[] a1 = v1.Split ('.');
			string[] a2 = v2.Split ('.');
			
			for (int n=0; n<a1.Length; n++) {
				if (n >= a2.Length)
					return -1;
				if (a1[n].Length == 0) {
					if (a2[n].Length != 0)
						return 1;
					continue;
				}
				try {
					int n1 = int.Parse (a1[n]);
					int n2 = int.Parse (a2[n]);
					if (n1 < n2)
						return 1;
					else if (n1 > n2)
						return -1;
				} catch {
					return 1;
				}
			}
			if (a2.Length > a1.Length)
				return 1;
			return 0;
		}
		
		public static Gdk.Pixbuf LoadIcon (string name, Gtk.IconSize size)
		{
			if (renderer == null)
				renderer = new Gtk.HBox ();
			Gdk.Pixbuf image = renderer.RenderIcon (name, size, null);
			if (image != null)
				return image;
			
			int w, h;
			Gtk.Icon.SizeLookup (size, out w, out h);
			try {
				return Gtk.IconTheme.Default.LoadIcon (name, w, 0);
			} catch {
				// Icon not in theme
				return MissingIcon;
			}
		}
	}
}
