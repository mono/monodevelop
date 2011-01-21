// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;

namespace D2DShapes
{
    internal class RoundRectangleShape : DrawingShape
    {
        internal RoundedRect rect;

        public RoundRectangleShape(RenderTarget initialRenderTarget, Random random, D2DFactory d2DFactory, D2DBitmap bitmap)
            : base(initialRenderTarget, random, d2DFactory, bitmap)
        {
            rect = RandomRoundedRect();
            double which = Random.NextDouble();
            if (which < 0.67)
                PenBrush = RandomBrush();
            if (which > 0.33)
                FillBrush = RandomBrush();
            if (CoinFlip)
                StrokeStyle = RandomStrokeStyle();
            StrokeWidth = RandomStrokeWidth();
        }

        protected internal override void ChangeRenderTarget(RenderTarget newRenderTarget)
        {
            PenBrush = CopyBrushToRenderTarget(PenBrush, newRenderTarget);
            FillBrush = CopyBrushToRenderTarget(FillBrush, newRenderTarget);
        }

        protected internal override void Draw(RenderTarget renderTarget)
        {
            if (FillBrush != null)
            {
                renderTarget.FillRoundedRectangle(rect, FillBrush);
            }
            if (PenBrush != null)
            {
                if (StrokeStyle != null)
                    renderTarget.DrawRoundedRectangle(rect, PenBrush, StrokeWidth, StrokeStyle);
                else
                    renderTarget.DrawRoundedRectangle(rect, PenBrush, StrokeWidth);
            }
        }

        public override bool HitTest(Point2F point)
        {
            RoundedRectangleGeometry g = d2DFactory.CreateRoundedRectangleGeometry(rect);
            bool ret = g.FillContainsPoint(point, 1);
            g.Dispose();
            return ret;
        }
   }
}
