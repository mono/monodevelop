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
using MonoDevelop.Components;
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
			public Color LineNumberBorderColor { get; internal set; }
			public Color LineNumberTextColor { get; internal set; }
			public Color LineNumberTextShadowColor { get; internal set; }
		}

		static Styles ()
		{
			LoadStyles ();
			MonoDevelop.Ide.Gui.Styles.Changed +=  (o, e) => LoadStyles ();
		}

		public static void LoadStyles ()
		{
			ExceptionCaughtDialog = new ExceptionCaughtDialogStyle ();
			if (IdeApp.Preferences.UserInterfaceSkin == Skin.Light) {
				ObjectValueTreeValuesButtonBackground = Color.FromName ("#e9f2fc");
				ObjectValueTreeValuesButtonText = Color.FromName ("#5294eb");
				ObjectValueTreeValuesButtonBorder = Color.FromName ("#5294eb");
				ObjectValueTreeValueErrorText = Color.FromName ("#FA6B46");
				ObjectValueTreeValueDisabledText = Color.FromName ("#7f7f7f");
				ObjectValueTreeValueModifiedText = Color.FromName ("#85B7F3");

				PreviewVisualizerBackgroundColor = Color.FromName ("#f2f2f2");
				PreviewVisualizerHeaderTextColor = Color.FromName ("#242424");
				PreviewVisualizerTextColor = Color.FromName ("#555555");

				ExceptionCaughtDialog.TreeBackgroundColor = Color.FromName ("#ffffff");
				ExceptionCaughtDialog.InfoFrameBackgroundColor = Color.FromName ("#fbefce");
				ExceptionCaughtDialog.InfoFrameBorderColor = Color.FromName ("#f0e4c2");
				ExceptionCaughtDialog.LineNumberBackgroundColor = Color.FromName ("#c4c4c4");
				ExceptionCaughtDialog.LineNumberInUserCodeBackgroundColor = Color.FromName ("#e599de");
				ExceptionCaughtDialog.LineNumberBorderColor = Color.FromName ("#e599de");
				ExceptionCaughtDialog.LineNumberTextColor = Color.FromName ("#ffffff");
				ExceptionCaughtDialog.LineNumberTextShadowColor = ExceptionCaughtDialog.LineNumberInUserCodeBackgroundColor;
			} else {
				ObjectValueTreeValuesButtonBackground = Color.FromName ("#e9f2fc"); // TODO
				ObjectValueTreeValuesButtonText = Color.FromName ("#5294eb"); // TODO
				ObjectValueTreeValuesButtonBorder = Color.FromName ("#5294eb"); // TODO
				ObjectValueTreeValueErrorText = Color.FromName ("#ff0000"); // TODO
				ObjectValueTreeValueDisabledText = Color.FromName ("#7f7f7f"); // TODO
				ObjectValueTreeValueModifiedText = Color.FromName ("#0000ff"); // TODO

				PreviewVisualizerBackgroundColor = MonoDevelop.Ide.Gui.Styles.PopoverWindow.DefaultBackgroundColor; // TODO
				PreviewVisualizerHeaderTextColor = Color.FromName ("#dbdbdb"); // TODO
				PreviewVisualizerTextColor = MonoDevelop.Ide.Gui.Styles.PopoverWindow.DefaultTextColor; // TODO

				ExceptionCaughtDialog.TreeBackgroundColor = Color.FromName ("#5a5a5a"); // TODO
				ExceptionCaughtDialog.InfoFrameBackgroundColor = Color.FromName ("#5a5a5a"); // TODO
				ExceptionCaughtDialog.InfoFrameBorderColor = Color.FromName ("#9da2a6"); // TODO
				ExceptionCaughtDialog.LineNumberBackgroundColor = Color.FromName ("#c4c4c4"); // TODO
				ExceptionCaughtDialog.LineNumberInUserCodeBackgroundColor = Color.FromName ("#e599de"); // TODO
				ExceptionCaughtDialog.LineNumberBorderColor = Color.FromName ("#000000").WithAlpha (.11); // TODO
				ExceptionCaughtDialog.LineNumberTextColor = Color.FromName ("#ffffff"); // TODO
				ExceptionCaughtDialog.LineNumberTextShadowColor = Color.FromName ("#000000").WithAlpha (.34); // TODO
			}
		}
	}
}

