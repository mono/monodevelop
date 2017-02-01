// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using Microsoft.VisualStudio.Text.Utilities;

    /// <summary>
    /// A collection of read only spans that are sorted by start position, with adjacent and overlapping spans combined.
    /// </summary>
    /// <remarks>
    ///  <para>
    /// If two snapshots have the same read only regions, then they will have the same read only span collection.
    /// </para>
    /// <para>
    /// Asking a ReadOnlySpanCollection if it intersects an ITrackingSpan or ITrackingPoint with a snapshot before
    /// the first one the ReadOnlySpanCollection was created with is undefined.
    /// </para>
    /// </remarks>
    internal class ReadOnlySpanCollection : ReadOnlyCollection<ReadOnlySpan>
    {
        readonly List<IReadOnlyRegion> regionsWithActions;

        internal IEnumerable<ReadOnlySpan> QueryAllEffectiveReadOnlySpans(ITextVersion version)
        {
            foreach (var span in this)
                yield return span;

            foreach (var regionWithAction in this.regionsWithActions)
                if (regionWithAction.QueryCallback(isEdit: false))
                    yield return new ReadOnlySpan(version, regionWithAction);
        }

        /// <summary>
        /// Construct a ReadOnlySpanCollection that contains everything in a list of spans.
        /// </summary>
        /// <param name="regions">ReadOnlyRegions</param>
        /// <param name="version">The version this span collection applies to.</param>
        /// <remarks>
        /// <para>The list of spans will be sorted and normalized (overlapping and adjoining spans will be combined).</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="regions"/> is null.</exception>
        internal ReadOnlySpanCollection(TextVersion version, IEnumerable<IReadOnlyRegion> regions)
            : base(NormalizeSpans(version, regions))
        {
            regionsWithActions = regions.Where(region => region.QueryCallback != null).ToList();
        }

        internal bool IsReadOnly(int position, ITextSnapshot textSnapshot, bool notify)
        {
            foreach (var region in this.regionsWithActions)
            {
                if (!IsEditAllowed(region, position, textSnapshot))
                {
                    if (region.QueryCallback(notify))
                        return true;
                }
            }

            // O(n) implementation which is much more straightforward
            for (int i = 0; i < this.Count; i++)
            {
                if (!this[i].IsInsertAllowed(position, textSnapshot))
                {
                    return true;
                }
            }

            return false;

            // This is the O(lg n) implementation
            #region Binary search implementation
            /*
            if (this.Count == 0)
            {
                return false;
            }

            // We know that the spans don't overlap, so we can do a binary search here.
            int start = this.Count / 2;
            ReadOnlySpan startSpan = this[start];
            while (true)
            {
                Span rawSpan = startSpan.GetSpan(textSnapshot);
                int newStart = start;
                if (rawSpan.End < position)
                {
                    newStart = start + 1 + start / 2;
                }
                else if (rawSpan.Start > position)
                {
                    newStart = start / 2;
                }
                else
                {
                    break;
                }

                if (newStart == start)
                {
                    Debug.Assert(!startSpan.GetSpan(textSnapshot).Contains(position));
                    return false;
                }

                if (newStart < this.Count)
                {
                    start = newStart;
                    startSpan = this[start];
                }
                else
                {
                    break;
                }
            }

            if (!startSpan.IsInsertAllowed(position, textSnapshot))
            {
                return true;
            }

            if (start > 0)
            {
                if (!this[start - 1].IsInsertAllowed(position, textSnapshot))
                {
                    return true;
                }
            }

            if (start < this.Count - 1)
            {
                return !this[start + 1].IsInsertAllowed(position, textSnapshot);
            }

            return false;
             * */
            #endregion
        }

        private static bool IsEditAllowed(IReadOnlyRegion region, int position, ITextSnapshot textSnapshot)
        {
            return new ReadOnlySpan(textSnapshot.Version, region).IsInsertAllowed(position, textSnapshot);
        }

        private static bool IsEditAllowed(IReadOnlyRegion region, Span span, ITextSnapshot textSnapshot)
        {
            return new ReadOnlySpan(textSnapshot.Version, region).IsReplaceAllowed(span, textSnapshot);
        }

        internal bool IsReadOnly(Span span, ITextSnapshot textSnapshot, bool notify)
        {
            foreach (var region in this.regionsWithActions)
            {
                if (!IsEditAllowed(region, span, textSnapshot))
                {
                    if (region.QueryCallback(notify))
                        return true;
                }
            }

            // O(n) implementation which is much more straightforward
            for (int i = 0; i < this.Count; i++)
            {
                if (!this[i].IsReplaceAllowed(span, textSnapshot))
                {
                    return true;
                }
            }

            return false;

            // This is the O(lg n) implementation
            #region Binary search implementation

            /*
            if (this.Count == 0)
            {
                return false;
            }

            // We know that the spans don't overlap, so we can do a binary search here.
            int start = this.Count / 2;
            ReadOnlySpan startSpan = this[start];
            while (true)
            {
                Span rawSpan = startSpan.GetSpan(textSnapshot);
                int newStart = start;
                if (rawSpan.End < span.Start)
                {
                    newStart = start + 1 + start / 2;
                }
                else if (rawSpan.Start > span.End)
                {
                    newStart = start / 2;
                }
                else
                {
                    break;
                }

                if (newStart == start)
                {
                    Debug.Assert(!startSpan.GetSpan(textSnapshot).Contains(span));
                    return false;
                }

                if (newStart < this.Count)
                {
                    start = newStart;
                    startSpan = this[start];
                }
                else
                {
                    break;
                }
            }

            if (!startSpan.IsReplaceAllowed(span, textSnapshot))
            {
                return true;
            }

            if (start > 0)
            {
                if (!this[start - 1].IsReplaceAllowed(span, textSnapshot))
                {
                    return true;
                }
            }

            if (start < this.Count - 1)
            {
                return !this[start + 1].IsReplaceAllowed(span, textSnapshot);
            }

            return false;
             */
            #endregion
        }

        private static IList<ReadOnlySpan> NormalizeSpans(TextVersion version, IEnumerable<IReadOnlyRegion> regions)
        {
            List<IReadOnlyRegion> sorted = new List<IReadOnlyRegion>(regions.Where(region => region.QueryCallback == null));

            if (sorted.Count == 0)
            {
                return new FrugalList<ReadOnlySpan>();
            }
            else if (sorted.Count == 1)
            {
                return new FrugalList<ReadOnlySpan>() {new ReadOnlySpan(version, sorted[0])};
            }
            else
            {
                sorted.Sort((s1, s2) => s1.Span.GetSpan(version).Start.CompareTo(s2.Span.GetSpan(version).Start));

                List<ReadOnlySpan> normalized = new List<ReadOnlySpan>(sorted.Count);

                int oldStart = sorted[0].Span.GetSpan(version).Start;
                int oldEnd = sorted[0].Span.GetSpan(version).End;
                EdgeInsertionMode oldStartEdgeInsertionMode = sorted[0].EdgeInsertionMode;
                EdgeInsertionMode oldEndEdgeInsertionMode = sorted[0].EdgeInsertionMode;
                SpanTrackingMode oldSpanTrackingMode = sorted[0].Span.TrackingMode;
                for (int i = 1; (i < sorted.Count); ++i)
                {
                    int newStart = sorted[i].Span.GetSpan(version).Start;
                    int newEnd = sorted[i].Span.GetSpan(version).End;

                    // Since the new span's start occurs after the old span's end, we can just add the old span directly.
                    if (oldEnd < newStart)
                    {
                        normalized.Add(new ReadOnlySpan(version, new Span(oldStart, oldEnd - oldStart), oldSpanTrackingMode, oldStartEdgeInsertionMode, oldEndEdgeInsertionMode));
                        oldStart = newStart;
                        oldEnd = newEnd;
                        oldStartEdgeInsertionMode = sorted[i].EdgeInsertionMode;
                        oldEndEdgeInsertionMode = sorted[i].EdgeInsertionMode;
                        oldSpanTrackingMode = sorted[i].Span.TrackingMode;
                    }
                    else
                    {
                        // The two read only regions start at the same position
                        if (newStart == oldStart)
                        {
                            // If one read only region denies edge insertions, combined they do as well
                            if (sorted[i].EdgeInsertionMode == EdgeInsertionMode.Deny)
                            {
                                oldStartEdgeInsertionMode = EdgeInsertionMode.Deny;
                            }

                            // This is tricky. We want one span that will be inclusive tracking, and one that won't.
                            if (oldSpanTrackingMode != sorted[i].Span.TrackingMode)
                            {
                                // Since the read only regions cover the same exact span, the combined one will be edge inclusive tracking
                                if (oldEnd == newEnd)
                                {
                                    oldSpanTrackingMode = SpanTrackingMode.EdgeInclusive;
                                }
                                else if (oldEnd < newEnd)
                                {
                                    // Since the old span and new span don't have the same span tracking mode and don't end in the same position, we need to create a new span that is edge inclusive
                                    // and deny inserts between it and the next span.
                                    normalized.Add(new ReadOnlySpan(version, new Span(oldStart, oldEnd - oldStart), SpanTrackingMode.EdgeInclusive, oldStartEdgeInsertionMode, EdgeInsertionMode.Deny));
                                    oldStart = oldEnd; // Explicitly use the old end here since we want these spans to be adjacent
                                    oldEnd = newEnd;
                                    oldStartEdgeInsertionMode = sorted[i].EdgeInsertionMode;
                                    oldEndEdgeInsertionMode = sorted[i].EdgeInsertionMode;
                                    oldSpanTrackingMode = sorted[i].Span.TrackingMode;
                                }
                                else
                                {
                                    // Since the new span ends first, create a span that is edge inclusive tracking that ends at the the new span's end.
                                    normalized.Add(new ReadOnlySpan(version, new Span(newStart, newEnd - newStart), SpanTrackingMode.EdgeInclusive, oldStartEdgeInsertionMode, EdgeInsertionMode.Deny));
                                    oldStart = newEnd; // Explicitly use the new end here since we want these spans to be adjacent
                                }
                            }
                        }

                        if (oldEnd < newEnd)
                        {
                            // If the tracking modes are different then we need to create a new span
                            // with the old tracking mode, and start a new span with the new span tracking mode.
                            // Also, if the old end and the new start are identical and both edge insertion mode's
                            // are allow, then we need to create a new span.
                            if (((oldEnd == newStart)
                                    &&
                                 ((oldEndEdgeInsertionMode == EdgeInsertionMode.Allow) && (sorted[i].EdgeInsertionMode == EdgeInsertionMode.Allow)))
                                ||
                                (oldSpanTrackingMode != sorted[i].Span.TrackingMode))
                            {
                                normalized.Add(new ReadOnlySpan(version, new Span(oldStart, oldEnd - oldStart), oldSpanTrackingMode, oldStartEdgeInsertionMode, oldEndEdgeInsertionMode));
                                oldStart = oldEnd; // Explicitly use the old end here since we want these spans to be adjacent.
                                oldEnd = newEnd;

                                // If we are splitting up the spans because of a change in tracking mode, then explicitly deny inserting between them
                                if (oldSpanTrackingMode != sorted[i].Span.TrackingMode)
                                {
                                    oldStartEdgeInsertionMode = EdgeInsertionMode.Deny; // Explicitly use deny here since we don't want to allow insertions between these spans
                                }
                                else
                                {
                                    oldStartEdgeInsertionMode = EdgeInsertionMode.Allow;
                                }
                                oldEndEdgeInsertionMode = sorted[i].EdgeInsertionMode;
                                oldSpanTrackingMode = sorted[i].Span.TrackingMode;
                            }
                            else
                            {
                                oldEnd = newEnd;
                                oldEndEdgeInsertionMode = sorted[i].EdgeInsertionMode;
                            }
                        }
                        else if (oldEnd == newEnd)
                        {
                            if (sorted[i].EdgeInsertionMode == EdgeInsertionMode.Deny)
                            {
                                oldEndEdgeInsertionMode = EdgeInsertionMode.Deny;
                            }
                            if (oldSpanTrackingMode != sorted[i].Span.TrackingMode)
                            {
                                normalized.Add(new ReadOnlySpan(version, new Span(oldStart, oldEnd - oldStart), oldSpanTrackingMode, oldStartEdgeInsertionMode, oldEndEdgeInsertionMode));
                                oldStart = newEnd;
                                oldEnd = newEnd;
                                oldStartEdgeInsertionMode = sorted[i].EdgeInsertionMode;
                                oldEndEdgeInsertionMode = sorted[i].EdgeInsertionMode;
                                oldSpanTrackingMode = sorted[i].Span.TrackingMode;
                            }
                        }
                    }
                }
                normalized.Add(new ReadOnlySpan(version, new Span(oldStart, oldEnd - oldStart), oldSpanTrackingMode, oldStartEdgeInsertionMode, oldEndEdgeInsertionMode));

                return normalized;
            }
        }
    }
}
