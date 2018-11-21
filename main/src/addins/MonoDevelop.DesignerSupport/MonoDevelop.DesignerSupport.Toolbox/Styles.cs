#if MAC
using AppKit;
using MonoDevelop.Ide;
using Xamarin.PropertyEditing.Mac;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	static class Styles
	{
		public static NSColor SectionForegroundColor { get; internal set; } = NSColor.Text;
		public static NSColor SectionBackgroundColor { get; private set; } = NSColor.ControlLightHighlight;

		public static NSColor CellBackgroundSelectedColor { get; private set; } 
		public static NSColor CellBackgroundUnfocusedSelectedColor { get; private set; } = NSColor.FromRgb (0.64f, 0.64f, 0.64f);
		public static NSColor CellBackgroundColor { get; private set; } = NSColor.ControlBackground;
		public static NSColor LabelSelectedForegroundColor { get; private set; }

		public static NSColor HeaderBackgroundColor { get; private set; }
		public static NSColor HeaderBorderBackgroundColor { get; private set; }

		public static NSColor ToolbarBackgroundColor { get; private set; }

		static Styles ()
		{
			LoadStyles ();
			Ide.Gui.Styles.Changed += (o, e) => LoadStyles ();
		}

		public static void LoadStyles ()
		{
			if (IdeApp.Preferences.UserInterfaceTheme == Theme.Light) {
				PropertyEditorPanel.ThemeManager.Theme = Xamarin.PropertyEditing.Themes.PropertyEditorTheme.Light;
				HeaderBackgroundColor = NSColor.FromRgb (0.98f, 0.98f, 0.98f);
				HeaderBorderBackgroundColor = NSColor.FromRgb (0.96f, 0.96f, 0.96f);
				LabelSelectedForegroundColor = NSColor.Highlight;
				ToolbarBackgroundColor = NSColor.White;
				CellBackgroundSelectedColor = NSColor.FromRgb (0.36f, 0.54f, 0.90f);
			} else {
				PropertyEditorPanel.ThemeManager.Theme = Xamarin.PropertyEditing.Themes.PropertyEditorTheme.Dark;
				CellBackgroundSelectedColor = NSColor.FromRgb (0.38f, 0.55f, 0.91f);
				HeaderBackgroundColor = NSColor.FromRgb (0.29f, 0.29f, 0.29f);
				HeaderBorderBackgroundColor = NSColor.FromRgb (0.29f, 0.29f, 0.29f);
				LabelSelectedForegroundColor = NSColor.SelectedText;
				ToolbarBackgroundColor = NSColor.FromRgb (0.25f, 0.25f, 0.25f);
			}
		}
	}
}
#endif