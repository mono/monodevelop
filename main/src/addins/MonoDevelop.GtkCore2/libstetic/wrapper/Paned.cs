using System;

namespace Stetic.Wrapper {

	public class Paned : Container {

		int position;
		
		public override void Wrap (object obj, bool initialized)
		{
			base.Wrap (obj, initialized);
			if (!initialized && AllowPlaceholders) {
				Placeholder ph = CreatePlaceholder ();
				paned.Pack1 (ph, true, false);
				NotifyChildAdded (ph);
				ph = CreatePlaceholder ();
				paned.Pack2 (ph, true, false);
				NotifyChildAdded (ph);
			}
			position = paned.Position;
			paned.Realized += PanedRealized;
		}

		void PanedRealized (object sender, EventArgs e)
		{
			// The position may be reset while realizing the object, so
			// we set it now here. See bug #542227. This seems to be Windows only.
			bool old = Loading;
			Loading = true;
			paned.Position = position;
			Loading = old;
		}
		
		public override void Dispose ()
		{
			base.Dispose ();
			paned.Realized -= PanedRealized;
		}
		
		protected Gtk.Paned paned {
			get {
				return (Gtk.Paned)Wrapped;
			}
		}
		
		public int Position {
			get {
				return paned.Position;
			}
			set {
				position = value;
				paned.Position = value;
			}
		}

		protected override void ReplaceChild (Gtk.Widget oldChild, Gtk.Widget newChild)
		{
			if (oldChild == paned.Child1) {
				paned.Remove (oldChild);
				paned.Add1 (newChild);
			} else if (oldChild == paned.Child2) {
				paned.Remove (oldChild);
				paned.Add2 (newChild);
			}
			NotifyChildAdded (newChild);
		}
		
		public override void Delete (Stetic.Placeholder ph)
		{
			// Don't allow deleting placeholders
		}
	}
}
