// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
#if _D2DTRACE
using System.Diagnostics;
#endif
using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;

namespace D2DShapes
{
    public abstract class DrawingShape : IDisposable
    {
        #region Properties and Fields
        private static int shapesCreated;
        private int shapeID;
        protected internal Random Random { get; set; }
        protected internal D2DFactory d2DFactory;

        protected D2DBitmap bitmap;
        internal virtual D2DBitmap Bitmap
        {
            get { return bitmap; }
            set { bitmap = value; }
        }

        [TypeConverter(typeof(ExpandableObjectConverter))]
        public Brush FillBrush { get; set; }

        [TypeConverter(typeof(ExpandableObjectConverter))]
        public Brush PenBrush { get; set; }

        public float StrokeWidth { get; set; }

        [TypeConverter(typeof(ExpandableObjectConverter))]
        public StrokeStyle StrokeStyle { get; set; }

        protected const float FlatteningTolerance = 5;

        private RenderTarget renderTarget;
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public RenderTarget RenderTarget
        {
            get { return renderTarget; }
            set
            {
                if (renderTarget != value)
                {
                    ChangeRenderTarget(value);
                    renderTarget = value;
                }
            }
        }

        protected bool coolStrokes; //used by GeometryShape to create geometries from modified dashed strokes

        public virtual List<DrawingShape> ChildShapes
        {
            get
            {
                return null;
            }
        }

        protected int CanvasWidth
        {
            get
            {
                return (int)RenderTarget.PixelSize.Width;
            }
        }

        protected int CanvasHeight
        {
            get
            {
                return (int)RenderTarget.PixelSize.Height;
            }
        } 
        #endregion

        #region DrawingShape() - CTOR
        protected DrawingShape(RenderTarget initialRenderTarget, Random random, D2DFactory d2DFactory, D2DBitmap bitmap)
        {
            renderTarget = initialRenderTarget;
            Random = random;
            this.d2DFactory = d2DFactory;
            this.bitmap = bitmap;
            shapeID = ++shapesCreated;
        } 
        #endregion

        public override string ToString()
        {
            return shapeID + ":" + GetType().Name;
        }

        #region Virtual methods
        /// <summary>
        /// Draws the shape to the specified render target.
        /// </summary>
        /// <param name="renderTarget">The render target.</param>
        protected internal abstract void Draw(RenderTarget renderTarget);
        /// <summary>
        /// Changes the render target of the shape - need to create copies of all render target-dependent properties to the new render target. 
        /// </summary>
        /// <param name="newRenderTarget">The render target.</param>
        protected internal abstract void ChangeRenderTarget(RenderTarget newRenderTarget);
        /// <summary>
        /// Hit test of the shape.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>true if the given point belongs to the shape</returns>
        public virtual bool HitTest(Point2F point)
        {
            return false;
        } 
        #endregion

        #region Methods to randomize properties of shapes
        #region CoinFlip
        protected internal bool CoinFlip
        {
            get
            {
                return Random.NextDouble() < 0.5;
            }
        }
        #endregion

        #region RandomStrokeWidth()
        protected float RandomStrokeWidth()
        {
            float ret = (float)(32 * Random.NextDouble() * Random.NextDouble());
#if _D2DTRACE
            Trace.WriteLine("Stroke width: " + ret);
#endif
            return ret;
        }
        #endregion

        #region RandomOpacity()
        protected float RandomOpacity()
        {
            float ret = Math.Min(1f, Math.Max(0f, 1.2f - (float)(Random.NextDouble() * 1.2)));
#if _D2DTRACE
            Trace.WriteLine("Opacity: " + ret);
#endif
            return ret;
        }
        #endregion

        #region RandomPoint()
        protected internal Point2F RandomPoint()
        {
            previousPoint = new Point2F(
                (float)Random.NextDouble() * CanvasWidth,
                (float)Random.NextDouble() * CanvasHeight);
#if _D2DTRACE
            Trace.WriteLine("Point: " + previousPoint.Value.X + "," + previousPoint.Value.Y);
#endif
            return previousPoint.Value;
        }
        #endregion

        #region RandomNearPoint()
        protected internal Point2F? previousPoint;
        protected internal Point2F RandomNearPoint()
        {
            Point2F ret;
            if (previousPoint == null || CoinFlip)
            {
                ret = RandomPoint();
            }
            else
            {
                ret = new Point2F(
                    previousPoint.Value.X + (float)Random.NextDouble() * 100 - 50,
                    previousPoint.Value.Y + (float)Random.NextDouble() * 100 - 50);
            }
            previousPoint = ret;
#if _D2DTRACE
            Trace.WriteLine("Point: " + previousPoint.Value.X + "," + previousPoint.Value.Y);
#endif
            return ret;
        }
        #endregion

        #region RandomRect()
        protected internal RectF RandomRect(float maxWidth, float maxHeight)
        {
            float x1 = (float)Random.NextDouble() * maxWidth;
            float x2 = (float)Random.NextDouble() * maxWidth;
            float y1 = (float)Random.NextDouble() * maxHeight;
            float y2 = (float)Random.NextDouble() * maxHeight;
            //return new RectF(x1, y1, x2, y2);
            RectF ret = new RectF(
                Math.Min(x1, x2),
                Math.Min(y1, y2),
                Math.Max(x1, x2),
                Math.Max(y1, y2));
#if _D2DTRACE
            Trace.WriteLine("RectF: Left:" + ret.Left + ", Top:" + ret.Top + ", Right:" + ret.Right + ", Bottom:" + ret.Bottom);
#endif
            return ret;
        }
        #endregion

        #region RandomBrush()
        protected internal Brush RandomBrush()
        {
            double which = Random.NextDouble();
            if (which < 0.5)
                return RandomSolidBrush();
            if (which < 0.7)
                return RandomGradientBrush();
            if (which < 0.9)
                return RandomRadialBrush();
            return RandomBitmapBrush();
        }
        #endregion

        #region RandomSolidBrush()
        protected internal SolidColorBrush RandomSolidBrush()
        {
#if _D2DTRACE
            Trace.WriteLine("SolidBrush:");
#endif
            return RenderTarget.CreateSolidColorBrush(
                RandomColor(),
                RandomBrushProperties());
        }
        #endregion

        #region RandomGradientBrush()
        protected internal LinearGradientBrush RandomGradientBrush()
        {
#if _D2DTRACE
            Trace.WriteLine("LinearGradientBrush:");
#endif
            return RenderTarget.CreateLinearGradientBrush(
                new LinearGradientBrushProperties(
                    RandomPoint(),
                    RandomPoint()),
                RandomGradientStopCollection(),
                RandomBrushProperties());
        }
        #endregion

        #region RandomRadialBrush()
        protected internal RadialGradientBrush RandomRadialBrush()
        {
#if _D2DTRACE
            Trace.WriteLine("RadialGradientBrush:");
#endif
            float radiusX = (float)(Random.NextDouble() * CanvasWidth);
            float radiusY = (float)(Random.NextDouble() * CanvasHeight);
#if _D2DTRACE
            Trace.WriteLine("Radius: " + radiusX + "," + radiusY);
#endif
            return RenderTarget.CreateRadialGradientBrush(
                new RadialGradientBrushProperties(
                    RandomPoint(),
                    RandomPoint(),
                    radiusX,
                    radiusY),
                RandomGradientStopCollection(),
                RandomBrushProperties());
        }
        #endregion

        #region RandomBitmapBrush()
        private BitmapBrush RandomBitmapBrush()
        {
#if _D2DTRACE
            Trace.WriteLine("SolidBrush:");
#endif
            BitmapInterpolationMode interpolationMode = Random.NextDouble() < 0.25 ? BitmapInterpolationMode.Linear : BitmapInterpolationMode.NearestNeighbor;
#if _D2DTRACE
            Trace.WriteLine("BitmapInterpolationMode: " + interpolationMode);
#endif
            BitmapBrush ret = RenderTarget.CreateBitmapBrush(
                Bitmap,
                new BitmapBrushProperties(
                    RandomExtendMode(),
                    RandomExtendMode(),
                    interpolationMode),
                new BrushProperties(
                    RandomOpacity(),
                    RandomMatrix3x2()));
            return ret;
        }
        #endregion

        #region RandomGradientStopCollection()
        private GradientStopCollection RandomGradientStopCollection()
        {
            int stopsCount = Random.Next(2, 16);
            var stopPoints = new List<float>();
            for (int i = 0; i < stopsCount; i++)
                stopPoints.Add((float)Random.NextDouble());
            stopPoints.Sort();
            var stops = new GradientStop[stopsCount];
            for (int i = 0; i < stopsCount; i++)
                stops[i] = new GradientStop(stopPoints[i], RandomColor());
            Gamma gamma = Random.NextDouble() < 0.7 ? Gamma.StandardRgb : Gamma.Linear;
#if _D2DTRACE
            Trace.WriteLine("GradientStopCollection:");
            Trace.WriteLine("  Gamma: " + gamma);
            foreach (var stop in stops)
                Trace.WriteLine(string.Format("  GradientStop: Stop: {0}, Color (RGBA): {1},{2},{3},{4}", stop.Position, stop.Color.R, stop.Color.G, stop.Color.B, stop.Color.A));
#endif
            return RenderTarget.CreateGradientStopCollection(
                stops,
                gamma,
                RandomExtendMode());
        }
        #endregion

        #region RandomExtendMode()
        private ExtendMode RandomExtendMode()
        {
            double which = Random.NextDouble();
            ExtendMode ret = which < 0.33 ? ExtendMode.Wrap : which < 0.65 ? ExtendMode.Mirror : ExtendMode.Clamp;
#if _D2DTRACE
            Trace.WriteLine("  ExtendMode:" + ret);
#endif
            return ret;
        }
        #endregion

        #region RandomBrushProperties()
        private BrushProperties RandomBrushProperties()
        {
            float opacity = (float)Random.NextDouble();
#if _D2DTRACE
            Trace.WriteLine("BrushProperties: Opacity: " + opacity);
#endif
            return new BrushProperties(
                opacity,
                RandomMatrix3x2());
        }
        #endregion

        #region RandomMatrix3x2()
        protected internal Matrix3x2F RandomMatrix3x2()
        {
            var which = Random.NextDouble();
            //return Matrix3x2F.Skew(90, 0); //check for bug 730701
            Matrix3x2F ret;
            if (which < 0.5)
            {
                ret = new Matrix3x2F(
                    1.0f - (float) Random.NextDouble()*(float) Random.NextDouble(),
                    (float) Random.NextDouble()*(float) Random.NextDouble(),
                    (float) Random.NextDouble()*(float) Random.NextDouble(),
                    1.0f - (float) Random.NextDouble()*(float) Random.NextDouble(),
                    (float) Random.NextDouble()*(float) Random.NextDouble(),
                    (float) Random.NextDouble()*(float) Random.NextDouble()
                    );
                TraceMatrix(ret);
                return ret;
            }
            if (which < 0.8)
            {
                ret = Matrix3x2F.Identity;
                TraceMatrix(ret);
                return ret;
            }
            if (which < 0.85)
            {
                ret = Matrix3x2F.Translation(
                    Random.Next(-20, 20),
                    Random.Next(-20, 20));
                TraceMatrix(ret);
                return ret;
            }
            if (which < 0.90)
            {
                ret = Matrix3x2F.Skew(
                    (float)(Random.NextDouble() * Random.NextDouble() * 89),
                    (float)(Random.NextDouble() * Random.NextDouble() * 89),
                    CoinFlip ? new Point2F(0, 0) : RandomPoint());
                TraceMatrix(ret);
                return ret;
            }
            if (which < 0.95)
            {
                ret = Matrix3x2F.Scale(
                    1 + (float)((Random.NextDouble() - 0.5) * Random.NextDouble()),
                    1 + (float)((Random.NextDouble() - 0.5) * Random.NextDouble()),
                    CoinFlip ? new Point2F(0, 0) : RandomPoint());
                TraceMatrix(ret);
                return ret;
            }
            ret = Matrix3x2F.Rotation(
                (float)((Random.NextDouble() - 0.5) * Random.NextDouble() * 720),
                CoinFlip ? new Point2F(0,0) : RandomPoint());
            TraceMatrix(ret);
            return ret;
        }

        private static void TraceMatrix(Matrix3x2F matrix)
        {
#if _D2DTRACE
            Trace.WriteLine(string.Format("  Matrix3x2: {0}, {1}", matrix.M11, matrix.M12));
            Trace.WriteLine(string.Format("             {0}, {1}", matrix.M21, matrix.M22));
            Trace.WriteLine(string.Format("             {0}, {1}", matrix.M31, matrix.M32));
#endif
        }
        #endregion

        #region RandomColor()
        protected internal ColorF RandomColor()
        {
            ColorF ret = new ColorF(
                (float)Random.NextDouble(),
                (float)Random.NextDouble(),
                (float)Random.NextDouble(),
                RandomOpacity());
#if _D2DTRACE
            Trace.WriteLine(string.Format("ColorF (RGBA): {0},{1},{2},{3}", ret.R, ret.G, ret.B, ret.A));
#endif
            return ret;
        }
        #endregion

        #region RandomStrokeStyle()
        protected internal StrokeStyle RandomStrokeStyle()
        {
            var strokeStyleProperties = new StrokeStyleProperties(
                RandomCapStyle(),
                RandomCapStyle(),
                RandomCapStyle(),
                RandomLineJoin(),
                1.0f + 2.0f * (float)Random.NextDouble(),
                RandomDashStyle(),
                5.0f * (float)Random.NextDouble());
            if (strokeStyleProperties.DashStyle == DashStyle.Custom)
                return d2DFactory.CreateStrokeStyle(strokeStyleProperties, RandomDashes());
            else
                return d2DFactory.CreateStrokeStyle(strokeStyleProperties);
        }
        #endregion

        #region RandomDashes()
        private float[] RandomDashes()
        {
            var dashes = new float[Random.Next(2, 20)];
            for (int i = 0; i < dashes.Length; i++)
                dashes[i] = 3.0f * (float)Random.NextDouble();
            return dashes;
        }
        #endregion

        #region RandomDashStyle()
        private DashStyle RandomDashStyle()
        {
            double which = Random.NextDouble();
            if (!coolStrokes && which < 0.5)
                return DashStyle.Solid;
            if (which < 0.75)
                return DashStyle.Custom;
            return (DashStyle)(Random.Next((int)DashStyle.Dash, (int)DashStyle.DashDotDot));
        }
        #endregion

        #region RandomLineJoin()
        private LineJoin RandomLineJoin()
        {
            return (LineJoin)(Random.Next(0, 3));
        }
        #endregion

        #region RandomCapStyle()
        private CapStyle RandomCapStyle()
        {
            return (CapStyle)(Random.Next(0, 3));
        }
        #endregion

        #region RandomEllipse()
        protected internal Ellipse RandomEllipse()
        {
            return new Ellipse(
                RandomPoint(),
                (float)(0.5 * CanvasWidth * Random.NextDouble()),
                (float)(0.5 * CanvasHeight * Random.NextDouble()));
        }
        #endregion

        #region RandomRoundedRect()
        protected internal RoundedRect RandomRoundedRect()
        {
            return new RoundedRect(
                RandomRect(CanvasWidth, CanvasHeight),
                (float)(32 * Random.NextDouble()),
                (float)(32 * Random.NextDouble()));
        }
        #endregion 
        #endregion

        #region CopyBrushToRenderTarget()
        /// <summary>
        /// Creates and returns a copy of the brush in the new render target.
        /// Used for changing render targets.
        /// A brush belongs to a render target, so when you want to draw with same brush in another render target
        /// - you need to create a copy of the brush in the new render target.
        /// </summary>
        /// <param name="sourceBrush">The brush.</param>
        /// <param name="newRenderTarget">The new render target.</param>
        /// <returns></returns>
        protected internal Brush CopyBrushToRenderTarget(Brush sourceBrush, RenderTarget newRenderTarget)
        {
            if (sourceBrush == null || newRenderTarget == null)
                return null;
            Brush newBrush;
            if (sourceBrush is SolidColorBrush)
            {
                newBrush = newRenderTarget.CreateSolidColorBrush(
                    ((SolidColorBrush)sourceBrush).Color,
                    new BrushProperties(sourceBrush.Opacity, sourceBrush.Transform));
                sourceBrush.Dispose();
                return newBrush;
            }
            if (sourceBrush is LinearGradientBrush)
            {
                var oldGSC = ((LinearGradientBrush)sourceBrush).GradientStops;
                var newGSC = newRenderTarget.CreateGradientStopCollection(oldGSC, oldGSC.ColorInterpolationGamma, oldGSC.ExtendMode);
                oldGSC.Dispose();
                newBrush = newRenderTarget.CreateLinearGradientBrush(
                    new LinearGradientBrushProperties(
                        ((LinearGradientBrush)sourceBrush).StartPoint,
                        ((LinearGradientBrush)sourceBrush).EndPoint),
                        newGSC,
                        new BrushProperties(sourceBrush.Opacity, sourceBrush.Transform));
                sourceBrush.Dispose();
                return newBrush;
            }
            if (sourceBrush is RadialGradientBrush)
            {
                var oldGSC = ((RadialGradientBrush)sourceBrush).GradientStops;
                var newGSC = newRenderTarget.CreateGradientStopCollection(oldGSC, oldGSC.ColorInterpolationGamma, oldGSC.ExtendMode);
                oldGSC.Dispose();
                newBrush = newRenderTarget.CreateRadialGradientBrush(
                    new RadialGradientBrushProperties(
                        ((RadialGradientBrush)sourceBrush).Center,
                        ((RadialGradientBrush)sourceBrush).GradientOriginOffset,
                        ((RadialGradientBrush)sourceBrush).RadiusX,
                        ((RadialGradientBrush)sourceBrush).RadiusY),
                        newGSC,
                        new BrushProperties(sourceBrush.Opacity, sourceBrush.Transform));
                sourceBrush.Dispose();
                return newBrush;
            }
            if (sourceBrush is BitmapBrush)
            {
                newBrush = newRenderTarget.CreateBitmapBrush(
                    Bitmap,
                    new BitmapBrushProperties(
                        ((BitmapBrush)sourceBrush).ExtendModeX,
                        ((BitmapBrush)sourceBrush).ExtendModeY,
                        ((BitmapBrush)sourceBrush).InterpolationMode),
                    new BrushProperties(sourceBrush.Opacity, sourceBrush.Transform));
                sourceBrush.Dispose();
                return newBrush;
            }
            throw new NotImplementedException("Unknown brush type used");
        }
        #endregion

        #region IDisposable.Dispose()
        protected bool disposed;
        public virtual void Dispose()
        {
            if (!disposed)
            {
                if (FillBrush != null)
                {
                    FillBrush.Dispose();
                    FillBrush = null;
                }
                if (PenBrush != null)
                {
                    PenBrush.Dispose();
                    PenBrush = null;
                }
                if (StrokeStyle != null)
                {
                    StrokeStyle.Dispose();
                    StrokeStyle = null;
                }
            }
        } 
        #endregion
    }
}
