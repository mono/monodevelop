////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Provides completion sources.  
    /// </summary>
    /// <remarks>
    /// This is a MEF component part, and should be exported with the following attribute:
    /// [Export(typeof(ICompletionSourceProvider))]
    /// You must provide the ContentType and Order
    /// attributes.
    /// </remarks>
    public interface ICompletionSourceProvider
    {
        /// <summary>
        /// Creates a completion provider for the given context.
        /// </summary>
        /// <param name="textBuffer">The text buffer over which to create a provider.</param>
        /// <returns>A valid <see cref="ICompletionSource"/> instance, or null if none could be created.</returns>
        ICompletionSource TryCreateCompletionSource ( ITextBuffer textBuffer );
    }
}
