using AppKit;
using CoreGraphics;

namespace MonoDevelop.DesignerSupport.Toolbox.NativeViews
{
	public class ClickedButton : NSButton
	{
		public override CGSize IntrinsicContentSize => new CGSize (26, 26);

		public ClickedButton ()
		{
			BezelStyle = NSBezelStyle.TexturedSquare;
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

		public override void MouseExited (NSEvent theEvent)
		{
			base.MouseExited (theEvent);
			isMouseHover = false;
			NeedsDisplay = true;
		}

		public override void DrawRect (CGRect dirtyRect)
		{
			base.DrawRect (dirtyRect);

			if (isMouseHover || State == NSCellStateValue.On) {
				var path = NSBezierPath.FromRoundedRect (dirtyRect, 
				Styles.ToggleButtonCornerRadius, Styles.ToggleButtonCornerRadius);
				path.ClosePath ();
				path.LineWidth = Styles.ToggleButtonLineWidth;
				Styles.ToggleButtonHoverBorderColor.Set ();
				path.Stroke ();
			}
		}

		public bool Visible {
			get { return !Hidden; }
			set {
				Hidden = !value;
			}
		}
	}
}
