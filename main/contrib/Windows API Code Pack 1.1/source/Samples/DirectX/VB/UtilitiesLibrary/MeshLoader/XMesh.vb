' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.IO
Imports Microsoft.WindowsAPICodePack.DirectX
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D10
Imports Microsoft.WindowsAPICodePack.DirectX.Graphics
Imports System.Windows.Media.Media3D
Imports System.Runtime.InteropServices

Namespace Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities
	''' <summary>
	''' 
	''' </summary>
	Public Class XMesh
		Implements IDisposable
		#Region "public methods"
		''' <summary>
		''' Renders the mesh with the specified transformation
		''' </summary>
		''' <param name="modelTransform"></param>
        Public Sub Render(ByVal modelTransform As Matrix4x4F)
            ' setup rasterization
            Dim rDescription As New RasterizerDescription() With _
            { _
                .FillMode = If(wireFrame, FillMode.Wireframe, FillMode.Solid), _
                .CullMode = CullMode.Back, _
                .FrontCounterClockwise = False, _
                .DepthBias = 0, _
                .DepthBiasClamp = 0, _
                .SlopeScaledDepthBias = 0, _
                .DepthClipEnable = True, _
                .ScissorEnable = False, _
                .MultisampleEnable = True, _
                .AntialiasedLineEnable = True _
            }

            Using rState As RasterizerState = Me.manager.device.CreateRasterizerState(rDescription)
                Me.manager.device.RS.State = rState

                Me.manager.brightnessVariable.AsSingle = Me.lightIntensity_Renamed

                If rootParts IsNot Nothing Then
                    Dim transform As Matrix3D = modelTransform.ToMatrix3D()

                    For Each part As Part In rootParts
                        RenderPart(part, transform, Nothing)
                    Next
                End If

                ' Note: see comment regarding input layout in RenderPart()
                ' method; the same thing applies to the render state of the
                ' rasterizer stage of the pipeline.
                Me.manager.device.RS.State = Nothing
            End Using
        End Sub
		#End Region

		#Region "public properties"
		''' <summary>
		''' Displays the unshaded wireframe if true
		''' </summary>
		Public Property ShowWireFrame() As Boolean
			Get
				Return wireFrame
			End Get
			Set(ByVal value As Boolean)
				wireFrame = value
			End Set
		End Property
		Private wireFrame As Boolean = False

		''' <summary>
		''' Sets the intensity of the light used in rendering.
		''' </summary>
		Public Property LightIntensity() As Single
			Get
				Return lightIntensity_Renamed
			End Get
			Set(ByVal value As Single)
				lightIntensity_Renamed = value
			End Set
		End Property
        Private lightIntensity_Renamed As Single = 1.0F
		#End Region

		#Region "virtual methods"
		Protected Overridable Function PartAnimation(ByVal partName As String) As Matrix3D
			Return Matrix3D.Identity
        End Function

        Friend Overridable Function UpdateRasterizerStateForPart(ByVal part As Part) As ShaderResourceView
            ' Empty base implementation
            Return Nothing
        End Function
#End Region

#Region "implementation"
        Friend Sub New()
        End Sub

        Friend Sub Load(ByVal path As String, ByVal manager As XMeshManager)
            Me.manager = manager
            Dim loader As New XMeshTextLoader(Me.manager.device)
            rootParts = loader.XMeshFromFile(path)
        End Sub

        Private Sub RenderPart(ByVal part As Part, ByVal parentMatrix As Matrix3D, ByVal parentTextureOverride As ShaderResourceView)
            ' set part transform
            Dim partGroup As New Transform3DGroup()
            partGroup.Children.Add(New MatrixTransform3D(PartAnimation(part.name)))
            partGroup.Children.Add(New MatrixTransform3D(part.partTransform.ToMatrix3D()))
            partGroup.Children.Add(New MatrixTransform3D(parentMatrix))

            parentMatrix = partGroup.Value

            Dim textureOverride As ShaderResourceView = UpdateRasterizerStateForPart(part)

            If textureOverride Is Nothing Then
                textureOverride = parentTextureOverride
            Else
                parentTextureOverride = textureOverride
            End If

            If part.vertexBuffer IsNot Nothing Then
                Dim technique As EffectTechnique = Nothing

                If textureOverride IsNot Nothing Then
                    technique = Me.manager.techniqueRenderTexture
                    Me.manager.diffuseVariable.Resource = textureOverride
                ElseIf part.material Is Nothing Then
                    technique = Me.manager.techniqueRenderVertexColor
                Else
                    If part.material.textureResource IsNot Nothing Then
                        technique = Me.manager.techniqueRenderTexture
                        Me.manager.diffuseVariable.Resource = part.material.textureResource
                    Else
                        technique = Me.manager.techniqueRenderMaterialColor
                        Me.manager.materialColorVariable.FloatVector = part.material.materialColor
                    End If
                End If

                Me.manager.worldVariable.Matrix = parentMatrix.ToMatrix4x4F()

                ' set up vertex buffer and index buffer
                Dim stride As UInteger = CType(Marshal.SizeOf(GetType(XMeshVertex)), UInteger)
                Dim offset As UInteger = 0
                Me.manager.device.IA.SetVertexBuffers(0, New D3DBuffer() _
                    {part.vertexBuffer}, _
                    New UInteger() {stride}, _
                    New UInteger() {offset})

                ' Set primitive topology
                Me.manager.device.IA.PrimitiveTopology = PrimitiveTopology.TriangleList

                Dim techDesc As TechniqueDescription = technique.Description
                For p As UInteger = 0 To CType(techDesc.Passes - 1, UInteger)
                    technique.GetPassByIndex(p).Apply()
                    Dim passDescription As PassDescription = technique.GetPassByIndex(p).Description

                    Using inputLayout As InputLayout = Me.manager.device.CreateInputLayout( _
                            part.dataDescription, _
                            passDescription.InputAssemblerInputSignature, _
                            passDescription.InputAssemblerInputSignatureSize)

                        ' set vertex layout
                        Me.manager.device.IA.InputLayout = inputLayout

                        ' draw part
                        Me.manager.device.Draw(CType(part.vertexCount, UInteger), 0)

                        ' Note: In Direct3D 10, the device will not retain a reference
                        ' to the input layout, so it's important to reset the device's
                        ' input layout before disposing the object.  Were this code
                        ' using Direct3D 11, the device would in fact retain a reference
                        ' and so it would be safe to go ahead and dispose the input
                        ' layout without resetting it; in that case, there could be just
                        ' a single assignment to null outside the 'for' loop, or even
                        ' no assignment at all.
                        Me.manager.device.IA.InputLayout = Nothing
                    End Using
                Next p
            End If

            For Each childPart In part.parts
                RenderPart(childPart, parentMatrix, parentTextureOverride)
            Next childPart
        End Sub


        ''' <summary>
        ''' The root part of this mesh
        ''' </summary>
        Friend rootParts As IEnumerable(Of Part)

        ''' <summary>
        ''' The object that manages the XMeshes
        ''' </summary>
        Friend manager As XMeshManager

#End Region

#Region "IDisposable Members"
        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub

        Private disposed As Boolean

        Private Sub DisposePart(ByVal part As Part)
            If part.vertexBuffer IsNot Nothing Then
                part.vertexBuffer.Dispose()
                part.vertexBuffer = Nothing
            End If
            If (part.material IsNot Nothing) And (part.material.textureResource IsNot Nothing) Then
                part.material.textureResource.Dispose()
                part.material.textureResource = Nothing
            End If

            For Each childPart In part.parts
                DisposePart(childPart)
            Next childPart

            part.parts = Nothing
        End Sub

        ''' <summary>
        ''' Releases resources no longer needed.
        ''' </summary>
        Protected Overridable Sub Dispose(ByVal disposing As Boolean)
            If Not disposed And rootParts IsNot Nothing Then
                For Each part In rootParts
                    DisposePart(part)
                Next part
                rootParts = Nothing
                disposed = True
            End If
        End Sub
#End Region ' Disposable Members
    End Class
End Namespace
