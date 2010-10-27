using System;
using System.Collections;
using System.Reflection;

namespace Stetic.Editor 
{
	public class ResponseId: PropertyEditorCell
	{
		protected override string GetValueText ()
		{
			if (Value == null)
				return "";

			int val = (int) Value;
			EnumDescriptor enm = Registry.LookupEnum ("Gtk.ResponseType");
			foreach (Enum value in enm.Values) {
				if (Convert.ToInt32 (enm[value].Value) == val) {
					return enm[value].Label;
				}
			}
			return val.ToString ();
		}
		
		protected override IPropertyEditor CreateEditor (Gdk.Rectangle cell_area, Gtk.StateType state)
		{
			return new ResponseIdEditor ();
		}
	}

	public class ResponseIdEditor : Gtk.HBox, IPropertyEditor {

		Gtk.ComboBoxEntry combo;
		Gtk.Entry entry;
		EnumDescriptor enm;
		ArrayList values;

		public ResponseIdEditor ()
		{
			combo = Gtk.ComboBoxEntry.NewText ();
			combo.Changed += combo_Changed;
			combo.Show ();
			PackStart (combo, true, true, 0);

			entry = combo.Child as Gtk.Entry;
			entry.Changed += entry_Changed;

			enm = Registry.LookupEnum ("Gtk.ResponseType");
			values = new ArrayList ();
			foreach (Enum value in enm.Values) {
				if (enm[value].Label != "") {
					combo.AppendText (enm[value].Label);
					values.Add (Convert.ToInt32 (enm[value].Value));
				}
			}
		}
		
		public void Initialize (PropertyDescriptor prop)
		{
			if (prop.PropertyType != typeof(int))
				throw new ApplicationException ("ResponseId editor does not support editing values of type " + prop.PropertyType);
		}
		
		public void AttachObject (object ob)
		{
		}

		public object Value {
			get {
				if (combo.Active != -1)
					return (int)values[combo.Active];
				else {
					try {
						return Int32.Parse (entry.Text);
					} catch {
						return 0;
					}
				}
			}
			set {
				combo.Active = values.IndexOf ((int)value);
				if (combo.Active == -1)
					entry.Text = value.ToString ();
			}
		}

		public event EventHandler ValueChanged;

		void combo_Changed (object o, EventArgs args)
		{
			if (ValueChanged != null)
				ValueChanged (this, EventArgs.Empty);
		}

		void entry_Changed (object o, EventArgs args)
		{
			if (combo.Active == -1 && ValueChanged != null)
				ValueChanged (this, EventArgs.Empty);
		}
	}
}
