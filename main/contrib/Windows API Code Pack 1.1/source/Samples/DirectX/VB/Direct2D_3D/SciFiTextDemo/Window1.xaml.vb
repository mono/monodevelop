' Copyright (c) Microsoft Corporation.  All rights reserved.

Imports Microsoft.VisualBasic
Imports System
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Windows

Imports Microsoft.WindowsAPICodePack.DirectX
Imports Microsoft.WindowsAPICodePack.DirectX.Direct2D1
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D10
Imports Microsoft.WindowsAPICodePack.DirectX.DirectWrite
Imports Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities
Imports Microsoft.WindowsAPICodePack.DirectX.Graphics


Namespace SciFiTextDemo
	''' <summary>
	''' Interaction logic for Window1.xaml
	''' </summary>
	Partial Public Class Window1
		Private syncObject As New Object()

		Private text As String = "Episode CCCXLVII:" & Constants.vbLf & "A Misguided Hope" & Constants.vbLf + Constants.vbLf & "Not so long ago, in a cubicle not so far away..." & Constants.vbLf + Constants.vbLf & "It is days before milestone lockdown. A small group of rebel developers toil through the weekend, relentlessly fixing bugs in defiance of familial obligations. Aside from pride in their work, their only reward will be takeout food and cinema gift certificates." & Constants.vbLf + Constants.vbLf & "Powered by coffee and soda, our hyper-caffeinated heroine stares at her screen with glazed-over eyes. She repeatedly slaps her face in a feeble attempt to stay awake. Lapsing into micro-naps, she reluctantly takes a break from debugging to replenish her caffeine levels." & Constants.vbLf + Constants.vbLf & "On her way to the kitchen she spots a fallen comrade, passed out on his keyboard and snoring loudly. After downing two coffees, she fills a pitcher with ice water and..."

		' The factories
		Private d2DFactory As D2DFactory
		Private dWriteFactory As DWriteFactory

		Private pause As Boolean
		Private lastSavedDelta As Integer

		'Device-Dependent Resources
		Private device As D3DDevice1
		Private swapChain As SwapChain
		Private rasterizerState As RasterizerState
		Private depthStencil As Texture2D
		Private depthStencilView As DepthStencilView
		Private renderTargetView As RenderTargetView
		Private offscreenTexture As Texture2D
		Private shader As Effect
		Private vertexBuffer As D3DBuffer
		Private vertexLayout As InputLayout
		Private facesIndexBuffer As D3DBuffer
		Private textureResourceView As ShaderResourceView

		Private renderTarget As RenderTarget
		Private textBrush As LinearGradientBrush

		Private opacityRenderTarget As BitmapRenderTarget
		Private isOpacityRTPopulated As Boolean

		Private technique As EffectTechnique
		Private worldMatrixVariable As EffectMatrixVariable
		Private viewMatrixVariable As EffectMatrixVariable
		Private projectionMarixVariable As EffectMatrixVariable
		Private diffuseVariable As EffectShaderResourceVariable

		' Device-Independent Resources
		Private textFormat As TextFormat

		Private worldMatrix As Matrix4x4F
		Private viewMatrix As Matrix4x4F
		Private projectionMatrix As Matrix4x4F

        Private backColor As New ColorRgba(GetColorValues(System.Windows.Media.Colors.Black))

		Private currentTimeVariation As Single
		Private startTime As Integer = Environment.TickCount

        Private inputLayoutDescriptions() As InputElementDescription = _
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
            New InputElementDescription With _
            { _
                .SemanticName = "TEXCOORD", _
                .SemanticIndex = 0, _
                .Format = Format.R32G32Float, _
                .InputSlot = 0, _
                .AlignedByteOffset = 12, _
                .InputSlotClass = InputClassification.PerVertexData, _
                .InstanceDataStepRate = 0 _
            } _
        }

		Private VertexArray As New VertexData()

        Private Shared Function GetColorValues(ByVal color As System.Windows.Media.Color) As Single()
            Return New Single() {color.ScR, color.ScG, color.ScB, color.ScA}
        End Function

		Public Sub New()
			InitializeComponent()
			textBox.Text = text
			AddHandler host.Loaded, AddressOf host_Loaded
			AddHandler host.SizeChanged, AddressOf host_SizeChanged
		End Sub

		Private Sub host_SizeChanged(ByVal sender As Object, ByVal e As SizeChangedEventArgs)
			SyncLock syncObject
				If device Is Nothing Then
					Return
				End If
				Dim nWidth As UInteger = CUInt(host.ActualWidth)
				Dim nHeight As UInteger = CUInt(host.ActualHeight)

                device.OM.RenderTargets = New OutputMergerRenderTargets(New RenderTargetView() {Nothing}, Nothing)
				'need to remove the reference to the swapchain's backbuffer to enable ResizeBuffers() call
				renderTargetView.Dispose()
				depthStencilView.Dispose()
				depthStencil.Dispose()

                device.RS.Viewports = Nothing

				Dim sd As SwapChainDescription = swapChain.Description
				'Change the swap chain's back buffer size, format, and number of buffers
                swapChain.ResizeBuffers(sd.BufferCount, nWidth, nHeight, sd.BufferDescription.Format, sd.Options)

				Using pBuffer As Texture2D = swapChain.GetBuffer(Of Texture2D)(0)
					renderTargetView = device.CreateRenderTargetView(pBuffer)
				End Using

				InitializeDepthStencil(nWidth, nHeight)

				' bind the views to the device
                device.OM.RenderTargets = New OutputMergerRenderTargets(New RenderTargetView() {renderTargetView}, depthStencilView)

				SetViewport(nWidth, nHeight)

				' update the aspect ratio
				projectionMatrix = Camera.MatrixPerspectiveFovLH(CSng(Math.PI) * 0.1f, nWidth / CSng(nHeight), 0.1f, 100.0f)
                projectionMarixVariable.Matrix = projectionMatrix
			End SyncLock
		End Sub

		Private Sub host_Loaded(ByVal sender As Object, ByVal e As RoutedEventArgs)
			CreateDeviceIndependentResources()
			startTime = Environment.TickCount
			host.Render = AddressOf RenderScene
		End Sub

		Private Shared Function LoadResourceShader(ByVal device As D3DDevice, ByVal resourceName As String) As Effect
			Using stream As Stream = Application.ResourceAssembly.GetManifestResourceStream(resourceName)
				Return device.CreateEffectFromCompiledBinary(stream)
			End Using
		End Function

		Private Sub CreateDeviceIndependentResources()
			' Create a Direct2D factory.
			d2DFactory = D2DFactory.CreateFactory(D2DFactoryType.SingleThreaded)

			' Create a DirectWrite factory.
			dWriteFactory = DWriteFactory.CreateFactory()

			' Create a DirectWrite text format object.
            textFormat = dWriteFactory.CreateTextFormat("Calibri", 50, Microsoft.WindowsAPICodePack.DirectX.DirectWrite.FontWeight.Bold, Microsoft.WindowsAPICodePack.DirectX.DirectWrite.FontStyle.Normal, Microsoft.WindowsAPICodePack.DirectX.DirectWrite.FontStretch.Normal)

			' Center the text both horizontally and vertically.
            textFormat.TextAlignment = Microsoft.WindowsAPICodePack.DirectX.DirectWrite.TextAlignment.Leading
			textFormat.ParagraphAlignment = ParagraphAlignment.Near
		End Sub

		Private Sub CreateDeviceResources()
			Dim width As UInteger = CUInt(host.ActualWidth)
			Dim height As UInteger = CUInt(host.ActualHeight)

			' If we don't have a device, need to create one now and all
			' accompanying D3D resources.
			CreateDevice()

            Dim dxgiFactory As Factory = Factory.Create()

            Dim swapDesc As New SwapChainDescription With _
            { _
                .BufferDescription = New ModeDescription With _
                { _
                    .Width = width, .Height = height, _
                    .Format = Format.R8G8B8A8UNorm, _
                    .RefreshRate = New Rational With {.Numerator = 60, .Denominator = 1} _
                }, _
                .SampleDescription = New SampleDescription With {.Count = 1, .Quality = 0}, _
                .BufferUsage = UsageOptions.RenderTargetOutput, _
                .BufferCount = 1, _
                .OutputWindowHandle = host.Handle, _
                .Windowed = True _
            }

			swapChain = dxgiFactory.CreateSwapChain(device, swapDesc)

			' Create rasterizer state object
			Dim rsDesc As New RasterizerDescription()
			rsDesc.AntialiasedLineEnable = False
			rsDesc.CullMode = CullMode.None
			rsDesc.DepthBias = 0
			rsDesc.DepthBiasClamp = 0
			rsDesc.DepthClipEnable = True
            rsDesc.FillMode = Microsoft.WindowsAPICodePack.DirectX.Direct3D10.FillMode.Solid
			rsDesc.FrontCounterClockwise = False ' Must be FALSE for 10on9
			rsDesc.MultisampleEnable = False
			rsDesc.ScissorEnable = False
			rsDesc.SlopeScaledDepthBias = 0

			rasterizerState = device.CreateRasterizerState(rsDesc)

            device.RS.State = rasterizerState

			' If we don't have a D2D render target, need to create all of the resources
			' required to render to one here.
			' Ensure that nobody is holding onto one of the old resources
            device.OM.RenderTargets = New OutputMergerRenderTargets(New RenderTargetView() {Nothing})

			InitializeDepthStencil(width, height)

			' Create views on the RT buffers and set them on the device
            Dim renderDesc As New RenderTargetViewDescription()
            Dim renderView As Texture2DRenderTargetView

            renderDesc.Format = Format.R8G8B8A8UNorm
            renderDesc.ViewDimension = RenderTargetViewDimension.Texture2D

            renderView = renderDesc.Texture2D
            renderView.MipSlice = 0
            renderDesc.Texture2D = renderView

			Using spBackBufferResource As D3DResource = swapChain.GetBuffer(Of D3DResource)(0)
				renderTargetView = device.CreateRenderTargetView(spBackBufferResource, renderDesc)
			End Using

            device.OM.RenderTargets = New OutputMergerRenderTargets(New RenderTargetView() {renderTargetView}, depthStencilView)

			SetViewport(width, height)


			' Create a D2D render target which can draw into the surface in the swap chain
            Dim props As New RenderTargetProperties(RenderTargetType.Default, New PixelFormat(Format.Unknown, AlphaMode.Premultiplied), 96, 96, RenderTargetUsages.None, FeatureLevel.Default)

			' Allocate a offscreen D3D surface for D2D to render our 2D content into
            Dim tex2DDescription As New Texture2DDescription With _
            { _
                .ArraySize = 1, _
                .BindingOptions = BindingOptions.RenderTarget Or BindingOptions.ShaderResource, _
                .CpuAccessOptions = CpuAccessOptions.None, _
                .Format = Format.R8G8B8A8UNorm, _
                .Height = 4096, _
                .Width = 512, _
                .MipLevels = 1, _
                .MiscellaneousResourceOptions = MiscellaneousResourceOptions.None, _
                .SampleDescription = New SampleDescription With {.Count = 1, .Quality = 0}, _
                .Usage = Usage.Default _
            }

			offscreenTexture = device.CreateTexture2D(tex2DDescription)

            Using dxgiSurface As Surface = offscreenTexture.GraphicsSurface()
                ' Create a D2D render target which can draw into our offscreen D3D surface
                renderTarget = d2DFactory.CreateGraphicsSurfaceRenderTarget(dxgiSurface, props)
            End Using

            Dim alphaOnlyFormat As New PixelFormat(Format.A8UNorm, AlphaMode.Premultiplied)

			opacityRenderTarget = renderTarget.CreateCompatibleRenderTarget(CompatibleRenderTargetOptions.None, alphaOnlyFormat)

			' Load pixel shader
			' Open precompiled vertex shader
			' This file was compiled using DirectX's SDK Shader compilation tool: 
			' fxc.exe /T fx_4_0 /Fo SciFiText.fxo SciFiText.fx
            shader = LoadResourceShader(device, "SciFiText.fxo")

			' Obtain the technique
			technique = shader.GetTechniqueByName("Render")

			' Obtain the variables
			worldMatrixVariable = shader.GetVariableByName("World").AsMatrix()
			viewMatrixVariable = shader.GetVariableByName("View").AsMatrix()
			projectionMarixVariable = shader.GetVariableByName("Projection").AsMatrix()
			diffuseVariable = shader.GetVariableByName("txDiffuse").AsShaderResource()

			' Create the input layout
			Dim passDesc As New PassDescription()
			passDesc = technique.GetPassByIndex(0).Description

			vertexLayout = device.CreateInputLayout(inputLayoutDescriptions, passDesc.InputAssemblerInputSignature, passDesc.InputAssemblerInputSignatureSize)

			' Set the input layout
            device.IA.InputLayout = vertexLayout

			Dim verticesDataPtr As IntPtr = Marshal.AllocHGlobal(Marshal.SizeOf(VertexArray.VerticesInstance))
			Marshal.StructureToPtr(VertexArray.VerticesInstance, verticesDataPtr, True)

			Dim bd As New BufferDescription()
			bd.Usage = Usage.Default
			bd.ByteWidth = CUInt(Marshal.SizeOf(VertexArray.VerticesInstance))
            bd.BindingOptions = BindingOptions.VertexBuffer
            bd.CpuAccessOptions = CpuAccessOptions.None
            bd.MiscellaneousResourceOptions = MiscellaneousResourceOptions.None

            Dim InitData As New SubresourceData() With {.SystemMemory = verticesDataPtr}

            vertexBuffer = device.CreateBuffer(bd, InitData)

			Marshal.FreeHGlobal(verticesDataPtr)

			' Set vertex buffer
			Dim stride As UInteger = CUInt(Marshal.SizeOf(GetType(SimpleVertex)))
			Dim offset As UInteger = 0

			device.IA.SetVertexBuffers(0, New D3DBuffer() {vertexBuffer}, New UInteger() {stride}, New UInteger() {offset})

			Dim indicesDataPtr As IntPtr = Marshal.AllocHGlobal(Marshal.SizeOf(VertexArray.IndicesInstance))
			Marshal.StructureToPtr(VertexArray.IndicesInstance, indicesDataPtr, True)

			bd.Usage = Usage.Default
			bd.ByteWidth = CUInt(Marshal.SizeOf(VertexArray.IndicesInstance))
            bd.BindingOptions = BindingOptions.IndexBuffer
            bd.CpuAccessOptions = CpuAccessOptions.None
            bd.MiscellaneousResourceOptions = MiscellaneousResourceOptions.None

            InitData.SystemMemory = indicesDataPtr

			facesIndexBuffer = device.CreateBuffer(bd, InitData)

			Marshal.FreeHGlobal(indicesDataPtr)

			' Set primitive topology
            device.IA.PrimitiveTopology = PrimitiveTopology.TriangleList

			' Convert the D2D texture into a Shader Resource View
			textureResourceView = device.CreateShaderResourceView(offscreenTexture)

			' Initialize the world matrices
            worldMatrix = Matrix4x4F.Identity

			' Initialize the view matrix
			Dim Eye As New Vector3F(0.0f, 0.0f, 13.0f)
			Dim At As New Vector3F(0.0f, -3.5f, 45.0f)
			Dim Up As New Vector3F(0.0f, 1.0f, 0.0f)

			viewMatrix = Camera.MatrixLookAtLH(Eye, At, Up)

			' Initialize the projection matrix
			projectionMatrix = Camera.MatrixPerspectiveFovLH(CSng(Math.PI)*0.1f, width/CSng(height), 0.1f, 100.0f)

			' Update Variables that never change
            viewMatrixVariable.Matrix = viewMatrix

            projectionMarixVariable.Matrix = projectionMatrix

            Dim gradientStops() As GradientStop = {New GradientStop(0.0F, New ColorF(GetColorValues(System.Windows.Media.Colors.Yellow))), New GradientStop(1.0F, New ColorF(GetColorValues(System.Windows.Media.Colors.Black)))}

            Dim spGradientStopCollection As GradientStopCollection = renderTarget.CreateGradientStopCollection(gradientStops, Gamma.StandardRgb, ExtendMode.Clamp)

			' Create a linear gradient brush for text
			textBrush = renderTarget.CreateLinearGradientBrush(New LinearGradientBrushProperties(New Point2F(0, 0), New Point2F(0, -2048)), spGradientStopCollection)
		End Sub

		Private Sub CreateDevice()
			Try
				' Create device
                device = D3DDevice1.CreateDevice1(Nothing, DriverType.Hardware, Nothing, CreateDeviceOptions.SupportBgra, FeatureLevel.Ten)
			Catch e1 As Exception
				' if we can't create a hardware device,
				' try the warp one
			End Try
			If device Is Nothing Then
                device = D3DDevice1.CreateDevice1(Nothing, DriverType.Software, "d3d10warp.dll", CreateDeviceOptions.SupportBgra, FeatureLevel.Ten)
			End If
		End Sub

		Private Sub RenderScene()
			SyncLock syncObject
				If device Is Nothing Then
					CreateDeviceResources()
				End If

				If Not pause Then
					If lastSavedDelta <> 0 Then
						startTime = Environment.TickCount - lastSavedDelta
						lastSavedDelta = 0
					End If
					currentTimeVariation = (Environment.TickCount - startTime)/6000.0f
					worldMatrix = MatrixMath.MatrixTranslate(0, 0, currentTimeVariation)
					textBrush.Transform = Matrix3x2F.Translation(0, (4096f/16f)*currentTimeVariation)
				End If

                device.ClearDepthStencilView(depthStencilView, ClearOptions.Depth, 1, 0)

				' Clear the back buffer
				device.ClearRenderTargetView(renderTargetView, backColor)

                diffuseVariable.Resource = Nothing

				technique.GetPassByIndex(0).Apply()

				' Draw the D2D content into our D3D surface
				RenderD2DContentIntoSurface()

                diffuseVariable.Resource = textureResourceView

				' Update variables
                worldMatrixVariable.Matrix = worldMatrix

				' Set index buffer
                device.IA.IndexBuffer = New IndexBuffer(facesIndexBuffer, Format.R16UInt, 0)

				' Draw the scene
				technique.GetPassByIndex(0).Apply()

				device.DrawIndexed(CUInt(Marshal.SizeOf(VertexArray.VerticesInstance)), 0, 0)

                swapChain.Present(0, Graphics.PresentOptions.None)
			End SyncLock
		End Sub

		Private Sub RenderD2DContentIntoSurface()
			Dim rtSize As SizeF = renderTarget.Size

			renderTarget.BeginDraw()

			If Not isOpacityRTPopulated Then
				opacityRenderTarget.BeginDraw()

				opacityRenderTarget.Transform = Matrix3x2F.Identity

                opacityRenderTarget.Clear(New ColorF(GetColorValues(System.Windows.Media.Colors.Black), 0))

				opacityRenderTarget.DrawText(text, textFormat, New RectF(0, 0, rtSize.Width, rtSize.Height), textBrush)

				opacityRenderTarget.EndDraw()

				isOpacityRTPopulated = True
			End If

            renderTarget.Clear(New ColorF(GetColorValues(System.Windows.Media.Colors.Black)))

			renderTarget.AntialiasMode = AntialiasMode.Aliased

            Dim spBitmap As D2DBitmap = opacityRenderTarget.Bitmap

			renderTarget.FillOpacityMask(spBitmap, textBrush, OpacityMaskContent.TextNatural, New RectF(0, 0, rtSize.Width, rtSize.Height), New RectF(0, 0, rtSize.Width, rtSize.Height))

			renderTarget.EndDraw()
		End Sub

		#Region "InitializeDepthStencil()"
		Private Sub InitializeDepthStencil(ByVal nWidth As UInteger, ByVal nHeight As UInteger)
			' Create depth stencil texture
            Dim descDepth As New Texture2DDescription() With _
            { _
                .Width = nWidth, _
                .Height = nHeight, _
                .MipLevels = 1, _
                .ArraySize = 1, _
                .Format = Format.D16UNorm, _
                .SampleDescription = New SampleDescription() With _
                { _
                    .Count = 1, _
                    .Quality = 0 _
                }, _
                .BindingOptions = BindingOptions.DepthStencil _
            }

			depthStencil = device.CreateTexture2D(descDepth)

			' Create the depth stencil view
			Dim depthStencilViewDesc As New DepthStencilViewDescription() With {.Format = descDepth.Format, .ViewDimension = DepthStencilViewDimension. Texture2D}
			depthStencilView = device.CreateDepthStencilView(depthStencil, depthStencilViewDesc)
		End Sub
		#End Region

		#Region "SetViewport()"
		Private Sub SetViewport(ByVal nWidth As UInteger, ByVal nHeight As UInteger)
			Dim viewport As Viewport = New Viewport With {.Width = nWidth, .Height = nHeight, .TopLeftX = 0, .TopLeftY = 0, .MinDepth = 0, .MaxDepth = 1}
            device.RS.Viewports = New Viewport() {viewport}
		End Sub
		#End Region

		Private Sub Button_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
			pause = Not pause

			If pause Then
				lastSavedDelta = Environment.TickCount - startTime
				actionText.Text = "Resume Text"
			Else
				actionText.Text = "Pause Text"
			End If
		End Sub

		Private Sub textBox_TextChanged(ByVal sender As Object, ByVal e As System.Windows.Controls.TextChangedEventArgs)
			text = textBox.Text
			isOpacityRTPopulated = False
		End Sub
	End Class
End Namespace
