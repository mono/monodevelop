//
// SimpleReadonlyDocument.cs
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
using System.Threading.Tasks;
using MonoDevelop.Core;
using System.Threading;

namespace MonoDevelop.Ide.Editor.Util
{
	/// <summary>
	/// A simple and fast implementation for a read only text document.
	/// </summary>
	public class SimpleReadonlyDocument : IReadonlyTextDocument
	{
		readonly ITextSource textSource;
		readonly List<Delimiter> delimiters = new List<Delimiter> ();

		SimpleReadonlyDocument (ITextSource readOnlyTextSource, string fileName, string mimeType)
		{
			textSource = readOnlyTextSource;
			FileName = fileName;
			MimeType = mimeType;
			Initalize (readOnlyTextSource.Text);
		}

		/// <summary>
		/// Creates a new readonly document. Note that the text source is not copied - it needs to be read only.
		/// </summary>
		/// <returns>The readonly document async.</returns>
		/// <param name="readOnlyTextSource">Read only text source.</param>
		/// <param name="fileName">File name.</param>
		public static Task<IReadonlyTextDocument> CreateReadonlyDocumentAsync (ITextSource readOnlyTextSource, string fileName = null, string mimeType = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			return Task.Run (delegate {
				return (IReadonlyTextDocument)new SimpleReadonlyDocument (readOnlyTextSource, fileName, mimeType);
			}, cancellationToken);
		}

		void Initalize (string text)
		{
			int offset = 0;
			while (true) {
				var delimiter = NextDelimiter (text, offset);
				if (delimiter.IsInvalid)
					break;

				delimiters.Add (delimiter);

				offset = delimiter.EndOffset;
			}
		}

		static unsafe Delimiter NextDelimiter (string text, int offset)
		{
			fixed (char* start = text) {
				char* p = start + offset;
				char* endPtr = start + text.Length;

				while (p < endPtr) {
					switch (*p) {
					case NewLine.CR:
						char* nextp = p + 1;
						if (nextp < endPtr && *nextp == NewLine.LF)
							return new Delimiter ((int)(p - start), UnicodeNewline.CRLF);
						return new Delimiter ((int)(p - start), UnicodeNewline.CR);
					case NewLine.LF:
						return new Delimiter ((int)(p - start), UnicodeNewline.LF);
					case NewLine.NEL:
						return new Delimiter ((int)(p - start), UnicodeNewline.NEL);
					//case NewLine.VT:
					//	return new Delimiter ((int)(p - start), UnicodeNewline.VT);
					//case NewLine.FF:
					//	return new Delimiter ((int)(p - start), UnicodeNewline.FF);
					case NewLine.LS:
						return new Delimiter ((int)(p - start), UnicodeNewline.LS);
					case NewLine.PS:
						return new Delimiter ((int)(p - start), UnicodeNewline.PS);
					}
					p++;
				}
				return Delimiter.Invalid;
			}
		}

		readonly struct Delimiter
		{
			public static readonly Delimiter Invalid = new Delimiter (-1, 0);

			public readonly int Offset;
			public readonly UnicodeNewline UnicodeNewline;

			public int Length {
				get {
					return UnicodeNewline == UnicodeNewline.CRLF ? 2 : 1;
				}
			}

			public int EndOffset {
				get { return Offset + Length; }
			}

			public bool IsInvalid {
				get {
					return Offset < 0;
				}
			}

			public Delimiter (int offset, UnicodeNewline unicodeNewline)
			{
				Offset = offset;
				UnicodeNewline = unicodeNewline;
			}
		}

		int OffsetToLineNumber (int offset)
		{
			for (int i = 0; i < delimiters.Count; i++) {
				var delimiter = delimiters[i];
				if (offset <= delimiter.Offset)
					return i + 1;
			}
			return delimiters.Count + 1;
		}

		#region IReadonlyTextDocument implementation

		/// <inheritdoc/>
		public int LocationToOffset (int line, int column)
		{
			if (line > LineCount || line < DocumentLocation.MinLine)
				return -1;
			var documentLine = GetLine (line);
			return Math.Min (Length, documentLine.Offset + Math.Max (0, Math.Min (documentLine.Length, column - 1)));
		}

		/// <inheritdoc/>
		public DocumentLocation OffsetToLocation (int offset)
		{
			int lineNr = OffsetToLineNumber (offset);
			if (lineNr < 1)
				return DocumentLocation.Empty;
			var line = GetLine (lineNr);
			var col = Math.Max (1, Math.Min (line.LengthIncludingDelimiter, offset - line.Offset) + 1);
			return new DocumentLocation (lineNr, col);
		}

		/// <inheritdoc/>
		public IDocumentLine GetLine (int number)
		{
			number--;
			if (number < 0)
				return null;
			int startOffset = number > 0 ? delimiters[number - 1].EndOffset : 0;
			int endOffset;
			UnicodeNewline newLine;
			if (number < delimiters.Count) {
				endOffset = delimiters[number].EndOffset;
				newLine = delimiters[number].UnicodeNewline;
			} else {
				endOffset = Length;
				newLine = UnicodeNewline.Unknown;
			}
			return new SimpleLineSegment (this, number + 1, startOffset, endOffset - startOffset, newLine);
		}

		sealed class SimpleLineSegment : IDocumentLine
		{
			readonly SimpleReadonlyDocument splitter;

			public SimpleLineSegment (SimpleReadonlyDocument splitter, int lineNumber, int offset, int length, UnicodeNewline newLine)
			{
				this.splitter = splitter;
				LineNumber = lineNumber;
				LengthIncludingDelimiter = length;
				UnicodeNewline = newLine;
				Offset = offset;
			}

			#region IDocumentLine implementation

			public int LengthIncludingDelimiter {
				get;
				private set;
			}

			public int EndOffsetIncludingDelimiter {
				get {
					return Offset + LengthIncludingDelimiter;
				}
			}

			public ISegment SegmentIncludingDelimiter {
				get {
					return new TextSegment (Offset, LengthIncludingDelimiter);
				}
			}

			public UnicodeNewline UnicodeNewline {
				get;
				private set;
			}

			public int DelimiterLength {
				get {
					switch (UnicodeNewline) {
					case UnicodeNewline.Unknown:
						return 0;
					case UnicodeNewline.CRLF:
						return 2;
					default:
						return 1;
					}
				}
			}

			public int LineNumber {
				get;
				private set;
			}

			public IDocumentLine PreviousLine {
				get {
					if (LineNumber == 1)
						return null;
					return splitter.GetLine (LineNumber - 1);
				}
			}

			public IDocumentLine NextLine {
				get {
					if (LineNumber >= splitter.LineCount)
						return null;
					return splitter.GetLine (LineNumber + 1);
				}
			}

			public bool IsDeleted {
				get {
					return false;
				}
			}
			#endregion

			#region ISegment implementation

			public int Offset {
				get;
				private set;
			}

			public int Length {
				get {
					return LengthIncludingDelimiter - DelimiterLength;
				}
			}

			public int EndOffset {
				get {
					return Offset + Length;
				}
			}
			#endregion
		}

		/// <inheritdoc/>
		public IDocumentLine GetLineByOffset (int offset)
		{
			return GetLine (OffsetToLineNumber (offset));
		}

		/// <inheritdoc/>
		public bool IsReadOnly {
			get {
				return true;
			}
		}

		/// <inheritdoc/>
		public FilePath FileName {
			get;
			private set;
		}

		/// <inheritdoc/>
		public string MimeType {
			get;
			private set;
		}

		/// <inheritdoc/>
		public int LineCount {
			get {
				return delimiters.Count + 1;
			}
		}
		#endregion

		#region ITextSource implementation

		/// <inheritdoc/>
		public char GetCharAt (int offset)
		{
			return textSource.GetCharAt (offset);
		}

		public char this [int offset] {
			get {
				return textSource.GetCharAt (offset);
			}
		}

		/// <inheritdoc/>
		public string GetTextAt (int offset, int length)
		{
			return textSource.GetTextAt (offset, length);
		}

		/// <inheritdoc/>
		public System.IO.TextReader CreateReader ()
		{
			return textSource.CreateReader ();
		}

		/// <inheritdoc/>
		public System.IO.TextReader CreateReader (int offset, int length)
		{
			return textSource.CreateReader (offset, length);
		}

		/// <inheritdoc/>
		public void WriteTextTo (System.IO.TextWriter writer)
		{
			textSource.WriteTextTo (writer);
		}

		/// <inheritdoc/>
		public void WriteTextTo (System.IO.TextWriter writer, int offset, int length)
		{
			textSource.WriteTextTo (writer, offset, length);
		}

		/// <inheritdoc/>
		public ITextSourceVersion Version {
			get {
				return textSource.Version;
			}
		}

		/// <inheritdoc/>
		public System.Text.Encoding Encoding {
			get {
				return textSource.Encoding;
			}
		}

		/// <inheritdoc/>
		public int Length {
			get {
				return textSource.Length;
			}
		}

		/// <inheritdoc/>
		public string Text {
			get {
				return textSource.Text;
			}
		}

		public ITextSource CreateSnapshot ()
		{
			return this;
		}

		public ITextSource CreateSnapshot (int offset, int length)
		{
			return new StringTextSource (Text.Substring (offset, length));
		}

		/// <inheritdoc/>
		public void CopyTo (int sourceIndex, char [] destination, int destinationIndex, int count)
		{
			textSource.CopyTo (sourceIndex, destination, destinationIndex, count); 
		}

		#endregion

	}
}