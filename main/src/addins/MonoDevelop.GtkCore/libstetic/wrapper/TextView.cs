using System;
using System.CodeDom;
using System.Collections;

namespace Stetic.Wrapper {

	public class TextView : Container {

		public override void Wrap (object obj, bool initialized)
		{
			base.Wrap (obj, initialized);
			((Gtk.TextView)Wrapped).Buffer.Changed += Buffer_Changed;
			if (!initialized)
				ShowScrollbars = true;
		}

		public override void Dispose ()
		{
			((Gtk.TextView)Wrapped).Buffer.Changed -= Buffer_Changed;
			base.Dispose ();
		}

		public string Text {
			get {
				return ((Gtk.TextView)Wrapped).Buffer.Text;
			}
			set {
				((Gtk.TextView)Wrapped).Buffer.Text = value;
			}
		}

		public void Buffer_Changed (object obj, EventArgs args)
		{
			EmitNotify ("Text");
		}
		
		protected override bool AllowPlaceholders {
			get {
				return false;
			}
		}
		
		internal protected override void GenerateBuildCode (GeneratorContext ctx, CodeExpression var)
		{
			if (Text.Length > 0) {
				PropertyDescriptor prop = (PropertyDescriptor)this.ClassDescriptor ["Text"];
				bool trans = prop.IsTranslated (Wrapped);

				ctx.Statements.Add (
					new CodeAssignStatement (
						new CodePropertyReferenceExpression (
							new CodePropertyReferenceExpression (
								var,
								"Buffer"
							),
							"Text"
						),
						ctx.GenerateValue (Text, typeof(string), trans)
					)
				);
			}
			base.GenerateBuildCode (ctx, var);
		}
	}
}
