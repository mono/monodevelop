using Microsoft.VisualStudio.Text;

namespace MonoDevelop.Debugger
{
	internal class ReturnStatementTagger : AbstractCurrentStatementTagger<ReturnStatementTag>
	{
		public ReturnStatementTagger (ITextBuffer textBuffer)
			: base (ReturnStatementTag.Instance, textBuffer, isGreen: true)
		{
		}
	}
}
