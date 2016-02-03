//
// Styles.cs
//
// Author:
//       Vsevolod Kukol <sevo@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://www.xamarin.com)
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
using Xwt.Drawing;

namespace Mono.TextEditor.PopupWindow
{
	public static class Styles
	{
		public static Color ModeHelpWindowTokenOutlineColor { get; internal set; }
		public static Color ModeHelpWindowTokenTextColor { get; internal set; }

		public static Color InsertionCursorBackgroundColor { get; internal set; }
		public static Color InsertionCursorTitleTextColor { get; internal set; }
		public static Color InsertionCursorBorderColor { get; internal set; }
		public static Color InsertionCursorTextColor { get; internal set; }
		public static Color InsertionCursorLineColor { get; internal set; }

		public static Color TableLayoutModeBackgroundColor { get; internal set; }
		public static Color TableLayoutModeTitleBackgroundColor { get; internal set; }
		public static Color TableLayoutModeCategoryBackgroundColor { get; internal set; }
		public static Color TableLayoutModeBorderColor { get; internal set; }
		public static Color TableLayoutModeTextColor { get; internal set; }
		public static Color TableLayoutModeGridColor { get; internal set; }

		static Styles ()
		{
			LoadStyles ();
			Context.GlobalStylesChanged += (o, e) => LoadStyles ();
		}

		public static void LoadStyles ()
		{
			if (!Context.HasGlobalStyle ("dark")) { // light
				
				// TODO: VV: #555555 needs to be fg_color from gtkrc
				// TODO: VV: #f2f2f2 needs to be tooltip_bg_color from gtkrc

				ModeHelpWindowTokenOutlineColor = Color.FromName ("#666666"); // TODO: VV: review color
				ModeHelpWindowTokenTextColor = Color.FromName ("#555555"); // TODO: VV: review color

				InsertionCursorBackgroundColor = Color.FromName ("#f2f2f2"); // TODO: VV: review color
				InsertionCursorTitleTextColor = Color.FromName ("#242424"); // TODO: VV: review color
				InsertionCursorBorderColor = Color.FromName ("#d5d5d5"); // TODO: VV: review color
				InsertionCursorTextColor = Color.FromName ("#4c4c4c"); // TODO: VV: review color
				InsertionCursorLineColor = Color.FromName ("#666666"); // TODO: VV: review color

			} else { // dark

				ModeHelpWindowTokenOutlineColor = Color.FromName ("#666666"); // TODO: VV: review color
				ModeHelpWindowTokenTextColor = Color.FromName ("#555555"); // TODO: VV: review color

				InsertionCursorBackgroundColor = Color.FromName ("#00f2f2"); // TODO: VV: review color
				InsertionCursorTitleTextColor = Color.FromName ("#00ff00"); // TODO: VV: review color
				InsertionCursorBorderColor = Color.FromName ("#d5d5d5"); // TODO: VV: review color
				InsertionCursorTextColor = Color.FromName ("#0000ff"); // TODO: VV: review color
				InsertionCursorLineColor = Color.FromName ("#666666");// TODO: VV: review color

			}

			TableLayoutModeBackgroundColor = new Color (1, 1, 1);
			TableLayoutModeTitleBackgroundColor = new Color (0.88, 0.88, 0.98);
			TableLayoutModeCategoryBackgroundColor = new Color (0.58, 0.58, 0.98);
			TableLayoutModeBorderColor = new Color (0.4, 0.4, 0.6);
			TableLayoutModeTextColor = new Color (0.3, 0.3, 1);
			TableLayoutModeGridColor = new Color (0.8, 0.8, 0.8);
		}

		public static Cairo.Color ToCairoColor (this Xwt.Drawing.Color color)
		{
			return new Cairo.Color (color.Red, color.Green, color.Blue, color.Alpha);
		}
	}
}

