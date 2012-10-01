using System;
using System.Reflection;
using System.Collections;
using System.Runtime.InteropServices;
using System.Xml;
using Stetic.Wrapper;

namespace Stetic {

	public static class GladeUtils {

		public const string Glade20SystemId = "http://glade.gnome.org/glade-2.0.dtd";

		static Gdk.Atom gladeAtom;
		public static Gdk.Atom ApplicationXGladeAtom {
			get {
				if (gladeAtom == null)
					gladeAtom = Gdk.Atom.Intern ("application/x-glade", false);
				return gladeAtom;
			}
		}

		public static XmlDocument XslImportTransform (XmlDocument doc)
		{
/*			XmlDocumentType doctype = doc.DocumentType;
			if (doctype == null ||
			    doctype.Name != "glade-interface" ||
			    doctype.SystemId != Glade20SystemId)
				throw new GladeException ("Not a glade file according to doctype");
*/
			XmlReader reader = Registry.GladeImportXsl.Transform (doc, null, (XmlResolver)null);
			doc = new XmlDocument ();
			doc.PreserveWhitespace = true;
			doc.Load (reader);

			return doc;
		}

		public static XmlDocument XslExportTransform (XmlDocument doc)
		{
			XmlReader reader = Registry.GladeExportXsl.Transform (doc, null, (XmlResolver)null);
			doc = new XmlDocument ();
			doc.PreserveWhitespace = true;
			doc.Load (reader);

			XmlDocumentType doctype = doc.CreateDocumentType ("glade-interface", null, Glade20SystemId, null);
			doc.PrependChild (doctype);

			return doc;
		}
		
		public static XmlDocument Export (Gtk.Widget widget)
		{
			Stetic.Wrapper.Widget wrapper = Stetic.Wrapper.Widget.Lookup (widget);
			if (wrapper == null)
				return null;

			XmlDocument doc = new XmlDocument ();
			doc.PreserveWhitespace = true;

			XmlElement toplevel = doc.CreateElement ("glade-interface");
			doc.AppendChild (toplevel);

			// For toplevel widgets, glade just saves it as-is. For
			// non-toplevels, it puts the widget into a dummy GtkWindow,
			// but using the packing attributes of the widget's real
			// container (so as to preserve expand/fill settings and the
			// like).

			XmlElement elem;
			Stetic.Wrapper.Container parent = wrapper.ParentWrapper;
			ObjectWriter writer = new ObjectWriter (doc, FileFormat.Glade);

			if (parent == null) {
				elem = wrapper.Write (writer);
				if (elem == null)
					return null;
				if (!(widget is Gtk.Window)) {
					XmlElement window = doc.CreateElement ("widget");
					window.SetAttribute ("class", "GtkWindow");
					window.SetAttribute ("id", "glade-dummy-container");
					XmlElement child = doc.CreateElement ("child");
					window.AppendChild (child);
					child.AppendChild (elem);
					elem = window;
				}
			} else {
				elem = doc.CreateElement ("widget");
				// Set the class correctly (temporarily) so the XSL
				// transforms will work correctly.
				ClassDescriptor klass = parent.ClassDescriptor;
				elem.SetAttribute ("class", klass.CName);
				elem.AppendChild (parent.WriteContainerChild (writer, wrapper));
			}
			toplevel.AppendChild (elem);

			doc = XslExportTransform (doc);

			if (parent != null) {
				elem = (XmlElement)doc.SelectSingleNode ("glade-interface/widget");
				elem.SetAttribute ("class", "GtkWindow");
				elem.SetAttribute ("id", "glade-dummy-container");
			}
			return doc;
		}

		public static Stetic.Wrapper.Widget Import (IProject project, XmlDocument doc)
		{
			try {
				doc = XslImportTransform (doc);
			} catch {
				return null;
			}

			ObjectReader reader = new ObjectReader (project, FileFormat.Glade);
			
			XmlElement elem = (XmlElement)doc.SelectSingleNode ("glade-interface/widget");
			if (elem.GetAttribute ("class") != "GtkWindow" ||
			    elem.GetAttribute ("id") != "glade-dummy-container") {
				// Creating a new toplevel
				Stetic.Wrapper.Widget toplevel = (Stetic.Wrapper.Widget)
					Stetic.ObjectWrapper.ReadObject (reader, elem);
				if (toplevel != null) {
					project.AddWindow ((Gtk.Window)toplevel.Wrapped);
				}
				return toplevel;
			}

			return (Stetic.Wrapper.Widget)
				Stetic.ObjectWrapper.ReadObject (reader, (XmlElement)elem.SelectSingleNode ("child/widget"));
		}
		
		public static void Copy (Gtk.Widget widget, Gtk.SelectionData seldata, bool copyAsText)
		{
			XmlDocument doc = Export (widget);
			if (doc == null)
				return;

			if (copyAsText)
				seldata.Text = doc.OuterXml;
			else
				seldata.Set (ApplicationXGladeAtom, 8, System.Text.Encoding.UTF8.GetBytes (doc.OuterXml));
		}

		public static Stetic.Wrapper.Widget Paste (IProject project, Gtk.SelectionData seldata)
		{
			if (seldata.Type != ApplicationXGladeAtom)
				return null;
			string data = System.Text.Encoding.UTF8.GetString (seldata.Data);

			XmlDocument doc = new XmlDocument ();
			doc.PreserveWhitespace = true;
			try {
				doc.LoadXml (data);
			} catch {
				return null;
			}
			
			return Import (project, doc);
		}

		static object GetProperty (XmlElement elem, string selector, object defaultValue, bool extract)
		{
			XmlElement prop = (XmlElement)elem.SelectSingleNode (selector);
			if (prop == null)
				return defaultValue;
			if (extract)
				prop.ParentNode.RemoveChild (prop);
			return ParseProperty (null, defaultValue.GetType (), prop.InnerText).Val;
		}

		public static object GetProperty (XmlElement elem, string name, object defaultValue)
		{
			return GetProperty (elem, "./property[@name='" + name + "']", defaultValue, false);
		}

		public static object ExtractProperty (XmlElement elem, string name, object defaultValue)
		{
			return GetProperty (elem, "./property[@name='" + name + "']", defaultValue, true);
		}

		public static object GetChildProperty (XmlElement elem, string name, object defaultValue)
		{
			return GetProperty (elem, "./packing/property[@name='" + name + "']", defaultValue, false);
		}

		public static object ExtractChildProperty (XmlElement elem, string name, object defaultValue)
		{
			return GetProperty (elem, "./packing/property[@name='" + name + "']", defaultValue, true);
		}
		
		public static void RenameProperty (XmlElement elem, string name, string newName)
		{
			XmlElement prop = (XmlElement)elem.SelectSingleNode ("./property[@name='" + name + "']");
			if (prop != null)
				prop.SetAttribute ("name", newName);
		}
		
		public static void SetProperty (XmlElement elem, string name, string value)
		{
			XmlElement prop_elem = elem.OwnerDocument.CreateElement ("property");
			prop_elem.SetAttribute ("name", name);
			prop_elem.InnerText = value;
			elem.AppendChild (prop_elem);
		}

		public static void SetChildProperty (XmlElement elem, string name, string value)
		{
			XmlElement packing_elem = elem["packing"];
			if (packing_elem == null) {
				packing_elem = elem.OwnerDocument.CreateElement ("packing");
				elem.AppendChild (packing_elem);
			}
			SetProperty (packing_elem, name, value);
		}

		static GLib.Value ParseBasicType (GLib.TypeFundamentals type, string strval)
		{
			switch (type) {
			case GLib.TypeFundamentals.TypeChar:
				return new GLib.Value (SByte.Parse (strval));
			case GLib.TypeFundamentals.TypeUChar:
				return new GLib.Value (Byte.Parse (strval));
			case GLib.TypeFundamentals.TypeBoolean:
				return new GLib.Value (strval == "True");
			case GLib.TypeFundamentals.TypeInt:
				return new GLib.Value (Int32.Parse (strval));
			case GLib.TypeFundamentals.TypeUInt:
				return new GLib.Value (UInt32.Parse (strval));
			case GLib.TypeFundamentals.TypeInt64:
				return new GLib.Value (Int64.Parse (strval));
			case GLib.TypeFundamentals.TypeUInt64:
				return new GLib.Value (UInt64.Parse (strval));
			case GLib.TypeFundamentals.TypeFloat:
				return new GLib.Value (Single.Parse (strval, System.Globalization.CultureInfo.InvariantCulture));
			case GLib.TypeFundamentals.TypeDouble:
				return new GLib.Value (Double.Parse (strval, System.Globalization.CultureInfo.InvariantCulture));
			case GLib.TypeFundamentals.TypeString:
				return new GLib.Value (strval);
			default:
				throw new GladeException ("Could not parse");
			}
		}

		static GLib.Value ParseEnum (IntPtr gtype, string strval)
		{
			IntPtr enum_class = g_type_class_ref (gtype);
			try {
				IntPtr enum_value = g_enum_get_value_by_name (enum_class, strval);
				if (enum_value == IntPtr.Zero)
					throw new GladeException ("Could not parse");

				int eval = Marshal.ReadInt32 (enum_value);
				return new GLib.Value (Enum.ToObject (GLib.GType.LookupType (gtype), eval));
			} finally {
				g_type_class_unref (enum_class);
			}
		}

		static GLib.Value ParseFlags (IntPtr gtype, string strval)
		{
			IntPtr flags_class = g_type_class_ref (gtype);
			uint fval = 0;

			try {
				foreach (string flag in strval.Split ('|')) {
					if (flag == "")
						continue;
					IntPtr flags_value = g_flags_get_value_by_name (flags_class, flag);
					if (flags_value == IntPtr.Zero)
						throw new GladeException ("Could not parse");

					int bits = Marshal.ReadInt32 (flags_value);
					fval |= (uint)bits;
				}

				return new GLib.Value (Enum.ToObject (GLib.GType.LookupType (gtype), fval));
			} finally {
				g_type_class_unref (flags_class);
			}
		}

		static GLib.Value ParseAdjustment (string strval)
		{
			string[] vals = strval.Split (' ');
			double deflt, min, max, step, page_inc, page_size;

			deflt = Double.Parse (vals[0], System.Globalization.CultureInfo.InvariantCulture);
			min = Double.Parse (vals[1], System.Globalization.CultureInfo.InvariantCulture);
			max = Double.Parse (vals[2], System.Globalization.CultureInfo.InvariantCulture);
			step = Double.Parse (vals[3], System.Globalization.CultureInfo.InvariantCulture);
			page_inc = Double.Parse (vals[4], System.Globalization.CultureInfo.InvariantCulture);
			page_size = Double.Parse (vals[5], System.Globalization.CultureInfo.InvariantCulture);
			return new GLib.Value (new Gtk.Adjustment (deflt, min, max, step, page_inc, page_size));
		}

	/*	static GLib.Value ParseUnichar (string strval)
		{
			return new GLib.Value (strval.Length == 1 ? (uint)strval[0] : 0U);
		}*/

		static GLib.Value ParseProperty (ParamSpec pspec, Type propType, string strval)
		{
			IntPtr gtype;
			if (propType != null)
				gtype = ((GLib.GType)propType).Val;
/*
			FIXME: ValueType is not supported right now

			else if (pspec != null)
				gtype = pspec.ValueType;
*/
			else
				throw new GladeException ("Bad type");

			GLib.TypeFundamentals typef = (GLib.TypeFundamentals)(int)g_type_fundamental (gtype);

			if (gtype == Gtk.Adjustment.GType.Val)
				return ParseAdjustment (strval);
			else if (typef == GLib.TypeFundamentals.TypeEnum)
				return ParseEnum (gtype, strval);
			else if (typef == GLib.TypeFundamentals.TypeFlags)
				return ParseFlags (gtype, strval);
// FIXME: Enable when ParamSpec.IsUnichar is implemented.
//			else if (pspec != null && pspec.IsUnichar)
//				return ParseUnichar (strval);
			else
				return ParseBasicType (typef, strval);
		}

		static PropertyInfo FindClrProperty (Type type, string name, bool childprop)
		{
			if (childprop) {
				Type[] types = type.GetNestedTypes ();
				foreach (Type t in types) {
					if (typeof(Gtk.Container.ContainerChild).IsAssignableFrom (t)) {
						type = t;
						break;
					}
				}
				foreach (PropertyInfo pi in type.GetProperties ()) {
					Gtk.ChildPropertyAttribute at = (Gtk.ChildPropertyAttribute) Attribute.GetCustomAttribute (pi, typeof(Gtk.ChildPropertyAttribute), false);
					if (at != null && at.Name == name)
						return pi;
				}
				if (typeof(GLib.Object).IsAssignableFrom (type.BaseType))
					return FindClrProperty (type.BaseType, name, true);
			}
			
			foreach (PropertyInfo pi in type.GetProperties ()) {
				GLib.PropertyAttribute at = (GLib.PropertyAttribute) Attribute.GetCustomAttribute (pi, typeof(GLib.PropertyAttribute), false);
				if (at != null && at.Name == name)
					return pi;
			}
			return null;
		}
			
		static GLib.Value ParseProperty (Type type, bool childprop, string name, string strval)
		{
			ParamSpec pspec;

			// FIXME: this can be removed when GParamSpec supports ValueType.			
			PropertyInfo pi = FindClrProperty (type, name, childprop);
			if (pi == null)
				throw new GladeException ("Unknown property", type.ToString (), childprop, name, strval);

			if (childprop)
				pspec = ParamSpec.LookupChildProperty (type, name);
			else
				pspec = ParamSpec.LookupObjectProperty (type, name);
			if (pspec == null)
				throw new GladeException ("Unknown property", type.ToString (), childprop, name, strval);

			try {
				return ParseProperty (pspec, pi.PropertyType, strval);
			} catch {
				throw new GladeException ("Could not parse property", type.ToString (), childprop, name, strval);
			}
		}

		static void ParseProperties (Type type, bool childprops, IEnumerable props,
					     out string[] propNames, out GLib.Value[] propVals)
		{
			ArrayList names = new ArrayList ();
			ArrayList values = new ArrayList ();

			foreach (XmlElement prop in props) {
				string name = prop.GetAttribute ("name").Replace ("_","-");
				string strval = prop.InnerText;

				// Skip translation context
				if (prop.GetAttribute ("context") == "yes" &&
				    strval.IndexOf ('|') != -1)
					strval = strval.Substring (strval.IndexOf ('|') + 1);

				GLib.Value value;
				try {
					value = ParseProperty (type, childprops, name, strval);
					names.Add (name);
					values.Add (value);
				} catch (GladeException ge) {
					Console.Error.WriteLine (ge.Message);
				}
			}

			propNames = (string[])names.ToArray (typeof (string));
			propVals = (GLib.Value[])values.ToArray (typeof (GLib.Value));
		}

		static void ExtractProperties (TypedClassDescriptor klass, XmlElement elem,
					       out Hashtable rawProps, out Hashtable overrideProps)
		{
			rawProps = new Hashtable ();
			overrideProps = new Hashtable ();
			foreach (ItemGroup group in klass.ItemGroups) {
				foreach (ItemDescriptor item in group) {
					TypedPropertyDescriptor prop = item as TypedPropertyDescriptor;
					if (prop == null)
						continue;
					prop = prop.GladeProperty;
					if (prop.GladeName == null)
						continue;

					XmlNode prop_node = elem.SelectSingleNode ("property[@name='" + prop.GladeName + "']");
					if (prop_node == null)
						continue;

					if (prop.GladeOverride)
						overrideProps[prop] = prop_node;
					else
						rawProps[prop] = prop_node;
				}
			}
		}

		static void ReadSignals (TypedClassDescriptor klass, ObjectWrapper wrapper, XmlElement elem)
		{
			Stetic.Wrapper.Widget ob = wrapper as Stetic.Wrapper.Widget;
			if (ob == null) return;
			
			foreach (ItemGroup group in klass.SignalGroups) {
				foreach (TypedSignalDescriptor signal in group) {
					if (signal.GladeName == null)
						continue;

					XmlElement signal_elem = elem.SelectSingleNode ("signal[@name='" + signal.GladeName + "']") as XmlElement;
					if (signal_elem == null)
						continue;
					
					string handler = signal_elem.GetAttribute ("handler");
					bool after = signal_elem.GetAttribute ("after") == "yes";
					ob.Signals.Add (new Signal (signal, handler, after));
				}
			}
		}
		
		static public void ImportWidget (ObjectWrapper wrapper, XmlElement elem)
		{
			string className = elem.GetAttribute ("class");
			if (className == null)
				throw new GladeException ("<widget> node with no class name");

			ClassDescriptor klassBase = Registry.LookupClassByCName (className);
			if (klassBase == null)
				throw new GladeException ("No stetic ClassDescriptor for " + className);
				
			TypedClassDescriptor klass = klassBase as TypedClassDescriptor;
			if (klass == null)
				throw new GladeException ("The widget class " + className + " is not supported by Glade");
			
			ReadSignals (klass, wrapper, elem);

			Hashtable rawProps, overrideProps;
			ExtractProperties (klass, elem, out rawProps, out overrideProps);

			string[] propNames;
			GLib.Value[] propVals;
			ParseProperties (klass.WrappedType, false, rawProps.Values,
					 out propNames, out propVals);

			Gtk.Widget widget;

			if (wrapper.Wrapped == null) {
				if (className == "GtkWindow" || className == "GtkDialog") {
					widget = (Gtk.Widget) klass.CreateInstance (wrapper.Project);
					ObjectWrapper.Bind (wrapper.Project, klass, wrapper, widget, true);
					SetProperties (klass, widget, propNames, propVals);
				} else {
					IntPtr raw = gtksharp_object_newv (klass.GType.Val, propNames.Length, propNames, propVals);
					if (raw == IntPtr.Zero)
						throw new GladeException ("Could not create widget", className);
					widget = (Gtk.Widget)GLib.Object.GetObject (raw, true);
					if (widget == null) {
						gtk_object_sink (raw);
						throw new GladeException ("Could not create gtk# wrapper", className);
					}
					ObjectWrapper.Bind (wrapper.Project, klass, wrapper, widget, true);
				}
			} else {
				widget = (Gtk.Widget)wrapper.Wrapped;
				for (int i = 0; i < propNames.Length; i++)
					g_object_set_property (widget.Handle, propNames[i], ref propVals[i]);
			}
			MarkTranslatables (widget, rawProps);

			widget.Name = elem.GetAttribute ("id");

			SetOverrideProperties (wrapper, overrideProps);
			MarkTranslatables (widget, overrideProps);
		}

		static void SetProperties (TypedClassDescriptor klass, Gtk.Widget widget, string[] propNames, GLib.Value[] propVals)
		{
			for (int n=0; n<propNames.Length; n++) {
				foreach (ItemGroup grp in klass.ItemGroups) {
					foreach (ItemDescriptor it in grp) {
						if (it is TypedPropertyDescriptor) {
							TypedPropertyDescriptor prop = (TypedPropertyDescriptor)it;
							if (prop.GladeName == propNames[n]) {
								prop.SetValue (widget, propVals[n].Val);
							}
						}
					}
				}
			}
		}

		
		static void SetOverrideProperties (ObjectWrapper wrapper, Hashtable overrideProps)
		{
			foreach (TypedPropertyDescriptor prop in overrideProps.Keys) {
				XmlElement prop_elem = overrideProps[prop] as XmlElement;

				try {
					GLib.Value value = ParseProperty (prop.ParamSpec, prop.PropertyType, prop_elem.InnerText);
					prop.SetValue (wrapper.Wrapped, value.Val);
				} catch {
					throw new GladeException ("Could not parse property", wrapper.GetType ().ToString (), wrapper is Stetic.Wrapper.Container.ContainerChild, prop.GladeName, prop_elem.InnerText);
				}
			}
		}

		static void MarkTranslatables (object obj, Hashtable props)
		{
			foreach (PropertyDescriptor prop in props.Keys) {
				if (!prop.Translatable)
					continue;

				XmlElement prop_elem = props[prop] as XmlElement;
				if (prop_elem.GetAttribute ("translatable") != "yes") {
					prop.SetTranslated (obj, false);
					continue;
				}

				prop.SetTranslated (obj, true);
				if (prop_elem.GetAttribute ("context") == "yes") {
					string strval = prop_elem.InnerText;
					int bar = strval.IndexOf ('|');
					if (bar != -1)
						prop.SetTranslationContext (obj, strval.Substring (0, bar));
				}

				if (prop_elem.HasAttribute ("comments"))
					prop.SetTranslationComment (obj, prop_elem.GetAttribute ("comments"));
			}
		}

		static public void SetPacking (Stetic.Wrapper.Container.ContainerChild wrapper, XmlElement child_elem)
		{
			XmlElement packing = child_elem["packing"];
			if (packing == null)
				return;

			Gtk.Container.ContainerChild cc = wrapper.Wrapped as Gtk.Container.ContainerChild;

			TypedClassDescriptor klass = wrapper.ClassDescriptor as TypedClassDescriptor;
			if (klass == null)
				throw new GladeException ("The widget class " + cc.GetType () + " is not supported by Glade");

			Hashtable rawProps, overrideProps;
			ExtractProperties (klass, packing, out rawProps, out overrideProps);

			string[] propNames;
			GLib.Value[] propVals;
			ParseProperties (cc.Parent.GetType (), true, rawProps.Values,
					 out propNames, out propVals);

			for (int i = 0; i < propNames.Length; i++)
				cc.Parent.ChildSetProperty (cc.Child, propNames[i], propVals[i]);
			MarkTranslatables (cc, rawProps);

			SetOverrideProperties (wrapper, overrideProps);
			MarkTranslatables (cc, overrideProps);
		}
		
		internal static XmlElement CreatePacking (XmlDocument doc, Stetic.Wrapper.Container.ContainerChild childwrapper)
		{
			XmlElement packing_elem = doc.CreateElement ("packing");
			GetProps (childwrapper, packing_elem);
			return packing_elem;
		}
		
		static string PropToString (ObjectWrapper wrapper, TypedPropertyDescriptor prop)
		{
			object value;

			if (!prop.GladeOverride) {
				Stetic.Wrapper.Container.ContainerChild ccwrap = wrapper as Stetic.Wrapper.Container.ContainerChild;
				GLib.Value gval;

				if (ccwrap != null) {
					Gtk.Container.ContainerChild cc = (Gtk.Container.ContainerChild)ccwrap.Wrapped;
					gval = new GLib.Value ((GLib.GType) prop.PropertyType);
					gtk_container_child_get_property (cc.Parent.Handle, cc.Child.Handle, prop.GladeName, ref gval);
				} else {
					Gtk.Widget widget = wrapper.Wrapped as Gtk.Widget;
					gval = new GLib.Value (widget, prop.GladeName);
					g_object_get_property (widget.Handle, prop.GladeName, ref gval);
				}
				value = gval.Val;
			} else
				value = prop.GetValue (wrapper.Wrapped);
			if (value == null)
				return null;

			// If the property has its default value, we don't need to write it
			if (prop.HasDefault && prop.ParamSpec.IsDefaultValue (value))
				return null;

			if (value is Gtk.Adjustment) {
				Gtk.Adjustment adj = value as Gtk.Adjustment;
				return String.Format ("{0:G} {1:G} {2:G} {3:G} {4:G} {5:G}",
						      adj.Value, adj.Lower, adj.Upper,
						      adj.StepIncrement, adj.PageIncrement,
						      adj.PageSize);
			} else if (value is Enum && prop.ParamSpec != null) {
				IntPtr klass = g_type_class_ref (((GLib.GType)prop.PropertyType).Val);

				if (prop.PropertyType.IsDefined (typeof (FlagsAttribute), false)) {
					System.Text.StringBuilder sb = new System.Text.StringBuilder ();
					uint val = (uint)System.Convert.ChangeType (value, typeof (uint));

					while (val != 0) {
						IntPtr flags_value = g_flags_get_first_value (klass, val);
						if (flags_value == IntPtr.Zero)
							break;
						IntPtr fval = Marshal.ReadIntPtr (flags_value);
						val &= ~(uint)fval;

						IntPtr name = Marshal.ReadIntPtr (flags_value, Marshal.SizeOf (typeof (IntPtr)));
						if (name != IntPtr.Zero) {
							if (sb.Length != 0)
								sb.Append ('|');
							sb.Append (GLib.Marshaller.Utf8PtrToString (name));
						}
					}

					g_type_class_unref (klass);
					return sb.ToString ();
				} else {
					int val = (int)System.Convert.ChangeType (value, typeof (int));
					IntPtr enum_value = g_enum_get_value (klass, val);
					g_type_class_unref (klass);

					IntPtr name = Marshal.ReadIntPtr (enum_value, Marshal.SizeOf (typeof (IntPtr)));
					return GLib.Marshaller.Utf8PtrToString (name);
				}
			} else if (value is bool)
				return (bool)value ? "True" : "False";
			else
				return value.ToString ();
		}

		static public XmlElement ExportWidget (ObjectWrapper wrapper, XmlDocument doc)
		{
			XmlElement  elem = doc.CreateElement ("widget");
			elem.SetAttribute ("class", wrapper.ClassDescriptor.CName);
			elem.SetAttribute ("id", ((Gtk.Widget)wrapper.Wrapped).Name);

			GetProps (wrapper, elem);
			GetSignals (wrapper, elem);
			return elem;
		}

		static public void GetProps (ObjectWrapper wrapper, XmlElement parent_elem)
		{
			ClassDescriptor klass = wrapper.ClassDescriptor;

			foreach (ItemGroup group in klass.ItemGroups) {
				foreach (ItemDescriptor item in group) {
					TypedPropertyDescriptor prop = item as TypedPropertyDescriptor;
					if (prop == null)
						continue;
					prop = prop.GladeProperty;
					if (prop.GladeName == null)
						continue;
					if (!prop.VisibleFor (wrapper.Wrapped))
						continue;

					string val = PropToString (wrapper, prop);
					if (val == null)
						continue;

					XmlElement prop_elem = parent_elem.OwnerDocument.CreateElement ("property");
					prop_elem.SetAttribute ("name", prop.GladeName);
					if (val.Length > 0)
						prop_elem.InnerText = val;

					if (prop.Translatable && prop.IsTranslated (wrapper.Wrapped)) {
						prop_elem.SetAttribute ("translatable", "yes");
						if (prop.TranslationContext (wrapper.Wrapped) != null) {
							prop_elem.SetAttribute ("context", "yes");
							prop_elem.InnerText = prop.TranslationContext (wrapper.Wrapped) + "|" + prop_elem.InnerText;
						}
						if (prop.TranslationComment (wrapper.Wrapped) != null)
							prop_elem.SetAttribute ("comments", prop.TranslationComment (wrapper.Wrapped));
					}

					parent_elem.AppendChild (prop_elem);
				}
			}
		}

		static public void GetSignals (ObjectWrapper wrapper, XmlElement parent_elem)
		{
			Stetic.Wrapper.Widget ob = wrapper as Stetic.Wrapper.Widget;
			if (ob == null) return;
			
			foreach (Signal signal in ob.Signals) {
				if (((TypedSignalDescriptor)signal.SignalDescriptor).GladeName == null)
					continue;
				if (!signal.SignalDescriptor.VisibleFor (wrapper.Wrapped))
					continue;

				XmlElement signal_elem = parent_elem.OwnerDocument.CreateElement ("signal");
				signal_elem.SetAttribute ("name", ((TypedSignalDescriptor)signal.SignalDescriptor).GladeName);
				signal_elem.SetAttribute ("handler", signal.Handler);
				if (signal.After)
					signal_elem.SetAttribute ("after", "yes");
				parent_elem.AppendChild (signal_elem);
			}
		}

		[DllImport ("libgobject-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr g_type_fundamental (IntPtr gtype);

		[DllImport ("libgobject-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr g_type_class_ref (IntPtr gtype);

		[DllImport ("libgobject-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr g_type_class_unref (IntPtr klass);

		[DllImport ("glibsharpglue-2", CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr gtksharp_object_newv (IntPtr gtype, int n_params, string[] names, GLib.Value[] vals);

		[DllImport ("libgtk-win32-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern void gtk_object_sink (IntPtr raw);

		[DllImport ("libgobject-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern void g_object_get_property (IntPtr obj, string name, ref GLib.Value val);

		[DllImport ("libgobject-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern void g_object_set_property (IntPtr obj, string name, ref GLib.Value val);

		[DllImport ("libgtk-win32-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern void gtk_container_child_get_property (IntPtr parent, IntPtr child, string name, ref GLib.Value val);

		[DllImport ("libgobject-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr g_enum_get_value_by_name (IntPtr enum_class, string name);

		[DllImport ("libgobject-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr g_enum_get_value (IntPtr enum_class, int val);

		[DllImport ("libgobject-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr g_flags_get_value_by_name (IntPtr flags_class, string nick);

		[DllImport ("libgobject-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr g_flags_get_first_value (IntPtr flags_class, uint val);
	}
}
