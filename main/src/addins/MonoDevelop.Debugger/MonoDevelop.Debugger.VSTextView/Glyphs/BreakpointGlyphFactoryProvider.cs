using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Core.Imaging;

namespace MonoDevelop.Debugger
{
	[Export (typeof (IGlyphFactoryProvider))]
	[Name (nameof (BreakpointGlyphTag))]
	[ContentType ("code")]
	[TagType (typeof (BreakpointGlyphTag))]
	internal class BreakpointGlyphFactoryProvider : IGlyphFactoryProvider
	{
		[Import]
		private IImageService imageService = null;
		private readonly ImageId imageId = new ImageId (new System.Guid ("{ae27a6b0-e345-4288-96df-5eaf394ee369}"), 324);

		public IGlyphFactory GetGlyphFactory (ICocoaTextView view, ICocoaTextViewMargin margin)
		{
			return new ImageSourceGlyphFactory<BreakpointGlyphTag> (imageId, imageService);
		}
	}

	[Export (typeof (IGlyphFactoryProvider))]
	[Name (nameof (BreakpointDisabledGlyphTag))]
	[ContentType ("code")]
	[TagType (typeof (BreakpointDisabledGlyphTag))]
	internal class BreakpointDisabledGlyphFactoryProvider : IGlyphFactoryProvider
	{
		[Import]
		private IImageService imageService = null;
		private readonly ImageId imageId = new ImageId (new System.Guid ("{ae27a6b0-e345-4288-96df-5eaf394ee369}"), 323);

		public IGlyphFactory GetGlyphFactory (ICocoaTextView view, ICocoaTextViewMargin margin)
		{
			return new ImageSourceGlyphFactory<BreakpointDisabledGlyphTag> (imageId, imageService);
		}
	}

	[Export (typeof (IGlyphFactoryProvider))]
	[Name (nameof (BreakpointInvalidGlyphTag))]
	[ContentType ("code")]
	[TagType (typeof (BreakpointInvalidGlyphTag))]
	internal class BreakpointInvalidGlyphFactoryProvider : IGlyphFactoryProvider
	{
		[Import]
		private IImageService imageService = null;
		private readonly ImageId imageId = new ImageId (new System.Guid ("{ae27a6b0-e345-4288-96df-5eaf394ee369}"), 327);

		public IGlyphFactory GetGlyphFactory (ICocoaTextView view, ICocoaTextViewMargin margin)
		{
			return new ImageSourceGlyphFactory<BreakpointInvalidGlyphTag> (imageId, imageService);
		}
	}

	[Export (typeof (IGlyphFactoryProvider))]
	[Name (nameof (TracepointGlyphTag))]
	[ContentType ("code")]
	[TagType (typeof (TracepointGlyphTag))]
	internal class TracepointGlyphFactoryProvider : IGlyphFactoryProvider
	{
		[Import]
		private IImageService imageService = null;
		private readonly ImageId imageId = new ImageId (new System.Guid ("{ae27a6b0-e345-4288-96df-5eaf394ee369}"), 3175);

		public IGlyphFactory GetGlyphFactory (ICocoaTextView view, ICocoaTextViewMargin margin)
		{
			return new ImageSourceGlyphFactory<TracepointGlyphTag> (imageId, imageService);
		}
	}

	[Export (typeof (IGlyphFactoryProvider))]
	[Name (nameof (TracepointDisabledGlyphTag))]
	[ContentType ("code")]
	[TagType (typeof (TracepointDisabledGlyphTag))]
	internal class TracepointDisabledGlyphFactoryProvider : IGlyphFactoryProvider
	{
		[Import]
		private IImageService imageService = null;
		private readonly ImageId imageId = new ImageId (new System.Guid ("{ae27a6b0-e345-4288-96df-5eaf394ee369}"), 3174);

		public IGlyphFactory GetGlyphFactory (ICocoaTextView view, ICocoaTextViewMargin margin)
		{
			return new ImageSourceGlyphFactory<TracepointDisabledGlyphTag> (imageId, imageService);
		}
	}

	[Export (typeof (IGlyphFactoryProvider))]
	[Name (nameof (TracepointInvalidGlyphTag))]
	[ContentType ("code")]
	[TagType (typeof (TracepointInvalidGlyphTag))]
	internal class TracepointInvalidGlyphFactoryProvider : IGlyphFactoryProvider
	{
		[Import]
		private IImageService imageService = null;
		private readonly ImageId imageId = new ImageId (new System.Guid ("{ae27a6b0-e345-4288-96df-5eaf394ee369}"), 3178);

		public IGlyphFactory GetGlyphFactory (ICocoaTextView view, ICocoaTextViewMargin margin)
		{
			return new ImageSourceGlyphFactory<TracepointInvalidGlyphTag> (imageId, imageService);
		}
	}
}
