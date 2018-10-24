#if MAC
using System;
using AppKit;
using CoreGraphics;
using Foundation;
using MonoDevelop.Ide;
using Xwt;

namespace MonoDevelop.DesignerSupport.Toolbox.NativeViews
{
	public class SearchTextField : NSSearchField, INativeChildView
	{
		public event EventHandler Focused;

		public SearchTextField ()
		{
			TranslatesAutoresizingMaskIntoConstraints = false;
		}

		public override bool BecomeFirstResponder ()
		{
			Focused?.Invoke (this, EventArgs.Empty);
			return base.BecomeFirstResponder ();
		}

		#region IEncapsuledView

		public void OnKeyPressed (object o, Gtk.KeyPressEventArgs ev)
		{

		}

		public void OnKeyReleased (object o, Gtk.KeyReleaseEventArgs ev)
		{

		}

		#endregion
	}
}
#endif