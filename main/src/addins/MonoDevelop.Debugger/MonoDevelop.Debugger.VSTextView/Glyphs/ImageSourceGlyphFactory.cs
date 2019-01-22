using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using AppKit;
using Microsoft.VisualStudio.Core.Imaging;
using MonoDevelop.Ide;

namespace Microsoft.Ide.Editor
{
	public class ImageSourceGlyphFactory<T> : IGlyphFactory
		where T : IGlyphTag
	{
		private readonly string imageSource;

		public ImageSourceGlyphFactory (string imageSource)
		{
			this.imageSource = imageSource;
		}

		public object GenerateGlyph (ICocoaFormattedLine line, IGlyphTag tag)
		{
			if (tag == null || !(tag is T) || imageSource == null) {
				return null;
			}
			Xwt.Drawing.Image xwtImage = ImageService.GetIcon (imageSource);
			var nsImage = (NSImage)Xwt.Toolkit.NativeEngine.GetNativeImage (xwtImage);

			var imageView = NSImageView.FromImage (nsImage);
			imageView.SetFrameSize (imageView.FittingSize);
			return imageView;
		}
	}
}
