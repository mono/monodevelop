// Copyright (c) Microsoft Corporation.  All rights reserved.

using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;

namespace D2DPaint
{
    internal class RoundRectangleShape : DrawingShape
    {
        internal RoundedRect _rect;
        internal float _strokeWidth;
        internal int _selectedBrushIndex;


        internal RoundRectangleShape(Paint2DForm parent, RoundedRect rect, float strokeWidth, int selectedBrush, bool fill)
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
                renderTarget.FillRoundedRectangle(_rect, _parent.brushes[_selectedBrushIndex]);
            }
            else
            {
                renderTarget.DrawRoundedRectangle(_rect, _parent.brushes[_selectedBrushIndex], _strokeWidth);
            }
        }

        protected internal override Point2F EndPoint
        {
            set
            {
                _rect.Rect = new RectF(_rect.Rect.Left, _rect.Rect.Top, value.X, value.Y);
            }
        }
    }
}