////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation. All rights reserved
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Defines a provider of smart tag data.
    /// </summary>
    [Obsolete(SmartTag.DeprecationMessage)]
    public interface ISmartTagSource : IDisposable
    {
        /// <summary>
        /// Determines which <see cref="SmartTagActionSet"/>s should be part of the specified <see cref="ISmartTagSession"/>.
        /// </summary>
        /// <param name="session">The session for which completions are to be computed.</param>
        /// <param name="smartTagActionSets">The set of the <see cref="SmartTagActionSet"/>s to be added to the session.</param>
        /// <remarks>
        /// Each applicable <see cref="ISmartTagSource.AugmentSmartTagSession"/> instance will be called in-order to (re)calculate
        /// a <see cref="ISmartTagSession"/>.  <see cref="SmartTagActionSet"/>s can be added to the session by adding them to the
        /// smartTagActionSets collection passed-in as a parameter.  In addition, by removing items from the collection, a source
        /// may filter <see cref="SmartTagActionSet"/>s provided by <see cref="ISmartTagSource"/>s earlier in the calculation chain.
        /// </remarks>
        void AugmentSmartTagSession(ISmartTagSession session, IList<SmartTagActionSet> smartTagActionSets);
    }
}
