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
using System.Threading;
using Mono.TextEditor.Highlighting;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;

namespace MonoDevelop.SourceEditor
{
	class DefaultAutoInsertBracketHandler : AutoInsertBracketHandler
	{
		const string openBrackets = "{[('\"";
		const string closingBrackets = "}])'\"";

		public override bool CanHandle (TextEditor editor)
		{
			return true;
		}

		public override bool Handle (TextEditor editor, DocumentContext ctx, KeyDescriptor descriptor)
		{
			if (descriptor.KeyChar == '\'' && editor.MimeType == "text/fsharp")
				return false;
			int braceIndex = openBrackets.IndexOf (descriptor.KeyChar);
			if (braceIndex < 0)
				return false;
			
			var extEditor = ((SourceEditorView)editor.Implementation).SourceEditorWidget.TextEditor;

			var line = extEditor.Document.GetLine (extEditor.Caret.Line);
			if (line == null)
				return false;

			bool inStringOrComment = false;

			var stack = extEditor.SyntaxHighlighting.GetScopeStackAsync (Math.Max (0, extEditor.Caret.Offset - 2), CancellationToken.None).WaitAndGetResult (CancellationToken.None);
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
				foreach (char curCh in ExtensibleTextEditor.GetTextWithoutCommentsAndStrings(extEditor.Document, 0, extEditor.Document.Length)) {
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
	}
}

