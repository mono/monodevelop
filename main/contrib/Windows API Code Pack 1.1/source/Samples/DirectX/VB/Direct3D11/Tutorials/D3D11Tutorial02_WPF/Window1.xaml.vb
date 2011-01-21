' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Windows
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D11
Imports Microsoft.WindowsAPICodePack.DirectX.Graphics

Namespace Microsoft.WindowsAPICodePack.Samples.Direct3D11
	''' <summary>
	''' Interaction logic for Window1.xaml
	''' </summary>
	Partial Public Class Window1
		Inherits Window
		#Region "Structs"

		<StructLayout(LayoutKind.Sequential)> _
		Private Class SimpleVertexArray
			' An array of 3 Vectors
			<MarshalAs(UnmanagedType.ByValArray, SizeConst := 3)> _
			Public vertices() As Vector3F = { New Vector3F() With {.x = 0.0F, .y = 0.5F, .z = 0.5F}, New Vector3F() With {.x = 0.5F, .y = -0.5F, .z = 0.5F}, New Vector3F() With {.x = -0.5F, .y = -0.5F, .z = 0.5F} }
		End Class
		#End Region

		Public Sub New()
			InitializeComponent()
		End Sub

		#Region "Private Fields"

		Private device As D3DDevice
		Private deviceContext As DeviceContext
		Private renderTargetView As RenderTargetView
		Private pixelShader As PixelShader
		Private vertexShader As VertexShader
		Private vertexBuffer As D3DBuffer
		Private swapChain As SwapChain
        Private needsResizing As Boolean
        #End Region

		#Region "Window_Loaded()"
		''' <summary>
		''' Handles the Loaded event of the window.
		''' </summary>
		''' <param name="sender">The source of the event.</param>
		''' <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
		Private Sub Window_Loaded(ByVal sender As Object, ByVal e As RoutedEventArgs)
			InitDevice()
			host.Render = AddressOf RenderScene
		End Sub
		#End Region

		#Region "Init device"

		''' <summary>
		''' Init device and required resources
		''' </summary>
		Private Sub InitDevice()
			' device creation
            device = D3DDevice.CreateDeviceAndSwapChain(host.Handle)
            swapChain = device.SwapChain
            deviceContext = device.ImmediateContext

            SetViews()

			' vertex shader & layout            

			' Open precompiled vertex shader
			' This file was compiled using: fxc Render.hlsl /T vs_4_0 /EVertShader /FoRender.vs
            Using stream As Stream = Application.ResourceAssembly.GetManifestResourceStream("Render.vs")
                vertexShader = device.CreateVertexShader(stream)
                deviceContext.VS.Shader = vertexShader

                ' input layout is for the vert shader
                Dim inputElementDescription As New InputElementDescription()
                inputElementDescription.SemanticName = "POSITION"
                inputElementDescription.SemanticIndex = 0
                inputElementDescription.Format = Format.R32G32B32Float
                inputElementDescription.InputSlot = 0
                inputElementDescription.AlignedByteOffset = 0
                inputElementDescription.InputSlotClass = InputClassification.PerVertexData
                inputElementDescription.InstanceDataStepRate = 0
                stream.Position = 0
                Dim inputLayout As InputLayout = device.CreateInputLayout(New InputElementDescription() {inputElementDescription}, stream)
                deviceContext.IA.InputLayout = inputLayout
            End Using

			' Open precompiled vertex shader
			' This file was compiled using: fxc Render.hlsl /T ps_4_0 /EPixShader /FoRender.ps
            Using stream As Stream = Application.ResourceAssembly.GetManifestResourceStream("Render.ps")
                pixelShader = device.CreatePixelShader(stream)
            End Using
			deviceContext.PS.SetShader(pixelShader, Nothing)

			' create some geometry to draw (1 triangle)
			Dim vertex As New SimpleVertexArray()

			' put the vertices into a vertex buffer

			Dim bufferDescription As New BufferDescription()
			bufferDescription.Usage = Usage.Default
			bufferDescription.ByteWidth = CUInt(Marshal.SizeOf(vertex))
            bufferDescription.BindingOptions = BindingOptions.VertexBuffer

			Dim subresourceData As New SubresourceData()

			Dim vertexData As IntPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf(vertex))
			Marshal.StructureToPtr(vertex, vertexData, False)

            subresourceData.SystemMemory = vertexData
			vertexBuffer = device.CreateBuffer(bufferDescription, subresourceData)


			deviceContext.IA.SetVertexBuffers(0, New D3DBuffer() { vertexBuffer }, New UInteger() { 12 }, New UInteger() { 0 })
            deviceContext.IA.PrimitiveTopology = PrimitiveTopology.TriangleList

			Marshal.FreeCoTaskMem(vertexData)
		End Sub
        #End Region

        #Region "SetViews()
        Private Sub SetViews()
            Dim texture2D As Texture2D = swapChain.GetBuffer(Of Texture2D)(0)
            renderTargetView = device.CreateRenderTargetView(texture2D)
            deviceContext.OM.RenderTargets = New OutputMergerRenderTargets(New RenderTargetView() {renderTargetView})
            texture2D.Dispose()

            ' viewport
            Dim desc As SwapChainDescription = swapChain.Description
            Dim viewport As New Viewport()
            viewport.Width = desc.BufferDescription.Width
            viewport.Height = desc.BufferDescription.Height
            viewport.MinDepth = 0.0F
            viewport.MaxDepth = 1.0F
            viewport.TopLeftX = 0
            viewport.TopLeftY = 0

            deviceContext.RS.Viewports = New Viewport() {viewport}
        End Sub
        #End Region

		#Region "Render Scene"

		''' <summary>
		''' Draw scene
		''' </summary>
		Private Sub RenderScene()
            If (needsResizing) Then
                needsResizing = False
                renderTargetView.Dispose()
                Dim sd As SwapChainDescription = swapChain.Description
                swapChain.ResizeBuffers(sd.BufferCount, CUInt(host.ActualWidth), CUInt(host.ActualHeight), sd.BufferDescription.Format, sd.Options)
                SetViews()
            End If
            deviceContext.ClearRenderTargetView(renderTargetView, New ColorRgba(0.2F, 0.125F, 0.3F, 1.0F))

			deviceContext.Draw(3, 0)

			swapChain.Present(0, 0)
		End Sub
		#End Region

        #Region "host_SizeChanged()"
        Private Sub host_SizeChanged(ByVal sender As System.Object, ByVal e As System.Windows.SizeChangedEventArgs)
            needsResizing = True
        End Sub
        #End Region
    End Class
End Namespace
