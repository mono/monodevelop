' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.Threading
Imports System.Windows.Forms
Imports Microsoft.WindowsAPICodePack.DirectX.Direct2D1
Imports Microsoft.WindowsAPICodePack.DirectX.DirectWrite
Imports Microsoft.WindowsAPICodePack.DirectX.WindowsImagingComponent
Imports Microsoft.WindowsAPICodePack.DirectX
Imports System.Drawing
Imports Brush = Microsoft.WindowsAPICodePack.DirectX.Direct2D1.Brush
Imports Graphics = Microsoft.WindowsAPICodePack.DirectX.Graphics

Namespace D2DPaint
	Friend Enum Shape
		None
		Line
		Bitmap
		Rectangle
		RoundedRectangle
		Ellipse
		Text
		Geometry
	End Enum

	Partial Public Class Paint2DForm
		Inherits Form
		#Region "Fields"
        Friend brushes As New List(Of Brush)()
		Friend currentBrushIndex As Integer = -1

		Private startPoint As Point2F
		Private endPoint As Point2F
		Private brushDialog As BrushDialog = Nothing
		Private drawingShapes As New List(Of DrawingShape)()
		Private currentBitmap As D2DBitmap
		Private currentShapeType As Shape = Shape.None
		Private currentShape As DrawingShape = Nothing
		Private fill As Boolean = False

		Friend TextBoxStroke As StrokeStyle

		Friend d2dFactory As D2DFactory
		Friend wicFactory As ImagingFactory
		Friend dwriteFactory As DWriteFactory
        Private renderTarget As HwndRenderTarget

		Private isDrawing As Boolean = False
		Private isDragging As Boolean = False

		Private currentStrokeSize As Single = 2
		Private currentTransparency As Single = 1

        Private ReadOnly WhiteBackgroundColor As New ColorF(Color.White.ToArgb())
		Private textDialog As TextDialog
		Private bitmapDialog As OpenFileDialog

		Private currentButton As ToolStripButton = Nothing

        Private renderProps As New RenderTargetProperties(RenderTargetType.Software, New PixelFormat(Graphics.Format.B8G8R8A8UNorm, AlphaMode.Ignore), 0, 0, RenderTargetUsages.None, Direct3D.FeatureLevel.Default)

		#End Region

		#Region "Paint2DForm()"
		Public Sub New()
            InitializeComponent()
            For i As Integer = 0 To traparencyList.Items.Count - 1
                traparencyList.Items(i) = CType(traparencyList.Items(i), String).Replace(".", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator)
            Next
            strokeWidths.SelectedItem = "2"
            traparencyList.SelectedIndex = 0
        End Sub
		#End Region

		#Region "renderControl_SizeChanged()"
		Private Sub renderControl_SizeChanged(ByVal sender As Object, ByVal e As EventArgs) Handles renderControl.SizeChanged
			If renderTarget IsNot Nothing Then
				' Resize the render targrt to the actual host size
				Dim size As New SizeU(CUInt(renderControl.ClientSize.Width), CUInt(renderControl.ClientSize.Height))
				renderTarget.Resize(size)
			End If
		End Sub
		#End Region

		#Region "CreateDeviceResources()"
		''' <summary>
		''' This method creates the render target and associated D2D and DWrite resources
		''' </summary>
		Private Sub CreateDeviceResources()
			' Only calls if resources have not been initialize before
			If renderTarget Is Nothing Then
				' Create the render target
				Dim size As New SizeU(CUInt(renderControl.ClientSize.Width), CUInt(renderControl.ClientSize.Height))
                Dim hwndProps As New HwndRenderTargetProperties(renderControl.Handle, size, PresentOptions.RetainContents)
                renderTarget = d2dFactory.CreateHwndRenderTarget(renderProps, hwndProps)

				' Create an initial black brush
                brushes.Add(renderTarget.CreateSolidColorBrush(New ColorF(Color.Black.ToArgb())))
				currentBrushIndex = 0
			End If
		End Sub
		#End Region

		#Region "RenderScene()"
		Private Sub RenderScene()
			CreateDeviceResources()

			If renderTarget.IsOccluded Then
				Return
			End If

			renderTarget.BeginDraw()

			renderTarget.Clear(WhiteBackgroundColor)

			For Each shape As DrawingShape In drawingShapes
				shape.Draw(renderTarget)
			Next shape

			renderTarget.EndDraw()
		End Sub
		#End Region

		#Region "renderControl_Load()"
		Private Sub renderControl_Load(ByVal sender As Object, ByVal e As EventArgs) Handles renderControl.Load
			LoadDeviceIndependentResource()
			renderControl.Render = AddressOf RenderScene
			currentButton = arrowButton
		End Sub

		Private Sub LoadDeviceIndependentResource()
			' Create the D2D Factory
			' This really needs to be set to type MultiThreaded if rendering is to be performed by multiple threads,
			' such as if used in a control similar to DirectControl sample control where rendering is done by a dedicated render thread,
			' especially if multiple such controls are used in one application, but also when multiple applications use D2D Factories.
			'
			' In this sample - SingleThreaded type is used because rendering is only done by the main/UI thread and only when required
			' (when the surface gets invalidated) making the risk of synchronization problems - quite low.
			d2dFactory = D2DFactory.CreateFactory(D2DFactoryType.MultiThreaded)

			' Create the DWrite Factory
			dwriteFactory = DWriteFactory.CreateFactory()

			' Create the WIC Factory
            wicFactory = ImagingFactory.Create()

			TextBoxStroke = d2dFactory.CreateStrokeStyle(New StrokeStyleProperties(CapStyle.Flat, CapStyle.Flat, CapStyle.Round, LineJoin.Miter, 5.0f, DashStyle.Dash, 3f), Nothing)

		End Sub
		#End Region

		#Region "renderControl_MouseDown()"
		Private Sub renderControl_MouseDown(ByVal sender As Object, ByVal e As MouseEventArgs) Handles renderControl.MouseDown
			If Not isDrawing Then
				Return
			End If

			isDragging = True
			startPoint.X = e.X
			startPoint.Y = e.Y
			endPoint = startPoint

			Select Case currentShapeType
				Case Shape.Line
					currentShape = New LineShape(Me, startPoint, startPoint, currentStrokeSize, currentBrushIndex)
					drawingShapes.Add(currentShape)
				Case Shape.Bitmap
					currentShape = New BitmapShape(Me, New RectF(startPoint.X, startPoint.Y, startPoint.X + 5, startPoint.Y + 5), currentBitmap, currentTransparency)
					drawingShapes.Add(currentShape)
				Case Shape.RoundedRectangle
					currentShape = New RoundRectangleShape(Me, New RoundedRect(New RectF(startPoint.X, startPoint.Y, startPoint.X, startPoint.Y), 20f, 20f), currentStrokeSize, currentBrushIndex, fill)
					drawingShapes.Add(currentShape)
				Case Shape.Rectangle
					currentShape = New RectangleShape(Me, New RectF(startPoint.X, startPoint.Y, startPoint.X, startPoint.Y), currentStrokeSize, currentBrushIndex, fill)
					drawingShapes.Add(currentShape)
				Case Shape.Ellipse
					currentShape = New EllipseShape(Me, New Ellipse(startPoint, 0, 0), currentStrokeSize, currentBrushIndex, fill)
					drawingShapes.Add(currentShape)
				Case Shape.Text
					currentShape = New TextShape(Me, textDialog.TextLayout, startPoint, 100, 100, currentBrushIndex)
					drawingShapes.Add(currentShape)
				Case Shape.Geometry
					currentShape = New GeometryShape(Me, startPoint, currentStrokeSize, currentBrushIndex, fill)
					drawingShapes.Add(currentShape)
			End Select
			Invalidate()
		End Sub
		#End Region

		#Region "renderControl_MouseMove()"
		Private Sub renderControl_MouseMove(ByVal sender As Object, ByVal e As MouseEventArgs) Handles renderControl.MouseMove
			If (Not isDrawing) OrElse (Not isDragging) Then
				Return
			End If

			endPoint.X = e.X
			endPoint.Y = e.Y

			currentShape.EndPoint = endPoint
			renderControl.Invalidate()
		End Sub
		#End Region

		#Region "renderControl_MouseUp()"
		Private Sub renderControl_MouseUp(ByVal sender As Object, ByVal e As MouseEventArgs) Handles renderControl.MouseUp
			If (Not isDragging) OrElse (Not isDrawing) Then
				Return
			End If
			currentShape.EndDraw()
			isDragging = False
			renderControl.Invalidate()
		End Sub
		#End Region

		#Region "SwitchDrawMode()"
		Private Sub SwitchDrawMode(ByVal currentModeButton As Object)

			isDrawing = True

			' Unselect the previous button
			If currentButton IsNot Nothing Then
				currentButton.Checked = False
			End If

			' Select the new button
			currentButton = TryCast(currentModeButton, ToolStripButton)
			If currentButton IsNot Nothing Then
				currentButton.Checked = True
			End If
		End Sub
		#End Region

		#Region "lineButton_Click()"
		Private Sub lineButton_Click(ByVal sender As Object, ByVal e As EventArgs) Handles lineButton.Click
			currentShapeType = Shape.Line
			SwitchDrawMode(sender)
		End Sub
		#End Region

		#Region "rectButton_Click()"
		Private Sub rectButton_Click(ByVal sender As Object, ByVal e As EventArgs) Handles rectButton.Click
			currentShapeType = Shape.Rectangle
			SwitchDrawMode(sender)
		End Sub
		#End Region

		#Region "roundrectButton_Click()"
		Private Sub roundrectButton_Click(ByVal sender As Object, ByVal e As EventArgs) Handles roundrectButton.Click
			currentShapeType = Shape.RoundedRectangle
			SwitchDrawMode(sender)
		End Sub
		#End Region

		#Region "ellipseButton_Click()"
		Private Sub ellipseButton_Click(ByVal sender As Object, ByVal e As EventArgs) Handles ellipseButton.Click
			currentShapeType = Shape.Ellipse
			SwitchDrawMode(sender)
		End Sub
		#End Region

		#Region "bitmapButton_Click()"
		Private Sub bitmapButton_Click(ByVal sender As Object, ByVal e As EventArgs) Handles bitmapButton.Click
			If bitmapDialog Is Nothing Then
				bitmapDialog = New OpenFileDialog()
				bitmapDialog.DefaultExt = "*.jpg;*.png"
			End If
			If bitmapDialog.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
				Dim filename As String = bitmapDialog.FileName
				currentBitmap = BitmapUtilities.LoadBitmapFromFile(renderTarget, wicFactory, filename)

				currentShapeType = Shape.Bitmap
				SwitchDrawMode(sender)
			End If
		End Sub
		#End Region

		#Region "textButton_Click()"
		Private Sub textButton_Click(ByVal sender As Object, ByVal e As EventArgs) Handles textButton.Click
			If textDialog Is Nothing Then
				textDialog = New TextDialog(Me)
			End If

			If textDialog.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
				currentShapeType = Shape.Text
				SwitchDrawMode(sender)
			End If
		End Sub
		#End Region

		#Region "geometryButton_Click()"
		Private Sub geometryButton_Click(ByVal sender As Object, ByVal e As EventArgs) Handles geometryButton.Click
			currentShapeType = Shape.Geometry
			SwitchDrawMode(sender)
		End Sub
		#End Region

		#Region "brushButton_Click()"
		Private Sub brushButton_Click(ByVal sender As Object, ByVal e As EventArgs) Handles brushButton.Click
			If brushDialog Is Nothing OrElse brushDialog.IsDisposed Then
                brushDialog = New BrushDialog(Me, renderTarget)
			End If

			brushDialog.Show()
			brushDialog.Activate()
		End Sub
		#End Region

		#Region "fillButton_Click()"
		Private Sub fillButton_Click(ByVal sender As Object, ByVal e As EventArgs) Handles fillButton.Click
			fill = Not fill
		End Sub
		#End Region

		#Region "strokeWidths_SelectedIndexChanged()"
		Private Sub strokeWidths_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) Handles strokeWidths.SelectedIndexChanged
			Dim f As Single
			If Single.TryParse(TryCast(strokeWidths.Text, String), f) Then
				Me.currentStrokeSize = f
			End If
		End Sub
		#End Region

		#Region "traparencyList_SelectedIndexChanged()"
		Private Sub traparencyList_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) Handles traparencyList.SelectedIndexChanged
			Dim f As Single
			If Single.TryParse(TryCast(traparencyList.Text, String), f) Then
				Me.currentTransparency = f
			End If
		End Sub
		#End Region

		#Region "clearButton_Click()"
		Private Sub clearButton_Click(ByVal sender As Object, ByVal e As EventArgs) Handles clearButton.Click
			drawingShapes.Clear()
			renderControl.Invalidate()
		End Sub
		#End Region

		Private Sub toolStrip1_MouseEnter(ByVal sender As Object, ByVal e As EventArgs) Handles toolStrip1.MouseEnter
			Me.Cursor = Cursors.Arrow
		End Sub

		Private Sub renderControl_MouseEnter(ByVal sender As Object, ByVal e As EventArgs) Handles renderControl.MouseEnter
			If isDrawing Then
				Me.Cursor = Cursors.Cross
			End If
		End Sub

		Private Sub toolStripButton1_Click(ByVal sender As Object, ByVal e As EventArgs) Handles arrowButton.Click
			Me.isDrawing = False
			Me.currentShape = Nothing

			If currentButton IsNot Nothing Then
				currentButton.Checked = False
			End If

			currentButton = arrowButton
			arrowButton.Checked = True
		End Sub

        Private Sub saveButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles saveButton.Click
            If renderTarget Is Nothing Then
                MessageBox.Show("Unable to save file.")
                Return
            End If

            Dim saveDlg As New SaveFileDialog()
            saveDlg.Filter = "Bitmap image (*.bmp)|*.bmp|Png image (*.png)|*.png|Jpeg image (*.jpg)|*.jpg|Gif image (*.gif)|*.gif"

            If saveDlg.ShowDialog() = Windows.Forms.DialogResult.OK Then

                Dim size = New SizeU(CUInt(ClientSize.Width), CUInt(ClientSize.Height))

                Dim wicBitmap = wicFactory.CreateImagingBitmap(size.Width, size.Height, PixelFormats.Bgr32Bpp, BitmapCreateCacheOption.CacheOnLoad)

                Dim d2dBitmap = renderTarget.CreateBitmap(size, New BitmapProperties(New PixelFormat(Microsoft.WindowsAPICodePack.DirectX.Graphics.Format.B8G8R8A8UNorm, AlphaMode.Ignore), renderTarget.Dpi.X, renderTarget.Dpi.Y))
                D2DBitmap.CopyFromRenderTarget(renderTarget)

                Dim wicRenderTarget = d2dFactory.CreateWicBitmapRenderTarget(wicBitmap, renderProps)

                wicRenderTarget.BeginDraw()

                wicRenderTarget.DrawBitmap(d2dBitmap)
                wicRenderTarget.EndDraw()

                Dim fileType As Guid
                If saveDlg.FilterIndex = 1 Then
                    fileType = ContainerFormats.Bmp
                ElseIf saveDlg.FilterIndex = 1 Then
                    fileType = ContainerFormats.Png
                ElseIf saveDlg.FilterIndex = 1 Then
                    fileType = ContainerFormats.Jpeg
                ElseIf saveDlg.FilterIndex = 1 Then
                    fileType = ContainerFormats.Gif
                Else
                    fileType = ContainerFormats.Bmp
                End If

                wicBitmap.SaveToFile(wicFactory, fileType, saveDlg.FileName)
            End If

        End Sub
    End Class
End Namespace
