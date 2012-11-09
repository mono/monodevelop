using System;

namespace MonoDevelop.Ide.WelcomePage
{
	public static class Styles
	{
		public static class WelcomeScreen
		{
			public const string FontFamilyMac = "Lucida Grande";
			public const string FontFamilyWindows = "Calibri";
			public const int VerticalPadding = 24;
			public const int HorizontalPadding = 50;
			public const string BackgroundTile = "./images/tiny_grid.png";
			public const string InnerShadowColor = "black";
			public const double InnerShadowOpacity = 0.4;
			public const int InnerShadowSize = 10;
			public static int Spacing = 20;
			
			public static class Links
			{
				public const string Color = "#555555";
				public const string HoverColor = "#000000";
				public const int FontSize = 16;
				public const int LinkSeparation = 24;
				public const int BottomMargin = 24;
				public const int IconTextSpacing = 8;
			}
			
			public static class Pad
			{
				public const string TitleFontFamilyMac = "Lucida Grande";
				public const string TitleFontFamilyWindows = "Calibri";
				public const int Padding = 20;
				public const string BackgroundColor = "#FFF";
				public const string BorderColor = "#CCC";
				public const string TextColor = "#555555";
				public const string ShadowColor = "#000";
				public const double ShadowOpacity = 0.2;
				public const int ShadowSize = 3;
				public const int ShadowVerticalOffset = 1;
				public const int LargeTitleFontSize = 22;
				public const string LargeTitleFontColor = "#444444";
				public const int LargeTitleMarginBottom = 10;
				public const string MediumTitleColor = "#222222";
				public const int MediumTitleFontSize = 15;
				public const int MediumTitleMarginBottom = 2;
				public const string SmallTitleColor = "#777777";
				public const int SmallTitleFontSize = 13;
				public const int SummaryFontSize = 12;
				public const string SummaryFontFamily = "Arial";
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
						public const string TitleHoverColor = "#0982B3";
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
						public const string HoverBackgroundColor = "#f9feff";
						public const string HoverBorderColor = "#dddddd";
						public const int TitleFontSize = 12;
						public const int PathFontSize = 11;
						public const int TextLeftPadding = 38;
						public const int TitleBottomMargin = 0;

						public static class PinButton
						{
							public const string NormalImage = "./images/WelcomeScreen/star-normal.png";
							public const string NormalHoverImage = "./images/WelcomeScreen/star-normal-hover.png";
							public const string PinnedImage = "./images/WelcomeScreen/star-pinned.png";
							public const string PinnedHoverImage = "./images/WelcomeScreen/star-pinned-hover.png";
						}
					}
				}
			}
		}

		public static string GetFormatString (string fontFace, int fontSize, string color, bool bold = false)
		{
			return "<span font=\"" + fontFace + " " + fontSize + "px\" foreground=\"" + color + "\" font_weight=\"" + (bold ? "bold" : "normal") + "\">{0}</span>";
		}
	}
}