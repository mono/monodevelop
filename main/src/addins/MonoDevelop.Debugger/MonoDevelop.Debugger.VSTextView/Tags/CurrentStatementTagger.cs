using Microsoft.VisualStudio.Text;

namespace MonoDevelop.Debugger
{
	internal class CurrentStatementTagger : AbstractCurrentStatementTagger<CurrentStatementTag>
	{
		public CurrentStatementTagger (ITextBuffer textBuffer)
			: base (CurrentStatementTag.Instance, textBuffer, isGreen: false)
		{
		}
	}
}
