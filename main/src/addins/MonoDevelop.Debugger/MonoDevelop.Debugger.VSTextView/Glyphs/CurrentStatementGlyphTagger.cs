using Microsoft.VisualStudio.Text;

namespace MonoDevelop.Debugger
{
	internal class CurrentStatementGlyphTagger : AbstractCurrentStatementTagger<CurrentStatementGlyphTag>
	{
		public CurrentStatementGlyphTagger (ITextBuffer textBuffer)
			: base (CurrentStatementGlyphTag.Instance, textBuffer, isGreen: false)
		{
		}
	}
}
