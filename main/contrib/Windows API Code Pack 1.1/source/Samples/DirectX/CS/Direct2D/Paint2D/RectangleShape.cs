// Copyright (c) Microsoft Corporation.  All rights reserved.

using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;

namespace D2DPaint
{
    internal class RectangleShape : DrawingShape
    {
        internal RectF _rect;
        internal float _strokeWidth;
        internal int _selectedBrushIndex;


        internal RectangleShape(Paint2DForm parent, RectF rect, float strokeWidth, int selectedBrush, bool fill)
            : base(parent, fill)
        {
            _rect = rect;
            _strokeWidth = strokeWidth;
            _selectedBrushIndex = selectedBrush;
        }

        protected internal override void Draw(RenderTarget renderTarget)
        {
            if (_fill)
            {
                renderTarget.FillRectangle(_rect, _parent.brushes[_selectedBrushIndex]);
            }
            else
            {
                renderTarget.DrawRectangle(_rect, _parent.brushes[_selectedBrushIndex], _strokeWidth);
            }
        }
        protected internal override Point2F EndPoint
        {
            set
            {
                _rect.Right = value.X;
                _rect.Bottom = value.Y;
            }
        }
    }
}