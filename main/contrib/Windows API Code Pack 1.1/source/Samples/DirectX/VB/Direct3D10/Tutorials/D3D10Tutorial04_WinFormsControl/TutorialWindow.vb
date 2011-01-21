' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports System.Windows.Media.Media3D
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D10
Imports Microsoft.WindowsAPICodePack.DirectX.Graphics
Imports Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities

Namespace D3D10Tutorial04_WinFormsControl
	''' <summary>
	''' This application displays a 3D cube using Direct3D 10
	''' 
	''' http://msdn.microsoft.com/en-us/library/bb172488(VS.85).aspx
	''' 
	''' Copyright (c) Microsoft Corporation. All rights reserved.
	''' </summary>
	Partial Public Class TutorialWindow
		Inherits Form
		#Region "Fields"
		Private device As D3DDevice
		Private swapChain As SwapChain
		Private renderTargetView As RenderTargetView
        Private backColor_Renamed As New ColorRgba(0.0F, 0.125F, 0.3F, 1.0F)

		Private effect As Effect
		Private technique As EffectTechnique
		Private vertexLayout As InputLayout
		Private vertexBuffer As D3DBuffer
		Private indexBuffer As D3DBuffer

		Private worldVariable As EffectMatrixVariable
		Private viewVariable As EffectMatrixVariable
		Private projectionVariable As EffectMatrixVariable

		Private worldMatrix As Matrix4x4F
		Private viewMatrix As Matrix4x4F
		Private projectionMatrix As Matrix4x4F

		Private t As Single = 0f
		Private dwTimeStart As UInteger = CUInt(Environment.TickCount)
		Private needsResizing As Boolean
		#End Region

		#Region "TutorialWindow()"
		''' <summary>
		''' Initializes a new instance of the <see cref="TutorialWindow"/> class.
		''' </summary>
		Public Sub New()
			InitializeComponent()
		End Sub
		#End Region

		#Region "TutorialWindow_Load()"
		Private Sub TutorialWindow_Load(ByVal sender As Object, ByVal e As EventArgs) Handles MyBase.Load
			InitDevice()
			directControl.Render = AddressOf Me.RenderScene
		End Sub
		#End Region

		#Region "directControl_SizeChanged()"
		Private Sub directControl_SizeChanged(ByVal sender As Object, ByVal e As EventArgs) Handles directControl.SizeChanged
			needsResizing = True
		End Sub
		#End Region

		#Region "TutorialWindow_FormClosing()"
		Private Sub TutorialWindow_FormClosing(ByVal sender As Object, ByVal e As FormClosingEventArgs) Handles MyBase.FormClosing
			directControl.Render = Nothing
			device.ClearState()
		End Sub
		#End Region

		#Region "InitDevice()"
		''' <summary>
		''' Create Direct3D device and swap chain
		''' </summary>
		Protected Sub InitDevice()
            device = D3DDevice.CreateDeviceAndSwapChain(directControl.Handle)
            swapChain = device.SwapChain

			SetViews()

			' Create the effect
			Using effectStream As FileStream = File.OpenRead("Tutorial04.fxo")
				effect = device.CreateEffectFromCompiledBinary(New BinaryReader(effectStream))
			End Using

			' Obtain the technique
			technique = effect.GetTechniqueByName("Render")

			' Obtain the variables
			worldVariable = effect.GetVariableByName("World").AsMatrix()
			viewVariable = effect.GetVariableByName("View").AsMatrix()
			projectionVariable = effect.GetVariableByName("Projection").AsMatrix()

			InitVertexLayout()
			InitVertexBuffer()
			InitIndexBuffer()

			' Set primitive topology
            device.IA.PrimitiveTopology = PrimitiveTopology.TriangleList

			InitMatrices()
			needsResizing = False
		End Sub
		#End Region

		#Region "SetViews()"
		Private Sub SetViews()
			' Create a render target view
			Using pBuffer As Texture2D = swapChain.GetBuffer(Of Texture2D)(0)
				renderTargetView = device.CreateRenderTargetView(pBuffer)
			End Using
            device.OM.RenderTargets = New OutputMergerRenderTargets(New RenderTargetView() {renderTargetView})

			' Setup the viewport
			Dim vp As New Viewport() With {.Width = CUInt(directControl.ClientSize.Width), .Height = CUInt(directControl.ClientSize.Height), .MinDepth = 0.0f, .MaxDepth = 1.0f, .TopLeftX = 0, .TopLeftY = 0}

            device.RS.Viewports = New Viewport() {vp}
		End Sub
		#End Region

#Region "InitVertexLayout()"
        Private Sub InitVertexLayout()
            ' Define the input layout
            ' The layout determines the stride in the vertex buffer,
            ' so changes in layout need to be reflected in SetVertexBuffers
            Dim layout() As InputElementDescription = _
            { _
                New InputElementDescription With _
                { _
                    .SemanticName = "POSITION", _
                    .SemanticIndex = 0, _
                    .Format = Format.R32G32B32Float, _
                    .InputSlot = 0, _
                    .AlignedByteOffset = 0, _
                    .InputSlotClass = InputClassification.PerVertexData, _
                    .InstanceDataStepRate = 0 _
                }, _
                New InputElementDescription() With _
                { _
                    .SemanticName = "COLOR", _
                    .SemanticIndex = 0, _
                    .Format = Format.R32G32B32A32Float, _
                    .InputSlot = 0, _
                    .AlignedByteOffset = 12, _
                    .InputSlotClass = InputClassification.PerVertexData, _
                    .InstanceDataStepRate = 0 _
                } _
            }

            Dim passDesc As PassDescription = technique.GetPassByIndex(0).Description

            vertexLayout = device.CreateInputLayout(layout, passDesc.InputAssemblerInputSignature, passDesc.InputAssemblerInputSignatureSize)

            device.IA.InputLayout = vertexLayout
        End Sub
#End Region

		#Region "InitVertexBuffer()"
		Private Sub InitVertexBuffer()
			Dim cube As New Cube()

			Dim verticesData As IntPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf(cube.Vertices))
			Marshal.StructureToPtr(cube.Vertices, verticesData, True)

            Dim bufferDesc As New BufferDescription() With _
            { _
                .Usage = Usage.Default, _
                .ByteWidth = CUInt(Marshal.SizeOf(cube.Vertices)), _
                .BindingOptions = BindingOptions.VertexBuffer, _
                .CpuAccessOptions = CpuAccessOptions.None, _
                .MiscellaneousResourceOptions = MiscellaneousResourceOptions.None _
            }

            Dim InitData As New SubresourceData() With {.SystemMemory = verticesData}

            'D3DBuffer buffer = null;
            vertexBuffer = device.CreateBuffer(bufferDesc, InitData)

            ' Set vertex buffer
            Dim stride As UInteger = CUInt(Marshal.SizeOf(GetType(SimpleVertex)))
            Dim offset As UInteger = 0
            device.IA.SetVertexBuffers(0, New D3DBuffer() {vertexBuffer}, New UInteger() {stride}, New UInteger() {offset})
            Marshal.FreeCoTaskMem(verticesData)
        End Sub
#End Region

#Region "InitIndexBuffer()"
        Private Sub InitIndexBuffer()
            Dim cube As New Cube()

            Dim indicesData As IntPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf(cube.Indices))
            Marshal.StructureToPtr(cube.Indices, indicesData, True)

            Dim bufferDesc As New BufferDescription() With {.Usage = Usage.Default, .ByteWidth = CUInt(Marshal.SizeOf(cube.Indices)), .BindingOptions = BindingOptions.IndexBuffer, .CpuAccessOptions = CpuAccessOptions.None, .MiscellaneousResourceOptions = MiscellaneousResourceOptions.None}

            Dim initData As New SubresourceData() With {.SystemMemory = indicesData}

            indexBuffer = device.CreateBuffer(bufferDesc, initData)
            device.IA.IndexBuffer = New IndexBuffer(indexBuffer, Format.R32UInt, 0)
            Marshal.FreeCoTaskMem(indicesData)
        End Sub
#End Region

		#Region "InitMatrices()"
		Private Sub InitMatrices()
			' Initialize the world matrices
			worldMatrix = Matrix4x4F.Identity

			' Initialize the view matrix
			Dim Eye As New Vector3F(0.0f, 2.0f, -5.0f)
			Dim At As New Vector3F(0.0f, 0.0f, 0.0f)
			Dim Up As New Vector3F(0.0f, 1.0f, 0.0f)

            viewMatrix = Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities.Camera.MatrixLookAtLH(Eye, At, Up)

			' Initialize the projection matrix
            projectionMatrix = Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities.Camera.MatrixPerspectiveFovLH(CSng(Math.PI) * 0.5F, (CSng(Me.ClientSize.Width) / CSng(Me.ClientSize.Height)), 0.1F, 100.0F)

			' Update Variables that never change
			viewVariable.Matrix = viewMatrix
			projectionVariable.Matrix = projectionMatrix
		End Sub
		#End Region

		#Region "RenderScene()"
		''' <summary>
		''' Render the frame
		''' </summary>
		Protected Sub RenderScene()
			If needsResizing Then
				needsResizing = False
				renderTargetView.Dispose()
				Dim sd As SwapChainDescription = swapChain.Description
                swapChain.ResizeBuffers(sd.BufferCount, CUInt(directControl.ClientSize.Width), CUInt(directControl.ClientSize.Height), sd.BufferDescription.Format, sd.Options)
				SetViews()
				' Update the projection matrix
                projectionMatrix = Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities.Camera.MatrixPerspectiveFovLH(CSng(Math.PI) * 0.5F, (CSng(directControl.ClientSize.Width) / CSng(directControl.ClientSize.Height)), 0.1F, 100.0F)
				projectionVariable.Matrix = projectionMatrix
			End If
            t = (Environment.TickCount - dwTimeStart) / 50.0F

			' Clear the backbuffer
			device.ClearRenderTargetView(renderTargetView, backColor_Renamed)

			' Rotate the cube
			Dim rt As New RotateTransform3D(New AxisAngleRotation3D(New Vector3D(0, 1, 0), t))
			worldMatrix = rt.Value.ToMatrix4x4F()

			'
			' Update variables that change once per frame
			'
			worldVariable.Matrix = worldMatrix

			'
			' Render the cube
			'
			Dim techDesc As TechniqueDescription = technique.Description

            For p As UInteger = 0 To techDesc.Passes - 1UI
                technique.GetPassByIndex(p).Apply()
                device.DrawIndexed(36, 0, 0)
            Next p

            swapChain.Present(0, PresentOptions.None)
		End Sub
		#End Region
	End Class
End Namespace
