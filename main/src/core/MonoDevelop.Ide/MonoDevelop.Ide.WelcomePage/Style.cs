using System;

namespace MonoDevelop.Ide.WelcomePage
{
	public static class Styles
	{
		public static class WelcomeScreen
		{
			public const string FontFamilyMac = "Lucida Grande";
			public const string FontFamilyWindows = "Calibri"; // TODO: VV: "Segoe UI"
			public const int VerticalPadding = 24;
			public const int HorizontalPadding = 50;
			public static string BackgroundColor { get; internal set; }
			public const string BackgroundTile = "./images/tiny_grid.png";
			public static string InnerShadowColor { get; internal set; }
			public const double InnerShadowOpacity = 0.4;
			public const int InnerShadowSize = 10;
			public static int Spacing = 20;

			public static class Links
			{
				public static string Color { get; internal set; }
				public static string HoverColor { get; internal set; }
				public const int FontSize = 16;
				public const int LinkSeparation = 24;
				public const int BottomMargin = 24;
				public const int IconTextSpacing = 8;
			}

			public static class Pad
			{
				public const string TitleFontFamilyMac = "Lucida Grande";
				public const string TitleFontFamilyWindows = "Calibri"; // TODO: VV: "Segoe UI"
				public const int Padding = 20;
				public static string BackgroundColor { get; internal set; }
				public static string BorderColor { get; internal set; }
				public static string TextColor { get; internal set; }
				public static string ShadowColor { get; internal set; }
				public const double ShadowOpacity = 0.2;
				public const int ShadowSize = 3;
				public const int ShadowVerticalOffset = 1;
				public const int LargeTitleFontSize = 22;
				public static string LargeTitleFontColor { get; internal set; }
				public const int LargeTitleMarginBottom = 10;
				public static string MediumTitleColor { get; internal set; }
				public const int MediumTitleFontSize = 15;
				public const int MediumTitleMarginBottom = 2;
				public static string SmallTitleColor { get; internal set; }
				public const int SmallTitleFontSize = 13;
				public const int SummaryFontSize = 12;
				public const string SummaryFontFamily = "Arial"; // TODO: VV: "Segoe UI"
				public const int SummaryLineHeight = 19;
				public const int SummaryParagraphMarginTop = 8;

				public static class FeaturedApp
				{
					public const int Width = 400;

					public static class Preview
					{
						public const int VerticalMargin = 20;
					}
				}

				public static class News
				{
					public const int Width = 500;

					public static class Item
					{
						public const int MarginBottom = 26;
						public static string TitleHoverColor { get; internal set; }
						public const int FirstMarginTop = 18;
					}
				}

				public static class Solutions
				{
					public const int LargeTitleMarginBottom = 14;

					public static class SolutionTile
					{
						public const int Width = 260;
						public const int Height = 46;
						public static string HoverBackgroundColor { get; internal set; }
						public static string HoverBorderColor { get; internal set; }
						public const int TitleFontSize = 12;
						public const int PathFontSize = 11;
						public const int TextLeftPadding = 38;
						public const int TitleBottomMargin = 0;

						public static class PinButton
						{
							public const string NormalImage = "unstar-16.png";
							public const string NormalHoverImage = "unstar-hover-16.png";
							public const string PinnedImage = "star-16.png";
							public const string PinnedHoverImage = "star-hover-16.png";
						}
					}
				}
			}

			static WelcomeScreen ()
			{
				LoadStyles ();
				MonoDevelop.Ide.Gui.Styles.Changed +=  (o, e) => LoadStyles ();
			}

			public static void LoadStyles ()
			{
				if (IdeApp.Preferences.UserInterfaceSkin == Skin.Light) {
					BackgroundColor = "white";
					InnerShadowColor = "black";
					Links.Color = "#555555";
					Links.HoverColor = "#000000";
					Pad.BackgroundColor = "#FFF";
					Pad.BorderColor = "#CCC";
					Pad.TextColor = "#555555";
					Pad.ShadowColor = "#000";
					Pad.LargeTitleFontColor = "#444444";
					Pad.MediumTitleColor = "#222222";
					Pad.SmallTitleColor = "#777777";
					Pad.News.Item.TitleHoverColor = "#0982B3";
					Pad.Solutions.SolutionTile.HoverBackgroundColor = "#f9feff";
					Pad.Solutions.SolutionTile.HoverBorderColor = "#dddddd";
				} else {
					BackgroundColor = "black";
					InnerShadowColor = "white";
					Links.Color = "#555555";
					Links.HoverColor = "#FFF";
					Pad.BackgroundColor = "#222";
					Pad.BorderColor = "#CCC";
					Pad.TextColor = "#555555";
					Pad.ShadowColor = "#000";
					Pad.LargeTitleFontColor = "#444444";
					Pad.MediumTitleColor = "#444444";
					Pad.SmallTitleColor = "#777777";
					Pad.News.Item.TitleHoverColor = "#0982B3";
					Pad.Solutions.SolutionTile.HoverBackgroundColor = "#f9feff";
					Pad.Solutions.SolutionTile.HoverBorderColor = "#dddddd";
				}
			}
		}

		public static string GetFormatString (string fontFace, int fontSize, string color, Pango.Weight weight = Pango.Weight.Normal)
		{
			return "<span font=\"" + fontFace + " " + fontSize + "px\" foreground=\"" + color + "\" font_weight=\"" + weight + "\">{0}</span>";
		}

	}
}
