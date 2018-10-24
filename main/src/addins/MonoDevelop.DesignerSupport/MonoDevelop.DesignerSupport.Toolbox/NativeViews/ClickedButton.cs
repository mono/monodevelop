#if MAC
using System;
using AppKit;
using CoreGraphics;
using Xwt;

namespace MonoDevelop.DesignerSupport.Toolbox.NativeViews
{
	public class ClickedButton : NSButton, INativeChildView
	{
		public event EventHandler Focused;

		//public override CGSize IntrinsicContentSize => new CGSize (25, 25);

		bool isFirstResponder;

		public ClickedButton ()
		{
			BezelStyle = NSBezelStyle.RoundRect;
			FocusRingType = NSFocusRingType.Default;
			SetButtonType (NSButtonType.MomentaryChange);
			Bordered = false;
			Title = "";
			TranslatesAutoresizingMaskIntoConstraints = false;
		}

		NSTrackingArea trackingArea;
		public override void UpdateTrackingAreas ()
		{
			base.UpdateTrackingAreas ();
			if (trackingArea != null) {
				RemoveTrackingArea (trackingArea);
				trackingArea.Dispose ();
			}
			var viewBounds = Bounds;
			var options = NSTrackingAreaOptions.MouseMoved | NSTrackingAreaOptions.ActiveInKeyWindow | NSTrackingAreaOptions.MouseEnteredAndExited;
			trackingArea = new NSTrackingArea (viewBounds, options, this, null);
			AddTrackingArea (trackingArea);
		}

		bool isMouseHover;
		public override void MouseEntered (NSEvent theEvent)
		{
			base.MouseEntered (theEvent);
			isMouseHover = true;
		}

		public override bool ResignFirstResponder ()
		{
			isFirstResponder = false;
			return base.ResignFirstResponder ();
		}

		public override bool BecomeFirstResponder ()
		{
			isFirstResponder = true;
			Focused?.Invoke (this, EventArgs.Empty);
			return base.BecomeFirstResponder ();
		}

		public override void MouseExited (NSEvent theEvent)
		{
			base.MouseExited (theEvent);
			isMouseHover = false;
		}

		#region IEncapsuledView

		public void OnKeyPressed (object o, Gtk.KeyPressEventArgs ev)
		{
			if (ev.Event.State == Gdk.ModifierType.None && (ev.Event.Key == Gdk.Key.KP_Enter || ev.Event.Key == Gdk.Key.KP_Space)) {
				PerformClick (this);
			}
		}

		public void OnKeyReleased (object o, Gtk.KeyReleaseEventArgs ev)
		{

		}

		public override void KeyDown (NSEvent theEvent)
		{
			base.KeyDown (theEvent);
			if ((int)theEvent.ModifierFlags == 256 && (theEvent.KeyCode == 49 || theEvent.KeyCode == 36)) {
				PerformClick (this);
			}
		}

		#endregion

		public bool Visible {
			get { return !Hidden; }
			set {
				Hidden = !value;
			}
		}
	}
}
#endif