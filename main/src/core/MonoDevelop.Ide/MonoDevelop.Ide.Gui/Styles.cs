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
using System.Security.Policy;

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
		public static Color BaseIconColor { get; internal set; }
		public static Color LinkForegroundColor { get; internal set; }
		
		public static Pango.FontDescription DefaultFont { get; internal set; }
		public static string DefaultFontName { get; internal set; }

		public static double FontScale11 = 0.92308;
		public static double FontScale12 = 1;
		public static double FontScale13 = 1.07693;
		public static double FontScale14 = 1.15385;

		public static Color ThinSplitterColor { get; internal set; }
		public static Color SeparatorColor { get; internal set; }
		public static Color PrimaryBackgroundColor { get; internal set; }
		public static Color SecondaryBackgroundLighterColor { get; internal set; }
		public static Color SecondaryBackgroundDarkerColor { get; internal set; }
		public static Color DimTextColor { get; internal set; }
		public static Color StatusInformationBackgroundColor { get; internal set; }
		public static Color StatusInformationTextColor { get; internal set; }
		public static Color StatusWarningBackgroundColor { get; internal set; }
		public static Color StatusWarningTextColor { get; internal set; }
		public static Color StatusErrorBackgroundColor { get; internal set; }
		public static Color StatusErrorTextColor { get; internal set; }

		// Document tab bar

		public static Color TabBarBackgroundColor { get; internal set; }
		public static Color TabBarActiveTextColor { get; internal set; }
		public static Color TabBarNotifyTextColor { get; internal set; }
		public static Color TabBarInactiveTextColor { get; internal set; }

		public static Color BreadcrumbBackgroundColor { get; internal set; }
		public static Color BreadcrumbTextColor { get; internal set; }
		public static Color BreadcrumbButtonFillColor { get; internal set; }
		public static Color BreadcrumbBottomBorderColor { get; internal set; }

		// Document Subview Tabs

		public static Color SubTabBarBackgroundColor { get; internal set; }
		public static Color SubTabBarTextColor { get; internal set; }
		public static Color SubTabBarActiveBackgroundColor { get; internal set; }
		public static Color SubTabBarActiveTextColor { get; internal set; }
		public static Color SubTabBarHoverBackgroundColor { get; internal set; }
		public static Color SubTabBarSeparatorColor { get; internal set; }

		// Dock pads

		public static Color PadBackground { get; internal set; }
		public static Color InactivePadBackground { get; internal set; }
		public static Color PadLabelColor { get; internal set; }
		public static Color InactivePadLabelColor { get; internal set; }
		public static Color DockFrameBackground { get; internal set; }
		public static Color DockSeparatorColor { get; internal set; }
		public static Color DockBarBackground { get; internal set; }
		public static Color DockBarPrelightColor { get; internal set; }
		public static Color DockBarLabelColor { get; internal set; }

		public static Color BrowserPadBackground { get; internal set; }
		public static Color InactiveBrowserPadBackground { get; internal set; }

		public static Color PadCategoryBackgroundColor { get; internal set; }
		public static Color PadCategoryBorderColor { get; internal set; }
		public static Color PadCategoryLabelColor { get; internal set; }

		public static Color PropertyPadLabelBackgroundColor { get; internal set; }
		public static Color PropertyPadDividerColor { get; internal set; }

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

		// Code Completion

		public static readonly int TooltipInfoSpacing;

		// Popover Windows

		public static class PopoverWindow
		{
			public static readonly int PagerTriangleSize = 6;
			public static readonly int PagerHeight = 16;

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
			public static Color ResultMatchTextColor { get; internal set; }
			public static Color SelectedResultTextColor { get; internal set; }
			public static Color SelectedResultDescriptionTextColor { get; internal set; }
			public static Color SelectedResultMatchTextColor { get; internal set; }
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
			public static Color SearchErrorBackgroundColor { get; internal set; }
			public static Color SearchErrorForegroundColor { get; internal set; }
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
			LoadStyle ();
		}

		internal static void LoadStyle ()
		{
			Gtk.Style defaultStyle;
			Gtk.Widget styledWidget;
			if (IdeApp.Workbench == null || IdeApp.Workbench.RootWindow == null) {
				styledWidget = new Gtk.Label (String.Empty);
				defaultStyle = styledWidget.Style;
			} else {
				styledWidget = IdeApp.Workbench.RootWindow;
				defaultStyle = Gtk.Rc.GetStyle (styledWidget);
			}

			BackgroundColor = defaultStyle.Background (Gtk.StateType.Normal).ToXwtColor ();	// must be the bg color from Gtkrc
			BaseBackgroundColor = defaultStyle.Base (Gtk.StateType.Normal).ToXwtColor ();	// must be the base color from Gtkrc
			BaseForegroundColor = defaultStyle.Foreground (Gtk.StateType.Normal).ToXwtColor ();	// must be the text color from Gtkrc
			BaseSelectionBackgroundColor = defaultStyle.Base (Gtk.StateType.Selected).ToXwtColor ();
			BaseSelectionTextColor = defaultStyle.Text (Gtk.StateType.Selected).ToXwtColor ();

			LinkForegroundColor = ((Gdk.Color)styledWidget.StyleGetProperty ("link-color")).ToXwtColor ();
			if (LinkForegroundColor == Colors.Black) // the style returs black when not initialized
				LinkForegroundColor = Colors.Blue;   // set the link color to generic blue until initialization is finished

			DefaultFont = defaultStyle.FontDescription.Copy ();
			DefaultFontName = DefaultFont.ToString ();

			if (IdeApp.Preferences == null || IdeApp.Preferences.UserInterfaceSkin == Skin.Light)
				LoadLightStyle ();
			else
				LoadDarkStyle ();

			// Shared colors

			DockBarLabelColor = BaseIconColor;
			DockSeparatorColor = DockFrameBackground;
			PropertyPadLabelBackgroundColor = PrimaryBackgroundColor;
			PadCategoryBorderColor = SeparatorColor;
			PadCategoryLabelColor = BaseForegroundColor;
			PadCategoryBackgroundColor = SecondaryBackgroundLighterColor;
			PadLabelColor = BaseForegroundColor;
			SubTabBarActiveBackgroundColor = BaseSelectionBackgroundColor;
			SubTabBarActiveTextColor = BaseSelectionTextColor;
			SubTabBarSeparatorColor = SubTabBarTextColor;
			InactiveBrowserPadBackground = InactivePadBackground;

			// Tabs

			TabBarBackgroundColor = DockFrameBackground;
			TabBarInactiveTextColor = InactivePadLabelColor;
			TabBarActiveTextColor = BaseForegroundColor;

			// Breadcrumbs

			BreadcrumbTextColor = BaseForegroundColor;

			// Document Subview Tabs

			SubTabBarTextColor = BaseForegroundColor;

			// Popover Window

			PopoverWindow.InformationBackgroundColor = StatusInformationBackgroundColor;
			PopoverWindow.InformationTextColor = StatusInformationTextColor;
			PopoverWindow.WarningBackgroundColor = StatusWarningBackgroundColor;
			PopoverWindow.WarningTextColor = StatusWarningTextColor;
			PopoverWindow.ErrorBackgroundColor = StatusErrorBackgroundColor;
			PopoverWindow.ErrorTextColor = StatusErrorTextColor;

			// Code Completion

			CodeCompletion.SelectionBackgroundColor = BaseSelectionBackgroundColor;
			CodeCompletion.SelectionTextColor = BaseSelectionTextColor;

			// Global Search

			GlobalSearch.BackgroundColor = PrimaryBackgroundColor;
			GlobalSearch.HeaderBackgroundColor = SecondaryBackgroundLighterColor;
			GlobalSearch.HeaderTextColor = DimTextColor;
			GlobalSearch.SeparatorLineColor = SeparatorColor;
			GlobalSearch.SelectionBackgroundColor = BaseSelectionBackgroundColor;
			GlobalSearch.ResultTextColor = BaseForegroundColor;
			GlobalSearch.ResultDescriptionTextColor = DimTextColor;
			GlobalSearch.SelectedResultTextColor = BaseSelectionTextColor;
			GlobalSearch.SelectedResultDescriptionTextColor = BaseSelectionTextColor;
			GlobalSearch.SelectedResultMatchTextColor = BaseSelectionTextColor;

			// New Project Dialog

			NewProjectDialog.TemplateBackgroundColor = PrimaryBackgroundColor;
			NewProjectDialog.TemplateLanguageButtonTriangle = BaseIconColor;
			NewProjectDialog.ProjectConfigurationPreviewLabelColor = BaseForegroundColor;
			NewProjectDialog.CategoriesBackgroundColor = SecondaryBackgroundDarkerColor;
			NewProjectDialog.ProjectConfigurationLeftHandBackgroundColor = SecondaryBackgroundDarkerColor;
			NewProjectDialog.ProjectConfigurationRightHandBackgroundColor = PrimaryBackgroundColor;

			// Editor

			Editor.SmartTagMarkerColorLight = Color.FromName ("#ff70fe").WithAlpha (.5);
			Editor.SmartTagMarkerColorDark = Color.FromName ("#ffffff").WithAlpha (.5);
			Editor.SearchErrorBackgroundColor = Color.FromName ("#ff6666");
			Editor.SearchErrorForegroundColor = BaseForegroundColor;

			if (Changed != null)
				Changed (null, EventArgs.Empty);
		}

		internal static void LoadLightStyle ()
		{
			BaseIconColor = Color.FromName ("#575757");
			ThinSplitterColor = Color.FromName ("#dadada");
			SeparatorColor = Color.FromName ("#f2f2f4");
			PrimaryBackgroundColor = BaseBackgroundColor;
			SecondaryBackgroundDarkerColor = Color.FromName ("#e7eaee");
			SecondaryBackgroundLighterColor = Color.FromName ("#f9f9fb");
			DimTextColor = Color.FromName ("#888888");
			PadBackground = Color.FromName ("#fafafa");
			InactivePadBackground = Color.FromName ("#e8e8e8");
			InactivePadLabelColor = Color.FromName ("#777777");
			DockFrameBackground = Color.FromName ("#bfbfbf");
			DockBarBackground = Color.FromName ("#dddddd");
			DockBarPrelightColor = Color.FromName ("#eeeeee");
			BrowserPadBackground = Color.FromName ("#ebedf0");
			PropertyPadDividerColor = Color.FromName ("#efefef");

			// these colors need to match colors from status icons
			StatusInformationBackgroundColor = Color.FromName ("#87b6f0");
			StatusInformationTextColor = BaseBackgroundColor;
			StatusWarningBackgroundColor = Color.FromName ("#f1c40f");
			StatusWarningTextColor = BaseBackgroundColor;
			StatusErrorBackgroundColor = Color.FromName ("#f56d4f");
			StatusErrorTextColor = BaseBackgroundColor;

			// Tabs

			TabBarNotifyTextColor = Color.FromName ("#ff00ff"); // TODO: VV

			// Breadcrumb

			BreadcrumbBackgroundColor = PadBackground;
			BreadcrumbButtonFillColor = BaseSelectionBackgroundColor.WithAlpha (0.2);
			BreadcrumbBottomBorderColor = DockBarBackground;

			// Document Subview Tabs

			SubTabBarBackgroundColor = PadBackground;
			SubTabBarHoverBackgroundColor = BaseSelectionBackgroundColor.WithAlpha (0.2);

			// WidgetBorderColor = Color.FromName ("#ff00ff"); // TODO: 8c8c8c - UNUSED (used for custom drawn `SearchEntry` but it isn’t used anymore, so its deprecated)

			// Status area (GTK)
			// FIXME: VV: Will test after the preview build

			StatusBarBorderColor = Color.FromName ("#ff00ff"); // TODO: VV: 919191
			StatusBarFill1Color = Color.FromName ("#ff00ff"); // TODO: VV: f5fafc
			StatusBarFill2Color = Color.FromName ("#ff00ff"); // TODO: VV: e9f1f3
			StatusBarFill3Color = Color.FromName ("#ff00ff"); // TODO: VV: d8e7ea
			StatusBarFill4Color = Color.FromName ("#ff00ff"); // TODO: VV: d1e3e7
			StatusBarErrorColor = Color.FromName ("#ff00ff"); // TODO: VV: FF6363
			StatusBarInnerColor = Color.FromName ("#ff00ff").WithAlpha (.08); // TODO: VV: 000000
			StatusBarShadowColor1 = Color.FromName ("#ff00ff").WithAlpha (.06); // TODO: VV: 000000
			StatusBarShadowColor2 = Color.FromName ("#ff00ff").WithAlpha (.02); // TODO: VV: 000000
			StatusBarTextColor = BaseForegroundColor; // TODO: VV
			StatusBarProgressBackgroundColor = Color.FromName ("#ff00ff").WithAlpha (.1); // TODO: VV: 000000
			StatusBarProgressOutlineColor = Color.FromName ("#ff00ff").WithAlpha (.1); // TODO: VV: 000000

			// Toolbar

			ToolbarBottomBorderColor = Color.FromName ("#afafaf");

			// Global Search

			GlobalSearch.ResultMatchTextColor = Color.FromName ("#4d4d4d");

			// Popover Window

			PopoverWindow.DefaultBackgroundColor = Color.FromName ("#f2f2f2"); // gtkrc @tooltip_bg_color
			PopoverWindow.DefaultTextColor = DimTextColor;
			PopoverWindow.ShadowColor = Color.FromName ("#000000").WithAlpha (.05);

			PopoverWindow.ParamaterWindows.GradientStartColor = Color.FromName ("#fffee6");
			PopoverWindow.ParamaterWindows.GradientEndColor = Color.FromName ("#fffcd1");

			// Code Completion

			CodeCompletion.BackgroundColor = Color.FromName ("#eef1f2");
			CodeCompletion.TextColor = Color.FromName ("#646566");
			CodeCompletion.HighlightColor = Color.FromName ("#ba3373");
			CodeCompletion.SelectionBackgroundInactiveColor = Color.FromName ("#bbbbbb");
			CodeCompletion.SelectionHighlightColor = CodeCompletion.HighlightColor;

			// New Project Dialog

			NewProjectDialog.BannerBackgroundColor = Color.FromName ("#77828c");
			NewProjectDialog.BannerLineColor = Color.FromName ("#707a83");
			NewProjectDialog.BannerForegroundColor = BaseBackgroundColor;
			NewProjectDialog.TemplateListBackgroundColor = Color.FromName ("#f9f9fa");
			NewProjectDialog.TemplateSectionSeparatorColor = Color.FromName ("#e2e2e2");
			NewProjectDialog.TemplateLanguageButtonBackground = BaseBackgroundColor;
			NewProjectDialog.ProjectConfigurationSeparatorColor = Color.FromName ("#d2d5d9");
		}

		internal static void LoadDarkStyle ()
		{
			BaseIconColor = Color.FromName ("#bfbfbf");
			ThinSplitterColor = Color.FromName ("#282828");
			SeparatorColor = Color.FromName ("#4e4e4e");
			PrimaryBackgroundColor = Color.FromName ("#575757");
			SecondaryBackgroundDarkerColor = Color.FromName ("#484b55");
			SecondaryBackgroundLighterColor = Color.FromName ("#616161");
			DimTextColor = Color.FromName ("#999999");
			PadBackground = Color.FromName ("#525252");
			InactivePadBackground = Color.FromName ("#474747");
			InactivePadLabelColor = Color.FromName ("#808080");
			DockFrameBackground = Color.FromName ("#303030");
			DockBarBackground = Color.FromName ("#5a5a5a");
			DockBarPrelightColor = Color.FromName ("#666666");
			BrowserPadBackground = Color.FromName ("#484b55");
			PropertyPadDividerColor = SeparatorColor;

			// these colors need to match colors from status icons
			StatusInformationBackgroundColor = Color.FromName ("#8fc1ff");
			StatusInformationTextColor = Color.FromName ("#394d66");
			StatusWarningBackgroundColor = Color.FromName ("#ffcf0f");
			StatusWarningTextColor = Color.FromName ("#665206");
			StatusErrorBackgroundColor = Color.FromName ("#ff7152");
			StatusErrorTextColor = Color.FromName ("#662d20");

			// Tabs

			TabBarNotifyTextColor = Color.FromName ("#ff00ff"); // TODO: VV

			// Breadcrumb

			BreadcrumbBackgroundColor = Color.FromName ("#525252");
			BreadcrumbButtonFillColor = Color.FromName ("#616161");
			BreadcrumbBottomBorderColor = BreadcrumbBackgroundColor;

			// Document Subview Tabs

			SubTabBarBackgroundColor = Color.FromName ("#525252");
			SubTabBarHoverBackgroundColor = Color.FromName ("#616161");

			// Status area (GTK)
			// FIXME: Will test after the preview build

			StatusBarBorderColor = Color.FromName ("#ff00ff"); // TODO: VV: 919191
			StatusBarFill1Color = Color.FromName ("#ff00ff"); // TODO: VV: f5fafc
			StatusBarFill2Color = Color.FromName ("#ff00ff"); // TODO: VV: e9f1f3
			StatusBarFill3Color = Color.FromName ("#ff00ff"); // TODO: VV: d8e7ea
			StatusBarFill4Color = Color.FromName ("#ff00ff"); // TODO: VV: d1e3e7
			StatusBarErrorColor = Color.FromName ("#ff00ff"); // TODO: VV: FF6363
			StatusBarInnerColor = Color.FromName ("#ff00ff").WithAlpha (.08); // TODO: VV: 000000
			StatusBarShadowColor1 = Color.FromName ("#ff00ff").WithAlpha (.06); // TODO: VV: 000000
			StatusBarShadowColor2 = Color.FromName ("#ff00ff").WithAlpha (.02); // TODO: VV: 000000
			StatusBarTextColor = BaseForegroundColor; // TODO: VV
			StatusBarProgressBackgroundColor = Color.FromName ("#ff00ff").WithAlpha (.1); // TODO: VV: 000000
			StatusBarProgressOutlineColor = Color.FromName ("#ff00ff").WithAlpha (.1); // TODO: VV: 000000

			// Toolbar

			ToolbarBottomBorderColor = Color.FromName ("#444444");

			// Global Search

			GlobalSearch.ResultMatchTextColor = BaseSelectionTextColor;

			// Popover window

			PopoverWindow.DefaultBackgroundColor = Color.FromName ("#5A5A5A");
			PopoverWindow.DefaultTextColor = Color.FromName ("#ffffff");
			PopoverWindow.ShadowColor = Color.FromName ("#000000").WithAlpha (0); // transparent since dark skin doesn't need shadows

			PopoverWindow.ParamaterWindows.GradientStartColor = Color.FromName ("#fffee6");
			PopoverWindow.ParamaterWindows.GradientEndColor = Color.FromName ("#fffcd1");

			// Code Completion

			CodeCompletion.BackgroundColor = Color.FromName ("#5b6365");
			CodeCompletion.TextColor = Color.FromName ("#c3c5c6");
			CodeCompletion.HighlightColor = Color.FromName ("#f9d33c");
			CodeCompletion.SelectionBackgroundInactiveColor = Color.FromName ("#bbbbbb");
			CodeCompletion.SelectionHighlightColor = CodeCompletion.HighlightColor;

			// New Project Dialog

			NewProjectDialog.BannerBackgroundColor = Color.FromName ("#77828c");
			NewProjectDialog.BannerLineColor = NewProjectDialog.BannerBackgroundColor;
			NewProjectDialog.BannerForegroundColor = Color.FromName ("#ffffff");
			NewProjectDialog.TemplateListBackgroundColor = Color.FromName ("#5a5a5a");
			NewProjectDialog.TemplateSectionSeparatorColor = ThinSplitterColor;
			NewProjectDialog.TemplateLanguageButtonBackground = SecondaryBackgroundDarkerColor;
			NewProjectDialog.ProjectConfigurationSeparatorColor = Color.FromName ("#5d616d");
		}
	}
}

