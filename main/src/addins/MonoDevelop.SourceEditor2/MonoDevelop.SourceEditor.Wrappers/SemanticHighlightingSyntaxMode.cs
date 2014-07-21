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

namespace MonoDevelop.SourceEditor.Wrappers
{
	sealed class SemanticHighlightingSyntaxMode : SyntaxMode
	{
		readonly ExtensibleTextEditor editor;
		readonly SyntaxMode syntaxMode;
		readonly SemanticHighlighting semanticHighlighting;

		public override TextDocument Document {
			get {
				return syntaxMode.Document;
			}
			set {
				syntaxMode.Document = value;
			}
		}

		internal class StyledTreeSegment : TreeSegment
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

		class HighlightingSegmentTree : SegmentTree<StyledTreeSegment>
		{
			public bool GetStyle (Chunk chunk, ref int endOffset, out string style)
			{
				var segment = GetSegmentsAt (chunk.Offset).FirstOrDefault ();
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

		Dictionary<DocumentLine, HighlightingSegmentTree> lineSegments = new Dictionary<DocumentLine, HighlightingSegmentTree> ();

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
			semanticHighlighting.SemanticHighlightingUpdated += delegate {
				foreach (var kv in lineSegments) {
					try {
						kv.Value.RemoveListener ();
					} catch (Exception) {
					}
				}
				lineSegments.Clear ();

				var margin = editor.TextViewMargin;
				margin.PurgeLayoutCache ();
				editor.QueueDraw ();
			};
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
			SemanticHighlightingSyntaxMode semanticMode;

			int lineNumber;
			public CSharpChunkParser (SemanticHighlightingSyntaxMode semanticMode, SpanParser spanParser, Mono.TextEditor.Highlighting.ColorScheme style, DocumentLine line) : base (semanticMode, spanParser, style, line)
			{
				lineNumber = line.LineNumber;
				this.semanticMode = semanticMode;
			}

			protected override void AddRealChunk (Chunk chunk)
			{
				if (!DefaultSourceEditorOptions.Instance.EnableSemanticHighlighting) {
					base.AddRealChunk (chunk);
					return;
				}
				int endLoc = -1;
				string semanticStyle = null;

				try {
					HighlightingSegmentTree tree;
					if (!semanticMode.lineSegments.TryGetValue (line, out tree)) {
						tree = new HighlightingSegmentTree ();
						tree.InstallListener (semanticMode.Document); 
						foreach (var seg in semanticMode.semanticHighlighting.GetColoredSegments (new MonoDevelop.Core.Text.TextSegment (line.Offset, line.Length))) {
							tree.AddStyle (seg, seg.ColorStyleKey);
						}
						semanticMode.lineSegments[line] = tree;
					}
					string style;
					if (tree.GetStyle (chunk, ref endLoc, out style)) {
						semanticStyle = style;
					}
				} catch (Exception e) {
					Console.WriteLine ("Error in semantic highlighting: " + e);
				}

				if (semanticStyle != null) {
					if (endLoc < chunk.EndOffset) {
						base.AddRealChunk (new Chunk (chunk.Offset, endLoc - chunk.Offset, semanticStyle));
						base.AddRealChunk (new Chunk (endLoc, chunk.EndOffset - endLoc, chunk.Style));
						return;
					}
					chunk.Style = semanticStyle;
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