//
// CSharpSelectionSurroundingProvider.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.CSharp.Formatting;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;

namespace MonoDevelop.CSharp.Highlighting
{
	class CSharpSelectionSurroundingProvider : SelectionSurroundingProvider
	{
		readonly DocumentContext context;
		readonly TextEditor editor;

		public CSharpSelectionSurroundingProvider (MonoDevelop.Ide.Editor.TextEditor editor, DocumentContext context)
		{
			this.editor = editor;
			this.context = context;
		}

		#region SelectionSurroundingProvider implementation

		public override bool GetSelectionSurroundings (uint unicodeKey, out string start, out string end)
		{
			switch ((char)unicodeKey) {
			case '"':
				start = editor.SelectionRegion.BeginLine != editor.SelectionRegion.EndLine ? "@\"" : "\"";
				end = "\"";
				return true;
			case '\'':
				start = end = "'";
				return true;
			case '(':
				start = "(";
				end = ")";
				return true;
			case '<':
				start = "<";
				end = ">";
				return true;
			case '[':
				start = "[";
				end = "]";
				return true;
			case '{':
				start = "{";
				end = "}";
				return true;
			case '/':
				start = "/*";
				end = "*/";
				return true;
			default:
				start = end = "";
				return false;
			}
		}

		public override void HandleSpecialSelectionKey (uint unicodeKey)
		{
			string start, end;
			((SelectionSurroundingProvider)this).GetSelectionSurroundings (unicodeKey, out start, out end);

			if (editor.SelectionMode == SelectionMode.Block) {
				var selection = editor.SelectionRegion;
				int startCol = System.Math.Min (selection.Begin.Column, selection.End.Column) - 1;
				int endCol = System.Math.Max (selection.Begin.Column, selection.End.Column);

				int minLine = System.Math.Min (selection.Begin.Line, selection.End.Line);
				int maxLine = System.Math.Max (selection.BeginLine, selection.End.Line);


				for (int lineNumber = minLine; lineNumber <= maxLine; lineNumber++) {
					var lineSegment = editor.GetLine (lineNumber);

					if (lineSegment.Offset + startCol < lineSegment.EndOffset)
						editor.InsertText (lineSegment.Offset + startCol, start);
					if (lineSegment.Offset + endCol < lineSegment.EndOffset)
						editor.InsertText (lineSegment.Offset + endCol, end);
				}

//				textEditorData.MainSelection = new Selection (
//					new DocumentLocation (selection.Anchor.Line, endCol == selection.Anchor.Column ? endCol + start.Length : startCol + 1 + start.Length),
//					new DocumentLocation (selection.Lead.Line, endCol == selection.Anchor.Column ? startCol + 1 + start.Length : endCol + start.Length),
//					MonoDevelop.Ide.Editor.SelectionMode.Block);
			} else {
				var selectionRange = editor.SelectionRange;
				int anchorOffset = selectionRange.Offset;
				int leadOffset = selectionRange.EndOffset;
				var text = editor.GetTextAt (selectionRange);
				if (editor.Options.GenerateFormattingUndoStep) {
					using (var undo = editor.OpenUndoGroup ()) {
						editor.ReplaceText (selectionRange, start);
					}
					using (var undo = editor.OpenUndoGroup ()) {
						editor.ReplaceText (anchorOffset, 1, start + text + end);
						editor.SetSelection (anchorOffset + start.Length, leadOffset + start.Length + end.Length);
					}
					if (unicodeKey == '{') {
						using (var undo = editor.OpenUndoGroup ()) {
							OnTheFlyFormatter.Format (editor, context, anchorOffset + start.Length - 1, leadOffset + start.Length + end.Length);
						}
					}
				} else {
					using (var undo = editor.OpenUndoGroup ()) {
						editor.InsertText (anchorOffset, start);
						editor.InsertText (leadOffset >= anchorOffset ? leadOffset + start.Length : leadOffset, end);
						if (unicodeKey == '{') {
							OnTheFlyFormatter.Format (editor, context, anchorOffset + start.Length, leadOffset + start.Length + end.Length);
						} else {
							editor.SetSelection (anchorOffset + start.Length, leadOffset + start.Length + end.Length);
						}
					}
				}
			}
		}
		#endregion
	}
}