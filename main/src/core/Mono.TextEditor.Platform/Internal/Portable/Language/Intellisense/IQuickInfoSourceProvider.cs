////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Describes a factory of Quick Info providers.  
    /// </summary>
    /// <remarks>
    /// This is a MEF component part, and should be exported with the following attribute:
    /// [Export(typeof(IQuickInfoSourceProvider))]
    /// </remarks>
    public interface IQuickInfoSourceProvider
    {
        /// <summary>
        /// Creates a Quick Info provider for the specified context.
        /// </summary>
        /// <param name="textBuffer">The text buffer for which to create a provider.</param>
        /// <returns>A valid <see cref="IQuickInfoSource" /> instance, or null if none could be created.</returns>
        IQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer);
    }
}
