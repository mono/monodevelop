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

		public ToggleButton () 
		{
			Title = "";
			BezelStyle = NSBezelStyle.TexturedSquare;
			SetButtonType (NSButtonType.OnOff);
			FocusRingType = NSFocusRingType.None;
			Bordered = false;
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
			NeedsDisplay = true;
		}

		public override void MouseExited (NSEvent theEvent)
		{
			base.MouseExited (theEvent);
			isMouseHover = false;
			NeedsDisplay = true;
		}

		public override bool BecomeFirstResponder ()
		{
			isFirstResponder = true;
			NeedsDisplay = true;
			Focused?.Invoke (this, EventArgs.Empty);
			return base.BecomeFirstResponder ();
		}

		public override bool ResignFirstResponder ()
		{
			isFirstResponder = false;
			NeedsDisplay = true;
			return base.ResignFirstResponder ();
		}

		public override CGSize IntrinsicContentSize => new CGSize (25, 25);

		public bool Visible {
			get { return !Hidden; }
			set {
				Hidden = !value;
			}
		}

		public bool Active {
			get => State == NSCellStateValue.On;
			set {
				State = value ? NSCellStateValue.On : NSCellStateValue.Off;
			}
		}

		//Over #3F3F3F - #212121
		//Focused #494949 - #282828
		//Over+Focused #393939 - #191919
		//On #323232 -  #1D1D1D
		//OnHover #363636 - #222222
		public override void DrawRect (CGRect dirtyRect)
		{
			base.DrawRect (dirtyRect);

			NSColor backgroundColor = null;
			NSColor borderColor = null;

			if (State == NSCellStateValue.On && isFirstResponder) {
				backgroundColor = Styles.ToggleButtonOnFocusedBackgroundColor;
				borderColor = Styles.ToggleButtonOnFocusedBorderColor;
			} else if (State == NSCellStateValue.On) {
				backgroundColor = Styles.ToggleButtonOnBackgroundColor;
				borderColor = Styles.ToggleButtonOnBorderColor;
			} else if (isFirstResponder && isMouseHover) {
				backgroundColor = Styles.ToggleButtonHoverFocusedBackgroundColor;
				borderColor = Styles.ToggleButtonHoverFocusedBorderColor;
			} else if (isFirstResponder) {
				backgroundColor = Styles.ToggleButtonFocusedBackgroundColor;
				borderColor = Styles.ToggleButtonFocusedBorderColor;
			} else if (isMouseHover) {
				backgroundColor = Styles.ToggleButtonHoverBackgroundColor;
				borderColor = Styles.ToggleButtonHoverBorderColor;
			}

			if (backgroundColor != null) {
				var path = NSBezierPath.FromRoundedRect (dirtyRect, Styles.ToggleButtonCornerRadius, Styles.ToggleButtonCornerRadius);
				path.ClosePath ();

				backgroundColor.Set ();
				path.Fill ();
			
				path.LineWidth = Styles.ToggleButtonLineWidth;
				borderColor.Set ();
				path.Stroke ();
			}

			if (Image != null) {
				var startX = (Frame.Width - Image.Size.Width) / 2;
				var startY = (Frame.Width - Image.Size.Width) / 2;
				var context = NSGraphicsContext.CurrentContext;
				context.SaveGraphicsState ();
				Image.Draw (new CGRect (startX, startY, Image.Size.Width, Image.Size.Height));
				context.RestoreGraphicsState ();
			}
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