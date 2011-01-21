' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Windows.Forms
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D10
Imports Microsoft.WindowsAPICodePack.DirectX.Graphics

Namespace D3D10Tutorial01_WinFormsControl
	''' <summary>
	''' This application demonstrates creating a Direct3D 10 device
	''' 
	''' http://msdn.microsoft.com/en-us/library/bb172485(VS.85).aspx
	''' 
	''' Copyright (c) Microsoft Corporation. All rights reserved.
	''' </summary>
	Partial Public Class TutorialWindow
		Inherits Form
		#Region "Fields"
		Private device As D3DDevice
		Private swapChain As SwapChain
		Private renderTargetView As RenderTargetView
        Private backColor_Renamed As New ColorRgba(0.0F, 0.125F, 0.3F, 1.0F)
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

		#Region "TutorialWindow_FormClosing()"
		Private Sub TutorialWindow_FormClosing(ByVal sender As Object, ByVal e As FormClosingEventArgs) Handles MyBase.FormClosing
			directControl.Render = Nothing
			device.ClearState()
		End Sub
		#End Region

		#Region "InitDevice()"
		''' <summary>
		''' Create Direct3D device and swap chain
		''' </summary>
		Protected Sub InitDevice()
            device = D3DDevice.CreateDeviceAndSwapChain(directControl.Handle)
            swapChain = device.SwapChain

			' Create a render target view
			Using pBuffer As Texture2D = swapChain.GetBuffer(Of Texture2D)(0)
				renderTargetView = device.CreateRenderTargetView(pBuffer)
			End Using

            device.OM.RenderTargets = New OutputMergerRenderTargets(New RenderTargetView() {renderTargetView})

			' Setup the viewport
			Dim vp As New Viewport() With {.Width = CUInt(directControl.ClientSize.Width), .Height = CUInt(directControl.ClientSize.Height), .MinDepth = 0.0f, .MaxDepth = 1.0f, .TopLeftX = 0, .TopLeftY = 0}

            device.RS.Viewports = New Viewport() {vp}
		End Sub
		#End Region

		#Region "RenderScene()"
		''' <summary>
		''' Render the frame
		''' </summary>
		Protected Sub RenderScene()
			' Just clear the backbuffer
			device.ClearRenderTargetView(renderTargetView, backColor_Renamed)
            swapChain.Present(0, PresentOptions.None)
		End Sub
		#End Region
	End Class
End Namespace
