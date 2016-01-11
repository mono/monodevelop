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
				ObjectValueTreeValuesButtonBackground = new Color (233 / 255.0, 242 / 255.0, 252 / 255.0);
				ObjectValueTreeValuesButtonText = new Color (82 / 255.0, 148 / 255.0, 235 / 255.0);
				ObjectValueTreeValuesButtonBorder = new Color (82 / 255.0, 148 / 255.0, 235 / 255.0);
				ObjectValueTreeValueErrorText = new Color (1.0, 0.0, 0.0);
				ObjectValueTreeValueDisabledText = new Color (0.5, 0.5, 0.5);
				ObjectValueTreeValueModifiedText = new Color (0.0, 0.0, 1.0);

				PreviewVisualizerBackgroundColor = new Color (245 / 256.0, 245 / 256.0, 245 / 256.0);
				PreviewVisualizerHeaderTextColor = new Color (36 / 255.0, 36 / 255.0, 36 / 255.0);
				PreviewVisualizerTextColor = new Color (85 / 255.0, 85 / 255.0, 85 / 255.0);

				ExceptionCaughtDialog.TreeBackgroundColor = new Color (223 / 255.0, 228 / 255.0, 235 / 255.0);
				ExceptionCaughtDialog.InfoFrameBackgroundColor = new Color (1.00, 0.98, 0.91);
				ExceptionCaughtDialog.InfoFrameBorderColor = new Color (0.87, 0.83, 0.74);
				ExceptionCaughtDialog.LineNumberBackgroundColor = new Color (0.77, 0.77, 0.77, 1.0);
				ExceptionCaughtDialog.LineNumberInUserCodeBackgroundColor = new Color (0.90, 0.60, 0.87, 1.0);
				ExceptionCaughtDialog.LineNumberBorderColor = new Color (0.0, 0.0, 0.0, 0.11);
				ExceptionCaughtDialog.LineNumberTextColor = new Color (1.0, 1.0, 1.0, 1.0);
				ExceptionCaughtDialog.LineNumberTextShadowColor = new Color (0.0, 0.0, 0.0, 0.34);
			} else {
				ObjectValueTreeValuesButtonBackground = new Color (233 / 255.0, 242 / 255.0, 252 / 255.0);
				ObjectValueTreeValuesButtonText = new Color (82 / 255.0, 148 / 255.0, 235 / 255.0);
				ObjectValueTreeValuesButtonBorder = new Color (82 / 255.0, 148 / 255.0, 235 / 255.0);
				ObjectValueTreeValueErrorText = new Color (1.0, 0.0, 0.0);
				ObjectValueTreeValueDisabledText = new Color (0.5, 0.5, 0.5);
				ObjectValueTreeValueModifiedText = new Color (0.0, 0.0, 1.0);

				PreviewVisualizerBackgroundColor = MonoDevelop.Ide.Gui.Styles.PopoverWindow.DefaultBackgroundColor;
				PreviewVisualizerHeaderTextColor = new Color (219 / 255.0, 219 / 255.0, 219 / 255.0);
				PreviewVisualizerTextColor = MonoDevelop.Ide.Gui.Styles.PopoverWindow.DefaultTextColor;

				ExceptionCaughtDialog.TreeBackgroundColor = new Color (90 / 255.0, 90 / 255.0, 90 / 255.0);
				ExceptionCaughtDialog.InfoFrameBackgroundColor = new Color (90d/255d, 90d/255d, 90d/255d);
				ExceptionCaughtDialog.InfoFrameBorderColor = new Color (157d/255d, 162d/255d, 166d/255d);
				ExceptionCaughtDialog.LineNumberBackgroundColor = new Color (0.77, 0.77, 0.77, 1.0);
				ExceptionCaughtDialog.LineNumberInUserCodeBackgroundColor = new Color (0.90, 0.60, 0.87, 1.0);
				ExceptionCaughtDialog.LineNumberBorderColor = new Color (0.0, 0.0, 0.0, 0.11);
				ExceptionCaughtDialog.LineNumberTextColor = new Color (1.0, 1.0, 1.0, 1.0);
				ExceptionCaughtDialog.LineNumberTextShadowColor = new Color (0.0, 0.0, 0.0, 0.34);
			}
		}
	}
}

