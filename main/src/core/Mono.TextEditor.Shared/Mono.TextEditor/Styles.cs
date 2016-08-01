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
using MonoDevelop.Core;
using Xwt.Drawing;

namespace Mono.TextEditor.PopupWindow
{
	static class Styles
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
			var bgColor = Platform.IsMac ? Color.FromName ("#5189ed") : Color.FromName ("#cce8ff");
			var fgColor = Platform.IsMac ? Color.FromName ("#ffffff") : Color.FromName ("#000000");

			ModeHelpWindowTokenOutlineColor = fgColor;
			ModeHelpWindowTokenTextColor = fgColor;

			InsertionCursorBackgroundColor = bgColor;
			InsertionCursorBorderColor = InsertionCursorBackgroundColor;
			InsertionCursorTitleTextColor = fgColor;
			InsertionCursorTextColor = InsertionCursorTitleTextColor;
			InsertionCursorLineColor = bgColor;

			TableLayoutModeBackgroundColor = new Color (1, 1, 1);
			TableLayoutModeTitleBackgroundColor = new Color (0.88, 0.88, 0.98);
			TableLayoutModeCategoryBackgroundColor = new Color (0.58, 0.58, 0.98);
			TableLayoutModeBorderColor = new Color (0.4, 0.4, 0.6);
			TableLayoutModeTextColor = new Color (0.3, 0.3, 1);
			TableLayoutModeGridColor = new Color (0.8, 0.8, 0.8);
		}
	}
}

