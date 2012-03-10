
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

		sealed class PrimitiveLineSegment : LineSegment
		{
			public override int Offset { get; set; }

			public PrimitiveLineSegment (int offset, int length, int delimiterLength) : base(length, delimiterLength)
			{
				Offset = offset;
			}
		}

		public int Count {
			get { return delimiters.Count + 1; }
		}

		public IEnumerable<LineSegment> Lines {
			get { return GetLinesStartingAt (DocumentLocation.MinLine); }
		}

		public void Initalize (string text)
		{
			delimiters = new List<LineSplitter.Delimiter> (LineSplitter.FindDelimiter (text));
			textLength = text.Length;
		}

		public void Clear ()
		{
			delimiters.Clear ();
			textLength = 0;
		}

		public LineSegment Get (int number)
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
			return new PrimitiveLineSegment (startOffset, endOffset - startOffset, delimiterLength);
		}

		public LineSegment GetLineByOffset (int offset)
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

		public IEnumerable<LineSegment> GetLinesBetween (int startLine, int endLine)
		{
			for (int i = startLine; i <= endLine; i++)
				yield return Get (i);
		}

		public IEnumerable<LineSegment> GetLinesStartingAt (int startLine)
		{
			for (int i = startLine; i <= Count; i++)
				yield return Get (i);
		}

		public IEnumerable<LineSegment> GetLinesReverseStartingAt (int startLine)
		{
			for (int i = startLine; i-- > DocumentLocation.MinLine;)
				yield return Get (i);
		}

		public event EventHandler<LineEventArgs> LineChanged;
		public event EventHandler<LineEventArgs> LineInserted;
		public event EventHandler<LineEventArgs> LineRemoved;
	}
}
