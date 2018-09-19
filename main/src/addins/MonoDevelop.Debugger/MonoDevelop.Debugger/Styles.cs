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

using MonoDevelop.Ide;
using Xwt.Drawing;

namespace MonoDevelop.Debugger
{
	public static class Styles
	{
		public static Color ObjectValueTreeValuesButtonBackground { get; internal set; }
		public static Color ObjectValueTreeValuesButtonText { get; internal set; }
		public static Color ObjectValueTreeValuesButtonBorder { get; internal set; }
		public static Color ObjectValueTreeValueErrorText { get; internal set; }
		public static Color ObjectValueTreeValueDisabledText { get; internal set; }
		public static Color ObjectValueTreeValueModifiedText { get; internal set; }
		public static Color PreviewVisualizerBackgroundColor { get; internal set; }
		public static Color PreviewVisualizerTextColor { get; internal set; }
		public static Color PreviewVisualizerHeaderTextColor { get; internal set; }

		public static ExceptionCaughtDialogStyle ExceptionCaughtDialog { get; internal set; }

		public class ExceptionCaughtDialogStyle
		{
			public Color HeaderBackgroundColor { get; internal set; }
			public Color HeaderTextColor { get; internal set; }
			public Color TreeBackgroundColor { get; internal set; }
			public Color TreeTextColor { get; internal set; }
			public Color LineNumberTextColor { get; internal set; }
			public Color ExternalCodeTextColor { get; internal set; }
			public Color TreeSelectedBackgroundColor { get; internal set; }
			public Color TreeSelectedTextColor { get; internal set; }
			public Color ValueTreeBackgroundColor { get; internal set; }
		}

		static Styles ()
		{
			LoadStyles ();
			Ide.Gui.Styles.Changed +=  (o, e) => LoadStyles ();
		}

		public static void LoadStyles ()
		{
			ExceptionCaughtDialog = new ExceptionCaughtDialogStyle ();
			ExceptionCaughtDialog.TreeBackgroundColor = Ide.Gui.Styles.BrowserPadBackground;
			ExceptionCaughtDialog.TreeTextColor = Ide.Gui.Styles.BaseForegroundColor;
			ExceptionCaughtDialog.TreeSelectedBackgroundColor = Ide.Gui.Styles.BaseSelectionBackgroundColor;
			ExceptionCaughtDialog.TreeSelectedTextColor = Ide.Gui.Styles.BaseSelectionTextColor;
			ExceptionCaughtDialog.HeaderBackgroundColor = Color.FromName ("#a06705");
			ExceptionCaughtDialog.HeaderTextColor = Color.FromName ("#ffffff");

			if (IdeApp.Preferences.UserInterfaceTheme == Theme.Light) {
				ObjectValueTreeValuesButtonBackground = Color.FromName ("#e9f2fc");
				ObjectValueTreeValuesButtonText = Color.FromName ("#175fde");
				ObjectValueTreeValuesButtonBorder = Color.FromName ("#175fde");
				ObjectValueTreeValueDisabledText = Color.FromName ("#7f7f7f");
				ObjectValueTreeValueModifiedText = Color.FromName ("#1FAECE");

				ExceptionCaughtDialog.LineNumberTextColor = Color.FromName ("#707070");
				ExceptionCaughtDialog.ExternalCodeTextColor = Color.FromName ("#707070");
				ExceptionCaughtDialog.ValueTreeBackgroundColor = Color.FromName ("#ffffff");
			} else {
				ObjectValueTreeValuesButtonBackground = Color.FromName ("#555b65");
				ObjectValueTreeValuesButtonText = Color.FromName ("#ace2ff");
				ObjectValueTreeValuesButtonBorder = Color.FromName ("#ace2ff");
				ObjectValueTreeValueDisabledText = Color.FromName ("#5a5a5a");
				ObjectValueTreeValueModifiedText = Color.FromName ("#4FCAE6");

				ExceptionCaughtDialog.LineNumberTextColor = Color.FromName ("#b4b4b4");
				ExceptionCaughtDialog.ExternalCodeTextColor = Color.FromName ("#b4b4b4");
				ExceptionCaughtDialog.ValueTreeBackgroundColor = Color.FromName ("#525252");
			}

			// Shared

			ObjectValueTreeValueErrorText = Ide.Gui.Styles.WarningForegroundColor;

			PreviewVisualizerBackgroundColor = Ide.Gui.Styles.PopoverWindow.DefaultBackgroundColor;
			PreviewVisualizerTextColor = Ide.Gui.Styles.PopoverWindow.DefaultTextColor;
			PreviewVisualizerHeaderTextColor = Ide.Gui.Styles.PopoverWindow.DefaultTextColor;
		}
	}
}

