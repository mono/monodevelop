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
		public static Xwt.Drawing.Color PackageSourceUrlTextColor { get; internal set; }
		public static Xwt.Drawing.Color PackageSourceUrlSelectedTextColor { get; internal set; }
		public static Xwt.Drawing.Color PackageSourceErrorTextColor { get; internal set; }
		public static Xwt.Drawing.Color PackageSourceErrorSelectedTextColor { get; internal set; }
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
				CellBackgroundColor = MonoDevelop.Ide.Gui.Styles.PadBackground;
				PackageSourceUrlTextColor = Xwt.Drawing.Color.FromName ("#ff00ff"); // TODO: VV: 747474
				PackageSourceErrorTextColor = Xwt.Drawing.Color.FromName ("#ff00ff"); // TODO: VV: 656565
			} else {
				CellBackgroundColor = Xwt.Drawing.Color.FromName ("#272727");
				PackageSourceUrlTextColor = Xwt.Drawing.Color.FromName ("#ff00ff"); // TODO: VV: 656565
				PackageSourceErrorTextColor = Xwt.Drawing.Color.FromName ("#ff00ff"); // TODO: VV: ff0000
			}

			// Shared

			BackgroundColor = MonoDevelop.Ide.Gui.Styles.PrimaryBackgroundColor;

			CellTextColor = MonoDevelop.Ide.Gui.Styles.BaseForegroundColor;
			CellStrongSelectionColor = MonoDevelop.Ide.Gui.Styles.BaseSelectionBackgroundColor;
			CellSelectionColor = MonoDevelop.Ide.Gui.Styles.BaseSelectionBackgroundColor;
			CellTextSelectionColor = MonoDevelop.Ide.Gui.Styles.BaseSelectionTextColor;

			PackageSourceUrlSelectedTextColor = PackageSourceUrlTextColor;
			PackageSourceErrorSelectedTextColor = PackageSourceErrorTextColor;
			PackageInfoBackgroundColor = MonoDevelop.Ide.Gui.Styles.SecondaryBackgroundLighterColor;

			LineBorderColor = MonoDevelop.Ide.Gui.Styles.SeparatorColor;

			ErrorBackgroundColor = MonoDevelop.Ide.Gui.Styles.StatusWarningBackgroundColor;
			ErrorForegroundColor = MonoDevelop.Ide.Gui.Styles.StatusWarningTextColor;
		}
	}
}

