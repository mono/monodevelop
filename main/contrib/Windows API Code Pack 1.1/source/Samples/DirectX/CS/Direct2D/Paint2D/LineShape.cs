// Copyright (c) Microsoft Corporation.  All rights reserved.

using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;

namespace D2DPaint
{
    internal class LineShape : DrawingShape
    {        
        internal Point2F _point0, _point1;
        internal float _strokeWidth;
        internal int _selectedBrushIndex;

        internal LineShape(Paint2DForm parent, Point2F point0, Point2F point1, float strokeWidth, int selectedBrush) : base(parent)
        {
            _point0 = point0;
            _point1 = point1;
            _strokeWidth = strokeWidth;
            _selectedBrushIndex = selectedBrush;

        }

        protected internal override void Draw(RenderTarget renderTarget)
        {
            renderTarget.DrawLine(_point0, _point1, _parent.brushes[_selectedBrushIndex], _strokeWidth);
        }

        protected internal override Point2F EndPoint
        {
            set
            {
                _point1 = value;
            }
        }
    }
}