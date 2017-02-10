////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents a set of completions.
    /// </summary>
    public class CompletionSet
    {
        private string _displayName;
        private ITrackingSpan _applicableTo;
        private CompletionSelectionStatus _selectionStatus = new CompletionSelectionStatus(null, false, false);
        private BulkObservableCollection<Completion> _completions = new BulkObservableCollection<Completion>();
        private BulkObservableCollection<Completion> _completionBuilders = new BulkObservableCollection<Completion>();
        private FilteredObservableCollection<Completion> _filteredCompletions;
        private FilteredObservableCollection<Completion> _filteredCompletionBuilders;
        private CompletionMatchType _filterMatchType = CompletionMatchType.MatchDisplayText;
        private bool _filterCaseSensitive = false;
        private string _filterBufferText;
        private int _filterBufferTextVersionNumber = -1;

        /// <summary>
        /// Stores information about the completion match result.
        /// </summary>
        protected class CompletionMatchResult
        {
            /// <summary>
            /// The selection status of the completion set.
            /// </summary>
            public CompletionSelectionStatus SelectionStatus { get; set; }

            /// <summary>
            /// The number of characters matched in the completion set.
            /// </summary>
            public int CharsMatchedCount { get; set; }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CompletionSet"/>.
        /// </summary>
        public CompletionSet()
        {
            this.Initialize();
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CompletionSet"/> with the specified name and text.
        /// </summary>
        /// <param name="moniker">The unique, non-localized identifier for the completion set.</param>
        /// <param name="displayName">The localized name of the completion set.</param>
        /// <param name="applicableTo">The tracking span to which the completions apply.</param>
        /// <param name="completions">The list of completions.</param>
        /// <param name="completionBuilders">The list of completion builders.</param>
        public CompletionSet
            (
            string moniker,
            string displayName,
            ITrackingSpan applicableTo,
            IEnumerable<Completion> completions,
            IEnumerable<Completion> completionBuilders
            )
        {
            // Save-off passed-in values.
            this.Moniker = moniker;
            _displayName = displayName;
            _applicableTo = applicableTo;

            // Copy the completions and completion builders to observable collections
            _completions.AddRange(completions);
            _completionBuilders.AddRange(completionBuilders);

            this.Initialize();
        }

        private void Initialize()
        {
            _filteredCompletions = new FilteredObservableCollection<Completion>(_completions);
            _filteredCompletionBuilders = new FilteredObservableCollection<Completion>(_completionBuilders);
        }

        /// <summary>
        /// The unique, non-localized identifier for the completion set.
        /// </summary>
        public virtual string Moniker { get; protected set; }

        /// <summary>
        /// Gets or sets the localized name of this completion set.  
        /// </summary>
        /// <remarks>In the default presenter, the display name
        /// appears as the header of a tab item in a WPF TabControl.</remarks>
        public virtual string DisplayName
        {
            get { return _displayName; }
            set { _displayName = value; }
        }

        /// <summary>
        /// Gets or sets the text tracking span to which this completion applies.  
        /// </summary>
        /// <remarks>If this completion is committed to the buffer, the
        /// span will be replaced with the completion insertion text.</remarks>
        public virtual ITrackingSpan ApplicableTo
        {
            get { return _applicableTo; }
            protected set
            {
                _filterBufferText = null;
                _applicableTo = value;
            }
        }

        /// <summary>
        /// Gets the list of completions that are part of this completion set.
        /// </summary>
        /// <remarks>
        /// When overriding this property, if you would like to provide a dynamic collection of completions,
        /// take care to implement <see cref="INotifyCollectionChanged" /> for your collection of completions.
        /// The default completion presenter uses <see cref="INotifyCollectionChanged" /> to listen for changes.
        /// </remarks>
        public virtual IList<Completion> Completions
        {
            get { return _filteredCompletions; }
        }

        /// <summary>
        /// Gets the list of completion builders that are part of this completion set.  
        /// </summary>
        /// <remarks>
        /// Completion builders are completions that are displayed separately from the other completions in the completion set.
        /// In the default presentation, completion builders appear in a non-scrolled list above the scrolled list of completions.
        /// When overriding this property, if you would like to provide a dynamic collection of completion builders,
        /// take care to implement <see cref="INotifyCollectionChanged" /> for your collection of completion builders.
        /// The default completion presenter uses <see cref="INotifyCollectionChanged" /> to listen for changes.
        /// </remarks>
        public virtual IList<Completion> CompletionBuilders
        {
            get { return _filteredCompletionBuilders; }
        }

        /// <summary>
        /// Determines the best match in the completion set.
        /// </summary>
        public virtual void SelectBestMatch()
        {
            this.SelectBestMatch(CompletionMatchType.MatchDisplayText, false);
        }

        /// <summary>
        /// Filters the set of completions to those that match the applicability text of the completion
        /// set and determines the best match.
        /// </summary>
        /// <exception cref="InvalidOperationException">Both the completions and the completion builders have been overridden.</exception>
        public virtual void Filter()
        {
            this.Filter(CompletionMatchType.MatchDisplayText, false);
        }

        /// <summary>
        /// Recalculates the set of completions for this completion set. 
        /// </summary>
        /// <remarks>
        /// The base implementation of Recalculate() does nothing.  
        /// Derived classes should override this method to implement custom
        /// recalculation behavior.
        /// </remarks>
        public virtual void Recalculate()
        { }

        /// <summary>
        /// Returns a list of spans (of character positions) that should be highlighted in the display text.
        /// </summary>
        /// <returns>A list of spans (e.g. {[0,1), [4,6)} to highlight the 1st, 5th and 6th characters) or null if no highlighting is desired</returns>
        public virtual IReadOnlyList<Span> GetHighlightedSpansInDisplayText(string displayText)
        {
#if false
            if (!string.IsNullOrEmpty(displayText))
            {
                var filterBufferText = this.FilterBufferText;

                if (!string.IsNullOrEmpty(filterBufferText))
                {
                    var highlightedSpans = new List<Span>();

                    int matchIndexStart = -1;
                    int matchIndexEnd = -1;
                    int filterIndex = 0;
                    for (int textIndex = 0; ((filterIndex < filterBufferText.Length) && (textIndex < displayText.Length)); ++textIndex)
                    {
                        if (_filterCaseSensitive ? (filterBufferText[filterIndex] == displayText[textIndex])
                                                 : (char.ToLowerInvariant(filterBufferText[filterIndex]) == char.ToLowerInvariant(displayText[textIndex])))
                        {
                            if (matchIndexStart == -1)
                            {
                                matchIndexStart = textIndex;
                            }

                            matchIndexEnd = textIndex + 1;

                            ++filterIndex;
                        }
                        else if (matchIndexStart != -1)
                        {
                            highlightedSpans.Add(Span.FromBounds(matchIndexStart, textIndex));
                            matchIndexStart = matchIndexEnd = -1;
                        }
                    }

                    if (matchIndexStart != -1)
                    {
                        highlightedSpans.Add(Span.FromBounds(matchIndexStart, matchIndexEnd));
                    }

                    if (highlightedSpans.Count != 0)
                    {
                        return highlightedSpans;
                    }
                }
            }
#endif

            return null;
        }

        /// <summary>
        /// Gets or sets the <see cref="CompletionSelectionStatus"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">The value is null.</exception>
        /// <exception cref="ArgumentException">The <see cref="CompletionSelectionStatus"/> is not contained in either the 
        /// completions or the completion builders.</exception>
        public CompletionSelectionStatus SelectionStatus
        {
            get { return _selectionStatus; }
            set
            {
                // The selection status can never be null.
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                // If nothing changed, there's no work to do.
                if (_selectionStatus == value)
                {
                    return;
                }

                // The completion may be null.  Otherwise, it must be part of the list of completions (or builders) in this
                // completion set.
                if (
                    (value.Completion != null) &&
                    (!this.CompletionBuilders.Contains(value.Completion)) &&
                    (!this.Completions.Contains(value.Completion))
                   )
                {
                    throw new ArgumentException
                        (
                        "The Selection Status may not be set to a completion that isn't part of the completion/builder list in the completion set",
                        "value"
                        );
                }

                // We've passed all of the tests.  Save off the old value and set the field, firing both events afterward.
                CompletionSelectionStatus oldSelectionStatus = _selectionStatus;
                _selectionStatus = value;

                this.RaiseSelectionStatusChanged(oldSelectionStatus);
            }
        }

        /// <summary>
        /// Occurs when the selection status has changed.
        /// </summary>
        public event EventHandler<ValueChangedEventArgs<CompletionSelectionStatus>> SelectionStatusChanged;

        private string FilterBufferText
        {
            get
            {
                if (_applicableTo != null)
                {
                    ITextSnapshot snapshot = _applicableTo.TextBuffer.CurrentSnapshot;
                    if ((_filterBufferText == null) || (_filterBufferTextVersionNumber != snapshot.Version.VersionNumber))
                    {
                        _filterBufferText = _applicableTo.GetText(snapshot);
                        _filterBufferTextVersionNumber = snapshot.Version.VersionNumber;
                    }
                }
                else
                {
                    Debug.Assert(_filterBufferText == null);
                }

                return _filterBufferText;
            }
        }

        /// <summary>
        /// Gets the ObservableCollection of writable completions.
        /// </summary>
        protected BulkObservableCollection<Completion> WritableCompletions
        {
            get { return _completions; }
        }

        /// <summary>
        /// Gets the ObservableCollection of writable completion builders.
        /// </summary>
        protected BulkObservableCollection<Completion> WritableCompletionBuilders
        {
            get { return _completionBuilders; }
        }

        /// <summary>
        /// Filters the set of completions to those that match the applicability text of the completion
        /// set and determines the best match.
        /// </summary>
        /// <param name="matchType">The <see cref="CompletionMatchType"/>.</param>
        /// <param name="caseSensitive"><c>true</c> if the match is case-sensitive, otherwise <c>false</c>.</param>
        /// <exception cref="InvalidOperationException">Both the completions and the completion builders have been overridden.</exception>
        protected void Filter(CompletionMatchType matchType, bool caseSensitive)
        {
            // Get the actual text from the buffer
            var filterBufferText = this.FilterBufferText;

            if (string.IsNullOrEmpty(filterBufferText))
            {
                _filteredCompletions.StopFiltering();
                _filteredCompletionBuilders.StopFiltering();
            }
            else
            {
                _filterMatchType = matchType;
                _filterCaseSensitive = caseSensitive;

                _filteredCompletions.Filter(new Predicate<Completion>(this.DoesCompletionMatchApplicabilityText));
                _filteredCompletionBuilders.Filter(new Predicate<Completion>(this.DoesCompletionMatchApplicabilityText));
            }
        }

        /// <summary>
        /// Redetermines the best matching completion in the completion set.
        /// </summary>
        /// <param name="matchType">The <see cref="CompletionMatchType"/>.</param>
        /// <param name="caseSensitive"><c>true</c> if the match is case-sensitive, otherwise <c>false</c>.</param>
        protected void SelectBestMatch(CompletionMatchType matchType, bool caseSensitive)
        {
            CompletionMatchResult completionMatch = this.MatchCompletionList
                (
                this.Completions,
                matchType,
                caseSensitive
                );
            CompletionMatchResult builderMatch = this.MatchCompletionList
                (
                this.CompletionBuilders,
                matchType,
                caseSensitive
                );

            // Only select a builder if it's far-and-away better.  Otherwise, select a completion
            int builderScore = 0;
            if (builderMatch != null)
            {
                builderScore =
                    builderMatch.CharsMatchedCount +
                    (builderMatch.SelectionStatus.IsSelected ? 1 : 0) +
                    (builderMatch.SelectionStatus.IsUnique ? 1 : 0);
            }
            int completionScore = 0;
            if (completionMatch != null)
            {
                completionScore =
                    completionMatch.CharsMatchedCount +
                    (completionMatch.SelectionStatus.IsSelected ? 1 : 0) +
                    (completionMatch.SelectionStatus.IsUnique ? 1 : 0);
            }

            if ((builderScore > completionScore) && (builderMatch != null))
            {
                this.SelectionStatus = builderMatch.SelectionStatus;
            }
            else if (completionMatch != null)
            {
                this.SelectionStatus = completionMatch.SelectionStatus;
            }
            else if (this.Completions.Count > 0)
            {
                // If we've already got a pretty good selection, just leave it selected, but not fully-selected.
                if (this.Completions.Contains(this.SelectionStatus.Completion))
                    this.SelectionStatus = new CompletionSelectionStatus(this.SelectionStatus.Completion, false, false);
                else
                    this.SelectionStatus = new CompletionSelectionStatus(this.Completions[0], false, false);
            }
            else if (this.CompletionBuilders.Count > 0)
            {
                // If we've already got a pretty good selection, just leave it selected, but not fully-selected.
                if (this.CompletionBuilders.Contains(this.SelectionStatus.Completion))
                    this.SelectionStatus = new CompletionSelectionStatus(this.SelectionStatus.Completion, false, false);
                else
                    this.SelectionStatus = new CompletionSelectionStatus(this.CompletionBuilders[0], false, false);
            }
            else
            {
                this.SelectionStatus = new CompletionSelectionStatus(null, false, false);
            }
        }

        /// <summary>
        /// Matches the completion list.
        /// </summary>
        /// <param name="completionList">The list of completions.</param>
        /// <param name="matchType">The <see cref="CompletionMatchType"/>.</param>
        /// <param name="caseSensitive"><c>true</c> if the match is case-sensitive, otherwise <c>false</c>.</param>
        /// <returns>A <see cref="CompletionMatchResult"/>.</returns>
        /// <exception cref="InvalidOperationException">The span to which this completion applies is null.</exception>
        protected CompletionMatchResult MatchCompletionList
            (
            IList<Completion> completionList,
            CompletionMatchType matchType,
            bool caseSensitive
            )
        {
            if (_applicableTo == null)
            {
                throw new InvalidOperationException("Cannot match completion set with no applicability span.");
            }

            // Get the actual text from the buffer
            ITextSnapshot snapshot = _applicableTo.TextBuffer.CurrentSnapshot;
            string bufferText = _applicableTo.GetText(snapshot);

            // Short-circuit if we detect that there's no text to match.
            if (bufferText.Length == 0)
            {
                return null;
            }

            Completion bestMatch = null;
            int bestMatchNumCharsMatched = -1;
            bool uniqueMatch = false;
            bool fullMatch = false;
            foreach (Completion completion in completionList)
            {
                // Determine what text should be matched.
                string matchText = string.Empty;
                if (matchType == CompletionMatchType.MatchDisplayText)
                {
                    matchText = completion.DisplayText;
                }
                else if (matchType == CompletionMatchType.MatchInsertionText)
                {
                    matchText = completion.InsertionText;
                }

                // Count the number of characters that match.
                int numCharsMatched = 0;
                for (int i = 0; i < bufferText.Length; i++)
                {
                    if (i >= matchText.Length)
                    {
                        break;
                    }
                    else
                    {
                        char b = bufferText[i];
                        char m = matchText[i];
                        if (!caseSensitive)
                        {
                            b = char.ToLowerInvariant(b);
                            m = char.ToLowerInvariant(m);
                        }

                        if (b == m)
                        {
                            numCharsMatched++;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                // See if this completion is the best match.
                if (numCharsMatched > bestMatchNumCharsMatched)
                {
                    bestMatchNumCharsMatched = numCharsMatched;
                    bestMatch = completion;

                    // Since we've found a completion with more characters matching than any before, this is now a unique
                    // match
                    uniqueMatch = true;

                    // Let's see if this is also a full match.  In order to be a full match, all of the characters in the buffer
                    // must have matched.  In addition, we must have at least one character matched.
                    if ((numCharsMatched == bufferText.Length) && (bestMatchNumCharsMatched > 0))
                    {
                        fullMatch = true;
                    }
                }
                else if (numCharsMatched == bestMatchNumCharsMatched)
                {
                    // We've come across a completion that matches exactly the same number of buffer characters as a previous
                    // completion.  We're going to take that to mean that we no longer have a unique match.
                    uniqueMatch = false;

                    // Now that we know we no longer have a unique match, we may be able to break out of the loop without looking at
                    // any more completions.  We can only do this if we've matched all of the characters in the applicable-to text,
                    // however.  If we've matched all of the text so far, there can't possibly be another completion that will match
                    // more.
                    if (fullMatch)
                    {
                        break;
                    }
                }
            }

            // If there was a match, return it.
            if (bestMatch != null)
            {
                return new CompletionMatchResult()
                {
                    SelectionStatus = new CompletionSelectionStatus(bestMatch, fullMatch, uniqueMatch),
                    CharsMatchedCount = bestMatchNumCharsMatched >= 0 ? bestMatchNumCharsMatched : 0
                };
            }

            // We didn't find anything.  Return an "empty" match status.
            return null;
        }

        private void RaiseSelectionStatusChanged(CompletionSelectionStatus oldValue)
        {
            EventHandler<ValueChangedEventArgs<CompletionSelectionStatus>> tempHandler = this.SelectionStatusChanged;
            if (tempHandler != null)
            {
                tempHandler(this, new ValueChangedEventArgs<CompletionSelectionStatus>(oldValue, _selectionStatus));
            }
        }

        private bool DoesCompletionMatchApplicabilityText(Completion completion)
        {
            // Determine the match text
            string matchText = string.Empty;
            if (_filterMatchType == CompletionMatchType.MatchDisplayText)
            {
                matchText = completion.DisplayText;
            }
            else if (_filterMatchType == CompletionMatchType.MatchInsertionText)
            {
                matchText = completion.InsertionText;
            }

            // Is the actual buffer text a prefix of the match text?
            return matchText.StartsWith(this.FilterBufferText, !_filterCaseSensitive, CultureInfo.CurrentCulture);
        }
    }
}
