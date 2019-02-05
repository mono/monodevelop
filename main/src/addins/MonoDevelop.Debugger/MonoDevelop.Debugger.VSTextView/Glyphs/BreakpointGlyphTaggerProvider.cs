using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.Debugger
{
	[Export (typeof (IViewTaggerProvider))]
	[ContentType ("code")]
	[TagType (typeof (BreakpointGlyphTag))]
	[TagType (typeof (BreakpointDisabledGlyphTag))]
	[TagType (typeof (BreakpointInvalidGlyphTag))]
	[TagType (typeof (TracepointGlyphTag))]
	[TagType (typeof (TracepointDisabledGlyphTag))]
	[TagType (typeof (TracepointInvalidGlyphTag))]
	internal class BreakpointGlyphTaggerProvider : IViewTaggerProvider
	{
		[Import]
		public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

		public ITagger<T> CreateTagger<T> (ITextView textView, ITextBuffer buffer) where T : ITag
		{
			return new BreakpointGlyphTagger (TextDocumentFactoryService, textView, BreakpointManagerService.GetBreakpointManager (textView)) as ITagger<T>;
		}
	}
}
