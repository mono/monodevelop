' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.IO
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D11
Imports Microsoft.WindowsAPICodePack.DirectX.Graphics

Namespace Microsoft.WindowsAPICodePack.Samples.Direct3D11
	Partial Public Class Form1
		Inherits Form
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

		Private device As D3DDevice
		Private deviceContext As DeviceContext
		Private renderTargetView As RenderTargetView
		Private pixelShader As PixelShader
		Private vertexShader As VertexShader
		Private vertexBuffer As D3DBuffer
		Private swapChain As SwapChain
        Private needsResizing As Boolean


		Private Sub directControl_Load(ByVal sender As Object, ByVal e As EventArgs) Handles directControl.Load
			InitDevice()
			directControl.Render = AddressOf RenderScene
        End Sub

        Private Sub InitDevice()
            device = D3DDevice.CreateDeviceAndSwapChain(directControl.Handle)
            swapChain = device.SwapChain
            deviceContext = device.ImmediateContext

            SetViews()

            ' Open precompiled vertex shader
            ' This file was compiled using: fxc Render.hlsl /T vs_4_0 /EVertShader /FoRender.vs
            Using stream As Stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Render.vs")
                vertexShader = device.CreateVertexShader(stream)
            End Using

            deviceContext.VS.SetShader(vertexShader, Nothing)

            ' input layout is for the vert shader
            Dim inputElementDescription As New InputElementDescription()
            inputElementDescription.SemanticName = "POSITION"
            inputElementDescription.SemanticIndex = 0
            inputElementDescription.Format = Format.R32G32B32Float
            inputElementDescription.InputSlot = 0
            inputElementDescription.AlignedByteOffset = 0
            inputElementDescription.InputSlotClass = InputClassification.PerVertexData
            inputElementDescription.InstanceDataStepRate = 0

            Dim inputLayout As InputLayout
            Using stream As Stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Render.vs")
                inputLayout = device.CreateInputLayout(New InputElementDescription() {inputElementDescription}, stream)
            End Using
            deviceContext.IA.InputLayout = inputLayout

            ' Open precompiled pixel shader
            ' This file was compiled using: fxc Render.hlsl /T ps_4_0 /EPixShader /FoRender.ps
            Using stream As Stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Render.ps")
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


            deviceContext.IA.SetVertexBuffers(0, New D3DBuffer() {vertexBuffer}, New UInteger() {12}, New UInteger() {0})
            deviceContext.IA.PrimitiveTopology = PrimitiveTopology.TriangleList

            Marshal.FreeCoTaskMem(vertexData)
        End Sub

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

        Private Sub RenderScene()
            If needsResizing Then
                needsResizing = False
                renderTargetView.Dispose()
                Dim sd As SwapChainDescription = swapChain.Description
                swapChain.ResizeBuffers(sd.BufferCount, CUInt(directControl.ClientSize.Width), CUInt(directControl.ClientSize.Height), sd.BufferDescription.Format, sd.Options)
                SetViews()
            End If
            deviceContext.ClearRenderTargetView(renderTargetView, New ColorRgba(0.2F, 0.125F, 0.3F, 1.0F))

            deviceContext.Draw(3, 0)

            swapChain.Present(0, 0)
        End Sub

        Private Sub directControl_SizeChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles directControl.SizeChanged
            needsResizing = True
        End Sub
    End Class
End Namespace
