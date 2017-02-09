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
    /// Represents a line of formatted text in the <see cref="ITextView"/>.
    /// </summary>
    /// <remarks>
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
    public interface IFormattedLine : IWpfTextViewLine, IDisposable
    {
        /// <summary>
        /// Sets the <see cref="ITextSnapshot"/>s upon which this formatted text line is based.
        /// </summary>
        /// <param name="visualSnapshot">the new snapshot for the line in the view model's visual buffer.</param>
        /// <param name="editSnapshot">the new snapshot for the line in the view model's edit buffer.</param>
        /// <remarks>The length of this text line is not allowed to change as a result of changing the snapshot.</remarks>
        /// <exception cref="ObjectDisposedException">This <see cref="IWpfTextViewLine"/> has been disposed.</exception>
        void SetSnapshot(ITextSnapshot visualSnapshot, ITextSnapshot editSnapshot);

        /// <summary>
        /// Sets the line transform used to format the text in this formatted text line.
        /// </summary>
        /// <param name="transform">The line transform for this formatted text line.</param>
        /// <exception cref="ObjectDisposedException">This <see cref="IWpfTextViewLine"/> has been disposed.</exception>
        void SetLineTransform(LineTransform transform);

        /// <summary>
        /// Sets the position used to format the text in this formatted text line.
        /// </summary>
        /// <param name="top">The position for the top of the formatted text line.</param>
        /// <exception cref="ObjectDisposedException">This <see cref="IWpfTextViewLine"/> has been disposed.</exception>
        void SetTop(double top);

        /// <summary>
        /// Sets the change in the position of the top of this formatted text line in the current
        /// view layout and the previous view layour.
        /// </summary>
        /// <param name="deltaY">The new deltaY value for the formatted text line.</param>
        void SetDeltaY(double deltaY);

        /// <summary>
        /// Sets the Change property for this text line.
        /// </summary>
        /// <param name="change">The <see cref="TextViewLineChange"/>.</param>
        void SetChange(TextViewLineChange change);

        /// <summary>
        /// Sets the visible area in which this text line will be formatted.
        /// </summary>
        /// <param name="visibleArea">The bounds of the visible area on the drawing surface upon which this text line will be formatted.</param>
        /// <remarks>The VisibilityState of this text line is determined strictly by the top and bottom of <paramref name="visibleArea"/>.</remarks>
        /// <exception cref="ObjectDisposedException">This <see cref="IWpfTextViewLine"/> has been disposed.</exception>
        void SetVisibleArea(Rect visibleArea);

        /// <summary>
        /// Gets the WPF <see cref="Visual"/> that can be used to add this formatted text line to a <see cref="VisualCollection"/>.
        /// </summary>
        /// <exception cref="ObjectDisposedException">This <see cref="IWpfTextViewLine"/> has been disposed.</exception>
        Visual GetOrCreateVisual();

        /// <summary>
        /// Remove the Wpf <see cref="Visual"/> that represents the rendered text of the line.
        /// </summary>
        void RemoveVisual();
    }
}