using System;
using System.Collections;
using System.Xml;
using System.CodeDom;

namespace Stetic.Wrapper {

	public class Expander : Container {

		public static new Gtk.Expander CreateInstance ()
		{
			return new Gtk.Expander ("");
		}

		public override void Wrap (object obj, bool initialized)
		{
			base.Wrap (obj, initialized);
			if (!initialized) {
				expander.Label = expander.Name;
				if (AllowPlaceholders)
					AddPlaceholder ();
			}
			if (expander.LabelWidget != null)
				ObjectWrapper.Create (proj, expander.LabelWidget);
		}

		protected override ObjectWrapper ReadChild (ObjectReader reader, XmlElement child_elem)
		{
			if ((string)GladeUtils.GetChildProperty (child_elem, "type", "") == "label_item") {
				ObjectWrapper wrapper = reader.ReadObject (child_elem["widget"]);
				expander.LabelWidget = (Gtk.Widget)wrapper.Wrapped;
				return wrapper;
			} else
				return base.ReadChild (reader, child_elem);
		}

		protected override XmlElement WriteChild (ObjectWriter writer, Widget wrapper)
		{
			XmlElement child_elem = base.WriteChild (writer, wrapper);
			if (wrapper.Wrapped == expander.LabelWidget)
				GladeUtils.SetChildProperty (child_elem, "type", "label_item");
			return child_elem;
		}

		Gtk.Expander expander {
			get {
				return (Gtk.Expander)Wrapped;
			}
		}

		protected override void ReplaceChild (Gtk.Widget oldChild, Gtk.Widget newChild)
		{
			if (oldChild == expander.LabelWidget)
				expander.LabelWidget = newChild;
			else
				base.ReplaceChild (oldChild, newChild);
		}
		
		internal protected override CodeExpression GenerateObjectCreation (GeneratorContext ctx)
		{
			return new CodeObjectCreateExpression (ClassDescriptor.WrappedTypeName, new CodePrimitiveExpression (null));
		}

		protected override void GenerateChildBuildCode (GeneratorContext ctx, CodeExpression parentVar, Widget wrapper)
		{
			if (wrapper.Wrapped == expander.LabelWidget) {
				CodeExpression var = ctx.GenerateNewInstanceCode (wrapper);
				CodeAssignStatement assign = new CodeAssignStatement (
					new CodePropertyReferenceExpression (
						parentVar,
						"LabelWidget"
					),
					var
				);
				ctx.Statements.Add (assign);
			} else
				base.GenerateChildBuildCode (ctx, parentVar, wrapper);
		}
	}
}
