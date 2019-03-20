using Microsoft.VisualStudio.Text.Editor;

namespace MonoDevelop.Debugger
{
	internal class CurrentStatementGlyphTag : IGlyphTag
	{
		public static readonly CurrentStatementGlyphTag Instance = new CurrentStatementGlyphTag ();
	}
}
