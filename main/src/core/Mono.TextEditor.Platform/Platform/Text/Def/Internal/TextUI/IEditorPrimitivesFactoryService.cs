//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using System.ComponentModel.Composition;

    /// <summary>
    /// A service that provides primitives for a given <see cref="ITextView"/> or <see cref="ITextBuffer"/>.
    /// </summary>
    /// <remarks>This is a MEF component part, and should be imported as follows:
    /// [Import]
    /// IEditorPrimitivesFactoryService factory = null;
    /// </remarks>
    public interface IEditorPrimitivesFactoryService
    {
        /// <summary>
        /// Gets the <see cref="IViewPrimitives"/> for the <see cref="ITextView"/>.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> for which to get the <see cref="IViewPrimitives"/>.</param>
        /// <returns>The <see cref="IViewPrimitives"/> for the given <see cref="ITextView"/>.</returns>
        IViewPrimitives GetViewPrimitives(ITextView textView);

        /// <summary>
        /// Gets the <see cref="IBufferPrimitives"/> for the <see cref="ITextBuffer"/>.
        /// </summary>
        /// <param name="textBuffer">The <see cref="ITextBuffer"/> for which to fetch <see cref="IBufferPrimitives"/>.</param>
        /// <returns>The <see cref="IBufferPrimitives"/> for the given <see cref="ITextBuffer"/>.</returns>
        IBufferPrimitives GetBufferPrimitives(ITextBuffer textBuffer);
    }
}
