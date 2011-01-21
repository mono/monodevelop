// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Runtime.InteropServices;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D;

namespace Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities
{
    [StructLayout(LayoutKind.Sequential)]
    public class MatrixMath
    {
        #region operator *

        public static Vector4F VectorMultiply(Matrix4x4F a, Vector4F b)
        {
            return new Vector4F(
                a.M11 * b.X + a.M21 * b.Y + a.M31 * b.Z + a.M41 * b.W,
                a.M12 * b.X + a.M22 * b.Y + a.M32 * b.Z + a.M42 * b.W,
                a.M13 * b.X + a.M23 * b.Y + a.M33 * b.Z + a.M43 * b.W,
                a.M14 * b.X + a.M24 * b.Y + a.M34 * b.Z + a.M44 * b.W
                );
        }
        #endregion

        #region MatrixScale
        public static Matrix4x4F MatrixScale(float x, float y, float z)
        {
            return new Matrix4x4F(
                x, 0, 0, 0,
                0, y, 0, 0,
                0, 0, z, 0,
                0, 0, 0, 1
                );
        } 
        #endregion

        #region MatrixTranslate
        public static Matrix4x4F MatrixTranslate(float x, float y, float z)
        {
            return new Matrix4x4F(
                1, 0, 0, 0,
                0, 1, 0, 0,
                0, 0, 1, 0,
                x, y, z, 1
                );
        }
        #endregion

        #region MatrixRotationX
        public static Matrix4x4F MatrixRotationX(float angle)
        {
            float sin = (float)Math.Sin(angle);
            float cos = (float)Math.Cos(angle);
            return new Matrix4x4F(
                1, 0, 0, 0,
                0, cos, sin, 0,
                0, -sin, cos, 0,
                0, 0, 0, 1
                );
        }
        #endregion

        #region MatrixRotationY
        public static Matrix4x4F MatrixRotationY(float angle)
        {
            float sin = (float)Math.Sin(angle);
            float cos = (float)Math.Cos(angle);
            return new Matrix4x4F(
                cos, 0, -sin, 0,
                0, 1, 0, 0,
                sin, 0, cos, 0,
                0, 0, 0, 1
                );
        }
        #endregion

        #region MatrixRotationZ
        public static Matrix4x4F MatrixRotationZ(float angle)
        {
            float sin = (float)Math.Sin(angle);
            float cos = (float)Math.Cos(angle);
            return new Matrix4x4F(
                cos, sin, 0, 0,
                -sin, cos, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1
                );
        }
        #endregion
    }

}
