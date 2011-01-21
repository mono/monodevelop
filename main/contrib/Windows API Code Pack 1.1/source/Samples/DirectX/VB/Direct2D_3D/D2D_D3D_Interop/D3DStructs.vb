' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Runtime.InteropServices
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D

Namespace Microsoft.WindowsAPICodePack.DirectX.Samples
	#Region "SimpleVertex"
	<StructLayout(LayoutKind.Sequential)> _
	Public Structure SimpleVertex
		<MarshalAs(UnmanagedType.Struct)> _
		Public Pos As Vector3F
		<MarshalAs(UnmanagedType.Struct)> _
		Public Tex As Vector2F
	End Structure
	#End Region

	#Region "Vertex Array"
	Public Class VertexData
		Public s_VertexArray As New VertexArray()
		Public s_FacesIndexArray As New FacesIndexArray()

		<StructLayout(LayoutKind.Sequential)> _
		Public Class VertexArray
			<MarshalAs(UnmanagedType.ByValArray, SizeConst := 24)> _
			Private vertices() As SimpleVertex = { New SimpleVertex() With {.Pos = New Vector3F (1.0f, 1.0f, -1.0f), .Tex = New Vector2F (0.0f, 0.0f)}, New SimpleVertex() With {.Pos = New Vector3F (-1.0f, 1.0f, -1.0f), .Tex = New Vector2F (1.0f, 0.0f)}, New SimpleVertex() With {.Pos = New Vector3F (-1.0f, -1.0f, -1.0f), .Tex = New Vector2F (1.0f, 1.0f)}, New SimpleVertex() With {.Pos = New Vector3F (1.0f, -1.0f, -1.0f), .Tex = New Vector2F (0.0f, 1.0f)}, New SimpleVertex() With {.Pos = New Vector3F (1.0f, 1.0f, 1.0f), .Tex = New Vector2F (0.0f, 0.0f)}, New SimpleVertex() With {.Pos = New Vector3F (-1.0f, 1.0f, 1.0f), .Tex = New Vector2F (1.0f, 0.0f)}, New SimpleVertex() With {.Pos = New Vector3F (-1.0f, -1.0f, 1.0f), .Tex = New Vector2F (1.0f, 1.0f)}, New SimpleVertex() With {.Pos = New Vector3F (1.0f, -1.0f, 1.0f), .Tex = New Vector2F (0.0f, 1.0f)}, New SimpleVertex() With {.Pos = New Vector3F (-1.0f, 1.0f, -1.0f), .Tex = New Vector2F (0.0f, 0.0f)}, New SimpleVertex() With {.Pos = New Vector3F (-1.0f, 1.0f, 1.0f), .Tex = New Vector2F (1.0f, 0.0f)}, New SimpleVertex() With {.Pos = New Vector3F (-1.0f, -1.0f, 1.0f), .Tex = New Vector2F (1.0f, 1.0f)}, New SimpleVertex() With {.Pos = New Vector3F (-1.0f, -1.0f, -1.0f), .Tex = New Vector2F (0.0f, 1.0f)}, New SimpleVertex() With {.Pos = New Vector3F (1.0f, 1.0f, -1.0f), .Tex = New Vector2F (0.0f, 0.0f)}, New SimpleVertex() With {.Pos = New Vector3F (1.0f, 1.0f, 1.0f), .Tex = New Vector2F (1.0f, 0.0f)}, New SimpleVertex() With {.Pos = New Vector3F (1.0f, -1.0f, 1.0f), .Tex = New Vector2F (1.0f, 1.0f)}, New SimpleVertex() With {.Pos = New Vector3F (1.0f, -1.0f, -1.0f), .Tex = New Vector2F (0.0f, 1.0f)}, New SimpleVertex() With {.Pos = New Vector3F (1.0f, 1.0f, 1.0f), .Tex = New Vector2F (0.0f, 0.0f)}, New SimpleVertex() With {.Pos = New Vector3F (-1.0f, 1.0f, 1.0f), .Tex = New Vector2F (1.0f, 0.0f)}, New SimpleVertex() With {.Pos = New Vector3F (-1.0f, 1.0f, -1.0f), .Tex = New Vector2F (1.0f, 1.0f)}, New SimpleVertex() With {.Pos = New Vector3F (1.0f, 1.0f, -1.0f), .Tex = New Vector2F (0.0f, 1.0f)}, New SimpleVertex() With {.Pos = New Vector3F (1.0f, -1.0f, 1.0f), .Tex = New Vector2F (0.0f, 0.0f)}, New SimpleVertex() With {.Pos = New Vector3F (-1.0f, -1.0f, 1.0f), .Tex = New Vector2F (1.0f, 0.0f)}, New SimpleVertex() With {.Pos = New Vector3F (-1.0f, -1.0f, -1.0f), .Tex = New Vector2F (1.0f, 1.0f)}, New SimpleVertex() With {.Pos = New Vector3F (1.0f, -1.0f, -1.0f), .Tex = New Vector2F (0.0f, 1.0f)} }
		End Class

		<StructLayout(LayoutKind.Sequential)> _
		Public Class FacesIndexArray
			<MarshalAs(UnmanagedType.ByValArray, SizeConst := 36)> _
			Private indices() As UShort = { 3,1,0, 2,1,3, 6,4,5, 7,4,6, 11,9,8, 10,9,11, 14,12,13, 15,12,14, 19,17,16, 18,17,19, 22,20,21, 23,20,22 }

			Public ReadOnly Property Length() As UInteger
				Get
					Return CUInt(indices.Length)
				End Get
			End Property
		End Class
	End Class
	#End Region
End Namespace
