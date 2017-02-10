////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation. All rights reserved
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Defines a QuickInfo provider, which acts as a provider of QuickInfo information over a text buffer.
    /// </summary>
    public interface IQuickInfoSource : IDisposable
    {
        /// <summary>
        /// Determines which pieces of QuickInfo content should be part of the specified <see cref="IQuickInfoSession"/>.
        /// </summary>
        /// <param name="session">The session for which completions are to be computed.</param>
        /// <param name="quickInfoContent">The QuickInfo content to be added to the session.</param>
        /// <param name="applicableToSpan">The <see cref="ITrackingSpan"/> to which this session applies.</param>
        /// <remarks>
        /// Each applicable <see cref="IQuickInfoSource.AugmentQuickInfoSession"/> instance will be called in-order to (re)calculate a
        /// <see cref="IQuickInfoSession"/>. Objects can be added to the session by adding them to the quickInfoContent collection
        /// passed-in as a parameter.  In addition, by removing items from the collection, a source may filter content provided by
        /// <see cref="IQuickInfoSource"/>s earlier in the calculation chain.
        /// </remarks>
        void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> quickInfoContent, out ITrackingSpan applicableToSpan);
    }
}
