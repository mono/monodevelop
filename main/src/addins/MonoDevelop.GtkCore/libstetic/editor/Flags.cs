using Gtk;
using System;
using System.Collections;

namespace Stetic.Editor {

	public class Flags: PropertyEditorCell
	{
		protected override string GetValueText ()
		{
			if (Value == null)
				return "";

			uint value = (uint)(int)Value;
			EnumDescriptor enm = Registry.LookupEnum (Property.PropertyType.FullName);
			string txt = "";
			foreach (Enum val in enm.Values) {
				EnumValue eval = enm[val];
				if (eval.Label == "")
					continue;
				
				if ((value & (uint) Convert.ToInt32 (eval.Value)) != 0) {
					if (txt.Length > 0) txt += ", ";
					txt += eval.Label;
				}
			}
			return txt;
		}
		
		protected override IPropertyEditor CreateEditor (Gdk.Rectangle cell_area, Gtk.StateType state)
		{
			return new FlagsEditor ();
		}
	}
	
	public class FlagsEditor : Gtk.HBox, IPropertyEditor {

		EnumDescriptor enm;
		Hashtable flags;
		Gtk.Tooltips tips;
		Gtk.Entry flagsLabel;
		string property;

		public FlagsEditor ()
		{
		}
		
		public void Initialize (PropertyDescriptor prop)
		{
			if (!prop.PropertyType.IsEnum)
				throw new ApplicationException ("Flags editor does not support editing values of type " + prop.PropertyType);
				
			property = prop.Label;
			Spacing = 3;

			// For small enums, the editor is a list of checkboxes inside a frame
			// For large enums (>5), use a selector dialog.

			enm = Registry.LookupEnum (prop.PropertyType.FullName);
			
			if (enm.Values.Length < 6) 
			{
				Gtk.VBox vbox = new Gtk.VBox (true, 3);

				tips = new Gtk.Tooltips ();
				flags = new Hashtable ();

				foreach (Enum value in enm.Values) {
					EnumValue eval = enm[value];
					if (eval.Label == "")
						continue;

					Gtk.CheckButton check = new Gtk.CheckButton (eval.Label);
					tips.SetTip (check, eval.Description, eval.Description);
					uint uintVal = (uint) Convert.ToInt32 (eval.Value);
					flags[check] = uintVal;
					flags[uintVal] = check;
					
					check.Toggled += FlagToggled;
					vbox.PackStart (check, false, false, 0);
				}

				Gtk.Frame frame = new Gtk.Frame ();
				frame.Add (vbox);
				frame.ShowAll ();
				PackStart (frame, true, true, 0);
			} 
			else 
			{
				flagsLabel = new Gtk.Entry ();
				flagsLabel.IsEditable = false;
				flagsLabel.HasFrame = false;
				flagsLabel.ShowAll ();
				PackStart (flagsLabel, true, true, 0);
				
				Gtk.Button but = new Gtk.Button ("...");
				but.Clicked += OnSelectFlags;
				but.ShowAll ();
				PackStart (but, false, false, 0);
			}
		}
		
		public void AttachObject (object ob)
		{
		}

		public override void Dispose ()
		{
			tips.Destroy ();
			base.Dispose ();
		}

		public object Value {
			get {
				return Enum.ToObject (enm.EnumType, UIntValue);
			}
			set {
				uint newVal = (uint)(int)value;
				if (flagsLabel != null) {
					string txt = "";
					foreach (Enum val in enm.Values) {
						EnumValue eval = enm[val];
						if (eval.Label == "")
							continue;
						
						if ((newVal & (uint) Convert.ToInt32 (eval.Value)) != 0) {
							if (txt.Length > 0) txt += ", ";
							txt += eval.Label;
						}
					}
					flagsLabel.Text = txt;
					UIntValue = newVal;
				}
				else {
					for (uint i = 1; i <= uintValue || i <= newVal; i = i << 1) {
						if ((uintValue & i) != (newVal & i)) {
							Gtk.CheckButton check = (Gtk.CheckButton)flags[i];
							if (check != null)
								check.Active = !check.Active;
						}
					}
				}
			}
		}

		public event EventHandler ValueChanged;

		uint uintValue;
		uint UIntValue {
			get {
				return uintValue;
			}
			set {
				if (uintValue != value) {
					uintValue = value;
					if (ValueChanged != null)
						ValueChanged (this, EventArgs.Empty);
				}
			}
		}

		void FlagToggled (object o, EventArgs args)
		{
			Gtk.CheckButton check = (Gtk.CheckButton)o;
			uint val = (uint)flags[o];

			if (check.Active)
				UIntValue |= val;
			else
				UIntValue &= ~val;
		}
		
		void OnSelectFlags (object o, EventArgs args)
		{
			using (FlagsSelectorDialog dialog = new FlagsSelectorDialog (null, enm, UIntValue, property)) {
				if (dialog.Run () == (int) ResponseType.Ok) {
					Value = Enum.ToObject (enm.EnumType, dialog.Value);
				}
			}
		}
	}
}
