' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Windows
Imports System.Windows.Media.Media3D
Imports Microsoft.WindowsAPICodePack.DirectX.Controls
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D10
Imports Microsoft.WindowsAPICodePack.DirectX.Graphics
Imports Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities


Namespace D3D10Tutorial09_WPF
	''' <summary>
	''' This application demonstrates the use of meshes
	''' 
	''' http://msdn.microsoft.com/en-us/library/bb172493(VS.85).aspx
	''' 
	''' Copyright (c) Microsoft Corporation. All rights reserved.
	''' </summary>
	Partial Public Class TutorialWindow
		Inherits Window
		#Region "Fields"
		Private host As DirectHost
		Private device As D3DDevice
		Private swapChain As SwapChain
		Private renderTargetView As RenderTargetView
		Private depthStencil As Texture2D
		Private depthStencilView As DepthStencilView
		Private backColor As New ColorRgba(0.0F, 0.125F, 0.3F, 1.0F)

		Private mesh As XMesh
		Private meshManager As XMeshManager

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
			host = New DirectHost()
			ControlHostElement.Child = host
		End Sub
		#End Region

		#Region "Window_Loaded()"
		''' <summary>
		''' Handles the Loaded event of the window.
		''' </summary>
		''' <param name="sender">The source of the event.</param>
		''' <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
		Private Sub Window_Loaded(ByVal sender As Object, ByVal e As RoutedEventArgs)
			InitDevice()
			host.Render = AddressOf Me.RenderScene
		End Sub
		#End Region

		#Region "InitDevice()"
		''' <summary>
		''' Create Direct3D device and swap chain
		''' </summary>
		Public Sub InitDevice()
            device = D3DDevice.CreateDeviceAndSwapChain(host.Handle)
            swapChain = device.SwapChain

            SetViews()

			meshManager = New XMeshManager(device)
			mesh = meshManager.Open("Media\Tiger\tiger.x")

			InitMatrices()
		End Sub
		#End Region

        #Region "SetViews()"
        Private Sub SetViews()
            ' Create a render target view
            Using pBuffer As Texture2D = swapChain.GetBuffer(Of Texture2D)(0)
                renderTargetView = device.CreateRenderTargetView(pBuffer)
            End Using

            ' Create depth stencil texture
            Dim descDepth As New Texture2DDescription() With _
            { _
                .Width = CUInt(host.ActualWidth), _
                .Height = CUInt(host.ActualHeight), _
                .MipLevels = 1, _
                .ArraySize = 1, _
                .Format = Format.D32Float, _
                .SampleDescription = New SampleDescription() With {.Count = 1, .Quality = 0}, _
                .BindingOptions = BindingOptions.DepthStencil _
            }

            depthStencil = device.CreateTexture2D(descDepth)

            ' Create the depth stencil view
            Dim depthStencilViewDesc As New DepthStencilViewDescription() With {.Format = descDepth.Format, .ViewDimension = DepthStencilViewDimension.Texture2D}
            depthStencilView = device.CreateDepthStencilView(depthStencil, depthStencilViewDesc)

            'bind the views to the device
            device.OM.RenderTargets = New OutputMergerRenderTargets(New RenderTargetView() {renderTargetView}, depthStencilView)

            ' Setup the viewport
            Dim vp As New Viewport() With {.Width = CUInt(host.ActualWidth), .Height = CUInt(host.ActualHeight), .MinDepth = 0.0F, .MaxDepth = 1.0F, .TopLeftX = 0, .TopLeftY = 0}

            device.RS.Viewports = New Viewport() {vp}
        End Sub
        #End Region

        #Region "InitMatrices()"
        Private Sub InitMatrices()
            ' Initialize the view matrix
            Dim Eye As New Vector3F(0.0F, 1.0F, -5.0F)
            Dim At As New Vector3F(0.0F, 0.0F, 0.0F)
            Dim Up As New Vector3F(0.0F, 1.0F, 0.0F)

            Dim viewMatrix As Matrix4x4F
            Dim projectionMatrix As Matrix4x4F
            viewMatrix = Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities.Camera.MatrixLookAtLH(Eye, At, Up)

            ' Initialize the projection matrix
            projectionMatrix = Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities.Camera.MatrixPerspectiveFovLH(CSng(Math.PI) * 0.25F, (CSng(host.ActualWidth) / CSng(host.ActualHeight)), 0.5F, 1000.0F)

            meshManager.SetViewAndProjection(viewMatrix, projectionMatrix)
        End Sub
#End Region

        #Region "RenderScene()"
        ''' <summary>
        ''' Render the frame
        ''' </summary>
        Protected Sub RenderScene()
            t = (Environment.TickCount - dwTimeStart) / 1000.0F

            If (needsResizing) Then
                needsResizing = False
                renderTargetView.Dispose()
                Dim sd As SwapChainDescription = swapChain.Description
                swapChain.ResizeBuffers(sd.BufferCount, CUInt(host.ActualWidth), CUInt(host.ActualHeight), sd.BufferDescription.Format, sd.Options)
                SetViews()
                InitMatrices()
            End If

            'WPF transforms used here use degrees as opposed to D3DX which uses radians in the native tutorial
            '360 degrees == 2 * Math.PI
            'world matrix rotates the first cube by t degrees
            Dim rt1 As New RotateTransform3D(New AxisAngleRotation3D(New Vector3D(0, 1, 0), t * 60))

            ' Clear the backbuffer
            device.ClearRenderTargetView(renderTargetView, backColor)

            ' Clear the depth buffer to 1.0 (max depth)
            device.ClearDepthStencilView(depthStencilView, ClearOptions.Depth, 1.0F, CByte(0))

            mesh.Render(rt1.Value.ToMatrix4x4F())

            Dim [error] As Microsoft.WindowsAPICodePack.DirectX.ErrorCode
            swapChain.TryPresent(1, PresentOptions.None, [error])
        End Sub
        #End Region

        #Region "ControlHostElement_SizeChanged()"
        Private Sub ControlHostElement_SizeChanged(ByVal sender As System.Object, ByVal e As System.Windows.SizeChangedEventArgs)
            needsResizing = True
        End Sub
        #End Region
    End Class
End Namespace
