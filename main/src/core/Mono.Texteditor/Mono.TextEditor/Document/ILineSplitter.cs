
using System;
using System.Collections.Generic;
using System.Linq;
namespace Mono.TextEditor
{
	public interface ILineSplitter
	{
		int Count { get; }

		IEnumerable<LineSegment> Lines { get; }

		void Clear ();

		/// <summary>
		/// Initializes the splitter with a new text. No events are fired during this process.
		/// </summary>
		/// <param name="text"></param>
		void Initalize (string text);

		LineSegment Get (int number);
		LineSegment GetLineByOffset (int offset);
		int OffsetToLineNumber (int offset);

		void TextReplaced (object sender, DocumentChangeEventArgs args);
		void TextRemove (int offset, int length);
		void TextInsert (int offset, string text);

		IEnumerable<LineSegment> GetLinesBetween (int startLine, int endLine);
		IEnumerable<LineSegment> GetLinesStartingAt (int startLine);
		IEnumerable<LineSegment> GetLinesReverseStartingAt (int startLine);

		event EventHandler<LineEventArgs> LineChanged;
		event EventHandler<LineEventArgs> LineInserted;
		event EventHandler<LineEventArgs> LineRemoved;
	}
}
