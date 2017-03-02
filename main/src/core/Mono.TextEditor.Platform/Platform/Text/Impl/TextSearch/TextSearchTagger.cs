//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Find.Implementation
{
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Operations;
    using Microsoft.VisualStudio.Text.Tagging;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A general tagger that takes a search term and tags all matching occurences of it.
    /// </summary>
    /// <remarks>
    /// This tagger -- like most others -- will not raise a TagsChanged event when the buffer changes.
    /// </remarks>
    class TextSearchTagger<T> : ITextSearchTagger<T> where T : ITag
    {
        // search service to use for doing the real search
        ITextSearchService2 _searchService;

        // list of items to search for and tag
        internal IList<BackgroundSearch<T>> _searchTerms = new List<BackgroundSearch<T>>();

        // buffer over which search is being performed
        ITextBuffer _buffer;

        public TextSearchTagger(ITextSearchService2 searchService, ITextBuffer buffer)
        {
            _searchService = searchService;
            _buffer = buffer;
        }

        #region Private Helpers

        private void InvalidateTags(SnapshotSpan span)
        {
            EventHandler<SnapshotSpanEventArgs> tagsChangedListeners = this.TagsChanged;

            if (tagsChangedListeners != null)
            {
                tagsChangedListeners.Invoke(this, new SnapshotSpanEventArgs(span));
            }
        }

        private void InvalidateTags()
        {
            this.InvalidateTags(new SnapshotSpan(_buffer.CurrentSnapshot, 0, _buffer.CurrentSnapshot.Length));
        }

        internal void ResultsCalculated(ITextSnapshot snapshot, NormalizedSpanCollection spans)
        {
            if (spans.Count > 0)
            {
                SnapshotSpan changedSpan = new SnapshotSpan(snapshot, Span.FromBounds(spans[0].Start, spans[spans.Count - 1].End));
                this.InvalidateTags(changedSpan);
            }
        }

        #endregion

        #region ITextSearchTagger<T> Members

        private NormalizedSnapshotSpanCollection _searchSpans;
        public NormalizedSnapshotSpanCollection SearchSpans
        {
            get
            {
                return _searchSpans;
            }
            set
            {
                if (value != null)
                {
                    if (value.Count == 0)
                    {
                        //Treat an empty collection as if it were null.
                        value = null;
                    }
                    else if (value[0].Snapshot.TextBuffer != _buffer)
                    {
                        throw new ArgumentException("The provided SearchSpan value must belong to the same buffer as the tagger itself.");
                    }
                }

                if (value == _searchSpans)
                {
                    return;
                }

                _searchSpans = value;

                this.InvalidateTags();
            }
        }

        public void TagTerm(string searchTerm, FindOptions searchOptions, Func<SnapshotSpan, T> tagFactory)
        {
            if ((searchOptions & FindOptions.SearchReverse) == FindOptions.SearchReverse)
            {
                throw new ArgumentException("FindOptions.SearchReverse is invalid as searches are performed forwards to ensure all matches in a requested search span are found.", "searchOptions");
            }

            if ((searchOptions & FindOptions.Wrap) == FindOptions.Wrap)
            {
                throw new ArgumentException("FindOptions.Wrap is invalid as searches are performed forwards with no wrapping to ensure all matches in a requested span are found.", "searchOptions");
            }

            _searchTerms.Add(new BackgroundSearch<T>(_searchService, _buffer, searchTerm, searchOptions, tagFactory, this.ResultsCalculated));

            this.InvalidateTags();
        }

        public void ClearTags()
        {
            if (_searchTerms.Count == 0)
            {
                return;
            }

            ITextSnapshot snapshot = _buffer.CurrentSnapshot;
            int start = int.MaxValue;
            int end = int.MinValue;

            foreach (BackgroundSearch<T> search in _searchTerms)
            {
                // Abort any ongoing background search operation since we no longer are interested in the results
                NormalizedSnapshotSpanCollection results = search.Results;

                if ((results != null) && (results.Count > 0))
                {
                    int s = results[0].Start.TranslateTo(snapshot, PointTrackingMode.Negative);
                    if (s < start)
                        start = s;

                    int e = results[results.Count - 1].End.TranslateTo(snapshot, PointTrackingMode.Positive);
                    if (e > end)
                        end = e;
                }

                search.Dispose();
            }

            // Clear all currently tagging search terms
            _searchTerms.Clear();

            // Notify listeners of changed tags over the span where we had any results.
            if (start < end)
                this.InvalidateTags(new SnapshotSpan(snapshot, start, end - start));
        }

        #endregion

        #region ITagger<T> Members

        public IEnumerable<ITagSpan<T>> GetTags(NormalizedSnapshotSpanCollection requestedSpans)
        {
            //We should always be called with a non-empty span.
            if (requestedSpans != null && requestedSpans.Count > 0)
            {
                ITextSnapshot searchSnapshot = _buffer.CurrentSnapshot;
                requestedSpans = new NormalizedSnapshotSpanCollection(searchSnapshot, TextSearchNavigator.TranslateTo(requestedSpans[0].Snapshot, requestedSpans, searchSnapshot));

                if ((_searchSpans != null) && (_searchSpans.Count > 0))
                {
                    //The search has been narrowed via _searchSpan ... limit the request to the search range (after making sure it is on the correct snapshot).
                    if (_searchSpans[0].Snapshot != searchSnapshot)
                    {
                        NormalizedSpanCollection newSpans = TextSearchNavigator.TranslateTo(_searchSpans[0].Snapshot, _searchSpans, searchSnapshot);
                        _searchSpans = new NormalizedSnapshotSpanCollection(searchSnapshot, newSpans);
                    }

                    requestedSpans = new NormalizedSnapshotSpanCollection(searchSnapshot, NormalizedSpanCollection.Intersection(requestedSpans, _searchSpans));

                    if (requestedSpans.Count == 0)
                    {
                        yield break;
                    }
                }

                foreach (var search in _searchTerms)
                {
                    //Queue up a search if we need one.
                    search.QueueSearch(requestedSpans);

                    //Report any results from the search (if we've got them)
                    var results = search.Results;
                    if (results.Count > 0) 
                    {
                        //Results could be on an old snapshot (and, if so, a new search has already been queued up) but we need to get the results on the current snapshot.
                        if (results[0].Snapshot != searchSnapshot)
                        {
                            results = new NormalizedSnapshotSpanCollection(searchSnapshot, TextSearchNavigator.TranslateTo(results[0].Snapshot, results, searchSnapshot));
                        }

                        if (_searchSpans != null)
                        {
                            results = new NormalizedSnapshotSpanCollection(searchSnapshot, NormalizedSpanCollection.Intersection(results, _searchSpans));
                        }

                        int start = 0;
                        foreach (var span in requestedSpans)
                        {
                            start = TextSearchTagger<T>.IndexOfContainingSpan(results, span.Start, start, false);
                            if (start >= results.Count)
                                break;      //All done.

                            int end = TextSearchTagger<T>.IndexOfContainingSpan(results, span.End, start, true);

                            while (start < end)
                            {
                                T tag = search.TagFactory.Invoke(results[start]);

                                if (tag != null)
                                {
                                    yield return new TagSpan<T>(results[start], tag);
                                }

                                start++;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Search spans from [start ... spans.Count-1] for a span that contains point.
        /// If a span does contain point, return the index + 1
        /// If a span does not contain point, return the index of the first span that starts after point (or spans.Count if there are none).
        /// </summary>
        private static int IndexOfContainingSpan(NormalizedSpanCollection spans, int point, int start, bool isEndPoint)
        {
            int lo = start;
            int hi = spans.Count;
            while (lo < hi)
            {
                int mid = (lo + hi) / 2;
                Span s = spans[mid];

                if (s.End < point)
                {
                    lo = mid + 1;
                }
                else if (s.Start > point)
                {
                    hi = mid;
                }
                else
                {
                    //We know s.Start <= point <= s.End
                    //
                    //If point is an endPoint
                    //  we want to return mid + 1 if a span ending at point overlaps s (== point != s.Start). Otherwise return mid.
                    //
                    //If point is a startPoint
                    //  we want to return mid if a span starting at point overlaps s (== point == s.End). Otherwise return mid + 1.
                    if (isEndPoint)
                    {
                        return (point != s.Start) ? (mid + 1) : mid;
                    }
                    else
                    {
                        return (point == s.End) ? (mid + 1) : mid;
                    }
                }
            }

            return lo;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        #endregion
    }
}
