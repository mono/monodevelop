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
using Xwt.Drawing;

namespace MonoDevelop.VersionControl
{
	public static class Styles
	{
		public static BlameViewStyle BlameView { get; internal set; }
		public static LogViewStyle LogView { get; internal set; }
		public static DiffViewStyle DiffView { get; internal set; }

		public class BlameViewStyle
		{
			public Color AnnotationBackgroundColor { get; internal set; }
			public Color AnnotationHighlightColor { get; internal set; }
			public Color AnnotationTextColor { get; internal set; }
			public Color AnnotationSummaryTextColor { get; internal set; }
			public Color AnnotationSplitterColor { get; internal set; }
			public Color AnnotationMarkColor { get; internal set; }
			public Color AnnotationMarkModifiedColor { get; internal set; }
			public Color RangeSplitterColor { get; internal set; }
			public Color RangeHazeColor { get; internal set; }
		}

		public class LogViewStyle
		{
			public Color CommitDescBackgroundColor { get; internal set; }
			public Color DiffAddBackgroundColor { get; internal set; }
			public Color DiffRemoveBackgroundColor { get; internal set; }
			public Color DiffHighlightColor { get; internal set; }
			public Color DiffBoxBorderColor { get; internal set; }
			public Color DiffBoxSplitterColor { get; internal set; }
		}

		public class DiffViewStyle
		{
			public Color AddBorderColor { get; internal set; }
			public Color AddBackgroundColor { get; internal set; }
			public Color RemoveBorderColor { get; internal set; }
			public Color RemoveBackgroundColor { get; internal set; }
			public Color MergeBorderColor { get; internal set; }
			public Color MergeBackgroundColor { get; internal set; }
		}

		static Styles ()
		{
			LoadStyles ();
			MonoDevelop.Ide.Gui.Styles.Changed +=  (o, e) => LoadStyles ();
		}

		public static void LoadStyles ()
		{
			if (IdeApp.Preferences.UserInterfaceSkin == Skin.Light) {
				
				BlameView = new BlameViewStyle {
					AnnotationBackgroundColor = new Color (0.95, 0.95, 0.95),
					AnnotationHighlightColor = new Color (1, 1, 1),
					AnnotationTextColor = new Color (0, 0, 0),
					AnnotationSummaryTextColor = new Color (0.7, 0.7, 0.7),
					AnnotationSplitterColor = new Color (0.6, 0.6, 0.6),
					AnnotationMarkColor = new Color (0.90, 0.90, 1),
					AnnotationMarkModifiedColor = new Color (1, 1, 0),
					RangeHazeColor = new Color (171d/255d, 171d/255d, 171d/255d, 0.1),
					RangeSplitterColor = new Color (171d/255d, 171d/255d, 171d/255d, 0.2),
				};

				LogView = new LogViewStyle {
					CommitDescBackgroundColor = new Color (255d/255d, 251d/255d, 242d/255d),
					DiffAddBackgroundColor = new Color (123d/255d, 200d/255d, 123d/255d).AddLight (0.1),
					DiffRemoveBackgroundColor = new Color (200d/255d, 140d/255d, 140d/255d).AddLight (0.1),
					DiffHighlightColor = new Color (100d/255d, 157d/255d, 214d/255d).AddLight (0.1),
					DiffBoxBorderColor = new Color (0.7, 0.7, 0.7),
					DiffBoxSplitterColor = new Color (0.9, 0.9, 0.9),
				};

				DiffView = new DiffViewStyle {
					AddBorderColor = new Color (190 / 255.0, 240 / 255.0, 190 / 255.0),
					AddBackgroundColor = new Color (133 / 255.0, 168 / 255.0, 133 / 255.0),
					RemoveBorderColor = new Color (255 / 255.0, 200 / 255.0, 200 / 255.0),
					RemoveBackgroundColor = new Color (178 / 255.0, 140 / 255.0, 140 / 255.0),
					MergeBorderColor = new Color (190 / 255.0, 190 / 255.0, 240 / 255.0),
					MergeBackgroundColor = new Color (133 / 255.0, 133 / 255.0, 168 / 255.0),
				};

			} else {

				BlameView = new BlameViewStyle {
					AnnotationBackgroundColor = MonoDevelop.Ide.Gui.Styles.PadBackground,
					AnnotationHighlightColor = MonoDevelop.Ide.Gui.Styles.BackgroundColor,
					AnnotationTextColor = new Color (1, 1, 1),
					AnnotationSummaryTextColor = MonoDevelop.Ide.Gui.Styles.BaseForegroundColor,
					AnnotationSplitterColor = MonoDevelop.Ide.Gui.Styles.ThinSplitterColor,
					AnnotationMarkColor = new Color (0.90, 0.90, 1),
					AnnotationMarkModifiedColor = new Color (1, 1, 0),
					RangeHazeColor = new Color (0.24, 0.24, 0.24, 0.6),
					RangeSplitterColor = new Color (0.24, 0.24, 0.24, 0.7),
				};

				LogView = new LogViewStyle () {
					CommitDescBackgroundColor = MonoDevelop.Ide.Gui.Styles.BaseBackgroundColor,
					DiffAddBackgroundColor = new Color (123d/255d, 200d/255d, 123d/255d).AddLight (-0.1),
					DiffRemoveBackgroundColor = new Color (200d/255d, 140d/255d, 140d/255d).AddLight (-0.1),
					DiffHighlightColor = MonoDevelop.Ide.Gui.Styles.BackgroundColor.AddLight (0.1),
					DiffBoxBorderColor = new Color (0.3, 0.3, 0.3),
					DiffBoxSplitterColor = new Color (0.5, 0.5, 0.5),
				};

				DiffView = new DiffViewStyle {
					AddBorderColor = new Color (190 / 255.0, 240 / 255.0, 190 / 255.0),
					AddBackgroundColor = new Color (133 / 255.0, 168 / 255.0, 133 / 255.0),
					RemoveBorderColor = new Color (255 / 255.0, 200 / 255.0, 200 / 255.0),
					RemoveBackgroundColor = new Color (178 / 255.0, 140 / 255.0, 140 / 255.0),
					MergeBorderColor = new Color (190 / 255.0, 190 / 255.0, 240 / 255.0),
					MergeBackgroundColor = new Color (133 / 255.0, 133 / 255.0, 168 / 255.0),
				};
			}
		}
	}
}

