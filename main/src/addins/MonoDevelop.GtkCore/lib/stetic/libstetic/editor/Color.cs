using System;

namespace Stetic.Editor {

	public class Color: PropertyEditorCell 
	{
		public override void GetSize (int availableWidth, out int width, out int height)
		{
			width = 16;
			height = 16;
		}
		
		public override void Render (Gdk.Drawable window, Gdk.Rectangle bounds, Gtk.StateType state)
		{
			Gdk.GC gc = new Gdk.GC (window);
	   		gc.RgbFgColor = (Gdk.Color) Value;
			window.DrawRectangle (gc, true, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
			window.DrawRectangle (Container.Style.BlackGC, false, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
		}
		
		protected override IPropertyEditor CreateEditor (Gdk.Rectangle cell_area, Gtk.StateType state)
		{
			return new ColorEditor ();
		}
	}
	
	[PropertyEditor ("Color", "ColorSet")]
	public class ColorEditor : Gtk.ColorButton, IPropertyEditor
	{
		public void Initialize (PropertyDescriptor descriptor)
		{
			if (descriptor.PropertyType != typeof(Gdk.Color))
				throw new ApplicationException ("Color editor does not support editing values of type " + descriptor.PropertyType);
		}
		
		public void AttachObject (object obj)
		{
		}
		
		public object Value { 
			get { return Color; } 
			set { Color = (Gdk.Color) value; }
		}
		
		protected override void OnColorSet ()
		{
			base.OnColorSet ();
			if (ValueChanged != null)
				ValueChanged (this, EventArgs.Empty);
		}

		public event EventHandler ValueChanged;
	}
}
