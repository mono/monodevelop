#if MAC
using System;
using System.Drawing;
using AppKit;
using CoreGraphics;
using Foundation;
using Xwt;

namespace MonoDevelop.DesignerSupport.Toolbox.NativeViews
{
	public class ExpanderButton : NSButton, INativeChildView
	{
		const int Margin = 5;
		static CGPoint textPoint = new CGPoint (7, 5);

		public event EventHandler Focused;

		public override bool BecomeFirstResponder ()
		{
			Focused?.Invoke (this, EventArgs.Empty);
			return base.BecomeFirstResponder ();
		}

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
#endif