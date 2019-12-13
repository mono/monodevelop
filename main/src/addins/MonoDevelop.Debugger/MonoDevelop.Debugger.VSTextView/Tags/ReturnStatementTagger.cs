using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace MonoDevelop.Debugger
{
	internal class ReturnStatementTagger : AbstractCurrentStatementTagger<ReturnStatementTag>
	{
		public ReturnStatementTagger (ITextView textView)
			: base (ReturnStatementTag.Instance, textView, isGreen: true)
		{
		}
	}
}
