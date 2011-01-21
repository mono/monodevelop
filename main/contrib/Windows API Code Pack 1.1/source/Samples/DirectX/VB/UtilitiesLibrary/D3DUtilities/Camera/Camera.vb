' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D

Namespace Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities
	Public NotInheritable Class Camera
		Private Sub New()
		End Sub
		Public Shared Function MatrixLookAtLH(ByVal eye As Vector3F, ByVal at As Vector3F, ByVal up As Vector3F) As Matrix4x4F
			Dim right, vec As Vector3F

			vec = at - eye
			vec.NormalizeInPlace()
			right = Vector3F.Cross(up, vec)
			up = Vector3F.Cross(vec, right)
			right.NormalizeInPlace()
			up.NormalizeInPlace()
			Return New Matrix4x4F(right.x, up.x, vec.x, 0.0f, right.y, up.y, vec.y, 0.0f, right.z, up.z, vec.z, 0.0f, -Vector3F.Dot(right, eye), -Vector3F.Dot(up, eye), -Vector3F.Dot(vec, eye), 1.0f)
		End Function

		Public Shared Function MatrixPerspectiveFovLH(ByVal fovy As Single, ByVal aspect As Single, ByVal zn As Single, ByVal zf As Single) As Matrix4x4F
			Dim ret As New Matrix4x4F()
			ret.M11 = 1.0f / (aspect * CSng(Math.Tan(fovy / 2)))
			ret.M22 = 1.0f / CSng(Math.Tan(fovy / 2))
			ret.M33 = zf / (zf - zn)
			ret.M34 = 1
			ret.M43 = (zf * zn) / (zn - zf)
			ret.M44 = 0
			Return ret
		End Function
	End Class
End Namespace
