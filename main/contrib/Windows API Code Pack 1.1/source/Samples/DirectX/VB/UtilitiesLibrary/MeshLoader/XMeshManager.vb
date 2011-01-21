' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports Microsoft.WindowsAPICodePack.DirectX
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D10
Imports Microsoft.WindowsAPICodePack.DirectX.Graphics
Imports System.Windows.Media.Media3D
Imports System.Runtime.InteropServices
Imports System.Reflection
Imports System.IO

Namespace Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities

	''' <summary>
	''' Manages the XMesh file loading
	''' </summary>
	Public Class XMeshManager
		Implements IDisposable
		Friend device As D3DDevice

		Friend effect As Effect
		Friend techniqueRenderTexture As EffectTechnique
		Friend techniqueRenderVertexColor As EffectTechnique
		Friend techniqueRenderMaterialColor As EffectTechnique

		Friend brightnessVariable As EffectScalarVariable
		Friend materialColorVariable As EffectVectorVariable
		Friend worldVariable As EffectMatrixVariable
		Friend viewVariable As EffectMatrixVariable
		Friend projectionVariable As EffectMatrixVariable
		Friend diffuseVariable As EffectShaderResourceVariable

		''' <summary>
		''' Creates the mesh manager
		''' </summary>
		''' <param name="device"></param>
		Public Sub New(ByVal device As D3DDevice)
			Me.device = device

			' Create the effect
			'XMesh.fxo was compiled from XMesh.fx using:
			' "$(DXSDK_DIR)utilities\bin\x86\fxc" "$(ProjectDir)Mesh\MeshLoaders\XMesh.fx" /T fx_4_0 /Fo"$(ProjectDir)Mesh\MeshLoaders\XMesh.fxo"
            Using effectStream As Stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("XMesh.fxo")
                effect = device.CreateEffectFromCompiledBinary(New BinaryReader(effectStream))
            End Using

			' Obtain the techniques
			techniqueRenderTexture = effect.GetTechniqueByName("RenderTextured")
			techniqueRenderVertexColor = effect.GetTechniqueByName("RenderVertexColor")
			techniqueRenderMaterialColor = effect.GetTechniqueByName("RenderMaterialColor")

			' Obtain the variables
			brightnessVariable = effect.GetVariableByName("Brightness").AsScalar()
			materialColorVariable = effect.GetVariableByName("MaterialColor").AsVector()
			worldVariable = effect.GetVariableByName("World").AsMatrix()
			viewVariable = effect.GetVariableByName("View").AsMatrix()
			projectionVariable = effect.GetVariableByName("Projection").AsMatrix()
			diffuseVariable = effect.GetVariableByName("tex2D").AsShaderResource()
		End Sub

		Public Sub SetViewAndProjection(ByVal view As Matrix4x4F, ByVal projection As Matrix4x4F)
            viewVariable.Matrix = view
            projectionVariable.Matrix = projection
		End Sub

		''' <summary>
		''' Returns an XMesh object that contains the data from a specified .X file.
		''' </summary>
		''' <param name="path"></param>
		''' <returns></returns>
		Public Function Open(ByVal path As String) As XMesh
			Dim mesh As New XMesh()
			mesh.Load(path, Me)
			Return mesh
		End Function

		''' <summary>
		''' Reutrns a specialization of an XMesh object that contains the data from a specified .X file
		''' </summary>
		''' <typeparam name="T"></typeparam>
		''' <param name="path"></param>
		''' <returns></returns>
		Public Function Open(Of T As {XMesh, New})(ByVal path As String) As T
			Dim mesh As New T()
			mesh.Load(path, Me)
			Return mesh
		End Function

		#Region "IDisposable Members"

		''' <summary>
		''' Cleans up the memory allocated by the manager.
		''' </summary>
		Public Sub Dispose() Implements IDisposable.Dispose
			effect.Dispose()
		End Sub

		#End Region
	End Class
End Namespace
