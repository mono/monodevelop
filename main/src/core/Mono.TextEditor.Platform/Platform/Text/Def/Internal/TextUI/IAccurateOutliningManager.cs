//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Outlining
{
    using Microsoft.VisualStudio.Text.Editor;
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Extension of IOutliningManager to allow it to get accurate (if slow) results from the outlining taggers.
    /// </summary>
    /// <remarks>
    /// <para>This interface only contains the minimal number of overloads of IOutliningManager methods to make restoring regions when opening a file
    /// work. More overloads can be added as needed.</para>
    /// </remarks>
    public interface IAccurateOutliningManager : IOutliningManager
    {
        #region Collapsing and Expanding

        /// <summary>
        /// Collapses all regions that match the specified predicate.
        /// </summary>
        /// <param name="span">The regions that intersect this span.</param>
        /// <param name="match">The predicate to match.</param>
        /// <returns>The newly-collapsed regions.</returns>
        /// <remarks>
        /// The <paramref name="match"/> predicate may be passed regions that cannot actually be collapsed, due
        /// to the region being partially obscured by another already collapsed region (either pre-existing or collapsed
        /// in an earlier call to the predicate).  The elements of the returned enumeration do accurately track
        /// the regions that were collapsed, so they may differ from the elements for which the predicate returned <c>true</c>.
        /// </remarks>
        IEnumerable<ICollapsed> CollapseAll(SnapshotSpan span, Predicate<ICollapsible> match, CancellationToken cancel);

        #endregion
    }
}
