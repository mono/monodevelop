using System;
using AppKit;
using CoreGraphics;
using Xwt;

namespace MonoDevelop.DesignerSupport.Toolbox.NativeViews
{
	public class ClickedButton : NSButton, INativeChildView
	{
		public event EventHandler Focused;

		public override CGSize IntrinsicContentSize => new CGSize (25, 25);

		bool isFirstResponder;

		public ClickedButton ()
		{
			BezelStyle = NSBezelStyle.TexturedSquare;
			FocusRingType = NSFocusRingType.None;
			SetButtonType (NSButtonType.OnOff);
			Bordered = false;
			Title = "";
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

		//focused -> #505050 - Line ->#494949
		//focus encima + mouse over ##565656 - #424242
		//mouse over -> #595959 - #333133
		public override void DrawRect (CGRect dirtyRect)
		{
			base.DrawRect (dirtyRect);

			NSColor backgroundColor = null;
			NSColor borderColor = null;
			if (isFirstResponder && isMouseHover) {
				backgroundColor = Styles.ClickedButtonHoverFocusedBackgroundColor;
				borderColor = Styles.ClickedButtonHoverFocusedBorderColor;
			} else if (isFirstResponder) {
				backgroundColor = Styles.ClickedButtonFocusedBackgroundColor;
				borderColor = Styles.ClickedButtonFocusedBorderColor;
			} else if (isMouseHover) {
				backgroundColor = Styles.ClickedButtonHoverBackgroundColor;
				borderColor = Styles.ClickedButtonHoverBorderColor;
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
