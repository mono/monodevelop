using GLib;
using System;
using System.CodeDom;
using System.Collections;

namespace Stetic.Wrapper {

	public class Window : Container {

		public override void Wrap (object obj, bool initialized)
		{
			Gtk.Window window = (Gtk.Window)obj;

			window.TypeHint = Gdk.WindowTypeHint.Normal;
			base.Wrap (obj, initialized);

			if (!initialized) {
				if (window.Child is Placeholder)
					window.Child.SetSizeRequest (200, 200);
			}

			window.DeleteEvent += DeleteEvent;
		}
		
		public override void Dispose ()
		{
			Wrapped.DeleteEvent -= DeleteEvent;
			base.Dispose ();
		}

		[ConnectBefore]
		void DeleteEvent (object obj, Gtk.DeleteEventArgs args)
		{
			Wrapped.Hide ();
			args.RetVal = true;
		}

		public override bool HExpandable { get { return true; } }
		public override bool VExpandable { get { return true; } }

		// We don't want to actually set the underlying properties for these;
		// that would be annoying to interact with.
		bool modal;
		public bool Modal {
			get {
				return modal;
			}
			set {
				modal = value;
				EmitNotify ("Modal");
			}
		}

		Gdk.WindowTypeHint typeHint;
		public Gdk.WindowTypeHint TypeHint {
			get {
				return typeHint;
			}
			set {
				typeHint = value;
				EmitNotify ("TypeHint");
			}
		}

		Gtk.WindowType type;
		public Gtk.WindowType Type {
			get {
				return type;
			}
			set {
				type = value;
				EmitNotify ("Type");
			}
		}

		Gtk.WindowPosition windowPosition;
		public Gtk.WindowPosition WindowPosition {
			get {
				return windowPosition;
			}
			set {
				windowPosition = value;
				EmitNotify ("WindowPosition");
			}
		}

		ImageInfo icon;
		public ImageInfo Icon {
			get {
				return icon;
			}
			set {
				icon = value;
				Gtk.Window window = (Gtk.Window)Wrapped;
				try {
					if (icon != null)
						window.Icon = icon.GetImage (Project);
					else
						window.Icon = null;
				} catch {
					window.Icon = null;
				}
			}
		}

		internal protected override void GenerateBuildCode (GeneratorContext ctx, CodeExpression var)
		{
			base.GenerateBuildCode (ctx, var);
			
			if (((Gtk.Window)Wrapped).DefaultWidth == -1) {
				ctx.Statements.Add (
					new CodeAssignStatement (
						new CodePropertyReferenceExpression (
							var,
							"DefaultWidth"
						),
						new CodePrimitiveExpression (DesignWidth)
					)
				);
			}
				
			if (((Gtk.Window)Wrapped).DefaultHeight == -1) {
				ctx.Statements.Add (
					new CodeAssignStatement	 (
						new CodePropertyReferenceExpression (
							var,
							"DefaultHeight"
						),
						new CodePrimitiveExpression (DesignHeight)
					)
				);
			}
		}
		
		protected override void GeneratePropertySet (GeneratorContext ctx, CodeExpression var, PropertyDescriptor prop)
		{
			if (prop.Name != "Type")
				base.GeneratePropertySet (ctx, var, prop);
		}
	}
}
