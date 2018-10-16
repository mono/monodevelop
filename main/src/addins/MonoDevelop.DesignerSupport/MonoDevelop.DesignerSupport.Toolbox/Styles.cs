using AppKit;
using MonoDevelop.Ide;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	static class Styles
	{
		static Styles ()
		{
			LoadStyles ();
			Ide.Gui.Styles.Changed += (o, e) => LoadStyles ();
		}

		public static NSColor HeaderForegroundColor { get; private set; }
		public static NSColor HeaderCellBackgroundSelectedColor { get; private set; }

		public static NSColor CellBackgroundSelectedColor { get; private set; }
		public static NSColor CellBorderSelectedColor { get; private set; }
		public static NSColor CellBackgroundColor { get; private set; }

		public static int SearchTextFieldLineBorderWidth { get; private set; }
		public static NSColor SearchTextFieldLineBorderColor { get; private set; }
		public static NSColor SearchTextFieldLineBackgroundColor { get; private set; }

		public static NSColor ClickedButtonFocusedBackgroundColor { get; private set; }
		public static NSColor ClickedButtonFocusedBorderColor { get; private set; }

		public static NSColor ToggleButtonFocusedBackgroundColor { get; private set; }
		public static NSColor ToggleButtonHoverBorderColor { get; private set; }
		public static NSColor ToggleButtonHoverClickedBackgroundColor { get; private set; }
		public static NSColor ToggleButtonHoverBackgroundColor { get; private set; }
		public static int ToggleButtonCornerRadius = 5;
		public static float ToggleButtonLineWidth = 0.5f;

		public static void LoadStyles ()
		{
			SearchTextFieldLineBorderWidth = 1;
			if (IdeApp.Preferences.UserInterfaceTheme == Theme.Light) {
				SearchTextFieldLineBorderColor = NSColor.Black;
				SearchTextFieldLineBackgroundColor = NSColor.FromRgba (red: 0.25f, green: 0.25f, blue: 0.25f, alpha: 1.0f);

				ToggleButtonHoverBorderColor = NSColor.Black;
				ToggleButtonHoverClickedBackgroundColor = NSColor.FromRgba (red: 0.22f, green: 0.22f, blue: 0.22f, alpha: 1.0f);
				ToggleButtonHoverBackgroundColor = NSColor.FromRgba (red: 0.25f, green: 0.25f, blue: 0.25f, alpha: 1.0f);

				ToggleButtonFocusedBackgroundColor = NSColor.FromRgba (red: 0.25f, green: 0.25f, blue: 0.25f, alpha: 1.0f);

				CellBackgroundSelectedColor = NSColor.FromRgba (red: 0.33f, green: 0.55f, blue: 0.92f, alpha: 1.0f);
				CellBorderSelectedColor = NSColor.Black;
				CellBackgroundColor = NSColor.FromRgba (red: 0.25f, green: 0.25f, blue: 0.25f, alpha: 1.0f);

				HeaderCellBackgroundSelectedColor = NSColor.FromRgb (0.29f, green: 0.29f, blue: 0.29f);
				HeaderForegroundColor = NSColor.White;

				ClickedButtonFocusedBackgroundColor = NSColor.FromRgba (red: 0.25f, green: 0.25f, blue: 0.25f, alpha: 1.0f);
				ClickedButtonFocusedBorderColor = NSColor.FromRgba (red: 0.21f, green: 0.21f, blue: 0.21f, alpha: 1.0f);

			} else {
				SearchTextFieldLineBorderColor = NSColor.Black;
				SearchTextFieldLineBackgroundColor = NSColor.FromRgba (red: 0.25f, green: 0.25f, blue: 0.25f, alpha: 1.0f);

				ToggleButtonHoverBorderColor = NSColor.Black;
				ToggleButtonHoverClickedBackgroundColor = NSColor.FromRgba (red: 0.22f, green: 0.22f, blue: 0.22f, alpha: 1.0f);
				ToggleButtonHoverBackgroundColor = NSColor.FromRgba (red: 0.25f, green: 0.25f, blue: 0.25f, alpha: 1.0f);
				ToggleButtonFocusedBackgroundColor = NSColor.FromRgba (red: 0.25f, green: 0.25f, blue: 0.25f, alpha: 1.0f);

				CellBackgroundSelectedColor = NSColor.FromRgba (red: 0.33f, green: 0.55f, blue: 0.92f, alpha: 1.0f);
				CellBorderSelectedColor = NSColor.Black;
				CellBackgroundColor = NSColor.FromRgba (red: 0.25f, green: 0.25f, blue: 0.25f, alpha: 1.0f);

				HeaderCellBackgroundSelectedColor = NSColor.FromRgb (0.29f, green: 0.29f, blue: 0.29f);
				HeaderForegroundColor = NSColor.White;

				ClickedButtonFocusedBackgroundColor = NSColor.FromRgba (red: 0.25f, green: 0.25f, blue: 0.25f, alpha: 1.0f);
				ClickedButtonFocusedBorderColor = NSColor.FromRgba (red: 0.21f, green: 0.21f, blue: 0.21f, alpha: 1.0f);

			}
		}
	}
}
