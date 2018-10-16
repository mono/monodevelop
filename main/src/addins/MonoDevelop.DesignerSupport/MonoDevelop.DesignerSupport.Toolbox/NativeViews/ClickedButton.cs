using System;
using AppKit;
using CoreGraphics;
using Xwt;

namespace MonoDevelop.DesignerSupport.Toolbox.NativeViews
{
	public class ClickedButton : NSButton, INativeChildView
	{
		public event EventHandler Focused;

		public override CGSize IntrinsicContentSize => new CGSize (26, 26);

		bool isFirstResponder;

		public ClickedButton ()
		{
			BezelStyle = NSBezelStyle.TexturedSquare;
			FocusRingType = NSFocusRingType.None;
			SetButtonType (NSButtonType.OnOff);
			Bordered = false;
			Title = "";
			WantsLayer = true;
			Layer.BackgroundColor = NSColor.Clear.CGColor;
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
			NeedsDisplay = true;
		}

		public override bool ResignFirstResponder ()
		{
			isFirstResponder = false;
			NeedsDisplay = true;
			return base.ResignFirstResponder ();
		}

		public override bool BecomeFirstResponder ()
		{
			isFirstResponder = true;
			Focused?.Invoke (this, EventArgs.Empty);
			NeedsDisplay = true;
			return base.BecomeFirstResponder ();
		}

		public override void MouseExited (NSEvent theEvent)
		{
			base.MouseExited (theEvent);
			isMouseHover = false;
			NeedsDisplay = true;
		}

		public override void DrawRect (CGRect dirtyRect)
		{
			base.DrawRect (dirtyRect);

			if (isMouseHover) {
				var path = NSBezierPath.FromRoundedRect (dirtyRect,Styles.ToggleButtonCornerRadius, Styles.ToggleButtonCornerRadius);
				path.ClosePath ();
				path.LineWidth = Styles.ToggleButtonLineWidth;
				Styles.ToggleButtonHoverBorderColor.Set ();
				path.Stroke ();
				return;
			}
			if (isFirstResponder) {
				var path = NSBezierPath.FromRoundedRect (dirtyRect, Styles.ToggleButtonCornerRadius, Styles.ToggleButtonCornerRadius);
				path.ClosePath ();
				path.LineWidth = Styles.ToggleButtonLineWidth;
				Styles.ToggleButtonHoverBorderColor.Set ();
				path.Stroke ();
			}
		}

		#region IEncapsuledView

		public void OnKeyPressed (object s, KeyEventArgs e)
		{

		}

		public void OnKeyReleased (object s, KeyEventArgs e)
		{

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
