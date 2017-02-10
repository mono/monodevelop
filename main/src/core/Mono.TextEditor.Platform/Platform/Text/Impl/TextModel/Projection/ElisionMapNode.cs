namespace Microsoft.VisualStudio.Text.Projection.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Microsoft.VisualStudio.Text.Implementation;
    using Microsoft.VisualStudio.Text.Utilities;

    internal class ElisionMapNode
    {
        #region State
        private readonly bool leftmostElision;      // it would be better to avoid burning this space in every node

        private readonly int exposedSize;
        private readonly int sourceSize;
        private readonly int exposedLineBreakCount;
        private readonly int sourceLineBreakCount;

        private readonly int totalExposedSize;
        private readonly int totalSourceSize;
        private readonly int totalExposedLineBreakCount;
        private readonly int totalSourceLineBreakCount;

        private readonly ElisionMapNode left;
        private readonly ElisionMapNode right;
        #endregion

        #region Debugging Support
        public override string ToString()
        {
            return String.Format(System.Globalization.CultureInfo.InvariantCulture,
                                 "Exp:{0} Src:{1} TExp:{2} Tsrc:{3}", this.exposedSize, this.sourceSize, this.totalExposedSize, this.totalSourceSize);
        }
        #endregion

        #region Constructors
        // leaf constructor
        public ElisionMapNode(int exposedSize, int sourceSize, int exposedLineBreakCount, int sourceLineBreakCount, bool leftmostElision)
        {
            this.exposedSize = exposedSize;
            this.sourceSize = sourceSize;
            this.exposedLineBreakCount = exposedLineBreakCount;
            this.sourceLineBreakCount = sourceLineBreakCount;

            this.totalExposedSize = exposedSize;
            this.totalSourceSize = sourceSize;
            this.totalExposedLineBreakCount = exposedLineBreakCount;
            this.totalSourceLineBreakCount = sourceLineBreakCount;

            this.leftmostElision = leftmostElision;
        }

        // internal node constructor
        public ElisionMapNode(int exposedSize, int sourceSize, int exposedLineBreakCount, int sourceLineBreakCount, ElisionMapNode left, ElisionMapNode right, bool leftmostElision)
        {
            this.exposedSize = exposedSize;
            this.sourceSize = sourceSize;
            this.exposedLineBreakCount = exposedLineBreakCount;
            this.sourceLineBreakCount = sourceLineBreakCount;
            this.left = left;
            this.right = right;

            this.totalExposedSize = LeftTotalExposedSize() + exposedSize + RightTotalExposedSize();
            this.totalSourceSize = LeftTotalSourceSize() + sourceSize + RightTotalSourceSize();
            this.totalExposedLineBreakCount = LeftTotalExposedLineBreakCount() + exposedLineBreakCount + RightTotalExposedLineBreakCount();
            this.totalSourceLineBreakCount = LeftTotalSourceLineBreakCount() + sourceLineBreakCount + RightTotalSourceLineBreakCount();

            this.leftmostElision = leftmostElision;
        }
        #endregion

        #region Public Properties
        public int TotalExposedSize
        {
            get { return this.totalExposedSize; }
        }

        public int TotalSourceSize
        {
            get { return this.totalSourceSize; }
        }

        public int TotalExposedLineBreakCount
        {
            get { return this.totalExposedLineBreakCount; }
        }

        public int TotalSourceLineBreakCount
        {
            get { return this.totalSourceLineBreakCount; }
        }
        #endregion

        #region Helpers
        private int LeftTotalSourceSize()
        {
            return this.left == null ? 0 : this.left.totalSourceSize;
        }

        private int LeftTotalExposedSize()
        {
            return this.left == null ? 0 : this.left.totalExposedSize;
        }

        private int LeftTotalHiddenSize()
        {
            return this.left == null ? 0 : this.left.totalSourceSize - this.left.totalExposedSize;
        }

        private int LeftTotalExposedLineBreakCount()
        {
            return this.left == null ? 0 : this.left.totalExposedLineBreakCount;
        }

        private int LeftTotalSourceLineBreakCount()
        {
            return this.left == null ? 0 : this.left.totalSourceLineBreakCount;
        }

        private int LeftTotalHiddenLineBreakCount()
        {
            return this.left == null ? 0 : this.left.totalSourceLineBreakCount - this.left.totalExposedLineBreakCount;
        }

        private int RightTotalSourceSize()
        {
            return this.right == null ? 0 : this.right.totalSourceSize;
        }

        private int RightTotalExposedSize()
        {
            return this.right == null ? 0 : this.right.totalExposedSize;
        }

        private int RightTotalExposedLineBreakCount()
        {
            return this.right == null ? 0 : this.right.totalExposedLineBreakCount;
        }

        private int RightTotalSourceLineBreakCount()
        {
            return this.right == null ? 0 : this.right.totalSourceLineBreakCount;
        }

        public void Dump(int level)
        {
            if (this.left != null)
            {
                this.left.Dump(level + 1);
            }
            Debug.Write(String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}{1},{2}({3},{4})\n", new string(' ', level * 3), this.exposedSize, this.sourceSize, this.totalExposedSize, this.totalSourceSize));
            if (this.right != null)
            {
                this.right.Dump(level + 1);
            }
        }
        #endregion

        #region Source Spans
        public void GetSourceSpans(ITextSnapshot sourceSnapshot, ref int rank, ref int sourcePrefixSize, int startSpanIndex, int endSpanIndex, IList<SnapshotSpan> result)
        {
            // We don't store explicit rank information in the nodes, so we walk the tree in inorder until we
            // get to the starting position and continue until we have what we need.
            if (this.left != null)
            {
                this.left.GetSourceSpans(sourceSnapshot, ref rank, ref sourcePrefixSize, startSpanIndex, endSpanIndex, result);
            }

            if (!this.leftmostElision)
            {
                // don't count the possible special case node at the beginning;
                rank++;
            }
            if (rank >= startSpanIndex && rank < endSpanIndex)
            {
                result.Add(new SnapshotSpan(sourceSnapshot, sourcePrefixSize, this.exposedSize));
            }
            sourcePrefixSize += this.sourceSize;

            if (this.right != null)
            {
                this.right.GetSourceSpans(sourceSnapshot, ref rank, ref sourcePrefixSize, startSpanIndex, endSpanIndex, result);
            }
        }
        #endregion

        #region Mapping
        public SnapshotPoint MapToSourceSnapshot(ITextSnapshot sourceSnapshot, int exposedPosition, int sourcePrefixLength, PositionAffinity affinity)
        {
            Debug.Assert(exposedPosition >= 0);
            Debug.Assert(exposedPosition <= this.totalExposedSize);

            int leftTotalExposedSize = LeftTotalExposedSize();

            if (((affinity == PositionAffinity.Predecessor) && (exposedPosition <= leftTotalExposedSize) && left != null && !left.leftmostElision) ||
                ((affinity == PositionAffinity.Successor) && (exposedPosition < leftTotalExposedSize)))
            {
                return this.left.MapToSourceSnapshot(sourceSnapshot     : sourceSnapshot,
                                                     exposedPosition    : exposedPosition,
                                                     sourcePrefixLength : sourcePrefixLength,
                                                     affinity           : affinity);
            }

            else if (((affinity == PositionAffinity.Predecessor) && (exposedPosition <= leftTotalExposedSize + this.exposedSize)) ||
                     ((affinity == PositionAffinity.Successor) && (exposedPosition < leftTotalExposedSize + this.exposedSize)))
            {
                return new SnapshotPoint(sourceSnapshot,
                                         sourcePrefixLength + LeftTotalSourceSize() + (exposedPosition - leftTotalExposedSize));
            }

            else if (right == null)
            {
                // we are mapping the last exposedPosition in the buffer, that is, the one that is just past the end.
                // ignore the affinity.
                Debug.Assert(exposedPosition == leftTotalExposedSize + this.exposedSize);
                return new SnapshotPoint(sourceSnapshot, sourcePrefixLength + LeftTotalSourceSize() + this.exposedSize);
            }

            else
            {
                return this.right.MapToSourceSnapshot(sourceSnapshot     : sourceSnapshot,
                                                      exposedPosition    : exposedPosition - (leftTotalExposedSize + this.exposedSize),
                                                      sourcePrefixLength : sourcePrefixLength + LeftTotalSourceSize() + this.sourceSize,
                                                      affinity           : affinity);
            }
        }

        public SnapshotPoint? MapFromSourceSnapshot(ITextSnapshot snapshot, int sourcePosition, int exposedPrefixLength)
        {
            Debug.Assert(sourcePosition <= this.totalSourceSize);
            int leftTotalSourceSize = LeftTotalSourceSize();

            if (sourcePosition < leftTotalSourceSize)
            {
                if (left == null)
                {
                    return null;
                }
                else
                {
                    return this.left.MapFromSourceSnapshot(snapshot, sourcePosition, exposedPrefixLength);
                }
            }
            else if (sourcePosition < leftTotalSourceSize + this.sourceSize)
            {
                if ((sourcePosition <= leftTotalSourceSize + this.exposedSize) && (!this.leftmostElision))
                {
                    return new SnapshotPoint(snapshot,
                                             exposedPrefixLength + LeftTotalExposedSize() + (sourcePosition - leftTotalSourceSize));
                }
                else
                {
                    return null;
                }
            }
            else if (right == null)
            {
                // We are mapping the last position in the source snapshot
                if (this.exposedSize < this.sourceSize)
                {
                    return null;
                }
                else
                {
                    return new SnapshotPoint(snapshot, exposedPrefixLength + LeftTotalExposedSize() + this.exposedSize);
                }
            }
            else
            {
                return this.right.MapFromSourceSnapshot(snapshot,
                                                        sourcePosition - (leftTotalSourceSize + this.sourceSize),
                                                        exposedPrefixLength + LeftTotalExposedSize() + this.exposedSize);
            }
        }

        public int MapFromSourceSnapshotToNearest(int sourcePosition, int exposedPrefixLength)
        {
            Debug.Assert(sourcePosition <= this.totalSourceSize);

            if (sourcePosition < LeftTotalSourceSize())
            {
                return this.left.MapFromSourceSnapshotToNearest(sourcePosition, exposedPrefixLength);
            }
            else
            {
                // shift coordinates to start with current node
                exposedPrefixLength += LeftTotalExposedSize();
                sourcePosition -= LeftTotalSourceSize();
                if (sourcePosition < this.sourceSize)
                {
                    if (sourcePosition < this.exposedSize)
                    {
                        // position is in the exposed segment
                        return exposedPrefixLength + sourcePosition;
                    }
                    else
                    {
                        // position is in the hidden segment
                        return exposedPrefixLength + this.exposedSize;
                    }
                }
                else if (right == null)
                {
                    // We are mapping the last position in the source snapshot
                    return exposedPrefixLength + this.exposedSize;
                }
                else
                {
                    // shift coordinates to right subtree
                    exposedPrefixLength += this.exposedSize;
                    sourcePosition -= this.sourceSize;
                    return this.right.MapFromSourceSnapshotToNearest(sourcePosition, exposedPrefixLength);
                }
            }
        }

        /// <summary>
        /// Map a span over the exposed text to spans on the source buffer.
        /// </summary>
        /// <param name="sourceSnapshot">Snapshot of the source buffer to which to map.</param>
        /// <param name="mapSpan">The span to map, expressed in terms of exposed text.</param>
        /// <param name="sourcePrefixSize">The amount of source text (hidden or exposed) that precedes the text in this subtree.</param>
        /// <param name="result">List of spans to which to append the result, expressed in terms of source text.</param>
        public void MapToSourceSnapshots(ITextSnapshot sourceSnapshot,
                                         Span mapSpan,
                                         int sourcePrefixSize,
                                         FrugalList<SnapshotSpan> result)
        {
            // This method is written for maximum clarity

            int leftTotalExposedSize = LeftTotalExposedSize();
            Span leftSpan = new Span(0, leftTotalExposedSize);
            Span midSpan = new Span(leftTotalExposedSize, this.exposedSize);
            Span rightSpan = new Span(leftTotalExposedSize + this.exposedSize, RightTotalExposedSize());

            // map the requested span to the left subtree, this node, and the right subtree, in terms of exposed text
            Span? leftMapSpan = mapSpan.Overlap(leftSpan);
            Span? midMapSpan = mapSpan.Overlap(midSpan);
            Span? rightMapSpan = mapSpan.Overlap(rightSpan);

            if (leftMapSpan != null)
            {
                this.left.MapToSourceSnapshots(sourceSnapshot,
                                               leftMapSpan.Value,
                                               sourcePrefixSize,
                                               result);
            }

            if (midMapSpan != null)
            {
                // the source span for the current node has the same length as the midMapSpan, but the start is
                // adjusted by the total amount of source text to the left of this node, which is the sourcePrefixSize plus
                // the amount of text hidden in the left subtree.
                Span sourceSpan = new Span(midMapSpan.Value.Start + sourcePrefixSize + LeftTotalHiddenSize(), midMapSpan.Value.Length);
                result.Add(new SnapshotSpan(sourceSnapshot, sourceSpan));
            }

            if (rightMapSpan != null)
            {
                this.right.MapToSourceSnapshots(sourceSnapshot,
                                                new Span(rightMapSpan.Value.Start - leftTotalExposedSize - this.exposedSize, rightMapSpan.Value.Length),
                                                sourcePrefixSize + LeftTotalSourceSize() + this.sourceSize,
                                                result);
            }
        }

        public void MapFromSourceSnapshot(Span mapSpan, int exposedPrefixSize, FrugalList<Span> result)
        {
            int leftTotalSourceSize = LeftTotalSourceSize();
            Span leftSourceSpan = new Span(0, leftTotalSourceSize);
            Span midExposedSpan = new Span(leftTotalSourceSize, this.exposedSize);
            Span rightSourceSpan = new Span(leftTotalSourceSize + this.sourceSize, RightTotalSourceSize());

            // map the requested span to the left subtree, this node, and the right subtree, in terms of source text
            Span? leftMapSpan = mapSpan.Overlap(leftSourceSpan);
            Span? midMapSpan = mapSpan.Overlap(midExposedSpan);
            Span? rightMapSpan = mapSpan.Overlap(rightSourceSpan);

            if (leftMapSpan != null)
            {
                this.left.MapFromSourceSnapshot(leftMapSpan.Value, exposedPrefixSize, result);
            }

            if (midMapSpan != null)
            {
                result.Add(new Span(midMapSpan.Value.Start + exposedPrefixSize - LeftTotalHiddenSize(), midMapSpan.Value.Length));
            }

            if (rightMapSpan != null)
            {
                this.right.MapFromSourceSnapshot(new Span(rightMapSpan.Value.Start - leftTotalSourceSize - this.sourceSize, rightMapSpan.Value.Length),
                                                 exposedPrefixSize + LeftTotalExposedSize() + this.exposedSize,
                                                 result);
            }
        }

        public void MapNullSpanFromSourceSnapshot(Span nullSourceSpan, int exposedPrefixSize, FrugalList<Span> result)
        {
            int leftTotalSourceSize = LeftTotalSourceSize();
            Span midExposedSpan = new Span(leftTotalSourceSize, this.exposedSize);

            if (this.left != null)
            {
                Span leftSourceSpan = new Span(0, leftTotalSourceSize);
                if (leftSourceSpan.IntersectsWith(nullSourceSpan))
                {
                    this.left.MapNullSpanFromSourceSnapshot(nullSourceSpan, exposedPrefixSize, result);
                }
            }

            if (!this.leftmostElision && midExposedSpan.IntersectsWith(nullSourceSpan))
            {
                Span intersection = midExposedSpan.Intersection(nullSourceSpan).Value;
                result.Add(new Span(intersection.Start + exposedPrefixSize - LeftTotalHiddenSize(), 0));
            }

            if (this.right != null)
            {
                Span rightSourceSpan = new Span(leftTotalSourceSize + this.sourceSize, RightTotalSourceSize());
                if (rightSourceSpan.IntersectsWith(nullSourceSpan))
                {
                    Span intersection = rightSourceSpan.Intersection(nullSourceSpan).Value;
                    this.right.MapNullSpanFromSourceSnapshot(new Span(intersection.Start - leftTotalSourceSize - this.sourceSize, 0),
                                                             exposedPrefixSize + LeftTotalExposedSize() + this.exposedSize,
                                                             result);
                }
            }
        }

        public void MapInsertionPointToSourceSnapshots(IElisionSnapshot elisionSnapshot,
                                                       int exposedPosition,
                                                       int sourcePrefixLength,
                                                       FrugalList<SnapshotPoint> points)
        {
            Debug.Assert(exposedPosition >= 0);
            Debug.Assert(exposedPosition <= this.totalExposedSize);

            int leftTotalExposedSize = LeftTotalExposedSize();

            if (this.left != null && exposedPosition <= leftTotalExposedSize)
            {
                this.left.MapInsertionPointToSourceSnapshots(elisionSnapshot,
                                                             exposedPosition,
                                                             sourcePrefixLength,
                                                             points);
            }

            bool ignoreThisNode = this.leftmostElision && elisionSnapshot.Length > 0;

            if (!ignoreThisNode)
            {
                if (exposedPosition >= leftTotalExposedSize && exposedPosition <= leftTotalExposedSize + this.exposedSize)
                {
                    points.Add(new SnapshotPoint(elisionSnapshot.SourceSnapshot, sourcePrefixLength + LeftTotalSourceSize() + (exposedPosition - leftTotalExposedSize)));
                }
            }

            if (this.right != null && exposedPosition >= leftTotalExposedSize + this.exposedSize)
            {
                this.right.MapInsertionPointToSourceSnapshots(elisionSnapshot,
                                                              exposedPosition - (LeftTotalExposedSize() + this.exposedSize),
                                                              sourcePrefixLength + LeftTotalSourceSize() + this.sourceSize,
                                                              points);
            }
        }
        #endregion

        #region Lines

        //private void TraceEnter(int level)
        //{
        //    Debug.Write(new string(' ', level * 3));
        //    Debug.Write(this.exposedSize);
        //    Debug.Write(' ');
        //    Debug.WriteLine(this.sourceSize);
        //}

        //private static void TraceExit(int level, LineInfo info)
        //{
        //    Debug.Write(new string(' ', level * 3));
        //    Debug.WriteLine(info.ToString());
        //}

        public ProjectionLineInfo GetLineFromPosition(ITextSnapshot sourceSnapshot,
                                            int exposedPosition,
                                            int sourcePrefixLineBreakCount,
                                            int hiddenPrefixLineBreakCount,
                                            int sourcePrefixSize,
                                            int exposedPrefixSize,
                                            int level)
        {
            //TraceEnter(level);
            Span relativeExposedSpan = new Span(LeftTotalExposedSize(), this.exposedSize);
            ProjectionLineCalculationState state = ProjectionLineCalculationState.Primary;
            int relativeExposedPosition = exposedPosition;
            ProjectionLineInfo pendingInfo = new ProjectionLineInfo();

            do
            {
                if (relativeExposedPosition < relativeExposedSpan.Start)
                {
                    #region relative ExposedPosition is in left subtree
                    Debug.Assert(state != ProjectionLineCalculationState.Append);

                    // recursively compute that part of the line that is in the left subtree
                    ProjectionLineInfo leftInfo = this.left.GetLineFromPosition
                                            (sourceSnapshot             : sourceSnapshot,
                                             exposedPosition            : relativeExposedPosition,
                                             sourcePrefixLineBreakCount : sourcePrefixLineBreakCount,
                                             hiddenPrefixLineBreakCount : hiddenPrefixLineBreakCount,
                                             sourcePrefixSize           : sourcePrefixSize,
                                             exposedPrefixSize          : exposedPrefixSize,
                                             level                      : level + 1);

                    if (state == ProjectionLineCalculationState.Primary)
                    {
                        if (leftInfo.endComplete)
                        {
                            // leftInfo.startComplete may be false, but we aren't going to find anything 
                            // further to the left at this level of the tree, so we are done here
                            // TraceExit(level, leftInfo);
                            return leftInfo;
                        }
                        else
                        {
                            // the end of the line extends into the current node. our new position
                            // of interest is the start of the exposed text in this node.
                            state = ProjectionLineCalculationState.Append;
                            pendingInfo = leftInfo;
                            relativeExposedPosition = relativeExposedSpan.Start;
                            continue;
                        }
                    }
                    else
                    {
                        // We've just been looking for the beginning of the line in the left subtree
                        Debug.Assert(state == ProjectionLineCalculationState.Prepend || state == ProjectionLineCalculationState.Bipend);
                        if (pendingInfo.lineNumber == leftInfo.lineNumber)
                        {
                            // the left node contained more of the line we are looking for
                            // we may or may not have found the start of the line, but there 
                            // is no more to find at this level
                            pendingInfo.start = leftInfo.start;
                            pendingInfo.startComplete = leftInfo.startComplete;
                        }
                        else
                        {
                            // the left exposed source ended with a line break, so
                            // there is no change to the previously computed start
                            pendingInfo.startComplete = true;
                        }
                        if (state == ProjectionLineCalculationState.Bipend)
                        {
                            // now we need to look in the right subtree
                            state = ProjectionLineCalculationState.Append;
                            relativeExposedPosition = relativeExposedSpan.End;
                            continue;
                        }
                        else
                        {
                            // TraceExit(level, pendingInfo);
                            return pendingInfo;
                        }
                    }
                    #endregion
                }
                else if (relativeExposedPosition < relativeExposedSpan.End || this.right == null)
                {
                    #region relative ExposedPosition is in current node
                    int absoluteSourcePosition = sourcePrefixSize + LeftTotalHiddenSize() + relativeExposedPosition;
                    ITextSnapshotLine sourceLine = sourceSnapshot.GetLineFromPosition(absoluteSourcePosition);
                    ProjectionLineCalculationState nextState = ProjectionLineCalculationState.Primary;
                    int provisionalLineNumber = sourceLine.LineNumber - (LeftTotalHiddenLineBreakCount() + hiddenPrefixLineBreakCount);

                    if (state == ProjectionLineCalculationState.Primary)
                    {
                        pendingInfo = new ProjectionLineInfo();

                        // the primary position we are searching for is in the current node.
                        // now we know the line number!
                        pendingInfo.lineNumber = provisionalLineNumber;
                        Debug.Assert(pendingInfo.lineNumber >= 0);
                    }

                    // compute the length of the portion of the source line that precedes the position we've been searching for
                    int sourceLeader = absoluteSourcePosition - sourceLine.Start;
                    // where would that map to in the current node?
                    int relativeExposedLineStart = relativeExposedPosition - sourceLeader;

                    if (state == ProjectionLineCalculationState.Prepend && provisionalLineNumber < pendingInfo.lineNumber)
                    {
                        // we were trying to pick up the beginning of a line that had been elided, but it was
                        // elided all the way to its beginning, so we are done.
                        pendingInfo.startComplete = true;
                    }
                    else if (state == ProjectionLineCalculationState.Primary || state == ProjectionLineCalculationState.Prepend)
                    {
                        // if we are lucky, the whole line will be contained in this node

                        if (relativeExposedLineStart > relativeExposedSpan.Start)
                        {
                            // we are sure that nothing to our left is of interest
                            pendingInfo.start = exposedPrefixSize + relativeExposedLineStart;
                            pendingInfo.startComplete = true;
                        }
                        else
                        {
                            // rats! part of the 'leader' is elided. 
                            // start with the beginning of this segment
                            pendingInfo.start = exposedPrefixSize + relativeExposedSpan.Start;
                            pendingInfo.startComplete = false;
                            // and check further to the left if there is anything there
                            if (LeftTotalExposedSize() > 0)
                            {
                                nextState = ProjectionLineCalculationState.Prepend;
                                relativeExposedPosition = relativeExposedSpan.Start - 1;
                            }
                        }
                    }

                    if (state == ProjectionLineCalculationState.Primary || state == ProjectionLineCalculationState.Append)
                    {
                        int exposedLineEnd = relativeExposedLineStart + sourceLine.LengthIncludingLineBreak;
                        if (exposedLineEnd <= relativeExposedSpan.End)
                        {
                            // good!
                            pendingInfo.end = exposedLineEnd + exposedPrefixSize - sourceLine.LineBreakLength;
                            pendingInfo.endComplete = true;
                            pendingInfo.lineBreakLength = sourceLine.LineBreakLength;
                        }
                        else
                        {
                            pendingInfo.end = exposedPrefixSize + relativeExposedSpan.End;
                            pendingInfo.endComplete = false;
                            if (this.right == null)
                            {
                                if (nextState != ProjectionLineCalculationState.Prepend)
                                {
                                    // we need to go further right but there is nothing more below us
                                    // TraceExit(level, pendingInfo);
                                    return pendingInfo;
                                }
                            }
                            else
                            {
                                if (nextState == ProjectionLineCalculationState.Prepend)
                                {
                                    nextState = ProjectionLineCalculationState.Bipend;
                                }
                                else
                                {
                                    nextState = ProjectionLineCalculationState.Append;
                                    relativeExposedPosition = relativeExposedSpan.End;
                                }
                            }
                        }
                    }
                    if (nextState == ProjectionLineCalculationState.Primary)
                    {
                        // TraceExit(level, pendingInfo);
                        return pendingInfo;
                    }
                    state = nextState;
                    #endregion
                }
                else
                {
                    #region relative ExposedPosition is in right subtree
                    Debug.Assert(state != ProjectionLineCalculationState.Bipend);

                    // recursively compute that part of the line that is in the right subtree
                    ProjectionLineInfo rightInfo = this.right.GetLineFromPosition
                                                (sourceSnapshot             : sourceSnapshot,
                                                 exposedPosition            : relativeExposedPosition - (LeftTotalExposedSize() + this.exposedSize),
                                                 sourcePrefixLineBreakCount : sourcePrefixLineBreakCount + LeftTotalSourceLineBreakCount() + this.sourceLineBreakCount,
                                                 hiddenPrefixLineBreakCount : hiddenPrefixLineBreakCount + LeftTotalHiddenLineBreakCount() + (this.sourceLineBreakCount - this.exposedLineBreakCount),
                                                 sourcePrefixSize           : sourcePrefixSize + LeftTotalSourceSize() + this.sourceSize,
                                                 exposedPrefixSize          : exposedPrefixSize + LeftTotalExposedSize() + this.exposedSize,
                                                 level                      : level + 1);
                    if (state == ProjectionLineCalculationState.Primary)
                    {
                        if (rightInfo.startComplete)
                        {
                            // rightInfo.endComplete may be false, but we aren't going to find anything
                            // further to the right at this level of three, so we are done here
                            // TraceExit(level, rightInfo);
                            return rightInfo;
                        }
                        else
                        {
                            // the begnning of the line extends into the current node. our new position
                            // of interest is the end of the exposed text in this node.
                            state = ProjectionLineCalculationState.Prepend;
                            pendingInfo = rightInfo;
                            relativeExposedPosition = relativeExposedSpan.End - 1;
                            continue;
                        }
                    }
                    else
                    {
                        // We've just been looking for the end of the line in the right subtree
                        Debug.Assert(state == ProjectionLineCalculationState.Append);

                        // the first line we saw in the right subtree must have been the same line
                        // since there wasn't a line break at the end of the current node
                        Debug.Assert(pendingInfo.lineNumber == rightInfo.lineNumber);

                        pendingInfo.end = rightInfo.end;
                        pendingInfo.endComplete = rightInfo.endComplete;
                        pendingInfo.lineBreakLength = rightInfo.lineBreakLength;
                        // TraceExit(level, pendingInfo);
                        return pendingInfo;
                    }
                    #endregion
                }
            } while (relativeExposedPosition >= 0 && relativeExposedPosition <= this.totalExposedSize);

            // TraceExit(level, pendingInfo);
            return pendingInfo;
        }

        public ProjectionLineInfo GetLineFromLineNumber(ITextSnapshot sourceSnapshot,
                                              int exposedLineNumber)
        {
            int pos = GetPositionFromLineNumber(sourceSnapshot, exposedLineNumber, 0, 0);
            ProjectionLineInfo info = this.GetLineFromPosition(sourceSnapshot, pos, 0, 0, 0, 0, 0);
            Debug.Assert(info.lineNumber == exposedLineNumber);
            return info;
        }

        private int GetPositionFromLineNumber(ITextSnapshot sourceSnapshot,
                                              int relativeExposedLineNumber,
                                              int sourcePrefixLineBreakCount,
                                              int sourcePrefixHiddenSize)

            // return the absolute (exposed) position of the (first) line break character in the requested line
        {
            if (relativeExposedLineNumber < LeftTotalExposedLineBreakCount())
            {
                return this.left.GetPositionFromLineNumber
                                    (sourceSnapshot             : sourceSnapshot,
                                     relativeExposedLineNumber  : relativeExposedLineNumber,
                                     sourcePrefixLineBreakCount : sourcePrefixLineBreakCount,
                                     sourcePrefixHiddenSize     : sourcePrefixHiddenSize);
            }

            else if (relativeExposedLineNumber < LeftTotalExposedLineBreakCount() + this.exposedLineBreakCount || this.right == null)
            {
                int absoluteSourceLineNumber = sourcePrefixLineBreakCount + LeftTotalHiddenLineBreakCount() + relativeExposedLineNumber;
                ITextSnapshotLine sourceLine = sourceSnapshot.GetLineFromLineNumber(absoluteSourceLineNumber);
                return sourceLine.End - sourcePrefixHiddenSize - LeftTotalHiddenSize();
            }

            else
            {
                return this.right.GetPositionFromLineNumber
                                    (sourceSnapshot             : sourceSnapshot,
                                     relativeExposedLineNumber  : relativeExposedLineNumber - (LeftTotalExposedLineBreakCount() + this.exposedLineBreakCount),
                                     sourcePrefixLineBreakCount : sourcePrefixLineBreakCount + LeftTotalSourceLineBreakCount() + this.sourceLineBreakCount,
                                     sourcePrefixHiddenSize     : sourcePrefixHiddenSize + LeftTotalHiddenSize() + (this.sourceSize - this.exposedSize));
            }
        }

        public int GetLineNumberFromPosition(ITextSnapshot sourceSnapshot, int exposedPosition, int hiddenPrefixLineBreakCount, int sourcePrefixSize)
        {
            Span midSpan = new Span(LeftTotalExposedSize(), this.exposedSize);

            if (exposedPosition < midSpan.Start)
            {
                return this.left.GetLineNumberFromPosition
                                    (sourceSnapshot             : sourceSnapshot,
                                     exposedPosition            : exposedPosition,
                                     hiddenPrefixLineBreakCount : hiddenPrefixLineBreakCount,
                                     sourcePrefixSize           : sourcePrefixSize);
            }
            else if ((exposedPosition < midSpan.End) || this.right == null)
            {
                // if we took this branch because this.right is null, the position is the last position in the elision buffer
                return sourceSnapshot.GetLineNumberFromPosition(exposedPosition + sourcePrefixSize + LeftTotalSourceSize() - LeftTotalExposedSize()) -
                        (LeftTotalHiddenLineBreakCount() + hiddenPrefixLineBreakCount);
            }
            else
            {
                return this.right.GetLineNumberFromPosition
                                    (sourceSnapshot            : sourceSnapshot,
                                    exposedPosition            : exposedPosition - midSpan.End,
                                    hiddenPrefixLineBreakCount : hiddenPrefixLineBreakCount + LeftTotalHiddenLineBreakCount() + (this.sourceLineBreakCount - this.exposedLineBreakCount),
                                    sourcePrefixSize           : sourcePrefixSize + LeftTotalSourceSize() + this.sourceSize);
            }
        }
        #endregion

        #region Change Incorporation
        /// <summary>
        /// Incorporate a text change in the source buffer into the elision map.
        /// </summary>
        /// <param name="beforeSourceSnapshot">Snapshot of the source buffer before the change occurred.</param>
        /// <param name="beforeElisionSnapshot">Snapshot of the elision buffer before the change occurred.</param>
        /// <param name="sourceInsertionPosition">If there is an insertion as part of the change, the position of that
        /// insertion relative to the subtree rooted at this node; otherwise null.</param>
        /// <param name="newText">New text to be inserted (or the null string).</param>
        /// <param name="sourceDeletionSpan">If there is a deletion as part of the change, the span of that
        /// deletion relative to the subtree rooted at this node; otherwise null.</param>
        /// <param name="absoluteSourceOldPosition">The absolute position of the change in the <paramref name="beforeSourceSnapshot"/>.</param>
        /// <param name="projectedPrefixSize">The size (in characters) of that portion of elision buffer that
        /// precedes this node and is not in its left subtree.</param>
        /// <param name="projectedChanges">List of changes projected into the elision buffer; constructed by
        /// this function.</param>
        /// <param name="incomingAccumulatedDelta">Accumulated delta from prior source changes in the current
        /// multipart edit transaction.</param>
        /// <param name="outgoingAccumulatedDelta">Increment to the accumulated delta resulting from the
        /// current change.</param>
        /// <param name="accumulatedDelete">Amount of text deleted so far for the current change.</param>
        /// <returns>A new immutable ElisionMapNode.</returns>
        public ElisionMapNode IncorporateChange(ITextSnapshot beforeSourceSnapshot,
                                                ITextSnapshot afterSourceSnapshot,
                                                ITextSnapshot beforeElisionSnapshot,
                                                int? sourceInsertionPosition,
                                                ChangeString newText,
                                                Span? sourceDeletionSpan,
                                                int absoluteSourceOldPosition,
                                                int absoluteSourceNewPosition,
                                                int projectedPrefixSize,
                                                FrugalList<TextChange> projectedChanges,
                                                int incomingAccumulatedDelta,
                                                ref int outgoingAccumulatedDelta,
                                                ref int accumulatedDelete)
        {
            // All positions and spans in this method are relative to the current subtree except those with 'absolute' in the name
            // All positions and spans in coordinate space of source buffer have 'source' in the name
            // All positions and spans in coordinate space of elision buffer have 'projected' in the name

            Debug.Assert(sourceDeletionSpan == null || sourceDeletionSpan.Value.End <= this.totalSourceSize);
            Debug.Assert(sourceInsertionPosition == null || sourceInsertionPosition >= 0);
            Debug.Assert(sourceInsertionPosition == null || sourceInsertionPosition <= this.totalSourceSize);

            ElisionMapNode newLeft = this.left;
            ElisionMapNode newRight = this.right;
            int newExposedLineBreakCount = this.exposedLineBreakCount;
            int newSourceLineBreakCount = this.sourceLineBreakCount;
            int newExposedSize = exposedSize;
            int newSourceSize = sourceSize;
            bool newLeftmostElision = this.leftmostElision;

            int leftTotalSourceSize = LeftTotalSourceSize();

            Span leftSourceSpan = new Span(0, leftTotalSourceSize);
            Span midExposedSourceSpan = new Span(leftTotalSourceSize, this.exposedSize);
            Span midHiddenSourceSpan = new Span(midExposedSourceSpan.End, this.sourceSize - this.exposedSize);
            Span rightSourceSpan = new Span(midHiddenSourceSpan.End, this.totalSourceSize - leftTotalSourceSize - this.sourceSize);

            #region Incorporate left subtree changes
            Span? leftSourceDeletionSpan = leftSourceSpan.Overlap(sourceDeletionSpan);
            bool insertionOnLeft = sourceInsertionPosition.HasValue && sourceInsertionPosition.Value < leftTotalSourceSize;
            int? leftSourceInsertionPosition = null;
            if (insertionOnLeft)
            {
                if (leftSourceDeletionSpan.HasValue && leftSourceDeletionSpan.Value.End == midExposedSourceSpan.Start)
                {
                    // we have a replacement, and the deleted text in the left subtree touches the left edge of the current node.
                    // The inserted text needs to be exposed in this node, not possibly swallowed into a hidden part of the left node.
                    // Leave the leftSourceInsertionPosition equal to null so the left subtree doesn't stick it at the end of itself
                    insertionOnLeft = false;
                    sourceInsertionPosition = midExposedSourceSpan.Start;
                    Debug.Assert(!this.leftmostElision);    // there is no node to the left of a leftmost elision node
                }
                else
                {
                    leftSourceInsertionPosition = sourceInsertionPosition;
                }
            }
            if (insertionOnLeft || leftSourceDeletionSpan.HasValue)
            {
                // insertion (if any) and start of deletion (if any) is in the left subtree
                newLeft = this.left.IncorporateChange(beforeSourceSnapshot      : beforeSourceSnapshot,
                                                      afterSourceSnapshot       : afterSourceSnapshot,
                                                      beforeElisionSnapshot     : beforeElisionSnapshot,
                                                      sourceInsertionPosition   : leftSourceInsertionPosition,
                                                      newText                   : newText,
                                                      sourceDeletionSpan        : leftSourceSpan.Overlap(sourceDeletionSpan),
                                                      absoluteSourceOldPosition : absoluteSourceOldPosition,
                                                      absoluteSourceNewPosition : absoluteSourceNewPosition,
                                                      projectedPrefixSize       : projectedPrefixSize,
                                                      projectedChanges          : projectedChanges,
                                                      incomingAccumulatedDelta  : incomingAccumulatedDelta,
                                                      outgoingAccumulatedDelta  : ref outgoingAccumulatedDelta,
                                                      accumulatedDelete         : ref accumulatedDelete);
            }
            #endregion

            #region Incorporate current subtree changes

            Span? exposedSourceDeletion = midExposedSourceSpan.Overlap(sourceDeletionSpan);
            Span? hiddenSourceDeletion = midHiddenSourceSpan.Overlap(sourceDeletionSpan);

            // Insertion and deletion are handled independently and recombined by text change normalization
            // Insertion

            // In each of the three cases below, if an insertion belongs in this node, set sourceInsertionPosition to
            // null, preventing it from also being performed in the right subtree when the current node has size zero.
            if (sourceInsertionPosition.HasValue)
            {
                // special case for leftmostElision node
                if (this.leftmostElision)
                {
                    // insertion into the exposed part of this node isn't possible
                    Debug.Assert(this.left == null);
                    Debug.Assert(this.exposedSize == 0);
                    Debug.Assert(leftTotalSourceSize == 0);
                    if (sourceInsertionPosition.Value <= this.sourceSize)
                    {
                        // insertion into hidden prefix of elision buffer
                        newSourceSize += newText.Length;

                        int incrementalLineCount;
                        ComputeIncrementalLineCountForHiddenInsertion(afterSourceSnapshot, absoluteSourceNewPosition, newText, out incrementalLineCount);

                        newSourceLineBreakCount += incrementalLineCount;
                        sourceInsertionPosition = null;
                    }
                }
                else if ((leftTotalSourceSize <= sourceInsertionPosition.Value) &&
                    (sourceInsertionPosition.Value <= leftTotalSourceSize + this.exposedSize))
                {
                    // insertion (if any) is in the exposed part of the current node
                    newExposedSize += newText.Length;
                    newSourceSize += newText.Length;
                    int projectedPosition = projectedPrefixSize + sourceInsertionPosition.Value - LeftTotalHiddenSize() - incomingAccumulatedDelta;

                    // effects on line count are computed based on local information. Interactions with adjacent segments
                    // must be handled at a higher level (undone).

                    int deletionLength = 0;
                    if (exposedSourceDeletion.HasValue)
                    {
                        Debug.Assert(exposedSourceDeletion.Value.Start == sourceInsertionPosition.Value);
                        deletionLength = exposedSourceDeletion.Value.Length;
                    }

                    char? predChar = sourceInsertionPosition.Value > midExposedSourceSpan.Start
                                    ? afterSourceSnapshot[absoluteSourceNewPosition - 1]
                                    : (char?)null;  // insertion is at start of segment
                    char? succChar = sourceInsertionPosition.Value + deletionLength < midExposedSourceSpan.End
                                    ? afterSourceSnapshot[absoluteSourceNewPosition + newText.Length]
                                    : (char?)null;  // insertion is at end of segment

                    LineBreakBoundaryConditions boundaryConditions;
                    int incrementalLineCount;
                    ComputeIncrementalLineCountForExposedInsertion(predChar, succChar, newText, out incrementalLineCount, out boundaryConditions);

                    newExposedLineBreakCount += incrementalLineCount;
                    newSourceLineBreakCount += incrementalLineCount;

                    TextChange change = new TextChange(projectedPosition, ChangeString.EmptyChangeString, newText, boundaryConditions);
                    projectedChanges.Add(change);
                    outgoingAccumulatedDelta += change.Delta;
                    sourceInsertionPosition = null;
                }
                else if ((leftTotalSourceSize + this.exposedSize < sourceInsertionPosition.Value) &&
                         ((sourceInsertionPosition.Value < leftTotalSourceSize + this.sourceSize) || (this.right == null)))
                {
                    // insertion (if any) is in the hidden part of the current node
                    // Unless...we are also deleting from the point of the insertion through the end of the node, in which case
                    // the insertion should go at the beginning of the next segment. If there is no right subtree, this case will
                    // have been handled on the way down by some ancestor node.
                    if (this.right != null && (hiddenSourceDeletion.HasValue && hiddenSourceDeletion.Value.End == midHiddenSourceSpan.End))
                    {
                        sourceInsertionPosition = midHiddenSourceSpan.End;
                    }
                    else
                    {
                        newSourceSize += newText.Length;

                        int incrementalLineCount;
                        ComputeIncrementalLineCountForHiddenInsertion(afterSourceSnapshot, absoluteSourceNewPosition, newText, out incrementalLineCount);

                        newSourceLineBreakCount += incrementalLineCount;
                        sourceInsertionPosition = null;
                    }
                }
            }

            // Deletion of exposed text
            if (exposedSourceDeletion.HasValue)
            {
                newExposedSize -= exposedSourceDeletion.Value.Length;
                newSourceSize -= exposedSourceDeletion.Value.Length;
                int projectedDeletionPosition = projectedPrefixSize + exposedSourceDeletion.Value.Start - LeftTotalHiddenSize();
                int sourceDeletionSegmentPosition = absoluteSourceOldPosition - accumulatedDelete;
                ReferenceChangeString exposedDeletionText = 
                    new ReferenceChangeString(new SnapshotSpan(beforeSourceSnapshot, sourceDeletionSegmentPosition, exposedSourceDeletion.Value.Length));

                LineBreakBoundaryConditions boundaryConditions;
                int incrementalLineCount;
                ComputeIncrementalLineCountForDeletion(beforeElisionSnapshot, new Span(projectedDeletionPosition - incomingAccumulatedDelta, exposedSourceDeletion.Value.Length), exposedDeletionText, out incrementalLineCount, out boundaryConditions);
                
                newExposedLineBreakCount += incrementalLineCount;
                newSourceLineBreakCount += incrementalLineCount;
                TextChange change = new TextChange(projectedDeletionPosition - incomingAccumulatedDelta, exposedDeletionText, ChangeString.EmptyChangeString, boundaryConditions);
                projectedChanges.Add(change);
                outgoingAccumulatedDelta += change.Delta;
                accumulatedDelete += change.Delta;
            }

            // Deletion of hidden text
            if (hiddenSourceDeletion.HasValue)
            {
                int sourceDeletionSegmentPosition = absoluteSourceOldPosition - accumulatedDelete;
                ReferenceChangeString hiddenDeletionText = 
                    new ReferenceChangeString(new SnapshotSpan(beforeSourceSnapshot, sourceDeletionSegmentPosition, hiddenSourceDeletion.Value.Length));

                LineBreakBoundaryConditions dontCare;
                int incrementalLineCount;
                ComputeIncrementalLineCountForDeletion(beforeSourceSnapshot, new Span(sourceDeletionSegmentPosition, hiddenSourceDeletion.Value.Length), hiddenDeletionText, out incrementalLineCount, out dontCare);
                newSourceLineBreakCount += incrementalLineCount;

                newSourceSize -= hiddenSourceDeletion.Value.Length;
                accumulatedDelete -= hiddenSourceDeletion.Value.Length;
            }
            #endregion

            #region Incorporate right subtree changes
            Span? rightSourceDeletionSpan = rightSourceSpan.Overlap(sourceDeletionSpan);
            bool insertionOnRight = (sourceInsertionPosition.HasValue) && (this.right != null) && (leftTotalSourceSize + this.sourceSize <= sourceInsertionPosition.Value);
            if (rightSourceDeletionSpan.HasValue || insertionOnRight)
            {
                // insertion (if any) or part of deletion (if any) is in the right subtree
                newRight = this.right.IncorporateChange(beforeSourceSnapshot      : beforeSourceSnapshot,
                                                        afterSourceSnapshot       : afterSourceSnapshot, 
                                                        beforeElisionSnapshot     : beforeElisionSnapshot,
                                                        sourceInsertionPosition   : insertionOnRight ? sourceInsertionPosition.Value - leftTotalSourceSize - this.sourceSize : (int?)null,
                                                        newText                   : insertionOnRight ? newText : ChangeString.EmptyChangeString,
                                                        sourceDeletionSpan        : rightSourceDeletionSpan.HasValue
                                                                                        ? new Span(rightSourceDeletionSpan.Value.Start - (leftTotalSourceSize + this.sourceSize),
                                                                                                   rightSourceDeletionSpan.Value.Length)
                                                                                        : (Span?)null,
                                                        absoluteSourceOldPosition : absoluteSourceOldPosition,
                                                        absoluteSourceNewPosition : absoluteSourceNewPosition,
                                                        projectedPrefixSize       : projectedPrefixSize + LeftTotalExposedSize() + this.exposedSize,
                                                        projectedChanges          : projectedChanges,
                                                        incomingAccumulatedDelta  : incomingAccumulatedDelta,
                                                        outgoingAccumulatedDelta  : ref outgoingAccumulatedDelta,
                                                        accumulatedDelete         : ref accumulatedDelete);
            }
            #endregion

            return new ElisionMapNode(newExposedSize, newSourceSize, newExposedLineBreakCount, newSourceLineBreakCount, newLeft, newRight, newLeftmostElision);
        }

        private static void ComputeIncrementalLineCountForHiddenInsertion(ITextSnapshot afterSnapshot,
                                                                          int start,
                                                                          ChangeString insertedText,
                                                                          out int incrementalLineCount)
        {
            int lineCount = insertedText.ComputeLineBreakCount();
            LineBreakBoundaryConditions bc = LineBreakBoundaryConditions.None;

            Debug.Assert(start >= 0 && start <= afterSnapshot.Length);
            Debug.Assert(insertedText.Length > 0);

            if (start > 0 && afterSnapshot[start - 1] == '\r')
            {
                bc = bc | LineBreakBoundaryConditions.PrecedingReturn;
                if (insertedText[0] == '\n')
                {
                    // \n was inserted after \r, and we counted it above as a new line, which it isn't.
                    // correct for that.
                    lineCount--;
                }
            }

            int end = start + insertedText.Length;
            if (end < afterSnapshot.Length && afterSnapshot[end] == '\n')
            {
                bc = bc | LineBreakBoundaryConditions.SucceedingNewline;
                if (insertedText[insertedText.Length - 1] == '\r')
                {
                    // \r was inserted before \n, and we counted it above as a new line, which it isn't.
                    // correct for that.
                    lineCount--;
                }
            }
            if (bc == (LineBreakBoundaryConditions.PrecedingReturn | LineBreakBoundaryConditions.SucceedingNewline))
            {
                // the insertion separated a \r and a \n, so increase the line count
                lineCount++;
            }
            incrementalLineCount = lineCount;
        }

        /// <summary>
        /// Determine the impact of an insertion on the line count.
        /// </summary>
        private static void ComputeIncrementalLineCountForExposedInsertion(char? predecessor,
                                                                           char? successor,
                                                                           ChangeString insertedText,
                                                                           out int incrementalLineCount,
                                                                           out LineBreakBoundaryConditions boundaryConditions)
        {
            int lineCount = insertedText.ComputeLineBreakCount();
            LineBreakBoundaryConditions bc = LineBreakBoundaryConditions.None;

            if (predecessor == '\r')
            {
                bc = bc | LineBreakBoundaryConditions.PrecedingReturn;
                if (insertedText[0] == '\n')
                {
                    // \n was inserted after \r, and we counted it above as a new line, which it isn't.
                    // correct for that.
                    lineCount--;
                }
            }
            if (successor == '\n')
            {
                bc = bc | LineBreakBoundaryConditions.SucceedingNewline;
                if (insertedText[insertedText.Length - 1] == '\r')
                {
                    // \r was inserted before \n, and we counted it above as a new line, which it isn't.
                    // correct for that.
                    lineCount--;
                }
            }
            if (bc == (LineBreakBoundaryConditions.PrecedingReturn | LineBreakBoundaryConditions.SucceedingNewline))
            {
                // the insertion separated a \r and a \n, so increase the line count
                lineCount++;
            }
            incrementalLineCount = lineCount;
            boundaryConditions = bc;
        }

        /// <summary>
        /// Determine the impact of a deletion on the line count.
        /// </summary>
        private static void ComputeIncrementalLineCountForDeletion(ITextSnapshot beforeSnapshot,
                                                                   Span deletionSpan,
                                                                   ReferenceChangeString deletedText,
                                                                   out int incrementalLineCount,
                                                                   out LineBreakBoundaryConditions boundaryConditions)
        {
            int lineCount = -deletedText.ComputeLineBreakCount();
            LineBreakBoundaryConditions bc = LineBreakBoundaryConditions.None;

            Debug.Assert(deletionSpan.End <= beforeSnapshot.Length);
            Debug.Assert(deletedText.Length > 0);

            if (deletionSpan.Start > 0 && beforeSnapshot[deletionSpan.Start - 1] == '\r')
            {
                bc = bc | LineBreakBoundaryConditions.PrecedingReturn;
                if (deletedText[0] == '\n')
                {
                    // the \n of a \r\n pair was deleted and we counted it as losing a line, which it isn't.
                    // correct for that.
                    lineCount++;
                }
            }
            if (deletionSpan.End < beforeSnapshot.Length && beforeSnapshot[deletionSpan.End] == '\n')
            {
                bc = bc | LineBreakBoundaryConditions.SucceedingNewline;
                if (deletedText[deletedText.Length - 1] == '\r')
                {
                    // the \r of a \r\n pair was deleted and we counted it as losing a line, which it isn't.
                    // correct for that.
                    lineCount++;
                }
            }
            if (bc == (LineBreakBoundaryConditions.PrecedingReturn | LineBreakBoundaryConditions.SucceedingNewline))
            {
                // the deletion joined a \r and a \n, so decrease the line count
                lineCount--;
            }
            incrementalLineCount = lineCount;
            boundaryConditions = bc;
        }
        #endregion
    }
}
