' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Media.Media3D
Imports System.Windows.Threading
Imports Microsoft.WindowsAPICodePack.Controls
Imports Microsoft.WindowsAPICodePack.DirectX.Controls
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D10
Imports Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities
Imports Microsoft.WindowsAPICodePack.DirectX.Graphics
Imports Microsoft.WindowsAPICodePack.Shell

Namespace TextureSwap
	''' <summary>
	''' Interaction logic for Window1.xaml
	''' </summary>
	Partial Public Class TexturizerDemo
		Inherits Window
		#Region "instance data"
		Private renderHost As RenderHost
		Private device As D3DDevice
		Private swapChain As SwapChain
		Private renderTargetView As RenderTargetView
		Private depthStencil As Texture2D
		Private depthStencilView As DepthStencilView
		Private backgroundColor As New ColorRgba(0.5F, 0.625F, 0.75F, 1.0F)
		Private camera As PerspectiveCamera = Nothing

		Private meshManager As XMeshManager
		Private mesh As Texturizer

		Private modelTransformGroup As New Transform3DGroup()
		Private xAxisRotation As New AxisAngleRotation3D(New Vector3D(1, 0, 0), 0)
		Private yAxisRotation As New AxisAngleRotation3D(New Vector3D(0, 1, 0), 0)
		Private zAxisRotation As New AxisAngleRotation3D(New Vector3D(0, 0, 1), 0)
		Private modelZoom As New ScaleTransform3D()

		Private timer As New DispatcherTimer()
		#End Region

		#Region "construction"
		Public Sub New()
			InitializeComponent()

			renderHost = New RenderHost()
			ControlHostElement.Child = renderHost

			AddHandler Loaded, AddressOf Window1_Loaded
			AddHandler SizeChanged, AddressOf Window1_SizeChanged
		End Sub
		#End Region

		#Region "D3D Device Initialization"
		Private Sub InitDevice()
			' create Direct 3D device
            device = D3DDevice.CreateDeviceAndSwapChain(renderHost.Handle)
            swapChain = device.SwapChain

			' Create a render target view
			Using pBuffer As Texture2D = swapChain.GetBuffer(Of Texture2D)(0)
				renderTargetView = device.CreateRenderTargetView(pBuffer)
			End Using

			' Create depth stencil texture
            Dim descDepth As New Texture2DDescription() With _
            { _
                .Width = CUInt(renderHost.ActualWidth), _
                .Height = CUInt(renderHost.ActualHeight), _
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

			' bind the views to the device
            device.OM.RenderTargets = New OutputMergerRenderTargets(New RenderTargetView() {renderTargetView}, depthStencilView)

			' Setup the viewport
			Dim vp As New Viewport() With {.Width = CUInt(renderHost.ActualWidth), .Height = CUInt(renderHost.ActualHeight), .MinDepth = 0.0f, .MaxDepth = 1.0f, .TopLeftX = 0, .TopLeftY = 0}

            device.RS.Viewports = New Viewport() {vp}
		End Sub
		#End Region

		#Region "Scene Initialization"
		Private Sub InitScene()
			' load mesh
			meshManager = New XMeshManager(device)
			mesh = meshManager.Open(Of Texturizer)("Resources\airplane 2.x")

			' initialize camera
			camera = New PerspectiveCamera(New Point3D(0, 0, -10), New Vector3D(0, 1, 0), New Vector3D(0, 1, 0), 45)
			camera.NearPlaneDistance =.1
			camera.FarPlaneDistance = 500

			' initialize camera transforms
			modelTransformGroup.Children.Add(modelZoom)
			modelTransformGroup.Children.Add(New RotateTransform3D(yAxisRotation))
			modelTransformGroup.Children.Add(New RotateTransform3D(xAxisRotation))
			modelTransformGroup.Children.Add(New RotateTransform3D(zAxisRotation))
		End Sub
		#End Region

		#Region "Rendering"
		Private Sub timer_Tick(ByVal sender As Object, ByVal e As EventArgs)
			RenderScene()
		End Sub

		Protected Sub RenderScene()
			' update view variables 
			xAxisRotation.Angle = XAxisSlider.Value
            yAxisRotation.Angle = -YAxisSlider.Value
			zAxisRotation.Angle = ZAxisSlider.Value
			modelZoom.ScaleX = ZoomSlider.Value / 2
			modelZoom.ScaleY = ZoomSlider.Value / 2
			modelZoom.ScaleZ = ZoomSlider.Value / 2

			' update view 
			meshManager.SetViewAndProjection(camera.ToViewLH().ToMatrix4x4F(), camera.ToPerspectiveLH(renderHost.ActualWidth / renderHost.ActualHeight).ToMatrix4x4F())

			' clear render target
			device.ClearRenderTargetView(renderTargetView, backgroundColor)

			' Clear the depth buffer to 1.0 (max depth)
            device.ClearDepthStencilView(depthStencilView, ClearOptions.Depth, 1.0F, CByte(0))

			' render mesh
			mesh.LightIntensity = 2.5f
			mesh.Render(modelTransformGroup.Value)

			' present back buffer
            swapChain.Present(1, PresentOptions.None)
		End Sub
		#End Region

		#Region "UI event handlers"
		Private Sub Window1_Loaded(ByVal sender As Object, ByVal e As RoutedEventArgs)
			InitDevice()

			InitScene()

			PartsList.ItemsSource = mesh.GetParts()
			PartsList.SelectedIndex = 0

			TextureBrowser.NavigationPane = PaneVisibilityState.Show
			TextureBrowser.NavigationTarget = CType(KnownFolders.PicturesLibrary, ShellObject)

			timer.Interval = TimeSpan.FromMilliseconds(30)
			AddHandler timer.Tick, AddressOf timer_Tick
			timer.Start()

			ShowOneTextureCheck.IsChecked = True
		End Sub

		Private Sub Window1_SizeChanged(ByVal sender As Object, ByVal e As SizeChangedEventArgs)
			If device IsNot Nothing Then
				'need to remove the reference to the swapchain's backbuffer to enable ResizeBuffers() call
				renderTargetView.Dispose()
				Dim sd As SwapChainDescription = swapChain.Description
                swapChain.ResizeBuffers(sd.BufferCount, CUInt(renderHost.ActualWidth), CUInt(renderHost.ActualHeight), sd.BufferDescription.Format, sd.Options)

				Using pBuffer As Texture2D = swapChain.GetBuffer(Of Texture2D)(0)
					renderTargetView = device.CreateRenderTargetView(pBuffer)
				End Using

				' Create depth stencil texture
                Dim descDepth As New Texture2DDescription() With {.Width = CUInt(renderHost.ActualWidth), .Height = CUInt(renderHost.ActualHeight), .MipLevels = 1, .ArraySize = 1, .Format = Format.D32Float, .SampleDescription = New SampleDescription() With {.Count = 1, .Quality = 0}, .BindingOptions = BindingOptions.DepthStencil}

				depthStencil = device.CreateTexture2D(descDepth)

				' Create the depth stencil view
				Dim depthStencilViewDesc As New DepthStencilViewDescription() With {.Format = descDepth.Format, .ViewDimension = DepthStencilViewDimension.Texture2D}
				depthStencilView = device.CreateDepthStencilView(depthStencil, depthStencilViewDesc)

				' bind the views to the device
                device.OM.RenderTargets = New OutputMergerRenderTargets(New RenderTargetView() {renderTargetView}, depthStencilView)

				' Setup the viewport
				Dim vp As New Viewport() With {.Width = CUInt(renderHost.ActualWidth), .Height = CUInt(renderHost.ActualHeight), .MinDepth = 0.0f, .MaxDepth = 1.0f, .TopLeftX = 0, .TopLeftY = 0}

                device.RS.Viewports = New Viewport() {vp}
			End If
		End Sub

		Private Sub PartsList_SelectionChanged(ByVal sender As Object, ByVal e As SelectionChangedEventArgs)
			mesh.PartToTexture(CStr(PartsList.SelectedItem))
		End Sub

		Private Sub Button_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
			If TextureBrowser.SelectedItems.Count > 0 Then
				Dim item As ShellObject = TextureBrowser.SelectedItems(0)
				Try
					Dim file As ShellFile = CType(item, ShellFile)
					mesh.SwapTexture(CStr(PartsList.SelectedItem), file.Path)
				Catch e1 As InvalidCastException
					' throws when item is not a shell file 
				End Try
			End If
		End Sub

		Private Sub RevertTextures_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
			mesh.RevertTextures()
		End Sub

		Private Sub ShowOneTexture_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
			mesh.ShowOneTexture = CBool(ShowOneTextureCheck.IsChecked)
		End Sub
		#End Region
	End Class
End Namespace
