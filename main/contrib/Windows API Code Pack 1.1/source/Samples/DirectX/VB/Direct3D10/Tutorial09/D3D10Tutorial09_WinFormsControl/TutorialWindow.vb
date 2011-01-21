' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Windows.Forms
Imports System.Windows.Media.Media3D
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D10
Imports Microsoft.WindowsAPICodePack.DirectX.Graphics
Imports Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities


Namespace D3D10Tutorial09_WinFormsControl
	''' <summary>
	''' This application demonstrates the use of meshes
	''' 
	''' http://msdn.microsoft.com/en-us/library/bb172493(VS.85).aspx
	''' 
	''' Copyright (c) Microsoft Corporation. All rights reserved.
	''' </summary>
	Partial Public Class TutorialWindow
		Inherits Form
		#Region "Fields"
		Private device As D3DDevice
		Private swapChain As SwapChain
		Private renderTargetView As RenderTargetView
		Private depthStencil As Texture2D
		Private depthStencilView As DepthStencilView
        Private backColor_Renamed As New ColorRgba(0.0F, 0.125F, 0.3F, 1.0F)

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
		End Sub
		#End Region

		#Region "TutorialWindow_Load()"
		Private Sub TutorialWindow_Load(ByVal sender As Object, ByVal e As EventArgs) Handles MyBase.Load
			InitDevice()
			directControl.Render = AddressOf Me.RenderScene
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

			meshManager = New XMeshManager(device)
			mesh = meshManager.Open("Media\Tiger\tiger.x")

			InitMatrices()
		End Sub
		#End Region

		#Region "InitMatrices()"
		Private Sub InitMatrices()
			' Initialize the view matrix
			Dim Eye As New Vector3F(0.0f, 1.0f, -5.0f)
			Dim At As New Vector3F(0.0f, 0.0f, 0.0f)
			Dim Up As New Vector3F(0.0f, 1.0f, 0.0f)

			Dim viewMatrix As Matrix4x4F
			Dim projectionMatrix As Matrix4x4F
            viewMatrix = Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities.Camera.MatrixLookAtLH(Eye, At, Up)

			' Initialize the projection matrix
            projectionMatrix = Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities.Camera.MatrixPerspectiveFovLH(CSng(Math.PI) * 0.25F, (CSng(Me.ClientSize.Width) / CSng(Me.ClientSize.Height)), 0.5F, 1000.0F)

			meshManager.SetViewAndProjection(viewMatrix, projectionMatrix)
		End Sub
		#End Region

		#Region "RenderScene()"
		''' <summary>
		''' Render the frame
		''' </summary>
		Protected Sub RenderScene()
			t = (Environment.TickCount - dwTimeStart) / 1000.0f

            If (needsResizing) Then
                needsResizing = False
                renderTargetView.Dispose()
                Dim sd As SwapChainDescription = swapChain.Description
                swapChain.ResizeBuffers(sd.BufferCount, CType(directControl.ClientSize.Width, UInteger), CType(directControl.ClientSize.Height, UInteger), sd.BufferDescription.Format, sd.Options)
                SetViews()
                InitMatrices()
            End If

			'WPF transforms used here use degrees as opposed to D3DX which uses radians in the native tutorial
			'360 degrees == 2 * Math.PI
			'world matrix rotates the first cube by t degrees
			Dim rt1 As New RotateTransform3D(New AxisAngleRotation3D(New Vector3D(0, 1, 0), t * 60))

			' Clear the backbuffer
			device.ClearRenderTargetView(renderTargetView, backColor_Renamed)

			' Clear the depth buffer to 1.0 (max depth)
            device.ClearDepthStencilView(depthStencilView, ClearOptions.Depth, 1.0F, CByte(0))

			mesh.Render(rt1.Value.ToMatrix4x4F())

			Dim [error] As Microsoft.WindowsAPICodePack.DirectX.ErrorCode
            swapChain.TryPresent(1, PresentOptions.None, [error])
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
                .Width = CUInt(directControl.ClientSize.Width), _
                .Height = CUInt(directControl.ClientSize.Height), _
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
            Dim vp As New Viewport() With {.Width = CUInt(directControl.ClientSize.Width), .Height = CUInt(directControl.ClientSize.Height), .MinDepth = 0.0F, .MaxDepth = 1.0F, .TopLeftX = 0, .TopLeftY = 0}

            device.RS.Viewports = New Viewport() {vp}
        End Sub
        #End Region

        #Region "directControl_SizeChanged()"
        Private Sub directControl_SizeChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles directControl.SizeChanged
            needsResizing = True
        End Sub
        #End Region
    End Class
End Namespace
