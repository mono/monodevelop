// 
// Styles.cs
//  
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.Gui
{
	public static class Styles
	{
		public static event EventHandler Changed;

		public static Cairo.Color BaseBackgroundColor { get; internal set; }
		public static Cairo.Color BaseForegroundColor { get; internal set; }

		// General

		public static Gdk.Color ThinSplitterColor { get; internal set; }

		// Document tab bar

		public static Cairo.Color TabBarBackgroundColor { get; internal set; }
		public static Cairo.Color TabBarActiveTextColor { get; internal set; }
		public static Cairo.Color TabBarNotifyTextColor { get; internal set; }

		public static Cairo.Color TabBarActiveGradientStartColor { get; internal set; }
		public static Cairo.Color TabBarActiveGradientEndColor { get; internal set; }
		public static Cairo.Color TabBarGradientStartColor { get; internal set; }
		public static Cairo.Color TabBarGradientEndColor { get; internal set; }
		public static Cairo.Color TabBarGradientShadowColor { get; internal set; }
		public static Cairo.Color TabBarGlowGradientStartColor { get; internal set; }
		public static Cairo.Color TabBarGlowGradientEndColor { get; internal set; }
		public static Cairo.Color TabBarHoverActiveTextColor { get; internal set; }
		public static Cairo.Color TabBarInactiveTextColor { get; internal set; }
		public static Cairo.Color TabBarHoverInactiveTextColor { get; internal set; }
		public static Cairo.Color TabBarInnerBorderColor { get; internal set; }
		public static Cairo.Color TabBarInactiveGradientStartColor { get; internal set; }
		public static Cairo.Color TabBarInactiveGradientEndColor { get; internal set; }

		public static Cairo.Color BreadcrumbGradientStartColor { get; internal set; }
		public static Cairo.Color BreadcrumbBackgroundColor { get; internal set; }
		public static Cairo.Color BreadcrumbGradientEndColor { get; internal set; }
		public static Cairo.Color BreadcrumbBorderColor { get; internal set; }
		public static Cairo.Color BreadcrumbInnerBorderColor { get; internal set; }
		public static Gdk.Color BreadcrumbTextColor { get; internal set; }
		public static Cairo.Color BreadcrumbButtonBorderColor { get; internal set; }
		public static Cairo.Color BreadcrumbButtonFillColor { get; internal set; }
		public static Cairo.Color BreadcrumbBottomBorderColor { get; internal set; }

		public static readonly bool BreadcrumbInvertedIcons = false;
		public static readonly bool BreadcrumbGreyscaleIcons = false;

		// Document Subview Tabs

		public static Cairo.Color SubTabBarBackgroundGradientTopColor { get; internal set; }
		public static Cairo.Color SubTabBarBackgroundGradientStartColor { get; internal set; }
		public static Cairo.Color SubTabBarBackgroundGradientEndColor { get; internal set; }
		public static Cairo.Color SubTabBarTextColor { get; internal set; }
		public static Cairo.Color SubTabBarActiveGradientStartColor { get; internal set; }
		public static Cairo.Color SubTabBarActiveGradientTopColor { get; internal set; }
		public static Cairo.Color SubTabBarActiveGradientEndColor { get; internal set; }
		public static Cairo.Color SubTabBarActiveTextColor { get; internal set; }
		public static Cairo.Color SubTabBarHoverGradientStartColor { get; internal set; }
		public static Cairo.Color SubTabBarHoverGradientEndColor { get; internal set; }
		public static Cairo.Color SubTabBarSeparatorColor { get; internal set; }

		// Dock pads
		
		public static Cairo.Color DockTabBarGradientTop { get; internal set; }
		public static Cairo.Color DockTabBarGradientStart { get; internal set; }
		public static Cairo.Color DockTabBarGradientEnd { get; internal set; }
		public static Cairo.Color DockTabBarShadowGradientStart { get; internal set; }
		public static Cairo.Color DockTabBarShadowGradientEnd { get; internal set; }

		public static Gdk.Color PadBackground { get; internal set; }
		public static Gdk.Color InactivePadBackground { get; internal set; }
		public static Gdk.Color PadLabelColor { get; internal set; }
		public static Gdk.Color DockFrameBackground { get; internal set; }
		public static Gdk.Color DockSeparatorColor { get; internal set; }

		public static Gdk.Color BrowserPadBackground { get; internal set; }
		public static Gdk.Color InactiveBrowserPadBackground { get; internal set; }

		public static Cairo.Color PadCategoryBackgroundGradientStartColor { get; internal set; }
		public static Cairo.Color PadCategoryBackgroundGradientEndColor { get; internal set; }
		public static Cairo.Color PadCategoryBorderColor { get; internal set; }
		public static Cairo.Color PadCategoryLabelColor { get; internal set; }

		public static Cairo.Color PropertyPadLabelBackgroundColor { get; internal set; }
		public static Cairo.Color PropertyPadDividerColor { get; internal set; }

		public static Cairo.Color DockBarBackground1 { get; internal set; }
		public static Cairo.Color DockBarBackground2 { get; internal set; }
		public static Cairo.Color DockBarSeparatorColorDark { get; internal set; }
		public static Cairo.Color DockBarSeparatorColorLight { get; internal set; }

		public static Cairo.Color DockBarPrelightColor { get; internal set; }

		// Status area

		public static Cairo.Color WidgetBorderColor { get; internal set; }

		public static Cairo.Color StatusBarBorderColor { get; internal set; }

		public static Cairo.Color StatusBarFill1Color { get; internal set; }
		public static Cairo.Color StatusBarFill2Color { get; internal set; }
		public static Cairo.Color StatusBarFill3Color { get; internal set; }
		public static Cairo.Color StatusBarFill4Color { get; internal set; }

		public static Cairo.Color StatusBarErrorColor { get; internal set; }

		public static Cairo.Color StatusBarInnerColor { get; internal set; }
		public static Cairo.Color StatusBarShadowColor1 { get; internal set; }
		public static Cairo.Color StatusBarShadowColor2 { get; internal set; }
		public static Cairo.Color StatusBarTextColor { get; internal set; }
		public static Cairo.Color StatusBarProgressBackgroundColor { get; internal set; }
		public static Cairo.Color StatusBarProgressOutlineColor { get; internal set; }

		public static readonly Pango.FontDescription StatusFont = Pango.FontDescription.FromString ("Normal");

		public static int StatusFontPixelHeight { get { return (int)(11 * PixelScale); } }
		public static int ProgressBarHeight { get { return (int)(18 * PixelScale); } }
		public static int ProgressBarInnerPadding { get { return (int)(4 * PixelScale); } }
		public static int ProgressBarOuterPadding { get { return (int)(4 * PixelScale); } }

		static readonly double PixelScale = GtkWorkarounds.GetPixelScale ();

		// Toolbar

		public static Cairo.Color ToolbarBottomBorderColor { get; internal set; }
		public static Cairo.Color ToolbarBottomGlowColor { get; internal set; }

		// Code Completion

		public static readonly int TooltipInfoSpacing = 1;

		// Popover Windows

		public static class PopoverWindow
		{
			public static readonly int PagerTriangleSize = 6;
			public static readonly int PagerHeight = 16;
			public static readonly double DefaultFontScale = 0.917; // 12pt default font size * 0.917 = 11pt

			public static Cairo.Color DefaultBackgroundColor { get; internal set; }
			public static Cairo.Color ErrorBackgroundColor { get; internal set; }
			public static Cairo.Color WarningBackgroundColor { get; internal set; }
			public static Cairo.Color InformationBackgroundColor { get; internal set; }

			public static Cairo.Color DefaultTextColor { get; internal set; }
			public static Cairo.Color ErrorTextColor { get; internal set; }
			public static Cairo.Color WarningTextColor { get; internal set; }
			public static Cairo.Color InformationTextColor { get; internal set; }

			public static Cairo.Color ShadowColor { get; internal set; }

			public static class ParamaterWindows
			{
				public static Cairo.Color GradientStartColor { get; internal set; }
				public static Cairo.Color GradientEndColor { get; internal set; }
			}
		}

		// Code Completion

		public static class CodeCompletion
		{
			public static Cairo.Color BackgroundColor { get; internal set; }
			public static Cairo.Color TextColor { get; internal set; }
			public static Cairo.Color HighlightColor { get; internal set; }
			public static Cairo.Color SelectionBackgroundColor { get; internal set; }
			public static Cairo.Color SelectionBackgroundInactiveColor { get; internal set; }
			public static Cairo.Color SelectionTextColor { get; internal set; }
			public static Cairo.Color SelectionHighlightColor { get; internal set; }
		}

		// Global Search

		public static class GlobalSearch
		{
			public static Cairo.Color HeaderTextColor { get; internal set; }
			public static Cairo.Color SeparatorLineColor { get; internal set; }
			public static Cairo.Color HeaderBackgroundColor { get; internal set; }
			public static Cairo.Color BackgroundColor { get; internal set; }
			public static Cairo.Color SelectionBackgroundColor { get; internal set; }
			public static Cairo.Color ResultTextColor { get; internal set; }
			public static Cairo.Color ResultDescriptionTextColor { get; internal set; }
		}

		// New Project Dialog

		public static class NewProjectDialog
		{
			public static Gdk.Color BannerBackgroundColor { get; internal set; }
			public static Gdk.Color BannerLineColor { get; internal set; }
			public static Gdk.Color BannerForegroundColor { get; internal set; }
			public static Gdk.Color CategoriesBackgroundColor { get; internal set; }
			public static Gdk.Color TemplateListBackgroundColor { get; internal set; }
			public static Gdk.Color TemplateBackgroundColor { get; internal set; }
			public static Gdk.Color TemplateSectionSeparatorColor { get; internal set; }
			public static Gdk.Color TemplateLanguageButtonBackground { get; internal set; }
			public static Gdk.Color TemplateLanguageButtonTriangle { get; internal set; }
			public static Gdk.Color ProjectConfigurationLeftHandBackgroundColor { get; internal set; }
			public static Gdk.Color ProjectConfigurationRightHandBackgroundColor { get; internal set; }
			public static Cairo.Color ProjectConfigurationPreviewLabelColor { get; internal set; }
			public static Gdk.Color ProjectConfigurationSeparatorColor { get; internal set; }
		}

		// Helper methods

		internal static Cairo.Color Shift (Cairo.Color color, double factor)
		{
			return new Cairo.Color (color.R * factor, color.G * factor, color.B * factor, color.A);
		}

		internal static Cairo.Color WithAlpha (Cairo.Color c, double alpha)
		{
			return new Cairo.Color (c.R, c.G, c.B, alpha);
		}

		internal static Cairo.Color Blend (Cairo.Color color, Cairo.Color targetColor, double factor)
		{
			return new Cairo.Color (color.R + ((targetColor.R - color.R) * factor),
			                        color.G + ((targetColor.G - color.G) * factor),
			                        color.B + ((targetColor.B - color.B) * factor),
			                        color.A
			                        );
		}

		internal static Cairo.Color MidColor (double factor)
		{
			return Blend (BaseBackgroundColor, BaseForegroundColor, factor);
		}

		internal static Cairo.Color ReduceLight (Cairo.Color color, double factor)
		{
			var c = color.ToXwtColor ();
			c.Light *= factor;
			return c.ToCairoColor ();
		}

		internal static Cairo.Color IncreaseLight (Cairo.Color color, double factor)
		{
			var c = color.ToXwtColor ();
			c.Light += (1 - c.Light) * factor;
			return c.ToCairoColor ();
		}

		internal static Gdk.Color ReduceLight (Gdk.Color color, double factor)
		{
			return ReduceLight (color.ToCairoColor (), factor).ToGdkColor ();
		}

		internal static Gdk.Color IncreaseLight (Gdk.Color color, double factor)
		{
			return IncreaseLight (color.ToCairoColor (), factor).ToGdkColor ();
		}

		internal static void LoadStyle ()
		{
			if (IdeApp.Preferences.UserInterfaceSkin == Skin.Light)
				LoadLightStyle ();
			else
				LoadDarkStyle ();

			if (Changed != null)
				Changed (null, EventArgs.Empty);
		}

		internal static void LoadLightStyle ()
		{
			BaseBackgroundColor = new Cairo.Color (1, 1, 1);
			BaseForegroundColor = new Cairo.Color (0, 0, 0);

			ThinSplitterColor = new Gdk.Color (166, 166, 166);

			TabBarBackgroundColor = CairoExtensions.ParseColor ("c2c2c2");
			TabBarActiveTextColor = new Cairo.Color (0, 0, 0);
			TabBarNotifyTextColor = new Cairo.Color (0, 0, 1);

			TabBarActiveGradientStartColor = Shift (TabBarBackgroundColor, 0.92);
			TabBarActiveGradientEndColor = TabBarBackgroundColor;
			TabBarGradientStartColor = Shift (TabBarBackgroundColor, 1.02);
			TabBarGradientEndColor = TabBarBackgroundColor;
			TabBarGradientShadowColor = Shift (TabBarBackgroundColor, 0.8);
			TabBarGlowGradientStartColor = new Cairo.Color (1, 1, 1, .4);
			TabBarGlowGradientEndColor = new Cairo.Color (1, 1, 1, 0);
			TabBarHoverActiveTextColor = TabBarActiveTextColor;
			TabBarInactiveTextColor = Blend (new Cairo.Color (0, 0, 0), TabBarGradientStartColor, 0.4);
			TabBarHoverInactiveTextColor = new Cairo.Color (0, 0, 0);
			TabBarInnerBorderColor = new Cairo.Color (1, 1, 1, .5);
			TabBarInactiveGradientStartColor = CairoExtensions.ParseColor ("f4f4f4");
			TabBarInactiveGradientEndColor = CairoExtensions.ParseColor ("cecece");

			BreadcrumbGradientStartColor = CairoExtensions.ParseColor ("FFFFFF");
			BreadcrumbBackgroundColor = Shift (BreadcrumbGradientStartColor, .95);
			BreadcrumbGradientEndColor = Shift (BreadcrumbGradientStartColor, 0.9);
			BreadcrumbBorderColor = Shift (BreadcrumbBackgroundColor, 0.6);
			BreadcrumbInnerBorderColor = WithAlpha (BaseBackgroundColor, 0.1d);
			BreadcrumbTextColor = Shift (BaseForegroundColor, 0.8).ToGdkColor ();
			BreadcrumbButtonBorderColor = Shift (BaseBackgroundColor, 0.8);
			BreadcrumbButtonFillColor = WithAlpha (BaseBackgroundColor, 0.1d);
			BreadcrumbBottomBorderColor = Shift (BreadcrumbBackgroundColor, 0.7d);

			// Document Subview Tabs

			SubTabBarBackgroundGradientTopColor = new Cairo.Color (1, 1, 1);
			SubTabBarBackgroundGradientStartColor = new Cairo.Color (241d / 255d, 241d / 255d, 241d / 255d);
			SubTabBarBackgroundGradientEndColor = SubTabBarBackgroundGradientStartColor;//new Cairo.Color (224d / 255d, 224d / 255d, 224d / 255d);
			SubTabBarTextColor = BaseForegroundColor;
			SubTabBarActiveGradientTopColor = new Cairo.Color (1, 1, 1, 0.05);
			SubTabBarActiveGradientStartColor = new Cairo.Color (92d / 255d, 93d / 255d, 94d / 255d);
			SubTabBarActiveGradientEndColor = new Cairo.Color (134d / 255d, 136d / 255d, 137d / 255d);
			SubTabBarActiveTextColor = new Cairo.Color (1, 1, 1);
			SubTabBarHoverGradientStartColor = new Cairo.Color (92d / 255d, 93d / 255d, 94d / 255d, 0.2);
			SubTabBarHoverGradientEndColor = new Cairo.Color (134d / 255d, 136d / 255d, 137d / 255d, 0.2);
			SubTabBarSeparatorColor = new Cairo.Color (171d / 255d, 171d / 255d, 171d / 255d);

			// Dock pads

			DockTabBarGradientTop = new Cairo.Color (248d / 255d, 248d / 255d, 248d / 255d);
			DockTabBarGradientStart = new Cairo.Color (242d / 255d, 242d / 255d, 242d / 255d);
			DockTabBarGradientEnd = new Cairo.Color (230d / 255d, 230d / 255d, 230d / 255d);
			DockTabBarShadowGradientStart = new Cairo.Color (154d / 255d, 154d / 255d, 154d / 255d, 1);
			DockTabBarShadowGradientEnd = new Cairo.Color (154d / 255d, 154d / 255d, 154d / 255d, 0);

			PadBackground = new Gdk.Color (240, 240, 240);
			InactivePadBackground = ReduceLight (PadBackground, 0.9);
			PadLabelColor = new Gdk.Color (92, 99, 102);
			DockFrameBackground = new Gdk.Color (157, 162, 166);
			DockSeparatorColor = ThinSplitterColor;

			BrowserPadBackground = new Gdk.Color (225, 228, 232);
			InactiveBrowserPadBackground = new Gdk.Color (240, 240, 240);

			PadCategoryBackgroundGradientStartColor = new Cairo.Color (248d/255d, 248d/255d, 248d/255d);
			PadCategoryBackgroundGradientEndColor = new Cairo.Color (240d/255d, 240d/255d, 240d/255d);
			PadCategoryBorderColor = new Cairo.Color (217d/255d, 217d/255d, 217d/255d);
			PadCategoryLabelColor = new Cairo.Color (128d/255d, 128d/255d, 128d/255d);

			PropertyPadLabelBackgroundColor = new Cairo.Color (250d/255d, 250d/255d, 250d/255d);
			PropertyPadDividerColor = PropertyPadLabelBackgroundColor;

			DockBarBackground1 = PadBackground.ToCairoColor ();
			DockBarBackground2 = Shift (PadBackground.ToCairoColor (), 0.95);
			DockBarSeparatorColorDark = new Cairo.Color (0, 0, 0, 0.2);
			DockBarSeparatorColorLight = new Cairo.Color (1, 1, 1, 0.3);

			DockBarPrelightColor = CairoExtensions.ParseColor ("ffffff");

			// Status area

			WidgetBorderColor = CairoExtensions.ParseColor ("8c8c8c");

			StatusBarBorderColor = CairoExtensions.ParseColor ("919191");

			StatusBarFill1Color = CairoExtensions.ParseColor ("f5fafc");
			StatusBarFill2Color = CairoExtensions.ParseColor ("e9f1f3");
			StatusBarFill3Color = CairoExtensions.ParseColor ("d8e7ea");
			StatusBarFill4Color = CairoExtensions.ParseColor ("d1e3e7");

			StatusBarErrorColor = CairoExtensions.ParseColor ("FF6363");

			StatusBarInnerColor = new Cairo.Color (0,0,0, 0.08);
			StatusBarShadowColor1 = new Cairo.Color (0,0,0, 0.06);
			StatusBarShadowColor2 = new Cairo.Color (0,0,0, 0.02);
			StatusBarTextColor = CairoExtensions.ParseColor ("555555");
			StatusBarProgressBackgroundColor = new Cairo.Color (0, 0, 0, 0.1);
			StatusBarProgressOutlineColor = new Cairo.Color (0, 0, 0, 0.1);

			// Toolbar

			ToolbarBottomBorderColor = new Cairo.Color (0.5, 0.5, 0.5);
			ToolbarBottomGlowColor = new Cairo.Color (1, 1, 1, 0.2);

			// Popover window

			PopoverWindow.DefaultBackgroundColor = CairoExtensions.ParseColor ("f2f2f2");
			PopoverWindow.ErrorBackgroundColor = CairoExtensions.ParseColor ("E27267");
			PopoverWindow.WarningBackgroundColor = CairoExtensions.ParseColor ("efd46c");
			PopoverWindow.InformationBackgroundColor = CairoExtensions.ParseColor ("709DC9");

			PopoverWindow.DefaultTextColor = CairoExtensions.ParseColor ("665a36");
			PopoverWindow.ErrorTextColor = CairoExtensions.ParseColor ("ffffff");
			PopoverWindow.WarningTextColor = CairoExtensions.ParseColor ("563b00");
			PopoverWindow.InformationTextColor = CairoExtensions.ParseColor ("ffffff");

			PopoverWindow.ShadowColor = new Cairo.Color (0, 0, 0, 0.1);

			PopoverWindow.ParamaterWindows.GradientStartColor = CairoExtensions.ParseColor ("fffee6");
			PopoverWindow.ParamaterWindows.GradientEndColor = CairoExtensions.ParseColor ("fffcd1");

			CodeCompletion.BackgroundColor = CairoExtensions.ParseColor ("eef1f2");
			CodeCompletion.TextColor = CairoExtensions.ParseColor ("665a36");
			CodeCompletion.HighlightColor = CairoExtensions.ParseColor ("ba3373");
			CodeCompletion.SelectionBackgroundColor = CairoExtensions.ParseColor ("3f59e5");
			CodeCompletion.SelectionBackgroundInactiveColor = CairoExtensions.ParseColor ("bbbbbb");
			CodeCompletion.SelectionTextColor = CairoExtensions.ParseColor ("ffffff");
			CodeCompletion.SelectionHighlightColor = CairoExtensions.ParseColor ("ba3373"); // TODO: VV: New value

			// Global Search

			GlobalSearch.HeaderTextColor = CairoExtensions.ParseColor ("8c8c8c");
			GlobalSearch.SeparatorLineColor = CairoExtensions.ParseColor ("dedede");
			GlobalSearch.HeaderBackgroundColor = CairoExtensions.ParseColor ("ffffff");
			GlobalSearch.BackgroundColor = CairoExtensions.ParseColor ("f7f7f7");
			GlobalSearch.SelectionBackgroundColor = CairoExtensions.ParseColor ("cccccc");
			GlobalSearch.ResultTextColor = CairoExtensions.ParseColor ("#606060");
			GlobalSearch.ResultDescriptionTextColor = CairoExtensions.ParseColor ("#8F8F8F");

			// New Project Dialog

			NewProjectDialog.BannerBackgroundColor = new Gdk.Color (119, 130, 140);
			NewProjectDialog.BannerLineColor = new Gdk.Color (112, 122, 131);
			NewProjectDialog.BannerForegroundColor = new Gdk.Color (255, 255, 255);
			NewProjectDialog.CategoriesBackgroundColor = new Gdk.Color (225, 228, 232);
			NewProjectDialog.TemplateListBackgroundColor = new Gdk.Color (240, 240, 240);
			NewProjectDialog.TemplateBackgroundColor = new Gdk.Color (255, 255, 255);
			NewProjectDialog.TemplateSectionSeparatorColor = new Gdk.Color (208, 208, 208);
			NewProjectDialog.TemplateLanguageButtonBackground = new Gdk.Color (247, 247, 247);
			NewProjectDialog.TemplateLanguageButtonTriangle = new Gdk.Color (83, 83, 83);
			NewProjectDialog.ProjectConfigurationLeftHandBackgroundColor = new Gdk.Color (225, 228, 232);
			NewProjectDialog.ProjectConfigurationRightHandBackgroundColor = new Gdk.Color (255, 255, 255);
			NewProjectDialog.ProjectConfigurationPreviewLabelColor = CairoExtensions.ParseColor ("#555555");
			NewProjectDialog.ProjectConfigurationSeparatorColor = new Gdk.Color (176, 178, 181);
		}

		internal static void LoadDarkStyle ()
		{
			BaseBackgroundColor = new Cairo.Color (0, 0, 0);
			BaseForegroundColor = new Cairo.Color (1, 1, 1);

			ThinSplitterColor = new Gdk.Color (89, 89, 89);

			TabBarBackgroundColor = CairoExtensions.ParseColor ("333333");
			TabBarActiveTextColor = new Cairo.Color (1, 1, 1);
			TabBarNotifyTextColor = new Cairo.Color (1, 1, 1);

			// Document tabs

			TabBarActiveGradientStartColor = Shift (TabBarBackgroundColor, 0.92);
			TabBarActiveGradientEndColor = TabBarBackgroundColor;
			TabBarGradientStartColor = Shift (TabBarBackgroundColor, 1.02);
			TabBarGradientEndColor = TabBarBackgroundColor;
			TabBarGradientShadowColor = Shift (TabBarBackgroundColor, 0.8);
			TabBarGlowGradientStartColor = new Cairo.Color (0, 0, 0, .4);
			TabBarGlowGradientEndColor = new Cairo.Color (0, 0, 0, 0);
			TabBarHoverActiveTextColor = TabBarActiveTextColor;
			TabBarInactiveTextColor = Blend (new Cairo.Color (0, 0, 0), TabBarGradientStartColor, 0.4);
			TabBarHoverInactiveTextColor = new Cairo.Color (1, 1, 1);
			TabBarInnerBorderColor = new Cairo.Color (0, 0, 0, .5);
			TabBarInactiveGradientStartColor = Shift (TabBarBackgroundColor, 0.8);
			TabBarInactiveGradientEndColor = Shift (TabBarBackgroundColor, 0.7);

			// Breadcrumb

			BreadcrumbGradientStartColor = new Cairo.Color (0, 0, 0);
			BreadcrumbBackgroundColor = new Cairo.Color (.05, .05, .05);
			BreadcrumbGradientEndColor = new Cairo.Color (.1, .1, .1);
			BreadcrumbBorderColor = Shift (BreadcrumbBackgroundColor, 0.4);
			BreadcrumbInnerBorderColor = WithAlpha (BaseBackgroundColor, 0.1d);
			BreadcrumbTextColor = Shift (BaseForegroundColor, 0.8).ToGdkColor ();
			BreadcrumbButtonBorderColor = Shift (BaseBackgroundColor, 0.8);
			BreadcrumbButtonFillColor = WithAlpha (BaseBackgroundColor, 0.1d);
			BreadcrumbBottomBorderColor = Shift (BreadcrumbBackgroundColor, 0.7d);

			// Document Subview Tabs

			SubTabBarBackgroundGradientTopColor = Shift (TabBarBackgroundColor, 0.8);
			SubTabBarBackgroundGradientStartColor = TabBarBackgroundColor;
			SubTabBarBackgroundGradientEndColor = SubTabBarBackgroundGradientStartColor;
			SubTabBarTextColor = BaseForegroundColor;
			SubTabBarActiveGradientTopColor = new Cairo.Color (0, 0, 0, 0.05);
			SubTabBarActiveGradientStartColor = new Cairo.Color (0, 0, 0);
			SubTabBarActiveGradientEndColor = new Cairo.Color (0, 0, 0);
			SubTabBarActiveTextColor = BaseForegroundColor;
			SubTabBarHoverGradientStartColor = Shift (SubTabBarBackgroundGradientTopColor, 0.8);
			SubTabBarHoverGradientEndColor =  Shift (SubTabBarBackgroundGradientTopColor, 0.8);
			SubTabBarSeparatorColor = ThinSplitterColor.ToCairoColor();

			// Dock pads

			DockTabBarGradientTop = new Cairo.Color (248d / 255d, 248d / 255d, 248d / 255d);
			DockTabBarGradientStart = new Cairo.Color (242d / 255d, 242d / 255d, 242d / 255d);
			DockTabBarGradientEnd = new Cairo.Color (230d / 255d, 230d / 255d, 230d / 255d);
			DockTabBarShadowGradientStart = new Cairo.Color (154d / 255d, 154d / 255d, 154d / 255d, 1);
			DockTabBarShadowGradientEnd = new Cairo.Color (154d / 255d, 154d / 255d, 154d / 255d, 0);

			PadBackground = new Gdk.Color (90, 90, 90);
			InactivePadBackground = ReduceLight (PadBackground, 0.9);
			PadLabelColor = new Gdk.Color (92, 99, 102);
			DockFrameBackground = new Gdk.Color (157, 162, 166);
			DockSeparatorColor = ThinSplitterColor;

			BrowserPadBackground = new Gdk.Color (32, 32, 32);
			InactiveBrowserPadBackground = new Gdk.Color (20, 20, 20);

			PadCategoryBackgroundGradientStartColor = new Cairo.Color (90d/255d, 90d/255d, 90d/255d);
			PadCategoryBackgroundGradientEndColor = new Cairo.Color (82d/255d, 82d/255d, 82d/255d);
			PadCategoryBorderColor = new Cairo.Color (96d/255d, 96d/255d, 96d/255d);
			PadCategoryLabelColor = Shift (BaseForegroundColor, 0.8);

			PropertyPadLabelBackgroundColor = PadBackground.ToCairoColor();
			PropertyPadDividerColor = PadBackground.ToCairoColor();

			DockBarBackground1 = PadBackground.ToCairoColor ();
			DockBarBackground2 = Shift (PadBackground.ToCairoColor (), 0.95);
			DockBarSeparatorColorDark = new Cairo.Color (1, 1, 1, 0.2);
			DockBarSeparatorColorLight = new Cairo.Color (0, 0, 0, 0.3);

			DockBarPrelightColor = new Cairo.Color (0, 0, 0);

			// Status area

			WidgetBorderColor = CairoExtensions.ParseColor ("8c8c8c");

			StatusBarBorderColor = CairoExtensions.ParseColor ("919191");

			StatusBarFill1Color = CairoExtensions.ParseColor ("f5fafc");
			StatusBarFill2Color = CairoExtensions.ParseColor ("e9f1f3");
			StatusBarFill3Color = CairoExtensions.ParseColor ("d8e7ea");
			StatusBarFill4Color = CairoExtensions.ParseColor ("d1e3e7");

			StatusBarErrorColor = CairoExtensions.ParseColor ("FF6363");

			StatusBarInnerColor = new Cairo.Color (0,0,0, 0.08);
			StatusBarShadowColor1 = new Cairo.Color (0,0,0, 0.06);
			StatusBarShadowColor2 = new Cairo.Color (0,0,0, 0.02);
			StatusBarTextColor = CairoExtensions.ParseColor ("555555");
			StatusBarProgressBackgroundColor = new Cairo.Color (0, 0, 0, 0.1);
			StatusBarProgressOutlineColor = new Cairo.Color (0, 0, 0, 0.1);

			// Toolbar

			ToolbarBottomBorderColor = new Cairo.Color (0.5, 0.5, 0.5);
			ToolbarBottomGlowColor = new Cairo.Color (1, 1, 1, 0.2);

			// Popover window

			PopoverWindow.DefaultBackgroundColor = CairoExtensions.ParseColor ("5A5A5A");
			PopoverWindow.ErrorBackgroundColor = CairoExtensions.ParseColor ("E27267");
			PopoverWindow.WarningBackgroundColor = CairoExtensions.ParseColor ("efd46c");
			PopoverWindow.InformationBackgroundColor = CairoExtensions.ParseColor ("709DC9");

			PopoverWindow.DefaultTextColor = CairoExtensions.ParseColor ("ffffff");
			PopoverWindow.ErrorTextColor = CairoExtensions.ParseColor ("ffffff");
			PopoverWindow.WarningTextColor = CairoExtensions.ParseColor ("563b00");
			PopoverWindow.InformationTextColor = CairoExtensions.ParseColor ("ffffff");

			PopoverWindow.ShadowColor = new Cairo.Color (0, 0, 0, 0); // transparent since dark skin doesn't need shadows

			PopoverWindow.ParamaterWindows.GradientStartColor = CairoExtensions.ParseColor ("fffee6");
			PopoverWindow.ParamaterWindows.GradientEndColor = CairoExtensions.ParseColor ("fffcd1");

			CodeCompletion.BackgroundColor = CairoExtensions.ParseColor ("5b6365");
			CodeCompletion.TextColor = CairoExtensions.ParseColor ("c3c5c6");
			CodeCompletion.HighlightColor = CairoExtensions.ParseColor ("f9d33c");
			CodeCompletion.SelectionBackgroundColor = CairoExtensions.ParseColor ("3d8afa");
			CodeCompletion.SelectionBackgroundInactiveColor = CairoExtensions.ParseColor ("555555");
			CodeCompletion.SelectionTextColor = CairoExtensions.ParseColor ("ffffff");
			CodeCompletion.SelectionHighlightColor = CairoExtensions.ParseColor ("f9d33c");


			// Global Search

			GlobalSearch.HeaderTextColor = CairoExtensions.ParseColor ("ffffff");
			GlobalSearch.HeaderBackgroundColor = CairoExtensions.ParseColor ("5a5a5a");
			GlobalSearch.SeparatorLineColor = CairoExtensions.ParseColor ("595959");
			GlobalSearch.BackgroundColor = CairoExtensions.ParseColor ("696969");
			GlobalSearch.SelectionBackgroundColor = CairoExtensions.ParseColor ("cccccc");
			GlobalSearch.ResultTextColor = CairoExtensions.ParseColor ("#ffffff");
			GlobalSearch.ResultDescriptionTextColor = CairoExtensions.ParseColor ("#a3a3a3");

			// New Project Dialog

			NewProjectDialog.BannerBackgroundColor = new Gdk.Color (119, 130, 140);
			NewProjectDialog.BannerLineColor = ThinSplitterColor;
			NewProjectDialog.BannerForegroundColor = new Gdk.Color (255, 255, 255);
			NewProjectDialog.CategoriesBackgroundColor = new Gdk.Color (85, 85, 85);
			NewProjectDialog.TemplateListBackgroundColor = new Gdk.Color (90, 90, 90);
			NewProjectDialog.TemplateBackgroundColor = new Gdk.Color (105, 105, 105);
			NewProjectDialog.TemplateSectionSeparatorColor = ThinSplitterColor;
			NewProjectDialog.TemplateLanguageButtonBackground = new Gdk.Color (97, 97, 97);
			NewProjectDialog.TemplateLanguageButtonTriangle = new Gdk.Color (255, 255, 255);
			NewProjectDialog.ProjectConfigurationLeftHandBackgroundColor = new Gdk.Color (85, 85, 85);
			NewProjectDialog.ProjectConfigurationRightHandBackgroundColor = NewProjectDialog.TemplateBackgroundColor;
			NewProjectDialog.ProjectConfigurationPreviewLabelColor = Shift (BaseForegroundColor, 0.8);
			NewProjectDialog.ProjectConfigurationSeparatorColor = ThinSplitterColor;
		}
	}
}

