using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Core.Imaging;
using MonoDevelop.Ide;
using System;

namespace MonoDevelop.Debugger
{
	class ImageSourceGlyphFactory<T> : IGlyphFactory
		where T : IGlyphTag
	{
		private readonly ImageId imageId;
		readonly IImageService imageService;
#if MAC
		private AppKit.NSImage nsImageCache;
#endif
		public ImageSourceGlyphFactory (ImageId imageId, IImageService imageService)
		{
			this.imageId = imageId;
			this.imageService = imageService;
		}

		public object GenerateGlyph (ITextViewLine line, IGlyphTag tag)
		{
			if (!(tag is T)) {
				return null;
			}
#if MAC
			if (nsImageCache == null)
				nsImageCache = (AppKit.NSImage)imageService.GetImage (imageId);
			var imageView = AppKit.NSImageView.FromImage (nsImageCache);
			imageView.SetFrameSize (imageView.FittingSize);
			return imageView;
#else
			throw new NotImplementedException ();
#endif
		}
	}
}
