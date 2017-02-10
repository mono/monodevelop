// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
#if TARGET_VS
    using System.Windows;
    using System.Windows.Media;
#endif
    using Microsoft.VisualStudio.Text.Formatting;

    /// <summary>
    /// Represents a Visual Studio <see cref="ITextView"/> for the WPF platform.
    /// </summary>
    public interface IWpfTextView : ITextView
    {
#if TARGET_VS
        /// <summary>
        /// Gets a named <see cref="IAdornmentLayer"/>.
        /// </summary>
        /// <param name="name">The name of the layer.</param>
        /// <returns>An instance of the layer in this view.</returns>
        /// <remarks>
        /// Layer names must be defined as <see cref="AdornmentLayerDefinition"/> component parts.
        /// The following layer names: "Text", "Caret", "Selection", and "ProvisionalHighlight" are reserved and cannot
        /// be requested using this method.
        /// </remarks>
        IAdornmentLayer GetAdornmentLayer(string name);

        /// <summary>
        /// Gets a named <see cref="ISpaceReservationManager"/>.
        /// </summary>
        /// <param name="name">The name of the manager.</param>
        /// <returns>An instance of the manager in this view. Not null.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="name"/> is not registered via an <see cref="SpaceReservationManagerDefinition"/>.</exception>
        /// <remarks>
        /// <para>Managers must be exported using <see cref="SpaceReservationManagerDefinition"/> component parts.</para>
        /// </remarks>
        ISpaceReservationManager GetSpaceReservationManager(string name);

        /// <summary>
        /// Gets the FrameworkElement that renders the view.
        /// </summary>
        FrameworkElement VisualElement
        {
            get;
        }

        /// <summary>
        /// Gets or sets the background for the visual element.
        /// </summary>
        Brush Background
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the text view lines as an <see cref="IWpfTextViewLineCollection"/>.
        /// </summary>
        /// <remarks>
        /// <para>This property will be null during the view's initialization.</para>
        /// <para>None of the lines in this collection have a <see cref="ITextViewLine.VisibilityState"/> of <see cref="VisibilityState.Unattached"/>.
        /// Often the first and last lines in this collection will have a <see cref="ITextViewLine.VisibilityState"/> of <see cref="VisibilityState.Hidden"/>.</para>
        /// </remarks>
        new IWpfTextViewLineCollection TextViewLines
        {
            get;
        }

        /// <summary>
        /// Occurs when the <see cref="Background"/> is set.
        /// </summary>
        event EventHandler<BackgroundBrushChangedEventArgs> BackgroundBrushChanged;

        /// <summary>
        /// Gets the <see cref="IWpfTextViewLine"/> that contains the specified text buffer position.
        /// </summary>
        /// <param name="bufferPosition">
        /// The text buffer position used to search for a text line.
        /// </param>
        /// <returns>
        /// The <see cref="IWpfTextViewLine"/> that contains the specified buffer position.
        /// </returns>
        /// <remarks>
        /// <para>This method returns an <see cref="IWpfTextViewLine"/> if it exists in the view.</para>
        /// <para>If the line does not exist in the cache of formatted lines, it will be formatted and added to the cache.</para>
        /// <para>The returned <see cref="IWpfTextViewLine"/> could be invalidated by either a layout by the view or by subsequent calls to this method.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bufferPosition"/> is not a valid buffer position.</exception>
        /// <exception cref="InvalidOperationException"> if the view has not completed initialization.</exception>
        new IWpfTextViewLine GetTextViewLineContainingBufferPosition(SnapshotPoint bufferPosition);

        /// <summary>
        /// Gets the text formatter used by the view.
        /// </summary>
        /// <remarks>
        /// <para>This property will be null during the view's initialization.</para>
        /// </remarks>
        IFormattedLineSource FormattedLineSource
        {
            get;
        }

        /// <summary>
        /// Gets the line transformer used by the view.
        /// </summary>
        ILineTransformSource LineTransformSource
        {
            get;
        }

        /// <summary>
        /// Gets or sets the Zoom level for the <see cref="IWpfTextView"/> between 20% to 400%
        /// </summary>
        double ZoomLevel
        {
            get;
            set;
        }

        /// <summary>
        /// Occurs when the <see cref="ZoomLevel"/> is set.
        /// </summary>
        event EventHandler<ZoomLevelChangedEventArgs> ZoomLevelChanged;
#endif
    }
}
