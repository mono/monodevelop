////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Provides access to all the IntelliSense session stacks created for all the different
    /// <see cref="ITextView"/> instances in the application.
    /// </summary>
    public interface IIntellisenseSessionStackMapService
    {
        /// <summary>
        /// Gets an <see cref="IIntellisenseSessionStack"/> for a specific <see cref="ITextView"/> instance.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/>.</param>
        /// <returns>The <see cref="IIntellisenseSessionStack"/>.</returns>
        IIntellisenseSessionStack GetStackForTextView ( ITextView textView );
    }
}
