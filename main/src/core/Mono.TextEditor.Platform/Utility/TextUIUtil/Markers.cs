// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Formatting;

    internal static class Markers
    {
        // pad the bottom part by 1 pixel to take advantage of the extra 1 pixel that's available in the default
        // line transform at the bottom of each line
        public readonly static Thickness SingleLinePadding = new Thickness(0.0, 0.0, 0.0, 1.0);
        public readonly static Thickness MultiLinePadding = new Thickness(0.0);

        public static bool MarkerGeometrySpansMultipleLines(ITextViewLineCollection collection, SnapshotSpan bufferSpan)
        {
            ITextViewLine start = collection.GetTextViewLineContainingBufferPosition(bufferSpan.Start);

            return (start == null || bufferSpan.End > start.EndIncludingLineBreak);
        }


        //use double.MinValue/double.MaxValue for leftClip & rightClip to avoid clipping.
        public static IList<Rect> GetRectanglesFromBounds(IList<TextBounds> bounds, Thickness padding, double leftClip, double rightClip, bool useTextBounds)
        {
            Debug.Assert(bounds != null);

            List<Rect> newBounds = new List<Rect>(bounds.Count);
            foreach (var b in bounds)
            {
                double x1 = Math.Max(leftClip, b.Left - padding.Left);
                double x2 = Math.Min(rightClip, b.Right + padding.Right);
                if (x1 < x2)
                {
                    double y1 = (useTextBounds ? b.TextTop : b.Top) - padding.Top;
                    double y2 = (useTextBounds ? b.TextBottom : b.Bottom) + padding.Bottom;

                    newBounds.Add(new Rect(x1, y1, x2 - x1, y2 - y1));
                }
            }

            return newBounds;
        }

        public static Geometry GetMarkerGeometryFromRectangles(IList<Rect> rectangles)
        {
            Debug.Assert(rectangles != null);

            if (rectangles.Count == 0)
                return null;

            // Set up the initial geometry
            PathGeometry geometry = new PathGeometry();
            geometry.FillRule = FillRule.Nonzero;

            foreach (var rectangle in rectangles)
            {
                geometry.AddGeometry(new RectangleGeometry(rectangle));
            }
            geometry.Freeze();

            if (rectangles.Count > 1)
            {
                geometry = geometry.GetOutlinedPathGeometry();
                geometry.Freeze();
            }
            return geometry;
        }
    }
}
