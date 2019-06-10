#if MAC
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.Debugger
{
	[Export (typeof (IGlyphMouseProcessorProvider))]
	[Name ("BreakpointGlyphMouseProcessorProvider")]
	[Order]
	[ContentType ("any")]
	class BreakpointGlyphMouseProcessorProvider : IGlyphMouseProcessorProvider
	{
		[Import]
		private IViewTagAggregatorFactoryService viewTagAggregatorService = null;

		[Import]
		private IEditorPrimitivesFactoryService editorPrimitivesFactoryService = null;

		public ICocoaMouseProcessor GetAssociatedMouseProcessor (
			ICocoaTextViewHost wpfTextViewHost,
			ICocoaTextViewMargin margin)
		{
			return new BreakpointGlyphMouseProcessor (
				wpfTextViewHost,
				margin,
				viewTagAggregatorService.CreateTagAggregator<IGlyphTag> (wpfTextViewHost.TextView),
				editorPrimitivesFactoryService.GetViewPrimitives (wpfTextViewHost.TextView));
		}
	}
}
#endif