using Microsoft.VisualStudio.Text;

namespace MonoDevelop.Debugger
{
	internal class ReturnStatementGlyphTagger : AbstractCurrentStatementTagger<ReturnStatementGlyphTag>
	{
		public ReturnStatementGlyphTagger (ITextBuffer textBuffer)
			: base (ReturnStatementGlyphTag.Instance, textBuffer, isGreen: true)
		{
		}
	}
}
