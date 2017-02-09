// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Defines <see cref="IPeekResult"/> presenter, which can create WPF visual representation of a Peek result.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each specific <see cref="IPeekResult"/> implementation needs an <see cref="IPeekResultPresenter"/> that 
    /// can create a visual representation of the result to be shown inside the Peek control. The Peek service exports 
    /// default <see cref="IPeekResultPresenter"/>s for known <see cref="IPeekResult"/> implementations such as 
    /// <see cref="IDocumentPeekResult"/> and <see cref="IExternallyBrowsablePeekResult"/>. When a content type specific 
    /// Peek provider provides a custom <see cref="IPeekResult"/> implementation it should also export an 
    /// <see cref="IPeekResultPresenter"/> that can create its visual representation. It is also possible to override
    /// existing <see cref="IPeekResultPresenter"/> using the Order attribute.
    /// </para>
    /// <para>
    /// This is a MEF component part, and should be exported with the following attributes:
    /// [Export(typeof(IPeekResultPresenter))]
    /// [Name("presenter name")]
    /// [Order()]
    /// Name attribute is required and Order attribute is optional.
    /// </para>
    /// </remarks>
    public interface IPeekResultPresenter
    {
        /// <summary>
        /// Creates <see cref="IPeekResultPresentation"/> instance for the given <see cref="IPeekResult"/>.
        /// </summary>
        /// <param name="result">The Peek result for which to create a visual representation.</param>
        /// <returns>A valid <see cref="IPeekResultPresentation"/> instance or null if none could be created by this presenter.</returns>
        IPeekResultPresentation TryCreatePeekResultPresentation(IPeekResult result);
    }
}
