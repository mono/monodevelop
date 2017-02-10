using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Text.Differencing.Implementation
{
    /// <summary>
    /// Generate a maximal common subsequence (or longest common subsequence) of
    /// two sequences (ILists).
    /// </summary>
    [Export(typeof(IDifferenceService))]
    internal sealed class MaximalSubsequenceAlgorithm : IDifferenceService
    {
        #region IDifferenceService Members
        static readonly Microsoft.TeamFoundation.Diff.Copy.IDiffChange[] Empty = new Microsoft.TeamFoundation.Diff.Copy.IDiffChange[0];
        
        public IDifferenceCollection<T> DifferenceSequences<T>(IList<T> left, IList<T> right)
        {
            return DifferenceSequences<T>(left, right, null);
        }

        public IDifferenceCollection<T> DifferenceSequences<T>(IList<T> left, IList<T> right, ContinueProcessingPredicate<T> continueProcessingPredicate)
        {
            return DifferenceSequences<T>(left, right, left, right, continueProcessingPredicate);
        }

        #endregion

        internal static DifferenceCollection<T> DifferenceSequences<T>(IList<T> left, IList<T> right, IList<T> originalLeft, IList<T> originalRight, ContinueProcessingPredicate<T> continueProcessingPredicate)
        {
            if (left == null)
                throw new ArgumentNullException("left");
            if (right == null)
                throw new ArgumentNullException("right");

            Microsoft.TeamFoundation.Diff.Copy.IDiffChange[] changes;
            if ((left.Count == 0) || (right.Count == 0))
            {
                if ((left.Count == 0) && (right.Count == 0))
                {
                    changes = MaximalSubsequenceAlgorithm.Empty;
                }
                else
                {
                    changes = new Microsoft.TeamFoundation.Diff.Copy.IDiffChange[1];
                    changes[0] = new Microsoft.TeamFoundation.Diff.Copy.DiffChange(0, left.Count,
                                                                              0, right.Count);
                }
            }
            else
                changes = ComputeMaximalSubsequence<T>(left, right, continueProcessingPredicate);

            return DiffChangeCollectionHelper<T>.Create(changes, originalLeft, originalRight);
        }

        private static Microsoft.TeamFoundation.Diff.Copy.IDiffChange[] ComputeMaximalSubsequence<T>(IList<T> left, IList<T> right, ContinueProcessingPredicate<T> continueProcessingPredicate)
        {
            var lcs = new Microsoft.TeamFoundation.Diff.Copy.LcsDiff<T>();
            var diffs = lcs.Diff(left, right, EqualityComparer<T>.Default, (continueProcessingPredicate == null)
                                                                        ? (Microsoft.TeamFoundation.Diff.Copy.ContinueDifferencePredicate<T>)null
                                                                        : (int originalIndex, IList<T> originalSequence, int longestMatchSoFar) => { return continueProcessingPredicate(originalIndex, originalSequence, longestMatchSoFar);});

            return diffs;
        }
    }
}