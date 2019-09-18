using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace MonoDevelop.Debugger
{
	internal class CurrentStatementGlyphTagger : AbstractCurrentStatementTagger<CurrentStatementGlyphTag>
	{
		public CurrentStatementGlyphTagger (ITextView textView)
			: base (CurrentStatementGlyphTag.Instance, textView, isGreen: false)
		{
		}
	}
}
