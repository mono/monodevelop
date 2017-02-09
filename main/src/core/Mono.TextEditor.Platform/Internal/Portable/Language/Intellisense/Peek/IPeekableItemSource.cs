// Copyright (c) Microsoft Corporation
// All rights reserved

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Provides <see cref="IPeekableItem"/>s for a given content type.
    /// </summary>
    /// <remarks>Peek providers implement <see cref="IPeekableItemSource"/> interface and provide instances via exported 
    /// <see cref="IPeekableItemSourceProvider"/> MEF component part.</remarks>
    public interface IPeekableItemSource : IDisposable
    {
        /// <summary>
        /// Determines which <see cref="IPeekableItem"/>s should be part of the specified <see cref="IPeekSession"/>.
        /// </summary>
        /// <param name="session">The session for which to compute <see cref="IPeekableItem"/>s.</param>
        /// <param name="peekableItems">The list of <see cref="IPeekableItem"/>s to add to the session.</param>
        /// <remarks>
        /// Each applicable <see cref="IPeekableItemSource"/> instance will be called in order when
        /// recalculating an <see cref="IPeekSession"/>. <see cref="IPeekableItem"/>s can be added to the session by adding
        /// them to the <paramref name="peekableItems"/> collection passed in as a parameter. In addition, by removing items 
        /// from the collection, a source may filter <see cref="IPeekableItem"/>s provided by 
        /// <see cref="IPeekableItemSource"/>s earlier in the calculation chain.
        /// </remarks>
        void AugmentPeekSession(IPeekSession session, IList<IPeekableItem> peekableItems);
    }
}
