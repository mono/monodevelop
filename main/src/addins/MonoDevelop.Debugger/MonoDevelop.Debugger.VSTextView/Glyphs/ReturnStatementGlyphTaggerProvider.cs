using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.Debugger
{
	[Export (typeof (IViewTaggerProvider))]
	[ContentType ("code")]
	[TagType (typeof (ReturnStatementGlyphTag))]
	internal class ReturnStatementGlyphTaggerProvider : IViewTaggerProvider
	{
		public ITagger<T> CreateTagger<T> (ITextView textView, ITextBuffer buffer) where T : ITag
		{
			return new ReturnStatementGlyphTagger (buffer) as ITagger<T>;
		}
	}
}
