using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.Debugger
{
	[Export (typeof (IGlyphFactoryProvider))]
	[Name ("ReturnStatementGlyph")]
	[Order (After = "CurrentStatementGlyph")]
	[ContentType ("any")]
	[TagType (typeof (ReturnStatementGlyphTag))]
	internal class ReturnStatementGlyphFactoryProvider : IGlyphFactoryProvider
	{
		[Import]
		private IImageService imageService = null;
		private readonly ImageId imageId = new ImageId (new System.Guid ("{ae27a6b0-e345-4288-96df-5eaf394ee369}"), 386);

		public IGlyphFactory GetGlyphFactory (ICocoaTextView view, ICocoaTextViewMargin margin)
		{
			return new ImageSourceGlyphFactory<ReturnStatementGlyphTag> (imageId, imageService);
		}
	}
}
