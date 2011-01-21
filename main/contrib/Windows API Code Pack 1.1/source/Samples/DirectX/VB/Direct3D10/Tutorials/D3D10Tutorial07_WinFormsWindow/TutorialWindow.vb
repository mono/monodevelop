' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Windows.Forms
Imports System.Runtime.InteropServices
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D10
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D
Imports Microsoft.WindowsAPICodePack.DirectX.Graphics
Imports System.IO
Imports Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities
Imports System.Windows.Media.Media3D

Namespace D3D10Tutorial07
	''' <summary>
	''' This application demonstrates texturing
	''' 
	''' http://msdn.microsoft.com/en-us/library/bb172491(VS.85).aspx
	''' 
	''' Copyright (c) Microsoft Corporation. All rights reserved.
	''' </summary>
	Partial Public Class TutorialWindow
		Inherits Form
		#Region "Fields"
		Private device As D3DDevice
		Private swapChain As SwapChain
		Private renderTargetView As RenderTargetView
		Private textureRV As ShaderResourceView
        Private backColor_Renamed As New ColorRgba(0.0F, 0.125F, 0.3F, 1.0F)
		Private meshColor As New Vector4F(0.7f, 0.7f, 0.7f, 1.0f)

		Private effect As Effect
		Private technique As EffectTechnique

		Private vertexLayout As InputLayout
		Private vertexBuffer As D3DBuffer
		Private indexBuffer As D3DBuffer

		'variables from the .fx file
		Private worldVariable As EffectMatrixVariable
		Private viewVariable As EffectMatrixVariable
		Private projectionVariable As EffectMatrixVariable

		Private meshColorVariable As EffectVectorVariable
		Private diffuseVariable As EffectShaderResourceVariable

		Private cube As New Cube()

		Private viewMatrix As Matrix4x4F
		Private projectionMatrix As Matrix4x4F

		Private currentTime As Single = 0f
		Private dwTimeStart As UInteger = CUInt(Environment.TickCount)
		Private active As Boolean
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
		End Sub
		#End Region

		#Region "WndProc()"
		''' <summary>
		''' Forces the window paint event
		''' </summary>
		''' <param name="m"></param>
		Protected Overrides Sub WndProc(ByRef m As System.Windows.Forms.Message)
			Invalidate()
			MyBase.WndProc(m)
		End Sub
		#End Region

		#Region "OnMouseDoubleClick()"
		''' <summary>
		''' Switches full-screen mode
		''' </summary>
		''' <param name="e"></param>
		Protected Overrides Sub OnMouseDoubleClick(ByVal e As MouseEventArgs)
			If active Then
				swapChain.IsFullScreen = Not swapChain.IsFullScreen
			End If
		End Sub
		#End Region

		#Region "OnPaintBackground()"
		Protected Overrides Sub OnPaintBackground(ByVal e As PaintEventArgs)
			' Leave empty so that invalidate does not redraw the background causing flickering
		End Sub
		#End Region

		#Region "OnPaint()"
		Protected Overrides Sub OnPaint(ByVal e As PaintEventArgs)
			If active Then
				RenderScene()
			End If
		End Sub
		#End Region

		#Region "OnSizeChanged()"
		Protected Overrides Sub OnSizeChanged(ByVal e As EventArgs)
			If active Then
				renderTargetView.Dispose()
				Dim sd As SwapChainDescription = swapChain.Description
                swapChain.ResizeBuffers(sd.BufferCount, CUInt(ClientSize.Width), CUInt(ClientSize.Height), sd.BufferDescription.Format, sd.Options)
				SetViews()
				' Update the projection matrix
                projectionMatrix = Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities.Camera.MatrixPerspectiveFovLH(CSng(Math.PI) * 0.25F, (CSng(ClientSize.Width) / CSng(ClientSize.Height)), 0.5F, 100.0F)
				projectionVariable.Matrix = projectionMatrix
			End If
			MyBase.OnSizeChanged(e)
		End Sub
		#End Region

		#Region "InitDevice()"
		''' <summary>
		''' Create Direct3D device and swap chain
		''' </summary>
		Protected Sub InitDevice()
            device = D3DDevice.CreateDeviceAndSwapChain(Me.Handle)
            swapChain = device.SwapChain

			SetViews()

			' Create the effect
			Using effectStream As FileStream = File.OpenRead("Tutorial07.fxo")
				effect = device.CreateEffectFromCompiledBinary(New BinaryReader(effectStream))
			End Using

			' Obtain the technique
			technique = effect.GetTechniqueByName("Render")

			' Obtain the variables
			worldVariable = effect.GetVariableByName("World").AsMatrix()
			viewVariable = effect.GetVariableByName("View").AsMatrix()
			projectionVariable = effect.GetVariableByName("Projection").AsMatrix()
			meshColorVariable = effect.GetVariableByName("vMeshColor").AsVector()
			diffuseVariable = effect.GetVariableByName("txDiffuse").AsShaderResource()

			InitVertexLayout()
			InitVertexBuffer()
			InitIndexBuffer()

			' Set primitive topology
            device.IA.PrimitiveTopology = PrimitiveTopology.TriangleList

			' Load the Texture
			Using stream As FileStream = File.OpenRead("seafloor.png")
				textureRV = TextureLoader.LoadTexture(device, stream)
			End Using

			InitMatrices()

            diffuseVariable.Resource = textureRV
			active = True
		End Sub
		#End Region

		#Region "SetViews()"
		''' <summary>
		''' Sets the views that depend on background buffer dimensions
		''' </summary>
		Private Sub SetViews()
			' Create a render target view
			Using pBuffer As Texture2D = swapChain.GetBuffer(Of Texture2D)(0)
				renderTargetView = device.CreateRenderTargetView(pBuffer)
			End Using

			'bind the render target to the device
            device.OM.RenderTargets = New OutputMergerRenderTargets(New RenderTargetView() {renderTargetView})

			' Setup the viewport
			Dim vp As New Viewport() With {.Width = CUInt(ClientSize.Width), .Height = CUInt(ClientSize.Height), .MinDepth = 0.0f, .MaxDepth = 1.0f, .TopLeftX = 0, .TopLeftY = 0}

            device.RS.Viewports = New Viewport() {vp}
		End Sub
		#End Region

		#Region "InitVertexLayout()"
		Private Sub InitVertexLayout()
			' Define the input layout
			' The layout determines the stride in the vertex buffer,
			' so changes in layout need to be reflected in SetVertexBuffers
            Dim layout() As InputElementDescription = {New InputElementDescription() With {.SemanticName = "POSITION", .SemanticIndex = 0, .Format = Format.R32G32B32Float, .InputSlot = 0, .AlignedByteOffset = 0, .InputSlotClass = InputClassification.PerVertexData, .InstanceDataStepRate = 0}, New InputElementDescription() With {.SemanticName = "TEXCOORD", .SemanticIndex = 0, .Format = Format.R32G32Float, .InputSlot = 0, .AlignedByteOffset = 12, .InputSlotClass = InputClassification.PerVertexData, .InstanceDataStepRate = 0}}

			Dim passDesc As PassDescription = technique.GetPassByIndex(0).Description

			vertexLayout = device.CreateInputLayout(layout, passDesc.InputAssemblerInputSignature, passDesc.InputAssemblerInputSignatureSize)

            device.IA.InputLayout = vertexLayout
		End Sub
		#End Region

		#Region "InitVertexBuffer()"
		Private Sub InitVertexBuffer()
			Dim verticesData As IntPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf(cube.Vertices))
			Marshal.StructureToPtr(cube.Vertices, verticesData, True)

            Dim bufferDesc As New BufferDescription() With {.Usage = Usage.Default, .ByteWidth = CUInt(Marshal.SizeOf(cube.Vertices)), .BindingOptions = BindingOptions.VertexBuffer, .CpuAccessOptions = CpuAccessOptions.None, .MiscellaneousResourceOptions = MiscellaneousResourceOptions.None}

            Dim InitData As New SubresourceData() With {.SystemMemory = verticesData}

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
			' Initialize the view matrix
			Dim Eye As New Vector3F(0.0f, 3.0f, -6.0f)
			Dim At As New Vector3F(0.0f, 0.0f, 0.0f)
			Dim Up As New Vector3F(0.0f, 1.0f, 0.0f)

            viewMatrix = Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities.Camera.MatrixLookAtLH(Eye, At, Up)

			' Initialize the projection matrix
            projectionMatrix = Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities.Camera.MatrixPerspectiveFovLH(CSng(Math.PI) * 0.25F, (CSng(Me.ClientSize.Width) / CSng(Me.ClientSize.Height)), 0.5F, 100.0F)

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
			Dim worldMatrix As Matrix4x4F

			currentTime = (Environment.TickCount - dwTimeStart) / 1000.0f

			'WPF transforms used here use degrees as opposed to D3DX which uses radians in the native tutorial
			'360 degrees == 2 * Math.PI
			'world matrix rotates the first cube by t degrees
			Dim rt1 As New RotateTransform3D(New AxisAngleRotation3D(New Vector3D(0, 1, 0), currentTime * 30))
			worldMatrix = rt1.Value.ToMatrix4x4F()

			' Modify the color
			meshColor.X = (CSng(Math.Sin(currentTime * 1.0f)) + 1.0f) * 0.5f
			meshColor.Y = (CSng(Math.Cos(currentTime * 3.0f)) + 1.0f) * 0.5f
			meshColor.Z = (CSng(Math.Sin(currentTime * 5.0f)) + 1.0f) * 0.5f

			' Clear the backbuffer
			device.ClearRenderTargetView(renderTargetView, backColor_Renamed)

			'
			' Update variables that change once per frame
			'
			worldVariable.Matrix = worldMatrix
			meshColorVariable.FloatVector = meshColor

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
