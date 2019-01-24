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

		public static SnapshotSpan SpanFromMDColumnAndLine (this ITextSnapshot snapshot, int line, int column, int endLine, int endColumn)
		{
			var startSnapLine = snapshot.GetLineFromLineNumber (line - 1);
			var endSnapLine = line == endLine ? startSnapLine : snapshot.GetLineFromLineNumber (endLine - 1);
			var startPos = startSnapLine.Start + column - 1;
			return new SnapshotSpan (startPos, endSnapLine.Start + endColumn - 1 - startPos);
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
