// Copyright (c) Microsoft Corporation
// All rights reserved

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Text;
using System;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents an <see cref="IPeekResult"/> that is based on a location in a document.
    /// </summary>
    /// <remarks>In a typical scenario Peek service creates <see cref="IDocumentPeekResult"/> instances 
    /// representing document based results supplied by Peek providers.
    /// </remarks>
    [CLSCompliant(false)]
    public interface IDocumentPeekResult : IPeekResult
    {
        /// <summary>
        /// The fully qualified file path identifying the document where the result is located.
        /// </summary>
        string FilePath { get; }

        /// <summary>
        /// Gets a <see cref="IPersistentSpan"/> corresponding to the result location span. For example if this result corresponds
        /// to a method, this span is the span of the method definition.
        /// </summary>
        IPersistentSpan Span { get; }

        /// <summary>
        /// Gets a <see cref="IPersistentSpan"/> corresponding to the span of the identifying
        /// token inside the result location span.
        /// For example if this result corresponds to a method, the identifying span is the 
        /// span of the method name token inside method definition span.
        /// </summary>
        IPersistentSpan IdentifyingSpan { get; }

        /// <summary>
        /// Gets whether this result is read-only.
        /// </summary>
        bool IsReadOnly { get; }

        /// <summary>
        /// Gets the display info that represents the <see cref="IDocumentPeekResult"/>, which is used to provide more indication
        /// on the symbol this <see cref="IDocumentPeekResult"/> represents.
        /// </summary>
        IPeekResultDisplayInfo2 DisplayInfo2 { get; }

        /// <summary>
        /// Gets an <see cref="ImageMoniker"/> representing an image equivalent for the 
        /// <see cref="IDocumentPeekResult"/>.
        /// </summary>
        ImageMoniker Image { get; }

        /// <summary>
        /// Gets the Guid for the desired editor to open when navigating.
        /// </summary>
        Guid DesiredEditorGuid { get; }
    }
}
