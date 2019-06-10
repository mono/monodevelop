#if MAC
using AppKit;
using MonoDevelop.Core.FeatureConfiguration;
using MonoDevelop.Ide;
using Xamarin.PropertyEditing.Mac;

namespace MonoDevelop.DesignerSupport
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

		public static PropertyPadStyle PropertyPad { get; private set; }

		// Used for the property panel in Xamarin.PropertyEditing
		public class PropertyPadStyle
		{
			public NSColor Checkerboard0 { get; internal set; }
			public NSColor Checkerboard1 { get; internal set; }
			public NSColor ValueBlockBackgroundColor { get; internal set; }
			public NSColor TabBorderColor { get; internal set; }
			public NSColor PanelTabBackground { get; internal set; }
		}

		static Styles ()
		{
			LoadStyles ();
			Ide.Gui.Styles.Changed += (o, e) => LoadStyles ();
		}

		public static void LoadStyles ()
		{
			if (IdeApp.Preferences.UserInterfaceTheme == Theme.Light) {
				HeaderBackgroundColor = NSColor.FromRgb (0.98f, 0.98f, 0.98f);
				HeaderBorderBackgroundColor = NSColor.FromRgb (0.96f, 0.96f, 0.96f);
				LabelSelectedForegroundColor = NSColor.Highlight;
				ToolbarBackgroundColor = NSColor.White;
				CellBackgroundSelectedColor = NSColor.FromRgb (0.36f, 0.54f, 0.90f);

				PropertyPad = new PropertyPadStyle {
					Checkerboard0 = NSColor.FromRgb (255, 255, 255),
					Checkerboard1 = NSColor.FromRgb (217, 217, 217),
					PanelTabBackground = NSColor.FromRgb (248, 247, 248),
					TabBorderColor = NSColor.FromRgba (0, 0, 0, 25),
					ValueBlockBackgroundColor = NSColor.FromRgba (0, 0, 0, 20)
				};
			} else {
				CellBackgroundSelectedColor = NSColor.FromRgb (0.38f, 0.55f, 0.91f);
				HeaderBackgroundColor = NSColor.FromRgb (0.29f, 0.29f, 0.29f);
				HeaderBorderBackgroundColor = NSColor.FromRgb (0.29f, 0.29f, 0.29f);
				LabelSelectedForegroundColor = NSColor.SelectedText;
				ToolbarBackgroundColor = NSColor.FromRgb (0.25f, 0.25f, 0.25f);

				PropertyPad = new PropertyPadStyle {
					Checkerboard0 = NSColor.FromRgb (38, 38, 38),
					Checkerboard1 = NSColor.FromRgb (0, 0, 0),
					PanelTabBackground = NSColor.FromRgb (85, 85, 85),
					TabBorderColor = NSColor.FromRgba (255, 255, 255, 0),
					ValueBlockBackgroundColor = NSColor.FromRgba (255, 255, 255, 25)
				};
			}
		}
	}
}
#endif
