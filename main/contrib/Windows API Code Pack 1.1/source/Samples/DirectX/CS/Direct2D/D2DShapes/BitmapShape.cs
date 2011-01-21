// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.ComponentModel;
using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;

namespace D2DShapes
{
    internal class BitmapShape : DrawingShape
    {
        #region Fields
        private RectF destRect;
        private RectF sourceRect;
        private bool drawSection;
        private float opacity; 
        #endregion

        #region Properties
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public RectF DestRect
        {
            get { return destRect; }
            set { destRect = value; }
        }

        [TypeConverter(typeof(ExpandableObjectConverter))]
        public RectF SourceRect
        {
            get { return sourceRect; }
            set { sourceRect = value; }
        }

        public bool DrawSection
        {
            get { return drawSection; }
            set { drawSection = value; }
        }

        public float Opacity
        {
            get { return opacity; }
            set { opacity = value; }
        } 
        #endregion

        public BitmapShape(RenderTarget initialRenderTarget, Random random, D2DFactory d2DFactory, D2DBitmap bitmap)
            : base(initialRenderTarget, random, d2DFactory, bitmap)
        {
            DestRect = RandomRect(CanvasWidth, CanvasHeight);
            opacity = RandomOpacity();
            DrawSection = Random.NextDouble() < 0.25;
            if (drawSection)
                SourceRect = RandomRect(
                    Bitmap.PixelSize.Width,
                    Bitmap.PixelSize.Height);
        }

        protected internal override void ChangeRenderTarget(RenderTarget newRenderTarget)
        {
            //no rendertarget dependent members
        }

        protected internal override void Draw(RenderTarget renderTarget)
        {
            if (DrawSection)
                renderTarget.DrawBitmap(Bitmap, opacity, BitmapInterpolationMode.Linear, DestRect, SourceRect);
            else
                renderTarget.DrawBitmap(Bitmap, opacity, BitmapInterpolationMode.Linear, DestRect);
        }

        public override bool HitTest(Point2F point)
        {
            return DestRect.Top <= point.Y &&
                   DestRect.Bottom >= point.Y &&
                   DestRect.Left <= point.X &&
                   DestRect.Right >= point.X;
        }
    }
}
