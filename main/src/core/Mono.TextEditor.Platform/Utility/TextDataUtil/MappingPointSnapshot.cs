// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Utilities
{
    using System;
    using System.Diagnostics;
    using Microsoft.VisualStudio.Text.Projection;

    internal class MappingPointSnapshot : IMappingPoint
    {
        internal ITextSnapshot _root;
        internal SnapshotPoint _anchor;
        internal PointTrackingMode _trackingMode;
        IBufferGraph _graph;
        internal bool _unmappable = false;

        public static IMappingPoint Create(ITextSnapshot root, SnapshotPoint anchor, PointTrackingMode trackingMode, IBufferGraph graph)
        {
            return new MappingPointSnapshot(root, anchor, trackingMode, graph);
        }

        private MappingPointSnapshot(ITextSnapshot root, SnapshotPoint anchor, PointTrackingMode trackingMode, IBufferGraph graph)
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

        public SnapshotPoint? GetPoint(ITextBuffer targetBuffer, PositionAffinity affinity)
        {
            if (targetBuffer == null)
                throw new ArgumentNullException("targetBuffer");
            if (_unmappable)
                return null;
            
            //Try mapping down first.
            SnapshotPoint? mappedPoint = MappingHelper.MapDownToBufferNoTrack(_anchor, targetBuffer, affinity);

            if (!mappedPoint.HasValue)
            {
                //Unable to map down ... try mapping up.
                return this.MapUpToBufferNoTrack(targetBuffer, affinity);
            }

            return mappedPoint;
        }

        public SnapshotPoint? GetPoint(ITextSnapshot targetSnapshot, PositionAffinity affinity)
        {
            if (targetSnapshot == null)
                throw new ArgumentNullException("targetSnapshot");
            if (_unmappable)
                return null;

            SnapshotPoint? mappedPoint = this.GetPoint(targetSnapshot.TextBuffer, affinity);
            if (mappedPoint.HasValue && (mappedPoint.Value.Snapshot != targetSnapshot))
            {
                mappedPoint = mappedPoint.Value.TranslateTo(targetSnapshot, _trackingMode);
            }

            return mappedPoint;
        }

        public SnapshotPoint? GetPoint(Predicate<ITextBuffer> match, PositionAffinity affinity)
        {
            if (match == null)
                throw new ArgumentNullException("match");
            if (_unmappable)
                return null;
            
            //Try mapping down first.
            SnapshotPoint? mappedPoint = MappingHelper.MapDownToFirstMatchNoTrack(_anchor, match, affinity);

            if (!mappedPoint.HasValue)
            {
                //Unable to map down ... try mapping up.
                return this.MapUpToFirstMatchNoTrack(match, affinity);
            }

            return mappedPoint;
        }

        public SnapshotPoint? GetInsertionPoint(Predicate<ITextBuffer> match)
        {
            if (match == null)
                throw new ArgumentNullException("match");
            if (_unmappable)
                return null; 
            
            return MappingHelper.MapDownToFirstMatchNoTrack(_anchor, match);
        }

        public ITextBuffer AnchorBuffer
        {
            get { return _anchor.Snapshot.TextBuffer; }
        }

        public IBufferGraph BufferGraph
        {
            get { return _graph; }
        }

        private SnapshotPoint? MapUpToBufferNoTrack(ITextBuffer targetBuffer, PositionAffinity affinity)
        {
            ITextSnapshot targetSnapshot = MappingHelper.FindCorrespondingSnapshot(_root, targetBuffer);
            if (targetSnapshot != null)
            {
                //Map _anchor up to targetSnapshot (they should be concurrent snapshots)
                return MapUpToSnapshotNoTrack(targetSnapshot, affinity);
            }

            return null;
        }

        private SnapshotPoint? MapUpToFirstMatchNoTrack(Predicate<ITextBuffer> match, PositionAffinity affinity)
        {
            ITextSnapshot targetSnapshot = MappingHelper.FindCorrespondingSnapshot(_root, match);
            if (targetSnapshot != null)
            {
                //Map _anchor up to targetSnapshot (they should be concurrent snapshots)
                return MapUpToSnapshotNoTrack(targetSnapshot, affinity);
            }

            return null;
        }

        private SnapshotPoint? MapUpToSnapshotNoTrack(ITextSnapshot targetSnapshot, PositionAffinity affinity)
        {
            return MapUpToSnapshotNoTrack(targetSnapshot, _anchor, affinity);
        }

        public static SnapshotPoint? MapUpToSnapshotNoTrack(ITextSnapshot targetSnapshot, SnapshotPoint anchor, PositionAffinity affinity)
        {
            if (anchor.Snapshot == targetSnapshot)
                return anchor;
            else
            {
                IProjectionSnapshot targetAsProjection = targetSnapshot as IProjectionSnapshot;
                if (targetAsProjection != null)
                {
                    var sourceSnapshots = targetAsProjection.SourceSnapshots;
                    for (int s = 0; s < sourceSnapshots.Count; ++s)
                    {
                        SnapshotPoint? downPoint = MapUpToSnapshotNoTrack(sourceSnapshots[s], anchor, affinity);
                        if (downPoint.HasValue)
                        {
                            SnapshotPoint? result = targetAsProjection.MapFromSourceSnapshot(downPoint.Value, affinity);
                            if (result.HasValue)
                                return result;
                        }
                    }
                }
            }

            return null;
        }
    }
}
