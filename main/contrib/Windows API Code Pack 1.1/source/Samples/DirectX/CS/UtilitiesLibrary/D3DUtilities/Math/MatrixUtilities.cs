// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Windows.Media.Media3D;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D;


namespace Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities
{
    public static class MatrixExtensions
    {
        public static Matrix4x4F ToMatrix4x4F( this Matrix3D source )
        {
            return new Matrix4x4F(
                (float)source.M11,
                (float)source.M12,
                (float)source.M13,
                (float)source.M14,

                (float)source.M21,
                (float)source.M22,
                (float)source.M23,
                (float)source.M24,

                (float)source.M31,
                (float)source.M32,
                (float)source.M33,
                (float)source.M34,

                (float)source.OffsetX,
                (float)source.OffsetY,
                (float)source.OffsetZ,
                (float)source.M44 );
        }

        public static Matrix3D ToMatrix3D( this Matrix4x4F source )
        {
            Matrix3D destination = new Matrix3D( );
            destination.M11 = (float)source.M11;
            destination.M12 = (float)source.M12;
            destination.M13 = (float)source.M13;
            destination.M14 = (float)source.M14;

            destination.M21 = (float)source.M21;
            destination.M22 = (float)source.M22;
            destination.M23 = (float)source.M23;
            destination.M24 = (float)source.M24;

            destination.M31 = (float)source.M31;
            destination.M32 = (float)source.M32;
            destination.M33 = (float)source.M33;
            destination.M34 = (float)source.M34;

            destination.OffsetX = (float)source.M41;
            destination.OffsetY = (float)source.M42;
            destination.OffsetZ = (float)source.M43;
            destination.M44 = (float)source.M44;

            return destination;
        }

        #region PerspectiveCamera extensions
        /// <summary>
        /// Returns the world*perspective matrix for the camera
        /// </summary>
        /// <param name="camera">The WPF camera</param>
        /// <param name="aspectRatio">The aspect ratio of the device surface</param>
        /// <returns></returns>
        public static Matrix3D ToMatrix3DLH( this PerspectiveCamera camera, double aspectRatio )
        {
            Transform3DGroup tg = new Transform3DGroup( );
            tg.Children.Add(new MatrixTransform3D(GetLookAtMatrixLH(camera)));
            tg.Children.Add(camera.Transform);
            tg.Children.Add(new MatrixTransform3D(GetPerspectiveMatrixLH(camera, aspectRatio)));
            return tg.Value;
        }

        public static Matrix3D ToViewLH( this PerspectiveCamera camera )
        {
            return GetLookAtMatrixLH( camera );
        }

        public static Matrix3D ToPerspectiveLH( this PerspectiveCamera camera, double aspectRatio )
        {
            return GetPerspectiveMatrixLH( camera, aspectRatio );
        }
        #endregion

        #region PerspectiveCamera implementation
        internal static Matrix3D GetPerspectiveMatrixLH( PerspectiveCamera camera, double aspectRatio )
        {
            double fov = (camera.FieldOfView / 360.0) * 2.0 * Math.PI;
            double zn = camera.NearPlaneDistance;
            double zf = camera.FarPlaneDistance;
            double f = 1.0 / Math.Tan( fov / 2.0 );
            double xScale = f / aspectRatio;
            double yScale = f;
            double n = (1.0 / (zf - zn));
            double m33 = zf * n;
            double m43 = -zf * zn * n;

            return
                new Matrix3D(
                    xScale, 0, 0, 0,
                    0, yScale, 0, 0,
                    0, 0, m33, 1,
                    0, 0, m43, 0 );
        }

        internal static Matrix3D GetLookAtMatrixLH( PerspectiveCamera camera )
        {
            Vector3D f = new Vector3D(
                    camera.Position.X - camera.LookDirection.X,
                    camera.Position.Y - camera.LookDirection.Y,
                    camera.Position.Z - camera.LookDirection.Z );
            f.Normalize();
            Vector3D vUpActual = camera.UpDirection;
            vUpActual.Normalize();
            Vector3D s = Vector3D.CrossProduct( f, vUpActual );
            Vector3D u = Vector3D.CrossProduct( s, f );

            return
                new Matrix3D(
                    s.X, u.X, -f.X, 0,
                    s.Y, u.Y, -f.Y, 0,
                    s.Z, u.Z, -f.Z, 0,
                    -camera.Position.X, -camera.Position.Y, -camera.Position.Z, 1 );
        }
        #endregion
    }
}
