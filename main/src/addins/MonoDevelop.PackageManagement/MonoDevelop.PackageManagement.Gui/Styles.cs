//
// Styles.cs
//
// Author:
//       Vsevolod Kukol <sevo@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://www.xamarin.com)
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
using MonoDevelop.Ide;
using MonoDevelop.Components;

namespace MonoDevelop.PackageManagement
{
	public static class Styles
	{
		public static Xwt.Drawing.Color LineBorderColor { get; internal set; }
		public static Xwt.Drawing.Color BackgroundColor { get; internal set; }
		public static Xwt.Drawing.Color PackageInfoBackgroundColor { get; internal set; }
		public static Xwt.Drawing.Color CellBackgroundColor { get; internal set; }
		public static Xwt.Drawing.Color CellSelectionColor { get; internal set; }
		public static Xwt.Drawing.Color CellStrongSelectionColor { get; internal set; }
		public static Xwt.Drawing.Color CellTextColor { get; internal set; }
		public static Xwt.Drawing.Color CellTextSelectionColor { get; internal set; }
		public static Cairo.Color PackageSourceUrlTextColor { get; internal set; }
		public static Cairo.Color PackageSourceUrlSelectedTextColor { get; internal set; }
		public static Cairo.Color PackageSourceErrorTextColor { get; internal set; }
		public static Cairo.Color PackageSourceErrorSelectedTextColor { get; internal set; }
		public static Xwt.Drawing.Color ErrorBackgroundColor { get; internal set; }
		public static Xwt.Drawing.Color ErrorForegroundColor { get; internal set; }

		static Styles ()
		{
			LoadStyles ();
			MonoDevelop.Ide.Gui.Styles.Changed +=  (o, e) => LoadStyles ();
		}

		public static void LoadStyles ()
		{
			if (IdeApp.Preferences.UserInterfaceSkin == Skin.Light) {
				LineBorderColor = Xwt.Drawing.Color.FromBytes (163, 166, 171);
				BackgroundColor = MonoDevelop.Ide.Gui.Styles.BaseBackgroundColor.ToXwtColor ();
				PackageInfoBackgroundColor = Xwt.Drawing.Color.FromBytes (227, 231, 237);
				CellBackgroundColor = Xwt.Drawing.Color.FromBytes (243, 246, 250);
				CellSelectionColor = Xwt.Drawing.Color.FromBytes (204, 204, 204);
				CellStrongSelectionColor = Xwt.Drawing.Color.FromBytes (49, 119, 216);
				CellTextSelectionColor = Xwt.Drawing.Colors.White;
				CellTextColor = Xwt.Drawing.Colors.Black;
				PackageSourceUrlTextColor = CairoExtensions.ParseColor ("#747474");
				PackageSourceErrorTextColor = CairoExtensions.ParseColor ("#656565");
				PackageSourceUrlSelectedTextColor = CairoExtensions.ParseColor ("#747474");
				PackageSourceErrorSelectedTextColor = CairoExtensions.ParseColor ("#656565");
				ErrorBackgroundColor = Xwt.Drawing.Colors.Orange;
				ErrorForegroundColor = Xwt.Drawing.Colors.White;
			} else {
				LineBorderColor = Xwt.Drawing.Color.FromBytes (89, 89, 89);
				BackgroundColor = MonoDevelop.Ide.Gui.Styles.BaseBackgroundColor.ToXwtColor ();
				PackageInfoBackgroundColor = Xwt.Drawing.Color.FromBytes (105, 105, 105);
				CellBackgroundColor = MonoDevelop.Ide.Gui.Styles.BackgroundColor.ToXwtColor();
				CellSelectionColor = Xwt.Drawing.Color.FromBytes (90, 90, 90);
				CellStrongSelectionColor = Xwt.Drawing.Color.FromBytes (49, 119, 216);
				CellTextSelectionColor = Xwt.Drawing.Colors.White;
				CellTextColor = Xwt.Drawing.Colors.White;
				PackageSourceUrlTextColor = CairoExtensions.ParseColor ("#656565");
				PackageSourceErrorTextColor = CairoExtensions.ParseColor ("#ff0000");
				PackageSourceUrlSelectedTextColor = CairoExtensions.ParseColor ("#656565");
				PackageSourceErrorSelectedTextColor = CairoExtensions.ParseColor ("#ff0000");
				ErrorBackgroundColor = Xwt.Drawing.Colors.Orange;
				ErrorForegroundColor = Xwt.Drawing.Colors.White;
			}
		}
	}
}

