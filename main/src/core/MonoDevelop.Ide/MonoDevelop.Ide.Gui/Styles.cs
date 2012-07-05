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
		public static readonly Cairo.Color TabBarGradientStartColor = new Cairo.Color (248d / 255d, 248d / 255d, 248d / 255d);
		public static readonly Cairo.Color TabBarGradientMidColor = new Cairo.Color (217d / 255d, 217d / 255d, 217d / 255d);
		public static readonly Cairo.Color TabBarGradientEndColor = new Cairo.Color (183d / 255d, 183d / 255d, 183d / 255d);
		public static readonly Cairo.Color BreadcrumbBackgroundColor = new Cairo.Color (77d / 255d, 77d / 255d, 77d / 255d);
		public static readonly Cairo.Color BreadcrumbGradientStartColor = new Cairo.Color (100d / 255d, 100d / 255d, 100d / 255d);
		public static readonly Cairo.Color BreadcrumbGradientEndColor = new Cairo.Color (51d / 255d, 51d / 255d, 51d / 255d);
		public static readonly Cairo.Color BreadcrumbBorderColor = new Cairo.Color (55d / 255d, 55d / 255d, 55d / 255d);
		public static readonly Cairo.Color BreadcrumbInnerBorderColor = new Cairo.Color (1, 1, 1, 0.1d);
		public static readonly Gdk.Color BreadcrumbTextColor = new Gdk.Color (204, 204, 204);
		public static readonly Cairo.Color BreadcrumbButtonBorderColor = new Cairo.Color (204d / 255d, 204d / 255d, 204d / 255d);
		public static readonly Cairo.Color BreadcrumbButtonFillColor = new Cairo.Color (1, 1, 1, 0.1d);

		public static readonly Cairo.Color DockTabBarGradientTop = new Cairo.Color (248d / 255d, 248d / 255d, 248d / 255d);
		public static readonly Cairo.Color DockTabBarGradientStart = new Cairo.Color (242d / 255d, 242d / 255d, 242d / 255d);
		public static readonly Cairo.Color DockTabBarGradientEnd = new Cairo.Color (230d / 255d, 230d / 255d, 230d / 255d);
		public static readonly Cairo.Color DockTabBarShadowGradientStart = new Cairo.Color (154d / 255d, 154d / 255d, 154d / 255d, 1);
		public static readonly Cairo.Color DockTabBarShadowGradientEnd = new Cairo.Color (154d / 255d, 154d / 255d, 154d / 255d, 0);

		public static readonly Gdk.Color BrowserPadBackground = new Gdk.Color (219, 224, 231);
		public static readonly Gdk.Color PadBackground = new Gdk.Color (240, 240, 240);

		public static readonly Gdk.Color DockFrameBackground = new Gdk.Color (157, 162, 166);

		public static readonly Cairo.Color WidgetBorderColor = CairoExtensions.ParseColor ("8c8c8c");
	}
}

