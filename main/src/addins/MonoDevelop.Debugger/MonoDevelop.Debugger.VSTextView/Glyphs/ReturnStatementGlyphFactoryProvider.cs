using System.ComponentModel.Composition;
using Microsoft.Ide.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.Debugger
{
	[Export (typeof (IGlyphFactoryProvider))]
	[Name ("ReturnStatementGlyph")]
	[Order (After = "CurrentStatementGlyph")]
	[ContentType ("code")]
	[TagType (typeof (ReturnStatementGlyphTag))]
	internal class ReturnStatementGlyphFactoryProvider : IGlyphFactoryProvider
	{
		public IGlyphFactory GetGlyphFactory (ICocoaTextView view, ICocoaTextViewMargin margin)
		{
			return new ImageSourceGlyphFactory<ReturnStatementGlyphTag> ("md-gutter-stack");
		}
	}
}
