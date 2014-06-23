//
// ITextDocument.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Core.Text;
using System.Collections.Generic;
using System.Text;

namespace MonoDevelop.Ide.Editor
{
	public interface IReadonlyTextDocument : ITextSource
	{
		bool IsReadOnly { get; }

		string FileName { get; }

		string MimeType { get; }

		/// <summary>
		/// Gets the number of lines in the document.
		/// </summary>
		int LineCount { get; }

		int LocationToOffset (int line, int column);

		DocumentLocation OffsetToLocation (int offset);

		IDocumentLine GetLine (int lineNumber);

		IDocumentLine GetLineByOffset (int offset);
	}

	public interface ITextDocument : IReadonlyTextDocument
	{
		/// <summary>
		/// Gets/Sets the text of the whole document..
		/// </summary>
		new string Text { get; set; } // hides ITextSource.Text to add the setter

		new bool IsReadOnly { get; set; }

		new string FileName { get; set; }
		new string MimeType { get; set; }
		new bool UseBOM { get; set; }
		new Encoding Encoding { get; set; }

		void Insert (int offset, string text);

		void Remove (int offset, int length);

		void Replace (int offset, int length, string value);

		bool IsInAtomicUndo {
			get;
		}

		void Undo ();
		void Redo ();

		IDisposable OpenUndoGroup();

		/// <summary>
		/// This event is called directly before a change is applied to the document.
		/// </summary>
		/// <remarks>
		/// It is invalid to modify the document within this event handler.
		/// Aborting the change (by throwing an exception) is likely to cause corruption of data structures
		/// that listen to the Changing and Changed events.
		/// </remarks>
		event EventHandler<TextChangeEventArgs> TextChanging;

		/// <summary>
		/// This event is called directly after a change is applied to the document.
		/// </summary>
		/// <remarks>
		/// It is invalid to modify the document within this event handler.
		/// Aborting the event handler (by throwing an exception) is likely to cause corruption of data structures
		/// that listen to the Changing and Changed events.
		/// </remarks>
		event EventHandler<TextChangeEventArgs> TextChanged;
	
		/// <summary>
		/// Creates an immutable snapshot of this document.
		/// </summary>
		IReadonlyTextDocument CreateDocumentSnapshot();
	}

	public static class DocumentExtensions
	{
		/// <summary>
		/// Retrieves the text for a portion of the document.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">offset or length is outside the valid range.</exception>
		public static string GetTextAt(this IReadonlyTextDocument source, ISegment segment)
		{
			if (source == null)
				throw new ArgumentNullException ("source");
			return source.GetTextAt (segment.Offset, segment.Length);
		}

		public static IEnumerable<IDocumentLine> GetLines (this IReadonlyTextDocument document)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			return document.GetLinesStartingAt (1);
		}

		public static IEnumerable<IDocumentLine> GetLinesBetween (this IReadonlyTextDocument document, int startLine, int endLine)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			if (startLine < 1 || startLine > document.LineCount)
				throw new ArgumentOutOfRangeException ("startLine", startLine, string.Format ("value should be between 1 and {0}", document.LineCount));
			if (endLine < 1 || endLine > document.LineCount)
				throw new ArgumentOutOfRangeException ("endLine", endLine, string.Format ("value should be between 1 and {0}", document.LineCount));

			var curLine = document.GetLine (startLine);
			int count = endLine - startLine;
			while (curLine != null && count --> 0) {
				yield return curLine;
				curLine = curLine.NextLine;
			}
		}

		public static IEnumerable<IDocumentLine> GetLinesStartingAt (this IReadonlyTextDocument document, int startLine)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			if (startLine < 1 || startLine > document.LineCount)
				throw new ArgumentOutOfRangeException ("startLine", startLine, string.Format ("value should be between 1 and {0}", document.LineCount));
			var curLine = document.GetLine (startLine);
			while (curLine != null) {
				yield return curLine;
				curLine = curLine.NextLine;
			}
		}

		public static IEnumerable<IDocumentLine> GetLinesReverseStartingAt (this IReadonlyTextDocument document, int startLine)
		{
			if (startLine < 1 || startLine > document.LineCount)
				throw new ArgumentOutOfRangeException ("startLine", startLine, string.Format ("value should be between 1 and {0}", document.LineCount));
			var curLine = document.GetLine (startLine);
			while (curLine != null) {
				yield return curLine;
				curLine = curLine.PreviousLine;
			}
		}

		public static string GetTextBetween (this IReadonlyTextDocument document, int startLine, int startColumn, int endLine, int endColumn)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			return document.GetTextBetween (new DocumentLocation (startLine, startColumn), new DocumentLocation (endLine, endColumn));
		}

		public static string GetLineIndent (this IReadonlyTextDocument document, int lineNumber)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			return document.GetLineIndent (document.GetLine (lineNumber));
		}

		public static string GetLineIndent (this IReadonlyTextDocument document, IDocumentLine segment)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			if (segment == null)
				throw new ArgumentNullException ("segment");
			return segment.GetIndentation (document);
		}

		public static string GetLineText (this IReadonlyTextDocument document, IDocumentLine line, bool includeDelimiter = false)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			if (line == null)
				throw new ArgumentNullException ("line");
			return document.GetTextAt (includeDelimiter ? line.SegmentIncludingDelimiter : line);
		}

		public static string GetLineText (this IReadonlyTextDocument document, int lineNumber, bool includeDelimiter = false)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			var line = document.GetLine (lineNumber);
			return document.GetTextAt (includeDelimiter ? line.SegmentIncludingDelimiter : line);
		}

		static int[] GetDiffCodes (IReadonlyTextDocument document, ref int codeCounter, Dictionary<string, int> codeDictionary, bool includeEol)
		{
			int i = 0;
			var result = new int[document.LineCount];
						foreach (var line in document.GetLinesStartingAt (1)) {
				string lineText = document.GetTextAt (line.Offset, includeEol ? line.LengthIncludingDelimiter : line.Length);
				int curCode;
				if (!codeDictionary.TryGetValue (lineText, out curCode)) {
					codeDictionary[lineText] = curCode = ++codeCounter;
				}
				result[i] = curCode;
				i++;
			}
			return result;
		}

		public static IEnumerable<Hunk> Diff (this IReadonlyTextDocument document, IReadonlyTextDocument changedDocument, bool includeEol = true)
		{
			var codeDictionary = new Dictionary<string, int> ();
			int codeCounter = 0;
			return MonoDevelop.Ide.Editor.Diff.GetDiff<int> (GetDiffCodes (document, ref codeCounter, codeDictionary, includeEol),
				GetDiffCodes (changedDocument, ref codeCounter, codeDictionary, includeEol));
		}

		public static int OffsetToLineNumber (this IReadonlyTextDocument document, int offset)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			if (offset < 0 || offset > document.Length)
				throw new ArgumentNullException ("offset");
			return document.OffsetToLocation (offset).Line;
		}

		public static int LocationToOffset (this IReadonlyTextDocument document, DocumentLocation location)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			return document.LocationToOffset (location.Line, location.Column);
		}

		public static string GetTextBetween (this IReadonlyTextDocument document, int startOffset, int endOffset)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			if (startOffset < 0 || startOffset > document.Length)
				throw new ArgumentNullException ("startOffset");
			if (endOffset < 0 || endOffset > document.Length)
				throw new ArgumentNullException ("endOffset");
			if (startOffset > endOffset)
				throw new InvalidOperationException ();
			return document.GetTextAt (startOffset, endOffset - startOffset);
		}

		public static string GetTextBetween (this IReadonlyTextDocument document, DocumentLocation start, DocumentLocation end)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			return document.GetTextBetween (document.LocationToOffset (start), document.LocationToOffset (end));
		}

		public static void Remove (this ITextDocument document, ISegment segment)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			document.Remove (segment.Offset, segment.Length);
		}

		public static void Replace (this ITextDocument document, ISegment segment, string value)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			document.Replace (segment.Offset, segment.Length, value);
		}

		public static string GetEolMarker (this IReadonlyTextDocument document)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			string eol = null;
			if (document.LineCount > 0) {
				var line = document.GetLine (1);
				if (line.DelimiterLength > 0) 
					eol = document.GetTextAt (line.Length, line.DelimiterLength);
			}

			return !string.IsNullOrEmpty (eol) ? eol : DefaultSourceEditorOptions.Instance.DefaultEolMarker;
		}
	}
}