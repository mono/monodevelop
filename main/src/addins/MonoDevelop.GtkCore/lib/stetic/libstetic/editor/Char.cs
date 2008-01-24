using System;

namespace Stetic.Editor {

	public class Char : Gtk.Entry, IPropertyEditor {

		public Char ()
		{
			MaxLength = 1;
		}

		public void Initialize (PropertyDescriptor descriptor)
		{
			if (descriptor.PropertyType != typeof(char))
				throw new ApplicationException ("Char editor does not support editing values of type " + descriptor.PropertyType);
		}
		
		public void AttachObject (object obj)
		{
		}
		
		char last;

		public object Value {
			get {
				if (Text.Length == 0)
					return last;
				else
					return Text[0];
			}
			set {
				Text = value.ToString ();
				last = (char) value;
			}
		}

		protected override void OnChanged ()
		{
			if (ValueChanged != null)
				ValueChanged (this, EventArgs.Empty);
		}

		public event EventHandler ValueChanged;
	}
}
