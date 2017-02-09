// Copyright (c) Microsoft Corporation
// All rights reserved

using System;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Defines a provider of a suggested actions source.
    /// </summary>
    /// <remarks>
    /// This is a MEF component part, and should be exported with the following attribute:
    /// [Export(typeof(ISuggestedActionsSourceProvider))]
    /// [Order]
    /// [Name]
    /// [ContentType]
    /// You must specify the ContentType so that the source provider creates sources for buffers of content types that it
    /// recognizes, and Order to specify the order in which the sources are called.
    /// </remarks>
    [CLSCompliant(false)]
    public interface ISuggestedActionsSourceProvider
    {
        /// <summary>
        /// Creates suggested actions source for a given text buffer.
        /// </summary>
        /// <param name="textView">The text view for which to create a suggested actions source.</param>
        /// <param name="textBuffer">The text buffer for which to create a suggested actions source.</param>
        /// <returns>The <see cref="ISuggestedActionsSource"/>, or null if no suggested actions source could be created.</returns>
        ISuggestedActionsSource CreateSuggestedActionsSource(ITextView textView, ITextBuffer textBuffer);
    }
}