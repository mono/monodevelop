//
// SemanticHighlightingSyntaxMode.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using Mono.TextEditor.Highlighting;
using MonoDevelop.Ide.Editor.Highlighting;
using Mono.TextEditor;
using System.Collections.Generic;
using MonoDevelop.Ide.Editor;
using System.Linq;
using Gtk;
using System.Collections.Immutable;
using System.Threading.Tasks;
using System.Threading;
using MonoDevelop.Core.Text;
using MonoDevelop.Core;

namespace MonoDevelop.SourceEditor.Wrappers
{
	sealed class SemanticHighlightingSyntaxMode : ISyntaxHighlighting
	{
		readonly ExtensibleTextEditor editor;
		readonly ISyntaxHighlighting syntaxMode;
		SemanticHighlighting semanticHighlighting;

		public ISyntaxHighlighting UnderlyingSyntaxMode {
			get {
				return this.syntaxMode;
			}
		}

		internal class StyledTreeSegment : Mono.TextEditor.TreeSegment
		{
			public string Style {
				get;
				private set;
			}

			public StyledTreeSegment (int offset, int length, string style) : base (offset, length)
			{
				Style = style;
			}

			public override string ToString ()
			{
				return string.Format ($"[StyledTreeSegment: Offset={Offset}, Length={Length}, Style={Style}]");
			}
		}

		class HighlightingSegmentTree : Mono.TextEditor.SegmentTree<StyledTreeSegment>
		{

			public void AddStyle (MonoDevelop.Core.Text.ISegment segment, string style)
			{
				if (IsDirty)
					return;
				Add (new StyledTreeSegment (segment.Offset, segment.Length, style));
			}
		}

		bool isDisposed;
		Queue<Tuple<IDocumentLine, HighlightingSegmentTree>> lineSegments = new Queue<Tuple<IDocumentLine, HighlightingSegmentTree>> ();

		public SemanticHighlightingSyntaxMode (ExtensibleTextEditor editor, ISyntaxHighlighting syntaxMode, SemanticHighlighting semanticHighlighting)
		{
			if (editor == null)
				throw new ArgumentNullException ("editor");
			if (syntaxMode == null)
				throw new ArgumentNullException ("syntaxMode");
			if (semanticHighlighting == null)
				throw new ArgumentNullException ("semanticHighlighting");
			this.editor = editor;
			this.semanticHighlighting = semanticHighlighting;
			this.syntaxMode = syntaxMode;
			semanticHighlighting.SemanticHighlightingUpdated += SemanticHighlighting_SemanticHighlightingUpdated;
		}

		public void UpdateSemanticHighlighting (SemanticHighlighting newHighlighting)
		{
			if (isDisposed)
				return;
			if (semanticHighlighting !=null)
				semanticHighlighting.SemanticHighlightingUpdated -= SemanticHighlighting_SemanticHighlightingUpdated;
			semanticHighlighting = newHighlighting;
			if (semanticHighlighting !=null)
				semanticHighlighting.SemanticHighlightingUpdated += SemanticHighlighting_SemanticHighlightingUpdated;
		}

		void SemanticHighlighting_SemanticHighlightingUpdated (object sender, EventArgs e)
		{
			Application.Invoke (delegate {
				if (isDisposed)
					return;
				UnregisterLineSegmentTrees ();
				lineSegments.Clear ();

				var margin = editor.TextViewMargin;
				if (margin == null)
					return;
				margin.PurgeLayoutCache ();
				editor.QueueDraw ();
			});
		}

		void UnregisterLineSegmentTrees ()
		{
			if (isDisposed)
				return;
			foreach (var kv in lineSegments) {
				try {
					kv.Item2.RemoveListener ();
				} catch (Exception) {
				}
			}
		}

		public void Dispose()
		{
			if (isDisposed)
				return;
			// Unregister before setting isDisposed=true, as that causes the method to bail out early.
			UnregisterLineSegmentTrees ();
			isDisposed = true;
			lineSegments = null;
			semanticHighlighting.SemanticHighlightingUpdated -= SemanticHighlighting_SemanticHighlightingUpdated;
		}

		const int MaximumCachedLineSegments = 200;

		async Task<HighlightedLine> ISyntaxHighlighting.GetHighlightedLineAsync (IDocumentLine line, CancellationToken cancellationToken)
		{
			if (line == null)
				throw new ArgumentNullException (nameof (line));
			if (!DefaultSourceEditorOptions.Instance.EnableSemanticHighlighting) {
				return await syntaxMode.GetHighlightedLineAsync (line, cancellationToken);
			}
			var syntaxLine = await syntaxMode.GetHighlightedLineAsync (line, cancellationToken).ConfigureAwait (false);
			if (syntaxLine.Segments.Count == 0)
				return syntaxLine;
			lock (lineSegments) {
				var segments = new List<ColoredSegment> (syntaxLine.Segments);
				int endOffset = segments [segments.Count - 1].EndOffset;
				try {
					Tuple<IDocumentLine, HighlightingSegmentTree> tree = null;

					// This code should not have any lambda capture linq, as it is a hot loop.
					foreach (var segment in lineSegments) {
						if (segment.Item1 == line) {
							tree = segment;
							break;
						}
					}
					int lineOffset = line.Offset;
					if (tree == null) {
						tree = Tuple.Create (line, new HighlightingSegmentTree ());
						tree.Item2.InstallListener (editor.Document);
						foreach (var seg2 in semanticHighlighting.GetColoredSegments (new TextSegment (lineOffset, line.Length))) {
							tree.Item2.AddStyle (seg2, seg2.ColorStyleKey);
						}
						while (lineSegments.Count > MaximumCachedLineSegments) {
							var removed = lineSegments.Dequeue ();
							try {
								removed.Item2.RemoveListener ();
							} catch (Exception) { }
						}
						lineSegments.Enqueue (tree);
					}
					foreach (var treeseg in tree.Item2.GetSegmentsOverlapping (line)) {
						var inLineStartOffset = Math.Max (0, treeseg.Offset - lineOffset);
						var inLineEndOffset = Math.Min (line.Length, treeseg.EndOffset - lineOffset);
						if (inLineEndOffset <= inLineStartOffset)
							continue;
						var semanticSegment = new ColoredSegment (inLineStartOffset, inLineEndOffset - inLineStartOffset, syntaxLine.Segments [0].ScopeStack.Push (treeseg.Style));
						SyntaxHighlighting.ReplaceSegment (segments, semanticSegment);
					}
				} catch (Exception e) {
					LoggingService.LogError ("Error in semantic highlighting: " + e);
					return syntaxLine;
				}
				return new HighlightedLine (line, segments);
			}
		}

		async Task<ScopeStack> ISyntaxHighlighting.GetScopeStackAsync (int offset, CancellationToken cancellationToken)
		{
			var line = editor.GetLineByOffset (offset);
			if (line == null)
				throw new ArgumentOutOfRangeException (nameof (offset), "Offset out of range.");
			foreach (var seg in (await ((ISyntaxHighlighting)this).GetHighlightedLineAsync (line, cancellationToken).ConfigureAwait (false)).Segments) {
				if (seg.Contains (offset))
					return seg.ScopeStack;
			}
			return await syntaxMode.GetScopeStackAsync (offset, cancellationToken).ConfigureAwait (false);
		}

		public event EventHandler<Ide.Editor.LineEventArgs> HighlightingStateChanged {
			add {
				syntaxMode.HighlightingStateChanged += value;
			}
			remove {
				syntaxMode.HighlightingStateChanged -= value;
			}
		}
	}
}
