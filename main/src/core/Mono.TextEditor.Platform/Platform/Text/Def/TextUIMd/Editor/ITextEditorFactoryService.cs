// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.Text.Document;

    /// <summary>
    /// Creates editor views.
    /// </summary>
    /// <remarks>This is a MEF component part, and should be imported as follows:
    /// [Import]
    /// ITextEditorFactoryService factory = null;
    /// </remarks>
    internal interface ITextEditorFactoryService
    {
        IWpfTextView CreateTextView(ITextBuffer textBuffer, ITextViewRoleSet roles, IEditorOptions parentOptions);

        /// <summary>
        /// The empty ITextViewRoleSet.
        /// </summary>
        ITextViewRoleSet NoRoles { get; }

        /// <summary>
        /// The set of all predefined text view roles.
        /// </summary>
        ITextViewRoleSet AllPredefinedRoles { get; }

        /// <summary>
        /// The set of roles that are used when creating a text view without specifying text view roles.
        /// </summary>
        ITextViewRoleSet DefaultRoles { get; }

        /// <summary>
        /// Creates a <see cref="ITextViewRoleSet"/> containing the given roles.
        /// </summary>
        /// <param name="roles">The roles of interest.</param>
        /// <returns>The text view role set.</returns>
        /// <exception cref="ArgumentNullException"> roles is null.</exception>
        ITextViewRoleSet CreateTextViewRoleSet(IEnumerable<string> roles);

        /// <summary>
        /// Creates a <see cref="ITextViewRoleSet"/> containing the given roles.
        /// </summary>
        /// <param name="roles">The roles of interest.</param>
        /// <returns>The text view role set.</returns>
        ITextViewRoleSet CreateTextViewRoleSet(params string[] roles);

        /// <summary>
        /// Raised when any <see cref="ITextView"/> is created.
        /// </summary>
        event EventHandler<TextViewCreatedEventArgs> TextViewCreated;
    }
}
