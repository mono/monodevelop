' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.IO
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D10
Imports Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities
Imports System.Reflection

Namespace WindowsFlag
	''' <summary>
	''' This class wraps the shaders
	''' </summary>
	Friend Class Effects
		#Region "Private fields"
		Private effect As Effect
        Private technique_Renamed As EffectTechnique
		Private worldVariable As EffectMatrixVariable
		Private viewVariable As EffectMatrixVariable
		Private projectionVariable As EffectMatrixVariable

		Private lightDirVariable As EffectVectorVariable
		Private lightColorVariable As EffectVectorVariable
		Private baseColorVariable As EffectVectorVariable

        Private vLightDirs() As Vector4F = {New Vector4F(0.0F, 1.0F, 3.0F, 1.0F), New Vector4F(0.0F, 1.0F, -3.0F, 1.0F)}

        Private vLightColors() As Vector4F = {New Vector4F(0.25F, 0.25F, 0.25F, 0.25F), New Vector4F(0.25F, 0.25F, 0.25F, 0.25F)}

		#End Region

		#Region "Internal properties"
		Friend WriteOnly Property WorldMatrix() As Matrix4x4F
			Set(ByVal value As Matrix4x4F)
                worldVariable.Matrix = value
			End Set
		End Property

		Friend WriteOnly Property ViewMatrix() As Matrix4x4F
			Set(ByVal value As Matrix4x4F)
                viewVariable.Matrix = value
			End Set
		End Property

		Friend WriteOnly Property ProjectionMatrix() As Matrix4x4F
			Set(ByVal value As Matrix4x4F)
                projectionVariable.Matrix = value
			End Set
		End Property

        Friend WriteOnly Property BaseColor() As Vector4F
            Set(ByVal value As Vector4F)
                baseColorVariable.FloatVector = value
            End Set
        End Property

		Friend ReadOnly Property Technique() As EffectTechnique
			Get
				Return technique_Renamed
			End Get
		End Property
		#End Region

		Public Sub New(ByVal device As D3DDevice)
			' File compiled using the following command:
			' "$(DXSDK_DIR)\utilities\bin\x86\fxc" "WindowsFlag.fx" /T fx_4_0 /Fo "WindowsFlag.fxo"
            Using stream As Stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("WindowsFlag.fxo")
                effect = device.CreateEffectFromCompiledBinary(stream)
            End Using
			' Obtain the technique
			technique_Renamed = effect.GetTechniqueByName("Render")

			' Obtain the variables
			worldVariable = effect.GetVariableByName("World").AsMatrix()
			viewVariable = effect.GetVariableByName("View").AsMatrix()
			projectionVariable = effect.GetVariableByName("Projection").AsMatrix()

			lightDirVariable = effect.GetVariableByName("vLightDir").AsVector()
			lightColorVariable = effect.GetVariableByName("vLightColor").AsVector()
			baseColorVariable = effect.GetVariableByName("vBaseColor").AsVector()

			' Set constants
			lightColorVariable.SetFloatVectorArray(vLightColors)
			lightDirVariable.SetFloatVectorArray(vLightDirs)
		End Sub
	End Class
End Namespace
