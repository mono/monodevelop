using System;
using System.CodeDom;

namespace Stetic.Wrapper {

	public class HScrollbar : Range {

		public static new Gtk.HScrollbar CreateInstance ()
		{
			return new Gtk.HScrollbar (new Gtk.Adjustment (0.0, 0.0, 100.0, 1.0, 10.0, 10.0));
		}
		
		internal protected override CodeExpression GenerateObjectCreation (GeneratorContext ctx)
		{
			return new CodeObjectCreateExpression (ClassDescriptor.WrappedTypeName, new CodePrimitiveExpression (null));
		}
	}
}
