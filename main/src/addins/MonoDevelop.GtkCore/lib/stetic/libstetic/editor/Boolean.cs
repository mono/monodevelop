using System;

namespace Stetic.Editor {

	public class Boolean : PropertyEditorCell 
	{
		public override void GetSize (int availableWidth, out int width, out int height)
		{
			width = 20;
			height = 20;
		}
		
		public override void Render (Gdk.Drawable window, Gdk.Rectangle bounds, Gtk.StateType state)
		{
			Gtk.ShadowType sh = (bool) Value ? Gtk.ShadowType.In : Gtk.ShadowType.Out;
			Gtk.Style.PaintCheck (Container.Style, window, state, sh, bounds, Container, "", bounds.X, bounds.Y, bounds.Width, bounds.Height);
		}
		
		protected override IPropertyEditor CreateEditor (Gdk.Rectangle cell_area, Gtk.StateType state)
		{
			return new BooleanEditor ();
		}
	}
	
	[PropertyEditor ("Active", "Toggled")]
	public class BooleanEditor : Gtk.CheckButton, IPropertyEditor 
	{
		public void Initialize (PropertyDescriptor descriptor)
		{
			if (descriptor.PropertyType != typeof(bool))
				throw new ApplicationException ("Boolean editor does not support editing values of type " + descriptor.PropertyType);
		}
		
		public void AttachObject (object obj)
		{
		}
		
		public object Value { 
			get { return Active; } 
			set { Active = (bool) value; }
		}
		
		protected override void OnToggled ()
		{
			base.OnToggled ();
			if (ValueChanged != null)
				ValueChanged (this, EventArgs.Empty);
		}

		public event EventHandler ValueChanged;
	}
}
