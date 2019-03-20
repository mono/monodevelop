namespace MonoDevelop.Debugger
{
	interface IInteractiveGlyph
	{
		object HoverCursor { get; }
		IActiveGlyphDropHandler DropHandler { get; }
		bool IsEnabled { get; }
		bool ExecuteCommand (GlyphCommandType markerCommand);
		string TooltipText { get; }
	}
}
