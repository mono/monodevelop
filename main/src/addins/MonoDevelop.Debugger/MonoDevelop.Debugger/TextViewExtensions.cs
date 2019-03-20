using System;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace MonoDevelop.Debugger
{
	static class TextViewExtensions
	{
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

		public static (int line, int column, int endLine, int endColumn) MDLineAndColumnFromSpan (this ITextSnapshot snapshot, Span span)
		{
			var startLine = snapshot.GetLineFromPosition (span.Start);
			var endLine = snapshot.GetLineFromPosition (span.End);
			return (startLine.LineNumber + 1, span.Start - startLine.Start + 1, endLine.LineNumber + 1, span.End - endLine.Start + 1);
		}

		public static SnapshotSpan SpanFromMDColumnAndLine (this ITextSnapshot snapshot, int line, int column, int endLine, int endColumn)
		{
			var startSnapLine = snapshot.GetLineFromLineNumber (line - 1);
			if (line > 0 && column > 0 && endLine > 0 && endColumn > 0) {
				var endSnapLine = line == endLine ? startSnapLine : snapshot.GetLineFromLineNumber (endLine - 1);
				var startPos = startSnapLine.Start + column - 1;
				return new SnapshotSpan (startPos, endSnapLine.Start + endColumn - 1 - startPos);
			}
			//if we don't have full info return whole line
			return startSnapLine.Extent;
		}

		public static string GetFilePathOrNull (this ITextBuffer textBuffer)
		{
			if (textBuffer.Properties.TryGetProperty (typeof (Microsoft.VisualStudio.Text.ITextDocument), out Microsoft.VisualStudio.Text.ITextDocument textDocument)) {
				return textDocument.FilePath;
			}

			return null;
		}
	}
}
