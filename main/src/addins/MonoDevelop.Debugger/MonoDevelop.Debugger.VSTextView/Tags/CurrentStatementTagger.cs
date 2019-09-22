using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace MonoDevelop.Debugger
{
	internal class CurrentStatementTagger : AbstractCurrentStatementTagger<CurrentStatementTag>
	{
		public CurrentStatementTagger (ITextView textView)
			: base (CurrentStatementTag.Instance, textView, isGreen: false)
		{
		}
	}
}
