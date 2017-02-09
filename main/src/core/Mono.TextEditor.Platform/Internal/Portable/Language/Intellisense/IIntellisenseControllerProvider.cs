////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Creates IntelliSense controllers for individual <see cref="ITextView"/> instances.
    /// </summary>
    public interface IIntellisenseControllerProvider
    {
        /// <summary>
        /// Attempts to create an IntelliSense controller for a specific text view opened in a specific context.
        /// </summary>
        /// <param name="textView">The text view for which a controller should be created.</param>
        /// <param name="subjectBuffers">The set of text buffers with matching content types that are potentially visible in the view.</param>
        /// <returns>A valid IntelliSense controller, or null if none could be created.</returns>
        IIntellisenseController TryCreateIntellisenseController ( ITextView textView, IList<ITextBuffer> subjectBuffers );
    }
}
