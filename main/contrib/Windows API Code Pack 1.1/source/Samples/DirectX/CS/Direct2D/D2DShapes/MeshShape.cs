// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;

namespace D2DShapes
{
    internal class MeshShape : DrawingShape
    {
        private Mesh mesh;
        private List<Triangle> triangles;
        private GeometryShape geometry;

        internal override D2DBitmap Bitmap
        {
            get
            {
                return base.Bitmap;
            }
            set
            {
                base.Bitmap = value;
                if (geometry != null)
                    geometry.Bitmap = value;
            }
        }

        public MeshShape(RenderTarget initialRenderTarget, Random random, D2DFactory d2DFactory, D2DBitmap bitmap)
            : base(initialRenderTarget, random, d2DFactory, bitmap)
        {
            FillBrush = RandomBrush();

            mesh = CoinFlip ? MeshFromRandomGeometry() : MeshFromRandomTriangles();
        }

        [TypeConverter(typeof(ExpandableObjectConverter))]
        public Mesh Mesh
        {
            get { return mesh; }
            set { mesh = value; }
        }

        private Mesh MeshFromRandomTriangles()
        {
            Mesh m = RenderTarget.CreateMesh();
            using (TessellationSink sink = m.Open())
            {
                int count = Random.Next(2, 20);
                triangles = new List<Triangle>();
                CreateRandomTriangles(count);

                sink.AddTriangles(triangles);
                sink.Close();
            }
            return m;
        }

        private void CreateRandomTriangles(int count)
        {
            var which = Random.NextDouble();
            if (which < 0.33) //random triangles
            {
                for (int i = 0; i < count; i++)
                {
                    triangles.Add(new Triangle(RandomNearPoint(), RandomNearPoint(), RandomNearPoint()));
                }
            }
            else if (which < 0.67) //fan of triangles
            {
                Point2F p1, p2, p3;
                p1 = RandomPoint();
                p3 = RandomNearPoint();
                for (int i = 0; i < count; i++)
                {
                    p2 = p3;
                    p3 = RandomNearPoint();
                    triangles.Add(new Triangle(p1, p2, p3));
                }
            }
            else //triangle strip
            {
                Point2F p1, p2, p3;
                p2 = RandomPoint();
                p3 = RandomNearPoint();
                for (int i = 0; i < count; i++)
                {
                    p1 = p2;
                    p2 = p3;
                    p3 = RandomNearPoint();
                    triangles.Add(new Triangle(p1, p2, p3));
                }
            }
        }

        private Mesh MeshFromRandomGeometry()
        {
            if (geometry != null)
                geometry.Dispose();
            geometry = new GeometryShape(RenderTarget, Random, d2DFactory, Bitmap);
            Mesh m = RenderTarget.CreateMesh();
            TessellationSink sink = m.Open();
            if (geometry.worldTransform.HasValue)
                geometry.Geometry.Tessellate(sink, FlatteningTolerance, geometry.worldTransform.Value);
            else
                geometry.Geometry.Tessellate(sink, FlatteningTolerance);
            sink.Close();
            sink.Dispose();
            return m;
        }

        protected internal override void ChangeRenderTarget(RenderTarget newRenderTarget)
        {
            TessellationSink sink;
            mesh.Dispose();
            mesh = newRenderTarget.CreateMesh();
            if (geometry != null)
            {
                geometry.RenderTarget = newRenderTarget;
                sink = mesh.Open();
                if (geometry.worldTransform.HasValue)
                    geometry.Geometry.Tessellate(sink, FlatteningTolerance, geometry.worldTransform.Value);
                else
                    geometry.Geometry.Tessellate(sink, FlatteningTolerance);
            }
            else
            {
                sink = mesh.Open();
                sink.AddTriangles(triangles);
            }
            sink.Close();
            sink.Dispose();
            FillBrush = CopyBrushToRenderTarget(FillBrush, newRenderTarget);
        }

        protected internal override void Draw(RenderTarget renderTarget)
        {
            DrawingStateBlock stateBlock = d2DFactory.CreateDrawingStateBlock();
            renderTarget.SaveDrawingState(stateBlock);
            //AntialiasMode push = RenderTarget.AntialiasMode;
            renderTarget.AntiAliasMode = AntiAliasMode.Aliased;
            renderTarget.FillMesh(mesh, FillBrush);
            //RenderTarget.AntialiasMode = push;
            renderTarget.RestoreDrawingState(stateBlock);
            stateBlock.Dispose();
        }

        public override bool HitTest(Point2F point)
        {
            if (geometry != null)
            {
                return geometry.HitTest(point);
            }
            if (triangles != null)
            {
                foreach (var triangle in triangles)
                {
                    if (IsPointInTriangle(triangle, point))
                        return true;
                }
            }
            return false;
        }

        private static bool IsPointInTriangle(Triangle triangle, Point2F point)
        {
            //no time to implement the proper algorithm, so let's just use a bounding rectangle...
            float left = Math.Min(triangle.Point1.X, Math.Min(triangle.Point2.X, triangle.Point3.X));
            float right = Math.Max(triangle.Point1.X, Math.Max(triangle.Point2.X, triangle.Point3.X));
            float top = Math.Min(triangle.Point1.Y, Math.Min(triangle.Point2.Y, triangle.Point3.Y));
            float bottom = Math.Max(triangle.Point1.Y, Math.Max(triangle.Point2.Y, triangle.Point3.Y));
            return point.X >= left &&
                point.X <= right &&
                point.Y >= top &&
                point.Y <= bottom;
        }

        public override void Dispose()
        {
            if (geometry != null)
                geometry.Dispose();
            geometry = null;
            if (mesh != null)
                mesh.Dispose();
            mesh = null;
            base.Dispose();
        }
    }
}
