using System;
using Mono.TextEditor.Highlighting;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor;

namespace Mono.TextEditor
{
	static class SelectionExtension
	{
		public static ISegment GetSelectionRange (this Selection selection, TextEditorData data)
		{
			int anchorOffset = GetAnchorOffset (selection, data);
			int leadOffset = GetLeadOffset (selection, data);
			return new TextSegment (System.Math.Min (anchorOffset, leadOffset), System.Math.Abs (anchorOffset - leadOffset));
		}

		// for markup syntax mode the syntax highlighting information need to be taken into account
		// when calculating the selection offsets.
		static int PosToOffset (TextEditorData data, DocumentLocation loc)
		{
			DocumentLine line = data.GetLine (loc.Line);
			if (line == null)
				return 0;
			var startChunk = data.GetChunks (line, line.Offset, line.LengthIncludingDelimiter);
			int col = 1;
			foreach (var chunk in startChunk) {
				if (col <= loc.Column && loc.Column < col + chunk.Length)
					return chunk.Offset - col + loc.Column;
				col += chunk.Length;
			}
			return line.Offset + line.Length;
		}

		public static int GetAnchorOffset (this Selection selection, TextEditorData data)
		{
			return data.Document.LocationToOffset (selection.Anchor);
		}

		public static int GetLeadOffset (this Selection selection, TextEditorData data)
		{
			return data.Document.LocationToOffset (selection.Lead);
		}

		public static int GetVirtualSpacesCount (this Selection selection, TextEditorData data)
		{
			if (selection.SelectionMode == SelectionMode.Normal)
				return 0;

			int result = 0;
			int minColumn = Math.Min (selection.Anchor.Column, selection.Lead.Column);
			int maxColumn = Math.Max (selection.Anchor.Column, selection.Lead.Column);

			for (int lineNr = selection.MinLine; lineNr <= selection.MaxLine; lineNr++) {
				var line = data.GetLine (lineNr);
				if (line.Length < maxColumn) {
					if (minColumn < line.Length) {
						result += maxColumn - line.Length - 1;
					} else {
						result += maxColumn - minColumn;
					}
				}
			}

			return result;
		}
	}
}
