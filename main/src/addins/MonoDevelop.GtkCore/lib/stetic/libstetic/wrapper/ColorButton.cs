using System;
using System.CodeDom;

namespace Stetic.Wrapper {

	public class ColorButton : Container {

		public int Alpha {
			get {
				Gtk.ColorButton cb = (Gtk.ColorButton)Wrapped;

				if (cb.UseAlpha)
					return cb.Alpha;
				else
					return -1;
			}
			set {
				Gtk.ColorButton cb = (Gtk.ColorButton)Wrapped;

				if (value == -1)
					cb.UseAlpha = false;
				else {
					cb.UseAlpha = true;
					cb.Alpha = (ushort)value;
				}
			}
		}
		
		protected override void GeneratePropertySet (GeneratorContext ctx, CodeExpression var, PropertyDescriptor prop)
		{
			if (prop.Name == "Alpha" && Alpha == -1)
				return;
			else
				base.GeneratePropertySet (ctx, var, prop);
		}
	}
}
