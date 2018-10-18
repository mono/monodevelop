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

		public static NSColor ClickedButtonFocusedBorderColor { get; internal set; }
		public static NSColor ClickedButtonFocusedBackgroundColor { get; private set; }
		public static NSColor ClickedButtonHoverBorderColor { get; private set; }
		public static NSColor ClickedButtonHoverBackgroundColor { get; private set; }
		public static NSColor ClickedButtonHoverFocusedBorderColor { get; internal set; }
		public static NSColor ClickedButtonHoverFocusedBackgroundColor { get; internal set; }

		public static NSColor ToggleButtonFocusedBorderColor { get; internal set; }
		public static NSColor ToggleButtonFocusedBackgroundColor { get; private set; }
		public static NSColor ToggleButtonHoverBorderColor { get; private set; }
		public static NSColor ToggleButtonHoverBackgroundColor { get; private set; }
		public static NSColor ToggleButtonHoverFocusedBorderColor { get; internal set; }
		public static NSColor ToggleButtonHoverFocusedBackgroundColor { get; internal set; }
		public static NSColor ToggleButtonOnFocusedBackgroundColor { get; internal set; }
		public static NSColor ToggleButtonOnFocusedBorderColor { get; internal set; }
		public static NSColor ToggleButtonOnBackgroundColor { get; internal set; }
		public static NSColor ToggleButtonOnBorderColor { get; internal set; }

		public static int ToggleButtonCornerRadius = 5;
		public static float ToggleButtonLineWidth = 1;

		public static void LoadStyles ()
		{
			SearchTextFieldLineBorderWidth = 1;

			ToggleButtonFocusedBorderColor = NSColor.FromRgba (red: 0.16f, green: 0.16f, blue: 0.16f, alpha: 1.0f);
			ToggleButtonFocusedBackgroundColor = NSColor.FromRgba (red: 0.29f, green: 0.29f, blue: 0.29f, alpha: 1.0f);

			ToggleButtonHoverBorderColor = NSColor.FromRgba (red: 0.13f, green: 0.13f, blue: 0.13f, alpha: 1.0f);
			ToggleButtonHoverBackgroundColor = NSColor.FromRgba (red: 0.25f, green: 0.25f, blue: 0.25f, alpha: 1.0f);

			ToggleButtonHoverFocusedBorderColor = NSColor.FromRgba (red: 0.10f, green: 0.10f, blue: 0.10f, alpha: 1.0f);
			ToggleButtonHoverFocusedBackgroundColor = NSColor.FromRgba (red: 0.22f, green: 0.22f, blue: 0.22f, alpha: 1.0f);

			ToggleButtonOnFocusedBorderColor = NSColor.FromRgba (red: 0.11f, green: 0.11f, blue: 0.11f, alpha: 1.0f);
			ToggleButtonOnFocusedBackgroundColor = NSColor.FromRgba (red: 0.23f, green: 0.23f, blue: 0.23f, alpha: 1.0f);
			ToggleButtonOnBorderColor = NSColor.FromRgba (red: 0.10f, green: 0.10f, blue: 0.10f, alpha: 1.0f);
			ToggleButtonOnBackgroundColor = NSColor.FromRgba (red: 0.25f, green: 0.25f, blue: 0.25f, alpha: 1.0f);

			ClickedButtonFocusedBorderColor = NSColor.FromRgba (red: 0.29f, green: 0.29f, blue: 0.29f, alpha: 1.0f);
			ClickedButtonFocusedBackgroundColor = NSColor.FromRgba (red: 0.31f, green: 0.31f, blue: 0.31f, alpha: 1.0f);
			ClickedButtonHoverBorderColor = NSColor.FromRgba (red: 0.20f, green: 0.19f, blue: 0.20f, alpha: 1.0f);
			ClickedButtonHoverBackgroundColor = NSColor.FromRgba (red: 0.35f, green: 0.35f, blue: 0.35f, alpha: 1.0f);
			ClickedButtonHoverFocusedBorderColor = NSColor.FromRgba (red: 0.26f, green: 0.26f, blue: 0.26f, alpha: 1.0f);
			ClickedButtonHoverFocusedBackgroundColor = NSColor.FromRgba (red: 0.34f, green: 0.34f, blue: 0.34f, alpha: 1.0f);

			SearchTextFieldLineBorderColor = NSColor.Black;
			SearchTextFieldLineBackgroundColor = NSColor.FromRgba (red: 0.25f, green: 0.25f, blue: 0.25f, alpha: 1.0f);

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
