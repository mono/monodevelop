' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Windows.Media.Media3D
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D


Namespace Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities
	Public Module MatrixExtensions
		<System.Runtime.CompilerServices.Extension> _
		Public Function ToMatrix4x4F(ByVal source As Matrix3D) As Matrix4x4F
			Return New Matrix4x4F(CSng(source.M11), CSng(source.M12), CSng(source.M13), CSng(source.M14), CSng(source.M21), CSng(source.M22), CSng(source.M23), CSng(source.M24), CSng(source.M31), CSng(source.M32), CSng(source.M33), CSng(source.M34), CSng(source.OffsetX), CSng(source.OffsetY), CSng(source.OffsetZ), CSng(source.M44))
		End Function

		<System.Runtime.CompilerServices.Extension> _
		Public Function ToMatrix3D(ByVal source As Matrix4x4F) As Matrix3D
			Dim destination As New Matrix3D()
			destination.M11 = CSng(source.M11)
			destination.M12 = CSng(source.M12)
			destination.M13 = CSng(source.M13)
			destination.M14 = CSng(source.M14)

			destination.M21 = CSng(source.M21)
			destination.M22 = CSng(source.M22)
			destination.M23 = CSng(source.M23)
			destination.M24 = CSng(source.M24)

			destination.M31 = CSng(source.M31)
			destination.M32 = CSng(source.M32)
			destination.M33 = CSng(source.M33)
			destination.M34 = CSng(source.M34)

			destination.OffsetX = CSng(source.M41)
			destination.OffsetY = CSng(source.M42)
			destination.OffsetZ = CSng(source.M43)
			destination.M44 = CSng(source.M44)

			Return destination
		End Function

		#Region "PerspectiveCamera extensions"
		''' <summary>
		''' Returns the world*perspective matrix for the camera
		''' </summary>
		''' <param name="camera">The WPF camera</param>
		''' <param name="aspectRatio">The aspect ratio of the device surface</param>
		''' <returns></returns>
		<System.Runtime.CompilerServices.Extension> _
		Public Function ToMatrix3DLH(ByVal camera As PerspectiveCamera, ByVal aspectRatio As Double) As Matrix3D
			Dim tg As New Transform3DGroup()
			tg.Children.Add(New MatrixTransform3D(GetLookAtMatrixLH(camera)))
			tg.Children.Add(camera.Transform)
			tg.Children.Add(New MatrixTransform3D(GetPerspectiveMatrixLH(camera, aspectRatio)))
			Return tg.Value
		End Function

		<System.Runtime.CompilerServices.Extension> _
		Public Function ToViewLH(ByVal camera As PerspectiveCamera) As Matrix3D
			Return GetLookAtMatrixLH(camera)
		End Function

		<System.Runtime.CompilerServices.Extension> _
		Public Function ToPerspectiveLH(ByVal camera As PerspectiveCamera, ByVal aspectRatio As Double) As Matrix3D
			Return GetPerspectiveMatrixLH(camera, aspectRatio)
		End Function
		#End Region

		#Region "PerspectiveCamera implementation"
		Friend Function GetPerspectiveMatrixLH(ByVal camera As PerspectiveCamera, ByVal aspectRatio As Double) As Matrix3D
			Dim fov As Double = (camera.FieldOfView / 360.0) * 2.0 * Math.PI
			Dim zn As Double = camera.NearPlaneDistance
			Dim zf As Double = camera.FarPlaneDistance
			Dim f As Double = 1.0 / Math.Tan(fov / 2.0)
			Dim xScale As Double = f / aspectRatio
			Dim yScale As Double = f
			Dim n As Double = (1.0 / (zf - zn))
			Dim m33 As Double = zf * n
			Dim m43 As Double = -zf * zn * n

			Return New Matrix3D(xScale, 0, 0, 0, 0, yScale, 0, 0, 0, 0, m33, 1, 0, 0, m43, 0)
		End Function

		Friend Function GetLookAtMatrixLH(ByVal camera As PerspectiveCamera) As Matrix3D
			Dim f As New Vector3D(camera.Position.X - camera.LookDirection.X, camera.Position.Y - camera.LookDirection.Y, camera.Position.Z - camera.LookDirection.Z)
			f.Normalize()
			Dim vUpActual As Vector3D = camera.UpDirection
			vUpActual.Normalize()
			Dim s As Vector3D = Vector3D.CrossProduct(f, vUpActual)
			Dim u As Vector3D = Vector3D.CrossProduct(s, f)

			Return New Matrix3D(s.X, u.X, -f.X, 0, s.Y, u.Y, -f.Y, 0, s.Z, u.Z, -f.Z, 0, -camera.Position.X, -camera.Position.Y, -camera.Position.Z, 1)
		End Function
		#End Region
	End Module
End Namespace
