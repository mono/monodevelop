// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Utilities
{
    using System.Windows;
    using System.Windows.Media;

    public class GeometryAdornment : UIElement
    {
        #region Private Attributes
        internal readonly DrawingVisual _child;
        internal readonly Brush _fillBrush;
        internal readonly Pen _borderPen;
        internal readonly Geometry _geometry;
        #endregion

        public GeometryAdornment(Pen borderPen, Brush fillBrush, double left, double top, double width, double height, bool isHitTestVisible = false, bool snapsToDevicePixels = false)
            : this(borderPen, fillBrush, new Rect(left, top, width, height), isHitTestVisible, snapsToDevicePixels)
        {
        }

        public GeometryAdornment(Pen borderPen, Brush fillBrush, Rect rect, bool isHitTestVisible = false, bool snapsToDevicePixels = false)
            : this(borderPen, fillBrush, new RectangleGeometry(rect), isHitTestVisible, snapsToDevicePixels)
        {
        }

        public GeometryAdornment(Pen borderPen, Brush fillBrush, Geometry geometry, bool isHitTestVisible = false, bool snapsToDevicePixels = false)
        {
            _borderPen = borderPen;
            _fillBrush = fillBrush;
            _geometry = geometry;

            this.IsHitTestVisible = isHitTestVisible;
            this.SnapsToDevicePixels = snapsToDevicePixels;

            _child = new DrawingVisual();
            DrawingContext context = _child.RenderOpen();
            context.DrawGeometry(fillBrush, borderPen, geometry);
            context.Close();

            this.AddVisualChild(_child);
        }

        #region Member Overrides
        protected override Visual GetVisualChild(int index)
        {
            return _child;
        }

        protected override int VisualChildrenCount
        {
            get
            {
                return 1;
            }
        }
        #endregion //Member Overrides
    }
}