using System;
using AppKit;
using CoreGraphics;
using Foundation;
using Xwt;

namespace MonoDevelop.DesignerSupport.Toolbox.NativeViews
{
	public class SearchTextField : NSSearchField, INativeChildView
	{
		public event EventHandler Focused;

		public override bool BecomeFirstResponder ()
		{
			NeedsDisplay = true;
			Focused?.Invoke (this, EventArgs.Empty);
			return base.BecomeFirstResponder ();
		}

		public string Text {
			get { return StringValue; }
			set {
				StringValue = value;
			}
		}

		public SearchTextField ()
		{
			WantsLayer = true;
			Layer.BackgroundColor = NSColor.Clear.CGColor;
			Layer.BorderWidth = Styles.SearchTextFieldLineBorderWidth;
			Layer.BackgroundColor = Styles.SearchTextFieldLineBackgroundColor.CGColor;
		}

		public override void DrawRect (CGRect dirtyRect)
		{
			base.DrawRect (dirtyRect);
			NSColor.Clear.Set ();
			NSBezierPath.FillRect (Bounds);
		}

		#region IEncapsuledView

		public override bool ResignFirstResponder ()
		{
			NeedsDisplay = true;
			return base.ResignFirstResponder ();
		}


		public void OnKeyPressed (object s, KeyEventArgs e)
		{
			//we want the native handling here
			e.Handled = false;
		}


		public void OnKeyReleased (object s, KeyEventArgs e)
		{

		}

		#endregion
	}
}
