' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Runtime.InteropServices
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D

Namespace Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities
	<StructLayout(LayoutKind.Sequential)> _
	Public Class MatrixMath
		#Region "operator *"

		Public Shared Function VectorMultiply(ByVal a As Matrix4x4F, ByVal b As Vector4F) As Vector4F
			Return New Vector4F(a.M11 * b.x + a.M12 * b.y + a.M13 * b.z + a.M14 * b.w, a.M21 * b.x + a.M22 * b.y + a.M23 * b.z + a.M24 * b.w, a.M31 * b.x + a.M32 * b.y + a.M33 * b.z + a.M34 * b.w, a.M41 * b.x + a.M42 * b.y + a.M43 * b.z + a.M44 * b.w)
		End Function
		#End Region

		#Region "MatrixScale"
		Public Shared Function MatrixScale(ByVal x As Single, ByVal y As Single, ByVal z As Single) As Matrix4x4F
			Return New Matrix4x4F(x, 0, 0, 0, 0, y, 0, 0, 0, 0, z, 0, 0, 0, 0, 1)
		End Function
		#End Region

		#Region "MatrixTranslate"
		Public Shared Function MatrixTranslate(ByVal x As Single, ByVal y As Single, ByVal z As Single) As Matrix4x4F
			Return New Matrix4x4F(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, x, y, z, 1)
		End Function
		#End Region

		#Region "MatrixRotationX"
		Public Shared Function MatrixRotationX(ByVal angle As Single) As Matrix4x4F
			Dim sin As Single = CSng(Math.Sin(angle))
			Dim cos As Single = CSng(Math.Cos(angle))
			Return New Matrix4x4F(1, 0, 0, 0, 0, cos, sin, 0, 0, -sin, cos, 0, 0, 0, 0, 1)
		End Function
		#End Region

		#Region "MatrixRotationY"
		Public Shared Function MatrixRotationY(ByVal angle As Single) As Matrix4x4F
			Dim sin As Single = CSng(Math.Sin(angle))
			Dim cos As Single = CSng(Math.Cos(angle))
			Return New Matrix4x4F(cos, 0, -sin, 0, 0, 1, 0, 0, sin, 0, cos, 0, 0, 0, 0, 1)
		End Function
		#End Region

		#Region "MatrixRotationZ"
		Public Shared Function MatrixRotationZ(ByVal angle As Single) As Matrix4x4F
			Dim sin As Single = CSng(Math.Sin(angle))
			Dim cos As Single = CSng(Math.Cos(angle))
			Return New Matrix4x4F(cos, sin, 0, 0, -sin, cos, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1)
		End Function
		#End Region
	End Class

End Namespace
