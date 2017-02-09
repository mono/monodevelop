////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Defines an extension used to create signature help providers from a given <see cref="ITextBuffer"/> opened in a given
    /// context.
    /// </summary>
    /// <remarks>
    /// This is a MEF component part, and should be exported with the following attribute:
    /// [Export(typeof(ISignaturehelpSourceProvider))]
    /// Component exporters must add at least one ContentType attribute to specify the
    /// content types for which the component is valid, and an Order to specify its order in the chain of signature help providers.
    /// </remarks>
    public interface ISignatureHelpSourceProvider
    {
        /// <summary>
        /// Attempts to create a signature help provider for the given text buffer opened in the given context.
        /// </summary>
        /// <param name="textBuffer">The text buffer for which to create a signature help provider.</param>
        /// <returns>A valid signature help provider, or null if none could be created.</returns>
        ISignatureHelpSource TryCreateSignatureHelpSource ( ITextBuffer textBuffer );
    }
}
