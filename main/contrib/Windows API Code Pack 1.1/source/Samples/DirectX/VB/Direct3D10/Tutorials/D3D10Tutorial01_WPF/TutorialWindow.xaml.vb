' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System.Windows
Imports Microsoft.WindowsAPICodePack.DirectX.Controls
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D10
Imports Microsoft.WindowsAPICodePack.DirectX.Graphics

Namespace D3D10Tutorial01_WPF
	''' <summary>
	''' This application demonstrates creating a Direct3D 10 device
	''' 
	''' http://msdn.microsoft.com/en-us/library/bb172485(VS.85).aspx
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
		Private backColor As New ColorRgba(0.0F, 0.125F, 0.3F, 1.0F)
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
			host.Render = AddressOf RenderScene
		End Sub
		#End Region

		#Region "InitDevice()"
		''' <summary>
		''' Create Direct3D device and swap chain
		''' </summary>
		Public Sub InitDevice()
            device = D3DDevice.CreateDeviceAndSwapChain(host.Handle)
            swapChain = device.SwapChain

			' Create a render target view
			Using pBuffer As Texture2D = swapChain.GetBuffer(Of Texture2D)(0)
				renderTargetView = device.CreateRenderTargetView(pBuffer)
			End Using

            device.OM.RenderTargets = New OutputMergerRenderTargets(New RenderTargetView() {renderTargetView}, Nothing)

			' Setup the viewport
			Dim vp As New Viewport() With {.Width = CUInt(host.ActualWidth), .Height = CUInt(host.ActualHeight), .MinDepth = 0.0f, .MaxDepth = 1.0f, .TopLeftX = 0, .TopLeftY = 0}

            device.RS.Viewports = New Viewport() {vp}
		End Sub
		#End Region

		#Region "RenderScene()"
		''' <summary>
		''' Render the frame
		''' </summary>
		Protected Sub RenderScene()
			' Just clear the backbuffer
			device.ClearRenderTargetView(renderTargetView, backColor)
            swapChain.Present(0, PresentOptions.None)
		End Sub
		#End Region
	End Class
End Namespace
