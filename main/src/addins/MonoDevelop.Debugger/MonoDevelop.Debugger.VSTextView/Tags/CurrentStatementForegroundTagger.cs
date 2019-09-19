using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace MonoDevelop.Debugger
{
	internal class CurrentStatementForegroundTagger : AbstractCurrentStatementTagger<ClassificationTag>
	{
		public CurrentStatementForegroundTagger (
			ClassificationTag tag,
			ITextView textView,
			bool isGreen)
			: base (tag, textView, isGreen)
		{
		}
	}
}
