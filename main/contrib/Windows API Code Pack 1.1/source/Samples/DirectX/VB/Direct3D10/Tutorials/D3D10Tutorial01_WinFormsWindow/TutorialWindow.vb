' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Windows.Forms
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D10
Imports Microsoft.WindowsAPICodePack.DirectX.Graphics

Namespace D3D10Tutorial01_WinFormsWindow
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
		Private active As Boolean = False
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
		''' <summary>
		''' Handles the Load event of the form.
		''' </summary>
		''' <param name="sender">The source of the event.</param>
		''' <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		Private Sub TutorialWindow_Load(ByVal sender As Object, ByVal e As EventArgs) Handles MyBase.Load
			If Not active Then
				InitDevice()
				active = True
			End If
		End Sub
		#End Region

		#Region "TutorialWindow_FormClosing()"
		''' <summary>
		''' Handles the FormClosing event of the form.
		''' </summary>
		''' <param name="sender">The source of the event.</param>
		''' <param name="e">The <see cref="System.Windows.Forms.FormClosingEventArgs"/> instance containing the event data.</param>
		Private Sub TutorialWindow_FormClosing(ByVal sender As Object, ByVal e As FormClosingEventArgs) Handles MyBase.FormClosing
			device.ClearState()
		End Sub
		#End Region

		#Region "WndProc()"
		''' <summary>
		''' The Window Procedure (message loop callback).
		''' </summary>
		''' <param name="m">The m.</param>
		Protected Overrides Sub WndProc(ByRef m As System.Windows.Forms.Message)
			Invalidate()
			MyBase.WndProc(m)
		End Sub
		#End Region

		#Region "OnPaintBackground()"
		''' <summary>
		''' Paints the background of the control.
		''' </summary>
		''' <param name="e">A <see cref="T:System.Windows.Forms.PaintEventArgs"/> that contains the event data.</param>
		Protected Overrides Sub OnPaintBackground(ByVal e As PaintEventArgs)
			' Leave empty so that invalidate does not redraw the background causing flickering
		End Sub
		#End Region

		#Region "OnPaint()"
		''' <summary>
		''' Handles painting of the window
		''' </summary>
		''' <param name="e">A <see cref="T:System.Windows.Forms.PaintEventArgs"/> that contains the event data.</param>
		Protected Overrides Sub OnPaint(ByVal e As PaintEventArgs)
			If active Then
				RenderScene()
			End If
		End Sub
		#End Region

		#Region "InitDevice()"
		''' <summary>
		''' Create Direct3D device and swap chain
		''' </summary>
		Protected Sub InitDevice()
            device = D3DDevice.CreateDeviceAndSwapChain(Me.Handle)
            swapChain = device.SwapChain

			' Create a render target view
			Using pBuffer As Texture2D = swapChain.GetBuffer(Of Texture2D)(0)
				renderTargetView = device.CreateRenderTargetView(pBuffer)
			End Using
            device.OM.RenderTargets = New OutputMergerRenderTargets(New RenderTargetView() {renderTargetView}, Nothing)

			' Setup the viewport
			Dim vp As New Viewport() With {.Width = CUInt(Me.ClientSize.Width), .Height = CUInt(Me.ClientSize.Height), .MinDepth = 0.0f, .MaxDepth = 1.0f, .TopLeftX = 0, .TopLeftY = 0}

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
