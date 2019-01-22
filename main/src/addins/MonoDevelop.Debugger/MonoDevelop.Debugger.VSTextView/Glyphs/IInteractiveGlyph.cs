using AppKit;

namespace MonoDevelop.Debugger
{
	public interface IInteractiveGlyph
	{
		NSCursor HoverCursor { get; }
		IActiveGlyphDropHandler DropHandler { get; }
		bool IsEnabled { get; }
		bool ExecuteCommand (GlyphCommandType markerCommand);
		string TooltipText { get; }
	}
}
