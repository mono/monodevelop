using AppKit;
using CoreGraphics;

namespace MonoDevelop.DesignerSupport.Toolbox.NativeViews
{
	public class LoadingView : NSView
	{
		readonly NSTextField titleLabel;

		public string StringValue {
			get => titleLabel.StringValue;
			set => titleLabel.StringValue = value ?? "";
		}

		public override bool IsOpaque => false;

		public LoadingView (string message = "")
		{
			titleLabel = new NSTextField () { Bezeled = false, Editable = false, Selectable = false, DrawsBackground = false };
			AddSubview (titleLabel);
			titleLabel.NeedsLayout = true;
			StringValue = message;
			NeedsDisplay = true;
			titleLabel.SizeToFit ();
		}

		public override void SetFrameSize (CGSize newSize)
		{
			base.SetFrameSize (newSize);
			var x = newSize.Width / 2 - titleLabel.Frame.Width / 2;
			var y = newSize.Height / 2 - titleLabel.Frame.Height / 2;
			titleLabel.Frame = new CGRect (x, y, titleLabel.Frame.Width, titleLabel.Frame.Height);
		}
	}
}
