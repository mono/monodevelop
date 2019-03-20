using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.Debugger
{
	[Export (typeof (IViewTaggerProvider))]
	[ContentType ("text")]
	[TagType (typeof (BreakpointTag))]
	[TagType (typeof (BreakpointDisabledTag))]
	[TagType (typeof (BreakpointInvalidTag))]
	internal class BreakpointTaggerProvider : IViewTaggerProvider
	{
		public ITagger<T> CreateTagger<T> (ITextView textView, ITextBuffer buffer) where T : ITag
		{
			return new BreakpointTagger (textView, BreakpointManagerService.GetBreakpointManager (textView)) as ITagger<T>;
		}
	}
}
