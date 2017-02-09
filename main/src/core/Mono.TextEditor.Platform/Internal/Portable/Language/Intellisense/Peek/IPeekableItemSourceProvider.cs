// Copyright (c) Microsoft Corporation
// All rights reserved

using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Provides <see cref="IPeekableItem"/> source for a text buffer.
    /// </summary>
    /// <remarks>
    /// Peek providers implement and export <see cref="IPeekableItemSourceProvider"/> for the
    /// content type they are interested to provide <see cref="IPeekableItem"/>s for.
    /// This is a MEF component, and should be exported with the following attribute:
    /// [Export(typeof(IPeekableItemSourceProvider))]
    /// You must provide the ContentType and Name attributes. The Order, SupportsStandaloneFiles and SupportsPeekRelationshipAttribute 
    /// attributes are optional.
    /// The default value of the SupportsStandaloneFiles attribute is false so if not specified the provider will be considered
    /// not supporting standalone files.
    /// </remarks>
    public interface IPeekableItemSourceProvider
    {
        /// <summary>
        /// Creates a <see cref="IPeekableItem"/> provider for the given text buffer.
        /// </summary>
        /// <param name="textBuffer">The text buffer to create a provider for.</param>
        /// <returns>A valid <see cref="IPeekableItemSource"/> instance, or null if none could be created.</returns>
        IPeekableItemSource TryCreatePeekableItemSource(ITextBuffer textBuffer);
    }
}
