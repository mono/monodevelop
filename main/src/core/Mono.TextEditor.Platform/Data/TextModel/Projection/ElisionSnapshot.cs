// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Projection.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;

    using Microsoft.VisualStudio.Text.Implementation;
    using Microsoft.VisualStudio.Text.Utilities;
    using Strings = Microsoft.VisualStudio.Text.Implementation.Strings;

    internal class ElisionSnapshot : BaseProjectionSnapshot, IProjectionSnapshot, IElisionSnapshot
    {
        #region State and Construction
        private readonly ElisionBuffer elisionBuffer;
        private readonly ITextSnapshot sourceSnapshot;
        private readonly ReadOnlyCollection<ITextSnapshot> sourceSnapshots;
        private readonly ElisionMap content;
        private readonly bool fillInMappingMode;

        public ElisionSnapshot(ElisionBuffer elisionBuffer,
                               ITextSnapshot sourceSnapshot, 
                               ITextVersion version, 
                               ElisionMap content, 
                               bool fillInMappingMode)
          : base(version)
        {
            this.elisionBuffer = elisionBuffer;
            this.sourceSnapshot = sourceSnapshot;
            // The SourceSnapshots property is used heavily, so cache a handy copy.
            this.sourceSnapshots = new ReadOnlyCollection<ITextSnapshot>(new FrugalList<ITextSnapshot>() { sourceSnapshot });
            this.totalLength = content.Length;
            this.content = content;
            this.totalLineCount = content.LineCount;
            this.fillInMappingMode = fillInMappingMode;
            Debug.Assert(this.totalLength == version.Length,
                         string.Format(System.Globalization.CultureInfo.CurrentCulture,
                                       "Elision Snapshot Inconsistency. Content: {0}, Previous + delta: {1}", this.totalLength, version.Length));
            if (this.totalLength != version.Length)
            {
                throw new InvalidOperationException(Strings.InvalidLengthCalculation);
            }
        }
        #endregion

        #region Buffers and Spans
        public override IProjectionBufferBase TextBuffer
        {
            get { return this.elisionBuffer; }
        }

        IElisionBuffer IElisionSnapshot.TextBuffer
        {
            get { return this.elisionBuffer; }
        }

        protected override ITextBuffer TextBufferHelper
        {
            get { return this.elisionBuffer; }
        }

        public override int SpanCount
        {
            get { return this.content.SpanCount; }
        }

        public override ReadOnlyCollection<ITextSnapshot> SourceSnapshots
        {
            get { return this.sourceSnapshots; }
        }

        public ITextSnapshot SourceSnapshot
        {
            get { return this.sourceSnapshot; }
        }

        public SnapshotPoint MapFromSourceSnapshotToNearest(SnapshotPoint point)
        {
            return this.content.MapFromSourceSnapshotToNearest(this, point.Position);
        }

        public override ITextSnapshot GetMatchingSnapshot(ITextBuffer textBuffer)
        {
            if (textBuffer == null)
            {
                throw new ArgumentNullException("textBuffer");
            }
            return this.sourceSnapshot.TextBuffer == textBuffer ? this.sourceSnapshot : null;
        }

        public override ITextSnapshot GetMatchingSnapshotInClosure(ITextBuffer textBuffer)
        {
            if (textBuffer == null)
            {
                throw new ArgumentNullException("textBuffer");
            }

            if (this.sourceSnapshot.TextBuffer == textBuffer)
            {
                return this.sourceSnapshot;
            }

            IProjectionSnapshot2 projSnap = this.sourceSnapshot as IProjectionSnapshot2;
            if (projSnap != null)
            {
                return projSnap.GetMatchingSnapshotInClosure(textBuffer);
            }

            return null;
        }

        public override ITextSnapshot GetMatchingSnapshotInClosure(Predicate<ITextBuffer> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException("match");
            }

            if (match(this.sourceSnapshot.TextBuffer))
            {
                return this.sourceSnapshot;
            }

            IProjectionSnapshot2 projSnap = this.sourceSnapshot as IProjectionSnapshot2;
            if (projSnap != null)
            {
                return projSnap.GetMatchingSnapshotInClosure(match);
            }

            return null;
        }

        public override ReadOnlyCollection<SnapshotSpan> GetSourceSpans(int startSpanIndex, int count)
        {
            if (startSpanIndex < 0)
            {
                throw new ArgumentOutOfRangeException("startSpanIndex");
            }
            if (count < 0 || startSpanIndex + count > SpanCount)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            return new ReadOnlyCollection<SnapshotSpan>(this.content.GetSourceSpans(this.sourceSnapshot, startSpanIndex, count));
        }

        public override ReadOnlyCollection<SnapshotSpan> GetSourceSpans()
        {
            return GetSourceSpans(0, this.content.SpanCount);
        }
        #endregion

        #region Line Methods
        public override ITextSnapshotLine GetLineFromLineNumber(int lineNumber)
        {
            if (lineNumber < 0 || lineNumber >= this.totalLineCount)
            {
                throw new ArgumentOutOfRangeException("lineNumber");
            }
            return new TextSnapshotLine(this, this.content.GetLineExtentFromLineNumber(lineNumber, this.sourceSnapshot));
        }

        public override ITextSnapshotLine GetLineFromPosition(int position)
        {
            if (position < 0 || position > this.totalLength)
            {
                throw new ArgumentOutOfRangeException("position");
            }
            return new TextSnapshotLine(this, this.content.GetLineExtentFromPosition(position, this.sourceSnapshot));
        }

        public override int GetLineNumberFromPosition(int position)
        {
            if (position < 0 || position > this.totalLength)
            {
                throw new ArgumentOutOfRangeException("position");
            }
            return this.content.GetLineNumberFromPosition(position, this.sourceSnapshot);
        }
        #endregion

        #region Mapping
        public override SnapshotPoint MapToSourceSnapshot(int position)
        {
            if (position < 0 || position > this.totalLength)
            {
                throw new ArgumentOutOfRangeException("position");
            }
            FrugalList<SnapshotPoint> points = this.content.MapInsertionPointToSourceSnapshots(this, position);
            if (points.Count == 1)
            {
                return points[0];
            }
            else if (this.elisionBuffer.resolver == null)
            {
                return points[points.Count - 1];
            }
            else
            {
                return points[this.elisionBuffer.resolver.GetTypicalInsertionPosition(new SnapshotPoint(this, position), new ReadOnlyCollection<SnapshotPoint>(points))];
            }
        }

        public override SnapshotPoint MapToSourceSnapshot(int position, PositionAffinity affinity)
        {
            if (position < 0 || position > this.totalLength)
            {
                throw new ArgumentOutOfRangeException("position");
            }
            if (affinity < PositionAffinity.Predecessor || affinity > PositionAffinity.Successor)
            {
                throw new ArgumentOutOfRangeException("affinity");
            }
            return this.content.MapToSourceSnapshot(this.sourceSnapshot, position, affinity);
        }

        public override SnapshotPoint? MapFromSourceSnapshot(SnapshotPoint point, PositionAffinity affinity)
        {
            if (point.Snapshot != this.sourceSnapshot)
            {
                throw new ArgumentException("The point does not belong to a source snapshot of the projection snapshot");
            }
            if (affinity < PositionAffinity.Predecessor || affinity > PositionAffinity.Successor)
            {
                throw new ArgumentOutOfRangeException("affinity");
            }
            return this.content.MapFromSourceSnapshot(this, point.Position);
        }

        private ReadOnlyCollection<SnapshotSpan> MapToSourceSnapshots(Span span, bool fillIn)
        {
            if (span.End > this.totalLength)
            {
                throw new ArgumentOutOfRangeException("span");
            }
            FrugalList<SnapshotSpan> result = new FrugalList<SnapshotSpan>();
            if (fillIn)
            {
                this.content.MapToSourceSnapshotsInFillInMode(this.sourceSnapshot, span, result);
            }
            else
            {
                this.content.MapToSourceSnapshots(this, span, result);
            }
            return new ReadOnlyCollection<SnapshotSpan>(result);
        }

        public override ReadOnlyCollection<SnapshotSpan> MapToSourceSnapshots(Span span)
        {
            return MapToSourceSnapshots(span, this.fillInMappingMode);
        }

        public override ReadOnlyCollection<SnapshotSpan> MapToSourceSnapshotsForRead(Span span)
        {
            return MapToSourceSnapshots(span, false);
        }

        public override ReadOnlyCollection<Span> MapFromSourceSnapshot(SnapshotSpan span)
        {
            if (span.Snapshot != this.sourceSnapshot)
            {
                throw new ArgumentException("The span does not belong to a source snapshot of the projection snapshot");
            }
            FrugalList<Span> result = new FrugalList<Span>();
            this.content.MapFromSourceSnapshot(span, result);
            return new ReadOnlyCollection<Span>(result);
        }

        internal override ReadOnlyCollection<SnapshotPoint> MapInsertionPointToSourceSnapshots(int position, ITextBuffer excludedBuffer)
        {
            return new ReadOnlyCollection<SnapshotPoint>(this.content.MapInsertionPointToSourceSnapshots(this, position));
        }

        internal override ReadOnlyCollection<SnapshotSpan> MapReplacementSpanToSourceSnapshots(Span replacementSpan, ITextBuffer excludedBuffer)
        {
            // this implementation won't return zero-length spans on the edges as it
            // should, but that's OK because it is not called in Beta1 (we never edit 
            // elision buffers directly). Third parties might do so, so we need a non-throwing
            // implementation here. Zero-length spans will be added for Beta2.
            return MapToSourceSnapshots(replacementSpan, false);
        }
        #endregion
    }
}
