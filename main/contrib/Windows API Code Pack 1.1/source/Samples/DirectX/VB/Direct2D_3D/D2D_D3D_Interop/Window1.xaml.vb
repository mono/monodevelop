' Copyright (c) Microsoft Corporation.  All rights reserved.

Imports Microsoft.VisualBasic
Imports System
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Windows
Imports Microsoft.WindowsAPICodePack.DirectX.Direct2D1
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D10
Imports Microsoft.WindowsAPICodePack.DirectX.DirectWrite
Imports Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities
Imports Microsoft.WindowsAPICodePack.DirectX.Graphics
Imports Microsoft.WindowsAPICodePack.DirectX.WindowsImagingComponent

Namespace Microsoft.WindowsAPICodePack.DirectX.Samples
	''' <summary>
	''' Interaction logic for Window1.xaml
	''' </summary>
	Partial Public Class Window1
		#Region "Fields"
		Private syncObject As New Object()
		Private Const HelloWorldText As String = "Hello, World!"
		Private d2DFactory As D2DFactory
		Private imagingFactory As ImagingFactory
		Private dWriteFactory As DWriteFactory

		Private currentTicks As Single
        Private startTime As Nullable(Of Integer)
		Private lastTicks As Integer
		Private fps As Single

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
		Private textureSurface As Surface

		Private backBufferRenderTarget As RenderTarget
		Private backBufferTextBrush As SolidColorBrush
		Private backBufferGradientBrush As LinearGradientBrush
		Private gridPatternBitmapBrush As BitmapBrush

		Private textureRenderTarget As RenderTarget
		Private linearGradientBrush As LinearGradientBrush
		Private blackBrush As SolidColorBrush
		Private d2dBitmap As D2DBitmap

		Private technique As EffectTechnique
		Private worldVariable As EffectMatrixVariable
		Private viewVariable As EffectMatrixVariable
		Private projectionVariable As EffectMatrixVariable
		Private diffuseVariable As EffectShaderResourceVariable

		' Device-Independent Resources
		Private textFormat As TextFormat
		Private textFormatFps As TextFormat
		Private pathGeometry As PathGeometry

		Private worldMatrix As Matrix4x4F
		Private viewMatrix As Matrix4x4F
		Private projectionMatrix As Matrix4x4F

		#Region "Read-only initialization values"
		Private ReadOnly vertexArray As New VertexData()

        Private ReadOnly inputLayouts() As InputElementDescription = _
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

        Private ReadOnly renderTargetProperties As New RenderTargetProperties(RenderTargetType.Default, New PixelFormat(Format.Unknown, AlphaMode.Premultiplied), 96, 96, RenderTargetUsages.None, FeatureLevel.Default)

        Private ReadOnly stopsBackground() As GradientStop = New GradientStop() _
            { _
                New GradientStop(0.0F, New ColorF(GetColorValues(System.Windows.Media.Colors.Blue))), _
                New GradientStop(1.0F, New ColorF(GetColorValues(System.Windows.Media.Colors.Black))) _
            }

        Private ReadOnly stopsGeometry() As GradientStop = New GradientStop() _
            { _
                New GradientStop(0.0F, New ColorF(GetColorValues(System.Windows.Media.Colors.LightBlue))), _
                New GradientStop(1.0F, New ColorF(GetColorValues(System.Windows.Media.Colors.Blue))) _
            }

		#End Region
		#End Region

        Private Shared Function GetColorValues(ByVal color As System.Windows.Media.Color) As Single()
            Return New Single() {color.ScR, color.ScG, color.ScB, color.ScA}
        End Function

		Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub host_Loaded(ByVal sender As Object, ByVal e As RoutedEventArgs)
            CreateDeviceIndependentResources()
            AddHandler host.SizeChanged, AddressOf host_SizeChanged
            host.Render = AddressOf RenderScene
        End Sub

#Region "RenderScene()"
        Private Sub RenderScene()
            SyncLock syncObject
                'initialize D3D device and D2D render targets the first time we get here
                If device Is Nothing Then
                    CreateDeviceResources()
                End If

                'tick count is used to control animation and calculate FPS
                Dim currentTime As Integer = Environment.TickCount
                If Not startTime.HasValue Then
                    startTime = New Nullable(Of Integer)(currentTime)
                End If

                currentTicks = currentTime - startTime.GetValueOrDefault()

                Dim a As Single = (currentTicks * 360.0F) * (CSng(Math.PI) / 180.0F) * 0.0001F
                worldMatrix = MatrixMath.MatrixRotationY(a)

                ' Swap chain will tell us how big the back buffer is
                Dim swapDesc As SwapChainDescription = swapChain.Description
                Dim nWidth As UInteger = swapDesc.BufferDescription.Width
                Dim nHeight As UInteger = swapDesc.BufferDescription.Height

                device.ClearDepthStencilView(depthStencilView, ClearOptions.Depth, 1, 0)

                ' Draw a gradient background before we draw the cube
                If backBufferRenderTarget IsNot Nothing Then
                    backBufferRenderTarget.BeginDraw()

                    backBufferGradientBrush.Transform = Matrix3x2F.Scale(backBufferRenderTarget.Size, New Point2F(0.0F, 0.0F))

                    Dim rect As New RectF(0.0F, 0.0F, nWidth, nHeight)

                    backBufferRenderTarget.FillRectangle(rect, backBufferGradientBrush)
                    backBufferRenderTarget.EndDraw()
                End If

                diffuseVariable.Resource = Nothing
                technique.GetPassByIndex(0).Apply()

                ' Draw the D2D content into a D3D surface.
                RenderD2DContentIntoTexture()

                ' Pass the updated texture to the pixel shader
                diffuseVariable.Resource = textureResourceView

                ' Update variables that change once per frame.
                worldVariable.Matrix = worldMatrix

                ' Set the index buffer.
                device.IA.IndexBuffer = New IndexBuffer(facesIndexBuffer, Format.R16UInt, 0)

                ' Render the scene
                technique.GetPassByIndex(0).Apply()

                device.DrawIndexed(vertexArray.s_FacesIndexArray.Length, 0, 0)

                ' Update fps
                currentTime = Environment.TickCount ' Get the ticks again
                currentTicks = currentTime - startTime.GetValueOrDefault()
                If (currentTime - lastTicks) > 250 Then
                    fps = (swapChain.LastPresentCount) / (currentTicks / 1000.0F)
                    lastTicks = currentTime
                End If

                backBufferRenderTarget.BeginDraw()

                ' Draw fps
                backBufferRenderTarget.DrawText(String.Format("Average FPS: {0:F1}", fps), textFormatFps, New RectF(10.0F, nHeight - 32.0F, nWidth, nHeight), backBufferTextBrush)

                backBufferRenderTarget.EndDraw()

                swapChain.Present(0, Graphics.PresentOptions.None)
            End SyncLock
        End Sub
#End Region

#Region "RenderD2DContentIntoTexture()"
        Private Sub RenderD2DContentIntoTexture()
            Dim rtSize As SizeF = textureRenderTarget.Size

            textureRenderTarget.BeginDraw()

            textureRenderTarget.Transform = Matrix3x2F.Identity
            textureRenderTarget.Clear(New ColorF(GetColorValues(System.Windows.Media.Colors.White)))

            textureRenderTarget.FillRectangle(New RectF(0.0F, 0.0F, rtSize.Width, rtSize.Height), gridPatternBitmapBrush)

            Dim size As SizeF = d2dBitmap.Size

            textureRenderTarget.DrawBitmap(d2dBitmap, 1.0F, BitmapInterpolationMode.Linear, New RectF(0.0F, 0.0F, size.Width, size.Height))

            ' Draw the bitmap at the bottom corner of the window
            textureRenderTarget.DrawBitmap(d2dBitmap, 1.0F, BitmapInterpolationMode.Linear, New RectF(rtSize.Width - size.Width, rtSize.Height - size.Height, rtSize.Width, rtSize.Height))

            ' Set the world transform to rotatate the drawing around the center of the render target
            ' and write "Hello World"
            Dim angle As Single = 0.1F * Environment.TickCount
            textureRenderTarget.Transform = Matrix3x2F.Rotation(angle, New Point2F(rtSize.Width / 2, rtSize.Height / 2))

            textureRenderTarget.DrawText(HelloWorldText, textFormat, New RectF(0, 0, rtSize.Width, rtSize.Height), blackBrush)

            ' Reset back to the identity transform
            textureRenderTarget.Transform = Matrix3x2F.Translation(0, rtSize.Height - 200)

            textureRenderTarget.FillGeometry(pathGeometry, linearGradientBrush)

            textureRenderTarget.Transform = Matrix3x2F.Translation(rtSize.Width - 200, 0)

            textureRenderTarget.FillGeometry(pathGeometry, linearGradientBrush)

            textureRenderTarget.EndDraw()
        End Sub
#End Region

#Region "host_SizeChanged()"
        Private Sub host_SizeChanged(ByVal sender As Object, ByVal e As SizeChangedEventArgs)
            SyncLock syncObject
                If device IsNot Nothing Then
                    Dim nWidth As UInteger = CUInt(host.ActualWidth)
                    Dim nHeight As UInteger = CUInt(host.ActualHeight)

                    backBufferRenderTarget.Dispose()
                    device.OM.RenderTargets = New OutputMergerRenderTargets(New RenderTargetView() {Nothing})
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

                    CreateBackBufferD2DRenderTarget()

                    ' update the aspect ratio
                    projectionMatrix = Camera.MatrixPerspectiveFovLH(CSng(Math.PI) * 0.24F, nWidth / CSng(nHeight), 0.1F, 100.0F)
                    projectionVariable.Matrix = projectionMatrix
                End If
            End SyncLock
        End Sub
#End Region

#Region "CreateDeviceResources()"
        Private Sub CreateDeviceResources()
            Dim nWidth As UInteger = CUInt(host.ActualWidth)
            Dim nHeight As UInteger = CUInt(host.ActualHeight)

            ' Create D3D device and swap chain
            Dim swapDesc As New SwapChainDescription With _
            { _
                .BufferDescription = New ModeDescription() With _
                { _
                    .Width = nWidth, .Height = nHeight, _
                    .Format = Format.R8G8B8A8UNorm, _
                    .RefreshRate = New Rational With {.Numerator = 60, .Denominator = 1} _
                }, _
                .SampleDescription = New SampleDescription() With {.Count = 1, .Quality = 0}, _
                .BufferUsage = UsageOptions.RenderTargetOutput, _
                .BufferCount = 1, _
                .OutputWindowHandle = host.Handle, _
                .Windowed = True _
            }
            device = D3DDevice1.CreateDeviceAndSwapChain1(Nothing, DriverType.Hardware, Nothing, CreateDeviceOptions.SupportBgra, FeatureLevel.NinePointThree, swapDesc)
            swapChain = device.SwapChain

            Using pBuffer As Texture2D = swapChain.GetBuffer(Of Texture2D)(0)
                renderTargetView = device.CreateRenderTargetView(pBuffer)
            End Using

            MakeBothSidesRendered()
            InitializeDepthStencil(nWidth, nHeight)

            device.OM.RenderTargets = New OutputMergerRenderTargets(New RenderTargetView() {renderTargetView}, depthStencilView)

            ' Set a new viewport based on the new dimensions
            SetViewport(nWidth, nHeight)

            ' Load pixel shader
            shader = LoadResourceShader(device, "dxgisample.fxo")

            ' Obtain the technique
            technique = shader.GetTechniqueByName("Render")

            ' Create the input layout
            InitializeGeometryBuffers()

            ' Obtain the variables
            Initialize3DTransformations(nWidth, nHeight)

            ' Allocate a offscreen D3D surface for D2D to render our 2D content into
            InitializeTextureRenderTarget()

            ' Create a D2D render target which can draw into the surface in the swap chain
            CreateD2DRenderTargets()
        End Sub
#End Region

#Region "Initialize3DTransformations()"
        Private Sub Initialize3DTransformations(ByVal nWidth As UInteger, ByVal nHeight As UInteger)
            worldVariable = shader.GetVariableByName("World").AsMatrix()
            viewVariable = shader.GetVariableByName("View").AsMatrix()
            projectionVariable = shader.GetVariableByName("Projection").AsMatrix()
            diffuseVariable = shader.GetVariableByName("txDiffuse").AsShaderResource()

            worldMatrix = Matrix4x4F.Identity

            ' Initialize the view matrix.
            Dim eye As New Vector3F(0.0F, 2.0F, -6.0F)
            Dim at As New Vector3F(0.0F, 0.0F, 0.0F)
            Dim up As New Vector3F(0.0F, 1.0F, 0.0F)
            viewMatrix = Camera.MatrixLookAtLH(eye, at, up)
            viewVariable.Matrix = viewMatrix

            ' Initialize the projection matrix
            projectionMatrix = Camera.MatrixPerspectiveFovLH(CSng(Math.PI) * 0.24F, nWidth / CSng(nHeight), 0.1F, 100.0F)
            projectionVariable.Matrix = projectionMatrix
        End Sub
#End Region

#Region "InitializeTextureRenderTarget()"
        Private Sub InitializeTextureRenderTarget()
            Dim offscreenTextureDesc As Texture2DDescription = New Texture2DDescription With {.ArraySize = 1, .BindingOptions = BindingOptions.RenderTarget Or BindingOptions.ShaderResource, .CpuAccessOptions = 0, .Format = Format.R8G8B8A8UNorm, .Height = 512, .Width = 512, .MipLevels = 1, .MiscellaneousResourceOptions = 0, .SampleDescription = New SampleDescription With {.Count = 1, .Quality = 0}, .Usage = Usage.Default}
            offscreenTexture = device.CreateTexture2D(offscreenTextureDesc)
            ' Convert the Direct2D texture into a Shader Resource View
            textureResourceView = device.CreateShaderResourceView(offscreenTexture)
            textureSurface = offscreenTexture.GraphicsSurface
        End Sub
#End Region

#Region "InitializeGeometryBuffers()"
        Private Sub InitializeGeometryBuffers()
            Dim PassDesc As PassDescription = technique.GetPassByIndex(0).Description

            vertexLayout = device.CreateInputLayout(inputLayouts, PassDesc.InputAssemblerInputSignature, PassDesc.InputAssemblerInputSignatureSize)

            ' Set the input layout
            device.IA.InputLayout = vertexLayout


            Dim bd As New BufferDescription()
            bd.Usage = Usage.Default
            bd.ByteWidth = CUInt(Marshal.SizeOf(vertexArray.s_VertexArray))
            bd.BindingOptions = BindingOptions.VertexBuffer
            bd.CpuAccessOptions = CpuAccessOptions.None
            bd.MiscellaneousResourceOptions = MiscellaneousResourceOptions.None

            Dim ptr As IntPtr = Marshal.AllocHGlobal(Marshal.SizeOf(vertexArray.s_VertexArray))
            Marshal.StructureToPtr(vertexArray.s_VertexArray, ptr, True)
            Dim initData As SubresourceData = New SubresourceData With {.SystemMemory = ptr}
            vertexBuffer = device.CreateBuffer(bd, initData)
            Marshal.FreeHGlobal(ptr)

            ' Set vertex buffer
            Dim stride As UInteger = CUInt(Marshal.SizeOf(GetType(SimpleVertex)))
            Dim offset As UInteger = 0

            device.IA.SetVertexBuffers(0, New D3DBuffer() {vertexBuffer}, New UInteger() {stride}, New UInteger() {offset})

            bd.Usage = Usage.Default
            bd.ByteWidth = CUInt(Marshal.SizeOf(vertexArray.s_FacesIndexArray))
            bd.BindingOptions = BindingOptions.IndexBuffer
            bd.CpuAccessOptions = CpuAccessOptions.None
            bd.MiscellaneousResourceOptions = MiscellaneousResourceOptions.None

            ptr = Marshal.AllocHGlobal(Marshal.SizeOf(vertexArray.s_FacesIndexArray))
            Marshal.StructureToPtr(vertexArray.s_FacesIndexArray, ptr, True)

            initData.SystemMemory = ptr
            facesIndexBuffer = device.CreateBuffer(bd, initData)

            ' Set primitive topology
            device.IA.PrimitiveTopology = PrimitiveTopology.TriangleList
        End Sub
#End Region

#Region "SetViewport()"
        Private Sub SetViewport(ByVal nWidth As UInteger, ByVal nHeight As UInteger)
            Dim viewport As Viewport = New Viewport With {.Width = nWidth, .Height = nHeight, .TopLeftX = 0, .TopLeftY = 0, .MinDepth = 0, .MaxDepth = 1}
            device.RS.Viewports = New Viewport() {viewport}
        End Sub
#End Region

#Region "InitializeDepthStencil()"
        Private Sub InitializeDepthStencil(ByVal nWidth As UInteger, ByVal nHeight As UInteger)
            ' Create depth stencil texture
            Dim descDepth As New Texture2DDescription() With {.Width = nWidth, .Height = nHeight, .MipLevels = 1, .ArraySize = 1, .Format = Format.D16UNorm, .SampleDescription = New SampleDescription() With {.Count = 1, .Quality = 0}, .BindingOptions = BindingOptions.DepthStencil}
            depthStencil = device.CreateTexture2D(descDepth)

            ' Create the depth stencil view
            Dim depthStencilViewDesc As New DepthStencilViewDescription() With {.Format = descDepth.Format, .ViewDimension = DepthStencilViewDimension.Texture2D}
            depthStencilView = device.CreateDepthStencilView(depthStencil, depthStencilViewDesc)
        End Sub
#End Region

#Region "MakeBothSidesRendered()"
        Private Sub MakeBothSidesRendered()
            Dim rsDesc As New RasterizerDescription()
            rsDesc.AntiAliasedLineEnable = False
            rsDesc.CullMode = CullMode.None
            rsDesc.DepthBias = 0
            rsDesc.DepthBiasClamp = 0
            rsDesc.DepthClipEnable = True
            rsDesc.FillMode = Microsoft.WindowsAPICodePack.DirectX.Direct3D10.FillMode.Solid
            rsDesc.FrontCounterclockwise = False ' Must be FALSE for 10on9
            rsDesc.MultisampleEnable = False
            rsDesc.ScissorEnable = False
            rsDesc.SlopeScaledDepthBias = 0
            rasterizerState = device.CreateRasterizerState(rsDesc)

            device.RS.State = rasterizerState
        End Sub
#End Region

#Region "LoadResourceShader()"
        Private Shared Function LoadResourceShader(ByVal device As D3DDevice, ByVal resourceName As String) As Effect
            Using stream As Stream = Application.ResourceAssembly.GetManifestResourceStream(resourceName)
                Return device.CreateEffectFromCompiledBinary(stream)
            End Using
        End Function
#End Region

#Region "CreateD2DRenderTargets()"
        Private Sub CreateD2DRenderTargets()
            ' Create a D2D render target which can draw into our offscreen D3D surface
            textureRenderTarget = d2DFactory.CreateGraphicsSurfaceRenderTarget(textureSurface, renderTargetProperties)

            ' Create a linear gradient brush for the 2D geometry
            Dim gradientStops As GradientStopCollection = textureRenderTarget.CreateGradientStopCollection(stopsGeometry, Gamma.StandardRgb, ExtendMode.Mirror)
            linearGradientBrush = textureRenderTarget.CreateLinearGradientBrush(New LinearGradientBrushProperties(New Point2F(100, 0), New Point2F(100, 200)), gradientStops)

            ' create a black brush
            blackBrush = textureRenderTarget.CreateSolidColorBrush(New ColorF(GetColorValues(System.Windows.Media.Colors.Black)))

            Using stream As Stream = Application.ResourceAssembly.GetManifestResourceStream("tulip.jpg")
                d2dBitmap = BitmapUtilities.LoadBitmapFromStream(textureRenderTarget, imagingFactory, stream)
            End Using

            gridPatternBitmapBrush = CreateGridPatternBrush(textureRenderTarget)
            gridPatternBitmapBrush.Opacity = 0.5F

            CreateBackBufferD2DRenderTarget()
        End Sub
#End Region

#Region "CreateBackBufferD2DRenderTarget()"
        Private Sub CreateBackBufferD2DRenderTarget()
            ' Get a surface in the swap chain
            Using backBufferSurface As Surface = swapChain.GetBuffer(Of Surface)(0)
                backBufferRenderTarget = d2DFactory.CreateGraphicsSurfaceRenderTarget(backBufferSurface, renderTargetProperties)

                Dim stops As GradientStopCollection = backBufferRenderTarget.CreateGradientStopCollection(stopsBackground, Gamma.StandardRgb, ExtendMode.Mirror)
                backBufferGradientBrush = backBufferRenderTarget.CreateLinearGradientBrush(New LinearGradientBrushProperties(New Point2F(0.0F, 0.0F), New Point2F(0.0F, 1.0F)), stops)

                ' Create a red brush for text drawn into the back buffer
                backBufferTextBrush = backBufferRenderTarget.CreateSolidColorBrush(New ColorF(GetColorValues(Windows.Media.Colors.WhiteSmoke)))
            End Using
        End Sub
#End Region

#Region "CreateDeviceIndependentResources()"
        Private Sub CreateDeviceIndependentResources()
            Dim msc_fontName As String = "Verdana"
            Dim msc_fontSize As Single = 50

            Dim fps_fontName As String = "Courier New"
            Dim fps_fontSize As Single = 12

            Dim spSink As GeometrySink

            ' Create D2D factory
            d2DFactory = Direct2D1.D2DFactory.CreateFactory(D2DFactoryType.SingleThreaded)

            ' Create WIC factory
            imagingFactory = WindowsImagingComponent.ImagingFactory.Create()

            ' Create DWrite factory
            dWriteFactory = DirectWrite.DWriteFactory.CreateFactory()

            ' Create DWrite text format object
            textFormat = dWriteFactory.CreateTextFormat(msc_fontName, msc_fontSize)

            textFormat.TextAlignment = Microsoft.WindowsAPICodePack.DirectX.DirectWrite.TextAlignment.Center
            textFormat.ParagraphAlignment = Microsoft.WindowsAPICodePack.DirectX.DirectWrite.ParagraphAlignment.Center


            ' Create DWrite text format object
            textFormatFps = dWriteFactory.CreateTextFormat(fps_fontName, fps_fontSize)

            textFormatFps.TextAlignment = Microsoft.WindowsAPICodePack.DirectX.DirectWrite.TextAlignment.Leading
            textFormatFps.ParagraphAlignment = Microsoft.WindowsAPICodePack.DirectX.DirectWrite.ParagraphAlignment.Near

            ' Create the path geometry.
            pathGeometry = d2DFactory.CreatePathGeometry()

            ' Write to the path geometry using the geometry sink. We are going to create an
            ' hour glass.
            spSink = pathGeometry.Open()

            spSink.SetFillMode(Microsoft.WindowsAPICodePack.DirectX.Direct2D1.FillMode.Alternate)

            spSink.BeginFigure(New Point2F(0, 0), FigureBegin.Filled)

            spSink.AddLine(New Point2F(200, 0))

            spSink.AddBezier(New BezierSegment(New Point2F(150, 50), New Point2F(150, 150), New Point2F(200, 200)))

            spSink.AddLine(New Point2F(0, 200))

            spSink.AddBezier(New BezierSegment(New Point2F(50, 150), New Point2F(50, 50), New Point2F(0, 0)))

            spSink.EndFigure(FigureEnd.Closed)

            spSink.Close()
        End Sub
#End Region

#Region "CreateGridPatternBrush()"
        Private Function CreateGridPatternBrush(ByVal pRenderTarget As RenderTarget) As BitmapBrush
            ' Create a compatible render target.
            Dim spCompatibleRenderTarget As BitmapRenderTarget = pRenderTarget.CreateCompatibleRenderTarget(CompatibleRenderTargetOptions.None, (New SizeF(10.0F, 10.0F)))

            ' Draw a pattern.
            Dim spGridBrush As SolidColorBrush = spCompatibleRenderTarget.CreateSolidColorBrush(New ColorF(0.93F, 0.94F, 0.96F, 1.0F))

            spCompatibleRenderTarget.BeginDraw()

            spCompatibleRenderTarget.FillRectangle(New RectF(0.0F, 0.0F, 10.0F, 1.0F), spGridBrush)
            spCompatibleRenderTarget.FillRectangle(New RectF(0.0F, 0.1F, 1.0F, 10.0F), spGridBrush)
            spCompatibleRenderTarget.EndDraw()

            ' Retrieve the bitmap from the render target.
            Dim spGridBitmap As D2DBitmap = spCompatibleRenderTarget.Bitmap

            ' Choose the tiling mode for the bitmap brush.
            Dim brushProperties As New BitmapBrushProperties(ExtendMode.Wrap, ExtendMode.Wrap, BitmapInterpolationMode.Linear)

            ' Create the bitmap brush.
            Return textureRenderTarget.CreateBitmapBrush(spGridBitmap, brushProperties)
        End Function
#End Region
    End Class
End Namespace
