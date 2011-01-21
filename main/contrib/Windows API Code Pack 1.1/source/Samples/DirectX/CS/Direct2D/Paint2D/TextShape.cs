// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;
using Microsoft.WindowsAPICodePack.DirectX.DirectWrite;

namespace D2DPaint
{
    internal class TextShape : DrawingShape
    {
        internal float _maxX;
        internal float _maxY;
        internal Point2F _point0;
        internal TextLayout _textLayout;
        internal int _selectedBrushIndex;

        private bool _drawBorder = true;

        internal TextShape(Paint2DForm parent, TextLayout textLayout, Point2F startPoint, float maxX, float maxY, int selectedBrush)
            : base(parent)
        {
            _maxX = maxX;
            _maxY = maxY;
            _textLayout = textLayout;
            _selectedBrushIndex = selectedBrush;
            _point0 = startPoint;
        }

        protected internal override void Draw(RenderTarget renderTarget)
        {
            if (_drawBorder)
            {

                renderTarget.DrawRectangle(
                    new RectF(_point0.X, _point0.Y, _point0.X + _textLayout.MaxWidth, _point0.Y + _textLayout.MaxHeight), 
                    _parent.brushes[0],
                    1.5f, _parent.TextBoxStroke);
            }

            renderTarget.DrawTextLayout(
                _point0, _textLayout, _parent.brushes[_selectedBrushIndex], DrawTextOptions.Clip);
        }

        protected internal override void EndDraw()
        {
            _drawBorder = false;
        }

        protected internal override Point2F EndPoint
        {
            set
            {
                _textLayout.MaxWidth = Math.Max(5, value.X - _point0.X);
                _textLayout.MaxHeight = Math.Max(5, value.Y - _point0.Y);
            }
        }
    }
}