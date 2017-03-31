//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Find.Implementation
{
    using Microsoft.VisualStudio.Text.Operations;
    using Microsoft.VisualStudio.Text.Tagging;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Performs text searches on lowest priority background threads and caches the results.
    /// </summary>
    /// <remarks>
    /// The goal here is to be completely thread-safe: searches can be requested from any thread (and actually happen on background threads).
    /// 
    /// Another goal is to never search more than we need to:
    ///     Once we've searched a section of the buffer we don't search it again unless it is modified.
    ///     Even if we get multiple, nearly simultaneous requests to search a section of the buffer, we only search it once.
    /// </remarks>
    internal class BackgroundSearch<T> : IDisposable where T : ITag
    {
        private ITextBuffer _buffer;
        private readonly ITextSearchService2 _textSearchService;
        private readonly string _searchTerm;
        private readonly FindOptions _options;
        public readonly Func<SnapshotSpan, T> TagFactory;
        private readonly Action<ITextSnapshot, NormalizedSpanCollection> _callback;
        private bool _isDisposed;

        //This is used for locks so it can never be deleted/recreated. Internal for unit tests.
        internal readonly Queue<NormalizedSnapshotSpanCollection> _requestQueue = new Queue<NormalizedSnapshotSpanCollection>();

        //This needs to update atomically and is internal so unit tests can (more) easily test edge cases.
        internal SearchResults _results;

        public BackgroundSearch(ITextSearchService2 textSearchService, ITextBuffer buffer, string searchTerm, FindOptions options,
                                Func<SnapshotSpan, T> tagFactory, Action<ITextSnapshot, NormalizedSpanCollection> callback)
        {
            _textSearchService = textSearchService;
            _buffer = buffer;

            _searchTerm = searchTerm;
            _options = options & ~FindOptions.SearchReverse;    //The tagger ignores the reversed flag.
            this.TagFactory = tagFactory;
            _callback = callback;

            _results = new SearchResults(_buffer.CurrentSnapshot, NormalizedSpanCollection.Empty, NormalizedSpanCollection.Empty);
        }

        public NormalizedSnapshotSpanCollection Results
        {
            get
            {
                var results = _results;     //Snapshot results to avoid taking a lock.
                return new NormalizedSnapshotSpanCollection(results.Snapshot, results.Matches);
            }
        }

        /// <summary>
        /// Kick off a background search if we don't have current results. Do nothing otherwise.
        /// </summary>
        /// <remarks>
        /// This method can be called from any thread (though it will generally only be called from the UI thread).
        /// </remarks>
        public void QueueSearch(NormalizedSnapshotSpanCollection requestedSnapshotSpans)
        {
            Debug.Assert(requestedSnapshotSpans.Count > 0);

            //Check to see if we have completely searched the current version of the text buffer
            //and quickly abort since there is no point in queuing up another search if we have.
            var results = _results;     //Snapshot results to avoid taking a lock.
            if (results.Snapshot == _buffer.CurrentSnapshot)
            {
                if ((results.SearchedSpans.Count == 1) && (results.SearchedSpans[0].Start == 0) && (results.SearchedSpans[0].Length == results.Snapshot.Length))
                {
                    //We've searched the entire snapshot.
                    return;
                }

                if (requestedSnapshotSpans[0].Snapshot == results.Snapshot)
                {
                    NormalizedSpanCollection unsearchedRequest = NormalizedSpanCollection.Difference(requestedSnapshotSpans, results.SearchedSpans);
                    if (unsearchedRequest.Count == 0)
                    {
                        return;
                    }
                }
            }

            lock (_requestQueue)
            {
                _requestQueue.Enqueue(requestedSnapshotSpans);
                if (_requestQueue.Count != 1)
                {
                    //Request has been queued & we already have an active thread processing requests.
                    return;
                }
            }

            Task.Factory.StartNew(this.ProcessQueue, CancellationToken.None, TaskCreationOptions.PreferFairness, TaskScheduler.Default);
        }

        #region Private Helpers
        internal void ProcessQueue()
        {
            // Ensure the thread that is doing the work is both low priority and also background
            try
            {
                Thread.CurrentThread.Priority = ThreadPriority.Lowest;

                //Only one instance of this thread is running at a time, so we don't need to put locks around the bits that update
                //our state (only the bits that play with the results queue).
                while (true)
                {
                    if (_isDisposed)
                        return;

                    NormalizedSnapshotSpanCollection request;
                    lock (_requestQueue)
                    {
                        //Do not dequeue the result here ... if a new request comes in while we are processing this request,
                        //we do not want to start a new thread.
                        request = _requestQueue.Peek();
                    }

                    //Always do searches on the current snapshot of the buffer, migrating results to that snapshot
                    //if needed.
                    ITextSnapshot snapshot = this.AdvanceToCurrentSnapshot();

                    NormalizedSpanCollection requestedSpans;
                    if (_options.HasFlag(FindOptions.Multiline))
                    {
                        //Multi-line searches are all or nothing.
                        if (_results.SearchedSpans.Count == 0)
                        {
                            requestedSpans = new NormalizedSpanCollection(new Span(0, snapshot.Length));
                        }
                        else
                        {
                            Debug.Assert((_results.SearchedSpans.Count == 1) && (_results.SearchedSpans[0].Start == 0) && (_results.SearchedSpans[0].End == snapshot.Length));
                            requestedSpans = NormalizedSpanCollection.Empty;
                        }
                    }
                    else
                    {
                        requestedSpans = BackgroundSearch<T>.TranslateToAndExtend(request[0].Snapshot, request, snapshot);

                        if (_results.SearchedSpans.Count > 0)
                        {
                            if ((_results.SearchedSpans.Count == 1) && (_results.SearchedSpans[0].Start == 0) && (_results.SearchedSpans[0].End == snapshot.Length))
                            {
                                //We've already got results for the entire buffer.
                                requestedSpans = NormalizedSpanCollection.Empty;
                            }
                            else
                            {
                                requestedSpans = NormalizedSpanCollection.Difference(requestedSpans, _results.SearchedSpans);
                            }
                        }
                    }

                    bool dequeueRequest = true;
                    if (requestedSpans.Count > 0)
                    {
                        IList<Span> newMatches = this.FindAll(snapshot, requestedSpans);

                        if (_isDisposed)
                            return;

                        if (snapshot == _buffer.CurrentSnapshot)
                        {
                            //The search completed without the buffer changing out from under us, add in the new results.
                            //Remove any stale results in the places we searched (since we do not remove potentially stale results
                            //on a text change, we have to remove them here) and then add in the results we found.
                            if (_options.HasFlag(FindOptions.Multiline))
                            {
                                //Multiline searches are always whole buffer searches, so we can skip the set operations.
                                Debug.Assert(requestedSpans.Count == 1);
                                Debug.Assert(requestedSpans[0].Start == 0);
                                Debug.Assert(requestedSpans[0].Length == snapshot.Length);

                                _results = new SearchResults(snapshot,
                                                                new NormalizedSpanCollection(newMatches),
                                                                new NormalizedSpanCollection(new Span(0, snapshot.Length)));
                            }
                            else
                            {
                                //Remove the stale results.
                                NormalizedSpanCollection m = NormalizedSpanCollection.Difference(_results.Matches, requestedSpans);

                                //Add in the new results.
                                if (newMatches.Count > 0)
                                {
                                    m = NormalizedSpanCollection.Union(m, new NormalizedSpanCollection(newMatches));
                                }

                                //Save the results
                                _results = new SearchResults(snapshot,
                                                                m,
                                                                NormalizedSpanCollection.Union(_results.SearchedSpans, requestedSpans));
                            }

                            //We completed the search & updated the results ... have the tagger to raise the appropriate changed event
                            //on the span we just searched.
                            //
                            //We can't raise the tags changed on just the results since we also need to signal that stale results have
                            //been removed.
                            _callback(snapshot, requestedSpans);
                        }
                        else
                        {
                            //The buffer changed so we can't trust the results we just got (the search may not have completed).
                            //Don't dequeue the request and we'll repeat the process (but on the correct snapshot).
                            dequeueRequest = false;
                        }
                    }

                    if (dequeueRequest)
                    {
                        lock (_requestQueue)
                        {
                            //Nothing should have moved the request out of the queue.
                            Debug.Assert(object.ReferenceEquals(request, _requestQueue.Peek()));

                            _requestQueue.Dequeue();

                            if (_requestQueue.Count == 0)
                            {
                                //No more requests are pending, release the worker thread.
                                return;
                            }
                        }
                    }
                }
            }
            finally
            {
                Thread.CurrentThread.Priority = ThreadPriority.Normal;
            }
        }

        internal ITextSnapshot AdvanceToCurrentSnapshot()
        {
            //We don't need to take a snapshot of the results because the results are only modified on this thread.
            ITextSnapshot oldSnapshot = _results.Snapshot;
            ITextSnapshot newSnapshot = _buffer.CurrentSnapshot;

            if (oldSnapshot != newSnapshot)
            {
                //The results are all on an old snapshot. We need to project them forward (even though that might cause some stale and incorrect
                //results).
                NormalizedSpanCollection newMatches = TextSearchNavigator.TranslateTo(oldSnapshot, _results.Matches, newSnapshot);
                NormalizedSpanCollection newSearchedSpans = NormalizedSpanCollection.Empty;

                if ((_results.SearchedSpans.Count != 0) && !_options.HasFlag(FindOptions.Multiline))
                {
                    //Advance our record of the spans that have already been searched to the new snapshot as well.
                    newSearchedSpans = BackgroundSearch<T>.TranslateToAndExtend(oldSnapshot, _results.SearchedSpans, newSnapshot);

                    //But remove anything on a TextSnapshotLine that was modified by the change.
                    List<Span> changedSpansOnNewSnapshot = new List<Span>();
                    ITextVersion version = oldSnapshot.Version;
                    while (version != newSnapshot.Version)
                    {
                        foreach (var change in version.Changes)
                        {
                            changedSpansOnNewSnapshot.Add(BackgroundSearch<T>.Extend(newSnapshot, Tracking.TrackSpanForwardInTime(SpanTrackingMode.EdgeInclusive, change.NewSpan,
                                                                                                                                  version.Next, newSnapshot.Version)));
                        }

                        version = version.Next;
                    }

                    if (changedSpansOnNewSnapshot.Count > 0)
                    {
                        NormalizedSpanCollection changes = new NormalizedSpanCollection(changedSpansOnNewSnapshot);

                        //Remove the spans touched by changes from the spans we've searched
                        newSearchedSpans = NormalizedSpanCollection.Difference(newSearchedSpans, changes);
                    }
                }

                _results = new SearchResults(newSnapshot, newMatches, newSearchedSpans);
            }

            return newSnapshot;
        }                

        public static NormalizedSpanCollection TranslateToAndExtend(ITextSnapshot currentSnapshot, NormalizedSpanCollection currentSpans, ITextSnapshot targetSnapshot)
        {
            if (currentSpans.Count == 0)
            {
                return currentSpans;
            }

            List<Span> spans = new List<Span>(currentSpans.Count);
            foreach (var s in currentSpans)
            {
                spans.Add(BackgroundSearch<T>.Extend(targetSnapshot, Tracking.TrackSpanForwardInTime(SpanTrackingMode.EdgeNegative,
                                                                                                     s,
                                                                                                     currentSnapshot.Version, targetSnapshot.Version)));
            }

            return new NormalizedSpanCollection(spans);
        }

        //Grow a snapshot span so that it includes all of the TextSnapshotLines that overlap the span (but always return at least one
        //complete line).
        public static Span Extend(ITextSnapshot snapshot, Span span)
        {
            ITextSnapshotLine start = snapshot.GetLineFromPosition(span.Start);
            if (span.End <= start.EndIncludingLineBreak.Position)
            {
                //source.End is on the same line (or possibly the start of the next line) ... return just this line.
                return start.ExtentIncludingLineBreak;
            }
            else
            {
                ITextSnapshotLine end = snapshot.GetLineFromPosition(span.End);

                //if source.End is at the start of the line, only return up to the start of the line, otherwise
                //include the entire line).
                return Span.FromBounds(start.Start,
                                       (end.Start.Position == span.End)
                                       ? end.Start
                                       : end.EndIncludingLineBreak);
            }              
        }

        /// <summary>
        /// Simulates a search on the range where the user is performing a series of find next operations, buts aborts quickly
        /// when either the BackgroundSearch advances to a new snapshot or is disposed.
        /// </summary>
        private IList<Span> FindAll(ITextSnapshot snapshot, NormalizedSpanCollection spans)
        {
            IList<Span> matches = new List<Span>();

            int start = int.MinValue;
            int end = int.MinValue;

            foreach (var span in spans)
            {
                //All the spans are normalized to conver entire text snapshot lines.
                Debug.Assert(snapshot.GetLineFromPosition(span.Start).Start == span.Start);
                Debug.Assert((span.End == snapshot.Length) || (snapshot.GetLineFromPosition(span.End).Start == span.End));

                if (span.Length > 0)
                {
                    SnapshotSpan searchRange = new SnapshotSpan(snapshot, span);
                    SnapshotPoint startingPosition = searchRange.Start;

                    while (true)
                    {
                        if (_isDisposed || (snapshot != _buffer.CurrentSnapshot))
                        {
                            //We've been disposed of or the buffer has advanced to a new snapshot. Either way, abort the search.
                            return matches;
                        }

                        SnapshotSpan? match = _textSearchService.Find(searchRange, startingPosition, _searchTerm, _options);

                        if (match.HasValue)
                        {
                            if (match.Value.Start > end)
                            {
                                //The current match is disjoint from the last match, add it to the list of matches.
                                if (end != int.MinValue)
                                {
                                    matches.Add(Span.FromBounds(start, end));

                                    //Avoid problems when there are so many matches (e.g. searching for 'a' in a 300MB file) that
                                    //we run out of memory tracking results.
                                    //
                                    //The effect of this cut-out isn't exactly predictable (doing several smaller searches will
                                    //allow the total number of results maintained by the background search class to grow past
                                    //the limit but a large search will hit the limit and miss results).
                                    if (matches.Count > 5000)
                                        return matches;
                                }

                                start = match.Value.Start;
                                end = match.Value.End;
                            }
                            else
                            {
                                //The new match overlaps the old. Simple extend the existing matched span.
                                end = Math.Max(end, match.Value.End);       //With an RE, the new match could end before the end of the previous match.
                            }

                            startingPosition = match.Value.Start;

                            if (startingPosition >= span.End)
                            {
                                break;
                            }
                            else
                            {
                                startingPosition += 1;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            if (end != int.MinValue)
            {
                matches.Add(Span.FromBounds(start, end));
            }

            return matches;
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            _isDisposed = true;
        }

        #endregion

        internal class SearchResults
        {
            public readonly ITextSnapshot Snapshot;
            public readonly NormalizedSpanCollection Matches;
            public readonly NormalizedSpanCollection SearchedSpans;

            public SearchResults(ITextSnapshot snapshot, NormalizedSpanCollection matches, NormalizedSpanCollection searchedSpans)
            {
                this.Snapshot = snapshot;
                this.Matches = matches;
                this.SearchedSpans = searchedSpans;
            }
        }
    }
}
