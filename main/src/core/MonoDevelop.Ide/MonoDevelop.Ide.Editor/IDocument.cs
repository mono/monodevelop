//
// IDocument.cs
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
using MonoDevelop.Ide.TextEditing;

namespace MonoDevelop.Ide.Editor
{
	/// <summary>
	/// A document representing a source code file for refactoring.
	/// Line and column counting starts at 1.
	/// Offset counting starts at 0.
	/// </summary>
	public interface IDocument : ITextSource, IServiceProvider
	{
		/// <summary>
		/// Gets or sets the type of the MIME.
		/// </summary>
		/// <value>The type of the MIME.</value>
		string MimeType {
			get;
			set;
		}

		/// <summary>
		/// Gets/Sets the text of the whole document..
		/// </summary>
		new string Text { get; set; } // hides ITextSource.Text to add the setter

		string EolMarker { get; }

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
		/// Gets the number of lines in the document.
		/// </summary>
		int LineCount { get; }

		IEnumerable<IDocumentLine> Lines {
			get;
		}


		string GetLineText (int line, bool includeDelimiter = false);

		IEnumerable<IDocumentLine> GetLinesBetween (int startLine, int endLine);

		IEnumerable<IDocumentLine> GetLinesStartingAt (int startLine);

		IEnumerable<IDocumentLine> GetLinesReverseStartingAt (int startLine);

		int LocationToOffset (int line, int column);

		int LocationToOffset (TextLocation location);

		TextLocation OffsetToLocation (int offset);

		int Insert (int offset, string text);

		void Remove (int offset, int count);

		void Remove (ISegment segment);

		int Replace (int offset, int count, string value);

		string GetTextBetween (int startOffset, int endOffset);

		string GetTextBetween (TextLocation start, TextLocation end);

		IDocumentLine GetLine (int lineNumber);

		IDocumentLine GetLineByOffset (int offset);

		int OffsetToLineNumber (int offset);

		/// <summary>
		/// Gets the name of the file the document is stored in.
		/// Could also be a non-existent dummy file name or null if no name has been set.
		/// </summary>
		string FileName { get; set; }

		/// <summary>
		/// Fired when the file name of the document changes.
		/// </summary>
		event EventHandler FileNameChanged;

		void AddMarker (IDocumentLine line, ITextLineMarker lineMarker);
		void AddMarker (int lineNumber, ITextLineMarker lineMarker);
		void RemoveMarker (ITextLineMarker lineMarker);
		IEnumerable<ITextLineMarker> GetLineMarker (IDocumentLine line);

		#region Text segment markers

		IEnumerable<ITextSegmentMarker> GetTextSegmentMarkersAt (ISegment segment);
		IEnumerable<ITextSegmentMarker> GetTextSegmentMarkersAt (int offset);

		/// <summary>
		/// Adds a marker to the document.
		/// </summary>
		void AddMarker (ITextSegmentMarker marker);

		/// <summary>
		/// Removes a marker from the document.
		/// </summary>
		/// <returns><c>true</c>, if marker was removed, <c>false</c> otherwise.</returns>
		/// <param name="marker">Marker.</param>
		bool RemoveMarker (ITextSegmentMarker marker);

		#endregion

		IEnumerable<IFoldSegment> GetFoldingsFromOffset (int offset);
		IEnumerable<IFoldSegment> GetFoldingContaining (int lineNumber);
		IEnumerable<IFoldSegment> GetFoldingContaining (IDocumentLine line);
		IEnumerable<IFoldSegment> GetStartFoldings (int lineNumber);
		IEnumerable<IFoldSegment> GetStartFoldings (IDocumentLine line);
		IEnumerable<IFoldSegment> GetEndFoldings (int lineNumber);
		IEnumerable<IFoldSegment> GetEndFoldings (IDocumentLine line);
	}

	public static class DocumentExtensions
	{
		public static string GetTextBetween (this IDocument document, int startLine, int startColumn, int endLine, int endColumn)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			return document.GetTextBetween (new TextLocation (startLine, startColumn), new TextLocation (endLine, endColumn));
		}

		public static string GetLineIndent (this IDocument document, int lineNumber)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			return document.GetLineIndent (document.GetLine (lineNumber));
		}

		public static string GetLineIndent (this IDocument document, IDocumentLine segment)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			if (segment == null)
				throw new ArgumentNullException ("segment");
			return segment.GetIndentation (document);
		}

		static int[] GetDiffCodes (IDocument document, ref int codeCounter, Dictionary<string, int> codeDictionary, bool includeEol)
		{
			int i = 0;
			var result = new int[document.LineCount];
			foreach (var line in document.Lines) {
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

		public static IEnumerable<Hunk> Diff (this IDocument document, IDocument changedDocument, bool includeEol = true)
		{
			var codeDictionary = new Dictionary<string, int> ();
			int codeCounter = 0;
			return MonoDevelop.Ide.Editor.Diff.GetDiff<int> (GetDiffCodes (document, ref codeCounter, codeDictionary, includeEol),
				GetDiffCodes (changedDocument, ref codeCounter, codeDictionary, includeEol));
		}

	}
}

