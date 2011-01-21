' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Runtime.InteropServices
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D

Namespace SciFiTextDemo
	<StructLayout(LayoutKind.Sequential)> _
	Public Structure SimpleVertex
		<MarshalAs(UnmanagedType.Struct)> _
		Public Pos As Vector3F
		<MarshalAs(UnmanagedType.Struct)> _
		Public Tex As Vector2F
	End Structure


	Public Class VertexData
		Public VerticesInstance As New Vertices()
		Public IndicesInstance As New Indices()

		<StructLayout(LayoutKind.Sequential)> _
		Public Class Vertices
			<MarshalAs(UnmanagedType.ByValArray, SizeConst := 4)> _
			Private vertices() As SimpleVertex = { New SimpleVertex() With {.Pos = New Vector3F (-1.0f, -1.0f, 16.0f), .Tex = New Vector2F(0.0f, 0.0f)}, New SimpleVertex() With {.Pos = New Vector3F (1.0f, -1.0f, 16.0f), .Tex = New Vector2F(1.0f, 0.0f)}, New SimpleVertex() With {.Pos = New Vector3F (1.0f, -1.0f, 0.0f), .Tex = New Vector2F(1.0f, 1.0f)}, New SimpleVertex() With {.Pos = New Vector3F (-1.0f, -1.0f, 0.0f), .Tex = New Vector2F(0.0f, 1.0f)} }
		End Class

		<StructLayout(LayoutKind.Sequential)> _
		Public Class Indices
			<MarshalAs(UnmanagedType.ByValArray, SizeConst := 6)> _
			Private indices() As UShort = { 3,1,0, 2,1,3 }
		End Class
	End Class
End Namespace
