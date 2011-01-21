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

        Private device As D3DDevice
        Private deviceContext As DeviceContext
        Private renderTargetView As RenderTargetView
        Private pixelShader As PixelShader
        Private vertexShader As VertexShader
        Private vertexBuffer As D3DBuffer
        Private swapChain As SwapChain

		Public Sub New()
			InitializeComponent()

			SetStyle(ControlStyles.UserPaint, True)
			SetStyle(ControlStyles.AllPaintingInWmPaint, True)
			UpdateStyles()

		End Sub

        Protected Overrides Sub OnMouseDoubleClick(ByVal e As MouseEventArgs)
            If Not swapChain Is Nothing Then
                swapChain.IsFullScreen = Not swapChain.IsFullScreen
            End If

        End Sub

		Protected Overrides Sub OnShown(ByVal e As EventArgs)
			MyBase.OnShown(e)

			' device creation
            device = D3DDevice.CreateDeviceAndSwapChain(Handle)
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

			' Open precompiled vertex shader
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


			deviceContext.IA.SetVertexBuffers(0, New D3DBuffer() { vertexBuffer }, New UInteger() { 12 }, New UInteger() { 0 })
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

		Protected Overrides Sub OnPaintBackground(ByVal e As PaintEventArgs)
			' Do not paint to prevent flickering
		End Sub

		Protected Overrides Sub OnPaint(ByVal e As PaintEventArgs)
			' Not required unless we need other controls!
			' base.OnPaint(e);  

			deviceContext.ClearRenderTargetView(renderTargetView, New ColorRgba(0.2f, 0.125f, 0.3f, 1.0f))

			deviceContext.Draw(3, 0)

			swapChain.Present(0, 0)
		End Sub

		Protected Overrides Sub OnClosed(ByVal e As EventArgs)
			' dispose all the DirectX bits

			deviceContext.ClearState()
			deviceContext.Flush()


			If vertexBuffer IsNot Nothing Then
				vertexBuffer.Dispose()
			End If

			If vertexShader IsNot Nothing Then
				vertexShader.Dispose()
			End If

			If pixelShader IsNot Nothing Then
				pixelShader.Dispose()
			End If

			If renderTargetView IsNot Nothing Then
				renderTargetView.Dispose()
			End If

			If device IsNot Nothing Then
				device.Dispose()
			End If

			MyBase.OnClosed(e)
		End Sub

        Private Sub Form1_SizeChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.SizeChanged
            If renderTargetView IsNot Nothing Then
                renderTargetView.Dispose()
                Dim sd As SwapChainDescription = swapChain.Description
                swapChain.ResizeBuffers(sd.BufferCount, CUInt(Me.ClientSize.Width), CUInt(Me.ClientSize.Height), sd.BufferDescription.Format, sd.Options)
                SetViews()
                Invalidate()
            End If
        End Sub
    End Class
End Namespace
