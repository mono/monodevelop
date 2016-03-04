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
			public Color TreeBackgroundColor { get; internal set; }
			public Color InfoFrameBackgroundColor { get; internal set; }
			public Color InfoFrameBorderColor { get; internal set; }
			public Color LineNumberBackgroundColor { get; internal set; }
			public Color LineNumberInUserCodeBackgroundColor { get; internal set; }
			public Color LineNumberTextColor { get; internal set; }
		}

		static Styles ()
		{
			LoadStyles ();
			Ide.Gui.Styles.Changed +=  (o, e) => LoadStyles ();
		}

		public static void LoadStyles ()
		{
			ExceptionCaughtDialog = new ExceptionCaughtDialogStyle ();

			if (IdeApp.Preferences.UserInterfaceSkin == Skin.Light) {
				ObjectValueTreeValuesButtonBackground = Color.FromName ("#e9f2fc");
				ObjectValueTreeValuesButtonText = Color.FromName ("#5294eb");
				ObjectValueTreeValuesButtonBorder = Color.FromName ("#5294eb");
				ObjectValueTreeValueDisabledText = Color.FromName ("#7f7f7f");
				ObjectValueTreeValueModifiedText = Color.FromName ("#1FAECE");

				ExceptionCaughtDialog.InfoFrameBackgroundColor = Color.FromName ("#fbefce");
				ExceptionCaughtDialog.InfoFrameBorderColor = Color.FromName ("#f0e4c2");
				ExceptionCaughtDialog.LineNumberBackgroundColor = Color.FromName ("#c4c4c4");
				ExceptionCaughtDialog.LineNumberInUserCodeBackgroundColor = Color.FromName ("#e599de");
				ExceptionCaughtDialog.LineNumberTextColor = Color.FromName ("#ffffff");
			} else {
				ObjectValueTreeValuesButtonBackground = Color.FromName ("#7c8695");
				ObjectValueTreeValuesButtonText = Color.FromName ("#cbe5ff");
				ObjectValueTreeValuesButtonBorder = Color.FromName ("#a4bbd5");
				ObjectValueTreeValueDisabledText = Color.FromName ("#5a5a5a");
				ObjectValueTreeValueModifiedText = Color.FromName ("#4FCAE6");

				ExceptionCaughtDialog.InfoFrameBackgroundColor = Color.FromName ("#675831");
				ExceptionCaughtDialog.InfoFrameBorderColor = Color.FromName ("#7a6a3d");
				ExceptionCaughtDialog.LineNumberBackgroundColor = Color.FromName ("#c4c4c4");
				ExceptionCaughtDialog.LineNumberInUserCodeBackgroundColor = Color.FromName ("#e599de");
				ExceptionCaughtDialog.LineNumberTextColor = Color.FromName ("#222222");
			}

			// Shared

			ObjectValueTreeValueErrorText = Ide.Gui.Styles.WarningForegroundColor;

			PreviewVisualizerBackgroundColor = Ide.Gui.Styles.PopoverWindow.DefaultBackgroundColor;
			PreviewVisualizerTextColor = Ide.Gui.Styles.PopoverWindow.DefaultTextColor;
			PreviewVisualizerHeaderTextColor = Ide.Gui.Styles.PopoverWindow.DefaultTextColor;

			ExceptionCaughtDialog.TreeBackgroundColor = Ide.Gui.Styles.PrimaryBackgroundColor;
		}
	}
}

