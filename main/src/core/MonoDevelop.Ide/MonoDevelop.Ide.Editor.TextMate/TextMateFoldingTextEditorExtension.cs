//
// TextMateFoldingTextEditorExtension.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2016 Microsoft Corporation
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
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.TypeSystem;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using MonoDevelop.Core;
using System.Linq;
using MonoDevelop.Ide.Editor.Highlighting.RegexEngine;

namespace MonoDevelop.Ide.Editor.TextMate
{
	class TextMateFoldingTextEditorExtension : TextEditorExtension
	{
		Regex foldingStartMarker, foldingStopMarker;
		
		protected override void Initialize ()
		{
			Editor.TextChanged += UpdateFoldings;

			var startScope = Editor.SyntaxHighlighting.GetScopeStackAsync (0, CancellationToken.None).WaitAndGetResult (CancellationToken.None);
			var lang = TextMateLanguage.Create (startScope);
			foldingStartMarker = lang.FoldingStartMarker;
			foldingStopMarker = lang.FoldingStopMarker;
			UpdateFoldings (null, null);
		}


		public override void Dispose ()
		{
			Editor.TextChanged -= UpdateFoldings;
		}

		struct LineInfo {
			public readonly IDocumentLine line;
			public readonly int indentLength;
			public readonly int nonWsLineNumber;

			public LineInfo (IDocumentLine line, int indentLength, int nonWsLineNumber)
			{
				this.line = line;
				this.indentLength = indentLength;
				this.nonWsLineNumber = nonWsLineNumber;
			}
		}

		CancellationTokenSource src = new CancellationTokenSource ();
		void UpdateFoldings (object sender, TextChangeEventArgs e)
		{
			if (TypeSystemService.GetParser (Editor.MimeType) != null || DocumentContext.ParsedDocument != null)
				return;
			var scopeStack = Editor.SyntaxHighlighting.GetScopeStackAsync (0, CancellationToken.None).WaitAndGetResult (CancellationToken.None);
			if (!scopeStack.Any (s => s.Contains ("source")))
				return;

			src.Cancel ();
			src = new CancellationTokenSource ();
			var token = src.Token;
			var snapshot = Editor.CreateDocumentSnapshot ();
			Task.Run (async delegate {
				var foldings = await GetFoldingsAsync (snapshot, token);
				await Runtime.RunInMainThread (delegate {
					if (token.IsCancellationRequested)
						return;
					Editor.SetFoldings (foldings);
				});
			});
		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		internal async Task<IEnumerable<IFoldSegment>> GetFoldingsAsync(IReadonlyTextDocument doc, CancellationToken token)
		#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
			if (foldingStartMarker == null || foldingStopMarker == null)
				return GenerateFoldingsFromIndentationStack (doc, token);
			var foldings = new List<FoldSegment> ();
			int offset = 0;
			var foldStack = new Stack<int> ();
			foreach (var line in doc.GetLines ()) {
				var lineText = doc.GetTextAt (offset, line.Length);
				var startMatch = foldingStartMarker.Match (lineText);
				var stopMatch = foldingStopMarker.Match (lineText);
				if (startMatch.Success && !stopMatch.Success) {
					foldStack.Push (offset + startMatch.Index);
				} else if (!startMatch.Success && stopMatch.Success) {
					if (foldStack.Count > 0) {
						int start = foldStack.Pop ();
						foldings.Add (new FoldSegment (start, offset + line.Length - start));
					}
				} else if (startMatch.Success && stopMatch.Success) {
					if (stopMatch.Index < startMatch.Index) {
						if (foldStack.Count > 0) {
							int start = foldStack.Pop ();
							foldings.Add (new FoldSegment (start, offset + line.Length - start));
						}
						foldStack.Push (startMatch.Index);
					}
					// ignore foldings inside a single line.
				}
				offset += line.LengthIncludingDelimiter;
			}
			return foldings;
		}

		class FoldTreeSegments : TreeSegment
		{
			public FoldTreeSegments (int offset, int length) : base (offset, length)
			{
			}
		}

		static IEnumerable<IFoldSegment> GenerateFoldingsFromIndentationStack (IReadonlyTextDocument doc, CancellationToken token)
		{
			var foldings = new List<FoldSegment> ();

			var indentStack = new Stack<LineInfo> ();
			var line = doc.GetLine (1);
			indentStack.Push (new LineInfo (line, line.GetIndentation (doc).Length, 1));
			int curLineNumber = 0;
			while ((line = line.NextLine) != null) {
				if (token.IsCancellationRequested)
					return Enumerable.Empty<IFoldSegment> ();
				var stackIndent = indentStack.Peek ();
				var curIndent = line.GetIndentation (doc);

				if (curIndent.Length == line.Length)
					continue;

				var curIndentLength = line.GetIndentation (doc).Length;
				curLineNumber++;


				if (stackIndent.indentLength < curIndentLength) {
					indentStack.Push (new LineInfo (line.PreviousLine, curIndentLength, curLineNumber));
				} else {
					while (curIndent.Length < stackIndent.indentLength) {
						if (token.IsCancellationRequested)
							return Enumerable.Empty<IFoldSegment> ();

						indentStack.Pop ();
						if (curLineNumber - stackIndent.nonWsLineNumber >= 2) {
							foldings.Add (new FoldSegment (stackIndent.line.EndOffset, line.EndOffset - stackIndent.line.EndOffset));
						}
						if (indentStack.Count == 0) {
							indentStack.Push (stackIndent);
							break;
						}
						stackIndent = indentStack.Peek ();
					}
				}
			}
			return foldings;
		}

		class FoldSegment : AbstractSegment, IFoldSegment, IComparable
		{
			public string CollapsedText {
				get {
					return "...";
				}
				set {
					throw new NotImplementedException ();
				}
			}

			public FoldingType FoldingType {
				get {
					return FoldingType.Unknown;
				}
				set {
					throw new NotImplementedException ();
				}
			}

			public bool IsCollapsed { get; set; }

			public FoldSegment (int offset, int length) : base (offset, length)
			{
			}

			int IComparable.CompareTo (object obj)
			{
				var segment = (IFoldSegment)obj;
				return this.Offset != segment.Offset ? this.Offset.CompareTo (segment.Offset) : 0;
			}
		}
	}
}