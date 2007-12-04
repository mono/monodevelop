using System;
using System.Collections;

namespace Stetic.Editor {

	public class Enumeration: PropertyEditorCell
	{
		protected override string GetValueText ()
		{
			if (Value == null)
				return "";

			EnumDescriptor enm = Registry.LookupEnum (Property.PropertyType.FullName);
			EnumValue ev = enm [(Enum)Value];
			if (ev != null)
				return ev.Label;
			else
				return "";
		}
		
		protected override IPropertyEditor CreateEditor (Gdk.Rectangle cell_area, Gtk.StateType state)
		{
			return new EnumerationEditor ();
		}
	}
	
	public class EnumerationEditor : Gtk.HBox, IPropertyEditor {

		Gtk.EventBox ebox;
		Gtk.ComboBoxEntry combo;
		Gtk.Tooltips tips;
		EnumDescriptor enm;

		public EnumerationEditor () : base (false, 0)
		{
		}
		
		public void Initialize (PropertyDescriptor prop)
		{
			if (!prop.PropertyType.IsEnum)
				throw new ApplicationException ("Enumeration editor does not support editing values of type " + prop.PropertyType);
				
			ebox = new Gtk.EventBox ();
			ebox.Show ();
			PackStart (ebox, true, true, 0);

			combo = Gtk.ComboBoxEntry.NewText ();
			combo.Changed += combo_Changed;
			combo.Entry.IsEditable = false;
			combo.Entry.HasFrame = false;
			combo.Entry.HeightRequest = combo.SizeRequest ().Height;	// The combo does not set the entry to the correct size when it does not have a frame
			combo.Show ();
			ebox.Add (combo);

			tips = new Gtk.Tooltips ();

			enm = Registry.LookupEnum (prop.PropertyType.FullName);
			foreach (Enum value in enm.Values)
				combo.AppendText (enm[value].Label);
		}

		public void AttachObject (object obj)
		{
		}
		
		public override void Dispose ()
		{
			tips.Destroy ();
			base.Dispose ();
		}

		public object Value {
			get {
				return enm.Values[combo.Active];
			}
			set {
				int i = Array.IndexOf (enm.Values, (Enum)value);
				if (i != -1)
					combo.Active = i;
			}
		}

		public event EventHandler ValueChanged;

		void combo_Changed (object o, EventArgs args)
		{
			if (ValueChanged != null)
				ValueChanged (this, EventArgs.Empty);
			EnumValue value = enm[(Enum)Value];
			if (value != null)
				tips.SetTip (ebox, value.Description, value.Description);
			else
				tips.SetTip (ebox, null, null);
		}
	}
}
