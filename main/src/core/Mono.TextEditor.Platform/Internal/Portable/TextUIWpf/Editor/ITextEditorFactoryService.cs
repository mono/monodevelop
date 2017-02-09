// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.Text.Document;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Creates editor views.
    /// </summary>
    /// <remarks>This is a MEF component part, and should be imported as follows:
    /// [Import]
    /// ITextEditorFactoryService factory = null;
    /// </remarks>
    public interface ITextEditorFactoryService
    {
        /// <summary>
        /// Creates an <see cref="IWpfTextView"/> that displays the contents of <paramref name="viewModel"/>.
        /// </summary>
        /// <param name="viewModel">The <see cref="ITextViewModel"/> that provides the text buffers for the view.</param>
        /// <param name="roles">The set of roles filled by the view.</param>
        /// <param name="parentOptions">The options environment for the text view.</param>
        /// <returns>An <see cref="IWpfTextView"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="viewModel"/> or <paramref name="roles"/> or <paramref name="parentOptions"/> is null.</exception>
        IWpfTextView CreateTextView(ITextViewModel viewModel, ITextViewRoleSet roles, IEditorOptions parentOptions);

        /// <summary>
        /// Creates an <see cref="IWpfTextView"/> that displays the contents of <paramref name="dataModel"/>.
        /// </summary>
        /// <param name="dataModel">The <see cref="ITextDataModel"/> that provides the text buffers over which an <see cref="ITextViewModel"/>
        /// will be built for the view.</param>
        /// <param name="roles">The set of roles filled by the view.</param>
        /// <param name="parentOptions">The options environment for the text view.</param>
        /// <returns>An <see cref="IWpfTextView"/>.</returns>
        /// <remarks>
        /// An <see cref="ITextDataModel"/> can be displayed in multiple views. An <see cref="ITextViewModel"/> will be constructed based on 
        /// the <see cref="IContentType"/> of the <paramref name="dataModel"/> and the <paramref name="roles"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="dataModel"/> or <paramref name="roles"/> or <paramref name="parentOptions"/> is null.</exception>
        IWpfTextView CreateTextView(ITextDataModel dataModel, ITextViewRoleSet roles, IEditorOptions parentOptions);

        /// <summary>
        /// Creates an <see cref="IWpfTextView"/> that displays the contents of <paramref name="textBuffer"/>.
        /// </summary>
        /// <param name="textBuffer">The <see cref="ITextBuffer"/> that provides the text for the view.</param>
        /// <param name="roles">The set of roles filled by the view.</param>
        /// <param name="parentOptions">The options environment for the text view.</param>
        /// <returns>An <see cref="IWpfTextView"/>.</returns>
        /// <remarks>
        /// An <see cref="ITextBuffer"/> can be displayed in multiple views. A trivial <see cref="ITextDataModel"/> will be constructed and
        /// an <see cref="ITextViewModel"/> will be constructed based on 
        /// the <see cref="IContentType"/> of the <paramref name="textBuffer"/> and the <paramref name="roles"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="textBuffer"/> or <paramref name="roles"/> or <paramref name="parentOptions"/> is null.</exception>
        IWpfTextView CreateTextView(ITextBuffer textBuffer, ITextViewRoleSet roles, IEditorOptions parentOptions);

        /// <summary>
        /// Creates an <see cref="IWpfTextView"/> that displays the contents of <paramref name="textBuffer"/>.
        /// </summary>
        /// <param name="textBuffer">The <see cref="ITextBuffer"/> that provides the text for the view.</param>
        /// <param name="roles">The set of roles filled by the view.</param>
        /// <returns>An <see cref="IWpfTextView"/>.</returns>
        /// <remarks>
        /// An <see cref="ITextBuffer"/> can be displayed in multiple views. A trivial <see cref="ITextDataModel"/> will be constructed and
        /// an <see cref="ITextViewModel"/> will be constructed based on 
        /// the <see cref="IContentType"/> of the <paramref name="textBuffer"/> and the <paramref name="roles"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="textBuffer"/> or <paramref name="roles"/> is null.</exception>
        IWpfTextView CreateTextView(ITextBuffer textBuffer, ITextViewRoleSet roles);

        /// <summary>
        /// Creates an <see cref="IWpfTextView"/> that displays the contents of <paramref name="textBuffer"/>.
        /// </summary>
        /// <param name="textBuffer">The <see cref="ITextBuffer"/> that provides the text for the view.</param>
        /// <returns>An <see cref="IWpfTextView"/> having the default set of text view roles.</returns>
        /// <remarks>
        /// An <see cref="ITextBuffer"/> can be displayed in multiple views. A trivial <see cref="ITextDataModel"/> will be constructed and
        /// an <see cref="ITextViewModel"/> will be constructed based on 
        /// the <see cref="IContentType"/> of the <paramref name="textBuffer"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="textBuffer"/> is null.</exception>
        IWpfTextView CreateTextView(ITextBuffer textBuffer);

        /// <summary>
        /// Creates an <see cref="IWpfTextView"/> on a newly created <see cref="ITextBuffer"/> having
        /// content type <code>Text</code>.
        /// </summary>
        /// <returns>A <see cref="IWpfTextView"/>.</returns>
        IWpfTextView CreateTextView();
        
        /// <summary>
        /// Creates a host for the text view.
        /// </summary>
        /// <param name="wpfTextView">The text view to host.</param>
        /// <param name="setFocus"><c>true</c> if the <see cref="IWpfTextViewHost"/> should take focus after it is initialized, <c>false</c> otherwise.</param>
        /// <returns>An <see cref="IWpfTextViewHost"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="wpfTextView"/> is null.</exception>
        IWpfTextViewHost CreateTextViewHost(IWpfTextView wpfTextView, bool setFocus);

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
