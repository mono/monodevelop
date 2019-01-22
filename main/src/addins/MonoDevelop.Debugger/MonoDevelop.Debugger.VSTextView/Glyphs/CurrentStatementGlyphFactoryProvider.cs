using System.ComponentModel.Composition;
using Microsoft.Ide.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.Debugger
{
	[Export (typeof (IGlyphFactoryProvider))]
	[Name ("CurrentStatementGlyph")]
	[Order (After = "BreakpointGlyph")]
	[ContentType ("code")]
	[TagType (typeof (CurrentStatementGlyphTag))]
	internal class CurrentStatementGlyphFactoryProvider : IGlyphFactoryProvider
	{
		public IGlyphFactory GetGlyphFactory (ICocoaTextView view, ICocoaTextViewMargin margin)
		{
			return new ImageSourceGlyphFactory<CurrentStatementGlyphTag> ("md-gutter-execution");
		}
	}
}
