#if MAC
using AppKit;
using MonoDevelop.Ide;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	static class Styles
	{
		public static NSColor SectionForegroundColor { get; internal set; } = NSColor.Text;
		public static NSColor SectionBackgroundColor { get; private set; } = NSColor.ControlLightHighlight;

		public static NSColor CellBackgroundSelectedColor { get; private set; } = NSColor.SelectedTextBackground;
		public static NSColor CellBackgroundColor { get; private set; } = NSColor.ControlBackground;
	}
}
#endif