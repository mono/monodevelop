namespace Microsoft.VisualStudio.Text.Projection.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    using Microsoft.VisualStudio.Text.Implementation;
    using Microsoft.VisualStudio.Text.Utilities;

    using Strings = Microsoft.VisualStudio.Text.Implementation.Strings;

    internal class ElisionMap
    {
        private readonly ElisionMapNode root;
        private readonly int spanCount;

        #region Construction
        /// <summary>
        /// Construct an elision map given the size of the source buffer and the set of exposed spans.
        /// </summary>
        public ElisionMap(ITextSnapshot sourceSnapshot, NormalizedSpanCollection exposedSpans)
        {
            this.spanCount = exposedSpans.Count;
            if (exposedSpans.Count == 0)
            {
                // everything is hidden: one leaf node
                this.root = new ElisionMapNode(0, sourceSnapshot.Length, 0, sourceSnapshot.LineCount - 1, true);
            }
            else
            {
                // build a nicely balanced tree
                // calculate all the line numbers we'll need in advance
                int[] lineNumbers = new int[exposedSpans.Count * 2 + 1];
                for (int es = 0; es < exposedSpans.Count; ++es)
                {
                    lineNumbers[es * 2] = sourceSnapshot.GetLineNumberFromPosition(exposedSpans[es].Start);
                    lineNumbers[es * 2 + 1] = sourceSnapshot.GetLineNumberFromPosition(exposedSpans[es].End);
                }
                lineNumbers[exposedSpans.Count * 2] = sourceSnapshot.LineCount - 1;
                this.root = Build(new SnapshotSpan(sourceSnapshot, 0, sourceSnapshot.Length), exposedSpans, lineNumbers, new Span(0, exposedSpans.Count));
            }
            if (BufferGroup.Tracing)
            {
                root.Dump(0);
            }
        }

        private ElisionMap(ElisionMapNode root, int spanCount)
        {
            this.root = root;
            this.spanCount = spanCount;
        }

        /// <summary>
        /// Recursively build span tree.
        /// </summary>
        /// <param name="sourceSpan">SnapshotSpan over the source segment covered by this subtree, including both exposed and hidden text.</param>
        /// <param name="exposedSpans">Set of exposed spans for the entire buffer.</param>
        /// <param name="lineNumbers">Precomputed line numbers at all seams.</param>
        /// <param name="slice">The slice of exposed spans in this subtree.</param>
        /// <returns></returns>
        private ElisionMapNode Build(SnapshotSpan sourceSpan, NormalizedSpanCollection exposedSpans, int[] lineNumbers, Span slice)
        {
            int mid = slice.Start + (slice.Length / 2);
            Span midExposedSpan = exposedSpans[mid];

            Span leftSlice = Span.FromBounds(slice.Start, mid);
            ElisionMapNode left;
            Span leftSpan;
            if (leftSlice.Length > 0)
            {
                leftSpan = Span.FromBounds(sourceSpan.Start, midExposedSpan.Start);
                left = Build(new SnapshotSpan(sourceSpan.Snapshot, leftSpan), exposedSpans, lineNumbers, leftSlice);
                Debug.Assert(left.TotalSourceSize == leftSpan.Length);
            }
            else if (slice.Start == 0 && midExposedSpan.Start != 0)
            {
                Debug.Assert(sourceSpan.Start == 0);
                leftSpan = Span.FromBounds(0, midExposedSpan.Start);
                // the beginning of the buffer is elided. Do the special case of the first
                // node in the tree having an exposed size of zero.
                // TODO: figure this out in advance so we don't screw up the balance of the tree
                left = new ElisionMapNode(0, leftSpan.Length, 0, 
                                          TextUtilities.ScanForLineCount(sourceSpan.Snapshot.GetText(leftSpan)), 
                                          true);
            }
            else
            {
                leftSpan = new Span(midExposedSpan.Start, 0);
                left = null;
            }

            Span rightSlice = Span.FromBounds(mid + 1, slice.End);
            ElisionMapNode right;
            Span rightSpan;
            if (rightSlice.Length > 0)
            {
                rightSpan = Span.FromBounds(exposedSpans[mid + 1].Start, sourceSpan.End);
                right = Build(new SnapshotSpan(sourceSpan.Snapshot, rightSpan), exposedSpans, lineNumbers, rightSlice);
                Debug.Assert(right.TotalSourceSize == rightSpan.Length);
            }
            else
            {
                rightSpan = new Span(sourceSpan.End, 0);
                right = null;
            }

            Span midHiddenSpan = Span.FromBounds(midExposedSpan.End, rightSpan.Start);
            ITextSnapshot sourceSnapshot = sourceSpan.Snapshot;

            int startLineNumber = lineNumbers[2 * mid];
            int endExposedLineNumber = lineNumbers[2 * mid + 1];
            int endSourceLineNumber = lineNumbers[2 * mid + 2];

            int exposedLineBreakCount = endExposedLineNumber - startLineNumber;
            int hiddenLineBreakCount = endSourceLineNumber - endExposedLineNumber;

            return new ElisionMapNode(midExposedSpan.Length, 
                                      sourceSpan.Length - (leftSpan.Length + rightSpan.Length),
                                      exposedLineBreakCount,
                                      exposedLineBreakCount + hiddenLineBreakCount,
                                      left, 
                                      right,
                                      false);
        }
        #endregion

        public int Length
        {
            get { return this.root.TotalExposedSize; }
        }

        public int LineCount
        {
            get { return this.root.TotalExposedLineBreakCount + 1; }
        }

        public int SpanCount
        {
            get { return spanCount; }
        }

        public ElisionMap EditSpans(ITextSnapshot sourceSnapshot,
                                    NormalizedSpanCollection spansToElide,
                                    NormalizedSpanCollection spansToExpand, 
                                    out FrugalList<TextChange> textChanges)
        {
            textChanges = new FrugalList<TextChange>();
            NormalizedSpanCollection beforeSourceSpans = new NormalizedSnapshotSpanCollection(GetSourceSpans(sourceSnapshot, 0, this.spanCount));

            NormalizedSpanCollection afterElisionSourceSpans = NormalizedSpanCollection.Difference(beforeSourceSpans, spansToElide);
            NormalizedSpanCollection elisionChangeSpans = NormalizedSpanCollection.Difference(beforeSourceSpans, afterElisionSourceSpans);
            foreach (Span s in elisionChangeSpans)
            {
                textChanges.Add(new TextChange(this.root.MapFromSourceSnapshotToNearest(s.Start, 0),
                                               new ReferenceChangeString(new SnapshotSpan(sourceSnapshot, s)),
                                               ChangeString.EmptyChangeString,
                                               sourceSnapshot));
            }

            NormalizedSpanCollection afterExpansionSourceSpans = NormalizedSpanCollection.Union(afterElisionSourceSpans, spansToExpand);
            NormalizedSpanCollection expansionChangeSpans = NormalizedSpanCollection.Difference(afterExpansionSourceSpans, afterElisionSourceSpans);
            foreach (Span s in expansionChangeSpans)
            {
                textChanges.Add(new TextChange(this.root.MapFromSourceSnapshotToNearest(s.Start, 0),
                                               ChangeString.EmptyChangeString,
                                               new ReferenceChangeString(new SnapshotSpan(sourceSnapshot, s)),
                                               sourceSnapshot));
            }

            return textChanges.Count > 0 ? new ElisionMap(sourceSnapshot, afterExpansionSourceSpans) : this;
        }

        public IList<SnapshotSpan> GetSourceSpans(ITextSnapshot sourceSnapshot, int startSpanIndex, int count)
        {
            FrugalList<SnapshotSpan> result = new FrugalList<SnapshotSpan>();
            int rank = -1;
            int sourcePrefixSize = 0;
            this.root.GetSourceSpans(sourceSnapshot, ref rank, ref sourcePrefixSize, startSpanIndex, startSpanIndex + count, result);
            return result;
        }

        public SnapshotPoint MapToSourceSnapshot(ITextSnapshot sourceSnapshot, int position, PositionAffinity affinity)
        {
            return this.root.MapToSourceSnapshot(sourceSnapshot, position, 0, affinity);
        }

        public SnapshotPoint? MapFromSourceSnapshot(ITextSnapshot snapshot, int position)
        {
            // affinity is superfluous for elision buffers
            return this.root.MapFromSourceSnapshot(snapshot, position, 0);
        }

        public SnapshotPoint MapFromSourceSnapshotToNearest(ITextSnapshot snapshot, int position)
        {
            return new SnapshotPoint(snapshot, this.root.MapFromSourceSnapshotToNearest(position, 0));
        }

        public void MapToSourceSnapshots(IElisionSnapshot elisionSnapshot, Span span, FrugalList<SnapshotSpan> result)
        {
            if (span.Length == 0)
            {
                span = MapNullSpansToSourceSnapshots(elisionSnapshot, span, result);
            }
            else
            {
                this.root.MapToSourceSnapshots(elisionSnapshot.SourceSnapshot, span, 0, result);
            }

#if DEBUG
            int length = 0;
            foreach (SnapshotSpan ss in result)
            {
                length += ss.Length;
            }
            Debug.Assert(length == span.Length);
#endif
        }

        private Span MapNullSpansToSourceSnapshots(IElisionSnapshot elisionSnapshot, Span span, FrugalList<SnapshotSpan> result)
        {
            // TODO: this is identical to projection snapshot; can it be shared?
            FrugalList<SnapshotPoint> points = MapInsertionPointToSourceSnapshots(elisionSnapshot, span.Start);
            for (int p = 0; p < points.Count; ++p)
            {
                SnapshotPoint point = points[p];
                SnapshotSpan mappedSpan = new SnapshotSpan(point.Snapshot, point.Position, 0);
                if (result.Count == 0 || mappedSpan != result[result.Count - 1])
                {
                    result.Add(mappedSpan);
                }
            }
            return span;
        }

        public void MapToSourceSnapshotsInFillInMode(ITextSnapshot sourceSnapshot, Span span, FrugalList<SnapshotSpan> result)
        {
            SnapshotPoint? start;
            SnapshotPoint? end;
            if (span.Length == 0)
            {
                start = span.Start == 0 ? new SnapshotPoint(sourceSnapshot, 0) : this.root.MapToSourceSnapshot(sourceSnapshot, span.Start, 0, PositionAffinity.Predecessor);
                end = span.End == this.Length ? new SnapshotPoint(sourceSnapshot, sourceSnapshot.Length) : this.root.MapToSourceSnapshot(sourceSnapshot, span.End, 0, PositionAffinity.Successor);
            }
            else
            {
                start = this.root.MapToSourceSnapshot(sourceSnapshot, span.Start, 0, PositionAffinity.Successor);
                end = this.root.MapToSourceSnapshot(sourceSnapshot, span.End, 0, PositionAffinity.Predecessor);
            }
            Debug.Assert(start.HasValue);
            Debug.Assert(end.HasValue);
            result.Add(new SnapshotSpan(sourceSnapshot, Span.FromBounds(start.Value, end.Value)));
        }

        public void MapFromSourceSnapshot(Span span, FrugalList<Span> result)
        {
            if (span.Length == 0)
            {
                this.root.MapNullSpanFromSourceSnapshot(span, 0, result);
            }
            else
            {
                this.root.MapFromSourceSnapshot(span, 0, result);
            }
        }

        public FrugalList<SnapshotPoint> MapInsertionPointToSourceSnapshots(IElisionSnapshot elisionSnapshot, int exposedPosition)
        {
            FrugalList<SnapshotPoint> points = new FrugalList<SnapshotPoint>();
            this.root.MapInsertionPointToSourceSnapshots(elisionSnapshot, exposedPosition, 0, points);
            return points;
        }

        public LineSpan GetLineExtentFromLineNumber(int lineNumber, ITextSnapshot sourceSnapshot)
        {
            ProjectionLineInfo lineInfo = this.root.GetLineFromLineNumber(sourceSnapshot, lineNumber);
            return new LineSpan(lineInfo.lineNumber, Span.FromBounds(lineInfo.start, lineInfo.end), lineInfo.lineBreakLength);
        }

        public LineSpan GetLineExtentFromPosition(int position, ITextSnapshot sourceSnapshot)
        {
            ProjectionLineInfo lineInfo = this.root.GetLineFromPosition(sourceSnapshot, position, 0, 0, 0, 0, 0);
            return new LineSpan(lineInfo.lineNumber, Span.FromBounds(lineInfo.start, lineInfo.end), lineInfo.lineBreakLength);
        }

        public int GetLineNumberFromPosition(int position, ITextSnapshot sourceSnapshot)
        {
            return this.root.GetLineNumberFromPosition(sourceSnapshot, position, 0, 0);
        }

        public ElisionMap IncorporateChanges(INormalizedTextChangeCollection sourceChanges, 
                                             FrugalList<TextChange> projectedChanges, 
                                             ITextSnapshot beforeSourceSnapshot, 
                                             ITextSnapshot sourceSnapshot,
                                             ITextSnapshot beforeElisionSnapshot)
        {
            ElisionMapNode newRoot = this.root;
            int accumulatedProjectedDelta = 0;
            foreach (ITextChange sourceChange in sourceChanges)
            {
                int accumulatedDelete = 0;
                int incrementalAccumulatedProjectedDelta = 0;
                ChangeString newText;
                TextChange concreteSourceChange = sourceChange as TextChange;
                if (concreteSourceChange != null)
                {
                    newText = concreteSourceChange._newText;
                }
                else
                {
                    // handle mocks in tests
                    newText = new LiteralChangeString(sourceChange.NewText);
                }
                newRoot = newRoot.IncorporateChange(beforeSourceSnapshot      : beforeSourceSnapshot,
                                                    afterSourceSnapshot       : sourceSnapshot,
                                                    beforeElisionSnapshot     : beforeElisionSnapshot,
                                                    sourceInsertionPosition   : sourceChange.NewLength > 0 ? sourceChange.NewPosition : (int?)null,
                                                    newText                   : newText, 
                                                    sourceDeletionSpan        : new Span(sourceChange.NewPosition, sourceChange.OldLength), 
                                                    absoluteSourceOldPosition : sourceChange.OldPosition,
                                                    absoluteSourceNewPosition : sourceChange.NewPosition,
                                                    projectedPrefixSize       : 0, 
                                                    projectedChanges          : projectedChanges,
                                                    incomingAccumulatedDelta  : accumulatedProjectedDelta,
                                                    outgoingAccumulatedDelta  : ref incrementalAccumulatedProjectedDelta,
                                                    accumulatedDelete         : ref accumulatedDelete);
                accumulatedProjectedDelta += incrementalAccumulatedProjectedDelta;
            }
            if (newRoot.TotalSourceSize != sourceSnapshot.Length)
            {
                Debug.Fail(String.Format(System.Globalization.CultureInfo.InvariantCulture,
                                         "Change incorporation length inconsistency. Elision:{0} Source:{1}", newRoot.TotalSourceSize, sourceSnapshot.Length));
                throw new InvalidOperationException(Strings.InvalidLengthCalculation);
            }
            if (newRoot.TotalSourceLineBreakCount + 1 != sourceSnapshot.LineCount)
            {
                Debug.Fail(String.Format(System.Globalization.CultureInfo.InvariantCulture,
                                         "Change incorporation line count inconsistency. Elision:{0} Source:{1}", newRoot.TotalSourceLineBreakCount + 1, sourceSnapshot.LineCount));
                throw new InvalidOperationException(Strings.InvalidLineCountCalculation);
            }
            return new ElisionMap(newRoot, this.spanCount);
        }
    }
}
