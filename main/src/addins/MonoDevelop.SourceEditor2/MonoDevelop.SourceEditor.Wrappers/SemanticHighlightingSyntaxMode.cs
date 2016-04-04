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

namespace MonoDevelop.SourceEditor.Wrappers
{
	sealed class SemanticHighlightingSyntaxMode : SyntaxMode, IDisposable
	{
		readonly ExtensibleTextEditor editor;
		readonly SyntaxMode syntaxMode;
		SemanticHighlighting semanticHighlighting;

		public override TextDocument Document {
			get {
				return syntaxMode.Document;
			}
			set {
				syntaxMode.Document = value;
			}
		}

		public Mono.TextEditor.Highlighting.SyntaxMode UnderlyingSyntaxMode {
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
		}

		class HighlightingSegmentTree : Mono.TextEditor.SegmentTree<StyledTreeSegment>
		{
			public bool GetStyle (Chunk chunk, ref int endOffset, out string style)
			{
				var segment = GetSegmentsAt (chunk.Offset).FirstOrDefault (s => s.EndOffset > chunk.Offset);
				if (segment == null) {
					style = null;
					return false;
				}
				endOffset = segment.EndOffset;
				style = segment.Style;
				return true;
			}

			public void AddStyle (MonoDevelop.Core.Text.ISegment segment, string style)
			{
				if (IsDirty)
					return;
				Add (new StyledTreeSegment (segment.Offset, segment.Length, style));
			}
		}

		bool isDisposed;
		Queue<Tuple<DocumentLine, HighlightingSegmentTree>> lineSegments = new Queue<Tuple<DocumentLine, HighlightingSegmentTree>> ();

		public SemanticHighlightingSyntaxMode (ExtensibleTextEditor editor, ISyntaxMode syntaxMode, SemanticHighlighting semanticHighlighting)
		{
			if (editor == null)
				throw new ArgumentNullException ("editor");
			if (syntaxMode == null)
				throw new ArgumentNullException ("syntaxMode");
			if (semanticHighlighting == null)
				throw new ArgumentNullException ("semanticHighlighting");
			this.editor = editor;
			this.semanticHighlighting = semanticHighlighting;
			this.syntaxMode = syntaxMode as SyntaxMode;
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
			isDisposed = true;
			UnregisterLineSegmentTrees ();
			lineSegments = null;
			semanticHighlighting.SemanticHighlightingUpdated -= SemanticHighlighting_SemanticHighlightingUpdated;
		}

		public override SpanParser CreateSpanParser (Mono.TextEditor.DocumentLine line, CloneableStack<Span> spanStack)
		{
			return syntaxMode.CreateSpanParser (line, spanStack);
		}

		public override ChunkParser CreateChunkParser (SpanParser spanParser, Mono.TextEditor.Highlighting.ColorScheme style, DocumentLine line)
		{
			return new CSharpChunkParser (this, spanParser, style, line);
		}

		class CSharpChunkParser : ChunkParser
		{
			const int MaximumCachedLineSegments = 200;
			SemanticHighlightingSyntaxMode semanticMode;

			public CSharpChunkParser (SemanticHighlightingSyntaxMode semanticMode, SpanParser spanParser, Mono.TextEditor.Highlighting.ColorScheme style, DocumentLine line) : base (semanticMode, spanParser, style, line)
			{
				this.semanticMode = semanticMode;
			}

			protected override void AddRealChunk (Chunk chunk)
			{
				if (!DefaultSourceEditorOptions.Instance.EnableSemanticHighlighting) {
					base.AddRealChunk (chunk);
					return;
				}
				StyledTreeSegment treeseg = null;

				try {
					var tree = semanticMode.lineSegments.FirstOrDefault (t => t.Item1 == line);
					if (tree == null) {
						tree = Tuple.Create (line, new HighlightingSegmentTree ());
						tree.Item2.InstallListener (semanticMode.Document); 
						int lineOffset = line.Offset;
						foreach (var seg in semanticMode.semanticHighlighting.GetColoredSegments (new MonoDevelop.Core.Text.TextSegment (lineOffset, line.Length))) {
							tree.Item2.AddStyle (seg, seg.ColorStyleKey);
						}
						while (semanticMode.lineSegments.Count > MaximumCachedLineSegments) {
							var removed = semanticMode.lineSegments.Dequeue ();
							try {
								removed.Item2.RemoveListener ();
							} catch (Exception) { }
						}
						semanticMode.lineSegments.Enqueue (tree);
					}
					treeseg = tree.Item2.GetSegmentsOverlapping (chunk).FirstOrDefault (s => s.Offset < chunk.EndOffset && s.EndOffset > chunk.Offset);
				} catch (Exception e) {
					Console.WriteLine ("Error in semantic highlighting: " + e);
				}

				if (treeseg != null) {
					if (treeseg.Offset - chunk.Offset > 0)
						AddRealChunk (new Chunk (chunk.Offset, treeseg.Offset - chunk.Offset, chunk.Style));

					var startOffset = Math.Max (chunk.Offset, treeseg.Offset);
					var endOffset = Math.Min (treeseg.EndOffset, chunk.EndOffset);

					base.AddRealChunk (new Chunk (startOffset, endOffset - startOffset, treeseg.Style));

					if (endOffset < chunk.EndOffset)
						AddRealChunk (new Chunk (treeseg.EndOffset, chunk.EndOffset - endOffset, chunk.Style));
					return;
				}

				base.AddRealChunk (chunk);
			}

			protected override string GetStyle (Chunk chunk)
			{
				/*if (spanParser.CurRule.Name == "Comment") {
					if (tags.Contains (doc.GetTextAt (chunk))) 
						return "Comment Tag";
				}*/
				return base.GetStyle (chunk);
			}
		}
	}
}