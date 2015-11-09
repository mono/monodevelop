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

namespace MonoDevelop.VersionControl
{
	public static class Styles
	{
		public static BlameViewStyle BlameView { get; internal set; }
		public static LogViewStyle LogView { get; internal set; }
		public static DiffViewStyle DiffView { get; internal set; }

		public class BlameViewStyle
		{
			public Cairo.Color AnnotationBackgroundColor { get; internal set; }
			public Cairo.Color AnnotationHighlightColor { get; internal set; }
			public Cairo.Color AnnotationTextColor { get; internal set; }
			public Cairo.Color AnnotationSummaryTextColor { get; internal set; }
			public Cairo.Color AnnotationSplitterColor { get; internal set; }
			public Cairo.Color AnnotationMarkColor { get; internal set; }
			public Cairo.Color AnnotationMarkModifiedColor { get; internal set; }
			public Cairo.Color RangeSplitterColor { get; internal set; }
			public Cairo.Color RangeHazeColor { get; internal set; }
		}

		public class LogViewStyle
		{
			public Gdk.Color CommitDescBackgroundColor { get; internal set; }
			public Gdk.Color DiffAddBackgroundColor { get; internal set; }
			public Gdk.Color DiffRemoveBackgroundColor { get; internal set; }
			public Gdk.Color DiffHighlightColor { get; internal set; }
			public Cairo.Color DiffBoxBorderColor { get; internal set; }
			public Cairo.Color DiffBoxSplitterColor { get; internal set; }
		}

		public class DiffViewStyle
		{
			public Cairo.Color AddBorderColor { get; internal set; }
			public Cairo.Color AddBackgroundColor { get; internal set; }
			public Cairo.Color RemoveBorderColor { get; internal set; }
			public Cairo.Color RemoveBackgroundColor { get; internal set; }
			public Cairo.Color MergeBorderColor { get; internal set; }
			public Cairo.Color MergeBackgroundColor { get; internal set; }
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
					AnnotationBackgroundColor = new Cairo.Color (0.95, 0.95, 0.95),
					AnnotationHighlightColor = new Cairo.Color (1, 1, 1),
					AnnotationTextColor = new Cairo.Color (0, 0, 0),
					AnnotationSummaryTextColor = new Cairo.Color (0.7, 0.7, 0.7),
					AnnotationSplitterColor = new Cairo.Color (0.6, 0.6, 0.6),
					AnnotationMarkColor = new Cairo.Color (0.90, 0.90, 1),
					AnnotationMarkModifiedColor = new Cairo.Color (1, 1, 0),
					RangeHazeColor = new Cairo.Color (171d/255d, 171d/255d, 171d/255d, 0.1),
					RangeSplitterColor = new Cairo.Color (171d/255d, 171d/255d, 171d/255d, 0.2),
				};

				LogView = new LogViewStyle {
					CommitDescBackgroundColor = new Gdk.Color (255, 251, 242),
					DiffAddBackgroundColor = new Gdk.Color (123, 200, 123).AddLight (0.1),
					DiffRemoveBackgroundColor = new Gdk.Color (200, 140, 140).AddLight (0.1),
					DiffHighlightColor = new Gdk.Color (100, 157, 214).AddLight (0.1),
					DiffBoxBorderColor = new Cairo.Color (0.7, 0.7, 0.7),
					DiffBoxSplitterColor = new Cairo.Color (0.9, 0.9, 0.9),
				};

				DiffView = new DiffViewStyle {
					AddBorderColor = new Cairo.Color (190 / 255.0, 240 / 255.0, 190 / 255.0),
					AddBackgroundColor = new Cairo.Color (133 / 255.0, 168 / 255.0, 133 / 255.0),
					RemoveBorderColor = new Cairo.Color (255 / 255.0, 200 / 255.0, 200 / 255.0),
					RemoveBackgroundColor = new Cairo.Color (178 / 255.0, 140 / 255.0, 140 / 255.0),
					MergeBorderColor = new Cairo.Color (190 / 255.0, 190 / 255.0, 240 / 255.0),
					MergeBackgroundColor = new Cairo.Color (133 / 255.0, 133 / 255.0, 168 / 255.0),
				};

			} else {

				BlameView = new BlameViewStyle {
					AnnotationBackgroundColor = MonoDevelop.Ide.Gui.Styles.PadBackground.ToCairoColor (),
					AnnotationHighlightColor = MonoDevelop.Ide.Gui.Styles.BackgroundColor,
					AnnotationTextColor = new Cairo.Color (1, 1, 1),
					AnnotationSummaryTextColor = MonoDevelop.Ide.Gui.Styles.BaseForegroundColor,
					AnnotationSplitterColor = MonoDevelop.Ide.Gui.Styles.ThinSplitterColor.ToCairoColor (),
					AnnotationMarkColor = new Cairo.Color (0.90, 0.90, 1),
					AnnotationMarkModifiedColor = new Cairo.Color (1, 1, 0),
					RangeHazeColor = new Cairo.Color (0.24, 0.24, 0.24, 0.6),
					RangeSplitterColor = new Cairo.Color (0.24, 0.24, 0.24, 0.7),
				};

				LogView = new LogViewStyle () {
					CommitDescBackgroundColor = MonoDevelop.Ide.Gui.Styles.BaseBackgroundColor.ToGdkColor (),
					DiffAddBackgroundColor = new Gdk.Color (123, 200, 123).AddLight (-0.1),
					DiffRemoveBackgroundColor = new Gdk.Color (200, 140, 140).AddLight (-0.1),
					DiffHighlightColor = MonoDevelop.Ide.Gui.Styles.BackgroundColor.ToGdkColor ().AddLight (0.1),
					DiffBoxBorderColor = new Cairo.Color (0.3, 0.3, 0.3),
					DiffBoxSplitterColor = new Cairo.Color (0.5, 0.5, 0.5),
				};

				DiffView = new DiffViewStyle {
					AddBorderColor = new Cairo.Color (190 / 255.0, 240 / 255.0, 190 / 255.0),
					AddBackgroundColor = new Cairo.Color (133 / 255.0, 168 / 255.0, 133 / 255.0),
					RemoveBorderColor = new Cairo.Color (255 / 255.0, 200 / 255.0, 200 / 255.0),
					RemoveBackgroundColor = new Cairo.Color (178 / 255.0, 140 / 255.0, 140 / 255.0),
					MergeBorderColor = new Cairo.Color (190 / 255.0, 190 / 255.0, 240 / 255.0),
					MergeBackgroundColor = new Cairo.Color (133 / 255.0, 133 / 255.0, 168 / 255.0),
				};
			}
		}
	}
}

