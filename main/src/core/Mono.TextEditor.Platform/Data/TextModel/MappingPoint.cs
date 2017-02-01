// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Implementation
{
    using System;
    using Microsoft.VisualStudio.Text.Projection;

    internal partial class MappingPoint : IMappingPoint
    {
        SnapshotPoint anchorPoint;
        PointTrackingMode trackingMode;
        IBufferGraph bufferGraph;

        public MappingPoint(SnapshotPoint anchorPoint, PointTrackingMode trackingMode, IBufferGraph bufferGraph)
        {
            if (anchorPoint.Snapshot == null)
            {
                throw new ArgumentNullException("anchorPoint");
            }
            if (trackingMode < PointTrackingMode.Positive || trackingMode > PointTrackingMode.Negative)
            {
                throw new ArgumentOutOfRangeException("trackingMode");
            }
            if (bufferGraph == null)
            {
                throw new ArgumentNullException("bufferGraph");
            }
            this.anchorPoint = anchorPoint;
            this.trackingMode = trackingMode;
            this.bufferGraph = bufferGraph;
        }

        public ITextBuffer AnchorBuffer 
        {
            get { return this.anchorPoint.Snapshot.TextBuffer; }
        }

        public IBufferGraph BufferGraph
        {
            get { return this.bufferGraph; }
        }

        public SnapshotPoint? GetPoint(ITextBuffer targetBuffer, PositionAffinity affinity)
        {
            if (targetBuffer == null)
            {
                throw new ArgumentNullException("targetBuffer");
            }
            ITextBuffer anchorBuffer = this.AnchorBuffer;
            SnapshotPoint currentPoint = this.anchorPoint.TranslateTo(anchorBuffer.CurrentSnapshot, this.trackingMode);
            if (anchorBuffer == targetBuffer)
            {
                return currentPoint;
            }

            ITextBuffer topBuffer = this.bufferGraph.TopBuffer;
            if (targetBuffer == topBuffer)
            {
                return this.bufferGraph.MapUpToBuffer(currentPoint, this.trackingMode, affinity, topBuffer);
            }
            else if (anchorBuffer == topBuffer)
            {
                return this.bufferGraph.MapDownToBuffer(currentPoint, this.trackingMode, targetBuffer, affinity);
            }
            else
            {
                // we don't know a priori which way to go, so we'll guess
                if (anchorBuffer is IProjectionBufferBase)
                {
                    SnapshotPoint? tentative = this.bufferGraph.MapDownToBuffer(currentPoint, this.trackingMode, targetBuffer, affinity);
                    if (tentative.HasValue)
                    {
                        return tentative;
                    }
                }
                // ok, go the other way
                return this.bufferGraph.MapUpToBuffer(currentPoint, this.trackingMode, affinity, targetBuffer);
            }
        }

        public SnapshotPoint? GetPoint(ITextSnapshot targetSnapshot, PositionAffinity affinity)
        {
            if (targetSnapshot == null)
                throw new ArgumentNullException("targetSnapshot");

            SnapshotPoint? result = GetPoint(targetSnapshot.TextBuffer, affinity);
            if (result.HasValue && (result.Value.Snapshot != targetSnapshot))
            {
                result = result.Value.TranslateTo(targetSnapshot, this.trackingMode);
            }

            return result;
        }

        public SnapshotPoint? GetPoint(Predicate<ITextBuffer> match, PositionAffinity affinity)
        {
            if (match == null)
            {
                throw new ArgumentNullException("match");
            }
            ITextBuffer anchorBuffer = this.AnchorBuffer;
            SnapshotPoint currentPoint = this.anchorPoint.TranslateTo(anchorBuffer.CurrentSnapshot, this.trackingMode);
            if (match(anchorBuffer))
            {
                return currentPoint;
            }

            if (anchorBuffer == this.bufferGraph.TopBuffer)
            {
                // the only way to go is down
                return this.bufferGraph.MapDownToFirstMatch(currentPoint, this.trackingMode, snapshot => (match(snapshot.TextBuffer)), affinity);
            }
            else
            {
                // guess which way to go
                if (anchorBuffer is IProjectionBufferBase)
                {
                    SnapshotPoint? tentative = this.bufferGraph.MapDownToFirstMatch(currentPoint, this.trackingMode, snapshot => (match(snapshot.TextBuffer)), affinity);
                    if (tentative.HasValue)
                    {
                        return tentative;
                    }
                }
                // go the other way.
                if (match(this.bufferGraph.TopBuffer))
                {
                    return this.bufferGraph.MapUpToBuffer(currentPoint, this.trackingMode, affinity, this.bufferGraph.TopBuffer);
                }
                else
                {
                    return this.bufferGraph.MapUpToFirstMatch(currentPoint, this.trackingMode, snapshot => (match(snapshot.TextBuffer)), affinity);
                }
            }
        }

        public SnapshotPoint? GetInsertionPoint(Predicate<ITextBuffer> match)
        {
            // always maps down
            if (match == null)
            {
                throw new ArgumentNullException("match");
            }
            ITextBuffer anchorBuffer = this.AnchorBuffer;
            SnapshotPoint currentPoint = this.anchorPoint.TranslateTo(anchorBuffer.CurrentSnapshot, this.trackingMode);
            return this.bufferGraph.MapDownToInsertionPoint(currentPoint, this.trackingMode, snapshot => (match(snapshot.TextBuffer)));
        }
    }
}