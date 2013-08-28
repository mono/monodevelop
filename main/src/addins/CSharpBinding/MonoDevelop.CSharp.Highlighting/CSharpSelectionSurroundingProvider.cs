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
using Mono.TextEditor;
using MonoDevelop.CSharp.Formatting;

namespace MonoDevelop.CSharp.Highlighting
{
	class CSharpSelectionSurroundingProvider : DefaultSelectionSurroundingProvider
	{
		MonoDevelop.Ide.Gui.Document document;

		public CSharpSelectionSurroundingProvider (MonoDevelop.Ide.Gui.Document document)
		{
			this.document = document;
		}

		public override bool GetSelectionSurroundings (TextEditorData textEditorData, uint unicodeKey, out string start, out string end)
		{
			if (unicodeKey == '/') {
				start = "/*";
				end = "*/";
				return true;
			}

			if (unicodeKey == '"') {
				start = textEditorData.MainSelection.Anchor.Line != textEditorData.MainSelection.Lead.Line ? "@\"" : "\"";
				end = "\"";
				return true;
			}
			return base.GetSelectionSurroundings (textEditorData, unicodeKey, out start, out end);
		}

		public override void HandleSpecialSelectionKey (TextEditorData textEditorData,uint unicodeKey)
		{
			string start, end;
			GetSelectionSurroundings (textEditorData, unicodeKey, out start, out end);
			var selection = textEditorData.MainSelection;

			if (textEditorData.MainSelection.SelectionMode == SelectionMode.Block) {
				int startCol = System.Math.Min (selection.Anchor.Column, selection.Lead.Column) - 1;
				int endCol = System.Math.Max (selection.Anchor.Column, selection.Lead.Column);
				for (int lineNumber = selection.MinLine; lineNumber <= selection.MaxLine; lineNumber++) {
					DocumentLine lineSegment = textEditorData.GetLine (lineNumber);

					if (lineSegment.Offset + startCol < lineSegment.EndOffset)
						textEditorData.Insert (lineSegment.Offset + startCol, start);
					if (lineSegment.Offset + endCol < lineSegment.EndOffset)
						textEditorData.Insert (lineSegment.Offset + endCol, end);
				}

				textEditorData.MainSelection = new Selection (
					new DocumentLocation (selection.Anchor.Line, endCol == selection.Anchor.Column ? endCol + start.Length : startCol + 1 + start.Length),
					new DocumentLocation (selection.Lead.Line, endCol == selection.Anchor.Column ? startCol + 1 + start.Length : endCol + start.Length),
					Mono.TextEditor.SelectionMode.Block);
				textEditorData.Document.CommitMultipleLineUpdate (textEditorData.MainSelection.MinLine, textEditorData.MainSelection.MaxLine);
			} else {
				int anchorOffset = selection.GetAnchorOffset (textEditorData);
				int leadOffset = selection.GetLeadOffset (textEditorData);
				if (leadOffset < anchorOffset) {
					int tmp = anchorOffset;
					anchorOffset = leadOffset;
					leadOffset = tmp;
				}
				textEditorData.Insert (anchorOffset, start);
				textEditorData.Insert (leadOffset >= anchorOffset ? leadOffset + start.Length : leadOffset, end);
			//	textEditorData.SetSelection (anchorOffset + start.Length, leadOffset + start.Length);
				if (CSharpTextEditorIndentation.OnTheFlyFormatting) {
					var l1 = textEditorData.GetLineByOffset (anchorOffset);
					var l2 = textEditorData.GetLineByOffset (leadOffset);
					OnTheFlyFormatter.Format (document, l1.Offset, l2.EndOffsetIncludingDelimiter);
				}
			}
		}

	}
}

