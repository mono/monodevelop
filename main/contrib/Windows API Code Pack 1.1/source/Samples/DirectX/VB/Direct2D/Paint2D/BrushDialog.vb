' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System.Windows.Forms
Imports Microsoft.WindowsAPICodePack.DirectX.Direct2D1
Imports System
Imports System.Globalization
Imports System.IO
Imports Microsoft.WindowsAPICodePack.DirectX
Imports System.Drawing
Imports Brush = Microsoft.WindowsAPICodePack.DirectX.Direct2D1.Brush


Namespace D2DPaint
	Friend Enum BrushType
		None
		Solid
		Bitmap
		LinearGradiant
		RadialGradient
	End Enum

	Partial Public Class BrushDialog
		Inherits Form
        Private color1 As New ColorF(Color.Black.ToArgb())
        Private color2 As New ColorF(Color.White.ToArgb())
		Private opacity_Renamed As Single = 1.0f
        Private ReadOnly parentCopy As Paint2DForm
        Private ReadOnly renderTargetCopy As RenderTarget
        Private imageFilename As String

        Public Sub New(ByVal parent As Paint2DForm, ByVal target As RenderTarget)
            Me.parentCopy = parent
            Me.renderTargetCopy = target
            InitializeComponent()
            For i As Integer = 0 To transparencyValues.Items.Count - 1
                transparencyValues.Items(i) = CType(transparencyValues.Items(i), String).Replace(".", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator)
            Next
            FillBrushesListBox()
        End Sub

        Private Sub FillBrushesListBox()
            Me.brushesList.Items.Clear()
            For Each brush As Brush In parentCopy.brushes
                If TypeOf brush Is SolidColorBrush Then
                    Dim solidBrush As SolidColorBrush = TryCast(brush, SolidColorBrush)
                    Me.brushesList.Items.Add(String.Format("Solid: R={0}, G={1}, B={2}, A={3}, Opacity={4}", solidBrush.Color.Red, solidBrush.Color.Green, solidBrush.Color.Blue, solidBrush.Color.Alpha, solidBrush.Opacity))
                ElseIf TypeOf brush Is BitmapBrush Then
                    Dim bitmapBrush As BitmapBrush = TryCast(brush, BitmapBrush)
                    Me.brushesList.Items.Add(String.Format("Bitmap Brush: Extended Mode X={0}, Extended Mode Y={1}, Inter. Mode={2}", bitmapBrush.ExtendModeX, bitmapBrush.ExtendModeY, bitmapBrush.InterpolationMode))
                Else
                    Me.brushesList.Items.Add(brush)
                End If
            Next brush
            brushesList.SelectedIndex = parentCopy.currentBrushIndex

        End Sub

        Private Sub SelectColorClick(ByVal sender As Object, ByVal e As EventArgs) Handles solidColorButton.Click
            colorDialog1.Color = System.Drawing.Color.Black
            If colorDialog1.ShowDialog() <> System.Windows.Forms.DialogResult.Cancel Then
                color1 = New ColorF(colorDialog1.Color.R / 255.0F, colorDialog1.Color.G / 255.0F, colorDialog1.Color.B / 255.0F, colorDialog1.Color.A / 255.0F)

                colorLabel.Text = String.Format("R = {0}, G = {1}, B = {2}, A = {3}", color1.Red, color1.Green, color1.Blue, color1.Alpha)
            End If
        End Sub

        Private Sub addBrushButton_Click(ByVal sender As Object, ByVal e As EventArgs) Handles addBrushButton.Click
            parentCopy.brushes.Add(renderTargetCopy.CreateSolidColorBrush(color1, New BrushProperties(opacity_Renamed, Matrix3x2F.Identity)))

            parentCopy.currentBrushIndex = parentCopy.brushes.Count - 1

            FillBrushesListBox()
        End Sub

        Private Sub transparencyValues_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) Handles transparencyValues.SelectedIndexChanged
            Dim f As Single
            If Single.TryParse(transparencyValues.Text, f) Then
                Me.opacity_Renamed = f
            End If
        End Sub

        Private Sub listBox1_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) Handles brushesList.SelectedIndexChanged
            parentCopy.currentBrushIndex = brushesList.SelectedIndex
        End Sub

        Private Sub button2_Click(ByVal sender As Object, ByVal e As EventArgs) Handles button2.Click
            Dim dialog As OpenFileDialog = New OpenFileDialog With {.DefaultExt = "*.jpg;*.png"}
            If dialog.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
                Me.imageFilename = dialog.FileName
                imageFileLabel.Text = Path.GetFileName(imageFilename)

            End If
        End Sub

        Private Sub addBitmapBrushBotton_Click(ByVal sender As Object, ByVal e As EventArgs) Handles addBitmapBrushBotton.Click
            Dim ex As ExtendMode = If(extendedModeXComboBox.SelectedIndex > 0, CType(extendedModeXComboBox.SelectedIndex, ExtendMode), ExtendMode.Wrap)
            Dim ey As ExtendMode = If(extendedModeYComboBox.SelectedIndex > 0, CType(extendedModeYComboBox.SelectedIndex, ExtendMode), ExtendMode.Wrap)

            Dim brushBitmap As D2DBitmap = BitmapUtilities.LoadBitmapFromFile(renderTargetCopy, parentCopy.wicFactory, imageFilename)
            Dim brush As BitmapBrush = renderTargetCopy.CreateBitmapBrush(brushBitmap, New BitmapBrushProperties(ex, ey, BitmapInterpolationMode.NearestNeighbor), New BrushProperties(opacity_Renamed, Matrix3x2F.Identity))
            parentCopy.brushes.Add(brush)
            parentCopy.currentBrushIndex = parentCopy.brushes.Count - 1
            FillBrushesListBox()
        End Sub

        Private Sub CloseButtonClicked(ByVal sender As Object, ByVal e As EventArgs) Handles button1.Click
            Me.Close()
        End Sub

        Private Sub OpacityButtonClicked(ByVal sender As Object, ByVal e As EventArgs) Handles comboBox2.SelectedIndexChanged
            Dim f As Single
            If Single.TryParse(comboBox2.Text, f) Then
                Me.opacity_Renamed = f
            End If
        End Sub

        Private Sub gradiantBrushColor1button_Click(ByVal sender As Object, ByVal e As EventArgs) Handles gradiantBrushColor1button.Click

            colorDialog1.Color = System.Drawing.Color.Black
            If colorDialog1.ShowDialog() <> System.Windows.Forms.DialogResult.Cancel Then
                color1 = New ColorF(colorDialog1.Color.R / 255.0F, colorDialog1.Color.G / 255.0F, colorDialog1.Color.B / 255.0F, colorDialog1.Color.A / 255.0F)

                gradBrushColor1Label.Text = String.Format("R = {0}, G = {1}, B = {2}, A = {3}", color1.Red, color1.Green, color1.Blue, color1.Alpha)
            End If
        End Sub

        Private Sub gradiantBrushColor2Button_Click(ByVal sender As Object, ByVal e As EventArgs) Handles gradiantBrushColor2Button.Click
            colorDialog1.Color = System.Drawing.Color.Black
            If colorDialog1.ShowDialog() <> System.Windows.Forms.DialogResult.Cancel Then
                color2 = New ColorF(colorDialog1.Color.R / 255.0F, colorDialog1.Color.G / 255.0F, colorDialog1.Color.B / 255.0F, colorDialog1.Color.A / 255.0F)

                gradBrushColor2Label.Text = String.Format("R = {0}, G = {1}, B = {2}, A = {3}", color2.Red, color2.Green, color2.Blue, color2.Alpha)
            End If
        End Sub

        Private Sub LinearGradientBrushAddClicked(ByVal sender As Object, ByVal e As EventArgs) Handles button3.Click
            Dim ex As ExtendMode = If(gradBrushExtendModeCombo.SelectedIndex > 0, CType(gradBrushExtendModeCombo.SelectedIndex, ExtendMode), ExtendMode.Clamp)
            Dim gamma As Gamma

            Select Case gammaComboBox.SelectedIndex
                Case 0
                    gamma = Direct2D1.Gamma.Linear
                    Exit Select
                Case 1
                    gamma = Direct2D1.Gamma.StandardRgb
                    Exit Select
                Case Else
                    Throw New InvalidOperationException("Unknown gamma selected")
            End Select

            Dim stops() As GradientStop = {New GradientStop(0.0F, color1), New GradientStop(1.0F, color2)}

            Dim stopCollection As GradientStopCollection = renderTargetCopy.CreateGradientStopCollection(stops, gamma, ex)

            Dim properties As LinearGradientBrushProperties
            If ex = ExtendMode.Clamp Then
                properties = New LinearGradientBrushProperties(New Point2F(50, 50), New Point2F(600, 400))
            Else
                properties = New LinearGradientBrushProperties(New Point2F(50, 50), New Point2F(0, 0))
            End If


            Dim brush As LinearGradientBrush = renderTargetCopy.CreateLinearGradientBrush(properties, stopCollection)

            parentCopy.brushes.Add(brush)
            parentCopy.currentBrushIndex = parentCopy.brushes.Count - 1
            FillBrushesListBox()

        End Sub

        Private Sub RadialGradientBrushAddClicked(ByVal sender As Object, ByVal e As EventArgs) Handles button4.Click
            Dim ex As ExtendMode = If(radialExtendCombo.SelectedIndex > 0, CType(radialExtendCombo.SelectedIndex, ExtendMode), ExtendMode.Clamp)
            Dim gamma As Gamma

            Select Case gammaComboBox.SelectedIndex
                Case 0
                    gamma = Direct2D1.Gamma.Linear
                    Exit Select
                Case 1
                    gamma = Direct2D1.Gamma.StandardRgb
                    Exit Select
                Case Else
                    Throw New InvalidOperationException("Unknown gamma selected")
            End Select

            Dim stops() As GradientStop = {New GradientStop(0, color1), New GradientStop(1.0F, color2)}

            Dim stopCollection As GradientStopCollection = renderTargetCopy.CreateGradientStopCollection(stops, gamma, ex)

            Dim properties As RadialGradientBrushProperties

            If ex = ExtendMode.Clamp Then
                properties = New RadialGradientBrushProperties(New Point2F(50, 50), New Point2F(600, 400), 600, 600)
            Else
                properties = New RadialGradientBrushProperties(New Point2F(50, 50), New Point2F(0, 0), 50, 50)
            End If

            Dim brush As RadialGradientBrush = renderTargetCopy.CreateRadialGradientBrush(properties, stopCollection)

            parentCopy.brushes.Add(brush)
            parentCopy.currentBrushIndex = parentCopy.brushes.Count - 1
            FillBrushesListBox()
        End Sub

		Private Sub SelectRadialColor1_Click(ByVal sender As Object, ByVal e As EventArgs) Handles SelectRadialColor1.Click
			colorDialog1.Color = System.Drawing.Color.Black
			If colorDialog1.ShowDialog() <> System.Windows.Forms.DialogResult.Cancel Then
				color1 = New ColorF(colorDialog1.Color.R / 255F, colorDialog1.Color.G / 255F, colorDialog1.Color.B / 255F, colorDialog1.Color.A / 255F)

                radialBrushColor1Label.Text = String.Format("R = {0}, G = {1}, B = {2}, A = {3}", color1.Red, color1.Green, color1.Blue, color1.Alpha)
			End If
		End Sub

		Private Sub SelectRadialColor2_Click(ByVal sender As Object, ByVal e As EventArgs) Handles SelectRadialColor2.Click
			colorDialog1.Color = System.Drawing.Color.Black
			If colorDialog1.ShowDialog() <> System.Windows.Forms.DialogResult.Cancel Then
				color2 = New ColorF(colorDialog1.Color.R / 255F, colorDialog1.Color.G / 255F, colorDialog1.Color.B / 255F, colorDialog1.Color.A / 255F)

                radialBrushColor2Label.Text = String.Format("R = {0}, G = {1}, B = {2}, A = {3}", color2.Red, color2.Green, color2.Blue, color2.Alpha)
			End If
		End Sub
	End Class
End Namespace
