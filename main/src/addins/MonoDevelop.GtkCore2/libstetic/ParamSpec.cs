using GLib;
using Gtk;
using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace Stetic {

	public class ParamSpec : IDisposable {
		IntPtr _obj;

		public ParamSpec (IntPtr raw)
		{
			Raw = raw;
		}

		~ParamSpec ()
		{
			Dispose ();
		}

		public void Dispose () 
		{
			Raw = IntPtr.Zero;
			GC.SuppressFinalize (this);
		}

		IntPtr Raw {
			get {
				return _obj;
			}
			set {
				if (_obj != IntPtr.Zero)
					g_param_spec_unref (_obj);
				_obj = value;
				if (_obj != IntPtr.Zero) {
					g_param_spec_ref (_obj);
					g_param_spec_sink (_obj);
				}
			}
		}

		public string Name {
			get {
				return GLib.Marshaller.Utf8PtrToString (g_param_spec_get_name (_obj));
			}
		}

		public string Nick {
			get {
				return GLib.Marshaller.Utf8PtrToString (g_param_spec_get_nick (_obj));
			}
		}

		public string Blurb {
			get {
				return GLib.Marshaller.Utf8PtrToString (g_param_spec_get_blurb (_obj));
			}
		}

/*		[DllImport("libsteticglue")]
		static extern bool stetic_param_spec_get_minimum (IntPtr pspec, ref GLib.Value value);

		public object Minimum {
			get {
				GLib.Value value = new GLib.Value ();

				if (stetic_param_spec_get_minimum (Raw, ref value))
					return value.Val;
				else
					return null;
			}
		}
*/
		public object Minimum {
			get {
				return null;
			}
		}

/*		[DllImport("libsteticglue")]
		static extern bool stetic_param_spec_get_maximum (IntPtr pspec, ref GLib.Value value);

		public object Maximum {
			get {
				GLib.Value value = new GLib.Value ();

				if (stetic_param_spec_get_maximum (Raw, ref value))
					return value.Val;
				else
					return null;
			}
		}
*/
		public object Maximum {
			get {
				return null;
			}
		}

		public bool IsDefaultValue (object value)
		{
			GLib.Value gvalue = new GLib.Value (value);
			return g_param_value_defaults  (Raw, ref gvalue);
		}

/*		[DllImport("libsteticglue")]
		static extern bool stetic_param_spec_get_default (IntPtr pspec, ref GLib.Value value);
		public object Default {
			get {
				GLib.Value value = new GLib.Value ();

				if (stetic_param_spec_get_default (Raw, ref value))
					return value.Val;
				else
					return null;
			}
		}
		
		[DllImport("libsteticglue")]
		static extern IntPtr stetic_param_spec_get_value_type (IntPtr obj);

		public IntPtr ValueType {
			get {
				return stetic_param_spec_get_value_type (_obj);
			}
		}
*/

/*		[DllImport("libsteticglue")]
		static extern bool stetic_param_spec_is_unichar (IntPtr pspec);

		public bool IsUnichar {
			get {
				return stetic_param_spec_is_unichar (_obj);
			}
		}
*/
		static Hashtable props = new Hashtable (), childProps = new Hashtable ();

		private class ParamSpecTypeHack : GLib.Object {
			private ParamSpecTypeHack () : base (IntPtr.Zero) {}

			static Hashtable classes = new Hashtable ();

			public static IntPtr LookupGTypeClass (System.Type t)
			{
				if (classes[t] == null) {
					GType gtype = GLib.Object.LookupGType (t);
					classes[t] = g_type_class_ref (gtype.Val);
				}

				return (IntPtr)classes[t];
			}
		}

		public static ParamSpec LookupObjectProperty (Type type, string name)
		{
			string key = type.FullName + ":" + name;
			if (props[key] != null)
				return (ParamSpec)props[key];

			IntPtr klass = ParamSpecTypeHack.LookupGTypeClass (type);
			if (klass == IntPtr.Zero)
				return null;

			IntPtr pspec_raw = g_object_class_find_property (klass, name);
			if (pspec_raw == IntPtr.Zero)
				return null;

			ParamSpec pspec = new ParamSpec (pspec_raw);
			props[key] = pspec;
			return pspec;
		}

		public static ParamSpec LookupChildProperty (Type type, string name)
		{
			string key = type.FullName + ":" + name;
			if (childProps[key] != null)
				return (ParamSpec)childProps[key];

			IntPtr klass = ParamSpecTypeHack.LookupGTypeClass (type);
			if (klass == IntPtr.Zero)
				return null;

			IntPtr pspec_raw = gtk_container_class_find_child_property (klass, name);
			if (pspec_raw == IntPtr.Zero)
				return null;

			ParamSpec pspec = new ParamSpec (pspec_raw);
			childProps[key] = pspec;
			return pspec;
		}

		[DllImport("libgobject-2.0-0.dll")]
		static extern void g_param_spec_ref (IntPtr obj);

		[DllImport("libgobject-2.0-0.dll")]
		static extern void g_param_spec_unref (IntPtr obj);

		[DllImport("libgobject-2.0-0.dll")]
		static extern void g_param_spec_sink (IntPtr obj);

		[DllImport("libgobject-2.0-0.dll")]
		static extern IntPtr g_param_spec_get_name (IntPtr obj);

		[DllImport("libgobject-2.0-0.dll")]
		static extern IntPtr g_param_spec_get_nick (IntPtr obj);

		[DllImport("libgobject-2.0-0.dll")]
		static extern IntPtr g_param_spec_get_blurb (IntPtr obj);

		[DllImport("libgobject-2.0-0.dll")]
		static extern bool g_param_value_defaults (IntPtr obj, ref GLib.Value value);

		[DllImport("libgobject-2.0-0.dll")]
		static extern IntPtr g_type_class_ref (IntPtr gtype);

		[DllImport("libgobject-2.0-0.dll")]
		static extern IntPtr g_object_class_find_property (IntPtr klass, string name);

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern IntPtr gtk_container_class_find_child_property (IntPtr klass, string name);
	}
}
