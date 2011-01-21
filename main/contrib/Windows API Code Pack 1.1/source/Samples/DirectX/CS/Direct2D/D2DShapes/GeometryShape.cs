// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;

namespace D2DShapes
{
    internal class GeometryShape : DrawingShape
    {
        private Geometry geometry;
        internal PathGeometry geometryOutlined;
        internal PathGeometry geometrySimplified;
        internal PathGeometry geometryWidened;
        internal Matrix3x2F? worldTransform;

        public GeometryShape(RenderTarget initialRenderTarget, Random random, D2DFactory d2DFactory, D2DBitmap bitmap)
            : base(initialRenderTarget, random, d2DFactory, bitmap)
        {
            coolStrokes = CoinFlip;
            double which = Random.NextDouble();
            if (which < 0.67 || coolStrokes)
                PenBrush = RandomBrush();
            if (!coolStrokes && which > 0.33)
                FillBrush = RandomBrush();
            if (coolStrokes || CoinFlip)
                StrokeStyle = RandomStrokeStyle();
            if (CoinFlip)
                worldTransform = RandomMatrix3x2();
            StrokeWidth = RandomStrokeWidth();
            geometry = RandomGeometry();
            if (coolStrokes || Random.NextDouble() < 0.3)
                ModifyGeometry();
            if (coolStrokes && CoinFlip)
            {
                ModifyGeometry();
            }
        }

        [TypeConverter(typeof(ExpandableObjectConverter))]
        public Geometry Geometry
        {
            get { return geometry; }
            set { geometry = value; }
        }

        private void ModifyGeometry()
        {
            GeometrySink geometrySink;
            double which = Random.NextDouble();
            if (which < 0.33)
            {
                geometryOutlined = d2DFactory.CreatePathGeometry();
                geometrySink = geometryOutlined.Open();
                geometry.Outline(geometrySink, FlatteningTolerance);
                geometrySink.Close();
                geometrySink.Dispose();
                geometry.Dispose();
                geometry = geometryOutlined;
            }
            else if (which < 0.67)
            {
                geometrySimplified = d2DFactory.CreatePathGeometry();
                geometrySink = geometrySimplified.Open();
                geometry.Simplify(
                    CoinFlip
                        ? GeometrySimplificationOption.Lines
                        : GeometrySimplificationOption.CubicsAndLines,
                    geometrySink, 
                    FlatteningTolerance
                    );
                geometrySink.Close();
                geometrySink.Dispose();
                geometry.Dispose();
                geometry = geometrySimplified;
            }
            else
            {
                geometryWidened = d2DFactory.CreatePathGeometry();
                geometrySink = geometryWidened.Open();
                geometry.Widen(
                    RandomStrokeWidth(), //75
                    RandomStrokeStyle(),                    
                    geometrySink,
                    FlatteningTolerance);
                geometrySink.Close();
                geometrySink.Dispose();
                geometry.Dispose();
                geometry = geometryWidened;
            }
        }

        //this could be called recursively
        public Geometry RandomGeometry()
        {
            return RandomGeometry(0);
        }

        public Geometry RandomGeometry(int level)
        {
            Geometry g = null;

            while (g == null)
            {
                double which = Random.NextDouble();

                if (which < 0.20)
                    g = RandomEllipseGeometry();
                else if (which < 0.40)
                    g = RandomRoundRectGeometry();
                else if (which < 0.60)
                    g = RandomRectangleGeometry();
                else if (which < 0.80)
                    g = RandomPathGeometry();
                else if (level < 3)
                    g = RandomGeometryGroup(level + 1);
            }

            if (worldTransform.HasValue)
                g = d2DFactory.CreateTransformedGeometry(g, worldTransform.Value);

            return g;
        }

        private Geometry RandomTransformedGeometry()
        {
            Geometry start, ret;
            start = RandomGeometry();
            ret = d2DFactory.CreateTransformedGeometry(
                start,
                RandomMatrix3x2());
            start.Dispose();
            return ret;
        }
        
        private Geometry RandomGeometryGroup(int level)
        {
            var geometries = new List<Geometry>();
            int count = Random.Next(1, 5);
            for (int i = 0; i < count; i++)
                geometries.Add(RandomGeometry(level));
            GeometryGroup ret = d2DFactory.CreateGeometryGroup(
                Random.NextDouble() < .5 ? FillMode.Winding : FillMode.Alternate,
                geometries);
            foreach (var g in geometries)
            {
                g.Dispose();
            }
            return ret;
        }

        private PathGeometry RandomPathGeometry()
        {
            PathGeometry g = d2DFactory.CreatePathGeometry();
            int totalSegmentCount = 0;
            int figureCount = Random.Next(1, 2);
            using (GeometrySink sink = g.Open())
            {
                for (int f = 0; f < figureCount; f++)
                {
                    int segmentCount = Random.Next(2, 20);
                    AddRandomFigure(sink, segmentCount);
                    totalSegmentCount += segmentCount;
                }
                sink.Close();
            }
            System.Diagnostics.Debug.Assert(g.SegmentCount == totalSegmentCount);
            System.Diagnostics.Debug.Assert(g.FigureCount == figureCount);
            return g;
        }

        private void AddRandomFigure(IGeometrySink sink, int segmentCount)
        {
            previousPoint = null;
            sink.BeginFigure(
                RandomNearPoint(),
                CoinFlip ? FigureBegin.Filled : FigureBegin.Hollow);
            FigureEnd end = CoinFlip ? FigureEnd.Closed : FigureEnd.Closed;
            if (end == FigureEnd.Closed)
                segmentCount--;
            if (CoinFlip)
                for (int i = 0; i < segmentCount; i++)
                    AddRandomSegment(sink);
            else
            {
                double which = Random.NextDouble();
                if (which < 0.33)
                    sink.AddLines(RandomLines(segmentCount));
                else if (which < 0.67)
                    sink.AddQuadraticBeziers(RandomQuadraticBeziers(segmentCount));
                else
                    sink.AddBeziers(RandomBeziers(segmentCount));
            }
            sink.EndFigure(end);
        }

        private IEnumerable<Point2F> RandomLines(int segmentCount)
        {
            var lines = new List<Point2F>();
            for (int i = 0; i < segmentCount; i++)
                lines.Add(RandomNearPoint());
            return lines;
        }

        private IEnumerable<QuadraticBezierSegment> RandomQuadraticBeziers(int segmentCount)
        {
            var beziers = new List<QuadraticBezierSegment>();
            for (int i = 0; i < segmentCount; i++)
                beziers.Add(new QuadraticBezierSegment(
                    RandomNearPoint(),
                    RandomNearPoint()));
            return beziers;
        }

        private IEnumerable<BezierSegment> RandomBeziers(int segmentCount)
        {
            var beziers = new List<BezierSegment>();
            for (int i = 0; i < segmentCount; i++)
                beziers.Add(new BezierSegment(
                    RandomNearPoint(),
                    RandomNearPoint(),
                    RandomNearPoint()));
            return beziers;
        }

        private void AddRandomSegment(IGeometrySink sink)
        {
            double which = Random.NextDouble();
            if (which < 0.25)
                sink.AddLine(RandomNearPoint());
            else if (which < 0.5)
                sink.AddArc(RandomArc());
            else if (which < 0.75)
                sink.AddBezier(RandomBezier());
            else if (which < 1.0)
            sink.AddQuadraticBezier(RandomQuadraticBezier());
        }

        private QuadraticBezierSegment RandomQuadraticBezier()
        {
            return new QuadraticBezierSegment(
                RandomNearPoint(),
                RandomNearPoint());
        }

        private BezierSegment RandomBezier()
        {
            return new BezierSegment(
                RandomNearPoint(),
                RandomNearPoint(),
                RandomNearPoint());
        }

        private ArcSegment RandomArc()
        {
            return new ArcSegment(
                RandomNearPoint(),
                RandomSize(),
                (float)Random.NextDouble() * 360,
                CoinFlip ? SweepDirection.Clockwise : SweepDirection.Counterclockwise,
                CoinFlip ? ArcSize.Large : ArcSize.Small
                );
        }

        private SizeF RandomSize()
        {
            return new SizeF(
                (float)Random.NextDouble() * CanvasWidth,
                (float)Random.NextDouble() * CanvasHeight);
        }

        private RoundedRectangleGeometry RandomRoundRectGeometry()
        {
            return d2DFactory.CreateRoundedRectangleGeometry(
                RandomRoundedRect());
        }

        private RectangleGeometry RandomRectangleGeometry()
        {
            return d2DFactory.CreateRectangleGeometry(
                RandomRect(CanvasWidth, CanvasHeight));
        }

        private EllipseGeometry RandomEllipseGeometry()
        {
            return d2DFactory.CreateEllipseGeometry(
                RandomEllipse());
        }

        protected internal override void ChangeRenderTarget(RenderTarget newRenderTarget)
        {
            PenBrush = CopyBrushToRenderTarget(PenBrush, newRenderTarget);
            FillBrush = CopyBrushToRenderTarget(FillBrush, newRenderTarget);
        }

        protected internal override void Draw(RenderTarget renderTarget)
        {
            Geometry g = geometry;
            if (FillBrush != null)
            {
                renderTarget.FillGeometry(g, FillBrush, null);
            }
            if (PenBrush != null)
            {
                if (StrokeStyle != null)
                {
                    renderTarget.DrawGeometry(g, PenBrush, StrokeWidth, StrokeStyle);
                }
                else
                {
                    renderTarget.DrawGeometry(g, PenBrush, StrokeWidth);
                }
            }
        }

        public override bool HitTest(Point2F point)
        {
            return geometry.FillContainsPoint(point, FlatteningTolerance);
        }

        public override void Dispose()
        {
            if (geometry != null)
                geometry.Dispose();
            geometry = null;
            base.Dispose();
        }
    }
}
