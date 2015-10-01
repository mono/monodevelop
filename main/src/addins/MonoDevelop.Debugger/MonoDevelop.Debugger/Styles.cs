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

namespace MonoDevelop.Debugger
{
	public static class Styles
	{
		public static Cairo.Color ObjectValueTreeValuesButtonBackground { get; internal set; }
		public static Cairo.Color ObjectValueTreeValuesButtonText { get; internal set; }
		public static Cairo.Color ObjectValueTreeValuesButtonBorder { get; internal set; }
		public static Cairo.Color PreviewVisualizerBackgroundColor { get; internal set; }
		public static Gdk.Color PreviewVisualizerTextColor { get; internal set; }
		public static Gdk.Color PreviewVisualizerHeaderTextColor { get; internal set; }

		public static ExceptionCaughtDialogStyle ExceptionCaughtDialog { get; internal set; }

		public class ExceptionCaughtDialogStyle
		{
			public Gdk.Color TreeBackgroundColor { get; internal set; }
			public Cairo.Color InfoFrameBackgroundColor { get; internal set; }
			public Cairo.Color InfoFrameBorderColor { get; internal set; }
			public Cairo.Color LineNumberBackgroundColor { get; internal set; }
			public Cairo.Color LineNumberInUserCodeBackgroundColor { get; internal set; }
			public Cairo.Color LineNumberBorderColor { get; internal set; }
			public Cairo.Color LineNumberTextColor { get; internal set; }
			public Cairo.Color LineNumberTextShadowColor { get; internal set; }
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
				ObjectValueTreeValuesButtonBackground = new Cairo.Color (233 / 255.0, 242 / 255.0, 252 / 255.0);
				ObjectValueTreeValuesButtonText = new Cairo.Color (82 / 255.0, 148 / 255.0, 235 / 255.0);
				ObjectValueTreeValuesButtonBorder = new Cairo.Color (82 / 255.0, 148 / 255.0, 235 / 255.0);
				PreviewVisualizerBackgroundColor = new Cairo.Color (245 / 256.0, 245 / 256.0, 245 / 256.0);
				PreviewVisualizerHeaderTextColor = new Gdk.Color (36, 36, 36);
				PreviewVisualizerTextColor = new Gdk.Color (85, 85, 85);

				ExceptionCaughtDialog.TreeBackgroundColor = new Gdk.Color (223, 228, 235);
				ExceptionCaughtDialog.InfoFrameBackgroundColor = new Cairo.Color (1.00, 0.98, 0.91);
				ExceptionCaughtDialog.InfoFrameBorderColor = new Cairo.Color (0.87, 0.83, 0.74);
				ExceptionCaughtDialog.LineNumberBackgroundColor = new Cairo.Color (0.77, 0.77, 0.77, 1.0);
				ExceptionCaughtDialog.LineNumberInUserCodeBackgroundColor = new Cairo.Color (0.90, 0.60, 0.87, 1.0);
				ExceptionCaughtDialog.LineNumberBorderColor = new Cairo.Color (0.0, 0.0, 0.0, 0.11);
				ExceptionCaughtDialog.LineNumberTextColor = new Cairo.Color (1.0, 1.0, 1.0, 1.0);
				ExceptionCaughtDialog.LineNumberTextShadowColor = new Cairo.Color (0.0, 0.0, 0.0, 0.34);
			} else {
				ObjectValueTreeValuesButtonBackground = new Cairo.Color (233 / 255.0, 242 / 255.0, 252 / 255.0);
				ObjectValueTreeValuesButtonText = new Cairo.Color (82 / 255.0, 148 / 255.0, 235 / 255.0);
				ObjectValueTreeValuesButtonBorder = new Cairo.Color (82 / 255.0, 148 / 255.0, 235 / 255.0);
				PreviewVisualizerBackgroundColor = MonoDevelop.Ide.Gui.Styles.PopoverWindow.DefaultBackgroundColor;
				PreviewVisualizerHeaderTextColor = new Gdk.Color (219, 219, 219);
				PreviewVisualizerTextColor = MonoDevelop.Ide.Gui.Styles.PopoverWindow.DefaultTextColor.ToGdkColor();

				ExceptionCaughtDialog.TreeBackgroundColor = new Gdk.Color (90, 90, 90);
				ExceptionCaughtDialog.InfoFrameBackgroundColor = new Cairo.Color (90d/255d, 90d/255d, 90d/255d);
				ExceptionCaughtDialog.InfoFrameBorderColor = new Cairo.Color (157d/255d, 162d/255d, 166d/255d);
				ExceptionCaughtDialog.LineNumberBackgroundColor = new Cairo.Color (0.77, 0.77, 0.77, 1.0);
				ExceptionCaughtDialog.LineNumberInUserCodeBackgroundColor = new Cairo.Color (0.90, 0.60, 0.87, 1.0);
				ExceptionCaughtDialog.LineNumberBorderColor = new Cairo.Color (0.0, 0.0, 0.0, 0.11);
				ExceptionCaughtDialog.LineNumberTextColor = new Cairo.Color (1.0, 1.0, 1.0, 1.0);
				ExceptionCaughtDialog.LineNumberTextShadowColor = new Cairo.Color (0.0, 0.0, 0.0, 0.34);
			}
		}
	}
}

