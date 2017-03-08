//
// TextMateDocumentIndentEngine.cs
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
using System.Collections.Generic;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Ide.Editor.Highlighting.RegexEngine;
using System.Threading;

namespace MonoDevelop.Ide.Editor.TextMate
{
	class TextMateDocumentIndentEngine : IDocumentIndentEngine
	{
		readonly TextEditor editor;

		bool increaseNextLine;
		int indentLevel = 0;
		int nextLineIndent = 0;
		Regex increaseIndentPattern, decreaseIndentPattern, indentNextLinePattern, unIndentedLinePattern;

		public int LineNumber {
			get;
			private set;
		}

		public string NextLineIndent {
			get {
				return CreateIndentString (nextLineIndent);
			}
		}

		public string ThisLineIndent {
			get {
				return CreateIndentString (indentLevel);
			}
		}

		string CreateIndentString (int indent)
		{
			return new string ('\t', indent / editor.Options.TabSize) + new string (' ', indent % editor.Options.TabSize);
		}

		public string CurrentIndent { get; private set; } = "";

		public bool IsValid { get { return increaseIndentPattern != null; } }


		public TextMateDocumentIndentEngine(TextEditor editor)
		{
			this.editor = editor;
			var startScope = editor.SyntaxHighlighting.GetScopeStackAsync (0, CancellationToken.None).WaitAndGetResult (CancellationToken.None);
			var lang = TextMateLanguage.Create (startScope);
			increaseIndentPattern = lang.IncreaseIndentPattern;
			decreaseIndentPattern = lang.DecreaseIndentPattern;
			indentNextLinePattern = lang.IndentNextLinePattern;
			unIndentedLinePattern = lang.UnIndentedLinePattern;
		}

		/// <summary>
		/// For unit testing.
		/// </summary>
		internal TextMateDocumentIndentEngine (TextEditor editor, Regex increaseIndentPattern, Regex decreaseIndentPattern, Regex indentNextLinePattern, Regex unIndentedLinePattern)
		{
			this.editor = editor;
			this.increaseIndentPattern = increaseIndentPattern;
			this.decreaseIndentPattern = decreaseIndentPattern;
			this.indentNextLinePattern = indentNextLinePattern;
			this.unIndentedLinePattern = unIndentedLinePattern;
		}

		public IDocumentIndentEngine Clone ()
		{
			return (IDocumentIndentEngine)MemberwiseClone ();
		}

		public void Push (IReadonlyTextDocument sourceText, IDocumentLine line)
		{
			if (sourceText == null)
				throw new ArgumentNullException (nameof (sourceText));
			if (line == null)
				throw new ArgumentNullException (nameof (line));
			int lineOffset = line.Offset;
			LineNumber++;
			CurrentIndent = line.GetIndentation (sourceText);

			indentLevel = nextLineIndent;
			if (CurrentIndent.Length == line.Length)
				return;
			nextLineIndent = GetIndentLevel (CurrentIndent);
			if (increaseNextLine) {
				DecreaseIndent (ref nextLineIndent);
				increaseNextLine = false;
			}
			string lineText = sourceText.GetTextAt (lineOffset, line.LengthIncludingDelimiter);
			if (increaseIndentPattern != null) {
				var match = increaseIndentPattern.Match (lineText);
				if (match.Success) {
					IncreaseIndent (ref nextLineIndent);
					//var matchLen = lineOffset + line.LengthIncludingDelimiter - (match.Index + match.Length);
					//if (matchLen <= 0)
					//	break;
					//match = increaseIndentPattern.Match (sourceText, match.Index + match.Length, matchLen);
				}
			}

			if (indentNextLinePattern != null) {
				var match = indentNextLinePattern.Match (lineText);
				if (match.Success) {
					IncreaseIndent (ref nextLineIndent);
					increaseNextLine = true;
				}
			}

			if (decreaseIndentPattern != null) {
				var match = decreaseIndentPattern.Match (lineText);
				if (match.Success) {
					DecreaseIndent (ref indentLevel);
					//var matchLen = lineOffset + line.LengthIncludingDelimiter - (match.Index + match.Length);
					//if (matchLen <= 0)
					//	break;
					//match = decreaseIndentPattern.Match (sourceText, match.Index + match.Length, matchLen);
				}
			}
			if (unIndentedLinePattern != null) {
				var match = unIndentedLinePattern.Match (lineText);
				if (match.Success)
					indentLevel = 0;
			}
		}

		int GetIndentLevel (string indent)
		{
			int result = 0;
			foreach (var ch in indent) {
				if (ch == '\t') {
					IncreaseIndent (ref result);
				} else {
					result++;
				}
			}
			return result;
		}

		void IncreaseIndent (ref int nextLineIndent)
		{
			nextLineIndent = (1 + nextLineIndent / editor.Options.TabSize) * editor.Options.TabSize;
		}

		void DecreaseIndent (ref int nextLineIndent)
		{
			nextLineIndent = Math.Max (0, (-1 + nextLineIndent / editor.Options.TabSize) * editor.Options.TabSize);
		}

		public void Reset ()
		{
			indentLevel = 0;
			nextLineIndent = 0;
			increaseNextLine = false;
			LineNumber = 0;
			CurrentIndent = "";
		}
	}
}
