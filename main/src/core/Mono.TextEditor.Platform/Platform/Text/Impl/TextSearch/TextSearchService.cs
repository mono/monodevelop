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
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Text.RegularExpressions;
    using Microsoft.VisualStudio.Text.Operations;
    using Microsoft.VisualStudio.Text.Utilities;
    using System.Linq;
    using System.Threading;

    [Export(typeof(ITextSearchService))]
    [Export(typeof(ITextSearchService2))]
    // Ensure only a singleton instance is used for all operations
    [PartCreationPolicy(System.ComponentModel.Composition.CreationPolicy.Shared)]
    internal partial class TextSearchService : ITextSearchService2
    {
        [Import]
        ITextStructureNavigatorSelectorService _navigatorSelectorService;

        // Cache of recently used Regex expressions to save on construction
        // of Regex objects.
        static IDictionary<string, WeakReference> _cachedRegexEngines;
        static ReaderWriterLockSlim _regexCacheLock;

        // Maximum number of Regex engines to cache
        const int _maxCachedRegexEngines = 10;

        static TextSearchService()
        {
            _cachedRegexEngines = new Dictionary<string, WeakReference>(_maxCachedRegexEngines);
            _regexCacheLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        }

        #region ITextSearchService Members

        public SnapshotSpan? FindNext(int startIndex, bool wraparound, FindData findData)
        {
            // We allow startIndex to be at the end of the buffer
            if ((startIndex < 0) || (startIndex > findData.TextSnapshotToSearch.Length))
            {
                throw new ArgumentOutOfRangeException("startIndex");
            }

            if (string.IsNullOrEmpty(findData.SearchString))
            {
                throw new ArgumentException("Search pattern can't be empty or null", "findData");
            }

            FindOptions options = findData.FindOptions;

            if (wraparound)
            {
                options |= FindOptions.Wrap;
            }

            if ((findData.FindOptions & FindOptions.UseRegularExpressions) == FindOptions.UseRegularExpressions)
            {
                // Validate regular expression
                GetRegularExpressionMatches(options, findData.SearchString, string.Empty);
            }

            bool wholeWord = (options & FindOptions.WholeWord) == FindOptions.WholeWord;

            // We want to wrap if either the boolean parameter is true or if the option is set via
            // FindOptions
            wraparound |= (findData.FindOptions & FindOptions.Wrap) == FindOptions.Wrap;

            ITextSnapshot snapshot = findData.TextSnapshotToSearch;

            // Remove whole word option, even if set so that new implementation's whole word logic is not used.
            // Instead we use our own legacy whole word logic here to match the old behavior and preserve 
            // backward compatibility.
            foreach (Tuple<SnapshotSpan, string> result in
                FindAllForReplace(new SnapshotPoint(snapshot, startIndex), new SnapshotSpan(snapshot, Span.FromBounds(0, snapshot.Length)),
                findData.SearchString, null, options & ~FindOptions.WholeWord))
            {
                // In case whole word option was requested and the result is not a whole word, then
                // keep looking.
                if (wholeWord && !this.LegacyMatchesAWholeWord(result.Item1, findData))
                {
                    continue;
                }

                return result.Item1;
            }

            return null;
        }

        public Collection<SnapshotSpan> FindAll(FindData findData)
        {
            if (string.IsNullOrEmpty(findData.SearchString))
            {
                throw new ArgumentException("Search pattern can't be empty or null", "findData");
            }

            FindOptions options = findData.FindOptions;

            bool wholeWord = (options & FindOptions.WholeWord) == FindOptions.WholeWord;

            ITextSnapshot snapshot = findData.TextSnapshotToSearch;
            SnapshotSpan searchRange = new SnapshotSpan(snapshot, Span.FromBounds(0, snapshot.Length));

            Collection<SnapshotSpan> results = new Collection<SnapshotSpan>();

            // Remove whole word option, even if set so that new implementation's whole word logic is not used.
            // Instead we use our own legacy whole word logic here to match the old behavior and preserve 
            // backward compatibility.
            foreach (SnapshotSpan result in this.FindAll(searchRange, findData.SearchString, options & ~FindOptions.WholeWord))
            {
                // In case whole word option was requested and the result is not a whole word, then
                // keep looking.
                if (wholeWord && !this.LegacyMatchesAWholeWord(result, findData))
                {
                    continue;
                }

                results.Add(result);
            }

            return results;
        }

        #endregion

        #region ITextSearchService2 Members

        public SnapshotSpan? Find(SnapshotPoint startingPosition, string searchPattern, FindOptions options)
        {
            if (string.IsNullOrEmpty(searchPattern))
            {
                throw new ArgumentException("Pattern can't be empty or null", "searchPattern");
            }

            return Find(startingPosition, new SnapshotSpan(startingPosition.Snapshot, Span.FromBounds(0, startingPosition.Snapshot.Length)), searchPattern, options);
        }

        public SnapshotSpan? Find(SnapshotSpan searchRange, SnapshotPoint startingPosition, string searchPattern, FindOptions options)
        {
            if (string.IsNullOrEmpty(searchPattern))
            {
                throw new ArgumentException("Pattern can't be empty or null", "searchPattern");
            }

            if (searchRange.Snapshot != startingPosition.Snapshot)
            {
                throw new ArgumentException("The search range and search starting position must belong to the same snapshot.");
            }

            if (!ContainedBySpan(searchRange, startingPosition))
            {
                throw new ArgumentException("The search start point must be contained by the search range.");
            }

            return Find(startingPosition, searchRange, searchPattern, options);
        }

        public SnapshotSpan? FindForReplace(SnapshotPoint startingPosition, string searchPattern, string replacePattern, FindOptions options, out string expandedReplacePattern)
        {
            if (string.IsNullOrEmpty(searchPattern))
            {
                throw new ArgumentException("Pattern can't be empty or null", "searchPattern");
            }

            if (replacePattern == null)
            {
                throw new ArgumentNullException("Replace pattern can't be null.", "replacePattern");
            }

            return FindForReplace(startingPosition, new SnapshotSpan(startingPosition.Snapshot, Span.FromBounds(0, startingPosition.Snapshot.Length)),
                searchPattern, replacePattern, options, out expandedReplacePattern);
        }

        public SnapshotSpan? FindForReplace(SnapshotSpan searchRange, string searchPattern, string replacePattern, FindOptions options, out string expandedReplacePattern)
        {
            if (string.IsNullOrEmpty(searchPattern))
            {
                throw new ArgumentException("Pattern can't be empty or null", "searchPattern");
            }

            if (replacePattern == null)
            {
                throw new ArgumentNullException("Replace pattern can't be null.", "replacePattern");
            }

            return FindForReplace(((options & FindOptions.SearchReverse) != FindOptions.SearchReverse) ? searchRange.Start : searchRange.End, searchRange, searchPattern, replacePattern, options, out expandedReplacePattern);
        }

        public IEnumerable<SnapshotSpan> FindAll(SnapshotSpan searchRange, string searchPattern, FindOptions options)
        {
            if (string.IsNullOrEmpty(searchPattern))
            {
                throw new ArgumentException("Pattern can't be empty or null", "searchPattern");
            }

            if (searchRange.Length == 0)
            {
                return new SnapshotSpan[] { };
            }

            return FindAllForReplace(searchRange.Start, searchRange, searchPattern, null, options).Select(r => r.Item1);
        }

        public IEnumerable<SnapshotSpan> FindAll(SnapshotSpan searchRange, SnapshotPoint startingPosition, string searchPattern, FindOptions options)
        {
            if (string.IsNullOrEmpty(searchPattern))
            {
                throw new ArgumentException("Pattern can't be empty or null", "searchPattern");
            }

            if (searchRange.Length == 0)
            {
                return new SnapshotSpan[] { };
            }

            if (searchRange.Snapshot != startingPosition.Snapshot)
            {
                throw new ArgumentException("searchRange and startingPosition parameters must belong to the same snapshot.");
            }

            if (!ContainedBySpan(searchRange, startingPosition))
            {
                throw new InvalidOperationException("Can't perform a search when the startingPosition is not contained by the searchRange.");
            }

            return FindAllForReplace(startingPosition, searchRange, searchPattern, null, options).Select(r => r.Item1);
        }

        public IEnumerable<Tuple<SnapshotSpan, string>> FindAllForReplace(SnapshotSpan searchRange, string searchPattern, string replacePattern, FindOptions options)
        {
            if (string.IsNullOrEmpty(searchPattern))
            {
                throw new ArgumentException("Search pattern can't be null or empty.", "searchPattern");
            }

            if (replacePattern == null)
            {
                throw new ArgumentNullException("Replace pattern can't be null.", "replacePattern");
            }

            return FindAllForReplace(searchRange.Start, searchRange, searchPattern, replacePattern, options);
        }

        #endregion

        #region Private Helpers

        private static SnapshotSpan? Find(SnapshotPoint startPosition, SnapshotSpan searchRange, string searchPattern, FindOptions options)
        {
            // Perform a find all and return the first result if any is available. Note that find all is an enumerator and won't search the
            // entire range unless it is necessary.
            Tuple<SnapshotSpan, string> match = FindAllForReplace(startPosition, searchRange, searchPattern, null, options).FirstOrDefault();

            if (match != null)
            {
                return match.Item1;
            }

            return null;
        }

        private static SnapshotSpan? FindForReplace(SnapshotPoint startPosition, SnapshotSpan searchRange, string searchPattern, string replacePattern, FindOptions options, out string expandedReplacePattern)
        {
            // Set this value to empty to adhere to the contract (i.e. when no matches are found, this value should be the empty string)
            expandedReplacePattern = string.Empty;

            // Perform a find all for replace over the range of interest. Note that this operation is lazy and will stop when the first result is
            // found.
            Tuple<SnapshotSpan, string> match = FindAllForReplace(startPosition, searchRange, searchPattern, replacePattern, options).FirstOrDefault();

            if (match != null)
            {
                expandedReplacePattern = match.Item2;
                return match.Item1;
            }

            return null;
        }

        private static IEnumerable<Tuple<SnapshotSpan, string>> FindAllForReplace(SnapshotPoint startPosition, SnapshotSpan searchRange, string searchPattern, string replacePattern, FindOptions options)
        {
            bool multiLine = (options & FindOptions.Multiline) == FindOptions.Multiline;
            bool wholeWord = (options & FindOptions.WholeWord) == FindOptions.WholeWord;

            IEnumerable<Tuple<SnapshotSpan, string>> searchResults = null;

            // Perform the search depending on whether we are forced to perform multi-line search
            if (multiLine)
            {
                searchResults = FindMultiline(startPosition, searchRange, options, searchPattern, replacePattern);
            }
            else
            {
                searchResults = FindSingleLine(startPosition, searchRange, options, searchPattern, replacePattern);
            }

            foreach (Tuple<SnapshotSpan, string> searchResult in searchResults)
            {
                // Don't accept the result if whole word option is selected and the result
                // is not a whole word
                if (wholeWord && !IsWholeWord(searchResult.Item1))
                {
                    continue;
                }

                // found a suitable match, return it!
                yield return searchResult;
            }
        }

        private static IEnumerable<Tuple<SnapshotSpan, string>> FindMultiline(SnapshotPoint startPosition, SnapshotSpan searchRange, FindOptions options, string searchPattern, string replacePattern = null)
        {
            Debug.Assert(searchRange.Contains(startPosition) || searchRange.End == startPosition);

            // This is pretty disgusting since we have to search including line endings
            // we have to look at the entire string. There can be optimizations done for specific cases
            // where one can count the number of line endings that a search pattern could at most include
            // and then search chunks with that many line endings at a time, but for patterns where the quantifier
            // is * this can't be done; e.g. a.*[\n]*.*b
            string rangeText = searchRange.GetText();

            bool wrap = (options & FindOptions.Wrap) == FindOptions.Wrap;

            foreach (var result in FindInString(searchRange.Start, startPosition - searchRange.Start, rangeText, options, searchPattern, replacePattern))
            {
                yield return result;
            }

            if (wrap)
            {
                bool reverse = (options & FindOptions.SearchReverse) == FindOptions.SearchReverse;

                foreach (var result in FindInString(searchRange.Start, reverse ? rangeText.Length : 0, rangeText, options, searchPattern, replacePattern))
                {
                    // Since we are wrapping, check the validity of the result to ensure it was not returned
                    // in our first search above
                    if (reverse)
                    {
                        if (result.Item1.End <= startPosition)
                        {
                            break;
                        }
                    }
                    else
                    {
                        if (result.Item1.Start >= startPosition)
                        {
                            break;
                        }
                    }

                    yield return result;
                }
            }
        }

        private static IEnumerable<Tuple<SnapshotSpan, string>> FindSingleLine(SnapshotPoint startPosition, SnapshotSpan searchRange, FindOptions options, string searchPattern, string replacePattern = null)
        {
            Debug.Assert(searchRange.Contains(startPosition) || searchRange.End == startPosition);

            bool reverse = (options & FindOptions.SearchReverse) == FindOptions.SearchReverse;
            bool regEx = (options & FindOptions.UseRegularExpressions) == FindOptions.UseRegularExpressions;
            bool wrap = (options & FindOptions.Wrap) == FindOptions.Wrap;

            ITextSnapshot snapshot = startPosition.Snapshot;
            ITextSnapshotLine startLine = startPosition.GetContainingLine();
            int lineIncrement = reverse ? -1 : 1;
            int startLineNumber = startLine.LineNumber;
            int searchLineStart = searchRange.Start.GetContainingLine().LineNumber;
            int searchLineEnd = searchRange.End.GetContainingLine().LineNumber;

            SnapshotSpan firstLineSpan = startLine.ExtentIncludingLineBreak.Intersection(searchRange).Value;

            // Find results in "first part" of the first line, then search the rest of the range. If wrap is on, afterwards wrap and search
            // the rest of the range, then finally check the first line again and search its "second part".

            // We are effecitvely splitting the first line into the following two portions:
            // 
            // FirstLineStart --------------------- startPosition --------------------- FirstLineEnd
            //
            // Depending on the direction of the search, one of the pieces will be searched first, and the other will be searched if
            // wrap is on towards the bottom of the algorithm after all other lines have been searched.

            // Examine first line (regEx can match 0 length strings so we can't exclude that case).
            if ((firstLineSpan.Length > 0) || regEx)
            {
                // Regular expression searches start from the index passed - 1 in reverse direction, as such, if we are at a line boundary
                // we need to be examining the line before
                bool skip = regEx && reverse && startPosition != 0 && startLine.Start == startPosition;

                if (!skip)
                {
                    foreach (var result in FindInString(firstLineSpan.Start, startPosition - firstLineSpan.Start, firstLineSpan.GetText(), options, searchPattern, replacePattern))
                    {
                        yield return result;
                    }
                }
            }

            // Examine the rest of the range in the original direction
            for (int i = startLineNumber + lineIncrement; (i >= searchLineStart && i <= searchLineEnd); i += lineIncrement)
            {
                foreach (var result in FindInLine(i, searchRange, options, searchPattern, replacePattern))
                {
                    yield return result;
                }
            }

            // If wrap is enabled, now wrap around the search boundary and continue the search
            if (wrap)
            {
                for (int i = reverse ? searchLineEnd : searchLineStart; i != startLineNumber; i += lineIncrement)
                {
                    foreach (var result in FindInLine(i, searchRange, options, searchPattern, replacePattern))
                    {
                        yield return result;
                    }
                }

                // Finally look at the remainder of the first line
                foreach (var result in FindInString(firstLineSpan.Start, reverse ? firstLineSpan.End - firstLineSpan.Start : 0,
                    firstLineSpan.GetText(), options, searchPattern, replacePattern))
                {
                    // Since we are wrapping, check the validity of the result to ensure it was not returned
                    // in our first search of the first line before wrapping
                    if (reverse)
                    {
                        if (result.Item1.End.Position <= startPosition.Position)
                        {
                            break;
                        }
                    }
                    else
                    {
                        if (result.Item1.Start >= startPosition)
                        {
                            break;
                        }
                    }

                    yield return result;
                }
            }
        }

        private static IEnumerable<Tuple<SnapshotSpan, string>> FindInLine(int lineNumber, SnapshotSpan searchRange, FindOptions options, string searchPattern, string replacePattern)
        {
            bool reverse = (options & FindOptions.SearchReverse) == FindOptions.SearchReverse;
            ITextSnapshotLine line = searchRange.Snapshot.GetLineFromLineNumber(lineNumber);
            SnapshotSpan? searchSpan = searchRange.Intersection(line.ExtentIncludingLineBreak);

            if (searchSpan.HasValue)
            {
                foreach (var result in FindInString(searchSpan.Value.Start, reverse ? searchSpan.Value.End - searchSpan.Value.Start : 0, searchSpan.Value.GetText(), options, searchPattern, replacePattern))
                {
                    yield return result;
                }
            }
        }

        private static bool IsWholeWord(SnapshotSpan result)
        {
            if (result.Length < 1)
            {
                return false;
            }

            return UnicodeWordExtent.IsWholeWord(result);
        }

        private static bool ContainedBySpan(SnapshotSpan searchRange, SnapshotPoint startingPosition)
        {
            // We make an exception for spans to contain points if the end point of the span matches the startingPosition.
            // For backwards searches, this allows the consumer to find a point at the very end of the searchRange, for
            // forward searches, this has no effect.
            return searchRange.Contains(startingPosition) || searchRange.End == startingPosition;
        }

        private static StringComparison GetStringComparison(FindOptions findOptions)
        {
            bool matchCase = ((findOptions & FindOptions.MatchCase) == FindOptions.MatchCase);
            bool cultureSensitive = ((findOptions & FindOptions.OrdinalComparison) != FindOptions.OrdinalComparison);

            if (cultureSensitive)
            {
                if (matchCase)
                {
                    return StringComparison.CurrentCulture;
                }
                else
                {
                    return StringComparison.CurrentCultureIgnoreCase;
                }
            }
            else
            {
                if (matchCase)
                {
                    return StringComparison.Ordinal;
                }
                else
                {
                    return StringComparison.OrdinalIgnoreCase;
                }
            }
        }

        /// <summary>
        /// Returns collection of matches of a regular expression search over the provided input string.
        /// </summary>
        private static MatchCollection GetRegularExpressionMatches(FindOptions options, string searchTerm, string toSearch, int startingIndex = -1)
        {
            RegexOptions regExOptions = GetRegexOptions(options);

            try
            {
                Regex engine = GetOrCreateCachedRegex(regExOptions, searchTerm);

                if (startingIndex == -1)
                {
                    return engine.Matches(toSearch);
                }
                else
                {
                    return engine.Matches(toSearch, startingIndex);
                }
            }
            catch (ArgumentException e)
            {
                throw new ArgumentException("Invalid regular expression", e);
            }
        }

        private static Regex GetOrCreateCachedRegex(RegexOptions options, string searchTerm)
        {
            // Try to reduce the cache entries if need be
            ScavengeRegexCache();

            string key = GetRegexKey(options, searchTerm);
            Regex result = null;

            try
            {
                _regexCacheLock.EnterReadLock();

                WeakReference regexReference = null;
                if (_cachedRegexEngines.TryGetValue(key, out regexReference) && regexReference.IsAlive)
                {
                    result = regexReference.Target as Regex;
                }
            }
            finally
            {
                _regexCacheLock.ExitReadLock();
            }

            if (result == null)
            {
                // Either the key was not found, or it was found and the reference is dead
                result = new Regex(searchTerm, options);

                try
                {
                    _regexCacheLock.EnterWriteLock();

                    // Cache the value
                    _cachedRegexEngines[key] = new WeakReference(result);
                }
                finally
                {
                    _regexCacheLock.ExitWriteLock();
                }
            }

            return result;
        }

        private static void ScavengeRegexCache()
        {
            // If we have reached our maximum capacity, do a pass and kill inactive references
            if (_maxCachedRegexEngines == _cachedRegexEngines.Count)
            {
                try
                {
                    _regexCacheLock.EnterWriteLock();

                    // Keep a list of stale keys
                    string[] staleKeys = new string[_maxCachedRegexEngines];

                    int staleKeyIterator = 0;

                    // Record all stale keys
                    foreach (var keyValuePair in _cachedRegexEngines)
                    {
                        if (!keyValuePair.Value.IsAlive)
                        {
                            staleKeys[staleKeyIterator++] = keyValuePair.Key;
                        }
                    }

                    // Remove stale keys
                    for (int i = 0; i < staleKeyIterator; i++)
                    {
                        _cachedRegexEngines.Remove(staleKeys[i]);
                    }
                }
                finally
                {
                    _regexCacheLock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Creates a unique key for the combination of regular expression options and the search term used. Logic
        /// is copied from ndp/fx/src/Regex/System/Text/RegularExpressions/Regex.cs
        /// </summary>
        private static string GetRegexKey(RegexOptions options, string searchTerm)
        {
            string cultureKey = null;

            // Try to look up this regex in the cache.  We do this regardless of whether useCache is true since there's
            // really no reason not to. 
            if ((options & RegexOptions.CultureInvariant) != 0)
                cultureKey = System.Globalization.CultureInfo.InvariantCulture.ToString(); // "English (United States)"
            else
                cultureKey = System.Globalization.CultureInfo.CurrentCulture.ToString();

            return ((int)options).ToString(System.Globalization.NumberFormatInfo.InvariantInfo) + ":" + cultureKey + ":" + searchTerm;
        }

        /// <summary>
        /// Converts regular find options to regular expression options.
        /// </summary>
        private static RegexOptions GetRegexOptions(FindOptions options)
        {
            RegexOptions regExOptions = RegexOptions.None;

            if ((options & FindOptions.Multiline) == FindOptions.Multiline)
            {
                regExOptions |= RegexOptions.Multiline;
            }

            if ((options & FindOptions.SingleLine) == FindOptions.SingleLine)
            {
                regExOptions |= RegexOptions.Singleline;
            }

            if ((options & FindOptions.MatchCase) != FindOptions.MatchCase)
            {
                regExOptions |= RegexOptions.IgnoreCase;
            }

            if ((options & FindOptions.SearchReverse) == FindOptions.SearchReverse)
            {
                regExOptions |= RegexOptions.RightToLeft;
            }

            return regExOptions;
        }

        /// <summary>
        /// Determines whether the given match qualifies as a whole word using <see cref="ITextStructureNavigator"/>.
        /// </summary>
        /// <remarks>
        /// In the new implementation, we don't use any external parties to determine word boundaries, rather we pick a small
        /// predictable range of characters and use them as word splitters. The primary reason for this is performance as calls
        /// to external components could be slow. Further, we want the search service to be a dumb but very predictable and simple
        /// text based search service so we enforce our own rules in the new implementation.
        /// </remarks>
        private bool LegacyMatchesAWholeWord(SnapshotSpan result, FindData findData)
        {
            ITextStructureNavigator textStructureNavigator = findData.TextStructureNavigator;

            if (textStructureNavigator == null)
            {
                // the navigator was never set; create one without benefit of context
                textStructureNavigator = _navigatorSelectorService.GetTextStructureNavigator(findData.TextSnapshotToSearch.TextBuffer);
            }

            // We'll need to get the left extent and right extent in case the match spans across multiple words
            Microsoft.VisualStudio.Text.Operations.TextExtent leftExtent = textStructureNavigator.GetExtentOfWord(result.Start);
            Microsoft.VisualStudio.Text.Operations.TextExtent rightExtent = textStructureNavigator.GetExtentOfWord(result.Length > 0 ? result.End - 1 : result.End);

            return (result.Start == leftExtent.Span.Start) && (result.End == rightExtent.Span.End);
        }

        private static IEnumerable<Tuple<SnapshotSpan, string>> FindInString(SnapshotPoint snapshotOffset, int searchStartIndex, string textData, FindOptions options, string searchTerm, string replaceTerm = null)
        {
            bool reverse = (options & FindOptions.SearchReverse) == FindOptions.SearchReverse;
            bool regEx = (options & FindOptions.UseRegularExpressions) == FindOptions.UseRegularExpressions;

            if (regEx)
            {
                // Perform search with regular expressions
                foreach (Match match in GetRegularExpressionMatches(options, searchTerm, textData, searchStartIndex))
                {
                    SnapshotSpan matchSpan = new SnapshotSpan(snapshotOffset.Snapshot, new Span(snapshotOffset + match.Index, match.Length));
                    string replacementValue = replaceTerm != null ? match.Result(replaceTerm) : null;

                    yield return Tuple.Create<SnapshotSpan, string>(matchSpan, replacementValue);
                }
            }
            else
            {
                // Perform raw string search
                while ((reverse && searchStartIndex > 0) || (!reverse && searchStartIndex < textData.Length))
                {
                    // Look for the next match
                    // Note: LastIndexOf performs a search inclusive of the position passed to it, but we want reverse searches
                    // to be exclusive of the start position.
                    int matchIndex = reverse ?
                        textData.LastIndexOf(searchTerm, searchStartIndex - 1, GetStringComparison(options)) :
                        textData.IndexOf(searchTerm, searchStartIndex, GetStringComparison(options));

                    if (matchIndex == -1)
                    {
                        // Couldn't find any more results
                        yield break;
                    }

                    SnapshotSpan potentialResult = new SnapshotSpan(snapshotOffset.Snapshot, snapshotOffset + matchIndex, searchTerm.Length);

                    yield return Tuple.Create<SnapshotSpan, string>(potentialResult, replaceTerm);

                    // Prepare for next search
                    // 
                    // If the search is forward, we want to continue one character next to the start of the previous result, for instance, consider
                    // searching for "aa" in "aaa".
                    // If the search is backward, we want to continue one character prior to the end of the previous result, again, consider searching
                    // for "aa" in "aaa" from the end of the buffer. Notice that even though the result span does not include its .End position, we
                    // subtract only one, because the search performed above is not inclusive of the start index for reverse direction
                    searchStartIndex = (reverse ? potentialResult.End.Position - 1 : potentialResult.Start.Position + 1) - snapshotOffset.Position;
                }
            }
        }

        #endregion
    }
}
