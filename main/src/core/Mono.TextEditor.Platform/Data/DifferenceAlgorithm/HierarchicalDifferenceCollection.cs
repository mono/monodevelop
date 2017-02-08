using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Differencing.Implementation
{
    /// <summary>
    /// The default implementation of IDecomposedDifferenceCollection.  This maintains
    /// a given IDifferenceCollection&lt;string&gt; and a mapping of Difference indices to
    /// contained differences (if there are any).
    /// </summary>
    internal class HierarchicalDifferenceCollection : IHierarchicalDifferenceCollection
    {
        private readonly ITokenizedStringListInternal left;
        private readonly ITokenizedStringListInternal right;
        private readonly IDifferenceCollection<string> differenceCollection;
        private readonly ITextDifferencingService differenceService;
        private readonly StringDifferenceOptions options;

        private readonly ConcurrentDictionary<int, IHierarchicalDifferenceCollection> containedDifferences;

        /// <summary>
        /// Create a new hierarchical difference collection.
        /// </summary>
        /// <param name="differenceCollection">The underlying difference collection for this level
        /// of the hierarchy.</param>
        /// <param name="differenceService">The difference service to use for doing the next level of
        /// differencing</param>
        /// <param name="options">The options to use for the next level of differencing.
        /// If <see cref="StringDifferenceOptions.DifferenceType" /> is <c>0</c>, then
        /// no further differencing will take place.</param>
        public HierarchicalDifferenceCollection(IDifferenceCollection<string> differenceCollection,
                                                ITokenizedStringListInternal left,
                                                ITokenizedStringListInternal right,
                                                ITextDifferencingService differenceService,
                                                StringDifferenceOptions options)
        {
            if (differenceCollection == null)
                throw new ArgumentNullException("differenceCollection");
            if (left == null)
                throw new ArgumentNullException("left");
            if (right == null)
                throw new ArgumentNullException("right");
            if (!object.ReferenceEquals(left, differenceCollection.LeftSequence))
                throw new ArgumentException("left must equal differenceCollection.LeftSequence");
            if (!object.ReferenceEquals(right, differenceCollection.RightSequence))
                throw new ArgumentException("right must equal differenceCollection.RightSequence");

            this.left = left;
            this.right = right;

            this.differenceCollection = differenceCollection;
            this.differenceService = differenceService;
            this.options = options;

            containedDifferences = new ConcurrentDictionary<int, IHierarchicalDifferenceCollection>();
        }

        #region IHierarchicalDifferenceCollection Members

        public ITokenizedStringList LeftDecomposition
        {
            get { return left; }
        }

        public ITokenizedStringList RightDecomposition
        {
            get { return right; }
        }

        public IHierarchicalDifferenceCollection GetContainedDifferences(int index)
        {
            if (options.DifferenceType == 0)
                return null;

            return containedDifferences.GetOrAdd(index, CalculateContainedDiff);
        }

        /// <summary>
        /// Calculate the contained difference at the given index.  Used by the concurrent dictionary's
        /// GetOrAdd that takes a value factory.
        /// </summary>
        private IHierarchicalDifferenceCollection CalculateContainedDiff(int index)
        {
            // We need to compute the next level of differences.
            var diff = this.Differences[index];

            if (diff.DifferenceType == DifferenceType.Change)
            {
                Span leftSpan = this.left.GetSpanInOriginal(diff.Left);
                Span rightSpan = this.right.GetSpanInOriginal(diff.Right);

                string leftString = this.left.OriginalSubstring(leftSpan.Start, leftSpan.Length);
                string rightString = this.right.OriginalSubstring(rightSpan.Start, rightSpan.Length);

                return differenceService.DiffStrings(leftString, rightString, options);
            }

            return null;
        }

        public bool HasContainedDifferences(int index)
        {
            return GetContainedDifferences(index) != null;
        }

        #endregion

        #region IDifferenceCollection<string> Members

        public IList<Difference> Differences
        {
            get { return differenceCollection.Differences;  }
        }

        public IList<string> LeftSequence
        {
            get { return left; }
        }

        public IEnumerable<Tuple<int, int>> MatchSequence
        {
            get { return differenceCollection.MatchSequence; }
        }

        public IList<string> RightSequence
        {
            get { return right; }
        }

        #endregion

        #region IEnumerable<Difference> Members

        public IEnumerator<Difference> GetEnumerator()
        {
            return differenceCollection.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
