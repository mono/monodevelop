' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Runtime.InteropServices
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D

Namespace D3D10Tutorial05_WinFormsControl
	#Region "SimpleVertex"
	<StructLayout(LayoutKind.Sequential)> _
	Public Structure SimpleVertex
		<MarshalAs(UnmanagedType.Struct)> _
		Public Pos As Vector3F
		<MarshalAs(UnmanagedType.Struct)> _
		Public Color As Vector4F
	End Structure
	#End Region

	#Region "Cube"
	Public Class Cube
		Public Vertices As New CubeVertices()
		Public Indices As New CubeIndices()

		<StructLayout(LayoutKind.Sequential)> _
		Public Class CubeVertices
			<MarshalAs(UnmanagedType.ByValArray, SizeConst := 8)> _
			Private vertices() As SimpleVertex = { New SimpleVertex() With {.Pos = New Vector3F (-1.0f, 1.0f, -1.0f), .Color = New Vector4F (0.0f, 0.0f, 1.0f, 1.0f)}, New SimpleVertex() With {.Pos = New Vector3F (1.0f, 1.0f, -1.0f), .Color = New Vector4F (0.0f, 1.0f, 0.0f, 1.0f)}, New SimpleVertex() With {.Pos = New Vector3F (1.0f, 1.0f, 1.0f), .Color = New Vector4F (0.0f, 1.0f, 1.0f, 1.0f)}, New SimpleVertex() With {.Pos = New Vector3F (-1.0f, 1.0f, 1.0f), .Color = New Vector4F (1.0f, 0.0f, 0.0f, 1.0f)}, New SimpleVertex() With {.Pos = New Vector3F (-1.0f, -1.0f, -1.0f), .Color = New Vector4F (1.0f, 0.0f, 1.0f, 1.0f)}, New SimpleVertex() With {.Pos = New Vector3F (1.0f, -1.0f, -1.0f), .Color = New Vector4F (1.0f, 1.0f, 0.0f, 1.0f)}, New SimpleVertex() With {.Pos = New Vector3F (1.0f, -1.0f, 1.0f), .Color = New Vector4F (1.0f, 1.0f, 1.0f, 1.0f)}, New SimpleVertex() With {.Pos = New Vector3F (-1.0f, -1.0f, 1.0f), .Color = New Vector4F (0.0f, 0.0f, 0.0f, 1.0f)} }
		End Class

		<StructLayout(LayoutKind.Sequential)> _
		Public Class CubeIndices
			<MarshalAs(UnmanagedType.ByValArray, SizeConst := 36)> _
			Private indices() As UInteger = { 3,1,0, 2,1,3, 0,5,4, 1,5,0, 3,4,7, 0,4,3, 1,6,5, 2,6,1, 2,7,6, 3,7,2, 6,4,5, 7,4,6 }
		End Class
	End Class
	#End Region
End Namespace
