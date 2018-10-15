using AppKit;
using CoreGraphics;

namespace MonoDevelop.DesignerSupport.Toolbox.NativeViews
{
	class PaddingView : NSView
	{
		public int Height {
			get;
			set;
		} = 100;

		int padding = 10;
		public int Padding {
			get => padding;
			set {
				if (padding == value) {
					return;
				}
				padding = value;
			}
		}

		NSView content;
		public NSView Content {
			get => content;
			set {
				if (content == value) {
					return;
				}

				if (content != null) {
					content.RemoveFromSuperview ();
				}

				content = value;
				AddSubview (content);
				SetFrameSize (Frame.Size);
				NeedsDisplay = true;
			}
		}

		public override CGSize IntrinsicContentSize => new CGSize (Frame.Width, Height); 

		public override void SetFrameSize (CGSize newSize)
		{
			base.SetFrameSize (newSize);
			if (content != null) {
				content.Frame = new CGRect (Frame.X + Padding, Frame.Y + Padding, Frame.Width - Padding, Frame.Height - Padding);
			}
		}
	}
}
