using System;
using System.Collections;
using System.Reflection;
using System.Xml;

namespace Stetic {

	public class CommandDescriptor : ItemDescriptor {

		string name, checkName, label, description, icon;

		const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

		public CommandDescriptor (XmlElement elem, ItemGroup group, ClassDescriptor klass) : base (elem, group, klass)
		{
			name = elem.GetAttribute ("name");
			label = elem.GetAttribute ("label");
			description = elem.GetAttribute ("description");
			checkName = elem.GetAttribute ("check");
			icon = elem.GetAttribute ("icon");
		}

		public override string Name {
			get {
				return name;
			}
		}

		public string Label {
			get {
				return label;
			}
		}

		public string Description {
			get {
				return description;
			}
		}
		
		public bool IsToggleCommand (object obj)
		{
			object target;
			return (FindBoolProperty (obj, out target) != null);
		}
		
		public bool IsToogled (object obj)
		{
			object target;
			PropertyInfo prop = FindBoolProperty (obj, out target);
			return prop != null && (bool) prop.GetValue (target, null);
		}
		
		PropertyInfo FindBoolProperty (object obj, out object target)
		{
			PropertyInfo prop = obj.GetType().GetProperty (name, flags);
			if (prop != null && prop.PropertyType == typeof(bool)) {
				target = obj;
				return prop;
			}
			
			ObjectWrapper wrap = ObjectWrapper.Lookup (obj);
			if (wrap != null) {
				prop = wrap.GetType().GetProperty (name, flags);
				if (prop != null && prop.PropertyType == typeof(bool)) {
					target = wrap;
					return prop;
				}
			}
			target = null;
			return null;
		}
		
		public Gtk.Image GetImage ()
		{
			if (icon == null || icon.Length == 0)
				return null;
			if (icon.StartsWith ("res:")) {
				System.IO.Stream s = this.ClassDescriptor.Library.GetResource (icon.Substring (4));
				if (s == null)
					return null;
				using (s) {
					return new Gtk.Image (new Gdk.Pixbuf (s));
				}
			} else {
				return new Gtk.Image (icon, Gtk.IconSize.Menu);
			}
		}

		public bool Enabled (object obj)
		{
			if (checkName == "")
				return EnabledFor (obj);
			else
				return (bool) InvokeMethod (ObjectWrapper.Lookup (obj), checkName, null, false);
		}

		public bool Enabled (object obj, Gtk.Widget context)
		{
			if (checkName == "")
				return EnabledFor (obj);

			ObjectWrapper wrapper = ObjectWrapper.Lookup (obj);
			return (bool) InvokeMethod (wrapper, checkName, context, true);
		}

		public void Run (object obj)
		{
			ObjectWrapper ww = ObjectWrapper.Lookup (obj);
			using (ww.UndoManager.AtomicChange) {
				InvokeMethod (ww, name, null, false);
			}
		}

		public void Run (object obj, Gtk.Widget context)
		{
			ObjectWrapper ww = ObjectWrapper.Lookup (obj);
			using (ww.UndoManager.AtomicChange) {
				InvokeMethod (ww, name, context, true);
			}
		}
		
		object InvokeMethod (object target, string name, object context, bool withContext)
		{
			object ptarget;
			PropertyInfo prop = FindBoolProperty (target, out ptarget);
			if (prop != null) {
				prop.SetValue (ptarget, !(bool)prop.GetValue (ptarget, null), null);
				return null;
			}
			
			if (withContext) {
				MethodInfo metc = target.GetType().GetMethod (name, flags, null, new Type[] {typeof(Gtk.Widget)}, null);
				if (metc != null)
					return metc.Invoke (target, new object[] { context });
			}
			
			MethodInfo met = target.GetType().GetMethod (name, flags, null, Type.EmptyTypes, null);
			if (met != null)
				return met.Invoke (target, new object[0]);
			
			throw new ArgumentException ("Invalid command or checker name. Method '" + name +"' not found in class '" + target.GetType() + "'");
		}
	}
}
