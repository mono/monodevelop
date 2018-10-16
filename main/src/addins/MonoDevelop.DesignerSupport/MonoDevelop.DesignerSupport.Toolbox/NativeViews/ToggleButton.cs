using System;
using AppKit;
using CoreGraphics;
using Xwt;

namespace MonoDevelop.DesignerSupport.Toolbox.NativeViews
{
	public class ToggleButton : NSButton, INativeChildView
	{
		public event EventHandler Focused;
		public event EventHandler RequestFocusPreviousItem;
		public event EventHandler RequestFocusNextItem;

		public ToggleButton () 
		{
			BezelStyle = NSBezelStyle.TexturedSquare;
			SetButtonType (NSButtonType.OnOff);
			Bordered = false;
			Title = "";
			WantsLayer = true;
			Layer.BackgroundColor = NSColor.Clear.CGColor;
			FocusRingType = NSFocusRingType.None;
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

		public override CGSize IntrinsicContentSize => new CGSize (26, 26);

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

		public override void DrawRect (CGRect dirtyRect)
		{
			base.DrawRect (dirtyRect);

			if (isMouseHover || isFirstResponder || State == NSCellStateValue.On) {
				var path = NSBezierPath.FromRoundedRect (dirtyRect, Styles.ToggleButtonCornerRadius, Styles.ToggleButtonCornerRadius);
				path.ClosePath ();
				path.LineWidth = Styles.ToggleButtonLineWidth;
				if (State == NSCellStateValue.On) {
					Styles.ToggleButtonHoverClickedBackgroundColor.Set ();
				} else {
					Styles.ToggleButtonHoverBackgroundColor.Set ();
				}

				path.Fill ();
				Styles.ToggleButtonHoverBorderColor.Set ();
				path.Stroke ();
			} else {
				NSColor.Clear.Set ();
				NSBezierPath.FillRect (Bounds);
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
	}
}
