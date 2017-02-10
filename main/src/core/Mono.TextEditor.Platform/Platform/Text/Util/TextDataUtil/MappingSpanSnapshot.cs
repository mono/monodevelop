// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Utilities
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.Text.Projection;

    internal class MappingSpanSnapshot : IMappingSpan
    {
        private ITextSnapshot _root;
        private SnapshotSpan _anchor;
        private SpanTrackingMode _trackingMode;
        private IBufferGraph _graph;
        private bool _unmappable = false;

        public static IMappingSpan Create(ITextSnapshot root, SnapshotSpan anchor, SpanTrackingMode trackingMode, IBufferGraph graph)
        {
            return new MappingSpanSnapshot(root, anchor, trackingMode, graph);
        }

        private MappingSpanSnapshot(ITextSnapshot root, SnapshotSpan anchor, SpanTrackingMode trackingMode, IBufferGraph graph)
        {
            //Anchor and root are expected to be from concurrent snapshots
            ITextSnapshot correspondingAnchorSnapshot = MappingHelper.FindCorrespondingSnapshot(root, anchor.Snapshot.TextBuffer);

            _root = root;
            if (correspondingAnchorSnapshot != null)
                _anchor = anchor.TranslateTo(correspondingAnchorSnapshot, trackingMode);
            else
            {
                _anchor = anchor;
                _unmappable = true;
            }
            _trackingMode = trackingMode;
            _graph = graph;
        }

        public NormalizedSnapshotSpanCollection GetSpans(ITextBuffer targetBuffer)
        {
            if (targetBuffer == null)
                throw new ArgumentNullException("targetBuffer");

            if (_unmappable)
                return NormalizedSnapshotSpanCollection.Empty;

            if (targetBuffer.Properties.ContainsProperty("IdentityMapping"))
            {
                // text buffer properties uses the hybrid dictionary, which requires TWO lookups to determine that
                // a key is not present. Since this test usually fails, do it the hard way (the second lookup shows up
                // in scrolling profiles).
                ITextBuffer doppelganger = (ITextBuffer)targetBuffer.Properties["IdentityMapping"];
                if (doppelganger == _anchor.Snapshot.TextBuffer)
                {
                    // We are mapping up from a doppelganger buffer; the coordinates will be the same. We
                    // just need to figure out the right snapshot.
                    ITextSnapshot targetSnapshot = MappingHelper.FindCorrespondingSnapshot(_root, targetBuffer);
                    return new NormalizedSnapshotSpanCollection(new SnapshotSpan(targetSnapshot, _anchor.Span));
                }
            }

            //Try mapping down first.
            FrugalList<SnapshotSpan> mappedSpans = new FrugalList<SnapshotSpan>();
            MappingHelper.MapDownToBufferNoTrack(_anchor, targetBuffer, mappedSpans);
            if (mappedSpans.Count == 0)
            {
                //Unable to map down ... try mapping up.
                this.MapUpToBufferNoTrack(targetBuffer, mappedSpans);
            }

            return new NormalizedSnapshotSpanCollection(mappedSpans);
        }

        public NormalizedSnapshotSpanCollection GetSpans(ITextSnapshot targetSnapshot)
        {
            if (targetSnapshot == null)
                throw new ArgumentNullException("targetSnapshot");
            if (_unmappable)
                return NormalizedSnapshotSpanCollection.Empty;

            NormalizedSnapshotSpanCollection results = this.GetSpans(targetSnapshot.TextBuffer);

            if ((results.Count > 0) && (results[0].Snapshot != targetSnapshot))
            {
                FrugalList<SnapshotSpan> translatedSpans = new FrugalList<SnapshotSpan>();
                foreach (SnapshotSpan s in results)
                {
                    translatedSpans.Add(s.TranslateTo(targetSnapshot, _trackingMode));
                }

                results = new NormalizedSnapshotSpanCollection(translatedSpans);
            }

            return results;
        }

        public NormalizedSnapshotSpanCollection GetSpans(Predicate<ITextBuffer> match)
        {
            if (_unmappable)
                return NormalizedSnapshotSpanCollection.Empty;

            //Try mapping down first.
            FrugalList<SnapshotSpan> mappedSpans = new FrugalList<SnapshotSpan>();
            MappingHelper.MapDownToFirstMatchNoTrack(_anchor, match, mappedSpans);
            if (mappedSpans.Count == 0)
            {
                //Unable to map down ... try mapping up.
                this.MapUpToBufferNoTrack(match, mappedSpans);
            }

            return new NormalizedSnapshotSpanCollection(mappedSpans);
        }

        public IMappingPoint Start
        {
            get
            {
                return MappingPointSnapshot.Create(_root, _anchor.Start, 
                                                   (_trackingMode == SpanTrackingMode.EdgeInclusive ||
                                                    _trackingMode == SpanTrackingMode.EdgeNegative)
                                                   ? PointTrackingMode.Negative
                                                   : PointTrackingMode.Positive,
                                                   _graph);
            }
        }

        public IMappingPoint End
        {
            get
            {
                return MappingPointSnapshot.Create(_root, _anchor.End,
                                                   (_trackingMode == SpanTrackingMode.EdgeExclusive ||
                                                    _trackingMode == SpanTrackingMode.EdgeNegative)
                                                   ? PointTrackingMode.Negative
                                                   : PointTrackingMode.Positive,
                                                   _graph);
            }
        }

        public ITextBuffer AnchorBuffer
        {
            get { return _anchor.Snapshot.TextBuffer; }
        }

        public IBufferGraph BufferGraph
        {
            get { return _graph; }
        }

        private void MapUpToBufferNoTrack(ITextBuffer targetBuffer, FrugalList<SnapshotSpan> mappedSpans)
        {
            ITextSnapshot targetSnapshot = MappingHelper.FindCorrespondingSnapshot(_root, targetBuffer);
            if (targetSnapshot != null)
            {
                //Map _anchor up to targetSnapshot (they should be concurrent snapshots)
                MapUpToSnapshotNoTrack(targetSnapshot, mappedSpans);
            }
        }

        private void MapUpToBufferNoTrack(Predicate<ITextBuffer> match, FrugalList<SnapshotSpan> mappedSpans)
        {
            ITextSnapshot targetSnapshot = MappingHelper.FindCorrespondingSnapshot(_root, match);
            if (targetSnapshot != null)
            {
                //Map _anchor up to targetSnapshot (they should be concurrent snapshots)
                MapUpToSnapshotNoTrack(targetSnapshot, mappedSpans);
            }
        }

        private void MapUpToSnapshotNoTrack(ITextSnapshot targetSnapshot, FrugalList<SnapshotSpan> mappedSpans)
        {
            MappingSpanSnapshot.MapUpToSnapshotNoTrack(targetSnapshot, _anchor, mappedSpans);
        }

        public static void MapUpToSnapshotNoTrack(ITextSnapshot targetSnapshot, SnapshotSpan anchor, IList<SnapshotSpan> mappedSpans)
        {
            if (anchor.Snapshot == targetSnapshot)
                mappedSpans.Add(anchor);
            else
            {
                IProjectionSnapshot targetAsProjection = targetSnapshot as IProjectionSnapshot;
                if (targetAsProjection != null)
                {
                    var sourceSnapshots = targetAsProjection.SourceSnapshots;
                    for (int s = 0; s < sourceSnapshots.Count; ++s)
                    {
                        FrugalList<SnapshotSpan> downSpans = new FrugalList<SnapshotSpan>();
                        MapUpToSnapshotNoTrack(sourceSnapshots[s], anchor, downSpans);
                        for (int ds = 0; ds < downSpans.Count; ++ds)
                        {
                            var upSpans = targetAsProjection.MapFromSourceSnapshot(downSpans[ds]);
                            for (int us = 0; us < upSpans.Count; ++us)
                            {
                                mappedSpans.Add(new SnapshotSpan(targetSnapshot, upSpans[us]));
                            }
                        }
                    }
                }
            }
        }

        public override string ToString()
        {
            return String.Format("MappingSpanSnapshot anchored at {0}", _anchor);
        }
    }
}
