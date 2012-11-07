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
	static class Styles
	{
		public static readonly Cairo.Color BaseBackgroundColor = new Cairo.Color (1, 1, 1);
		public static readonly Cairo.Color BaseForegroundColor = new Cairo.Color (0, 0, 0);

		// General

		public static readonly Gdk.Color ThinSplitterColor = new Gdk.Color (166, 166, 166);

		// Document tab bar


		public static readonly Cairo.Color TabBarBackgroundColor = CairoExtensions.ParseColor ("c2c2c2");
		public static readonly Cairo.Color TabBarActiveTextColor = new Cairo.Color (0, 0, 0);

		public static readonly Cairo.Color TabBarGradientStartColor = Shift (TabBarBackgroundColor, 0.92);
		public static readonly Cairo.Color TabBarGradientMidColor = TabBarBackgroundColor;
		public static readonly Cairo.Color TabBarGradientEndColor = Shift (TabBarBackgroundColor, 0.9);
		public static readonly Cairo.Color TabBarGradientShadowColor = Shift (TabBarBackgroundColor, 0.8);
		public static readonly Cairo.Color TabBarHoverActiveTextColor = TabBarActiveTextColor;
		public static readonly Cairo.Color TabBarInactiveTextColor = Blend (new Cairo.Color (0, 0, 0), TabBarGradientStartColor, 0.4);
		public static readonly Cairo.Color TabBarHoverInactiveTextColor = new Cairo.Color (0, 0, 0);

		public static readonly Cairo.Color BreadcrumbGradientStartColor = CairoExtensions.ParseColor ("FFFFFF");
		public static readonly Cairo.Color BreadcrumbBackgroundColor = Shift (BreadcrumbGradientStartColor, .95);
		public static readonly Cairo.Color BreadcrumbGradientEndColor = Shift (BreadcrumbGradientStartColor, 0.9);
		public static readonly Cairo.Color BreadcrumbBorderColor = Shift (BreadcrumbBackgroundColor, 0.6);
		public static readonly Cairo.Color BreadcrumbInnerBorderColor = WithAlpha (BaseBackgroundColor, 0.1d);
		public static readonly Gdk.Color BreadcrumbTextColor = Shift (BaseForegroundColor, 0.8).ToGdkColor ();
		public static readonly Cairo.Color BreadcrumbButtonBorderColor = Shift (BaseBackgroundColor, 0.8);
		public static readonly Cairo.Color BreadcrumbButtonFillColor = WithAlpha (BaseBackgroundColor, 0.1d);
		public static readonly Cairo.Color BreadcrumbBottomBorderColor = Shift (BreadcrumbBackgroundColor, 0.7d);
		public static readonly bool BreadcrumbInvertedIcons = false;
		public static readonly bool BreadcrumbGreyscaleIcons = false;

		// Dock pads
		
		public static readonly Cairo.Color DockTabBarGradientTop = new Cairo.Color (248d / 255d, 248d / 255d, 248d / 255d);
		public static readonly Cairo.Color DockTabBarGradientStart = new Cairo.Color (242d / 255d, 242d / 255d, 242d / 255d);
		public static readonly Cairo.Color DockTabBarGradientEnd = new Cairo.Color (230d / 255d, 230d / 255d, 230d / 255d);
		public static readonly Cairo.Color DockTabBarShadowGradientStart = new Cairo.Color (154d / 255d, 154d / 255d, 154d / 255d, 1);
		public static readonly Cairo.Color DockTabBarShadowGradientEnd = new Cairo.Color (154d / 255d, 154d / 255d, 154d / 255d, 0);

		public static readonly Gdk.Color PadBackground = new Gdk.Color (240, 240, 240);
		public static readonly Gdk.Color InactivePadBackground = ReduceLight (PadBackground, 0.9);
		public static readonly Gdk.Color PadLabelColor = new Gdk.Color (92, 99, 102);
		public static readonly Gdk.Color DockFrameBackground = new Gdk.Color (157, 162, 166);
		public static readonly Gdk.Color DockSeparatorColor = ThinSplitterColor;

		public static readonly Gdk.Color BrowserPadBackground = new Gdk.Color (0xE5, 0xEC, 0xEE);
		public static readonly Gdk.Color InactiveBrowserPadBackground = ReduceLight (BrowserPadBackground, 0.92);

		public static readonly Cairo.Color DockBarBackground1 = PadBackground.ToCairoColor ();
		public static readonly Cairo.Color DockBarBackground2 = Shift (PadBackground.ToCairoColor (), 0.95);
		public static readonly Cairo.Color DockBarSeparatorColorDark = new Cairo.Color (0, 0, 0, 0.2);
		public static readonly Cairo.Color DockBarSeparatorColorLight = new Cairo.Color (1, 1, 1, 0.3);

		public static readonly Cairo.Color DockBarPrelightColor = CairoExtensions.ParseColor ("ffffff");

		// Status area

		public static readonly Cairo.Color WidgetBorderColor = CairoExtensions.ParseColor ("8c8c8c");

		public static readonly Cairo.Color StatusBarBorderColor = CairoExtensions.ParseColor ("919191");

		public static readonly Cairo.Color StatusBarFill1Color = CairoExtensions.ParseColor ("f5fafc");
		public static readonly Cairo.Color StatusBarFill2Color = CairoExtensions.ParseColor ("e9f1f3");
		public static readonly Cairo.Color StatusBarFill3Color = CairoExtensions.ParseColor ("d8e7ea");
		public static readonly Cairo.Color StatusBarFill4Color = CairoExtensions.ParseColor ("d1e3e7");

		public static readonly Cairo.Color StatusBarErrorColor = CairoExtensions.ParseColor ("FF6363");

		public static readonly Cairo.Color StatusBarInnerColor = new Cairo.Color (0,0,0, 0.08);
		public static readonly Cairo.Color StatusBarShadowColor1 = new Cairo.Color (0,0,0, 0.06);
		public static readonly Cairo.Color StatusBarShadowColor2 = new Cairo.Color (0,0,0, 0.03);
		public static readonly Cairo.Color StatusBarTextColor = CairoExtensions.ParseColor ("555555");
		public static readonly Cairo.Color StatusBarProgressBackgroundColor = new Cairo.Color (0, 0, 0, 0.1);
		public static readonly Cairo.Color StatusBarProgressOutlineColor = new Cairo.Color (0, 0, 0, 0.2);

		public static readonly Pango.FontDescription StatusFont = Pango.FontDescription.FromString ("Normal");

		public static readonly int StatusFontPixelHeight = 12;
		public static readonly int ProgressBarHeight = 16;
		public static readonly int ProgressBarInnerPadding = 3;
		public static readonly int ProgressBarOuterPadding = 3;

		// Toolbar

		public static readonly Cairo.Color ToolbarBottomBorderColor = new Cairo.Color (0.5, 0.5, 0.5);
		public static readonly Cairo.Color ToolbarBottomGlowColor = new Cairo.Color (1, 1, 1, 0.2);

		// Code Completion

		public static readonly int TooltipInfoSpacing = 1;

		// Popover Windows

		public static class PopoverWindow
		{
			public static readonly int PagerTriangleSize = 6;
			public static readonly int PagerHeight = 16;

			public static class ParamaterWindows
			{
				public static readonly Cairo.Color GradientStartColor = CairoExtensions.ParseColor ("fffee6");
				public static readonly Cairo.Color GradientEndColor = CairoExtensions.ParseColor ("fffcd1");
			}
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
			var c = new HslColor (color);
			c.L *= factor;
			return c;
		}

		internal static Cairo.Color IncreaseLight (Cairo.Color color, double factor)
		{
			var c = new HslColor (color);
			c.L += (1 - c.L) * factor;
			return c;
		}

		internal static Gdk.Color ReduceLight (Gdk.Color color, double factor)
		{
			return ReduceLight (color.ToCairoColor (), factor).ToGdkColor ();
		}

		internal static Gdk.Color IncreaseLight (Gdk.Color color, double factor)
		{
			return IncreaseLight (color.ToCairoColor (), factor).ToGdkColor ();
		}
	}
}

