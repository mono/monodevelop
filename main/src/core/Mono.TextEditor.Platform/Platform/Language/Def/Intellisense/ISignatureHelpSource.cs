////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Defines a provider of signature help information that is used in the IntelliSense process.
    /// </summary>
    public interface ISignatureHelpSource : IDisposable
    {
        /// <summary>
        /// Determines which <see cref="ISignature"/>s should be part of the specified <see cref="ISignatureHelpSession"/>.
        /// </summary>
        /// <param name="session">The session for which completions are to be computed.</param>
        /// <param name="signatures">The set of the <see cref="ISignature"/>s to be added to the session.</param>
        /// <remarks>
        /// Each applicable <see cref="ISignatureHelpSource.AugmentSignatureHelpSession"/> instance will be called in-order to
        /// (re)calculate a <see cref="ISignatureHelpSession"/>.  <see cref="ISignature"/>s can be added to the session by adding
        /// them to the signatures collection passed-in as a parameter.  In addition, by removing items from the collection, a
        /// source may filter <see cref="ISignature"/>s provided by <see cref="ISignatureHelpSource"/>s earlier in the calculation
        /// chain.
        /// </remarks>
        void AugmentSignatureHelpSession(ISignatureHelpSession session, IList<ISignature> signatures);

        /// <summary>
        /// Computes the best matching <see cref="ISignature" /> instance for the given signature help session. Only the highest-
        /// priority signature help provider is asked for this information.
        /// </summary>
        /// <param name="session">
        /// The <see cref="ISignatureHelpSession" /> for which the best matching <see cref="ISignature" /> should be determined.
        /// </param>
        /// <returns>
        /// A valid <see cref="ISignature" /> that is also a member of the Signatures collection of the specified
        /// <see cref="ISignatureHelpSession" />. It can return null if no best match could be determined, 
        /// and the next highest-priority signature help provider will be asked to determine the best match.
        /// </returns>
        ISignature GetBestMatch(ISignatureHelpSession session);
    }
}
