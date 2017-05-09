
using System;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor;

namespace Mono.TextEditor
{
	/// <summary>
	/// A very fast line splitter for read-only documents that generates lines only on demand.
	/// </summary>
	class PrimitiveLineSplitter : ILineSplitter
	{
		int textLength;
		List<LineSplitter.Delimiter> delimiters = new List<LineSplitter.Delimiter> ();

		sealed class PrimitiveLineSegment : DocumentLine
		{
			readonly PrimitiveLineSplitter splitter;
			readonly int lineNumber;

			public override int Offset { get; set; }

			public override int LineNumber {
				get {
					return lineNumber;
				}
			}

			public override DocumentLine NextLine {
				get {
					return splitter.Get (lineNumber + 1);
				}
			}

			public override DocumentLine PreviousLine {
				get {
					return splitter.Get (lineNumber - 1);
				}
			}

			public PrimitiveLineSegment (PrimitiveLineSplitter splitter, int lineNumber, int offset, int length, UnicodeNewline newLine) : base(length, newLine)
			{
				this.splitter = splitter;
				this.lineNumber = lineNumber;
				Offset = offset;
			}
		}

		public bool LineEndingMismatch {
			get;
			set;
		}

		public int Count {
			get { return delimiters.Count + 1; }
		}

		public IEnumerable<DocumentLine> Lines {
			get { return GetLinesStartingAt (DocumentLocation.MinLine); }
		}

		public void Initalize (string text, out DocumentLine longestLine)
		{
			delimiters = new List<LineSplitter.Delimiter> ();

			int offset = 0, maxLength = 0, maxLine = 0;
			while (true) {
				var delimiter = LineSplitter.NextDelimiter (text, offset);
				if (delimiter.IsInvalid)
					break;

				var length = delimiter.EndOffset - offset;
				if (length > maxLength) {
					maxLength = length;
					maxLine = delimiters.Count;
				}
				delimiters.Add (delimiter);
				offset = delimiter.EndOffset;
			}
			longestLine = Get (maxLine);

			textLength = text.Length;
		}

		public void Clear ()
		{
			delimiters.Clear ();
			textLength = 0;
		}

		public DocumentLine Get (int number)
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
				endOffset = textLength;
				newLine = UnicodeNewline.Unknown;
			}
			return new PrimitiveLineSegment (this, number, startOffset, endOffset - startOffset, newLine);
		}

		public DocumentLine GetLineByOffset (int offset)
		{
			return Get (OffsetToLineNumber (offset));
		}

		public int OffsetToLineNumber (int offset)
		{
			for (int i = 0; i < delimiters.Count; i++) {
				var delimiter = delimiters[i];
				if (offset < delimiter.Offset)
					return i + 1;
			}
			return delimiters.Count;
		}

		public void TextReplaced (object sender, TextChangeEventArgs args)
		{
			throw new NotSupportedException ("Operation not supported on this line splitter.");
		}

		public void TextRemove (int offset, int length)
		{
			throw new NotSupportedException ("Operation not supported on this line splitter.");
		}

		public void TextInsert (int offset, string text)
		{
			throw new NotSupportedException ("Operation not supported on this line splitter.");
		}

		public IEnumerable<DocumentLine> GetLinesBetween (int startLine, int endLine)
		{
			for (int i = startLine; i <= endLine; i++)
				yield return Get (i);
		}

		public IEnumerable<DocumentLine> GetLinesStartingAt (int startLine)
		{
			for (int i = startLine; i <= Count; i++)
				yield return Get (i);
		}

		public IEnumerable<DocumentLine> GetLinesReverseStartingAt (int startLine)
		{
			for (int i = startLine; i-- > DocumentLocation.MinLine;)
				yield return Get (i);
		}

	}
}
