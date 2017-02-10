// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Utilities
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.Text.Projection;

    internal static class MappingHelper
    {
        //These two methods are nearly duplicates of one another but delegates can be expensive and this is inner loop code.
        internal static ITextSnapshot FindCorrespondingSnapshot(ITextSnapshot sourceSnapshot, ITextBuffer targetBuffer)
        {
            if (sourceSnapshot.TextBuffer == targetBuffer)
            {
                // simple case: single buffer
                return sourceSnapshot;
            }
            else
            {
                IProjectionSnapshot2 projSnap = sourceSnapshot as IProjectionSnapshot2;
                if (projSnap != null)
                {
                    return projSnap.GetMatchingSnapshotInClosure(targetBuffer);
                }
                else
                {
                    return null;
                }
            }
        }

        internal static ITextSnapshot FindCorrespondingSnapshot(ITextSnapshot sourceSnapshot, Predicate<ITextBuffer> match)
        {
            if (match(sourceSnapshot.TextBuffer))
            {
                // simple case: single buffer
                return sourceSnapshot;
            }
            else
            {
                IProjectionSnapshot2 projSnap = sourceSnapshot as IProjectionSnapshot2;
                if (projSnap != null)
                {
                    return projSnap.GetMatchingSnapshotInClosure(match);
                }
                else
                {
                    return null;
                }
            }
        }

        internal static NormalizedSnapshotSpanCollection MapDownToBufferNoTrack(SnapshotSpan sourceSpan, ITextBuffer targetBuffer, bool mapByContentType = false)
        {
            FrugalList<SnapshotSpan> mappedSpans = new FrugalList<SnapshotSpan>();

            MapDownToFirstMatchNoTrack(sourceSpan, (ITextBuffer b) => { return b == targetBuffer; }, mappedSpans, mapByContentType);

            return new NormalizedSnapshotSpanCollection(mappedSpans);
        }

        internal static void MapDownToBufferNoTrack(SnapshotSpan sourceSpan, ITextBuffer targetBuffer, IList<SnapshotSpan> mappedSpans, bool mapByContentType = false)
        {
            // Most of the time, the sourceSpan will map to the targetBuffer as a single span, rather than being split.
            // Since this method is called a lot, we'll assume first that we'll get a single span and don't need to
            // allocate a stack to keep track of unmapped spans. If that fails we'll fall back on the more expensive approach.
            // Scroll around for a while and this saves a bunch of allocations.
            SnapshotSpan mappedSpan = sourceSpan;
            while (true)
            {
                if (mappedSpan.Snapshot.TextBuffer == targetBuffer)
                {
                    mappedSpans.Add(mappedSpan);
                    return;
                }
                else
                {
                    IProjectionSnapshot mappedSpanProjectionSnapshot = mappedSpan.Snapshot as IProjectionSnapshot;
                    if (mappedSpanProjectionSnapshot != null &&
                        (!mapByContentType || mappedSpanProjectionSnapshot.ContentType.IsOfType("projection")))
                    {
                        var mappedDownSpans = mappedSpanProjectionSnapshot.MapToSourceSnapshots(mappedSpan);
                        if (mappedDownSpans.Count == 1)
                        {
                            mappedSpan = mappedDownSpans[0];
                            continue;
                        }
                        else if (mappedDownSpans.Count == 0)
                        {
                            return;
                        }
                        else
                        {
                            // the projection mapping resulted in more than one span
                            FrugalList<SnapshotSpan> unmappedSpans = new FrugalList<SnapshotSpan>(mappedDownSpans);
                            SplitMapDownToBufferNoTrack(unmappedSpans, targetBuffer, mappedSpans, mapByContentType);
                            return;
                        }
                    }
                    else
                    {
                        // either it's a projection buffer we can't look through, or it's
                        // an ordinary buffer that didn't match
                        return;
                    }
                }
            }
        }

        private static void SplitMapDownToBufferNoTrack(FrugalList<SnapshotSpan> unmappedSpans, ITextBuffer targetBuffer, IList<SnapshotSpan> mappedSpans, bool mapByContentType)
        {
            while (unmappedSpans.Count > 0)
            {
                SnapshotSpan span = unmappedSpans[unmappedSpans.Count - 1];
                unmappedSpans.RemoveAt(unmappedSpans.Count - 1);

                if (span.Snapshot.TextBuffer == targetBuffer)
                {
                    mappedSpans.Add(span);
                }
                else
                {
                    IProjectionSnapshot spanSnapshotAsProjection = span.Snapshot as IProjectionSnapshot;
                    if (spanSnapshotAsProjection != null && 
                        (!mapByContentType || span.Snapshot.TextBuffer.ContentType.IsOfType("projection")))
                    {
                        unmappedSpans.AddRange(spanSnapshotAsProjection.MapToSourceSnapshots(span));
                    }
                }
            }
        }

        internal static void MapDownToFirstMatchNoTrack(SnapshotSpan sourceSpan, Predicate<ITextBuffer> match, IList<SnapshotSpan> mappedSpans, bool mapByContentType = false)
        {
            // Most of the time, the sourceSpan will map to the targetBuffer as a single span, rather than being split.
            // Since this method is called a lot, we'll assume first that we'll get a single span and don't need to
            // allocate a stack to keep track of unmapped spans. If that fails we'll fall back on the more expensive approach.
            // Scroll around for a while and this saves a bunch of allocations.
            SnapshotSpan mappedSpan = sourceSpan;
            while (true)
            {
                if (match(mappedSpan.Snapshot.TextBuffer))
                {
                    mappedSpans.Add(mappedSpan);
                    return;
                }
                else
                {
                    IProjectionSnapshot mappedSpanProjectionSnapshot = mappedSpan.Snapshot as IProjectionSnapshot;
                    if (mappedSpanProjectionSnapshot != null &&
                        (!mapByContentType || mappedSpanProjectionSnapshot.ContentType.IsOfType("projection")))
                    {
                        var mappedDownSpans = mappedSpanProjectionSnapshot.MapToSourceSnapshots(mappedSpan);
                        if (mappedDownSpans.Count == 1)
                        {
                            mappedSpan = mappedDownSpans[0];
                            continue;
                        }
                        else if (mappedDownSpans.Count == 0)
                        {
                            return;
                        }
                        else
                        {
                            // the projection mapping resulted in more than one span
                            FrugalList<SnapshotSpan> unmappedSpans = new FrugalList<SnapshotSpan>(mappedDownSpans);
                            SplitMapDownToFirstMatchNoTrack(unmappedSpans, match, mappedSpans, mapByContentType);
                            return;
                        }
                    }
                    else
                    {
                        // either it's a projection buffer we can't look through, or it's
                        // an ordinary buffer that didn't match
                        return;
                    }
                }
            }
        }

        private static void SplitMapDownToFirstMatchNoTrack(FrugalList<SnapshotSpan> unmappedSpans, Predicate<ITextBuffer> match, IList<SnapshotSpan> mappedSpans, bool mapByContentType)
        {
            ITextSnapshot matchingSnapshot = null;

            while (unmappedSpans.Count > 0)
            {
                SnapshotSpan span = unmappedSpans[unmappedSpans.Count - 1];
                unmappedSpans.RemoveAt(unmappedSpans.Count - 1);

                if (span.Snapshot == matchingSnapshot)
                {
                    mappedSpans.Add(span);
                }
                else if (match(span.Snapshot.TextBuffer))
                {
                    mappedSpans.Add(span);
                    matchingSnapshot = span.Snapshot;
                }
                else
                {
                    IProjectionSnapshot spanSnapshotAsProjection = span.Snapshot as IProjectionSnapshot;
                    if (spanSnapshotAsProjection != null &&
                        (!mapByContentType || span.Snapshot.TextBuffer.ContentType.IsOfType("projection")))
                    {
                        unmappedSpans.AddRange(spanSnapshotAsProjection.MapToSourceSnapshots(span));
                    }
                }
            }
        }

        internal static SnapshotPoint? MapDownToBufferNoTrack(SnapshotPoint position, ITextBuffer targetBuffer, PositionAffinity affinity)
        {
            while (position.Snapshot.TextBuffer != targetBuffer)
            {
                IProjectionSnapshot projSnap = position.Snapshot as IProjectionSnapshot;
                if ((projSnap == null) || (projSnap.SourceSnapshots.Count == 0))
                {
                    return null;
                }

                position = projSnap.MapToSourceSnapshot(position, affinity);
            }
            return position;
        }

        internal static SnapshotPoint? MapDownToFirstMatchNoTrack(SnapshotPoint position, Predicate<ITextBuffer> match, PositionAffinity affinity)
        {
            while (!match(position.Snapshot.TextBuffer))
            {
                IProjectionSnapshot projSnap = position.Snapshot as IProjectionSnapshot;
                if ((projSnap == null) || (projSnap.SourceSnapshots.Count == 0))
                {
                    return null;
                }

                position = projSnap.MapToSourceSnapshot(position, affinity);
            }
            return position;
        }

        internal static SnapshotPoint? MapDownToBufferNoTrack(SnapshotPoint position, ITextBuffer targetBuffer)
        {
            while (position.Snapshot.TextBuffer != targetBuffer)
            {
                IProjectionSnapshot projSnap = position.Snapshot as IProjectionSnapshot;
                if ((projSnap == null) || (projSnap.SourceSnapshots.Count == 0))
                {
                    return null;
                }

                position = projSnap.MapToSourceSnapshot(position);
            }

            return position;
        }

        internal static SnapshotPoint? MapDownToFirstMatchNoTrack(SnapshotPoint position, Predicate<ITextBuffer> match)
        {
            while (!match(position.Snapshot.TextBuffer))
            {
                IProjectionSnapshot projSnap = position.Snapshot as IProjectionSnapshot;
                if ((projSnap == null) || (projSnap.SourceSnapshots.Count == 0))
                {
                    return null;
                }

                position = projSnap.MapToSourceSnapshot(position);
            }
            return position;
        }
    }
}
