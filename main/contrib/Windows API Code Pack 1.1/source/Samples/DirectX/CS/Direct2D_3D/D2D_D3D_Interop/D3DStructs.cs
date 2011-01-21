// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Runtime.InteropServices;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D;

namespace Microsoft.WindowsAPICodePack.DirectX.Samples
{
    #region SimpleVertex
    [StructLayout(LayoutKind.Sequential)]
    public struct SimpleVertex
    {
        [MarshalAs(UnmanagedType.Struct)]
        public Vector3F Pos;
        [MarshalAs(UnmanagedType.Struct)]
        public Vector2F Tex;
    } 
    #endregion

    #region Vertex Array
    public class VertexData
    {
        public VertexArray s_VertexArray = new VertexArray();
        public FacesIndexArray s_FacesIndexArray = new FacesIndexArray();

        [StructLayout(LayoutKind.Sequential)]
        public class VertexArray
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
            private SimpleVertex[] vertices = 
            {
                new SimpleVertex() { Pos = new Vector3F ( 1.0f, 1.0f, -1.0f ), Tex = new Vector2F ( 0.0f, 0.0f ) },
                new SimpleVertex() { Pos = new Vector3F ( -1.0f, 1.0f, -1.0f ), Tex = new Vector2F ( 1.0f, 0.0f ) },
                new SimpleVertex() { Pos = new Vector3F ( -1.0f, -1.0f, -1.0f ), Tex = new Vector2F ( 1.0f, 1.0f ) },
                new SimpleVertex() { Pos = new Vector3F ( 1.0f, -1.0f, -1.0f ), Tex = new Vector2F ( 0.0f, 1.0f ) },

                new SimpleVertex() { Pos = new Vector3F ( 1.0f, 1.0f, 1.0f ), Tex = new Vector2F ( 0.0f, 0.0f ) },
                new SimpleVertex() { Pos = new Vector3F ( -1.0f, 1.0f, 1.0f ), Tex = new Vector2F ( 1.0f, 0.0f ) },
                new SimpleVertex() { Pos = new Vector3F ( -1.0f, -1.0f, 1.0f ), Tex = new Vector2F ( 1.0f, 1.0f ) },
                new SimpleVertex() { Pos = new Vector3F ( 1.0f, -1.0f, 1.0f ), Tex = new Vector2F ( 0.0f, 1.0f ) },

                new SimpleVertex() { Pos = new Vector3F ( -1.0f, 1.0f, -1.0f ), Tex = new Vector2F ( 0.0f, 0.0f ) },
                new SimpleVertex() { Pos = new Vector3F ( -1.0f, 1.0f, 1.0f ), Tex = new Vector2F ( 1.0f, 0.0f ) },
                new SimpleVertex() { Pos = new Vector3F ( -1.0f, -1.0f, 1.0f ), Tex = new Vector2F ( 1.0f, 1.0f ) },
                new SimpleVertex() { Pos = new Vector3F ( -1.0f, -1.0f, -1.0f ), Tex = new Vector2F ( 0.0f, 1.0f ) },

                new SimpleVertex() { Pos = new Vector3F ( 1.0f, 1.0f, -1.0f ), Tex = new Vector2F ( 0.0f, 0.0f ) },
                new SimpleVertex() { Pos = new Vector3F ( 1.0f, 1.0f, 1.0f ), Tex = new Vector2F ( 1.0f, 0.0f ) },
                new SimpleVertex() { Pos = new Vector3F ( 1.0f, -1.0f, 1.0f ), Tex = new Vector2F ( 1.0f, 1.0f ) },
                new SimpleVertex() { Pos = new Vector3F ( 1.0f, -1.0f, -1.0f ), Tex = new Vector2F ( 0.0f, 1.0f ) },

                new SimpleVertex() { Pos = new Vector3F ( 1.0f, 1.0f, 1.0f ), Tex = new Vector2F ( 0.0f, 0.0f ) },
                new SimpleVertex() { Pos = new Vector3F ( -1.0f, 1.0f, 1.0f ), Tex = new Vector2F ( 1.0f, 0.0f ) },
                new SimpleVertex() { Pos = new Vector3F ( -1.0f, 1.0f, -1.0f ), Tex = new Vector2F ( 1.0f, 1.0f ) },
                new SimpleVertex() { Pos = new Vector3F ( 1.0f, 1.0f, -1.0f ), Tex = new Vector2F ( 0.0f, 1.0f ) },

                new SimpleVertex() { Pos = new Vector3F ( 1.0f, -1.0f, 1.0f ), Tex = new Vector2F ( 0.0f, 0.0f ) },
                new SimpleVertex() { Pos = new Vector3F ( -1.0f, -1.0f, 1.0f ), Tex = new Vector2F ( 1.0f, 0.0f ) },
                new SimpleVertex() { Pos = new Vector3F ( -1.0f, -1.0f, -1.0f ), Tex = new Vector2F ( 1.0f, 1.0f ) },
                new SimpleVertex() { Pos = new Vector3F ( 1.0f, -1.0f, -1.0f ), Tex = new Vector2F ( 0.0f, 1.0f ) },
            };
        }

        [StructLayout(LayoutKind.Sequential)]
        public class FacesIndexArray
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 36)]
            private ushort[] indices = 
            {
                3,1,0,
                2,1,3,

                6,4,5,
                7,4,6,

                11,9,8,
                10,9,11,

                14,12,13,
                15,12,14,

                19,17,16,
                18,17,19,

                22,20,21,
                23,20,22
            };

            public uint Length
            {
                get { return (uint)indices.Length; }
            }
        }
    }
    #endregion
}
