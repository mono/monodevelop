using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Text.Differencing.Implementation
{
// Ignore deprecated (IHierarchicalStringDifferenceService is deprecated)
#pragma warning disable 0618

    [Export(typeof(IHierarchicalStringDifferenceService))]
    internal sealed class DefaultTextDifferencingService : ITextDifferencingService, IHierarchicalStringDifferenceService
    {
        #region ITextDifferencingService-specific

        public IHierarchicalDifferenceCollection DiffSnapshotSpans(SnapshotSpan left, SnapshotSpan right, StringDifferenceOptions differenceOptions)
        {
            return DiffSnapshotSpans(left, right, differenceOptions, DefaultGetLineTextCallback);
        }

        public IHierarchicalDifferenceCollection DiffSnapshotSpans(SnapshotSpan left, SnapshotSpan right, StringDifferenceOptions differenceOptions, Func<ITextSnapshotLine, string> getLineTextCallback)
        {
            if (left == null)
                throw new ArgumentNullException("left");
            if (right == null)
                throw new ArgumentNullException("right");

            var leftDecompositions = new SnapshotSpanDecompositionListMaker(left, differenceOptions, getLineTextCallback);
            var rightDecompositions = new SnapshotSpanDecompositionListMaker(right, differenceOptions, getLineTextCallback);

            return DiffText(leftDecompositions, rightDecompositions, differenceOptions);
        }

        public IHierarchicalDifferenceCollection DiffStrings(string left, string right, StringDifferenceOptions differenceOptions)
        {
            if (left == null)
                throw new ArgumentNullException("left");
            if (right == null)
                throw new ArgumentNullException("right");

            var leftDecompositions = new StringDecompositionListMaker(left, differenceOptions);
            var rightDecompositions = new StringDecompositionListMaker(right, differenceOptions);

            return DiffText(leftDecompositions, rightDecompositions, differenceOptions);
        }

        IHierarchicalDifferenceCollection DiffText(DecompositionListMaker left, DecompositionListMaker right, StringDifferenceOptions differenceOptions)
        {
            if (left == null)
                throw new ArgumentNullException("left");
            if (right == null)
                throw new ArgumentNullException("right");

            StringDifferenceTypes differenceType = differenceOptions.DifferenceType;
            if (differenceType == 0)
                throw new ArgumentOutOfRangeException("differenceOptions");

            if ((differenceType & StringDifferenceTypes.Line) != 0)
            {
                //The DecompositionListMaker creates a new copy of the list so make sure we only ask once
                var leftUnfilteredLineList = left.UnfilteredLineList;
                var rightUnfilteredLineList = right.UnfilteredLineList;

                var diffCollection = ComputeMatches(differenceType, differenceOptions, left.FilteredLineList, right.FilteredLineList, leftUnfilteredLineList, rightUnfilteredLineList);

                //Try to clean up the differences so things align better with the block structure of typical languages.
                diffCollection = FinessedDifferenceCollection.FinesseLineDifferences(diffCollection, leftUnfilteredLineList, rightUnfilteredLineList);

                StringDifferenceOptions nextOptions = new StringDifferenceOptions(differenceOptions);
                nextOptions.DifferenceType &= ~StringDifferenceTypes.Line;

                return new HierarchicalDifferenceCollection(diffCollection, leftUnfilteredLineList, rightUnfilteredLineList, this, nextOptions);
            }
            else if ((differenceType & StringDifferenceTypes.Word) != 0)
            {
                var leftWords = left.WordList;
                var rightWords = right.WordList;

                var diffCollection = ComputeMatches(StringDifferenceTypes.Word, differenceOptions, leftWords, rightWords);

                StringDifferenceOptions nextOptions = new StringDifferenceOptions(differenceOptions);
                nextOptions.DifferenceType &= ~StringDifferenceTypes.Word;

                return new HierarchicalDifferenceCollection(diffCollection, leftWords, rightWords, this, nextOptions);
            }
            else if ((differenceType & StringDifferenceTypes.Character) != 0)
            {
                var leftChars = left.CharacterList;
                var rightChars = right.CharacterList;

                var diffCollection = ComputeMatches(StringDifferenceTypes.Character, differenceOptions, leftChars, rightChars);

                // This should always be 0.
                StringDifferenceOptions nextOptions = new StringDifferenceOptions(differenceOptions);
                nextOptions.DifferenceType &= ~StringDifferenceTypes.Character;

                Debug.Assert(nextOptions.DifferenceType == 0,
                    "After character differencing, the difference type should be empty (invalid).");

                return new HierarchicalDifferenceCollection(diffCollection, leftChars, rightChars, this, nextOptions);
            }
            else
            {
                throw new ArgumentOutOfRangeException("differenceOptions");
            }
        }

        internal static string DefaultGetLineTextCallback(ITextSnapshotLine line)
        {
            return line.GetTextIncludingLineBreak();
        }

        static IDifferenceCollection<string> ComputeMatches(StringDifferenceTypes differenceType, StringDifferenceOptions differenceOptions,
                                           IList<string> leftSequence, IList<string> rightSequence)
        {
            return ComputeMatches(differenceType, differenceOptions, leftSequence, rightSequence, leftSequence, rightSequence);
        }

        static IDifferenceCollection<string> ComputeMatches(StringDifferenceTypes differenceType, StringDifferenceOptions differenceOptions,
                                                           IList<string> leftSequence, IList<string> rightSequence,
                                                           IList<string> originalLeftSequence, IList<string> originalRightSequence)
        {
            return MaximalSubsequenceAlgorithm.DifferenceSequences(leftSequence, rightSequence, originalLeftSequence, originalRightSequence, differenceOptions.ContinueProcessingPredicate);
        }

        #endregion
    }

#pragma warning restore 0618
}
