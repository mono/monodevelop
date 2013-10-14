
using System;
using System.Collections.Generic;
using System.Linq;
namespace Mono.TextEditor
{
	public interface ILineSplitter
	{
		int Count { get; }

		/// <summary>
		/// True if during initialization a line ending mismatch was encountered.
		/// </summary>
		bool LineEndingMismatch {
			get;
			set;
		}

		IEnumerable<DocumentLine> Lines { get; }

		void Clear ();

		/// <summary>
		/// Initializes the splitter with a new text. No events are fired during this process.
		/// </summary>
		/// <param name="text"></param>
		void Initalize (string text);

		DocumentLine Get (int number);
		DocumentLine GetLineByOffset (int offset);
		int OffsetToLineNumber (int offset);

		void TextReplaced (object sender, DocumentChangeEventArgs args);
		void TextRemove (int offset, int length);
		void TextInsert (int offset, string text);

		IEnumerable<DocumentLine> GetLinesBetween (int startLine, int endLine);
		IEnumerable<DocumentLine> GetLinesStartingAt (int startLine);
		IEnumerable<DocumentLine> GetLinesReverseStartingAt (int startLine);

		event EventHandler<LineEventArgs> LineChanged;
		event EventHandler<LineEventArgs> LineInserted;
		event EventHandler<LineEventArgs> LineRemoved;
	}
}
