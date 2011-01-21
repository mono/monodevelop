// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Drawing;
using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;
using Microsoft.WindowsAPICodePack.DirectX.Graphics;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D;

namespace D2DShapes
{
    internal class GDIEllipsesShape : DrawingShape
    {
        private struct GdiEllipse
        {
            public Pen pen;
            public Rectangle rect;
        };

        readonly List<GdiEllipse> ellipses = new List<GdiEllipse>();
        private GdiInteropRenderTarget gdiRenderTarget;

        public GDIEllipsesShape(RenderTarget initialRenderTarget, Random random, D2DFactory d2DFactory, D2DBitmap bitmap, int count)
            : base(initialRenderTarget, random, d2DFactory, bitmap)
        {
            for(int i = 0; i < count; i++)
            {
                ellipses.Add(RandomGdiEllipse());
            }

            if (RenderTargetSupportsGDI(RenderTarget))
            {
                gdiRenderTarget = RenderTarget.GdiInteropRenderTarget;
            }
        }

        private static bool RenderTargetSupportsGDI(RenderTarget rt)
        {
            var propertiesToSupport = new RenderTargetProperties(
                RenderTargetType.Default,
                new PixelFormat(
                    Format.B8G8R8A8UNorm,
                    AlphaMode.Ignore),
                96,
                96,
                RenderTargetUsages.GdiCompatible,
                FeatureLevel.Default);
            if (rt.IsSupported(propertiesToSupport))
                return true;
            propertiesToSupport = new RenderTargetProperties(
                RenderTargetType.Default,
                new PixelFormat(
                    Format.B8G8R8A8UNorm,
                    AlphaMode.Premultiplied),
                96,
                96,
                RenderTargetUsages.GdiCompatible,
                FeatureLevel.Default);
            return rt.IsSupported(propertiesToSupport);
        }

        private GdiEllipse RandomGdiEllipse()
        {
            return new GdiEllipse
                       {
                           pen = new Pen(Brushes.Black),
                           rect = RandomGdiRect()
                       };
        }

        private Rectangle RandomGdiRect()
        {
            int x1 = Random.Next(0, CanvasWidth);
            int x2 = Random.Next(0, CanvasWidth);
            int y1 = Random.Next(0, CanvasHeight);
            int y2 = Random.Next(0, CanvasHeight);
            return new Rectangle(
                Math.Min(x1, x2),
                Math.Min(y1, y2),
                Math.Max(x1, x2) - Math.Min(x1, x2),
                Math.Max(y1, y2) - Math.Min(y1, y2));
        }

        protected internal override void ChangeRenderTarget(RenderTarget newRenderTarget)
        {
            if (RenderTargetSupportsGDI(newRenderTarget))
            {
                gdiRenderTarget = newRenderTarget.GdiInteropRenderTarget;
            }
            else if (gdiRenderTarget != null)
            {
                gdiRenderTarget.Dispose();
                gdiRenderTarget = null;
            }
        }

        protected internal override void Draw(RenderTarget renderTarget)
        {
            if (gdiRenderTarget != null)
            {
                IntPtr dc = gdiRenderTarget.GetDC(DCInitializeMode.Copy);
                Graphics g = Graphics.FromHdc(dc);
                foreach (var ellipse in ellipses)
                {
                    g.DrawEllipse(ellipse.pen, ellipse.rect);
                }
                g.Dispose();
                gdiRenderTarget.ReleaseDC();
            }
        }

        public override bool HitTest(Point2F point)
        {
            foreach (var ellipse in ellipses)
            {

                EllipseGeometry g = d2DFactory.CreateEllipseGeometry(new Ellipse(
                    new Point2F(
                        (ellipse.rect.Left + ellipse.rect.Right) / 2,
                        (ellipse.rect.Top + ellipse.rect.Bottom) / 2),
                        ellipse.rect.Width / 2,
                        ellipse.rect.Height / 2));
                bool ret = g.FillContainsPoint(point, 1);
                g.Dispose();
                if (ret)
                    return true;
            }
            return false;
        }

        public override void Dispose()
        {
            if (gdiRenderTarget != null)
                gdiRenderTarget.Dispose();
            gdiRenderTarget = null;
            base.Dispose();
        }
    }
}
