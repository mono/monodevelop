// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Implementation
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.Text.Projection;
    using Microsoft.VisualStudio.Text.Utilities;

    internal partial class MappingSpan : IMappingSpan
    {
        private SnapshotSpan anchorSpan;
        private SpanTrackingMode trackingMode;
        private IBufferGraph bufferGraph;

        public MappingSpan(SnapshotSpan anchorSpan, SpanTrackingMode trackingMode, IBufferGraph bufferGraph)
        {
            if (anchorSpan.Snapshot == null)
            {
                throw new ArgumentNullException("anchorSpan");
            }
            if (trackingMode < SpanTrackingMode.EdgeExclusive || trackingMode > SpanTrackingMode.EdgeNegative)
            {
                throw new ArgumentOutOfRangeException("trackingMode");
            }
            if (bufferGraph == null)
            {
                throw new ArgumentNullException("bufferGraph");
            }
            this.anchorSpan = anchorSpan;
            this.trackingMode = trackingMode;
            this.bufferGraph = bufferGraph;
        }

        public IMappingPoint Start 
        {
            get { return new MappingPoint(new SnapshotPoint(this.anchorSpan.Snapshot, this.anchorSpan.Start), 
                                                            (this.trackingMode == SpanTrackingMode.EdgeInclusive ||
                                                             this.trackingMode == SpanTrackingMode.EdgeNegative) 
                                                                ? PointTrackingMode.Negative 
                                                                : PointTrackingMode.Positive,
                                                            this.bufferGraph); }
        }

        public IMappingPoint End 
        {
            get { return new MappingPoint(new SnapshotPoint(this.anchorSpan.Snapshot, this.anchorSpan.End),
                                                            (this.trackingMode == SpanTrackingMode.EdgeExclusive ||
                                                             this.trackingMode == SpanTrackingMode.EdgeNegative)
                                                                ? PointTrackingMode.Negative
                                                                : PointTrackingMode.Positive,
                                                            this.bufferGraph);
            }
        }

        public ITextBuffer AnchorBuffer
        {
            get { return this.anchorSpan.Snapshot.TextBuffer; }
        }

        public IBufferGraph BufferGraph
        {
            get { return this.bufferGraph; }
        }

        public NormalizedSnapshotSpanCollection GetSpans(ITextBuffer targetBuffer)
        {
            // null textBuffer check will be handled by the buffer graph
            ITextBuffer anchorBuffer = this.AnchorBuffer;
            SnapshotSpan currentSpan = this.anchorSpan.TranslateTo(anchorBuffer.CurrentSnapshot, this.trackingMode);

            if (anchorBuffer == targetBuffer)
            {
                return new NormalizedSnapshotSpanCollection(currentSpan);
            }

            ITextBuffer topBuffer = this.bufferGraph.TopBuffer;
            if (targetBuffer == topBuffer)
            {
                return this.bufferGraph.MapUpToBuffer(currentSpan, this.trackingMode, topBuffer);
            }
            else if (anchorBuffer == topBuffer)
            {
                return this.bufferGraph.MapDownToBuffer(currentSpan, this.trackingMode, targetBuffer);
            }
            else
            {
                if (anchorBuffer is IProjectionBufferBase)
                {
                    NormalizedSnapshotSpanCollection tentative = this.bufferGraph.MapDownToBuffer(currentSpan, this.trackingMode, targetBuffer);
                    if (tentative.Count > 0)
                    {
                        return tentative;
                    }
                }
                return this.bufferGraph.MapUpToBuffer(currentSpan, this.trackingMode, targetBuffer);
            }
        }

        public NormalizedSnapshotSpanCollection GetSpans(ITextSnapshot targetSnapshot)
        {
            if (targetSnapshot == null)
                throw new ArgumentNullException("targetSnapshot");

            NormalizedSnapshotSpanCollection results = GetSpans(targetSnapshot.TextBuffer);
            if ((results.Count > 0) && (results[0].Snapshot != targetSnapshot))
            {
                FrugalList<SnapshotSpan> translatedSpans = new FrugalList<SnapshotSpan>();
                foreach (SnapshotSpan s in results)
                {
                    translatedSpans.Add(s.TranslateTo(targetSnapshot, trackingMode));
                }

                results = new NormalizedSnapshotSpanCollection(translatedSpans);
            }

            return results;
        }

        public NormalizedSnapshotSpanCollection GetSpans(Predicate<ITextBuffer> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException("match");
            }

            ITextBuffer anchorBuffer = this.AnchorBuffer;
            SnapshotSpan currentSpan = this.anchorSpan.TranslateTo(anchorBuffer.CurrentSnapshot, this.trackingMode);
            if (match(anchorBuffer))
            {
                return new NormalizedSnapshotSpanCollection(currentSpan);
            }
            if (anchorBuffer == this.bufferGraph.TopBuffer)
            {
                return this.bufferGraph.MapDownToFirstMatch(currentSpan, this.trackingMode, snapshot => (match(snapshot.TextBuffer)));
            }
            else
            {
                // guess which way to go
                if (anchorBuffer is IProjectionBufferBase)
                {
                    NormalizedSnapshotSpanCollection tentative = this.bufferGraph.MapDownToFirstMatch(currentSpan, this.trackingMode, snapshot => (match(snapshot.TextBuffer)));
                    if (tentative.Count > 0)
                    {
                        return tentative;
                    }
                }
                return this.bufferGraph.MapUpToFirstMatch(currentSpan, this.trackingMode, snapshot => (match(snapshot.TextBuffer)));
            }
        }

        public override string ToString()
        {
            return String.Format("MappingSpan anchored at {0}", this.anchorSpan);
        }
    }
}