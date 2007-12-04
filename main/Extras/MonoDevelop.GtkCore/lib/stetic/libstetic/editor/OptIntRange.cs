using System;

namespace Stetic.Editor {

	public class OptIntRange : Gtk.HBox, IPropertyEditor {

		Gtk.CheckButton check;
		Gtk.SpinButton spin;
		object omin, omax;

		public OptIntRange () : base (false, 6)
		{
		}
		
		public OptIntRange (object omin, object omax) : base (false, 6)
		{
			this.omin = omin;
			this.omax = omax;
		}
		
		public void Initialize (PropertyDescriptor prop)
		{
			if (prop.PropertyType != typeof(int))
				throw new ApplicationException ("OptIntRange editor does not support editing values of type " + prop.PropertyType);

			double min = (double) Int32.MinValue;
			double max = (double) Int32.MaxValue;
			
			if (omin == null)
				omin = prop.Minimum;
			if (omax == null)
				omax = prop.Maximum;
			
			if (omin != null)
				min = (double) Convert.ChangeType (omin, typeof(double));
			if (omax != null)
				max = (double) Convert.ChangeType (omax, typeof(double));
			
			check = new Gtk.CheckButton ();
			check.Show ();
			check.Toggled += check_Toggled;
			PackStart (check, false, false, 0);

			spin = new Gtk.SpinButton (min, max, 1.0);
			spin.Show ();
			spin.HasFrame = false;
			spin.ValueChanged += spin_ValueChanged;
			PackStart (spin, true, true, 0);
		}
		
		public void AttachObject (object ob)
		{
		}

		void check_Toggled (object o, EventArgs args)
		{
			spin.Sensitive = check.Active;
			if (ValueChanged != null)
				ValueChanged (this, EventArgs.Empty);
		}

		void spin_ValueChanged (object o, EventArgs args)
		{
			if (ValueChanged != null)
				ValueChanged (this, EventArgs.Empty);
		}

		public object Value {
			get {
				if (check.Active)
					return (int)spin.Value;
				else
					return -1;
			}
			set {
				int val = (int) value;
				if (val == -1) {
					check.Active = false;
					spin.Sensitive = false;
				} else {
					check.Active = true;
					spin.Sensitive = true;
					spin.Value = (double)val;
				}
			}
		}

		public event EventHandler ValueChanged;
	}
}
