// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;

namespace D2DShapes
{
    internal class LayerShape : DrawingShape
    {
        #region Properties
        private readonly List<DrawingShape> shapes = new List<DrawingShape>();
        public override List<DrawingShape> ChildShapes
        {
            get
            {
                return shapes;
            }
        }

        internal override sealed D2DBitmap Bitmap
        {
            get
            {
                return base.Bitmap;
            }
            set
            {
                base.Bitmap = value;
                foreach (var shape in ChildShapes)
                {
                    shape.Bitmap = value;
                }
            }
        }

        private LayerParameters parameters;
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public LayerParameters Parameters
        {
            get { return parameters; }
            set { parameters = value; }
        }

        [TypeConverter(typeof(ExpandableObjectConverter))]
        public Layer Layer { get; set; }

        [TypeConverter(typeof(ExpandableObjectConverter))]
        public GeometryShape GeometricMaskShape { get; set; } 
        #endregion

        public LayerShape(RenderTarget initialRenderTarget, Random random, D2DFactory d2DFactory, D2DBitmap bitmap, int count)
            : base(initialRenderTarget, random, d2DFactory, bitmap)
        {
            Parameters = new LayerParameters();
            parameters.ContentBounds = CoinFlip ? RandomRect(CanvasWidth, CanvasHeight) : new RectF(0, 0, CanvasWidth, CanvasHeight);
            if (CoinFlip)
            {
                GeometricMaskShape = new GeometryShape(initialRenderTarget, random, d2DFactory, Bitmap);
                parameters.GeometricMask = GeometricMaskShape.Geometry;
            }
            parameters.MaskAntiAliasMode = CoinFlip ? AntiAliasMode.Aliased : AntiAliasMode.PerPrimitive;
            parameters.MaskTransform = RandomMatrix3x2();
            parameters.Opacity = RandomOpacity();
            if (CoinFlip)
                parameters.OpacityBrush = RandomOpacityBrush();
            parameters.Options = CoinFlip ? LayerOptions.InitializeForClearType : LayerOptions.None;

            for(int i = 0; i < count; i++)
            {
                shapes.Add(RandomShape());
            }
        }

        public override bool HitTest(Point2F point)
        {
            return parameters.ContentBounds.Top <= point.Y &&
                   parameters.ContentBounds.Bottom >= point.Y &&
                   parameters.ContentBounds.Left <= point.X &&
                   parameters.ContentBounds.Right >= point.X &&
                   (GeometricMaskShape != null ? GeometricMaskShape.Geometry.FillContainsPoint(point, 5) : true) &&
                   parameters.Opacity > 0;
        }

        private DrawingShape RandomShape()
        {
            double which = Random.NextDouble();
            //GDI does not work in layers
            //return new GDIEllipsesShape(RenderTarget, Random, d2DFactory, Bitmap, 1);
            //layers inside of layers can be really slow
            //if (which < 0.01)
            //    return new LayerShape(RenderTarget, Random, d2DFactory, Bitmap, 1);
            if (which < 0.1)
                return new LineShape(RenderTarget, Random, d2DFactory, Bitmap);
            if (which < 0.3)
                return new RectangleShape(RenderTarget, Random, d2DFactory, Bitmap);
            if (which < 0.5)
                return new RoundRectangleShape(RenderTarget, Random, d2DFactory, Bitmap);
            if (which < 0.6)
                return new BitmapShape(RenderTarget, Random, d2DFactory, Bitmap);
            if (which < 0.8)
                return new EllipseShape(RenderTarget, Random, d2DFactory, Bitmap);
            return new GeometryShape(RenderTarget, Random, d2DFactory, Bitmap);
        }

        private Brush RandomOpacityBrush()
        {
            return CoinFlip ? (Brush) RandomRadialBrush() : RandomGradientBrush();
        }

        protected internal override void ChangeRenderTarget(RenderTarget newRenderTarget)
        {
            if (GeometricMaskShape != null)
            {
                GeometricMaskShape.Bitmap = Bitmap;
                GeometricMaskShape.RenderTarget = newRenderTarget;
            }
            if (parameters.OpacityBrush != null)
                parameters.OpacityBrush = CopyBrushToRenderTarget(parameters.OpacityBrush, newRenderTarget);
            foreach (var shape in ChildShapes)
            {
                shape.Bitmap = Bitmap;
                shape.RenderTarget = newRenderTarget;
            }
            Layer = null;
        }

        protected internal override void Draw(RenderTarget renderTarget)
        {
            if (Layer == null ||
                Layer.Size.Width != renderTarget.Size.Width ||
                Layer.Size.Height != renderTarget.Size.Height)
            {
                if (Layer != null)
                    Layer.Dispose();
                Layer = renderTarget.CreateLayer(renderTarget.Size);
            }
            renderTarget.PushLayer(Parameters, Layer);
            foreach(DrawingShape shape in shapes)
                shape.Draw(renderTarget);
            renderTarget.PopLayer();
        }

        public override void Dispose()
        {
            foreach (var shape in ChildShapes)
            {
                shape.Dispose();
            }
            ChildShapes.Clear();
            if (parameters.OpacityBrush != null)
                parameters.OpacityBrush.Dispose();
            if (GeometricMaskShape != null)
                GeometricMaskShape.Dispose();
            GeometricMaskShape = null;
            parameters.OpacityBrush = null;
            if (Layer != null)
                Layer.Dispose();
            Layer = null;
            base.Dispose();
        }
    }
}
