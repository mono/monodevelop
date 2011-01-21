' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Windows
Imports Microsoft.WindowsAPICodePack.DirectX.Controls
Imports Microsoft.WindowsAPICodePack.DirectX.Direct2D1
Imports Microsoft.WindowsAPICodePack.DirectX.DirectWrite


Namespace Microsoft.WindowsAPICodePack.Samples
	''' <summary>
	''' Interaction logic for Window1.xaml
	''' </summary>
	Partial Public Class Window1
		Inherits Window
		Private d2dFactory As D2DFactory
		Private dwriteFactory As DWriteFactory
		Private renderTarget As HwndRenderTarget
		Private blackBrush As SolidColorBrush
		Private gridPatternBitmapBrush As BitmapBrush
		Private solidBrush1 As SolidColorBrush
		Private solidBrush2 As SolidColorBrush
		Private solidBrush3 As SolidColorBrush
		Private linearGradientBrush As LinearGradientBrush
		Private radialGradientBrush As RadialGradientBrush
		Private textFormat As TextFormat
		Private textLayout As TextLayout

		Private x1 As Integer = 70, x2 As Integer = 82, x3 As Integer = 25, x4 As Integer = 75, x5 As Integer = 54

		Public Sub New()
			InitializeComponent()
			AddHandler host.Loaded, AddressOf host_Loaded
			AddHandler host.SizeChanged, AddressOf host_SizeChanged
		End Sub


		Private Sub host_Loaded(ByVal sender As Object, ByVal e As RoutedEventArgs)
			' Create the D2D Factory
			d2dFactory = D2DFactory.CreateFactory(D2DFactoryType.SingleThreaded)

			' Create the DWrite Factory
			dwriteFactory = DWriteFactory.CreateFactory()

			' Start rendering now!
			host.Render = AddressOf Render
			host.InvalidateVisual()
		End Sub

		Private Sub host_SizeChanged(ByVal sender As Object, ByVal e As SizeChangedEventArgs)
			If renderTarget IsNot Nothing Then
				' Resize the render targrt to the actual host size
				renderTarget.Resize(New SizeU(CUInt(host.ActualWidth), CUInt(host.ActualHeight)))
			End If
			InvalidateVisual()
		End Sub

		''' <summary>
		''' This method creates the render target and all associated D2D and DWrite resources
		''' </summary>
		Private Sub CreateDeviceResources()
			' Only calls if resources have not been initialize before
			If renderTarget Is Nothing Then
				' The text format
				textFormat = dwriteFactory.CreateTextFormat("Bodoni MT", 24,  DirectX.DirectWrite.FontWeight.Medium, DirectX.DirectWrite.FontStyle.Italic,DirectX.DirectWrite.FontStretch.Normal)


				' Create the render target
				Dim size As New SizeU(CUInt(host.ActualWidth), CUInt(host.ActualHeight))
				Dim props As New RenderTargetProperties()
				Dim hwndProps As New HwndRenderTargetProperties(host.Handle, size, PresentOptions.None)
				renderTarget = d2dFactory.CreateHwndRenderTarget(props, hwndProps)

				' A black brush to be used for drawing text
				Dim cf As New ColorF(0, 0, 0, 1)
				blackBrush = renderTarget.CreateSolidColorBrush(cf)

				' Create a linear gradient.
				Dim stops() As GradientStop = { New GradientStop(1, New ColorF(1f, 0f, 0f, 0.25f)), New GradientStop(0, New ColorF(0f, 0f, 1f, 1f)) }

                Dim pGradientStops As GradientStopCollection = renderTarget.CreateGradientStopCollection(stops, Gamma.Linear, ExtendMode.Wrap)
				Dim gradBrushProps As New LinearGradientBrushProperties(New Point2F(50, 25), New Point2F(25, 50))

				linearGradientBrush = renderTarget.CreateLinearGradientBrush(gradBrushProps, pGradientStops)

				gridPatternBitmapBrush = CreateGridPatternBrush(renderTarget)

				solidBrush1 = renderTarget.CreateSolidColorBrush(New ColorF(0.3F, 0.5F, 0.65F, 0.25F))
				solidBrush2 = renderTarget.CreateSolidColorBrush(New ColorF(0.0F, 0.0F, 0.65F, 0.5F))
				solidBrush3 = renderTarget.CreateSolidColorBrush(New ColorF(0.9F, 0.5F, 0.3F, 0.75F))

				' Create a linear gradient.
				stops(0) = New GradientStop(1, New ColorF(0f, 0f, 0f, 0.25f))
				stops(1) = New GradientStop(0, New ColorF(1f, 1f, 0.2f, 1f))
                Dim radiantGradientStops As GradientStopCollection = renderTarget.CreateGradientStopCollection(stops, Gamma.Linear, ExtendMode.Wrap)

				Dim radialBrushProps As New RadialGradientBrushProperties(New Point2F(25, 25), New Point2F(0, 0), 10, 10)
				radialGradientBrush = renderTarget.CreateRadialGradientBrush(radialBrushProps, radiantGradientStops)
			End If
		End Sub

		''' <summary>
		''' Create the grid pattern (squares) brush 
		''' </summary>
		''' <param name="target"></param>
		''' <returns></returns>
		Private Function CreateGridPatternBrush(ByVal target As RenderTarget) As BitmapBrush

			' Create a compatible render target.
			Dim compatibleRenderTarget As BitmapRenderTarget = target.CreateCompatibleRenderTarget(CompatibleRenderTargetOptions.None, New SizeF(10.0f, 10.0f))

			'// Draw a pattern.
			Dim spGridBrush As SolidColorBrush = compatibleRenderTarget.CreateSolidColorBrush(New ColorF(0.93f, 0.94f, 0.96f, 1.0f))
			compatibleRenderTarget.BeginDraw()
			compatibleRenderTarget.FillRectangle(New RectF(0.0f, 0.0f, 10.0f, 1.0f), spGridBrush)
			compatibleRenderTarget.FillRectangle(New RectF(0.0f, 0.1f, 1.0f, 10.0f), spGridBrush)
			compatibleRenderTarget.EndDraw()

			'// Retrieve the bitmap from the render target.
			Dim spGridBitmap As D2DBitmap
            spGridBitmap = compatibleRenderTarget.Bitmap

			'// Choose the tiling mode for the bitmap brush.
			Dim brushProperties As New BitmapBrushProperties(ExtendMode.Wrap, ExtendMode.Wrap, BitmapInterpolationMode.Linear)
			'// Create the bitmap brush.
			Return renderTarget.CreateBitmapBrush(spGridBitmap, brushProperties)
		End Function

		Private Sub Render()

			CreateDeviceResources()

			If renderTarget.IsOccluded Then
				Return
			End If

			Dim renderTargetSize As SizeF = renderTarget.Size

			renderTarget.BeginDraw()

			renderTarget.Clear(New ColorF(1, 1, 1, 0))

			' Paint a grid background.
			Dim rf As New RectF(0.0f, 0.0f, renderTargetSize.Width, renderTargetSize.Height)
			renderTarget.FillRectangle(rf, gridPatternBitmapBrush)

			Dim curLeft As Single = 0

			rf = New RectF(curLeft, renderTargetSize.Height, (curLeft + renderTargetSize.Width / 5.0F), renderTargetSize.Height - renderTargetSize.Height * (CSng(x1) / 100.0F))

			renderTarget.FillRectangle(rf, solidBrush1)

			textLayout = dwriteFactory.CreateTextLayout(String.Format("  {0}%", x1), textFormat, renderTargetSize.Width / 5.0F, 30)

			renderTarget.DrawTextLayout(New Point2F(curLeft, renderTargetSize.Height - 30), textLayout, blackBrush)

			curLeft = (curLeft + renderTargetSize.Width / 5.0F)
			rf = New RectF(curLeft, renderTargetSize.Height, (curLeft + renderTargetSize.Width / 5.0F), renderTargetSize.Height - renderTargetSize.Height * (CSng(x2) / 100.0F))
			renderTarget.FillRectangle(rf, radialGradientBrush)
			renderTarget.DrawText(String.Format("  {0}%", x2), textFormat, New RectF(curLeft, renderTargetSize.Height - 30, (curLeft + renderTargetSize.Width / 5.0F), renderTargetSize.Height), blackBrush)

			curLeft = (curLeft + renderTargetSize.Width / 5.0F)
			rf = New RectF(curLeft, renderTargetSize.Height, (curLeft + renderTargetSize.Width / 5.0F), renderTargetSize.Height - renderTargetSize.Height * (CSng(x3) / 100.0F))
			renderTarget.FillRectangle(rf, solidBrush3)
			renderTarget.DrawText(String.Format("  {0}%", x3), textFormat, New RectF(curLeft, renderTargetSize.Height - 30, (curLeft + renderTargetSize.Width / 5.0F), renderTargetSize.Height), blackBrush)

			curLeft = (curLeft + renderTargetSize.Width / 5.0F)
			rf = New RectF(curLeft, renderTargetSize.Height, (curLeft + renderTargetSize.Width / 5.0F), renderTargetSize.Height - renderTargetSize.Height * (CSng(x4) / 100.0F))
			renderTarget.FillRectangle(rf, linearGradientBrush)
			renderTarget.DrawText(String.Format("  {0}%", x4), textFormat, New RectF(curLeft, renderTargetSize.Height - 30, (curLeft + renderTargetSize.Width / 5.0F), renderTargetSize.Height), blackBrush)


			curLeft = (curLeft + renderTargetSize.Width / 5.0F)
			rf = New RectF(curLeft, renderTargetSize.Height, (curLeft + renderTargetSize.Width / 5.0F), renderTargetSize.Height - renderTargetSize.Height * (CSng(x5) / 100.0F))
			renderTarget.FillRectangle(rf, solidBrush2)
			renderTarget.DrawText(String.Format("  {0}%", x5), textFormat, New RectF(curLeft, renderTargetSize.Height - 30, (curLeft + renderTargetSize.Width / 5.0F), renderTargetSize.Height), blackBrush)

			renderTarget.EndDraw()

		End Sub

		Private Sub generate_Data(ByVal sender As Object, ByVal e As RoutedEventArgs)
			Dim rand As New Random(CInt(Fix(Environment.TickCount)))
			x1 = rand.Next(100)
			x2 = rand.Next(100)
			x3 = rand.Next(100)
			x4 = rand.Next(100)
			x5 = rand.Next(100)

			Render()
		End Sub

	End Class
End Namespace
