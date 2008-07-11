using System;
using System.Xml;
using System.Collections;
using System.CodeDom;

namespace Stetic.Wrapper {

	public class ButtonBox : Box {

		Dialog actionDialog;
		
		public override void Wrap (object obj, bool initialized)
		{
			base.Wrap (obj, initialized);
			foreach (Gtk.Widget child in buttonbox.Children) {
				if (child is Placeholder)
					ReplaceChild (child, NewButton (), true);
			}
		}
		
		public void SetActionDialog (Dialog dialog)
		{
			actionDialog = dialog;
		}

		Gtk.Button NewButton ()
		{
			Gtk.Button button = (Gtk.Button)Registry.NewInstance ("Gtk.Button", proj);
			button.CanDefault = true;
			return button;
		}

		protected Gtk.ButtonBox buttonbox {
			get {
				return (Gtk.ButtonBox)Wrapped;
			}
		}

		protected override bool AllowPlaceholders {
			get {
				return false;
			}
		}
		internal new void InsertBefore (Gtk.Widget context)
		{
			int position;
			bool secondary;

			if (context == buttonbox) {
				position = 0;
				secondary = false;
			} else {
				Gtk.ButtonBox.ButtonBoxChild bbc = (Gtk.ButtonBox.ButtonBoxChild)ContextChildProps (context);
				position = bbc.Position;
				secondary = bbc.Secondary;
			}

			Gtk.Button button = NewButton ();
			buttonbox.PackStart (button, false, false, 0);
			buttonbox.ReorderChild (button, position);
			buttonbox.SetChildSecondary (button, secondary);
			EmitContentsChanged ();
		}

		internal new void InsertAfter (Gtk.Widget context)
		{
			int position;
			bool secondary;

			if (context == buttonbox) {
				position = buttonbox.Children.Length - 1;
				secondary = false;
			} else {
				Gtk.ButtonBox.ButtonBoxChild bbc = (Gtk.ButtonBox.ButtonBoxChild)ContextChildProps (context);
				position = bbc.Position;
				secondary = bbc.Secondary;
			}

			Gtk.Button button = NewButton ();
			buttonbox.PackStart (button, false, false, 0);
			buttonbox.ReorderChild (button, position + 1);
			buttonbox.SetChildSecondary (button, secondary);
			EmitContentsChanged ();
		}

		public int Size {
			get {
				return buttonbox.Children.Length;
			}
			set {
				Gtk.Widget[] children = buttonbox.Children;
				int cursize = children.Length;

				while (cursize > value) {
					Gtk.Widget w = children[--cursize];
					buttonbox.Remove (w);
					w.Destroy ();
				}
				while (cursize < value) {
					buttonbox.PackStart (NewButton (), false, false, 0);
					cursize++;
				}
			}
		}
		
		protected override void ReadChildren (ObjectReader reader, XmlElement elem)
		{
			// Reset the button count
			Size = 0;
			base.ReadChildren (reader, elem);
		}
		
		protected override void GenerateChildBuildCode (GeneratorContext ctx, CodeExpression parentVar, Widget wrapper)
		{
			if (actionDialog != null && wrapper is Button) {
			
				// If this is the action area of a dialog, buttons must be added using AddActionWidget,
				// so they are properly registered.
				
				ObjectWrapper childwrapper = ChildWrapper (wrapper);
				Button button = wrapper as Button;
				
				if (childwrapper != null) {
					CodeExpression dialogVar = ctx.WidgetMap.GetWidgetExp (actionDialog);
					ctx.Statements.Add (new CodeCommentStatement ("Container child " + Wrapped.Name + "." + childwrapper.Wrapped.GetType ()));
					CodeExpression var = ctx.GenerateNewInstanceCode (wrapper);
					if (button.ResponseId != (int) Gtk.ResponseType.None) {
						CodeMethodInvokeExpression invoke = new CodeMethodInvokeExpression (
							dialogVar,
							"AddActionWidget",
							var,
							new CodePrimitiveExpression (button.ResponseId)
						);
						ctx.Statements.Add (invoke);
					}
					else {
						CodeMethodInvokeExpression invoke = new CodeMethodInvokeExpression (
							parentVar,
							"Add",
							var
						);
						ctx.Statements.Add (invoke);
					}
					GenerateSetPacking (ctx, parentVar, var, childwrapper);
				}
			} else
				base.GenerateChildBuildCode (ctx, parentVar, wrapper);
		}

		public class ButtonBoxChild : Box.BoxChild {

			public bool InDialog {
				get {
					if (ParentWrapper == null)
						return false;
					return ParentWrapper.InternalChildProperty != null && ParentWrapper.InternalChildProperty.Name == "ActionArea";
				}
			}
		}
	}
}
