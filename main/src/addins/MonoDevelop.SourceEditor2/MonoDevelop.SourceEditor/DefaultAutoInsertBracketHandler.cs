//
// DefaultAutoInsertBracketHandler.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using System.Threading;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;

namespace MonoDevelop.SourceEditor
{
	class DefaultAutoInsertBracketHandler : AutoInsertBracketHandler
	{
		const string openBrackets = "{[('\"";
		const string closingBrackets = "}])'\"";

		static readonly string[] excludedMimeTypes = {
			"text/fsharp",
			"text/x-csharp",
			"text/x-json",
			"text/x-javascript",
			"text/x-typescript",
		};

		public override bool Handle (TextEditor editor, DocumentContext ctx, KeyDescriptor descriptor)
		{
			if (Array.IndexOf (excludedMimeTypes, editor.MimeType) >= 0)
				return false;
			int braceIndex = openBrackets.IndexOf (descriptor.KeyChar);
			if (braceIndex < 0)
				return false;


			var line = editor.GetLine (editor.CaretLine);
			if (line == null)
				return false;

			bool inStringOrComment = false;

			var stack = editor.SyntaxHighlighting.GetScopeStackAsync (Math.Max (0, editor.CaretOffset - 2), CancellationToken.None).WaitAndGetResult (CancellationToken.None);
			foreach (var span in stack) {
				if (string.IsNullOrEmpty (span))
					continue;
				if (span.Contains ("string") ||
				    span.Contains ("comment")) {
					inStringOrComment = true;
					break;
				}
			}
			char insertionChar = '\0';
			bool insertMatchingBracket = false;
			if (!inStringOrComment) {
				char closingBrace = closingBrackets [braceIndex];
				char openingBrace = openBrackets [braceIndex];

				int count = 0;
				foreach (char curCh in GetTextWithoutCommentsAndStrings(editor, 0, editor.Length)) {
					if (curCh == openingBrace) {
						count++;
					} else if (curCh == closingBrace) {
						count--;
					}
				}

				if (count >= 0) {
					insertMatchingBracket = true;
					insertionChar = closingBrace;
				}
			}

			if (insertMatchingBracket) {
				using (var undo = editor.OpenUndoGroup ()) {
					editor.EnsureCaretIsNotVirtual ();
					editor.InsertAtCaret (insertionChar.ToString ());
					editor.CaretOffset--;
					editor.StartSession (new SkipCharSession (insertionChar));
				}
				return true;
			}

			return false;
		}

		internal static IEnumerable<char> GetTextWithoutCommentsAndStrings (ITextSource doc, int start, int end)
		{
			bool isInString = false, isInChar = false;
			bool isInLineComment = false, isInBlockComment = false;
			int escaping = 0;

			for (int pos = start; pos < end; pos++) {
				char ch = doc.GetCharAt (pos);
				switch (ch) {
				case '\r':
				case '\n':
					isInLineComment = false;
					break;
				case '/':
					if (isInBlockComment) {
						if (pos > 0 && doc.GetCharAt (pos - 1) == '*')
							isInBlockComment = false;
					} else if (!isInString && !isInChar && pos + 1 < doc.Length) {
						char nextChar = doc.GetCharAt (pos + 1);
						if (nextChar == '/')
							isInLineComment = true;
						if (!isInLineComment && nextChar == '*')
							isInBlockComment = true;
					}
					break;
				case '"':
					if (!(isInChar || isInLineComment || isInBlockComment))
						if (!isInString || escaping != 1)
							isInString = !isInString;
					break;
				case '\'':
					if (!(isInString || isInLineComment || isInBlockComment))
						if (!isInChar || escaping != 1)
							isInChar = !isInChar;
					break;
				case '\\':
					if (escaping != 1)
						escaping = 2;
					break;
				default:
					if (!(isInString || isInChar || isInLineComment || isInBlockComment))
						yield return ch;
					break;
				}
				escaping--;
			}
		}
	}
}

