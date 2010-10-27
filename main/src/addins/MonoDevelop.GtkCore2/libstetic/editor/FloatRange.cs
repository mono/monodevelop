using System;

namespace Stetic.Editor {

	public class FloatRange : Gtk.SpinButton, IPropertyEditor
	{
		Type propType;
		
		public FloatRange (): base (0, 0, 0.01)
		{
		}
		
		public void Initialize (PropertyDescriptor prop)
		{
			propType = prop.PropertyType;
			
			double min, max;
			
			if (propType == typeof(double)) {
				min = Double.MinValue;
				max = Double.MaxValue;
			} else if (propType == typeof(float)) {
				min = float.MinValue;
				max = float.MaxValue;
			} else
				throw new ApplicationException ("FloatRange editor does not support editing values of type " + propType);
			
			if (prop.Minimum != null)
				min = (double) Convert.ChangeType (prop.Minimum, typeof(double));
			if (prop.Maximum != null)
				max = (double) Convert.ChangeType (prop.Maximum, typeof(double));
			
			SetRange (min, max);
			
			Digits = 2;
		}
		
		public void AttachObject (object ob)
		{
		}
		
		object IPropertyEditor.Value {
			get { return Convert.ChangeType (base.Value, propType); }
			set { base.Value = (double) Convert.ChangeType (value, typeof(double)); }
		}
	}
}
