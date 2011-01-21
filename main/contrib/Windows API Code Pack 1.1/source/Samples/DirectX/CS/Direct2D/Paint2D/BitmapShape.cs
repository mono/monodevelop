// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;

namespace D2DPaint
{
    internal class BitmapShape : DrawingShape
    {
        internal RectF _rect;
        internal D2DBitmap _bitmap;
        internal float _transparency;

        internal BitmapShape(Paint2DForm parent, RectF rect, D2DBitmap bitmap, float transparency)
            : base(parent)
        {
            _rect = rect;
            _bitmap = bitmap;
            _transparency = transparency;
        }

        protected internal override void Draw(RenderTarget renderTarget)
        {
            renderTarget.DrawBitmap(_bitmap, _transparency, BitmapInterpolationMode.Linear, _rect);
        }

        protected internal override Point2F EndPoint
        {
            set
            {
                _rect.Right = Math.Max(_rect.Left + 5, value.X);
                _rect.Bottom = Math.Max(_rect.Top + 5, value.Y);
            }
        }
    }
}