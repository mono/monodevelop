using System;

namespace Stetic.Wrapper {

	public class Paned : Container {

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
		}

		protected Gtk.Paned paned {
			get {
				return (Gtk.Paned)Wrapped;
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
