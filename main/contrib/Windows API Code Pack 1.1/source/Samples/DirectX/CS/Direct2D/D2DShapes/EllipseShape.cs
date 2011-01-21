// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;

namespace D2DShapes
{
    internal class EllipseShape : DrawingShape
    {
        internal Ellipse ellipse;

        public EllipseShape(RenderTarget initialRenderTarget, Random random, D2DFactory d2DFactory, D2DBitmap bitmap)
            : base(initialRenderTarget, random, d2DFactory, bitmap)
        {
            ellipse = RandomEllipse();
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
                renderTarget.FillEllipse(ellipse, FillBrush);
            }
            if (PenBrush != null)
            {
                if (StrokeStyle != null)
                    renderTarget.DrawEllipse(ellipse, PenBrush, StrokeWidth, StrokeStyle);
                else
                    renderTarget.DrawEllipse(ellipse, PenBrush, StrokeWidth);
            }
        }

        public override bool HitTest(Point2F point)
        {
            EllipseGeometry g = d2DFactory.CreateEllipseGeometry(ellipse);
            bool ret = g.FillContainsPoint(point, 1);
            g.Dispose();
            return ret;
        }
    }
}
