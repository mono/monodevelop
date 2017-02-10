namespace Microsoft.VisualStudio.Text.Projection.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Text;

    using Microsoft.VisualStudio.Text.Implementation;

    internal abstract class BaseProjectionSnapshot : BaseSnapshot, IProjectionSnapshot2
    {
        #region State and Construction
        protected int totalLength = 0;
        protected int totalLineCount = 1;

        protected BaseProjectionSnapshot(ITextVersion version)
          : base(version)
        {
        }
        #endregion

        public new abstract IProjectionBufferBase TextBuffer { get; }

        #region Text Fetching
        public override int Length
        {
            get { return this.totalLength; }
        }

        public override string GetText(Span span)
        {
            if (span.End > this.totalLength)
            {
                throw new ArgumentOutOfRangeException("span");
            }

            IList<SnapshotSpan> copySourceSpans = MapToSourceSnapshotsForRead(span);
            if (copySourceSpans.Count == 1)
            {
                return copySourceSpans[0].GetText();
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                foreach (SnapshotSpan copySourceSpan in copySourceSpans)
                {
                    sb.Append(copySourceSpan.GetText());
                }
                return sb.ToString();
            }
        }

        public override char this[int position]
        {
            get 
            {
                SnapshotPoint? p = MapToSourceSnapshot(position, PositionAffinity.Successor);
                if (p.HasValue)
                {
                    return p.Value.GetChar();
                }
                else
                {
                    throw new ArgumentOutOfRangeException("position");
                }
            }
        }

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            if (destination == null)
            {
                throw new ArgumentNullException("destination");
            }
            if (sourceIndex < 0 || sourceIndex > this.totalLength)
            {
                throw new ArgumentOutOfRangeException("sourceIndex");
            }
            if (count < 0 || sourceIndex + count > this.totalLength || destinationIndex + count > destination.Length)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            if (count > 0)
            {
                IList<SnapshotSpan> copySourceSpans = MapToSourceSnapshotsForRead(new Span(sourceIndex, count));
                foreach (SnapshotSpan copySourceSpan in copySourceSpans)
                {
                    copySourceSpan.Snapshot.CopyTo(copySourceSpan.Start, destination, destinationIndex, copySourceSpan.Length);
                    destinationIndex += copySourceSpan.Length;
                }
            }
        }

        public override char[] ToCharArray(int startIndex, int length)
        {
            if (length < 0 || length > this.totalLength)
            {
                throw new ArgumentOutOfRangeException("length");
            }
            char[] destination = new char[length];
            CopyTo(startIndex, destination, 0, length);
            return destination;
        }
        #endregion

        #region Line Methods
        public override int LineCount
        {
            get { return this.totalLineCount; }
        }

        public override IEnumerable<ITextSnapshotLine> Lines
        {
            get
            {
                for (int line = 0; line < this.totalLineCount; ++line)
                {
                    yield return GetLineFromLineNumber(line);
                }
            }
        }
        #endregion

        #region Writing
        public override void Write(System.IO.TextWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            UncheckedWrite(writer, new Span(0, this.totalLength));
        }

        public override void Write(System.IO.TextWriter writer, Span span)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            if (span.End > this.totalLength)
            {
                throw new ArgumentOutOfRangeException("span");
            }
            UncheckedWrite(writer, span);
        }

        private void UncheckedWrite(System.IO.TextWriter writer, Span span)
        {
            IList<SnapshotSpan> writeSourceSpans = MapToSourceSnapshotsForRead(span);
            foreach (SnapshotSpan writeSourceSpan in writeSourceSpans)
            {
                writeSourceSpan.Snapshot.Write(writer, writeSourceSpan.Span);
            }
        }
        #endregion

        public ReadOnlyCollection<SnapshotPoint> MapToSourceSnapshots(int position)
        {
            return MapInsertionPointToSourceSnapshots(position, null);
        }

        #region Abstract Members
        /// <summary>
        /// Given the position of a pure insertion (not a replacement), return the list of source points at which the inserted text
        /// can be placed. This list has length greater than one only when the insertion point is on the seam of two or more source
        /// spans.
        /// </summary>
        /// <param name="position">The position of the insertion into the projection buffer.</param>
        /// <param name="excludedBuffer">Buffer to be ignored by virtue of its being a readonly literal buffer.</param>
        internal abstract ReadOnlyCollection<SnapshotPoint> MapInsertionPointToSourceSnapshots(int position, ITextBuffer excludedBuffer);

        /// <summary>
        /// Given the span of text to be deleted in a Replace operation, return the list of source spans to which it maps. Include any
        /// zero-length spans either on the boundaries or in the middle of the replacement span; the idea is both map to the deleted
        /// text and return the list of positions across which the inserted text can be placed.
        /// </summary>
        /// <param name="replacementSpan">The span of text to be replaced.</param>
        /// <param name="excludedBuffer">Buffer to be ignored by virtue of its being a readonly literal buffer; only zero-length spans are possible
        /// in this buffer.</param>
        internal abstract ReadOnlyCollection<SnapshotSpan> MapReplacementSpanToSourceSnapshots(Span replacementSpan, ITextBuffer excludedBuffer);

        public abstract int SpanCount { get; }
        public abstract ReadOnlyCollection<ITextSnapshot> SourceSnapshots { get; }
        public abstract ITextSnapshot GetMatchingSnapshot(ITextBuffer textBuffer);
        public abstract ITextSnapshot GetMatchingSnapshotInClosure(ITextBuffer targetBuffer);
        public abstract ITextSnapshot GetMatchingSnapshotInClosure(Predicate<ITextBuffer> match);
        public abstract ReadOnlyCollection<SnapshotSpan> GetSourceSpans(int startSpanIndex, int count);
        public abstract ReadOnlyCollection<SnapshotSpan> GetSourceSpans();

        public abstract SnapshotPoint MapToSourceSnapshot(int position);
        public abstract SnapshotPoint MapToSourceSnapshot(int position, PositionAffinity affinity);
        public abstract SnapshotPoint? MapFromSourceSnapshot(SnapshotPoint point, PositionAffinity affinity);
        public abstract ReadOnlyCollection<SnapshotSpan> MapToSourceSnapshots(Span span);
        public abstract ReadOnlyCollection<SnapshotSpan> MapToSourceSnapshotsForRead(Span span);
        public abstract ReadOnlyCollection<Span> MapFromSourceSnapshot(SnapshotSpan span);
        #endregion
    }
}
