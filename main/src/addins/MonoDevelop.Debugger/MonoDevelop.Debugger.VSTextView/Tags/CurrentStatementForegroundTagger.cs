using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace MonoDevelop.Debugger
{
	internal class CurrentStatementForegroundTagger : AbstractCurrentStatementTagger<ClassificationTag>
	{
		public CurrentStatementForegroundTagger (
			ClassificationTag tag,
			ITextBuffer textBuffer,
			bool isGreen)
			: base (tag, textBuffer, isGreen)
		{
		}
	}
}
