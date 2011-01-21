// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;

namespace D2DShapes
{
    internal class LineShape : DrawingShape
    {
        internal Point2F point0, point1;

        public LineShape(RenderTarget initialRenderTarget, Random random, D2DFactory d2DFactory, D2DBitmap bitmap)
            : base(initialRenderTarget, random, d2DFactory, bitmap)
        {
            point0 = RandomPoint();
            point1 = RandomPoint();
            PenBrush = RandomBrush();
            StrokeWidth = RandomStrokeWidth();
            if (CoinFlip)
                StrokeStyle = RandomStrokeStyle();
        }

        protected internal override void ChangeRenderTarget(RenderTarget newRenderTarget)
        {
            PenBrush = CopyBrushToRenderTarget(PenBrush, newRenderTarget);
        }

        protected internal override void Draw(RenderTarget renderTarget)
        {
            if (StrokeStyle != null)
                renderTarget.DrawLine(point0, point1, PenBrush, StrokeWidth, StrokeStyle);
            else
                renderTarget.DrawLine(point0, point1, PenBrush, StrokeWidth);
        }
    }
}
