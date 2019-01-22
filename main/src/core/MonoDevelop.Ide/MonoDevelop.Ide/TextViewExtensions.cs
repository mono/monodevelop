//
// Copyright (c) Microsoft Corp. (https://www.microsoft.com)
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

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace MonoDevelop.Ide
{
	public static class TextViewExtensions
	{
		/// <summary>
		/// Gets the parent <see cref="Ide.Gui.Document"/> from an <see cref="ITextView"/>
		/// </summary>
		public static Ide.Gui.Document TryGetParentDocument (this ITextView view)
		{
			if (view.Properties.TryGetProperty<Ide.Gui.Document> (typeof (Ide.Gui.Document), out var document)) {
				return document;
			}
			return null;
		}

		public static string GetFilePathOrNull (this ITextBuffer textBuffer)
		{
			if (textBuffer.Properties.TryGetProperty (typeof (Microsoft.VisualStudio.Text.ITextDocument), out Microsoft.VisualStudio.Text.ITextDocument textDocument)) {
				return textDocument.FilePath;
			}

			return null;
		}

		public static int MDCaretLine (this ITextView view)
		{
			var position = view.Caret.Position.BufferPosition;
			return position.Snapshot.GetLineNumberFromPosition (position.Position) + 1;// MonoDevelop starts with 1, ITextView with 0
		}

		public static (int caretLine, int caretColumn) MDCaretLineAndColumn (this ITextView view)
		{
			var point = view.Caret.Position.BufferPosition;
			var textSnapshotLine = point.GetContainingLine ();
			return (textSnapshotLine.LineNumber + 1, point - textSnapshotLine.Start + 1);
		}

		public static SnapshotSpan SpanFromMDColumnAndLine (this ITextSnapshot snapshot, int line, int column, int endLine, int endColumn)
		{
			var startSnapLine = snapshot.GetLineFromLineNumber (line - 1);
			var endSnapLine = line == endLine ? startSnapLine : snapshot.GetLineFromLineNumber (endLine - 1);
			var startPos = startSnapLine.Start + column - 1;
			return new SnapshotSpan (startPos, endSnapLine.Start + endColumn - 1 - startPos);
		}
	}
}
