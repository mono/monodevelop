using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text.Differencing;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Differencing.Implementation
{
    /// <summary>
    /// Represents a collection of differences over two lists of same-typed elements,
    /// given a "maximal" match sequence (as generated from a difference algorithm).
    /// You can enumerate over the Differences in this collection.
    /// </summary>
    /// <typeparam name="T">The element type of the compared lists</typeparam>
    internal class DifferenceCollection<T> : IDifferenceCollection<T>
    {
        private IEnumerable<Tuple<int, int>> sequence;
        private IList<T> originalLeft;
        private IList<T> originalRight;

        private IList<Difference> diffs;

        public DifferenceCollection(IList<Difference> diffs, IList<T> originalLeft, IList<T> originalRight)
        {
            this.originalLeft = originalLeft;
            this.originalRight = originalRight;

            this.diffs = diffs;
            this.sequence = new MatchEnumerator(diffs, originalLeft.Count);
        }

        public static Match CreateInitialMatch(int originalStart)
        {
            return (originalStart != 0) ? (new Match(new Span(0, originalStart), new Span(0, originalStart))) : (Match)null;
        }

        public static void AddDifference(int originalStart, int originalEnd, int nextOriginalEnd,
                                         int modifiedStart, int modifiedEnd, int nextModifiedEnd,
                                         IList<Difference> diffs, ref Match before)
        {
            Match after = (originalEnd != nextOriginalEnd) ? (new Match(Span.FromBounds(originalEnd, nextOriginalEnd), Span.FromBounds(modifiedEnd, nextModifiedEnd))) : (Match)null;

            diffs.Add(new Difference(Span.FromBounds(originalStart, originalEnd), Span.FromBounds(modifiedStart, modifiedEnd),
                                     before, after));

            before = after;
        }

        /// <summary>
        /// Get the original match sequence that was used to create this diff collection
        /// </summary>
        public IEnumerable<Tuple<int, int>> MatchSequence
        {
            get { return sequence; }
        }

        /// <summary>
        /// Get the left sequence that was used to create this diff collection
        /// </summary>
        public IList<T> LeftSequence
        {
            get { return originalLeft; }
        }

        /// <summary>
        /// Get the right sequence that was used to create this diff collection
        /// </summary>
        public IList<T> RightSequence
        {
            get { return originalRight; }
        }

        /// <summary>
        /// Get the differences as a list.
        /// If you just want to enumerate over the differences, you can use
        /// the DiffCollection directly, as it is IEnumerable.
        /// </summary>
        public IList<Difference> Differences
        {
            get { return this.diffs; }
        }

        #region Private Helpers

        /// <summary>
        /// Create a list of matches from a given ordered collection of
        /// match pairs (e.g. a MatchSequence).
        /// </summary>
        /// <param name="matches">An ordered collection of matching pairs, like a MatchSequence</param>
        /// <returns>An IList of the generated Matches</returns>
        internal static IList<Match> MatchesFromPairs(IList<Tuple<int, int>> matches)
        {
            if (matches.Count == 0)
            {
                return new List<Match>();
            }

            IList<Match> mranges = new List<Match>();

            Tuple<int, int> firstMatch = matches[0];
            int leftStart = firstMatch.Item1;
            int leftEnd = leftStart + 1;

            int rightStart = firstMatch.Item2;
            int rightEnd = rightStart + 1;

            for (int i = 1; i < matches.Count; i++)
            {
                Tuple<int, int> pair = matches[i];

                if (pair.Item1 == leftEnd &&
                    pair.Item2 == rightEnd)
                {
                    leftEnd++;
                    rightEnd++;
                }
                else
                {
                    mranges.Add(new Match(Span.FromBounds(leftStart, leftEnd), Span.FromBounds(rightStart, rightEnd)));
                    leftStart = pair.Item1;
                    leftEnd = leftStart + 1;

                    rightStart = pair.Item2;
                    rightEnd = rightStart + 1;
                }
            }

            mranges.Add(new Match(Span.FromBounds(leftStart, leftEnd), Span.FromBounds(rightStart, rightEnd)));

            return mranges;
        }

        #endregion

        #region IEnumerable<Difference> Members

        public IEnumerator<Difference> GetEnumerator()
        {
            return diffs.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }

    internal sealed class MatchEnumerator : IEnumerable<Tuple<int, int>>
    {
        IList<Difference> _differences;
        int _leftCount;

        public MatchEnumerator(IList<Difference> differences, int leftCount)
        {
            _differences = differences;
            _leftCount = leftCount;
        }

        public IEnumerator<Tuple<int, int>> GetEnumerator()
        {
            int leftStart = 0;
            int rightStart = 0;
            if (_differences.Count != 0)
            {
                foreach (var difference in _differences)
                {
                    Match m = difference.Before;
                    if (m != null)
                    {
                        for (int i = 0; i < m.Length; i++)
                        {
                            yield return new Tuple<int, int>(m.Left.Start + i, m.Right.Start + i);
                        }
                    }

                    leftStart = difference.Left.End;
                    rightStart = difference.Right.End;
                }
            }

            for (int i = leftStart; (i < _leftCount); ++i)
            {
                yield return new Tuple<int, int>(i, i + rightStart - leftStart);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
