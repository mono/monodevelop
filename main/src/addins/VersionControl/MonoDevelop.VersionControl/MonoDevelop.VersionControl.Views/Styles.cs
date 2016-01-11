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
					AnnotationBackgroundColor = Color.FromName ("#f2f2f2"),
					AnnotationHighlightColor = Color.FromName ("#ffffff"),
					AnnotationTextColor = Color.FromName ("#000000"),
					AnnotationSummaryTextColor = Color.FromName ("#b2b2b2"),
					AnnotationSplitterColor = Color.FromName ("#999999"),
					AnnotationMarkColor = Color.FromName ("#e5e5ff"),
					AnnotationMarkModifiedColor = Color.FromName ("#ffff00"),
					RangeHazeColor = Color.FromName ("#ababab").WithAlpha (.1),
					RangeSplitterColor = Color.FromName ("#ababab").WithAlpha (.2),
				};

				LogView = new LogViewStyle {
					CommitDescBackgroundColor = Color.FromName ("#fffbf2"),
					DiffAddBackgroundColor = Color.FromName ("#7bc87b").AddLight (0.1),
					DiffRemoveBackgroundColor = Color.FromName ("#c88c8c").AddLight (0.1),
					DiffHighlightColor = Color.FromName ("#649dd6").AddLight (0.1),
					DiffBoxBorderColor = Color.FromName ("#b2b2b2"),
					DiffBoxSplitterColor = Color.FromName ("#e5e5e5"),
				};

				DiffView = new DiffViewStyle {
					AddBorderColor = Color.FromName ("#bef0be"),
					AddBackgroundColor = Color.FromName ("#85a885"),
					RemoveBorderColor = Color.FromName ("#ffffc8"),
					RemoveBackgroundColor = Color.FromName ("#b28c8c"),
					MergeBorderColor = Color.FromName ("#bebef0"),
					MergeBackgroundColor = Color.FromName ("#8585a8"),
				};

			} else {

				BlameView = new BlameViewStyle {
					AnnotationBackgroundColor = MonoDevelop.Ide.Gui.Styles.PadBackground,
					AnnotationHighlightColor = MonoDevelop.Ide.Gui.Styles.BackgroundColor,
					AnnotationTextColor = Color.FromName ("#ffffff"),
					AnnotationSummaryTextColor = MonoDevelop.Ide.Gui.Styles.BaseForegroundColor,
					AnnotationSplitterColor = MonoDevelop.Ide.Gui.Styles.ThinSplitterColor,
					AnnotationMarkColor = Color.FromName ("#e5e5ff"),
					AnnotationMarkModifiedColor = Color.FromName ("#ffff00"),
					RangeHazeColor = Color.FromName ("#3d3d3d").WithAlpha (.6),
					RangeSplitterColor = Color.FromName ("#3d3d3d").WithAlpha (.7),
				};

				LogView = new LogViewStyle () {
					CommitDescBackgroundColor = MonoDevelop.Ide.Gui.Styles.BaseBackgroundColor,
					DiffAddBackgroundColor = Color.FromName ("#7bc87b").AddLight (-0.1),
					DiffRemoveBackgroundColor = Color.FromName ("#c88c8c").AddLight (-0.1),
					DiffHighlightColor = MonoDevelop.Ide.Gui.Styles.BackgroundColor.AddLight (0.1),
					DiffBoxBorderColor = Color.FromName ("#4c4c4c"),
					DiffBoxSplitterColor = Color.FromName ("#7f7f7f"),
				};

				DiffView = new DiffViewStyle {
					AddBorderColor = Color.FromName ("#bef0be"),
					AddBackgroundColor = Color.FromName ("#85a885"),
					RemoveBorderColor = Color.FromName ("#ffffc8"),
					RemoveBackgroundColor = Color.FromName ("#b28c8c"),
					MergeBorderColor = Color.FromName ("#bebef0"),
					MergeBackgroundColor = Color.FromName ("#8585a8"),
				};
			}
		}
	}
}

