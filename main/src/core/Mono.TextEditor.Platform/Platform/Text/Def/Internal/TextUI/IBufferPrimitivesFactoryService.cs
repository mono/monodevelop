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
    /// Creates buffer primitives.  
    /// </summary>
    /// <remarks>
    /// <para>
    /// This factory is designed to be used by other primitives. Consumers of the primitives 
    /// should use the <see cref="IEditorPrimitivesFactoryService"/> to get a reference to a <see cref="TextBuffer"/> 
    /// and use that to create the primitives.
    /// </para>
    /// </remarks>
    /// <remarks>This is a MEF component part, and should be imported as follows:
    /// [Import]
    /// IBufferPrimitivesFactoryService factory = null;
    /// </remarks>
    public interface IBufferPrimitivesFactoryService
    {
        /// <summary>
        /// Creates a <see cref="TextBuffer"/> primitive.
        /// </summary>
        /// <param name="textBuffer">The <see cref="ITextBuffer"/> on which to base this primitive.</param>
        /// <returns>The <see cref="TextBuffer"/> primitive for the given <see cref="ITextBuffer"/>.</returns>
        /// <remarks>
        /// <para>
        /// This method always returns the same object if the same <see cref="ITextBuffer"/> is passed in.
        /// </para>
        /// </remarks>
        TextBuffer CreateTextBuffer(ITextBuffer textBuffer);

        /// <summary>
        /// Creates a new <see cref="TextPoint"/> primitive.
        /// </summary>
        /// <param name="textBuffer">The <see cref="TextBuffer"/> to which this <see cref="TextPoint"/> belongs.</param>
        /// <param name="position">The position of this <see cref="TextPoint"/>.</param>
        /// <returns>A new <see cref="TextPoint"/> primitive at the given <paramref name="position"/>.</returns>
        TextPoint CreateTextPoint(TextBuffer textBuffer, int position);

        /// <summary>
        /// Creates a new <see cref="TextRange"/> primitive.
        /// </summary>
        /// <param name="textBuffer">The <see cref="TextBuffer"/> to which this <see cref="TextPoint"/> belongs.</param>
        /// <param name="startPoint">The <see cref="TextPoint"/> of the start.</param>
        /// <param name="endPoint">The <see cref="TextPoint"/> of the end.</param>
        /// <returns>A new <see cref="TextRange"/> primitive at the given <paramref name="startPoint"/> and <paramref name="endPoint"/>.</returns>
        TextRange CreateTextRange(TextBuffer textBuffer, TextPoint startPoint, TextPoint endPoint);
    }
}
