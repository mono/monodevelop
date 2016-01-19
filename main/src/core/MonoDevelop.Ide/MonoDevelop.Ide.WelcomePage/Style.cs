using System;

namespace MonoDevelop.Ide.WelcomePage
{
	public static class Styles
	{
		public static class WelcomeScreen
		{
			public const string FontFamilyMac = "Sans";
			public const string FontFamilyWindows = "Sans";
			public const int VerticalPadding = 24; // TODO: VV: Seems to be unused
			public const int HorizontalPadding = 50; // TODO: VV: Seems to be unused
			public static string BackgroundColor { get; internal set; }
			public const string BackgroundTile = "./images/tiny_grid.png"; // TODO: VV: Seems to be unused
			public static string InnerShadowColor { get; internal set; }
			public const double InnerShadowOpacity = 0.4; // TODO: VV: Seems to be unused
			public const int InnerShadowSize = 10; // TODO: VV: Seems to be unused
			public static int Spacing = 30; // space between header and pads

			public static class Links
			{
				public static string Color { get; internal set; }
				public static string HoverColor { get; internal set; }
				public const int FontSize = 12;
				public const int LinkSeparation = 30;
				public const int BottomMargin = 24; // TODO: VV: Seems to be unused
				public const int IconTextSpacing = 4;
			}

			public static class Pad
			{
				public const string TitleFontFamilyMac = "Sans";
				public const string TitleFontFamilyWindows = "Sans";
				public const int Padding = 20;
				public static string BackgroundColor { get; internal set; }
				public static string BorderColor { get; internal set; }
				public static string TextColor { get; internal set; }
				public static string ShadowColor { get; internal set; }
				public const double ShadowOpacity = 0.2; // TODO: VV: Seems to be unused
				public const int ShadowSize = 2;
				public const int ShadowVerticalOffset = 1;

				public const int LargeTitleFontSize = 22;
				public static string LargeTitleFontColor { get; internal set; }
				public const int LargeTitleMarginBottom = 22;

				public static string MediumTitleColor { get; internal set; }
				public const int MediumTitleFontSize = 12;
				public const int MediumTitleMarginBottom = 7;

				public static string SmallTitleColor { get; internal set; }
				public const int SmallTitleFontSize = 10;

				public const int SummaryFontSize = 11;
				public const string SummaryFontFamily = "Sans";
				public const int SummaryLineHeight = 19; // TODO: VV: Seems to be unused
				public const int SummaryParagraphMarginTop = 8;

				public static class FeaturedApp
				{
					public const int Width = 370;

					public static class Preview
					{
						public const int VerticalMargin = 20;
					}
				}

				public static class News
				{
					public const int Width = 470;

					public static class Item
					{
						public const int MarginBottom = 26;
						public static string TitleHoverColor { get; internal set; }
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
						public const int PathFontSize = 10;
						public const int TextLeftPadding = 38;
						public const int TitleBottomMargin = 4;

						// TODO: VV: Seems to be unused
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
					BackgroundColor = "#fff";
					InnerShadowColor = "#fff";

					Links.Color = "#888";
					Links.HoverColor = "#555";

					Pad.BackgroundColor = "#fafafa";
					Pad.BorderColor = "#fafafa";
					Pad.TextColor = "#555";
					Pad.ShadowColor = "#000";
					Pad.LargeTitleFontColor = "#555";
					Pad.MediumTitleColor = "#555";
					Pad.SmallTitleColor = "#aaa";

					Pad.News.Item.TitleHoverColor = "#3496d9";

					Pad.Solutions.SolutionTile.HoverBackgroundColor = "#eee";
					Pad.Solutions.SolutionTile.HoverBorderColor = "#eee";
				} else {
					BackgroundColor = "#000";
					InnerShadowColor = "#000";

					Links.Color = "#868686";
					Links.HoverColor = "#bebebe";

					Pad.BackgroundColor = "#222";
					Pad.BorderColor = "#222";
					Pad.TextColor = "#868686";
					Pad.ShadowColor = "#000";
					Pad.LargeTitleFontColor = "#868686";
					Pad.MediumTitleColor = "#bdbdbd";
					Pad.SmallTitleColor = "#666";

					Pad.News.Item.TitleHoverColor = "#5babed";

					Pad.Solutions.SolutionTile.HoverBackgroundColor = "#333";
					Pad.Solutions.SolutionTile.HoverBorderColor = "#333";
				}
			}
		}

		public static string GetFormatString (string fontFace, int fontSize, string color, Pango.Weight weight = Pango.Weight.Normal)
		{
			return "<span font=\"" + fontFace + " " + fontSize + "px\" foreground=\"" + color + "\" font_weight=\"" + weight + "\">{0}</span>";
		}

	}
}
