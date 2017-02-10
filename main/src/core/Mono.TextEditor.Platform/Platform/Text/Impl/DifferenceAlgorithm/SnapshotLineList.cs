using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Text.Differencing.Implementation
{
    /// <summary>
    /// This class translates an <see cref="ITextSnapshot"/> and a set of line transforms
    /// into a list of lines that is suitable for diffing. The line transforms are evaluated
    /// as each line is requested, to minimize the extra memory required to translate every
    /// line of the snapshot.
    /// </summary>
    class SnapshotLineList : IList<string>, ITokenizedStringListInternal
    {
        SnapshotSpan _snapshotSpan;
        Span _lineSpan;
        Func<ITextSnapshotLine, string> _getLineTextCallback;
        StringDifferenceOptions _options;

        public SnapshotLineList(SnapshotSpan snapshotSpan, Func<ITextSnapshotLine, string> getLineTextCallback, StringDifferenceOptions options)
        {
            if (getLineTextCallback == null)
                throw new ArgumentNullException("getLineTextCallback");
            if ((options.DifferenceType & StringDifferenceTypes.Line) == 0)
                throw new InvalidOperationException("This collection can only be used for line differencing");

            _snapshotSpan = snapshotSpan;
            _getLineTextCallback = getLineTextCallback;
            _options = options;

            // Figure out the first and last line in the span
            var startLine = snapshotSpan.Start.GetContainingLine();
            int start = snapshotSpan.Start.GetContainingLine().LineNumber;

            //Perf hack to avoid calling GetContainingLine() if the lines are the same.
            SnapshotPoint endPoint = snapshotSpan.End;
            int end = ((endPoint.Position < startLine.EndIncludingLineBreak) ? start : endPoint.GetContainingLine().LineNumber) + 1;

            _lineSpan = Span.FromBounds(start, end);
        }

        public int Count
        {
            get { return _lineSpan.Length; }
        }

        public string this[int index]
        {
            get
            {
                SnapshotSpan lineSpan = GetSpanOfIndex(index);
                ITextSnapshotLine line = _snapshotSpan.Snapshot.GetLineFromLineNumber(_lineSpan.Start + index);

                bool isPartialLine = lineSpan.Length != line.LengthIncludingLineBreak;

                string text;
                if (isPartialLine)
                    text = lineSpan.GetText();
                else
                    text = _getLineTextCallback(line);

                if (_options.IgnoreTrimWhiteSpace)
                {
                    if (isPartialLine)
                    {
                        // For a partial line, only trim the sides that are included in the line.
                        // This may not be entirely exact (the partial line may still include what would have been
                        // leading whitespace), but we're ok with it for partial lines, which already don't use the
                        // provided _getLineTextCallback.
                        if (lineSpan.Start == line.Start)
                            text = text.TrimStart();
                        if (lineSpan.End == line.EndIncludingLineBreak)
                            text = text.TrimEnd();
                    }
                    else
                    {
                        text = text.Trim();
                    }
                }

                return text;
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        SnapshotSpan GetSpanOfIndex(int index)
        {
            if (index < 0 || index >= _lineSpan.Length)
                throw new ArgumentOutOfRangeException("index");

            ITextSnapshotLine line = _snapshotSpan.Snapshot.GetLineFromLineNumber(_lineSpan.Start + index);
            SnapshotSpan? lineSpan = line.ExtentIncludingLineBreak.Intersection(_snapshotSpan);
            if (!lineSpan.HasValue)
            {
                Debug.Fail("Unexpected - we have a line with no intersection.");
                return new SnapshotSpan(line.Start, 0);
            }

            return lineSpan.Value;
        }

        #region Not supported

        public int IndexOf(string item)
        {
            throw new NotSupportedException();
        }

        public void Insert(int index, string item)
        {
            throw new NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        public void Add(string item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(string item)
        {
            throw new NotSupportedException();
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            throw new NotSupportedException();
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool Remove(string item)
        {
            throw new NotSupportedException();
        }

        public IEnumerator<string> GetEnumerator()
        {
            for (int i = 0; i < Count ; i++)
            {
                yield return this[i];
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<string>)this).GetEnumerator();
        }

        #endregion


        public string Original
        {
            get
            {
                return _snapshotSpan.GetText();
            }
        }

        public string OriginalSubstring(int startIndex, int length)
        {
            return _snapshotSpan.Snapshot.GetText(_snapshotSpan.Start + startIndex, length);
        }

        public Span GetElementInOriginal(int index)
        {
            if (index == _lineSpan.Length)
            {
                return new Span(_snapshotSpan.End, 0);
            }
            else
            {
                // Get the span for the index, but make sure to offset by the _snapshotSpan,
                // so the coordinates are relative to Original (and not the snapshot itself)
                SnapshotSpan span = GetSpanOfIndex(index);
                return new Span(span.Start - _snapshotSpan.Start, span.Length);
            }
        }

        public Span GetSpanInOriginal(Span span)
        {
            int startPoint = GetElementInOriginal(span.Start).Start;

            if (span.IsEmpty)
                return new Span(startPoint, 0);

            int endPoint = GetElementInOriginal(span.End - 1).End;

            return Span.FromBounds(startPoint, endPoint);
        }
    }
}
