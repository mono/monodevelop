' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Windows
Imports Microsoft.WindowsAPICodePack.DirectX.Controls
Imports Microsoft.WindowsAPICodePack.DirectX.Direct2D1
Imports Microsoft.WindowsAPICodePack.DirectX.DirectWrite

Imports Microsoft.WindowsAPICodePack.DirectX.WindowsImagingComponent

Namespace Microsoft.WindowsAPICodePack.Samples
	''' <summary>
	''' Interaction logic for Window1.xaml
	''' </summary>
	Partial Public Class Window1
		Inherits Window
		Private d2dFactory As D2DFactory
		Private dwriteFactory As DWriteFactory
		Private wicFactory As ImagingFactory
		Private renderTarget As HwndRenderTarget
		Private blackBrush As SolidColorBrush
		Private inlineImage As ImageInlineObject
		Private textFormat As TextFormat
		Private textLayout As TextLayout

		Public Sub New()
			InitializeComponent()
			AddHandler host.Loaded, AddressOf host_Loaded
			AddHandler host.SizeChanged, AddressOf host_SizeChanged
		End Sub

		Private Sub host_SizeChanged(ByVal sender As Object, ByVal e As SizeChangedEventArgs)
			If renderTarget IsNot Nothing Then
				' Resize the render targrt to the actual host size
				renderTarget.Resize(New SizeU(CUInt(host.ActualWidth), CUInt(host.ActualHeight)))

				' Resize the text layout max width and height as well
				textLayout.MaxWidth = CSng(host.ActualWidth)
				textLayout.MaxHeight = CSng(host.ActualHeight)
			End If

			InvalidateVisual()
		End Sub

		Private Sub host_Loaded(ByVal sender As Object, ByVal e As RoutedEventArgs)
			CreateDeviceIndependentResources()

			' Start rendering now
			host.Render = AddressOf Render
			host.InvalidateVisual()
		End Sub

		Private Sub CreateDeviceIndependentResources()
			' Create the D2D Factory
			d2dFactory = D2DFactory.CreateFactory(D2DFactoryType.SingleThreaded)

			' Create the DWrite Factory
			dwriteFactory = DWriteFactory.CreateFactory()

            wicFactory = ImagingFactory.Create()

			Dim text As String = "Inline Object * Sample"

			textFormat = dwriteFactory.CreateTextFormat("Gabriola", 72)

            textFormat.TextAlignment = DirectX.DirectWrite.TextAlignment.Center            
            textFormat.ParagraphAlignment = DirectX.DirectWrite.ParagraphAlignment.Center

			textLayout = dwriteFactory.CreateTextLayout(text, textFormat, CSng(host.ActualWidth), CSng(host.ActualHeight))
		End Sub


		''' <summary>
		''' This method creates the render target and all associated D2D and DWrite resources
		''' </summary>
		Private Sub CreateDeviceResources()
			' Only calls if resources have not been initialize before
			If renderTarget Is Nothing Then
				' Create the render target
				Dim size As New SizeU(CUInt(host.ActualWidth), CUInt(host.ActualHeight))
				Dim props As New RenderTargetProperties()
				Dim hwndProps As New HwndRenderTargetProperties(host.Handle, size, PresentOptions.None)
				renderTarget = d2dFactory.CreateHwndRenderTarget(props, hwndProps)

				' Create the black brush for text
				blackBrush = renderTarget.CreateSolidColorBrush(New ColorF(0,0, 0, 1))

                inlineImage = New ImageInlineObject(renderTarget, wicFactory, "img1.jpg")

				Dim textRange As New TextRange(14, 1)
				textLayout.SetInlineObject(inlineImage, textRange)
			End If
		End Sub

		Private Sub Render()

			CreateDeviceResources()

			If renderTarget.IsOccluded Then
				Return
			End If

			Dim renderTargetSize As SizeF = renderTarget.Size

			renderTarget.BeginDraw()

			renderTarget.Clear(New ColorF(1, 1, 1, 0))

			renderTarget.DrawTextLayout(New Point2F(0,0), textLayout, blackBrush)

			renderTarget.EndDraw()

		End Sub

	End Class
End Namespace
