using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace MonoDevelop.Debugger
{
	internal class ReturnStatementGlyphTagger : AbstractCurrentStatementTagger<ReturnStatementGlyphTag>
	{
		public ReturnStatementGlyphTagger (ITextView textView)
			: base (ReturnStatementGlyphTag.Instance, textView, isGreen: true)
		{
		}
	}
}
