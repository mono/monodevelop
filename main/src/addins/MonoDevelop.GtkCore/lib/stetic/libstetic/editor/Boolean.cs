using System;

namespace Stetic.Editor {

	public class Boolean : PropertyEditorCell 
	{
		static int indicatorSize;
		static int indicatorSpacing;
		
		static Boolean ()
		{
			Gtk.CheckButton cb = new Gtk.CheckButton ();
			indicatorSize = (int) cb.StyleGetProperty ("indicator-size");
			indicatorSpacing = (int) cb.StyleGetProperty ("indicator-spacing");
		}
		
		public override void GetSize (int availableWidth, out int width, out int height)
		{
			width = 20;
			height = 20;
		}
		
		public override void Render (Gdk.Drawable window, Gdk.Rectangle bounds, Gtk.StateType state)
		{
			Gtk.ShadowType sh = (bool) Value ? Gtk.ShadowType.In : Gtk.ShadowType.Out;
			int s = indicatorSize - 1;
			if (s > bounds.Height)
				s = bounds.Height;
			if (s > bounds.Width)
				s = bounds.Width;
			Gtk.Style.PaintCheck (Container.Style, window, state, sh, bounds, Container, "checkbutton", bounds.X + indicatorSpacing - 1, bounds.Y + (bounds.Height - s)/2, s, s);
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
