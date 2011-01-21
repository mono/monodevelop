'Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Windows
Imports System.Windows.Media
Imports System.Windows.Media.Imaging

Namespace Microsoft.WindowsAPICodePack.Samples.StarBackupSample
	Public NotInheritable Class StarBackupHelper
		''' <summary>
		''' Convert GDI bitmap into a WPF BitmapSource
		''' </summary>
		''' <param name="source"></param>
		''' <param name="width"></param>
		''' <param name="height"></param>
		''' <returns></returns>
		Private Sub New()
		End Sub
		Public Shared Function ConvertGDI_To_WPF(ByVal bmp As System.Drawing.Bitmap) As BitmapSource
			Dim bms As BitmapSource = Nothing
			If bmp IsNot Nothing Then
				Dim h_bm As IntPtr = bmp.GetHbitmap()
				bms = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(h_bm, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions())
			End If
			Return bms
		End Function

		''' <summary>
		''' Resize a given image using the new width/height
		''' </summary>
		''' <param name="source"></param>
		''' <param name="width"></param>
		''' <param name="height"></param>
		''' <returns></returns>
		Public Shared Function CreateResizedImage(ByVal source As ImageSource, ByVal width As Integer, ByVal height As Integer) As ImageSource
			' Target Rect for the resize operation
			Dim rect As New Rect(0, 0, width, height)

			' Create a DrawingVisual/Context to render with
			Dim drawingVisual As New DrawingVisual()
			Using drawingContext As DrawingContext = drawingVisual.RenderOpen()
				drawingContext.DrawImage(source, rect)
			End Using

			' Use RenderTargetBitmap to resize the original image
			Dim resizedImage As New RenderTargetBitmap(CInt(Fix(rect.Width)), CInt(Fix(rect.Height)), 96, 96, PixelFormats.Default) ' Default pixel format
			resizedImage.Render(drawingVisual)

			' Return the resized image
			Return resizedImage
		End Function


	End Class
End Namespace
