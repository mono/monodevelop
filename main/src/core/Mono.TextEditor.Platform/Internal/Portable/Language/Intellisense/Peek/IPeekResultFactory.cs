// Copyright (c) Microsoft Corporation
// All rights reserved

using System;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents a factory for creating <see cref="IPeekResult"/>s.
    /// </summary>
    /// <remarks>This is a MEF Component, and should be imported with the following attribute:
    /// [Import(typeof(IPeekResultFactory))]
    /// </remarks>
    [CLSCompliant(false)]
    public interface IPeekResultFactory
    {
        /// <summary>
        /// Creates an instance of <see cref="IPeekResult"/> that is based on a location in a document.
        /// </summary>
        /// <param name="displayInfo">Defines properties used for displaying this result to the user.</param>
        /// <param name="filePath">The fully qualified file path identifying the document where the result is located.</param>
        /// <param name="startLine">Line number of the result location's start position.</param>
        /// <param name="startIndex">Character index of the result location's start position.</param>
        /// <param name="endLine">Line number of the result location's end position.</param>
        /// <param name="endIndex">Character index of the result location's end position.</param>
        /// <param name="idLine">Line number of the result's identifying position (e.g a position of method's name token).</param>
        /// <param name="idIndex">Character index of the result's identifying position (e.g a position of method's name token).</param>
        /// <returns>A valid instance of the <see cref="IDocumentPeekResult"/>.</returns>
        IDocumentPeekResult Create(IPeekResultDisplayInfo displayInfo, string filePath,
                                   int startLine, int startIndex, int endLine, int endIndex,
                                   int idLine, int idIndex);

        /// <summary>
        /// Creates an instance of <see cref="IPeekResult"/> that is based on a location in a document.
        /// </summary>
        /// <param name="displayInfo">Defines properties used for displaying this result to the user.</param>
        /// <param name="filePath">The fully qualified file path identifying the document where the result is located.</param>
        /// <param name="startLine">Line number of the result location's start position.</param>
        /// <param name="startIndex">Character index of the result location's start position.</param>
        /// <param name="endLine">Line number of the result location's end position.</param>
        /// <param name="endIndex">Character index of the result location's end position.</param>
        /// <param name="idLine">Line number of the result's identifying position (e.g a position of method's name token).</param>
        /// <param name="idIndex">Character index of the result's identifying position (e.g a position of method's name token).</param>
        /// <param name="isReadOnly">Defines whether this result is read-only.</param>
        /// <returns>A valid instance of the <see cref="IDocumentPeekResult"/>.</returns>
        IDocumentPeekResult Create(IPeekResultDisplayInfo displayInfo, string filePath,
                                   int startLine, int startIndex, int endLine, int endIndex,
                                   int idLine, int idIndex,
                                   bool isReadOnly);

        /// <summary>
        /// Creates an instance of <see cref="IPeekResult"/> that is based on a location in a document.
        /// </summary>
        /// <param name="displayInfo">Defines properties used for displaying this result to the user.</param>
        /// <param name="image">An image representing the <see cref="IDocumentPeekResult"/>.</param>
        /// <param name="filePath">The fully qualified file path identifying the document where the result is located.</param>
        /// <param name="startLine">Line number of the result location's start position.</param>
        /// <param name="startIndex">Character index of the result location's start position.</param>
        /// <param name="endLine">Line number of the result location's end position.</param>
        /// <param name="endIndex">Character index of the result location's end position.</param>
        /// <param name="idStartLine">Line number of the result's identifying span's start position (e.g a position of method's name token).</param>
        /// <param name="idStartIndex">Character index of the result's identifying span's start position (e.g a position of method's name token).</param>
        /// <param name="idEndLine">Line number of the result's identifying span's end position.</param>
        /// <param name="idEndIndex">Character index of the result's identifying span's end position.</param>
        /// <returns>A valid instance of the <see cref="IDocumentPeekResult"/>.</returns>
        IDocumentPeekResult Create(IPeekResultDisplayInfo2 displayInfo, ImageMoniker image, string filePath,
                                   int startLine, int startIndex, int endLine, int endIndex,
                                   int idStartLine, int idStartIndex, int idEndLine, int idEndIndex);

        /// <summary>
        /// Creates an instance of <see cref="IPeekResult"/> that is based on a location in a document.
        /// </summary>
        /// <param name="displayInfo">Defines properties used for displaying this result to the user.</param>
        /// <param name="image">An image representing the <see cref="IDocumentPeekResult"/>.</param>
        /// <param name="filePath">The fully qualified file path identifying the document where the result is located.</param>
        /// <param name="startLine">Line number of the result location's start position.</param>
        /// <param name="startIndex">Character index of the result location's start position.</param>
        /// <param name="endLine">Line number of the result location's end position.</param>
        /// <param name="endIndex">Character index of the result location's end position.</param>
        /// <param name="idStartLine">Line number of the result's identifying span's start position (e.g a position of method's name token).</param>
        /// <param name="idStartIndex">Character index of the result's identifying span's start position (e.g a position of method's name token).</param>
        /// <param name="idEndLine">Line number of the result's identifying span's end position.</param>
        /// <param name="idEndIndex">Character index of the result's identifying span's end position.</param>
        /// <param name="isReadOnly">Defines whether this result is read-only.</param>
        /// <returns>A valid instance of the <see cref="IDocumentPeekResult"/>.</returns>
        IDocumentPeekResult Create(IPeekResultDisplayInfo2 displayInfo, ImageMoniker image, string filePath,
                                   int startLine, int startIndex, int endLine, int endIndex,
                                   int idStartLine, int idStartIndex, int idEndLine, int idEndIndex,
                                   bool isReadOnly);

        /// <summary>
        /// Creates an instance of <see cref="IPeekResult"/> that is based on a location in a document.
        /// </summary>
        /// <param name="displayInfo">Defines properties used for displaying this result to the user.</param>
        /// <param name="image">An image representing the <see cref="IDocumentPeekResult"/>.</param>
        /// <param name="filePath">The fully qualified file path identifying the document where the result is located.</param>
        /// <param name="startLine">Line number of the result location's start position.</param>
        /// <param name="startIndex">Character index of the result location's start position.</param>
        /// <param name="endLine">Line number of the result location's end position.</param>
        /// <param name="endIndex">Character index of the result location's end position.</param>
        /// <param name="idStartLine">Line number of the result's identifying span's start position (e.g a position of method's name token).</param>
        /// <param name="idStartIndex">Character index of the result's identifying span's start position (e.g a position of method's name token).</param>
        /// <param name="idEndLine">Line number of the result's identifying span's end position.</param>
        /// <param name="idEndIndex">Character index of the result's identifying span's end position.</param>
        /// <param name="isReadOnly">Defines whether this result is read-only.</param>
        /// <param name="editorDestination">A Guid representing the editor the <see cref="IDocumentPeekResult"/> should navigate to.</param>
        /// <returns>A valid instance of the <see cref="IDocumentPeekResult"/>.</returns>
        IDocumentPeekResult Create(IPeekResultDisplayInfo2 displayInfo, ImageMoniker image, string filePath,
                                   int startLine, int startIndex, int endLine, int endIndex,
                                   int idStartLine, int idStartIndex, int idEndLine, int idEndIndex,
                                   bool isReadOnly, Guid editorDestination);

        /// <summary>
        /// Creates an instance of <see cref="IPeekResult"/> that is based on a location in a document.
        /// </summary>
        /// <param name="displayInfo">Defines properties used for displaying this result to the user.</param>
        /// <param name="image">An image representing the <see cref="IDocumentPeekResult"/>.</param>
        /// <param name="filePath">The fully qualified file path identifying the document where the result is located.</param>
        /// <param name="startLine">Line number of the result location's start position.</param>
        /// <param name="startIndex">Character index of the result location's start position.</param>
        /// <param name="endLine">Line number of the result location's end position.</param>
        /// <param name="endIndex">Character index of the result location's end position.</param>
        /// <param name="idStartLine">Line number of the result's identifying span's start position (e.g a position of method's name token).</param>
        /// <param name="idStartIndex">Character index of the result's identifying span's start position (e.g a position of method's name token).</param>
        /// <param name="idEndLine">Line number of the result's identifying span's end position.</param>
        /// <param name="idEndIndex">Character index of the result's identifying span's end position.</param>
        /// <param name="isReadOnly">Defines whether this result is read-only.</param>
        /// <param name="editorDestination">A Guid representing the editor the <see cref="IDocumentPeekResult"/> should navigate to.</param>
        /// <param name="postNavigationCallback">Pass in a callback function to the <see cref="IPeekResult"/>.<seealso cref="IPeekResult.PostNavigationCallback"/></param>
        /// <returns>A valid instance of the <see cref="IDocumentPeekResult"/>.</returns>
        IDocumentPeekResult Create(IPeekResultDisplayInfo2 displayInfo, ImageMoniker image, string filePath,
                                   int startLine, int startIndex, int endLine, int endIndex,
                                   int idStartLine, int idStartIndex, int idEndLine, int idEndIndex,
                                   bool isReadOnly, Guid editorDestination, Action<IPeekResult, object, object> postNavigationCallback);

        /// <summary>
        /// Creates an instance of <see cref="IPeekResult"/> that is based on a location in a document.
        /// </summary>
        /// <param name="displayInfo">Defines properties used for displaying this result to the user.</param>
        /// <param name="filePath">The fully qualified file path identifying the document where the result is located.</param>
        /// <param name="eoiSpan">Span of the entity of interest as a character offset from the start of the buffer.</param>
        /// <param name="idPosition">Position of the identifying position as a character offset from the start of the buffer.</param>
        /// <param name="isReadOnly">Defines whether this result is read-only.</param>
        /// <returns>A valid instance of the <see cref="IDocumentPeekResult"/>.</returns>
        IDocumentPeekResult Create(IPeekResultDisplayInfo displayInfo, string filePath,
                                   Span eoiSpan,
                                   int idPosition,
                                   bool isReadOnly);

        /// <summary>
        /// Creates an instance of <see cref="IPeekResult"/> that is not based on a location in a document, but can
        /// be browsed externally, for example a metadata class that can only be browsed in Object Browser.
        /// </summary>
        /// <param name="displayInfo">Defines properties used for displaying this result to the user.</param>
        /// <param name="browseAction">An action to browse the result externally (outside of Peek).</param>
        /// <returns>A valid instance of the <see cref="IExternallyBrowsablePeekResult"/>.</returns>
        IExternallyBrowsablePeekResult Create(IPeekResultDisplayInfo displayInfo, Action browseAction);
    }
}
