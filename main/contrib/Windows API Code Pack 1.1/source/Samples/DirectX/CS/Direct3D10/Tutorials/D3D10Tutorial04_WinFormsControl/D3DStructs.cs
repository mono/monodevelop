// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Runtime.InteropServices;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D;

namespace D3D10Tutorial04_WinFormsControl
{
    #region SimpleVertex
    [StructLayout(LayoutKind.Sequential)]
    public struct SimpleVertex
    {
        [MarshalAs(UnmanagedType.Struct)]
        public Vector3F Pos;
        [MarshalAs(UnmanagedType.Struct)]
        public Vector4F Color;
    } 
    #endregion

    #region Cube
    public class Cube
    {
        public CubeVertices Vertices = new CubeVertices();
        public CubeIndices Indices = new CubeIndices();

        [StructLayout(LayoutKind.Sequential)]
        public class CubeVertices
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            private SimpleVertex[] vertices = 
            {
                new SimpleVertex() { Pos = new Vector3F ( -1.0f, 1.0f, -1.0f ), Color = new Vector4F ( 0.0f, 1.0f, 0.0f, 1.0f ) },
                new SimpleVertex() { Pos = new Vector3F ( 1.0f, 1.0f, -1.0f ), Color = new Vector4F ( 0.0f, 1.0f, 0.0f, 1.0f ) },
                new SimpleVertex() { Pos = new Vector3F ( 1.0f, 1.0f, 1.0f ), Color = new Vector4F ( 0.0f, 1.0f, 1.0f, 1.0f ) },
                new SimpleVertex() { Pos = new Vector3F ( -1.0f, 1.0f, 1.0f ), Color = new Vector4F ( 1.0f, 0.0f, 0.0f, 1.0f ) },

                new SimpleVertex() { Pos = new Vector3F ( -1.0f, -1.0f, -1.0f ), Color = new Vector4F ( 1.0f, 0.0f, 1.0f, 1.0f ) },
                new SimpleVertex() { Pos = new Vector3F ( 1.0f, -1.0f, -1.0f ), Color = new Vector4F ( 1.0f, 1.0f, 0.0f, 1.0f ) },
                new SimpleVertex() { Pos = new Vector3F ( 1.0f, -1.0f, 1.0f ), Color = new Vector4F ( 1.0f, 1.0f, 1.0f, 1.0f ) },
                new SimpleVertex() { Pos = new Vector3F ( -1.0f, -1.0f, 1.0f ), Color = new Vector4F ( 0.0f, 0.0f, 0.0f, 1.0f ) },
            };
        }

        [StructLayout(LayoutKind.Sequential)]
        public class CubeIndices
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 36)]
            private uint[] indices = 
            {
                3,1,0,
                2,1,3,

                0,5,4,
                1,5,0,

                3,4,7,
                0,4,3,

                1,6,5,
                2,6,1,

                2,7,6,
                3,7,2,

                6,4,5,
                7,4,6
            };
        }
    }
    #endregion
}
