using System.Drawing;
using AppKit;
using CoreGraphics;
using Foundation;

namespace MonoDevelop.DesignerSupport.Toolbox.NativeViews
{
	public class ExpanderButton : NSButton
	{
		const int Margin = 5;
		static CGPoint textPoint = new CGPoint (7, 5);

		public ExpanderButton ()
		{
			BezelStyle = NSBezelStyle.TexturedSquare;
			SetButtonType (NSButtonType.OnOff);
			Bordered = false;
			Title = "";
			WantsLayer = true;
			Layer.BackgroundColor = NSColor.Clear.CGColor;
		}

		public void SetCustomTitle (string title)
		{
			var font = NativeViewHelper.GetSystemFont (false, (int)NSFont.SmallSystemFontSize);
			AttributedTitle = NativeViewHelper.GetAttributedString (title, Styles.HeaderForegroundColor, font);
		}

		public override void DrawRect (CGRect dirtyRect)
		{
			base.DrawRect (dirtyRect);

			Styles.HeaderCellBackgroundSelectedColor.Set ();
			NSBezierPath.FillRect (Bounds);

			AttributedTitle.DrawAtPoint (textPoint);

			if (Image != null) {
				var context = NSGraphicsContext.CurrentContext;
				context.SaveGraphicsState ();
				Image.Draw (new CGRect (Frame.Width - Image.Size.Height - Margin, 4, Image.Size.Width, Image.Size.Height));
				context.RestoreGraphicsState ();
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
