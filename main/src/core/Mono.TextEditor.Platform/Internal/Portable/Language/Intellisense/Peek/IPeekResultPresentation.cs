// Copyright (c) Microsoft Corporation
// All rights reserved

using System;
using System.Windows;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Defines WPF visual representation of an <see cref="IPeekResult"/>.
    /// </summary>
    /// <remarks>
    /// A visual representation of an <see cref="IDocumentPeekResult"/> for example is
    /// a WPF control of the IWpfTextViewHost containing an <see cref="ITextView"/>
    /// with an open document.
    /// </remarks>
    public interface IPeekResultPresentation : IDisposable
    {
        /// <summary>
        /// Tries to open another <see cref="IPeekResult"/> while keeping the same presentation.
        /// For example document result presentation might check if <paramref name="otherResult"/>
        /// represents a result in the same document and would reuse already open document.
        /// </summary>
        /// <param name="otherResult">Another result to be opened.</param>
        ///<returns><c>true</c> if succeeded in opening <paramref name="otherResult"/>, <c>false</c> otherwise.</returns>
        bool TryOpen(IPeekResult otherResult);

        /// <summary>
        /// Prepare to close the presentation.
        /// </summary>
        /// <returns>Returns <c>true</c> if the presentation is allowed to close; <c>false</c> otherwise.</returns>
        /// <remarks>
        /// <para>
        /// This method is called with the presentation is explicitly being closed to give the user, if the presentation
        /// corresponds to a modified document, the opportunity to save the document if desired.
        /// </para>
        /// <para>
        /// If this method returns <c>true</c>, the caller must close the presentation (typically by dismissing the
        /// containing peek session).
        /// </para>
        /// </remarks>
        bool TryPrepareToClose();

        /// <summary>
        /// Creates WPF visual representation of the Peek result.
        /// </summary>
        /// <remarks>
        /// An <see cref="IPeekResultPresentation"/> for an <see cref="IDocumentPeekResult"/> would
        /// for example open document and return a WPF control of the IWpfTextViewHost.
        /// </remarks>
        /// <param name="session">The <see cref="IPeekSession"/> containing the Peek result.</param>
        /// <param name="scrollState">The state that defines the desired scroll state of the result. May be null (in which case the default scroll state is used).</param>
        /// <returns>A valid <see cref="UIElement"/> representing the Peek result.</returns>
        UIElement Create(IPeekSession session, IPeekResultScrollState scrollState);

        /// <summary>
        /// Scrolls open representation of the Peek result into view.
        /// </summary>
        /// <param name="scrollState">The state that defines the desired scroll state of the result. May be null (in which case the default scroll state is used).</param>
        void ScrollIntoView(IPeekResultScrollState scrollState);

        /// <summary>
        /// Captures any information about the result prior to navigating to another frame (by using the peek navigation history
        /// or doing a recursive peek).
        /// </summary>
        IPeekResultScrollState CaptureScrollState();

        /// <summary>
        /// Closes the represenation of the Peek result.
        /// </summary>
        /// <remarks>
        /// An <see cref="IPeekResultPresentation"/> for an <see cref="IDocumentPeekResult"/> would
        /// for example close the document in this method.
        /// </remarks>
        void Close();

        /// <summary>
        /// Raised when the content of the presentation needs to be recreated. 
        /// </summary>
        event EventHandler<RecreateContentEventArgs> RecreateContent;

        /// <summary>
        /// Sets keyboard focus to the open representation of the Peek result.
        /// </summary>
        void SetKeyboardFocus();

        /// <summary>
        /// The ZoomLevel factor associated with the presentation.
        /// </summary>
        /// <remarks>
        /// Represented as a percentage (100.0 == default).
        /// </remarks>
        double ZoomLevel { get; set; }

        /// <summary>
        /// Gets a value indicating whether or not this presentation is dirty.
        /// </summary>
        bool IsDirty { get; }

        /// <summary>
        /// Raised when <see cref="IsDirty"/> changes.
        /// </summary>
        event EventHandler IsDirtyChanged;

        /// <summary>
        /// Gets a value indicating whether or not this presentation is read-only.
        /// </summary>
        bool IsReadOnly { get; }

        /// <summary>
        /// Raised when <see cref="IsReadOnly"/> changes.
        /// </summary>
        event EventHandler IsReadOnlyChanged;

        /// <summary>
        /// Can this presentation be saved?
        /// </summary>
        /// <param name="defaultPath">Location the presentation will be saved to by default (will be null if returning false).</param>
        bool CanSave(out string defaultPath);

        /// <summary>
        /// Save the current version of this presentation.
        /// </summary>
        /// <param name="saveAs">If true, ask the user for a save location.</param>
        /// <returns>true if the save succeeded.</returns>
        bool TrySave(bool saveAs);
    }
}
