// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;

namespace D2DPaint
{
    internal class GeometryShape : DrawingShape
    {        
        internal List<Point2F> _points;
        internal float _strokeWidth;
        internal int _selectedBrushIndex;
        internal Geometry _geometry;

        internal GeometryShape(Paint2DForm parent, Point2F point0, float strokeWidth, int selectedBrush, bool fill)
            : base(parent)
        {
            _points = new List<Point2F> { point0 };
            _strokeWidth = strokeWidth;
            _selectedBrushIndex = selectedBrush;
            _fill = fill;
        }

        protected internal override void Draw(RenderTarget renderTarget)
        {
            if (_geometry != null)
            {
                if (_fill)
                    renderTarget.FillGeometry(_geometry, _parent.brushes[_selectedBrushIndex]);
                else
                    renderTarget.DrawGeometry(_geometry, _parent.brushes[_selectedBrushIndex], _strokeWidth,
                        _parent.d2dFactory.CreateStrokeStyle(
                        new StrokeStyleProperties(
                            CapStyle.Round,
                            CapStyle.Round,
                            CapStyle.Round,
                            LineJoin.Round,
                            1,
                            DashStyle.Solid,
                            0)));
            }
        }

        protected internal override Point2F EndPoint
        {
            set
            {
                _points.Add(value);
                PathGeometry g = _parent.d2dFactory.CreatePathGeometry();
                var sink = g.Open();
                sink.BeginFigure(_points[0], _fill ? FigureBegin.Filled : FigureBegin.Hollow);
                for ( int i = 1; i < _points.Count; i++)
                {
                    //smoothing
                    if (i > 1 && i < _points.Count - 1)
                    {
                        Point2F cp1;
                        Point2F cp2;
                        GetSmoothingPoints(i, out cp1, out cp2);
                        sink.AddBezier(
                            new BezierSegment(
                                cp1,
                                cp2,
                                _points[i]));
                    }
                    else
                    {
                        sink.AddLine(_points[i]);
                    }
                }
                sink.EndFigure(_fill ? FigureEnd.Closed : FigureEnd.Open);
                sink.Close();
                _geometry = g;
            }
        }

        private void GetSmoothingPoints(int i, out Point2F cp1, out Point2F cp2)
        {
            float smoothing = .25f; //0 - no smoothing
            float lx = _points[i].X - _points[i - 1].X;
            float ly = _points[i].Y - _points[i - 1].Y;
            float l = (float)Math.Sqrt(lx * lx + ly * ly); // distance from previous point
            float l1x = _points[i].X - _points[i - 2].X;
            float l1y = _points[i].Y - _points[i - 2].Y;
            float l1 = (float)Math.Sqrt(l1x * l1x + l1y * l1y); // distance between two points back and current point
            float l2x = _points[i + 1].X - _points[i - 1].X;
            float l2y = _points[i + 1].Y - _points[i - 1].Y;
            float l2 = (float)Math.Sqrt(l2x * l2x + l2y * l2y); //distance between previous point and the next point

            cp1 = new Point2F(
                _points[i - 1].X + (l1x == 0 ? 0 : (smoothing * l * l1x / l1)),
                _points[i - 1].Y + (l1y == 0 ? 0 : (smoothing * l * l1y / l1))
                );
            cp2 = new Point2F(
                _points[i].X - (l2x == 0 ? 0 : (smoothing * l * l2x / l2)),
                _points[i].Y - (l2y == 0 ? 0 : (smoothing * l * l2y / l2))
                );
        }
    }
}