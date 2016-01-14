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
				LineBorderColor = Xwt.Drawing.Color.FromName ("#e9e9eb");
				BackgroundColor = Xwt.Drawing.Color.FromName ("#ffffff");
				PackageInfoBackgroundColor = Xwt.Drawing.Color.FromName ("#f0f1f3");
				CellBackgroundColor = Xwt.Drawing.Color.FromName ("#fafafa");
				CellSelectionColor = Xwt.Drawing.Color.FromName ("#cccccc");
				CellTextColor = Xwt.Drawing.Color.FromName ("#555555");
				PackageSourceUrlTextColor = Xwt.Drawing.Color.FromName ("#747474");
				PackageSourceErrorTextColor = Xwt.Drawing.Color.FromName ("#656565");
				PackageSourceUrlSelectedTextColor = Xwt.Drawing.Color.FromName ("#747474");
				PackageSourceErrorSelectedTextColor = Xwt.Drawing.Color.FromName ("#656565");
				ErrorBackgroundColor = Xwt.Drawing.Color.FromName ("#f1c40f");
				ErrorForegroundColor = Xwt.Drawing.Color.FromName ("#ffffff");
			} else {
				LineBorderColor = Xwt.Drawing.Color.FromName ("#595959"); // TODO
				BackgroundColor = MonoDevelop.Ide.Gui.Styles.BaseBackgroundColor; // TODO
				PackageInfoBackgroundColor = Xwt.Drawing.Color.FromName ("#696969"); // TODO
				CellBackgroundColor = MonoDevelop.Ide.Gui.Styles.BackgroundColor; // TODO
				CellSelectionColor = Xwt.Drawing.Color.FromName ("#5a5a5a"); // TODO
				CellTextColor = Xwt.Drawing.Color.FromName ("#ffffff"); // TODO
				PackageSourceUrlTextColor = Xwt.Drawing.Color.FromName ("#656565"); // TODO
				PackageSourceErrorTextColor = Xwt.Drawing.Color.FromName ("#ff0000"); // TODO
				PackageSourceUrlSelectedTextColor = Xwt.Drawing.Color.FromName ("#656565"); // TODO
				PackageSourceErrorSelectedTextColor = Xwt.Drawing.Color.FromName ("#ff0000"); // TODO
				ErrorBackgroundColor = Xwt.Drawing.Color.FromName ("#ffa500"); // TODO
				ErrorForegroundColor = Xwt.Drawing.Color.FromName ("#ffffff"); // TODO
			}

			CellStrongSelectionColor = MonoDevelop.Ide.Gui.Styles.BaseSelectionBackgroundColor;
			CellTextSelectionColor = MonoDevelop.Ide.Gui.Styles.BaseSelectionTextColor;
		}
	}
}

