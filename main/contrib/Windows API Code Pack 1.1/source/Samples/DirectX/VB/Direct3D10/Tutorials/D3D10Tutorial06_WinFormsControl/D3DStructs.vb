' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Runtime.InteropServices
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D

Namespace D3D10Tutorial06_WinFormsControl
	#Region "SimpleVertex"
	<StructLayout(LayoutKind.Sequential)> _
	Public Structure SimpleVertex
		<MarshalAs(UnmanagedType.Struct)> _
		Public Pos As Vector3F
		<MarshalAs(UnmanagedType.Struct)> _
		Public Normal As Vector3F
	End Structure
	#End Region

	#Region "Cube"
	Public Class Cube
		Public Vertices As New CubeVertices()
		Public Indices As New CubeIndices()

		<StructLayout(LayoutKind.Sequential)> _
		Public Class CubeVertices
			<MarshalAs(UnmanagedType.ByValArray, SizeConst := 24)> _
			Private vertices() As SimpleVertex = { New SimpleVertex() With {.Pos = New Vector3F (-1.0f, 1.0f, -1.0f), .Normal = New Vector3F (0.0f, 1.0f, 0.0f)}, New SimpleVertex() With {.Pos = New Vector3F (1.0f, 1.0f, -1.0f), .Normal = New Vector3F (0.0f, 1.0f, 0.0f)}, New SimpleVertex() With {.Pos = New Vector3F (1.0f, 1.0f, 1.0f), .Normal = New Vector3F (0.0f, 1.0f, 0.0f)}, New SimpleVertex() With {.Pos = New Vector3F (-1.0f, 1.0f, 1.0f), .Normal = New Vector3F (0.0f, 1.0f, 0.0f)}, New SimpleVertex() With {.Pos = New Vector3F (-1.0f, -1.0f, -1.0f), .Normal = New Vector3F (0.0f, -1.0f, 0.0f)}, New SimpleVertex() With {.Pos = New Vector3F (1.0f, -1.0f, -1.0f), .Normal = New Vector3F (0.0f, -1.0f, 0.0f)}, New SimpleVertex() With {.Pos = New Vector3F (1.0f, -1.0f, 1.0f), .Normal = New Vector3F (0.0f, -1.0f, 0.0f)}, New SimpleVertex() With {.Pos = New Vector3F (-1.0f, -1.0f, 1.0f), .Normal = New Vector3F (0.0f, -1.0f, 0.0f)}, New SimpleVertex() With {.Pos = New Vector3F (-1.0f, -1.0f, 1.0f), .Normal = New Vector3F (-1.0f, 0.0f, 0.0f)}, New SimpleVertex() With {.Pos = New Vector3F (-1.0f, -1.0f, -1.0f), .Normal = New Vector3F (-1.0f, 0.0f, 0.0f)}, New SimpleVertex() With {.Pos = New Vector3F (-1.0f, 1.0f, -1.0f), .Normal = New Vector3F (-1.0f, 0.0f, 0.0f)}, New SimpleVertex() With {.Pos = New Vector3F (-1.0f, 1.0f, 1.0f), .Normal = New Vector3F (-1.0f, 0.0f, 0.0f)}, New SimpleVertex() With {.Pos = New Vector3F (1.0f, -1.0f, 1.0f), .Normal = New Vector3F (1.0f, 0.0f, 0.0f)}, New SimpleVertex() With {.Pos = New Vector3F (1.0f, -1.0f, -1.0f), .Normal = New Vector3F (1.0f, 0.0f, 0.0f)}, New SimpleVertex() With {.Pos = New Vector3F (1.0f, 1.0f, -1.0f), .Normal = New Vector3F (1.0f, 0.0f, 0.0f)}, New SimpleVertex() With {.Pos = New Vector3F (1.0f, 1.0f, 1.0f), .Normal = New Vector3F (1.0f, 0.0f, 0.0f)}, New SimpleVertex() With {.Pos = New Vector3F (-1.0f, -1.0f, -1.0f), .Normal = New Vector3F (0.0f, 0.0f, -1.0f)}, New SimpleVertex() With {.Pos = New Vector3F (1.0f, -1.0f, -1.0f), .Normal = New Vector3F (0.0f, 0.0f, -1.0f)}, New SimpleVertex() With {.Pos = New Vector3F (1.0f, 1.0f, -1.0f), .Normal = New Vector3F (0.0f, 0.0f, -1.0f)}, New SimpleVertex() With {.Pos = New Vector3F (-1.0f, 1.0f, -1.0f), .Normal = New Vector3F (0.0f, 0.0f, -1.0f)}, New SimpleVertex() With {.Pos = New Vector3F (-1.0f, -1.0f, 1.0f), .Normal = New Vector3F (0.0f, 0.0f, 1.0f)}, New SimpleVertex() With {.Pos = New Vector3F (1.0f, -1.0f, 1.0f), .Normal = New Vector3F (0.0f, 0.0f, 1.0f)}, New SimpleVertex() With {.Pos = New Vector3F (1.0f, 1.0f, 1.0f), .Normal = New Vector3F (0.0f, 0.0f, 1.0f)}, New SimpleVertex() With {.Pos = New Vector3F (-1.0f, 1.0f, 1.0f), .Normal = New Vector3F (0.0f, 0.0f, 1.0f)} }
		End Class

		<StructLayout(LayoutKind.Sequential)> _
		Public Class CubeIndices
			<MarshalAs(UnmanagedType.ByValArray, SizeConst := 36)> _
			Private indices() As UInteger = { 3,1,0, 2,1,3, 6,4,5, 7,4,6, 11,9,8, 10,9,11, 14,12,13, 15,12,14, 19,17,16, 18,17,19, 22,20,21, 23,20,22 }
		End Class
	End Class
	#End Region
End Namespace
