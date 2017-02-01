// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Diagnostics;

    /// <summary>
    /// Handy list-oriented utilities.
    /// </summary>
    internal static class ListUtilities
    {
        /// <summary>
        /// Do a binary search in <paramref name="list"/> for an element that matches <paramref name="target"/>
        /// </summary>
        /// <param name="list">List to search.</param>
        /// <param name="target">Object of the search.</param>
        /// <param name="compare">Comparison function between an element and target (returns &lt; 0 if e comes before t, 0 if e matches, &gt; 0 if e comes after).
        /// <param name="index">Index of the matching element (or, if there is no exact match, index of the element that follows it).</param>
        /// <returns>true if an exact match was found.</returns>
        /// <remarks>Yes, I know there is List.BinarySearch but that doesn't do exactly what I need most of the time.</remarks>
        public static bool BinarySearch<E>(IList<E> list, Func<E, int> compare, out int index)
        {
            int lo = 0;
            int hi = list.Count;

            while (lo < hi)
            {
                index = (lo + hi) / 2;

                int cmp = compare(list[index]);
                if (cmp < 0)
                {
                    lo = index + 1;
                }
                else if (cmp == 0)
                {
                    return true;
                }
                else
                {
                    hi = index;
                }
            }

            index = lo;
            return false;
        }
    }
}