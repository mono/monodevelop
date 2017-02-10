using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text.Differencing;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Differencing.Implementation
{
    internal static class DiffChangeCollectionHelper<T>
    {
        public static DifferenceCollection<T> Create(Microsoft.TeamFoundation.Diff.Copy.IDiffChange[] changes, IList<T> originalLeft, IList<T> originalRight)
        {
            return new DifferenceCollection<T>(CreateDiffs(changes, originalLeft, originalRight), originalLeft, originalRight);
        }

        static IList<Difference> CreateDiffs(Microsoft.TeamFoundation.Diff.Copy.IDiffChange[] changes, IList<T> originalLeft, IList<T> originalRight)
        {
            IList<Difference> diffs = new List<Difference>(changes.Length);

            if (changes.Length != 0)
            {
                //Create the match before (if any)
                var change = changes[0];
                Match before = DifferenceCollection<T>.CreateInitialMatch(change.OriginalStart);

                for (int i = 1; (i < changes.Length); ++i)
                {
                    var nextChange = changes[i];

                    DifferenceCollection<T>.AddDifference(change.OriginalStart, change.OriginalEnd, nextChange.OriginalStart,
                                                          change.ModifiedStart, change.ModifiedEnd, nextChange.ModifiedStart,
                                                          diffs, ref before);

                    change = nextChange;
                }

                DifferenceCollection<T>.AddDifference(change.OriginalStart, change.OriginalEnd, originalLeft.Count,
                                                      change.ModifiedStart, change.ModifiedEnd, originalRight.Count,
                                                      diffs, ref before);
            }

            return diffs;
        }
    }
}
