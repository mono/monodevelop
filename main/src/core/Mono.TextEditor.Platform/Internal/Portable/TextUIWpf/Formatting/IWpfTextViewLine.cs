// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Formatting
{
    using System;
    using System.Collections.ObjectModel;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.TextFormatting;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// Represents a line of rendered text in the <see cref="ITextView"/>.
    /// </summary>
    /// <remarks>
    /// <para>Only those <see cref="ITextSnapshotLine"/> objects of which parts are visible in the viewport will be formatted.</para>
    /// <para>Most properties and parameters that are doubles correspond to coordinates or distances in the text
    /// rendering coordinate system. In this coordinate system, x = 0.0 corresponds to the left edge of the drawing
    /// surface onto which text is rendered (x = view.ViewportLeft corresponds to the left edge of the viewport), and y = view.ViewportTop corresponds to the top edge of the viewport. The x-coordinate increases
    /// from left to right, and the y-coordinate increases from top to bottom. </para>
    /// <para>The horizontal and vertical axes of the view behave differently. When the text in the view is
    /// formatted, only the visible lines are formatted. As a result,
    /// a viewport cannot be scrolled horizontally and vertically in the same way.</para>
    /// <para>A viewport is scrolled horizontally by changing the left coordinate of the
    /// viewport so that it moves with respect to the drawing surface.</para>
    /// <para>A view can be scrolled vertically only by performing a new layout.</para>
    /// <para>Doing a layout in the view may cause the ViewportTop property of the view to change. For example, scrolling down one line will not translate any of the visible lines.
    /// Instead it will simply change the view's ViewportTop property (causing the lines to move on the screen even though their y-coordinates have not changed).</para>
    /// <para>Distances in the text rendering coordinate system correspond to logical pixels. If the text rendering
    /// surface is displayed without any scaling transform, then 1 unit in the text rendering coordinate system
    /// corresponds to one pixel on the display.</para>
    /// </remarks>
    public interface IWpfTextViewLine : ITextViewLine
    {

        /// <summary>
        /// Gets the visible area in which this text line will be rendered.
        /// </summary>
        /// <exception cref="ObjectDisposedException">this <see cref="IWpfTextViewLine"/> has been disposed.</exception>
        Rect VisibleArea { get; }

        /// <summary>
        /// Gets the formatting for a particular character in the line.
        /// </summary>
        /// <param name="bufferPosition">The buffer position of the desired character.</param>
        /// <returns>The <see cref="TextRunProperties"/> used to format that character.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bufferPosition"/> does not correspond to a position on this line.</exception>
        /// <exception cref="ObjectDisposedException">this <see cref="IWpfTextViewLine"/> has been disposed.</exception>
        TextRunProperties GetCharacterFormatting(SnapshotPoint bufferPosition);

        /// <summary>
        /// Gets a list of WPF text lines that make up the formatted text line.
        /// </summary>
        /// <exception cref="ObjectDisposedException">this <see cref="IWpfTextViewLine"/> has been disposed of.</exception>
        ReadOnlyCollection<TextLine> TextLines { get; }
    }
}
