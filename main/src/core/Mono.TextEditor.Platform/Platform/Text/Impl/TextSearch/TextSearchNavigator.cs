//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Find.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Operations;

    class TextSearchNavigator : ITextSearchNavigator2
    {
        readonly ITextBuffer _buffer;
        readonly ITextSearchService2 _textSearchService;

        public TextSearchNavigator(ITextSearchService2 textSearchService, ITextBuffer buffer)
        {
            _buffer = buffer;
            _textSearchService = textSearchService;
        }

        #region Private Helpers

        /// <summary>
        /// Calculates the search start point for the next search operation. Always returns a point on the buffer's
        /// current snapshot. If no point is returned, then we're at ends of the buffer (or search range)
        /// and wrap is turned off.
        /// </summary>
        private SnapshotPoint? CalculateStartPoint(ITextSnapshot searchSnapshot, bool wrap, bool forward)
        {
            // If there is a current result, then use its span to figure out the next starting point, otherwise
            // use the StartPoint itself
            SnapshotSpan? currentResult = this.CurrentResult;
            SnapshotPoint nextSearchStart;

            if (currentResult.HasValue)
            {
                int position;
                if (forward)
                {
                    // moving forwards (by default one more than the start of the previous match).
                    position = currentResult.Value.Start.Position + 1;

                    if (position > searchSnapshot.Length)
                    {
                        // The previous search result was at the end of the buffer
                        if (wrap)
                        {
                            position = 0;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                else
                {
                    // Moving backwards (by default 1 less than the end of the previous match).
                    position = currentResult.Value.End.Position - 1;
                    if (position < 0)
                    {
                        // The last position was at the start of the buffer
                        if (wrap)
                        {
                            position = searchSnapshot.Length;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }

                nextSearchStart = new SnapshotPoint(currentResult.Value.Snapshot, position);
            }
            else if (this.StartPoint != null)
            {
                // We have no current result, simply use the starting point as the search point.
                // If there is none, then use the start of the buffer as the search starting point.
                nextSearchStart = this.StartPoint.Value;
            }
            else
            {
                //If all else fails, start at the start of the buffer.
                nextSearchStart = new SnapshotPoint(_buffer.CurrentSnapshot, 0);
            }

            return nextSearchStart.TranslateTo(searchSnapshot, GetTrackingMode(forward));
        }
        #endregion

        #region ITextSearchNavigator Members
        public string SearchTerm { get; set; }

        public string ReplaceTerm { get; set; }

        public FindOptions SearchOptions { get; set; }

        public SnapshotSpan? CurrentResult { get; private set; }

        private ITrackingSpan _searchSpan;
        public ITrackingSpan SearchSpan
        {
            get
            {
                var span = _searchSpan;
                if ((span == null) && (_searchSpans != null) && (_searchSpans.Count > 0))
                {
                    var snapshot = _searchSpans[0].Snapshot;
                    span = snapshot.CreateTrackingSpan(Span.FromBounds(_searchSpans[0].Start, _searchSpans[_searchSpans.Count - 1].End), SpanTrackingMode.EdgeInclusive);
                }
                return span;
            }
            set
            {
                if (value != null && value.TextBuffer != _buffer)
                {
                    throw new InvalidOperationException("The SearchSpan must be on the same buffer as the navigator itself.");
                }

                //We keep _searchSpan & _searchSpans (instead of converting this to a NormalizedSnapshotSpanCollection) because the user
                //could have provided a custom tracking span and we'd loose the tracking behavior in the conversion.
                _searchSpan = value;
                _searchSpans = null;
            }
        }

        private SnapshotPoint? _startPoint;
        public SnapshotPoint? StartPoint
        {
            get
            {
                UpdateStartPoint();
                return _startPoint;
            }
            set
            {
                if (value != null && value.Value.Snapshot.TextBuffer != _buffer)
                {
                    throw new ArgumentException("StartPoint must be on the same buffer as the search navigator itself.");
                }

                _startPoint = value;
            }
        }

        public bool Find()
        {
            if (string.IsNullOrEmpty(this.SearchTerm))
            {
                throw new InvalidOperationException("You must set a non-empty search term before searching.");
            }

            bool forward = (this.SearchOptions & FindOptions.SearchReverse) != FindOptions.SearchReverse;
            bool wrap = (this.SearchOptions & FindOptions.Wrap) == FindOptions.Wrap;
            bool regEx = (this.SearchOptions & FindOptions.UseRegularExpressions) == FindOptions.UseRegularExpressions;

            ITextSnapshot searchSnapshot = _buffer.CurrentSnapshot;

            //There could be a version skew here if someone calls find from inside a text changed callback on the buffer. That probably wouldn't be a good
            //idea but we need to handle it gracefully.
            this.AdvanceToSnapshot(searchSnapshot);

            SnapshotPoint? searchStart = this.CalculateStartPoint(searchSnapshot, wrap, forward);
            if (searchStart.HasValue)
            {
                int index = 0;
                NormalizedSnapshotSpanCollection searchSpans = this.SearchSpans;

                if (searchSpans != null)
                {
                    Debug.Assert(searchSpans.Count > 0);

                    //Index is potentially outside the range of [0...searchSpans.Count-1] but we handle that below.
                    if (!(TextSearchNavigator.TryGetIndexOfContainingSpan(searchSpans, searchStart.Value, out index) || forward))
                    {
                        //For reversed searches, we want the index of the span before the point if we can't get a span that contains the point.
                        --index;
                    }
                }
                else
                {
                    searchSpans = new NormalizedSnapshotSpanCollection(new SnapshotSpan(searchSnapshot, Span.FromBounds(0, searchSnapshot.Length)));
                }

                int searchIterations = searchSpans.Count;
                for (int i = 0; (i < searchIterations); ++i)
                {
                    //index needs to be normalized to [0 ... searchSpans.Count - 1] but could be negative.
                    index = (index + searchSpans.Count) % searchSpans.Count;

                    SnapshotSpan searchSpan = searchSpans[index];
                    if ((i != 0) || (searchStart.Value < searchSpan.Start) || (searchStart.Value > searchSpan.End))
                    {
                        searchStart = forward ? searchSpan.Start : searchSpan.End;
                    }
                    else if (wrap && (i == 0))
                    {
                        //We will need to repeat the search to account for wrap being on and we are not searching everything in searchSpans[0].
                        //This is the same as simply doing a search for i == searchSpans.Count we we can make happen by bumping the number of iterations.
                        ++searchIterations;
                    }

                    foreach (var result in _textSearchService.FindAll(searchSpan, searchStart.Value, this.SearchTerm, this.SearchOptions & ~FindOptions.Wrap))
                    {
                        // As a safety measure, we don't include results of length zero in the navigator unless regular expressions are being used.
                        // Zero width matches could be useful in RegEx when for example somebody is trying to replace the start of the line using the "^"
                        // pattern.
                        if (result.Length == 0 && !regEx)
                        {
                            continue;
                        }
                        else
                        {
                            // We accept the first match
                            this.CurrentResult = result;
                            return true;
                        }
                    }

                    if (forward)
                    {
                        ++index;
                    }
                    else
                    {
                        --index;
                    }
                }
            }

            // If nothing was found, then clear the current result
            this.ClearCurrentResult();

            return false;
        }

        public bool Replace()
        {
            if (this.ReplaceTerm == null)
            {
                throw new InvalidOperationException("Can't replace with a null value. Set ReplaceTerm before performing a replace operation.");
            }

            if (!this.CurrentResult.HasValue)
            {
                throw new InvalidOperationException("Need to have a current result before being able to replace. Perform a FindNext or FindPrevious operation first.");
            }

            bool forward = (this.SearchOptions & FindOptions.SearchReverse) != FindOptions.SearchReverse;
            bool regEx = (this.SearchOptions & FindOptions.UseRegularExpressions) == FindOptions.UseRegularExpressions;

            //This may not be the text buffer's current snapshot but that is the desired behavior. We're replacing the current result
            //with the replace tuern.
            SnapshotSpan result = this.CurrentResult.Value;
            ITextSnapshot replaceSnapshot = result.Snapshot;

            SnapshotPoint searchStart = forward ? result.Start : result.End;

            SnapshotSpan searchSpan;
            NormalizedSnapshotSpanCollection searchSpans = this.SearchSpans;
            if ((searchSpans != null) && (searchSpans.Count > 0))
            {
                //There could be a version skew here.
                if (searchSpans[0].Snapshot != replaceSnapshot)
                {
                    searchSpans = new  NormalizedSnapshotSpanCollection(replaceSnapshot, TextSearchNavigator.TranslateTo(searchSpans[0].Snapshot, searchSpans, replaceSnapshot));
                }

                int index;
                if (!TextSearchNavigator.TryGetIndexOfContainingSpan(searchSpans, searchStart, out index))
                {
                    // If the match is outside of the search range, then we should noop
                    return false;
                }
                searchSpan = searchSpans[index];
            }
            else
            {
                searchSpan = new SnapshotSpan(replaceSnapshot, 0, replaceSnapshot.Length);
            }

            searchSpan = forward ? new SnapshotSpan(searchStart, searchSpan.End) : new SnapshotSpan(searchSpan.Start, searchStart);

            //Ask the search engine to find the actual span we need to replace (& the corresponding replacement string).
            string replacementValue = null;
            SnapshotSpan? toReplace = _textSearchService.FindForReplace(searchSpan, this.SearchTerm, this.ReplaceTerm, this.SearchOptions, out replacementValue);

            if (toReplace.HasValue)
            {
                using (ITextEdit edit = _buffer.CreateEdit())
                {
                    Span replacementSpan = toReplace.Value.TranslateTo(edit.Snapshot, SpanTrackingMode.EdgeInclusive);

                    if (!edit.Replace(replacementSpan, replacementValue))
                    {
                        // The edit failed for some reason, perhaps read-only regions?
                        return false;
                    }

                    edit.Apply();

                    if (edit.Canceled)
                    {
                        // The edit failed, most likely a handler of the changed event forced the edit to be canceled.
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        public void ClearCurrentResult()
        {
            this.CurrentResult = null;
        }

        /// <summary>
        /// Translate start point to current snapshot
        /// </summary>
        private void UpdateStartPoint()
        {
            if (_startPoint.HasValue && _startPoint.Value.Snapshot != _buffer.CurrentSnapshot)
            {
                ITextVersion currentVersion = _startPoint.Value.Snapshot.Version;
                ITextVersion targetVersion = _buffer.CurrentSnapshot.Version;
                bool reverse = this.SearchOptions.HasFlag(Text.Operations.FindOptions.SearchReverse);
                int currentStartPointPosition = _startPoint.Value.Position;

                Debug.Assert(targetVersion.VersionNumber >= currentVersion.VersionNumber, "We should never have to translate StartPoint into past");

                while (currentVersion != targetVersion)
                {
                    // Per INormalizedTextChangeCollection contract, there is not more than one change that we need to check. Find it with binary search
                    int changeCount = currentVersion.Changes.Count;
                    int lo = 0;
                    int hi = changeCount - 1;

                    while (lo <= hi)
                    {
                        int mid = (lo + hi) / 2;
                        ITextChange textChange = currentVersion.Changes[mid];

                        if (currentStartPointPosition < textChange.OldPosition)
                        {
                            hi = mid - 1;
                        }
                        else if (currentStartPointPosition > textChange.OldEnd)
                        {
                            lo = mid + 1;
                        }
                        else
                        {
                            // Found the change. Let's adjust currentStartPointPosition
                            if (reverse)
                            {
                                // Partially verified by binary search. The full condition is in assert:
                                Debug.Assert(textChange.OldSpan.Start <= currentStartPointPosition && currentStartPointPosition <= textChange.OldSpan.End);

                                if (currentStartPointPosition < textChange.OldSpan.End && !textChange.NewSpan.IsEmpty)
                                {
                                    currentStartPointPosition = textChange.NewSpan.End - 1;
                                }
                                else // currentStartPosition == textChange.OldSpan.End
                                {
                                    currentStartPointPosition = textChange.NewSpan.End;
                                }
                            }
                            else
                            {
                                if (textChange.OldSpan.End == currentStartPointPosition)
                                {
                                    currentStartPointPosition = textChange.NewSpan.End;
                                }
                                else if (textChange.OldSpan.Start < currentStartPointPosition && !textChange.NewSpan.IsEmpty)
                                {
                                    currentStartPointPosition = textChange.NewSpan.End - 1;
                                }
                                else
                                {
                                    currentStartPointPosition = textChange.NewSpan.Start;
                                }
                            }
                            break;
                        }
                    }

                    if (hi < lo)
                    {
                        Debug.Assert(hi == lo - 1, "If we haven't found a change, hi should be equal to lo - 1");

                        if (lo > 0) // Current position lies between the changes
                        {
                            ITextChange textChange = currentVersion.Changes[lo - 1];
                            currentStartPointPosition = currentStartPointPosition + (textChange.NewEnd - textChange.OldEnd);
                        }
                        // else the start point lays prior to the first change (or there are no changes) and should remain intact
                    }

                    currentVersion = currentVersion.Next;
                }

                _startPoint = new SnapshotPoint(_buffer.CurrentSnapshot, currentStartPointPosition);
            }
        }

        static PointTrackingMode GetTrackingMode(bool isReverse)
        {
            return isReverse ? PointTrackingMode.Positive : PointTrackingMode.Negative;
        }
        #endregion

        #region ITextSearchNavigator2 Members
        private NormalizedSnapshotSpanCollection _searchSpans;
        public NormalizedSnapshotSpanCollection SearchSpans
        {
            get
            {
                var spans = _searchSpans;
                if ((spans == null) && (_searchSpan != null))
                {
                    var snapshot = _buffer.CurrentSnapshot;
                    spans = new NormalizedSnapshotSpanCollection(new SnapshotSpan(snapshot, _searchSpan.GetSpan(snapshot)));
                }
                return spans; 
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
                        throw new InvalidOperationException("The SearchSpan must be on the same buffer as the navigator itself.");
                    }
                }

                _searchSpans = value;
                _searchSpan = null;
            }
        }
        #endregion

        private void AdvanceToSnapshot(ITextSnapshot snapshot)
        {
            if ((_searchSpans != null) && (_searchSpans.Count > 0) && (_searchSpans[0].Snapshot != snapshot))
            {
                NormalizedSpanCollection newSpans = TextSearchNavigator.TranslateTo(_searchSpans[0].Snapshot, _searchSpans, snapshot);
                this.SearchSpans = new NormalizedSnapshotSpanCollection(snapshot, newSpans);
            }
        }

        public static NormalizedSpanCollection TranslateTo(ITextSnapshot currentSnapshot, NormalizedSpanCollection currentSpans, ITextSnapshot targetSnapshot)
        {
            if ((currentSpans.Count == 0) || (currentSnapshot == targetSnapshot))
            {
                return currentSpans;
            }

            bool forwardInTime = (currentSnapshot.Version.VersionNumber < targetSnapshot.Version.VersionNumber);

            return new NormalizedSnapshotSpanCollection(targetSnapshot,
                                             currentSpans.Select(s =>
                                                                 forwardInTime
                                                                 ? Tracking.TrackSpanForwardInTime(SpanTrackingMode.EdgeInclusive,
                                                                                                   s,
                                                                                                   currentSnapshot.Version, targetSnapshot.Version)
                                                                 : Tracking.TrackSpanBackwardInTime(SpanTrackingMode.EdgeInclusive,
                                                                                                   s,
                                                                                                   currentSnapshot.Version, targetSnapshot.Version)).Where(s => s.Length != 0));
        }

        /// <summary>
        /// Search spans from [start ... spans.Count-1] for a span that contains point.
        /// </summary>
        private static bool TryGetIndexOfContainingSpan(NormalizedSpanCollection spans, int point, out int index)
        {
            int lo = 0;
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
                    index = mid;
                    return true;
                }
            }

            //None of the spans contains point. lo is the index of the span that follows point
            index = lo;
            return false;
        }
    }
}
