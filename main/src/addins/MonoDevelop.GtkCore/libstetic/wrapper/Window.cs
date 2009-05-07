using GLib;
using System;
using System.CodeDom;
using System.Collections;

namespace Stetic.Wrapper {

	public class Window : Container {

		public override void Wrap (object obj, bool initialized)
		{
			TopLevelWindow window = (TopLevelWindow) obj;

			base.Wrap (obj, initialized);

			if (!initialized) {
				if (window.Child is Placeholder)
					window.Child.SetSizeRequest (200, 200);
			}

			window.DeleteEvent += DeleteEvent;
		}

		public static new TopLevelWindow CreateInstance ( )
		{
			TopLevelWindow t = new TopLevelWindow ();
			return t;
		}

		public override void Dispose ( )
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

		public bool Modal {
			get {
				return window.Modal;
			}
			set {
				window.Modal = value;
				EmitNotify ("Modal");
			}
		}

		public Gdk.WindowTypeHint TypeHint {
			get {
				return window.TypeHint;
			}
			set {
				window.TypeHint = value;
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
				EmitNotify ("Icon");
			}
		}

		TopLevelWindow window {
			get { return (TopLevelWindow) Wrapped; }
		}

		public string Title {
			get { return window.Title; }
			set { window.Title = value; EmitNotify ("Title"); }
		}

		public bool Resizable
		{
			get { return window.Resizable; }
			set { window.Resizable = value; EmitNotify ("Resizable"); }
		}

		bool allowGrow = true;
		public bool AllowGrow {
			get { return allowGrow; }
			set { allowGrow = value; EmitNotify ("AllowGrow"); }
		}

		bool allowShrink = false;
		public bool AllowShrink {
			get { return allowShrink; }
			set { allowShrink = value; EmitNotify ("AllowShrink"); }
		}

		int defaultWidth = -1;
		public int DefaultWidth {
			get { return defaultWidth; }
			set { defaultWidth = value; EmitNotify ("DefaultWidth"); }
		}

		int defaultHeight = -1;
		public int DefaultHeight {
			get { return defaultHeight; }
			set { defaultHeight = value; EmitNotify ("DefaultHeight"); }
		}

		bool acceptFocus = true;
		public bool AcceptFocus {
			get { return acceptFocus; }
			set { acceptFocus = value; EmitNotify ("AcceptFocus"); }
		}

		bool decorated = true;
		public bool Decorated {
			get { return decorated; }
			set { decorated = value; EmitNotify ("Decorated"); }
		}

		bool destroyWithParent;
		public bool DestroyWithParent {
			get { return destroyWithParent; }
			set { destroyWithParent = value; EmitNotify ("DestroyWithParent"); }
		}

		Gdk.Gravity gravity = Gdk.Gravity.NorthWest;
		public Gdk.Gravity Gravity {
			get { return gravity; }
			set { gravity = value; EmitNotify ("Gravity"); }
		}

		string role;
		public string Role {
			get { return role; }
			set { role = value; EmitNotify ("Role"); }
		}

		bool skipPagerHint;
		public bool SkipPagerHint {
			get { return skipPagerHint; }
			set { skipPagerHint = value; EmitNotify ("SkipPagerHint"); }
		}

		bool skipTaskbarHint;
		public bool SkipTaskbarHint {
			get { return skipTaskbarHint; }
			set { skipTaskbarHint = value; EmitNotify ("SkipTaskbarHint"); }
		}

		bool focusOnMap = true;
		public bool FocusOnMap {
			get { return focusOnMap; }
			set { focusOnMap = value; EmitNotify ("FocusOnMap"); }
		}

		internal protected override void GenerateBuildCode (GeneratorContext ctx, CodeExpression var)
		{
			base.GenerateBuildCode (ctx, var);
			
			if (DefaultWidth == -1) {
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
				
			if (DefaultHeight == -1) {
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
