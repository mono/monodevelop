' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Windows.Media.Media3D

Namespace Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities
	''' <summary>
	''' A specialization of an XMesh that rotates the propeller of "airplane 2.x".
	''' </summary>
	Public Class AirplaneXMesh
		Inherits XMesh
		Private propRotation As Double = 0
		Private hubOffsetX As Double =.05
		Private hubOffsetY As Double =.43
		Private propZOffset As Double = -3.7
		Private propAngle As Double = 20

		Protected Overrides Function PartAnimation(ByVal partName As String) As Matrix3D
			If partName = "propeller" Then
				Dim group As New Transform3DGroup()

				group.Children.Add(New TranslateTransform3D(-hubOffsetX, -hubOffsetY, -propZOffset))
				group.Children.Add(New RotateTransform3D(New AxisAngleRotation3D(New Vector3D(1, 0, 0), -propAngle), 0, 0, 0))
				group.Children.Add(New RotateTransform3D(New AxisAngleRotation3D(New Vector3D(0, 0, 1), propRotation), 0, 0, 0))
				group.Children.Add(New RotateTransform3D(New AxisAngleRotation3D(New Vector3D(1, 0, 0), +propAngle), 0, 0, 0))
				group.Children.Add(New TranslateTransform3D(+hubOffsetX, +hubOffsetY, +propZOffset))

				propRotation += 11

				Return group.Value
			Else
				Return Matrix3D.Identity
			End If
		End Function
	End Class
End Namespace
