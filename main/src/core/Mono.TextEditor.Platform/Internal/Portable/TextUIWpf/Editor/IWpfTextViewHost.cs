// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Contains an <see cref="IWpfTextView"/> and the margins that surround it,
    /// such as a scrollbar or line number gutter.
    /// </summary>
    public interface IWpfTextViewHost
    {
        /// <summary>
        /// Closes the text view host and its underlying text view.
        /// </summary>
        /// <exception cref="InvalidOperationException">The text view host is already closed.</exception>
        void Close();

        /// <summary>
        /// Determines whether this text view has been closed.
        /// </summary>
        bool IsClosed { get; }

        /// <summary>
        /// Occurs immediately after closing the text view.
        /// </summary>
        event EventHandler Closed;

        /// <summary>
        /// Gets the <see cref="IWpfTextViewMargin"/> with the given <paramref name="marginName"/> that is attached to an edge of this <see cref="IWpfTextView"/>.
        /// </summary>
        /// <param name="marginName">The name of the <see cref="ITextViewMargin"/>.</param>
        /// <returns>The <see cref="ITextViewMargin"/> with a name that matches <paramref name="marginName"/>.</returns>
        /// <remarks>Callers of this method should only utilize the method after the <see cref="FrameworkElement.Loaded"/> event is raised.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="marginName"/> is null.</exception>
        IWpfTextViewMargin GetTextViewMargin(string marginName);

        /// <summary>
        /// Gets the <see cref="IWpfTextView"/> that is contained within this <see cref="IWpfTextViewHost"/>.
        /// </summary>
        IWpfTextView TextView
        {
            get;
        }

        /// <summary>
        /// Gets the WPF control for this <see cref="IWpfTextViewHost"/>.
        /// </summary>
        /// <remarks> Use this property to display the <see cref="IWpfTextViewHost"/> WPF control.</remarks>
        Control HostControl
        {
            get;
        }
    }
}