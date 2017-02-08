// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text
{
    using System;
    using System.IO;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// The factory service for ordinary TextBuffers.
    /// </summary>
    /// <remarks>This is a MEF Component, and should be imported as follows:
    /// [Import]
    /// ITextBufferFactoryService factory = null;
    /// </remarks>
    public interface ITextBufferFactoryService2 : ITextBufferFactoryService
    {
        /// <summary>
        /// Creates an <see cref="ITextBuffer"/> with the specified <see cref="IContentType"/> and populates it 
        /// with the given text contained in <paramref name="span"/>.
        /// </summary>
        /// <param name="span">The initial text to add.</param>
        /// <param name="contentType">The <see cref="IContentType"/> for the new <see cref="ITextBuffer"/>.</param>
        /// <returns>
        /// A <see cref="ITextBuffer"/> object with the given text and <see cref="IContentType"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">Either <paramref name="span"/> or <paramref name="contentType"/> is null.</exception>
        ITextBuffer CreateTextBuffer(SnapshotSpan span, IContentType contentType);

        /// <summary>
        /// Creates an <see cref="ITextBuffer"/> with the given <paramref name="contentType"/> and populates it by 
        /// reading data from the specified TextReader.
        /// </summary>
        /// <param name="reader">The TextReader from which to read.</param>
        /// <param name="contentType">The <paramref name="contentType"/> for the text contained in the new <see cref="ITextBuffer"/></param>
        /// <param name="length">The length of the file backing the text reader, if known; otherwise -1.</param>
        /// <param name="traceId">An optional identifier used in debug tracing.</param>
        /// <returns>
        /// An <see cref="ITextBuffer"/> object with the given TextReader and <paramref name="contentType"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="contentType"/> is null.</exception>
        /// <remarks>
        /// <para>The <paramref name="reader"/> is not closed by this operation.</para>
        /// <para>The <paramref name="length"/> is used to help select a storage strategy for the text buffer.</para>
        /// </remarks>
        ITextBuffer CreateTextBuffer(TextReader reader, IContentType contentType, long length, string traceId = "");
    }
}
