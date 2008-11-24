using System;
using System.Xml;
using System.CodeDom;

namespace Stetic.Wrapper {

	public class CheckButton : Container {

		public override void Wrap (object obj, bool initialized)
		{
			base.Wrap (obj, initialized);
			if (!initialized)
				checkbutton.UseUnderline = true;
		}
		
		public override void Read (ObjectReader reader, XmlElement elem)
		{
			base.Read (reader, elem);
			if (reader.Format == FileFormat.Glade)
				checkbutton.UseUnderline = true;
		}
		protected override ObjectWrapper ReadChild (ObjectReader reader, XmlElement child_elem)
		{
			hasLabel = false;
			if (checkbutton.Child != null)
				checkbutton.Remove (checkbutton.Child);
			return base.ReadChild (reader, child_elem);
		}

		public Gtk.CheckButton checkbutton {
			get {
				return (Gtk.CheckButton)Wrapped;
			}
		}

		bool hasLabel = true;
		public bool HasLabel {
			get {
				return hasLabel;
			}
			set {
				hasLabel = value;
				EmitNotify ("HasLabel");
			}
		}

		internal void RemoveLabel ()
		{
			AddPlaceholder ();
			HasLabel = false;
		}

		public override Placeholder AddPlaceholder ()
		{
			if (checkbutton.Child != null)
				checkbutton.Remove (checkbutton.Child);
			return base.AddPlaceholder ();
		}
		
		internal void RestoreLabel ()
		{
			checkbutton.Label = checkbutton.Name;
			HasLabel = true;
		}

		protected override void ReplaceChild (Gtk.Widget oldChild, Gtk.Widget newChild)
		{
			base.ReplaceChild (oldChild, newChild);
			EmitNotify ("HasContents");
		}

		protected override void GenerateChildBuildCode (GeneratorContext ctx, CodeExpression parentVar, Widget wrapper)
		{
			if (!HasLabel) {
				// CheckButton generates a label by default. Remove it if it is not required.
				ctx.Statements.Add (
					new CodeMethodInvokeExpression (
						parentVar,
						"Remove",
						new CodePropertyReferenceExpression (
							parentVar,
							"Child"
						)
					)
				);
			}
			base.GenerateChildBuildCode (ctx, parentVar, wrapper);
		}
	}
}
