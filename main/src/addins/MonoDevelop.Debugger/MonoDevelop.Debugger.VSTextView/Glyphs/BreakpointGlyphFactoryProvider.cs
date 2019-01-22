using System.ComponentModel.Composition;
using Microsoft.Ide.Editor;
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
		public IGlyphFactory GetGlyphFactory (ICocoaTextView view, ICocoaTextViewMargin margin)
		{
			return new ImageSourceGlyphFactory<BreakpointGlyphTag> ("md-breakpoint");
		}
	}

	[Export (typeof (IGlyphFactoryProvider))]
	[Name (nameof (BreakpointDisabledGlyphTag))]
	[ContentType ("code")]
	[TagType (typeof (BreakpointDisabledGlyphTag))]
	internal class BreakpointDisabledGlyphFactoryProvider : IGlyphFactoryProvider
	{
		public IGlyphFactory GetGlyphFactory (ICocoaTextView view, ICocoaTextViewMargin margin)
		{
			return new ImageSourceGlyphFactory<BreakpointDisabledGlyphTag> ("md-breakpoint-disabled");
		}
	}

	[Export (typeof (IGlyphFactoryProvider))]
	[Name (nameof (BreakpointInvalidGlyphTag))]
	[ContentType ("code")]
	[TagType (typeof (BreakpointInvalidGlyphTag))]
	internal class BreakpointInvalidGlyphFactoryProvider : IGlyphFactoryProvider
	{
		public IGlyphFactory GetGlyphFactory (ICocoaTextView view, ICocoaTextViewMargin margin)
		{
			return new ImageSourceGlyphFactory<BreakpointInvalidGlyphTag> ("md-breakpoint-invalid");
		}
	}

	[Export (typeof (IGlyphFactoryProvider))]
	[Name (nameof (TracepointGlyphTag))]
	[ContentType ("code")]
	[TagType (typeof (TracepointGlyphTag))]
	internal class TracepointGlyphFactoryProvider : IGlyphFactoryProvider
	{
		public IGlyphFactory GetGlyphFactory (ICocoaTextView view, ICocoaTextViewMargin margin)
		{
			return new ImageSourceGlyphFactory<TracepointGlyphTag> ("md-gutter-tracepoint");
		}
	}

	[Export (typeof (IGlyphFactoryProvider))]
	[Name (nameof (TracepointDisabledGlyphTag))]
	[ContentType ("code")]
	[TagType (typeof (TracepointDisabledGlyphTag))]
	internal class TracepointDisabledGlyphFactoryProvider : IGlyphFactoryProvider
	{
		public IGlyphFactory GetGlyphFactory (ICocoaTextView view, ICocoaTextViewMargin margin)
		{
			return new ImageSourceGlyphFactory<TracepointDisabledGlyphTag> ("md-gutter-tracepoint-disabled");
		}
	}

	[Export (typeof (IGlyphFactoryProvider))]
	[Name (nameof (TracepointInvalidGlyphTag))]
	[ContentType ("code")]
	[TagType (typeof (TracepointInvalidGlyphTag))]
	internal class TracepointInvalidGlyphFactoryProvider : IGlyphFactoryProvider
	{
		public IGlyphFactory GetGlyphFactory (ICocoaTextView view, ICocoaTextViewMargin margin)
		{
			return new ImageSourceGlyphFactory<TracepointInvalidGlyphTag> ("md-gutter-tracepoint-invalid");
		}
	}
}
