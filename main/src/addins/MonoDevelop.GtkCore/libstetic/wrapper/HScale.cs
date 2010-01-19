using System;
using System.CodeDom;

namespace Stetic.Wrapper {

	public class HScale : Scale {

		public static Gtk.HScale CreateInstance ()
		{
			return new Gtk.HScale (0.0, 100.0, 1.0);
		}
		
		internal protected override CodeExpression GenerateObjectCreation (GeneratorContext ctx)
		{
			return new CodeObjectCreateExpression (ClassDescriptor.WrappedTypeName.ToGlobalTypeRef (), new CodePrimitiveExpression (null));
		}
	}
}
