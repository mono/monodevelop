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
    /// Creates view primitives.  
    /// </summary>
    /// <remarks>
    /// <para>
    /// This factory is designed to  be used by other primitives. Consumers of the primitives 
    /// should use the <see cref="IEditorPrimitivesFactoryService"/> to get a reference to a 
    /// <see cref="TextView"/> and use that to create the primitives.
    /// </para>
    /// </remarks>
    /// <remarks>This is a MEF component part, and should be imported as follows:
    /// [Import]
    /// IViewPrimitivesFactoryService factory = null;
    /// </remarks>
    public interface IViewPrimitivesFactoryService
    {
        /// <summary>
        /// Creates a <see cref="TextView"/> primitive.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> on which to base this primitive.</param>
        /// <returns>The <see cref="TextView"/> primitive for the given <see cref="ITextView"/>.</returns>
        /// <remarks>
        /// <para>
        /// This method always returns the same object if the same <see cref="ITextView"/> is passed in.
        /// </para>
        /// </remarks>
        TextView CreateTextView(ITextView textView);

        /// <summary>
        /// Creates a new <see cref="DisplayTextPoint"/> primitive.
        /// </summary>
        /// <param name="textView">The <see cref="TextView"/> to which this <see cref="DisplayTextPoint"/> belongs.</param>
        /// <param name="position">The position of this <see cref="DisplayTextPoint"/>.</param>
        /// <returns>A new <see cref="DisplayTextPoint"/> primitive at the given <paramref name="position"/>.</returns>
        DisplayTextPoint CreateDisplayTextPoint(TextView textView, int position);

        /// <summary>
        /// Creates a new <see cref="DisplayTextRange"/> primitive.
        /// </summary>
        /// <param name="textView">The <see cref="TextView"/> to which this <see cref="DisplayTextRange"/> belongs.</param>
        /// <param name="textRange">The <see cref="TextRange"/> in the <see cref="TextBuffer"/>.</param>
        /// <returns>A new <see cref="DisplayTextRange"/> primitive at the given <paramref name="textView"/> and <paramref name="textRange"/>.</returns>
        DisplayTextRange CreateDisplayTextRange(TextView textView, TextRange textRange);

        /// <summary>
        /// Creates a <see cref="Selection"/> primitive.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> on which to base this primitive.</param>
        /// <returns>The <see cref="Selection"/> primitive for the given <see cref="ITextView"/>.</returns>
        /// <remarks>
        /// <para>
        /// This method always returns the same object if the same <see cref="ITextView"/> is passed in.
        /// </para>
        /// </remarks>
        Selection CreateSelection(TextView textView);

        /// <summary>
        /// Creates a <see cref="Caret"/> primitive.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> on which to base this primitive.</param>
        /// <returns>The <see cref="Caret"/> primitive for the given <see cref="ITextView"/>.</returns>
        /// <remarks>
        /// <para>
        /// This method always returns the same object if the same <see cref="ITextView"/> is passed in.
        /// </para>
        /// </remarks>
        Caret CreateCaret(TextView textView);
    }
}
