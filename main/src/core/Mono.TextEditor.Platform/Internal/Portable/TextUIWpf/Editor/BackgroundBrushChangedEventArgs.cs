// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
    using System.Windows.Media;

    /// <summary>
    /// Provides information for a BackgroundBrushChanged event in the <see cref="IWpfTextView"/>.
    /// </summary>
    public class BackgroundBrushChangedEventArgs : EventArgs
    {
        #region Private Members
        Brush _newBackgroundBrush;
        #endregion

        /// <summary>
        /// Initializes a new instance of a <see cref="BackgroundBrushChangedEventArgs"/>.
        /// </summary>
        /// <param name="newBackgroundBrush">The new <see cref="Brush"/> for an <see cref="IWpfTextView"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="newBackgroundBrush"/> is null.</exception>
        public BackgroundBrushChangedEventArgs(Brush newBackgroundBrush)
        {
            if (newBackgroundBrush == null)
                throw new ArgumentNullException("newBackgroundBrush");

            _newBackgroundBrush = newBackgroundBrush;
        }

        /// <summary>
        /// Gets the new <see cref="Brush"/> for an <see cref="IWpfTextView"/>.
        /// </summary>
        public Brush NewBackgroundBrush
        {
            get
            {
                return _newBackgroundBrush;
            }
        }
    }
}
