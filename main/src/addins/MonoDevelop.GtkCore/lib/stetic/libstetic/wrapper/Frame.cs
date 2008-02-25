using System;
using System.Collections;
using System.Xml;
using System.CodeDom;

namespace Stetic.Wrapper {

	public class Frame : Container {

		public override void Wrap (object obj, bool initialized)
		{
			base.Wrap (obj, initialized);
			if (!initialized) {
				frame.Label = "<b>" + frame.Name + "</b>";
				((Gtk.Label)frame.LabelWidget).UseMarkup = true;
				frame.Shadow = Gtk.ShadowType.None;
				if (AllowPlaceholders) {
					Gtk.Alignment align = new Gtk.Alignment (0, 0, 1, 1);
					align.LeftPadding = 12;
					Container align_wrapper = (Container)ObjectWrapper.Create (proj, align);
					align_wrapper.AddPlaceholder ();
					ReplaceChild (frame.Child, (Gtk.Widget)align_wrapper.Wrapped, true);
				}
			}

			if (frame.LabelWidget != null)
				ObjectWrapper.Create (proj, frame.LabelWidget);
			frame.AddNotification ("label-widget", LabelWidgetChanged);
		}

		void LabelWidgetChanged (object obj, GLib.NotifyArgs args)
		{
			if (!IsDisposed && frame.LabelWidget != null && !(frame.LabelWidget is Stetic.Placeholder))
				ObjectWrapper.Create (proj, frame.LabelWidget);
		}

		Gtk.Frame frame {
			get {
				return (Gtk.Frame)Wrapped;
			}
		}

		protected override ObjectWrapper ReadChild (ObjectReader reader, XmlElement child_elem)
		{
			if ((string)GladeUtils.GetChildProperty (child_elem, "type", "") == "label_item") {
				ObjectWrapper wrapper = reader.ReadObject (child_elem["widget"]);
				frame.LabelWidget = (Gtk.Widget)wrapper.Wrapped;
				return wrapper;
			} else
				return base.ReadChild (reader, child_elem);
		}

		protected override XmlElement WriteChild (ObjectWriter writer, Widget wrapper)
		{
			XmlElement child_elem = base.WriteChild (writer, wrapper);
			if (wrapper.Wrapped == frame.LabelWidget)
				GladeUtils.SetChildProperty (child_elem, "type", "label_item");
			return child_elem;
		}

		protected override void GenerateChildBuildCode (GeneratorContext ctx, CodeExpression parentVar, Widget wrapper)
		{
			if (wrapper.Wrapped == frame.LabelWidget) {
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
		
		protected override void ReplaceChild (Gtk.Widget oldChild, Gtk.Widget newChild)
		{
			if (oldChild == frame.LabelWidget)
				frame.LabelWidget = newChild;
			else
				base.ReplaceChild (oldChild, newChild);
		}

		public override void Delete (Stetic.Placeholder ph)
		{
			using (UndoManager.AtomicChange) {
				Delete ();
			}
		}
	}
}
