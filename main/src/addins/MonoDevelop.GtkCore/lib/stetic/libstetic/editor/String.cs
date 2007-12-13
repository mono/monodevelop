using System;

namespace Stetic.Editor {

	[PropertyEditor ("Text", "Changed")]
	public class String : Translatable {

		Gtk.Entry entry;

		public override void Initialize (PropertyDescriptor prop)
		{
			base.Initialize (prop);
			
			entry = new Gtk.Entry ();
			entry.HasFrame = false;
			entry.Show ();
			entry.Changed += EntryChanged;
			Add (entry);
		}

		protected override void CheckType (PropertyDescriptor prop)
		{
			if (prop.PropertyType != typeof(string))
				throw new ApplicationException ("String editor does not support editing values of type " + prop.PropertyType);
		}
		
		public override object Value {
			get {
				return entry.Text;
			}
			set {
				if (value == null)
					entry.Text = "";
				else
					entry.Text = (string) value;
			}
		}

		void EntryChanged (object obj, EventArgs args)
		{
			OnValueChanged ();
		}
	}
}
