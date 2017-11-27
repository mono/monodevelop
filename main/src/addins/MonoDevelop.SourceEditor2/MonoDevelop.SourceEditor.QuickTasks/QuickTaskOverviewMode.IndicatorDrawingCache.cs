//
// QuickTaskOverviewMode.IndicatorDrawingCache.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2017 Microsoft
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
using System.Collections.Generic;
using MonoDevelop.Components;
using MonoDevelop.Ide.Editor.Extension;
using Microsoft.CodeAnalysis;
using MonoDevelop.Core.Text;

namespace MonoDevelop.SourceEditor.QuickTasks
{
	partial class QuickTaskOverviewMode
	{
		class IndicatorDrawingState : IEquatable<IndicatorDrawingState>
		{
			public int Width { get; set; }
			public int Height { get; set; }

			public List<ISegment> SearchResults { get; } = new List<ISegment> ();
			public List<Usage> Usages { get; } = new List<Usage> ();
			public List<QuickTask> Tasks { get; } = new List<QuickTask> ();

			public int MainSelection { get; set; } = -1;
			public List<int> SearchResultIndicators { get; set; } = new List<int> ();
			public List<(int, int)> UsageRectangles { get; set; } = new List<(int, int)> ();
			public List<(int, int)> TaskRectangles { get; set; } = new List<(int, int)> ();

			public DiagnosticSeverity Severity { get; set; } = DiagnosticSeverity.Hidden;

			public List<HashSet<int>> lineCache;

			public int taskIterator = 0;
			public int usageIterator = 0;

			public HslColor [] ColorCache = new HslColor [8];

			public const int UsageColor = 0;
			public const int FocusColor = 1;
			public const int ChangingUsagesColor = 2;

			public const int UsagesRectangleColor = 3;

			public const int UnderlineErrorColor = 4;
			public const int UnderlineWarningColor = 5;
			public const int UnderlineSuggestionColor = 6;
			public const int BackgroundColor = 7;

			IndicatorDrawingState ()
			{
				lineCache = new List<HashSet<int>> ();
				lineCache.Add (new HashSet<int> ());
				lineCache.Add (new HashSet<int> ());
			}

			public static int GetBarColor (DiagnosticSeverity severity)
			{
				switch (severity) {
				case DiagnosticSeverity.Error:
					return UnderlineErrorColor;
				case DiagnosticSeverity.Warning:
					return UnderlineWarningColor;
				case DiagnosticSeverity.Info:
					return UnderlineSuggestionColor;
				case DiagnosticSeverity.Hidden:
					return BackgroundColor;
				default:
					throw new ArgumentOutOfRangeException ();
				}
			}

			public bool Equals (IndicatorDrawingState other)
			{
				if (other == null)
					return false;
				if (Height != other.Height || Width != other.Width ||
				    Severity != other.Severity ||
				    MainSelection != other.MainSelection ||
				    SearchResultIndicators.Count != other.SearchResultIndicators.Count ||
				    UsageRectangles.Count != other.UsageRectangles.Count ||
				    TaskRectangles.Count != other.TaskRectangles.Count)
					return false;
				for (int i = 0; i < UsageRectangles.Count; i++) {
					if (UsageRectangles [i].Item1 != other.UsageRectangles [i].Item1 || UsageRectangles [i].Item2 != other.UsageRectangles [i].Item2)
						return false;
				}
				for (int i = 0; i < TaskRectangles.Count; i++) {
					if (TaskRectangles [i].Item1 != other.TaskRectangles [i].Item1 || TaskRectangles [i].Item2 != other.TaskRectangles [i].Item2)
						return false;
				}
				return true;
			}

			static Queue<IndicatorDrawingState> stateCache = new Queue<IndicatorDrawingState> ();

			internal static void Dispose (IndicatorDrawingState state)
			{
				state.Width = state.Height = 0;

				state.SearchResults.Clear ();
				state.Usages.Clear ();
				state.Tasks.Clear ();

				state.MainSelection = -1;
				state.SearchResultIndicators.Clear ();
				state.UsageRectangles.Clear ();
				state.TaskRectangles.Clear ();

				state.Severity = DiagnosticSeverity.Hidden;

				foreach (var lc in state.lineCache)
					lc.Clear ();

				stateCache.Enqueue (state);
			}

			internal static IndicatorDrawingState Create ()
			{
				if (stateCache.Count == 0)
					return new IndicatorDrawingState ();
				return stateCache.Dequeue ();
			}
		}
	}
}
