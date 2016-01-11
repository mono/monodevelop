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
using Xwt.Drawing;

#if MAC
using AppKit;
#endif

namespace MonoDevelop.Ide.Gui
{
	public static class Styles
	{
		public static event EventHandler Changed;

		public static Color BackgroundColor { get; internal set; }        // must be the bg color from Gtkrc
		public static Color BaseBackgroundColor { get; internal set; }    // must be the base color from Gtkrc
		public static Color BaseForegroundColor { get; internal set; }    // must be the text color from Gtkrc
		public static Color BaseSelectionBackgroundColor { get; internal set; }
		public static Color BaseSelectionTextColor { get; internal set; }

		// General

		public static Color ThinSplitterColor { get; internal set; }

		// Document tab bar

		public static Color TabBarBackgroundColor { get; internal set; }
		public static Color TabBarActiveTextColor { get; internal set; }
		public static Color TabBarNotifyTextColor { get; internal set; }

		public static Color TabBarActiveGradientStartColor { get; internal set; }
		public static Color TabBarActiveGradientEndColor { get; internal set; }
		public static Color TabBarGradientStartColor { get; internal set; }
		public static Color TabBarGradientEndColor { get; internal set; }
		public static Color TabBarGradientShadowColor { get; internal set; }
		public static Color TabBarGlowGradientStartColor { get; internal set; }
		public static Color TabBarGlowGradientEndColor { get; internal set; }
		public static Color TabBarHoverActiveTextColor { get; internal set; }
		public static Color TabBarInactiveTextColor { get; internal set; }
		public static Color TabBarHoverInactiveTextColor { get; internal set; }
		public static Color TabBarInnerBorderColor { get; internal set; }
		public static Color TabBarInactiveGradientStartColor { get; internal set; }
		public static Color TabBarInactiveGradientEndColor { get; internal set; }

		public static Color BreadcrumbGradientStartColor { get; internal set; }
		public static Color BreadcrumbBackgroundColor { get; internal set; }
		public static Color BreadcrumbGradientEndColor { get; internal set; }
		public static Color BreadcrumbBorderColor { get; internal set; }
		public static Color BreadcrumbInnerBorderColor { get; internal set; }
		public static Color BreadcrumbTextColor { get; internal set; }
		public static Color BreadcrumbButtonBorderColor { get; internal set; }
		public static Color BreadcrumbButtonFillColor { get; internal set; }
		public static Color BreadcrumbBottomBorderColor { get; internal set; }

		public static readonly bool BreadcrumbInvertedIcons = false;
		public static readonly bool BreadcrumbGreyscaleIcons = false;

		// Document Subview Tabs

		public static Color SubTabBarBackgroundGradientTopColor { get; internal set; }
		public static Color SubTabBarBackgroundGradientStartColor { get; internal set; }
		public static Color SubTabBarBackgroundGradientEndColor { get; internal set; }
		public static Color SubTabBarTextColor { get; internal set; }
		public static Color SubTabBarActiveGradientStartColor { get; internal set; }
		public static Color SubTabBarActiveGradientTopColor { get; internal set; }
		public static Color SubTabBarActiveGradientEndColor { get; internal set; }
		public static Color SubTabBarActiveTextColor { get; internal set; }
		public static Color SubTabBarHoverGradientStartColor { get; internal set; }
		public static Color SubTabBarHoverGradientEndColor { get; internal set; }
		public static Color SubTabBarSeparatorColor { get; internal set; }

		// Dock pads

		public static Color DockTabBarGradientTop { get; internal set; }
		public static Color DockTabBarGradientStart { get; internal set; }
		public static Color DockTabBarGradientEnd { get; internal set; }
		public static Color DockTabBarShadowGradientStart { get; internal set; }
		public static Color DockTabBarShadowGradientEnd { get; internal set; }

		public static Color PadBackground { get; internal set; }
		public static Color InactivePadBackground { get; internal set; }
		public static Color PadLabelColor { get; internal set; }
		public static Color DockFrameBackground { get; internal set; }
		public static Color DockSeparatorColor { get; internal set; }

		public static Color BrowserPadBackground { get; internal set; }
		public static Color InactiveBrowserPadBackground { get; internal set; }

		public static Color PadCategoryBackgroundGradientStartColor { get; internal set; }
		public static Color PadCategoryBackgroundGradientEndColor { get; internal set; }
		public static Color PadCategoryBorderColor { get; internal set; }
		public static Color PadCategoryLabelColor { get; internal set; }

		public static Color PropertyPadLabelBackgroundColor { get; internal set; }
		public static Color PropertyPadDividerColor { get; internal set; }

		public static Color DockBarBackground1 { get; internal set; }
		public static Color DockBarBackground2 { get; internal set; }
		public static Color DockBarSeparatorColorDark { get; internal set; }
		public static Color DockBarSeparatorColorLight { get; internal set; }

		public static Color DockBarPrelightColor { get; internal set; }

		// Status area

		public static Color WidgetBorderColor { get; internal set; }

		public static Color StatusBarBorderColor { get; internal set; }

		public static Color StatusBarFill1Color { get; internal set; }
		public static Color StatusBarFill2Color { get; internal set; }
		public static Color StatusBarFill3Color { get; internal set; }
		public static Color StatusBarFill4Color { get; internal set; }

		public static Color StatusBarErrorColor { get; internal set; }

		public static Color StatusBarInnerColor { get; internal set; }
		public static Color StatusBarShadowColor1 { get; internal set; }
		public static Color StatusBarShadowColor2 { get; internal set; }
		public static Color StatusBarTextColor { get; internal set; }
		public static Color StatusBarProgressBackgroundColor { get; internal set; }
		public static Color StatusBarProgressOutlineColor { get; internal set; }

		public static readonly Pango.FontDescription StatusFont = Pango.FontDescription.FromString ("Normal");

		public static int StatusFontPixelHeight { get { return (int)(11 * PixelScale); } }
		public static int ProgressBarHeight { get { return (int)(18 * PixelScale); } }
		public static int ProgressBarInnerPadding { get { return (int)(4 * PixelScale); } }
		public static int ProgressBarOuterPadding { get { return (int)(4 * PixelScale); } }

		static double? pixelScale = null;

		static double PixelScale {
			get {
				if (!pixelScale.HasValue)
					pixelScale = GtkWorkarounds.GetPixelScale ();
				return (double)pixelScale;
			}
		}

		// Toolbar

		public static Color ToolbarBottomBorderColor { get; internal set; }
		public static Color ToolbarBottomGlowColor { get; internal set; }

		// Code Completion

		public static readonly int TooltipInfoSpacing;

		// Popover Windows

		public static class PopoverWindow
		{
			public static readonly int PagerTriangleSize = 6;
			public static readonly int PagerHeight = 16;
			public static readonly double DefaultFontScale = 0.917; // 12pt default font size * 0.917 = 11pt

			public static Color DefaultBackgroundColor { get; internal set; }
			public static Color ErrorBackgroundColor { get; internal set; }
			public static Color WarningBackgroundColor { get; internal set; }
			public static Color InformationBackgroundColor { get; internal set; }

			public static Color DefaultTextColor { get; internal set; }
			public static Color ErrorTextColor { get; internal set; }
			public static Color WarningTextColor { get; internal set; }
			public static Color InformationTextColor { get; internal set; }

			public static Color ShadowColor { get; internal set; }

			public static class ParamaterWindows
			{
				public static Color GradientStartColor { get; internal set; }
				public static Color GradientEndColor { get; internal set; }
			}
		}

		// Code Completion

		public static class CodeCompletion
		{
			public static Color BackgroundColor { get; internal set; }
			public static Color TextColor { get; internal set; }
			public static Color HighlightColor { get; internal set; }
			public static Color SelectionBackgroundColor { get; internal set; }
			public static Color SelectionBackgroundInactiveColor { get; internal set; }
			public static Color SelectionTextColor { get; internal set; }
			public static Color SelectionHighlightColor { get; internal set; }
		}

		// Global Search

		public static class GlobalSearch
		{
			public static Color HeaderTextColor { get; internal set; }
			public static Color SeparatorLineColor { get; internal set; }
			public static Color HeaderBackgroundColor { get; internal set; }
			public static Color BackgroundColor { get; internal set; }
			public static Color SelectionBackgroundColor { get; internal set; }
			public static Color ResultTextColor { get; internal set; }
			public static Color ResultDescriptionTextColor { get; internal set; }
		}

		// New Project Dialog

		public static class NewProjectDialog
		{
			public static Color BannerBackgroundColor { get; internal set; }
			public static Color BannerLineColor { get; internal set; }
			public static Color BannerForegroundColor { get; internal set; }
			public static Color CategoriesBackgroundColor { get; internal set; }
			public static Color TemplateListBackgroundColor { get; internal set; }
			public static Color TemplateBackgroundColor { get; internal set; }
			public static Color TemplateSectionSeparatorColor { get; internal set; }
			public static Color TemplateLanguageButtonBackground { get; internal set; }
			public static Color TemplateLanguageButtonTriangle { get; internal set; }
			public static Color ProjectConfigurationLeftHandBackgroundColor { get; internal set; }
			public static Color ProjectConfigurationRightHandBackgroundColor { get; internal set; }
			public static Color ProjectConfigurationPreviewLabelColor { get; internal set; }
			public static Color ProjectConfigurationSeparatorColor { get; internal set; }
		}

		// Editor

		public static class Editor
		{
			public static Color SmartTagMarkerColorLight { get; internal set; }
			public static Color SmartTagMarkerColorDark { get; internal set; }
		}

		// Helper methods

		internal static Color Shift (Color color, double factor)
		{
			return new Color (color.Red * factor, color.Green * factor, color.Blue * factor, color.Alpha);
		}

		internal static Color MidColor (double factor)
		{
			return BaseBackgroundColor.BlendWith (BaseForegroundColor, factor);
		}

		internal static Color ReduceLight (Color color, double factor)
		{
			color.Light *= factor;
			return color;
		}

		internal static Color IncreaseLight (Color color, double factor)
		{
			color.Light += (1 - color.Light) * factor;
			return color;
		}

		public static string ColorGetHex (Color color, bool withAlpha = false)
		{
			if (withAlpha) {
				return String.Format("#{0:x2}{1:x2}{2:x2}{3:x2}", (byte)(color.Red * 255), (byte)(color.Green * 255),
				                     (byte)(color.Blue * 255), (byte)(color.Alpha * 255));
			} else {
				return String.Format("#{0:x2}{1:x2}{2:x2}", (byte)(color.Red * 255), (byte)(color.Green * 255),
				                     (byte)(color.Blue * 255));
			}
		}

		static Styles ()
		{
			if (Core.Platform.IsWindows)
				TooltipInfoSpacing = 0;
			else
				TooltipInfoSpacing = -5;
		}

		internal static void LoadStyle ()
		{
			var defaultStyle = Gtk.Rc.GetStyle (IdeApp.Workbench.RootWindow);
			var bgColor = defaultStyle.Background (Gtk.StateType.Normal);

			BackgroundColor = defaultStyle.Background (Gtk.StateType.Normal).ToXwtColor ();	// must be the bg color from Gtkrc
			BaseBackgroundColor = defaultStyle.Base (Gtk.StateType.Normal).ToXwtColor ();	// must be the base color from Gtkrc
			BaseForegroundColor = defaultStyle.Foreground (Gtk.StateType.Normal).ToXwtColor ();	// must be the text color from Gtkrc
			BaseSelectionBackgroundColor = defaultStyle.Base (Gtk.StateType.Selected).ToXwtColor ();
			BaseSelectionTextColor = defaultStyle.Text (Gtk.StateType.Selected).ToXwtColor ();

			if (IdeApp.Preferences.UserInterfaceSkin == Skin.Light)
				LoadLightStyle ();
			else
				LoadDarkStyle ();
			
			Editor.SmartTagMarkerColorLight = new Color (255d / 255d, 112d / 255d, 254d / 255d, 0.5);
			Editor.SmartTagMarkerColorDark = new Color (255d / 255d, 169d / 255d, 255d / 255d, 0.5);

			if (Changed != null)
				Changed (null, EventArgs.Empty);
		}

		internal static void LoadLightStyle ()
		{
			ThinSplitterColor = new Color (166d / 255d, 166d / 255d, 166d / 255d);

			TabBarBackgroundColor = Color.FromName ("#c2c2c2");
			TabBarActiveTextColor = new Color (0, 0, 0);
			TabBarNotifyTextColor = new Color (0, 0, 1);

			// Document tabs

			TabBarActiveGradientStartColor = Shift (TabBarBackgroundColor, 0.92);
			TabBarActiveGradientEndColor = TabBarBackgroundColor;
			TabBarGradientStartColor = Shift (TabBarBackgroundColor, 1.02);
			TabBarGradientEndColor = TabBarBackgroundColor;
			TabBarGradientShadowColor = Shift (TabBarBackgroundColor, 0.8);
			TabBarGlowGradientStartColor = new Color (1, 1, 1, .4);
			TabBarGlowGradientEndColor = new Color (1, 1, 1, 0);
			TabBarHoverActiveTextColor = TabBarActiveTextColor;
			TabBarInactiveTextColor = new Color (0, 0, 0).BlendWith (TabBarGradientStartColor, 0.4);
			TabBarHoverInactiveTextColor = new Color (0, 0, 0);
			TabBarInnerBorderColor = new Color (1, 1, 1, .5);
			TabBarInactiveGradientStartColor = Color.FromName ("#f4f4f4");
			TabBarInactiveGradientEndColor = Color.FromName ("#cecece");

			// Breadcrumb

			BreadcrumbGradientStartColor = Color.FromName ("#FFFFFF");
			BreadcrumbBackgroundColor = Shift (BreadcrumbGradientStartColor, .95);
			BreadcrumbGradientEndColor = Shift (BreadcrumbGradientStartColor, 0.9);
			BreadcrumbBorderColor = Shift (BreadcrumbBackgroundColor, 0.6);
			BreadcrumbInnerBorderColor = BaseBackgroundColor.WithAlpha (0.1d);
			BreadcrumbTextColor = Shift (BaseForegroundColor, 0.8);
			BreadcrumbButtonBorderColor = Shift (BaseBackgroundColor, 0.8);
			BreadcrumbButtonFillColor = BaseBackgroundColor.WithAlpha (0.1d);
			BreadcrumbBottomBorderColor = Shift (BreadcrumbBackgroundColor, 0.7d);

			// Document Subview Tabs

			SubTabBarBackgroundGradientTopColor = new Color (1, 1, 1);
			SubTabBarBackgroundGradientStartColor = new Color (241d / 255d, 241d / 255d, 241d / 255d);
			SubTabBarBackgroundGradientEndColor = SubTabBarBackgroundGradientStartColor;//new Color (224d / 255d, 224d / 255d, 224d / 255d);
			SubTabBarTextColor = BaseForegroundColor;
			SubTabBarActiveGradientTopColor = new Color (1, 1, 1, 0.05);
			SubTabBarActiveGradientStartColor = new Color (92d / 255d, 93d / 255d, 94d / 255d);
			SubTabBarActiveGradientEndColor = new Color (134d / 255d, 136d / 255d, 137d / 255d);
			SubTabBarActiveTextColor = new Color (1, 1, 1);
			SubTabBarHoverGradientStartColor = new Color (92d / 255d, 93d / 255d, 94d / 255d, 0.2);
			SubTabBarHoverGradientEndColor = new Color (134d / 255d, 136d / 255d, 137d / 255d, 0.2);
			SubTabBarSeparatorColor = new Color (171d / 255d, 171d / 255d, 171d / 255d);

			// Dock pads

			DockTabBarGradientTop = new Color (248d / 255d, 248d / 255d, 248d / 255d);
			DockTabBarGradientStart = new Color (242d / 255d, 242d / 255d, 242d / 255d);
			DockTabBarGradientEnd = new Color (230d / 255d, 230d / 255d, 230d / 255d);
			DockTabBarShadowGradientStart = new Color (154d / 255d, 154d / 255d, 154d / 255d, 1);
			DockTabBarShadowGradientEnd = new Color (154d / 255d, 154d / 255d, 154d / 255d, 0);

			PadBackground = new Color (240d / 255d, 240d / 255d, 240d / 255d);
			InactivePadBackground = ReduceLight (PadBackground, 0.9);
			PadLabelColor = new Color (92d / 255d, 99d / 255d, 102d / 255d);
			DockFrameBackground = new Color (157d / 255d, 162d / 255d, 166d / 255d);
			DockSeparatorColor = ThinSplitterColor;

			BrowserPadBackground = Color.FromName("#f0f1f3");
			InactiveBrowserPadBackground = new Color (240d / 255d, 240d / 255d, 240d / 255d);

			PadCategoryBackgroundGradientStartColor = new Color (248d/255d, 248d/255d, 248d/255d);
			PadCategoryBackgroundGradientEndColor = new Color (240d/255d, 240d/255d, 240d/255d);
			PadCategoryBorderColor = new Color (217d/255d, 217d/255d, 217d/255d);
			PadCategoryLabelColor = new Color (128d/255d, 128d/255d, 128d/255d);

			PropertyPadLabelBackgroundColor = new Color (250d/255d, 250d/255d, 250d/255d);
			PropertyPadDividerColor = PropertyPadLabelBackgroundColor;

			DockBarBackground1 = PadBackground;
			DockBarBackground2 = Shift (PadBackground, 0.95);
			DockBarSeparatorColorDark = new Color (0, 0, 0, 0.2);
			DockBarSeparatorColorLight = new Color (1, 1, 1, 0.3);

			DockBarPrelightColor = Color.FromName ("#ffffff");

			// Status area

			WidgetBorderColor = Color.FromName ("#8c8c8c");

			StatusBarBorderColor = Color.FromName ("#919191");

			StatusBarFill1Color = Color.FromName ("#f5fafc");
			StatusBarFill2Color = Color.FromName ("#e9f1f3");
			StatusBarFill3Color = Color.FromName ("#d8e7ea");
			StatusBarFill4Color = Color.FromName ("#d1e3e7");

			StatusBarErrorColor = Color.FromName ("#FF6363");

			StatusBarInnerColor = new Color (0,0,0, 0.08);
			StatusBarShadowColor1 = new Color (0,0,0, 0.06);
			StatusBarShadowColor2 = new Color (0,0,0, 0.02);
			StatusBarTextColor = Color.FromName ("#555555");
			StatusBarProgressBackgroundColor = new Color (0, 0, 0, 0.1);
			StatusBarProgressOutlineColor = new Color (0, 0, 0, 0.1);

			// Toolbar

			ToolbarBottomBorderColor = new Color (0.5, 0.5, 0.5);
			ToolbarBottomGlowColor = new Color (1, 1, 1, 0.2);

			// Popover window

			PopoverWindow.DefaultBackgroundColor = Color.FromName ("#f2f2f2");
			PopoverWindow.ErrorBackgroundColor = Color.FromName ("#E27267");
			PopoverWindow.WarningBackgroundColor = Color.FromName ("#efd46c");
			PopoverWindow.InformationBackgroundColor = Color.FromName ("#709DC9");

			PopoverWindow.DefaultTextColor = Color.FromName ("#665a36");
			PopoverWindow.ErrorTextColor = Color.FromName ("#ffffff");
			PopoverWindow.WarningTextColor = Color.FromName ("#563b00");
			PopoverWindow.InformationTextColor = Color.FromName ("#ffffff");

			PopoverWindow.ShadowColor = new Color (0, 0, 0, 0.1);

			PopoverWindow.ParamaterWindows.GradientStartColor = Color.FromName ("#fffee6");
			PopoverWindow.ParamaterWindows.GradientEndColor = Color.FromName ("#fffcd1");

			CodeCompletion.BackgroundColor = Color.FromName ("#eef1f2");
			CodeCompletion.TextColor = Color.FromName ("#646566");
			CodeCompletion.HighlightColor = Color.FromName ("#ba3373");

			#if MAC
			CodeCompletion.SelectionBackgroundInactiveColor = Color.FromName ("#bbbbbb");
			CodeCompletion.SelectionHighlightColor = Color.FromName ("#ba3373");
			#else
			CodeCompletion.SelectionBackgroundInactiveColor = Color.FromName ("#bbbbbb"); // TODO: VV: Windows colors
			CodeCompletion.SelectionHighlightColor = Color.FromName ("#ba3373");
			#endif

			CodeCompletion.SelectionBackgroundColor = BaseSelectionBackgroundColor;
			CodeCompletion.SelectionTextColor = BaseSelectionTextColor;

			// Global Search

			GlobalSearch.HeaderTextColor = Color.FromName ("#8c8c8c");
			GlobalSearch.SeparatorLineColor = Color.FromName ("#dedede");
			GlobalSearch.HeaderBackgroundColor = Color.FromName ("#ffffff");
			GlobalSearch.BackgroundColor = Color.FromName ("#f7f7f7");
			GlobalSearch.SelectionBackgroundColor = Color.FromName ("#cccccc");
			GlobalSearch.ResultTextColor = Color.FromName ("#606060");
			GlobalSearch.ResultDescriptionTextColor = Color.FromName ("#8F8F8F");

			// New Project Dialog

			NewProjectDialog.BannerBackgroundColor = new Color (119d / 255d, 130d / 255d, 140d / 255d);
			NewProjectDialog.BannerLineColor = new Color (112d / 255d, 122d / 255d, 131d / 255d);
			NewProjectDialog.BannerForegroundColor = new Color (255d / 255d, 255d / 255d, 255d / 255d);
			NewProjectDialog.CategoriesBackgroundColor = new Color (225d / 255d, 228d / 255d, 232d / 255d);
			NewProjectDialog.TemplateListBackgroundColor = new Color (240d / 255d, 240d / 255d, 240d / 255d);
			NewProjectDialog.TemplateBackgroundColor = new Color (255d / 255d, 255d / 255d, 255d / 255d);
			NewProjectDialog.TemplateSectionSeparatorColor = new Color (208d / 255d, 208d / 255d, 208d / 255d);
			NewProjectDialog.TemplateLanguageButtonBackground = new Color (247d / 255d, 247d / 255d, 247d / 255d);
			NewProjectDialog.TemplateLanguageButtonTriangle = new Color (83d / 255d, 83d / 255d, 83d / 255d);
			NewProjectDialog.ProjectConfigurationLeftHandBackgroundColor = new Color (225d / 255d, 228d / 255d, 232d / 255d);
			NewProjectDialog.ProjectConfigurationRightHandBackgroundColor = new Color (255d / 255d, 255d / 255d, 255d / 255d);
			NewProjectDialog.ProjectConfigurationPreviewLabelColor = Color.FromName ("#555555");
			NewProjectDialog.ProjectConfigurationSeparatorColor = new Color (176d / 255d, 178d / 255d, 181d / 255d);
		}

		internal static void LoadDarkStyle ()
		{
			ThinSplitterColor = new Color (89d / 255d, 89d / 255d, 89d / 255d);

			TabBarBackgroundColor = Color.FromName ("#333333");
			TabBarActiveTextColor = new Color (1, 1, 1);
			TabBarNotifyTextColor = new Color (1, 1, 1);

			// Document tabs

			TabBarActiveGradientStartColor = Shift (TabBarBackgroundColor, 0.92);
			TabBarActiveGradientEndColor = TabBarBackgroundColor;
			TabBarGradientStartColor = Shift (TabBarBackgroundColor, 1.02);
			TabBarGradientEndColor = TabBarBackgroundColor;
			TabBarGradientShadowColor = Shift (TabBarBackgroundColor, 0.8);
			TabBarGlowGradientStartColor = new Color (0, 0, 0, .4);
			TabBarGlowGradientEndColor = new Color (0, 0, 0, 0);
			TabBarHoverActiveTextColor = TabBarActiveTextColor;
			TabBarInactiveTextColor = new Color (0, 0, 0).BlendWith (TabBarGradientStartColor, 0.4);
			TabBarHoverInactiveTextColor = new Color (1, 1, 1);
			TabBarInnerBorderColor = new Color (0, 0, 0, .5);
			TabBarInactiveGradientStartColor = Shift (TabBarBackgroundColor, 0.8);
			TabBarInactiveGradientEndColor = Shift (TabBarBackgroundColor, 0.7);

			// Breadcrumb

			BreadcrumbGradientStartColor = new Color (0, 0, 0);
			BreadcrumbBackgroundColor = new Color (.05, .05, .05);
			BreadcrumbGradientEndColor = new Color (.1, .1, .1);
			BreadcrumbBorderColor = Shift (BreadcrumbBackgroundColor, 0.4);
			BreadcrumbInnerBorderColor = BaseBackgroundColor.WithAlpha (0.1d);
			BreadcrumbTextColor = Shift (BaseForegroundColor, 0.8);
			BreadcrumbButtonBorderColor = Shift (BaseBackgroundColor, 0.8);
			BreadcrumbButtonFillColor = BaseBackgroundColor.WithAlpha (0.1d);
			BreadcrumbBottomBorderColor = Shift (BreadcrumbBackgroundColor, 0.7d);

			// Document Subview Tabs

			SubTabBarBackgroundGradientTopColor = Shift (TabBarBackgroundColor, 0.8);
			SubTabBarBackgroundGradientStartColor = TabBarBackgroundColor;
			SubTabBarBackgroundGradientEndColor = SubTabBarBackgroundGradientStartColor;
			SubTabBarTextColor = BaseForegroundColor;
			SubTabBarActiveGradientTopColor = new Color (0, 0, 0, 0.05);
			SubTabBarActiveGradientStartColor = new Color (0, 0, 0);
			SubTabBarActiveGradientEndColor = new Color (0, 0, 0);
			SubTabBarActiveTextColor = BaseForegroundColor;
			SubTabBarHoverGradientStartColor = Shift (SubTabBarBackgroundGradientTopColor, 0.8);
			SubTabBarHoverGradientEndColor =  Shift (SubTabBarBackgroundGradientTopColor, 0.8);
			SubTabBarSeparatorColor = ThinSplitterColor;

			// Dock pads

			DockTabBarGradientTop = new Color (248d / 255d, 248d / 255d, 248d / 255d);
			DockTabBarGradientStart = new Color (242d / 255d, 242d / 255d, 242d / 255d);
			DockTabBarGradientEnd = new Color (230d / 255d, 230d / 255d, 230d / 255d);
			DockTabBarShadowGradientStart = new Color (154d / 255d, 154d / 255d, 154d / 255d, 1);
			DockTabBarShadowGradientEnd = new Color (154d / 255d, 154d / 255d, 154d / 255d, 0);

			PadBackground = new Color (90d / 255d, 90d / 255d, 90d / 255d);
			InactivePadBackground = ReduceLight (PadBackground, 0.9);
			PadLabelColor = new Color (92d / 255d, 99d / 255d, 102d / 255d);
			DockFrameBackground = new Color (157d / 255d, 162d / 255d, 166d / 255d);
			DockSeparatorColor = ThinSplitterColor;

			BrowserPadBackground = new Color (32d / 255d, 32d / 255d, 32d / 255d);
			InactiveBrowserPadBackground = new Color (20d / 255d, 20d / 255d, 20d / 255d);

			PadCategoryBackgroundGradientStartColor = new Color (90d/255d, 90d/255d, 90d/255d);
			PadCategoryBackgroundGradientEndColor = new Color (82d/255d, 82d/255d, 82d/255d);
			PadCategoryBorderColor = new Color (96d/255d, 96d/255d, 96d/255d);
			PadCategoryLabelColor = Shift (BaseForegroundColor, 0.8);

			PropertyPadLabelBackgroundColor = PadBackground;
			PropertyPadDividerColor = PadBackground;

			DockBarBackground1 = PadBackground;
			DockBarBackground2 = Shift (PadBackground, 0.95);
			DockBarSeparatorColorDark = new Color (1, 1, 1, 0.2);
			DockBarSeparatorColorLight = new Color (0, 0, 0, 0.3);

			DockBarPrelightColor = new Color (0, 0, 0);

			// Status area

			WidgetBorderColor = Color.FromName ("#8c8c8c");

			StatusBarBorderColor = Color.FromName ("#919191");

			StatusBarFill1Color = Color.FromName ("#f5fafc");
			StatusBarFill2Color = Color.FromName ("#e9f1f3");
			StatusBarFill3Color = Color.FromName ("#d8e7ea");
			StatusBarFill4Color = Color.FromName ("#d1e3e7");

			StatusBarErrorColor = Color.FromName ("#FF6363");

			StatusBarInnerColor = new Color (0,0,0, 0.08);
			StatusBarShadowColor1 = new Color (0,0,0, 0.06);
			StatusBarShadowColor2 = new Color (0,0,0, 0.02);
			StatusBarTextColor = Color.FromName ("#555555");
			StatusBarProgressBackgroundColor = new Color (0, 0, 0, 0.1);
			StatusBarProgressOutlineColor = new Color (0, 0, 0, 0.1);

			// Toolbar

			ToolbarBottomBorderColor = new Color (0.5, 0.5, 0.5);
			ToolbarBottomGlowColor = new Color (1, 1, 1, 0.2);

			// Popover window

			PopoverWindow.DefaultBackgroundColor = Color.FromName ("#5A5A5A");
			PopoverWindow.ErrorBackgroundColor = Color.FromName ("#E27267");
			PopoverWindow.WarningBackgroundColor = Color.FromName ("#efd46c");
			PopoverWindow.InformationBackgroundColor = Color.FromName ("#709DC9");

			PopoverWindow.DefaultTextColor = Color.FromName ("#ffffff");
			PopoverWindow.ErrorTextColor = Color.FromName ("#ffffff");
			PopoverWindow.WarningTextColor = Color.FromName ("#563b00");
			PopoverWindow.InformationTextColor = Color.FromName ("#ffffff");

			PopoverWindow.ShadowColor = new Color (0, 0, 0, 0); // transparent since dark skin doesn't need shadows

			PopoverWindow.ParamaterWindows.GradientStartColor = Color.FromName ("#fffee6");
			PopoverWindow.ParamaterWindows.GradientEndColor = Color.FromName ("#fffcd1");

			CodeCompletion.BackgroundColor = Color.FromName ("#5b6365");
			CodeCompletion.TextColor = Color.FromName ("#c3c5c6");
			CodeCompletion.HighlightColor = Color.FromName ("#f9d33c");

			#if MAC
			CodeCompletion.SelectionBackgroundInactiveColor = Color.FromName ("#555555");
			CodeCompletion.SelectionHighlightColor = Color.FromName ("#f9d33c");
			#else
			CodeCompletion.SelectionBackgroundInactiveColor = Color.FromName ("#bbbbbb"); // TODO: VV: Windows colors
			CodeCompletion.SelectionHighlightColor = Color.FromName ("#f9d33c");
			#endif

			CodeCompletion.SelectionBackgroundColor = BaseSelectionBackgroundColor;
			CodeCompletion.SelectionTextColor = BaseSelectionTextColor;

			// Global Search

			GlobalSearch.HeaderTextColor = Color.FromName ("#ffffff");
			GlobalSearch.HeaderBackgroundColor = Color.FromName ("#5a5a5a");
			GlobalSearch.SeparatorLineColor = Color.FromName ("#595959");
			GlobalSearch.BackgroundColor = Color.FromName ("#696969");
			GlobalSearch.SelectionBackgroundColor = Color.FromName ("#cccccc");
			GlobalSearch.ResultTextColor = Color.FromName ("#ffffff");
			GlobalSearch.ResultDescriptionTextColor = Color.FromName ("#a3a3a3");

			// New Project Dialog

			NewProjectDialog.BannerBackgroundColor = new Color (119d / 255d, 130d / 255d, 140d / 255d);
			NewProjectDialog.BannerLineColor = ThinSplitterColor;
			NewProjectDialog.BannerForegroundColor = new Color (255d / 255d, 255d / 255d, 255d / 255d);
			NewProjectDialog.CategoriesBackgroundColor = new Color (85d / 255d, 85d / 255d, 85d / 255d);
			NewProjectDialog.TemplateListBackgroundColor = new Color (90d / 255d, 90d / 255d, 90d / 255d);
			NewProjectDialog.TemplateBackgroundColor = new Color (105d / 255d, 105d / 255d, 105d / 255d);
			NewProjectDialog.TemplateSectionSeparatorColor = ThinSplitterColor;
			NewProjectDialog.TemplateLanguageButtonBackground = new Color (97d / 255d, 97d / 255d, 97d / 255d);
			NewProjectDialog.TemplateLanguageButtonTriangle = new Color (255d / 255d, 255d / 255d, 255d / 255d);
			NewProjectDialog.ProjectConfigurationLeftHandBackgroundColor = new Color (85d / 255d, 85d / 255d, 85d / 255d);
			NewProjectDialog.ProjectConfigurationRightHandBackgroundColor = NewProjectDialog.TemplateBackgroundColor;
			NewProjectDialog.ProjectConfigurationPreviewLabelColor = Shift (BaseForegroundColor, 0.8);
			NewProjectDialog.ProjectConfigurationSeparatorColor = ThinSplitterColor;
		}
	}
}

