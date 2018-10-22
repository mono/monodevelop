#if MAC
using System;
using AppKit;
using CoreGraphics;
using Foundation;
using MonoDevelop.Ide;
using Xwt;

namespace MonoDevelop.DesignerSupport.Toolbox.NativeViews
{
	public class SearchTextField : NSSearchField, INativeChildView
	{
		public event EventHandler Focused;

		static NSImage searchImage = ImageService.GetIcon("md-searchbox-search", Gtk.IconSize.Menu).ToNative ();

		public string Text {
			get { return StringValue; }
			set {
				StringValue = value;
			}
		}

		public SearchTextField ()
		{
		}

		public override bool BecomeFirstResponder ()
		{
			NeedsDisplay = true;
			Focused?.Invoke (this, EventArgs.Empty);
			return base.BecomeFirstResponder ();
		}

		public override void DrawRect (CGRect dirtyRect)
		{
			base.DrawRect (dirtyRect);
			Styles.SearchTextFieldLineBackgroundColor.Set ();
			NSBezierPath.FillRect (Bounds);
			Styles.SearchTextFieldLineBorderColor.Set ();
			NSBezierPath.DefaultLineWidth = 1.5f;
			NSBezierPath.StrokeRect (Bounds);

			var startY = (Frame.Height - searchImage.Size.Height) / 2;
			var context = NSGraphicsContext.CurrentContext;
			context.SaveGraphicsState ();
			searchImage.Draw (new CGRect (3, startY, searchImage.Size.Width, searchImage.Size.Height));
			context.RestoreGraphicsState ();
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
#endif