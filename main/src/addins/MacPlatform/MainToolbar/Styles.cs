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
using MonoDevelop.Ide;
using Xwt.Drawing;

namespace MonoDevelop.MacIntegration.MainToolbar
{
	public static class Styles
	{
		public static Color BaseBackgroundColor { get; private set; }
		public static Color BaseForegroundColor { get; private set; }
		public static Color DisabledForegroundColor { get; private set; }

		public static Color StatusErrorTextColor { get; private set; }
		public static Color StatusWarningTextColor { get; private set; }
		public static Color StatusReadyTextColor { get; private set; }

		// Dark workaround colors
		public static Color DarkBorderColor { get; private set; }
		public static Color DarkBorderBrokenColor { get; private set; }
		public static Color DarkToolbarBackgroundColor { get; private set; }

		static Styles ()
		{
			LoadStyles ();
			Ide.Gui.Styles.Changed +=  (o, e) => LoadStyles ();
		}

		public static void LoadStyles ()
		{
			if (IdeApp.Preferences.UserInterfaceTheme == Theme.Light) {
				BaseBackgroundColor = Ide.Gui.Styles.BaseBackgroundColor;
				BaseForegroundColor = Ide.Gui.Styles.BaseForegroundColor;
				DisabledForegroundColor = Xwt.Mac.Util.ToXwtColor (AppKit.NSColor.DisabledControlText); //Ide.Gui.Styles.SecondaryTextColor;
				StatusErrorTextColor = Color.FromName ("#fa5433");
				StatusWarningTextColor = Color.FromName ("#e8bd0d");
				StatusReadyTextColor = Color.FromName ("#7f7f7f");
			} else {
				BaseBackgroundColor = Color.FromName ("#000000");
				BaseForegroundColor = Color.FromName ("#ffffff");
				DisabledForegroundColor = Color.FromName ("#e1e1e1");
				StatusErrorTextColor = Color.FromName ("#fa5433");
				StatusWarningTextColor = Color.FromName ("#e8bd0d");
				StatusReadyTextColor = Color.FromName ("#7f7f7f");

				DarkBorderColor = Color.FromName ("#8f8f8f");

				// With the NSAppearance.NameVibrantDark appearance the first time a NSButtonCell
				// is drawn it has a filter of some sort attached so that the colours are made lighter onscreen.
				// To get the DarkBorderColor we need to use a workaround.
				// See comment in ColoredButtonCell.DrawBezelWithFrame (RunButton.cs)
				DarkBorderBrokenColor = Color.FromName ("#3e3e3e");

				DarkToolbarBackgroundColor = Color.FromName ("#4e4e4e");
			}
		}
	}
}

