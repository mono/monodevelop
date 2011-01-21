// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Runtime.InteropServices;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D;

namespace SciFiTextDemo
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SimpleVertex
    {
        [MarshalAs(UnmanagedType.Struct)]
        public Vector3F Pos;
        [MarshalAs(UnmanagedType.Struct)]
        public Vector2F Tex;
    }


    public class VertexData
    {
        public Vertices VerticesInstance = new Vertices();
        public Indices IndicesInstance = new Indices();

        [StructLayout(LayoutKind.Sequential)]
        public class Vertices
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            private SimpleVertex[] vertices = 
            {
               new SimpleVertex() { Pos = new Vector3F ( -1.0f, -1.0f, 16.0f ), Tex = new Vector2F( 0.0f, 0.0f )}, // upper-left
               new SimpleVertex() { Pos = new Vector3F (  1.0f, -1.0f, 16.0f ), Tex = new Vector2F( 1.0f, 0.0f )}, // upper-right
               new SimpleVertex() { Pos = new Vector3F (  1.0f, -1.0f, 0.0f ), Tex = new Vector2F( 1.0f, 1.0f )}, // lower-right
               new SimpleVertex() { Pos = new Vector3F ( -1.0f, -1.0f, 0.0f ), Tex = new Vector2F( 0.0f, 1.0f )} // lower-left
            };
        }

        [StructLayout(LayoutKind.Sequential)]
        public class Indices
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            private ushort[] indices = 
            {
                3,1,0,
                2,1,3,
            };
        }
    }
}
