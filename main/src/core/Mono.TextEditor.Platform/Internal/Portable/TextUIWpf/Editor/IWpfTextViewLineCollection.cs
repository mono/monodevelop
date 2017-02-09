// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
    using System.Collections.ObjectModel;
    using System.Windows.Media;
    using System.Windows;

    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Formatting;

    /// <summary>
    /// <para>Allows the <see cref="ITextView"/> to access the view's collection of <see cref="ITextViewLine"/> objects. The
    /// TextViewLines property on the <see cref="ITextView"/> is used to get an instance of the
    /// ITextViewLineCollection interface.</para>
    /// </summary>
    /// <remarks>
    /// <para>The <see cref="ITextView"/> disposes its ITextViewLineCollection 
    /// and all the ITextViewLines it contains every time it generates a new layout.</para>
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
    public interface IWpfTextViewLineCollection : ITextViewLineCollection
    {
        /// <summary>
        /// Gets a collection of <see cref="IWpfTextViewLine"/> objects.
        /// </summary>
        ReadOnlyCollection<IWpfTextViewLine> WpfTextViewLines { get; }

        /// <summary>
        /// Gets the text marker geometry for the specified range of text in the buffer by using a polygonal approximation algorithm to calculate
        /// the outline path of the text regions.
        /// </summary>
        /// <param name="bufferSpan">
        /// The span of text.
        /// </param>
        /// <returns>
        /// A <see cref="Geometry"/> that contains the bounds of all of the formatted text in the span. It is null if the
        /// span is empty or does not intersect the text formatted in the <see cref="ITextView"/>.
        /// </returns>
        /// <remarks>
        /// <para>The returned geometry may contain several disjoint regions if the span
        /// contains a mix of conventional and bi-directional text.</para>
        /// <para>This method uses the height of the rendered text glyphs (<see cref="ITextViewLine.TextHeight"/>) to calculate the height of the geometry on each line.</para>
        /// <para>This method adds a 1-pixel padding to bottom of the geometries.</para>
        /// <para>The returned geometry is not clipped to the boundaries of the viewport.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bufferSpan"/> is not a valid 
        /// <see cref="SnapshotSpan"/> on the buffer.</exception>
        Geometry GetTextMarkerGeometry(SnapshotSpan bufferSpan);

        /// <summary>
        /// Gets the text marker geometry for the specified range of text in the buffer by using a polygonal approximation algorithm to calculate
        /// the outline path of the text regions.
        /// </summary>
        /// <param name="bufferSpan">
        /// The span of text.
        /// </param>
        /// <param name="clipToViewport">
        /// If true, the created geometry will be clipped to the viewport.
        /// </param>
        /// <param name="padding">
        /// A padding that's applied to the elements on a per line basis.
        /// </param>
        /// <returns>
        /// A <see cref="Geometry"/> that contains the bounds of all of the formatted text in the span. It is null if the
        /// span is empty or does not intersect the text formatted in the <see cref="ITextView"/>.
        /// </returns>
        /// <remarks>
        /// <para>The returned geometry may contain several disjoint regions if the span
        /// contains a mix of conventional and bi-directional text.</para>
        /// <para>This method uses the height of the rendered text glyphs (<see cref="ITextViewLine.TextHeight"/>) to calculate the height of the geometry on each line.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bufferSpan"/> is not a valid 
        /// <see cref="SnapshotSpan"/> on the buffer.</exception>
        Geometry GetTextMarkerGeometry(SnapshotSpan bufferSpan, bool clipToViewport, Thickness padding);

        /// <summary>
        /// Gets the text marker geometry for the specified range of text in the buffer by using a polygonal approximation algorithm to calculate
        /// the outline path of the text regions.
        /// </summary>
        /// <param name="bufferSpan">
        /// The span of text.
        /// </param>
        /// <returns>
        /// A <see cref="Geometry"/> that contains the bounds of all of the formatted text in the span. It is null if the
        /// span is empty or does not intersect the text formatted in the <see cref="ITextView"/>.
        /// </returns>
        /// <remarks>
        /// <para>The returned geometry may contain several disjoint regions if the span
        /// contains a mix of conventional and bi-directional text.</para>
        /// <para>This method uses the height of the rendered line (<see cref="ITextViewLine.Height"/>) to calculate the height of the geometry on each line.</para>
        /// <para>The returned geometry is not clipped to the boundaries of the viewport.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bufferSpan"/> is not a valid 
        /// <see cref="SnapshotSpan"/> on the buffer.</exception>
        Geometry GetLineMarkerGeometry(SnapshotSpan bufferSpan);

        /// <summary>
        /// Gets the text marker geometry for the specified range of text in the buffer by using a polygonal approximation algorithm to calculate
        /// the outline path of the text regions.
        /// </summary>
        /// <param name="bufferSpan">
        /// The span of text.
        /// </param>
        /// <param name="padding">
        /// A padding that's applied to the elements on a per line basis.
        /// </param>
        /// <param name="clipToViewport">
        /// If true, the created geometry will be clipped to the viewport.
        /// </param>
        /// <returns>
        /// A <see cref="Geometry"/> that contains the bounds of all of the formatted text in the span. It is null if the
        /// span is empty or does not intersect the text formatted in the <see cref="ITextView"/>.
        /// </returns>
        /// <remarks>
        /// <para>The returned geometry may contain several disjoint regions if the span
        /// contains a mix of conventional and bi-directional text.</para>
        /// <para>This method uses the height of the rendered line (<see cref="ITextViewLine.Height"/>) to calculate the height of the geometry on each line.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bufferSpan"/> is not a valid 
        /// <see cref="SnapshotSpan"/> on the buffer.</exception>
        Geometry GetLineMarkerGeometry(SnapshotSpan bufferSpan, bool clipToViewport, Thickness padding);

        /// <summary>
        /// Creates a marker geometry for the provided <paramref name="bufferSpan"/>. If the provided <paramref name="bufferSpan"/>
        /// extends beyond one line, then <see cref="GetLineMarkerGeometry(SnapshotSpan,bool,Thickness)"/> is used to calculate the marker geometry, otherwise
        /// this method uses <see cref="GetTextMarkerGeometry(SnapshotSpan,bool,Thickness)"/> to construct the geometry.
        /// </summary>
        /// <param name="bufferSpan">
        /// The span of text.
        /// </param>
        /// <param name="padding">
        /// A padding that's applied to the elements on a per line basis.
        /// </param>
        /// <param name="clipToViewport">
        /// If true, the created geometry will be clipped to the viewport.
        /// </param>
        /// <returns>
        /// A <see cref="Geometry"/> that contains the bounds of all of the formatted text in <paramref name="bufferSpan"/>.
        /// </returns>
        Geometry GetMarkerGeometry(SnapshotSpan bufferSpan, bool clipToViewport, Thickness padding);

        /// <summary>
        /// Creates a marker geometry for the provided <paramref name="bufferSpan"/>. If the provided <paramref name="bufferSpan"/>
        /// extends beyond one line, then <see cref="GetLineMarkerGeometry(SnapshotSpan)"/> is used to calculate the marker geometry, otherwise
        /// this method uses <see cref="GetTextMarkerGeometry(SnapshotSpan)"/> to construct the geometry.
        /// </summary>
        /// <returns>
        /// A <see cref="Geometry"/> that contains the bounds of all of the formatted text in <paramref name="bufferSpan"/>.
        /// </returns>
        Geometry GetMarkerGeometry(SnapshotSpan bufferSpan);

        /// <summary>
        /// Gets the <see cref="IWpfTextViewLine"/> that contains the specified text buffer position.
        /// </summary>
        /// <param name="bufferPosition">
        /// The text buffer position used to search for a text line.
        /// </param>
        /// <returns>
        /// An <see cref="IWpfTextViewLine"/> that contains the position, or null if none exist.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bufferPosition"/> is not a valid buffer position.</exception>
        new IWpfTextViewLine GetTextViewLineContainingBufferPosition(SnapshotPoint bufferPosition);

        /// <summary>
        /// Get the <see cref="IWpfTextViewLine"/> at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The zero based index of the item</param>
        /// <returns>
        /// Returns the <see cref="IWpfTextViewLine"/> at the <paramref name="index"/>th position.
        /// </returns>
        new IWpfTextViewLine this[int index] { get; }

        /// <summary>
        /// Gets the first line that is not completely hidden.
        /// </summary>
        new IWpfTextViewLine FirstVisibleLine
        {
            get;
        }

        /// <summary>
        /// Gets the last line that is not completely hidden.
        /// </summary>
        new IWpfTextViewLine LastVisibleLine
        {
            get;
        }

    }
}
