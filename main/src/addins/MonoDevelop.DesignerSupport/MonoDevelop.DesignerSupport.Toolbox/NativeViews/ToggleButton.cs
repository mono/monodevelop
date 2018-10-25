#if MAC
using System;
using AppKit;
using CoreGraphics;
using Xwt;

namespace MonoDevelop.DesignerSupport.Toolbox.NativeViews
{
	public class ToggleButton : NSButton, INativeChildView
	{
		public event EventHandler Focused;

		public override CGSize IntrinsicContentSize => Hidden ? CGSize.Empty : new CGSize (25, 25);

		public ToggleButton () 
		{
			Title = "";
			BezelStyle = NSBezelStyle.RoundRect;
			SetButtonType (NSButtonType.OnOff);
			FocusRingType = NSFocusRingType.Default;
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

		bool isMouseHover, isFirstResponder;
		public override void MouseEntered (NSEvent theEvent)
		{
			base.MouseEntered (theEvent);
			isMouseHover = true;
		}

		public override void MouseExited (NSEvent theEvent)
		{
			base.MouseExited (theEvent);
			isMouseHover = false;
		}

		public override bool BecomeFirstResponder ()
		{
			isFirstResponder = true;
			Focused?.Invoke (this, EventArgs.Empty);
			return base.BecomeFirstResponder ();
		}

		public override bool ResignFirstResponder ()
		{
			isFirstResponder = false;
			return base.ResignFirstResponder ();
		}

		public bool Active {
			get => State == NSCellStateValue.On;
			set {
				State = value ? NSCellStateValue.On : NSCellStateValue.Off;
			}
		}

		public override void DrawRect (CGRect dirtyRect)
		{
			base.DrawRect (dirtyRect);
		}

		public override void KeyDown (NSEvent theEvent)
		{
			base.KeyDown (theEvent);
			if ((int)theEvent.ModifierFlags == 256 && (theEvent.KeyCode == 49 || theEvent.KeyCode == 36)) {
				PerformClick (this);
			}
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

		#endregion
	}
}
#endif