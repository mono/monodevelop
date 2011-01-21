// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D;

namespace Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities
{
    public static class Camera
    {
        public static Matrix4x4F MatrixLookAtLH(Vector3F eye, Vector3F at, Vector3F up)
        {
            Vector3F right, vec;
            
            vec = at - eye;
            vec.NormalizeInPlace();
            right = Vector3F.Cross(up, vec);
            up = Vector3F.Cross(vec, right);
            right.NormalizeInPlace();
            up.NormalizeInPlace();
            return new Matrix4x4F(
                right.X,
                up.X,
                vec.X,
                0.0f,

                right.Y,
                up.Y,
                vec.Y,
                0.0f,

                right.Z,
                up.Z,
                vec.Z,
                0.0f,

                -Vector3F.Dot(right, eye),
                -Vector3F.Dot(up, eye),
                -Vector3F.Dot(vec, eye),
                1.0f
            );
        }

        public static Matrix4x4F MatrixPerspectiveFovLH(float fovy, float aspect, float zn, float zf)
        {
            Matrix4x4F ret = new Matrix4x4F();
            ret.M11 = 1.0f / (aspect * (float)Math.Tan(fovy / 2));
            ret.M22 = 1.0f / (float)Math.Tan(fovy / 2);
            ret.M33 = zf / (zf - zn);
            ret.M34 = 1;
            ret.M43 = (zf * zn) / (zn - zf);
            ret.M44 = 0;
            return ret;
        }
    }
}
