// Copyright (c) Microsoft Corporation.  All rights reserved.

using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;

namespace D2DPaint
{
    internal class EllipseShape : DrawingShape
    {
        internal Ellipse _ellipse;
        internal float _strokeWidth;
        internal int _selectedBrushIndex;
        Point2F _startPoint;


        internal EllipseShape(Paint2DForm parent, Ellipse ellipse, float strokeWidth, int selectedBrush, bool fill)
            : base(parent, fill)
        {
            _startPoint = ellipse.Point;
            _ellipse = ellipse;
            _strokeWidth = strokeWidth;
            _selectedBrushIndex = selectedBrush;
        }

        protected internal override void Draw(RenderTarget renderTarget)
        {
            if (_fill)
            {
                renderTarget.FillEllipse(_ellipse, _parent.brushes[_selectedBrushIndex]);
            }
            else
            {
                renderTarget.DrawEllipse(_ellipse, _parent.brushes[_selectedBrushIndex], _strokeWidth);
            }
        }
        protected internal override Point2F EndPoint
        {
            set
            {
                _ellipse.RadiusX = (value.X - _startPoint.X) / 2f;
                _ellipse.RadiusY = (value.Y - _startPoint.Y) / 2f;

                _ellipse.Point = new Point2F(
                    _startPoint.X + ((value.X - _startPoint.X) / 2f),
                    _startPoint.Y + ((value.Y - _startPoint.Y) / 2f));
            }
        }
    }
}
