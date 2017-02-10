namespace Microsoft.VisualStudio.Text.Projection.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Implementation;
    using Microsoft.VisualStudio.Text.Utilities;
    using Strings = Microsoft.VisualStudio.Text.Implementation.Strings;

    partial class BufferGraph : IBufferGraph
    {
        #region Private members
        private readonly ITextBuffer topBuffer;
        private readonly GuardedOperations guardedOperations;
        internal Dictionary<ITextBuffer, FrugalList<IProjectionBufferBase>> importingProjectionBufferMap = new Dictionary<ITextBuffer, FrugalList<IProjectionBufferBase>>();
        internal List<WeakEventHookForBufferGraph> eventHooks = new List<WeakEventHookForBufferGraph>();
        #endregion

        #region Construction
        public BufferGraph(ITextBuffer topBuffer, GuardedOperations guardedOperations)
        {
            if (topBuffer == null)
            {
                throw new ArgumentNullException("topBuffer");
            }
            if (guardedOperations == null)
            {
                throw new ArgumentNullException("guardedOperations");
            }

            this.topBuffer = topBuffer;
            this.guardedOperations = guardedOperations;

            this.importingProjectionBufferMap.Add(topBuffer, null);
            // The top buffer has no targets, but it is put here so membership in this map can be used uniformly
            // to determine whether a buffer is in the buffer graph

            // Subscribe to content type changed events on the toplevel buffer
            this.eventHooks.Add(new WeakEventHookForBufferGraph(this, topBuffer));

            IProjectionBufferBase projectionBufferBase = topBuffer as IProjectionBufferBase;
            if (projectionBufferBase != null)
            {
                IList<ITextBuffer> sourceBuffers = projectionBufferBase.SourceBuffers;
                FrugalList<ITextBuffer> dontCare = new FrugalList<ITextBuffer>();
                foreach (ITextBuffer sourceBuffer in sourceBuffers)
                {
                    AddSourceBuffer(projectionBufferBase, sourceBuffer, dontCare);
                }
            }
        }
        #endregion

        #region Buffers
        public ITextBuffer TopBuffer
        {
            get { return this.topBuffer; }
        }

        public Collection<ITextBuffer> GetTextBuffers(Predicate<ITextBuffer> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException("match");
            }
            FrugalList<ITextBuffer> buffers = new FrugalList<ITextBuffer>();
            foreach (ITextBuffer buffer in this.importingProjectionBufferMap.Keys)
            {
                if (match(buffer))
                {
                    buffers.Add(buffer);
                }
            }
            return new Collection<ITextBuffer>(buffers);
        }
        #endregion

        #region Mapping Point/Span Factories
        public IMappingPoint CreateMappingPoint(SnapshotPoint point, PointTrackingMode trackingMode)
        {
            return new MappingPoint(point, trackingMode, this);
        }

        public IMappingSpan CreateMappingSpan(SnapshotSpan span, SpanTrackingMode trackingMode)
        {
            return new MappingSpan(span, trackingMode, this);
        }
        #endregion

        #region Point Mapping
        public SnapshotPoint? MapDownToFirstMatch(SnapshotPoint position, PointTrackingMode trackingMode, Predicate<ITextSnapshot> match, PositionAffinity affinity)
        {
            if (position.Snapshot == null)
            {
                throw new ArgumentNullException("position");
            }
            if (trackingMode < PointTrackingMode.Positive || trackingMode > PointTrackingMode.Negative)
            {
                throw new ArgumentOutOfRangeException("trackingMode");
            }
            if (match == null)
            {
                throw new ArgumentNullException("match");
            }
            if (affinity < PositionAffinity.Predecessor || affinity > PositionAffinity.Successor)
            {
                throw new ArgumentOutOfRangeException("affinity");
            }
            if (!this.importingProjectionBufferMap.ContainsKey(position.Snapshot.TextBuffer))
            {
                return null;
            }

            ITextBuffer currentBuffer = position.Snapshot.TextBuffer;
            ITextSnapshot currentSnapshot = currentBuffer.CurrentSnapshot;
            int currentPosition = position.TranslateTo(currentSnapshot, trackingMode).Position;
            while (!match(currentSnapshot))
            {
                IProjectionBufferBase projBuffer = currentBuffer as IProjectionBufferBase;
                if (projBuffer == null)
                {
                    return null;
                }
                IProjectionSnapshot projSnap = projBuffer.CurrentSnapshot;
                if (projSnap.SourceSnapshots.Count == 0)
                {
                    return null;
                }
                SnapshotPoint currentPoint = projSnap.MapToSourceSnapshot(currentPosition, affinity);
                currentPosition = currentPoint.Position;
                currentSnapshot = currentPoint.Snapshot;
                currentBuffer = currentSnapshot.TextBuffer;
            }
            return new SnapshotPoint(currentSnapshot, currentPosition);
        }

        public SnapshotPoint? MapDownToInsertionPoint(SnapshotPoint position, PointTrackingMode trackingMode, Predicate<ITextSnapshot> match)
        {
            if (position.Snapshot == null)
            {
                throw new ArgumentNullException("position");
            }
            if (trackingMode < PointTrackingMode.Positive || trackingMode > PointTrackingMode.Negative)
            {
                throw new ArgumentOutOfRangeException("trackingMode");
            }
            if (match == null)
            {
                throw new ArgumentNullException("match");
            }

            ITextBuffer currentBuffer = position.Snapshot.TextBuffer;
            int currentPosition = position.TranslateTo(currentBuffer.CurrentSnapshot, trackingMode);
            ITextSnapshot currentSnapshot = currentBuffer.CurrentSnapshot;
            while (!match(currentSnapshot))
            {
                IProjectionBufferBase projBuffer = currentBuffer as IProjectionBufferBase;
                if (projBuffer == null)
                {
                    return null;
                }
                IProjectionSnapshot projSnap = projBuffer.CurrentSnapshot;
                if (projSnap.SourceSnapshots.Count == 0)
                {
                    return null;
                }
                SnapshotPoint currentPoint = projSnap.MapToSourceSnapshot(currentPosition);
                currentPosition = currentPoint.Position;
                currentSnapshot = currentPoint.Snapshot;
                currentBuffer = currentSnapshot.TextBuffer;
            }
            return new SnapshotPoint(currentSnapshot, currentPosition);
        }

        public SnapshotPoint? MapDownToBuffer(SnapshotPoint position, PointTrackingMode trackingMode, ITextBuffer targetBuffer, PositionAffinity affinity)
        {
            if (position.Snapshot == null)
            {
                throw new ArgumentNullException("position");
            }
            if (trackingMode < PointTrackingMode.Positive || trackingMode > PointTrackingMode.Negative)
            {
                throw new ArgumentOutOfRangeException("trackingMode");
            }
            if (targetBuffer == null)
            {
                throw new ArgumentNullException("targetBuffer");
            }
            if (affinity < PositionAffinity.Predecessor || affinity > PositionAffinity.Successor)
            {
                throw new ArgumentOutOfRangeException("affinity");
            }

            ITextBuffer currentBuffer = position.Snapshot.TextBuffer;
            ITextSnapshot currentSnapshot = currentBuffer.CurrentSnapshot;
            int currentPosition = position.TranslateTo(currentSnapshot, trackingMode).Position;

            while (currentBuffer != targetBuffer)
            {
                IProjectionBufferBase projBuffer = currentBuffer as IProjectionBufferBase;
                if (projBuffer == null)
                {
                    return null;
                }
                IProjectionSnapshot projSnap = projBuffer.CurrentSnapshot;
                if (projSnap.SourceSnapshots.Count == 0)
                {
                    return null;
                }
                SnapshotPoint currentPoint = projSnap.MapToSourceSnapshot(currentPosition, affinity);
                currentPosition = currentPoint.Position;
                currentSnapshot = currentPoint.Snapshot;
                currentBuffer = currentSnapshot.TextBuffer;
            }

            return new SnapshotPoint(currentSnapshot, currentPosition);
        }

        public SnapshotPoint? MapDownToSnapshot(SnapshotPoint position, PointTrackingMode trackingMode, ITextSnapshot targetSnapshot, PositionAffinity affinity)
        {
            if (targetSnapshot == null)
            {
                throw new ArgumentNullException("targetSnapshot");
            }

            SnapshotPoint? result = MapDownToBuffer(position, trackingMode, targetSnapshot.TextBuffer, affinity);
            if (result.HasValue && (result.Value.Snapshot != targetSnapshot))
            {
                result = result.Value.TranslateTo(targetSnapshot, trackingMode);
            }

            return result;
        }

        public SnapshotPoint? MapUpToBuffer(SnapshotPoint point, PointTrackingMode trackingMode, PositionAffinity affinity, ITextBuffer targetBuffer)
        {
            return CheckedMapUpToBuffer(point, trackingMode, snapshot => (snapshot.TextBuffer == targetBuffer), affinity);
        }


        public SnapshotPoint? MapUpToSnapshot(SnapshotPoint position, PointTrackingMode trackingMode, PositionAffinity affinity, ITextSnapshot targetSnapshot)
        {
            if (targetSnapshot == null)
            {
                throw new ArgumentNullException("targetSnapshot");
            }

            SnapshotPoint? result = MapUpToBuffer(position, trackingMode, affinity, targetSnapshot.TextBuffer);
            if (result.HasValue && (result.Value.Snapshot != targetSnapshot))
            {
                result = result.Value.TranslateTo(targetSnapshot, trackingMode);
            }

            return result;
        }

        public SnapshotPoint? MapUpToFirstMatch(SnapshotPoint point, PointTrackingMode trackingMode, Predicate<ITextSnapshot> match, PositionAffinity affinity)
        {
            if (match == null)
            {
                throw new ArgumentNullException("match");
            }
            return CheckedMapUpToBuffer(point, trackingMode, match, affinity);
        }

        private SnapshotPoint? CheckedMapUpToBuffer(SnapshotPoint point, PointTrackingMode trackingMode, Predicate<ITextSnapshot> match, PositionAffinity affinity)
        {
            if (point.Snapshot == null)
            {
                throw new ArgumentNullException("point");
            }
            if (trackingMode < PointTrackingMode.Positive || trackingMode > PointTrackingMode.Negative)
            {
                throw new ArgumentOutOfRangeException("trackingMode");
            }
            if (affinity < PositionAffinity.Predecessor || affinity > PositionAffinity.Successor)
            {
                throw new ArgumentOutOfRangeException("affinity");
            }

            if (!this.importingProjectionBufferMap.ContainsKey(point.Snapshot.TextBuffer))
            {
                return null;
            }
            else
            {
                SnapshotPoint currentPoint = point.TranslateTo(point.Snapshot.TextBuffer.CurrentSnapshot, trackingMode);
                return MapUpToBufferGuts(currentPoint, affinity, match);
            }
        }

        private SnapshotPoint? MapUpToBufferGuts(SnapshotPoint currentPoint, PositionAffinity affinity, Predicate<ITextSnapshot> match)
        {
            if (match(currentPoint.Snapshot))
            {
                return currentPoint;
            }
            FrugalList<IProjectionBufferBase> targetBuffers = this.importingProjectionBufferMap[currentPoint.Snapshot.TextBuffer];
            if (targetBuffers != null)  // targetBuffers will be null if we fell off the top
            {
                foreach (IProjectionBufferBase projBuffer in targetBuffers)
                {
                    SnapshotPoint? position = projBuffer.CurrentSnapshot.MapFromSourceSnapshot(currentPoint, affinity);
                    if (position.HasValue)
                    {
                        position = MapUpToBufferGuts(position.Value, affinity, match);
                        if (position.HasValue)
                        {
                            return position;
                        }
                    }
                }
            }
            return null;
        }
        #endregion

        #region Span Mapping
        public NormalizedSnapshotSpanCollection MapDownToFirstMatch(SnapshotSpan span, SpanTrackingMode trackingMode, Predicate<ITextSnapshot> match)
        {
            if (span.Snapshot == null)
            {
                throw new ArgumentNullException("span");
            }
            if (trackingMode < SpanTrackingMode.EdgeExclusive || trackingMode > SpanTrackingMode.EdgeNegative)
            {
                throw new ArgumentOutOfRangeException("trackingMode");
            }
            if (match == null)
            {
                throw new ArgumentNullException("match");
            }

            if (!this.importingProjectionBufferMap.ContainsKey(span.Snapshot.TextBuffer))
            {
                return NormalizedSnapshotSpanCollection.Empty;
            }

            ITextBuffer currentBuffer = span.Snapshot.TextBuffer;
            SnapshotSpan currentTopSpan = span.TranslateTo(currentBuffer.CurrentSnapshot, trackingMode);

            if (match(currentBuffer.CurrentSnapshot))
            {
                return new NormalizedSnapshotSpanCollection(currentTopSpan);
            }
            else if (!(currentBuffer is IProjectionBufferBase))
            {
                return NormalizedSnapshotSpanCollection.Empty;
            }
            else
            {
                FrugalList<Span> targetSpans = new FrugalList<Span>();
                FrugalList<SnapshotSpan> spans = new FrugalList<SnapshotSpan>() { currentTopSpan };
                ITextSnapshot chosenSnapshot = null;
                do
                {
                    spans = MapDownOneLevel(spans, match, ref chosenSnapshot, ref targetSpans);
                } while (spans.Count > 0);
                return chosenSnapshot == null ? NormalizedSnapshotSpanCollection.Empty : new NormalizedSnapshotSpanCollection(chosenSnapshot, targetSpans);
            }
        }

        public NormalizedSnapshotSpanCollection MapDownToBuffer(SnapshotSpan span, SpanTrackingMode trackingMode, ITextBuffer targetBuffer)
        {
            if (targetBuffer == null)
            {
                throw new ArgumentNullException("targetBuffer");
            }

            if (!this.importingProjectionBufferMap.ContainsKey(targetBuffer))
            {
                return NormalizedSnapshotSpanCollection.Empty;
            }
            else
            {
                return MapDownToFirstMatch(span, trackingMode, snapshot => (snapshot.TextBuffer == targetBuffer));
            }
        }

        public NormalizedSnapshotSpanCollection MapDownToSnapshot(SnapshotSpan span, SpanTrackingMode trackingMode, ITextSnapshot targetSnapshot)
        {
            if (targetSnapshot == null)
            {
                throw new ArgumentNullException("targetSnapshot");
            }

            NormalizedSnapshotSpanCollection results = MapDownToBuffer(span, trackingMode, targetSnapshot.TextBuffer);
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

        public NormalizedSnapshotSpanCollection MapUpToSnapshot(SnapshotSpan span, SpanTrackingMode trackingMode, ITextSnapshot targetSnapshot)
        {
            if (targetSnapshot == null)
            {
                throw new ArgumentNullException("targetSnapshot");
            }

            NormalizedSnapshotSpanCollection results = MapUpToBuffer(span, trackingMode, targetSnapshot.TextBuffer);
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

        private static FrugalList<SnapshotSpan> MapDownOneLevel(FrugalList<SnapshotSpan> inputSpans, Predicate<ITextSnapshot> match, ref ITextSnapshot chosenSnapshot, ref FrugalList<Span> targetSpans)
        {
            FrugalList<SnapshotSpan> downSpans = new FrugalList<SnapshotSpan>();
            foreach (SnapshotSpan inputSpan in inputSpans)
            {
                IProjectionBufferBase projBuffer = (IProjectionBufferBase)inputSpan.Snapshot.TextBuffer;
                IProjectionSnapshot projSnap = projBuffer.CurrentSnapshot;
                if (projSnap.SourceSnapshots.Count > 0)
                {
                    IList<SnapshotSpan> mappedSpans = projSnap.MapToSourceSnapshots(inputSpan);
                    for (int s = 0; s < mappedSpans.Count; ++s)
                    {
                        SnapshotSpan mappedSpan = mappedSpans[s];
                        ITextBuffer mappedBuffer = mappedSpan.Snapshot.TextBuffer;
                        if (mappedBuffer.CurrentSnapshot == chosenSnapshot)
                        {
                            targetSpans.Add(mappedSpan.Span);
                        }
                        else if (chosenSnapshot == null && match(mappedBuffer.CurrentSnapshot))
                        {
                            chosenSnapshot = mappedBuffer.CurrentSnapshot;
                            targetSpans.Add(mappedSpan.Span);
                        }
                        else
                        {
                            IProjectionBufferBase mappedProjBuffer = mappedBuffer as IProjectionBufferBase;
                            if (mappedProjBuffer != null)
                            {
                                downSpans.Add(mappedSpan);
                            }
                        }
                    }
                }
            }
            return downSpans;
        }

        public NormalizedSnapshotSpanCollection MapUpToBuffer(SnapshotSpan span, SpanTrackingMode trackingMode, ITextBuffer targetBuffer)
        {
            if (!this.importingProjectionBufferMap.ContainsKey(targetBuffer))
            {
                return NormalizedSnapshotSpanCollection.Empty;
            }
            else
            {
                return CheckedMapUpToBuffer(span, trackingMode, snapshot => (snapshot.TextBuffer == targetBuffer));
            }
        }

        public NormalizedSnapshotSpanCollection MapUpToFirstMatch(SnapshotSpan span, SpanTrackingMode trackingMode, Predicate<ITextSnapshot> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException("match");
            }
            return CheckedMapUpToBuffer(span, trackingMode, match);
        }

        public NormalizedSnapshotSpanCollection CheckedMapUpToBuffer(SnapshotSpan span, SpanTrackingMode trackingMode, Predicate<ITextSnapshot> match)
        {
            if (span.Snapshot == null)
            {
                throw new ArgumentNullException("span");
            }
            if (trackingMode < SpanTrackingMode.EdgeExclusive || trackingMode > SpanTrackingMode.EdgeNegative)
            {
                throw new ArgumentOutOfRangeException("trackingMode");
            }
            ITextBuffer buffer = span.Snapshot.TextBuffer;
            if (!this.importingProjectionBufferMap.ContainsKey(buffer))
            {
                return NormalizedSnapshotSpanCollection.Empty;
            }
            SnapshotSpan currentSpan = span.TranslateTo(buffer.CurrentSnapshot, trackingMode);
            if (match(buffer.CurrentSnapshot))
            {
                return new NormalizedSnapshotSpanCollection(currentSpan);
            }

            ITextSnapshot chosenSnapshot = null;
            FrugalList<Span> result = new FrugalList<Span>();
            FrugalList<SnapshotSpan> spans = new FrugalList<SnapshotSpan>() { currentSpan };
            do
            {
                spans = MapUpOneLevel(spans, ref chosenSnapshot, match, result);
            } while (spans.Count > 0);

            if (chosenSnapshot == null)
            {
                return NormalizedSnapshotSpanCollection.Empty;
            }
            else
            {
                return new NormalizedSnapshotSpanCollection(chosenSnapshot, result);
            }
        }

        private FrugalList<SnapshotSpan> MapUpOneLevel(FrugalList<SnapshotSpan> spans, ref ITextSnapshot chosenSnapshot, Predicate<ITextSnapshot> match, FrugalList<Span> topSpans)
        {
            FrugalList<SnapshotSpan> upSpans = new FrugalList<SnapshotSpan>();
            foreach (SnapshotSpan span in spans)
            {
                FrugalList<IProjectionBufferBase> targetBuffers;
                if (this.importingProjectionBufferMap.TryGetValue(span.Snapshot.TextBuffer, out targetBuffers))
                {
                    if (targetBuffers != null)  // make sure we don't fall off the top
                    {
                        foreach (IProjectionBufferBase projBuffer in targetBuffers)
                        {
                            IList<Span> projSpans = projBuffer.CurrentSnapshot.MapFromSourceSnapshot(span);
                            if (projBuffer.CurrentSnapshot == chosenSnapshot)
                            {
                                topSpans.AddRange(projSpans);
                            }
                            else if (chosenSnapshot == null && match(projBuffer.CurrentSnapshot))
                            {
                                chosenSnapshot = projBuffer.CurrentSnapshot;
                                topSpans.AddRange(projSpans);
                            }
                            else
                            {
                                foreach (Span projSpan in projSpans)
                                {
                                    upSpans.Add(new SnapshotSpan(projBuffer.CurrentSnapshot, projSpan));
                                }
                            }
                        }
                    }
                }
            }
            return upSpans;
        }
        #endregion

        #region Event Handling
        private class GraphEventRaiser : BaseBuffer.ITextEventRaiser
        {
            private BufferGraph graph;
            private GraphBuffersChangedEventArgs args;

            public GraphEventRaiser(BufferGraph graph, GraphBuffersChangedEventArgs args)
            {
                this.graph = graph;
                this.args = args;
            }

            public void RaiseEvent(BaseBuffer baseBuffer, bool immediate)
            {
                this.graph.RaiseGraphBuffersChanged(this.args);
            }

            public bool HasPostEvent
            {
                get { return false; }
            }
        }

        public void RaiseGraphBuffersChanged(GraphBuffersChangedEventArgs args)
        {
            var listeners = GraphBuffersChanged;
            if (listeners != null)
            {
                this.guardedOperations.RaiseEvent(this, listeners, args);
            }
        }

        private void SourceBuffersChanged(object sender, ProjectionSourceBuffersChangedEventArgs e)
        {
            IProjectionBufferBase projBuffer = (IProjectionBufferBase)sender;
            FrugalList<ITextBuffer> addedToGraphBuffers = new FrugalList<ITextBuffer>();
            FrugalList<ITextBuffer> removedFromGraphBuffers = new FrugalList<ITextBuffer>();

            foreach (ITextBuffer addedBuffer in e.AddedBuffers)
            {
                AddSourceBuffer(projBuffer, addedBuffer, addedToGraphBuffers);
            }

            foreach (ITextBuffer removedBuffer in e.RemovedBuffers)
            {
                RemoveSourceBuffer(projBuffer, removedBuffer, removedFromGraphBuffers);
            }

            if (addedToGraphBuffers.Count > 0 || removedFromGraphBuffers.Count > 0)
            {
                var listeners = GraphBuffersChanged;
                if (listeners != null)
                {
                    ((BaseBuffer)projBuffer).group.EnqueueEvents
                        (new GraphEventRaiser(this, new GraphBuffersChangedEventArgs(addedToGraphBuffers, removedFromGraphBuffers)), null);
                }
            }
        }

        private void AddSourceBuffer(IProjectionBufferBase projBuffer, ITextBuffer sourceBuffer, FrugalList<ITextBuffer> addedToGraphBuffers)
        {
            bool firstEncounter = false;
            FrugalList<IProjectionBufferBase> importingProjectionBuffers;
            if (!this.importingProjectionBufferMap.TryGetValue(sourceBuffer, out importingProjectionBuffers))
            {
                // sourceBuffer is new to this buffer graph
                addedToGraphBuffers.Add(sourceBuffer);
                firstEncounter = true;

                importingProjectionBuffers = new FrugalList<IProjectionBufferBase>();
                this.importingProjectionBufferMap.Add(sourceBuffer, importingProjectionBuffers);

                this.eventHooks.Add(new WeakEventHookForBufferGraph(this, sourceBuffer));
            }
            importingProjectionBuffers.Add(projBuffer);

            if (firstEncounter)
            {
                IProjectionBufferBase addedProjBufferBase = sourceBuffer as IProjectionBufferBase;
                if (addedProjBufferBase != null)
                {
                    foreach (ITextBuffer furtherSourceBuffer in addedProjBufferBase.SourceBuffers)
                    {
                        AddSourceBuffer(addedProjBufferBase, furtherSourceBuffer, addedToGraphBuffers);
                    }
                }
            }
        }

        private void RemoveSourceBuffer(IProjectionBufferBase projBuffer, ITextBuffer sourceBuffer, FrugalList<ITextBuffer> removedFromGraphBuffers)
        {
            IList<IProjectionBufferBase> importingProjectionBuffers = this.importingProjectionBufferMap[sourceBuffer];
            importingProjectionBuffers.Remove(projBuffer);
            if (importingProjectionBuffers.Count == 0)
            {
                removedFromGraphBuffers.Add(sourceBuffer);
                this.importingProjectionBufferMap.Remove(sourceBuffer);

                for (int i = 0; (i < this.eventHooks.Count); ++i)
                {
                    if (this.eventHooks[i].SourceBuffer == sourceBuffer)
                    {
                        this.eventHooks[i].UnsubscribeFromSourceBuffer();
                        this.eventHooks.RemoveAt(i);
                        break;
                    }
                }

                IProjectionBufferBase removedProjBufferBase = sourceBuffer as IProjectionBufferBase;
                if (removedProjBufferBase != null)
                {
                    foreach (ITextBuffer furtherSourceBuffer in removedProjBufferBase.SourceBuffers)
                    {
                        RemoveSourceBuffer(removedProjBufferBase, furtherSourceBuffer, removedFromGraphBuffers);
                    }
                }
            }
        }

        protected void ContentTypeChanged(object sender, ContentTypeChangedEventArgs args)
        {
            // we do not subscribe to the immediate form of the sender's content type changed
            // event, so we do not need to queue this event
            var handler = GraphBufferContentTypeChanged;
            if (handler != null)
            {
                handler(this, new GraphBufferContentTypeChangedEventArgs((ITextBuffer)sender, args.BeforeContentType, args.AfterContentType));
            }
        }

        public event EventHandler<GraphBuffersChangedEventArgs> GraphBuffersChanged;
        public event EventHandler<GraphBufferContentTypeChangedEventArgs> GraphBufferContentTypeChanged;

        #endregion

        /// <summary>
        /// This an equivalent of the WeakEventHook, but for the buffer graph instead of being for a projection buffer.
        /// </summary>
        internal class WeakEventHookForBufferGraph
        {
            private readonly WeakReference<BufferGraph> _targetGraph;
            private ITextBuffer _sourceBuffer;

            public WeakEventHookForBufferGraph(BufferGraph targetGraph, ITextBuffer sourceBuffer)
            {
                _targetGraph = new WeakReference<BufferGraph>(targetGraph);
                _sourceBuffer = sourceBuffer;

                sourceBuffer.ContentTypeChanged += OnSourceBufferContentTypeChanged;
                ProjectionBuffer projectionBuffer = sourceBuffer as ProjectionBuffer;
                if (projectionBuffer != null)
                {
                    projectionBuffer.SourceBuffersChangedImmediate += OnSourceBuffersChanged;
                }
            }

            public ITextBuffer SourceBuffer { get { return _sourceBuffer; } }

            public BufferGraph GetTargetGraph()  // Not a property since it has side-effects
            {
                BufferGraph targetGraph;
                if (_targetGraph.TryGetTarget(out targetGraph))
                {
                    return targetGraph;
                }

                // The target buffer that was listening to events on the source buffer has died (no one was using it).
                // Dead buffers tell no tales so they get to stop listening to tales as well. Unsubscribe from the
                // events it hooked on the source buffer.
                this.UnsubscribeFromSourceBuffer();
                return null;
            }

            private void OnSourceBufferContentTypeChanged(object sender, ContentTypeChangedEventArgs e)
            {
                BufferGraph targetGraph = this.GetTargetGraph();
                if (targetGraph != null)
                {
                    targetGraph.ContentTypeChanged(sender, e);
                }
            }

            private void OnSourceBuffersChanged(object sender, ProjectionSourceBuffersChangedEventArgs e)
            {
                BufferGraph targetGraph = this.GetTargetGraph();
                if (targetGraph != null)
                {
                    targetGraph.SourceBuffersChanged(sender, e);
                }
            }

            public void UnsubscribeFromSourceBuffer()
            {
                if (_sourceBuffer != null)
                {
                    _sourceBuffer.ContentTypeChanged -= OnSourceBufferContentTypeChanged;
                    ProjectionBuffer projectionBuffer = _sourceBuffer as ProjectionBuffer;
                    if (projectionBuffer != null)
                    {
                        projectionBuffer.SourceBuffersChangedImmediate -= OnSourceBuffersChanged;
                    }

                    _sourceBuffer = null;
                }
            }
        }
    }
}
