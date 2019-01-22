using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.Debugger
{
	[Export (typeof (IViewTaggerProvider))]
	[ContentType ("text")]
	[TagType (typeof (CurrentStatementTag))]
	internal class CurrentStatementTaggerProvider : IViewTaggerProvider
	{
		public ITagger<T> CreateTagger<T> (ITextView textView, ITextBuffer buffer) where T : ITag
		{
			return new CurrentStatementTagger (buffer) as ITagger<T>;
		}
	}
}
