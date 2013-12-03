
using System;
using System.Collections.Generic;
using System.Linq;
namespace Mono.TextEditor
{
	/// <summary>
	/// A very fast line splitter for read-only documents that generates lines only on demand.
	/// </summary>
	public class PrimitiveLineSplitter : ILineSplitter
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

			public PrimitiveLineSegment (PrimitiveLineSplitter splitter, int lineNumber, int offset, int length, int delimiterLength) : base(length, delimiterLength)
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

		public void Initalize (string text)
		{
			delimiters = new List<LineSplitter.Delimiter> ();

			int offset = 0;
			while (true) {
				var delimiter = LineSplitter.NextDelimiter (text, offset);
				if (delimiter.IsInvalid)
					break;

				delimiters.Add (delimiter);

				offset = delimiter.EndOffset;
			}


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
			int delimiterLength;
			if (number < delimiters.Count) {
				endOffset = delimiters[number].EndOffset;
				delimiterLength = delimiters[number].Length;
			} else {
				endOffset = textLength;
				delimiterLength = 0;
			}
			return new PrimitiveLineSegment (this, number, startOffset, endOffset - startOffset, delimiterLength);
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

		public void TextReplaced (object sender, DocumentChangeEventArgs args)
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

		public event EventHandler<LineEventArgs> LineChanged;
		public event EventHandler<LineEventArgs> LineInserted;
		public event EventHandler<LineEventArgs> LineRemoved;
	}
}
