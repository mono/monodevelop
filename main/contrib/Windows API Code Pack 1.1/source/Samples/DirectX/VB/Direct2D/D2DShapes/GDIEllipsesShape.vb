' Copyright (c) Microsoft Corporation.  All rights reserved.

Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports Microsoft.WindowsAPICodePack.DirectX.Direct2D1
Imports Microsoft.WindowsAPICodePack.DirectX.Graphics
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D

Namespace D2DShapes
	Friend Class GDIEllipsesShape
		Inherits DrawingShape
		Private Structure GdiEllipse
			Public pen As Pen
			Public rect As Rectangle
		End Structure

		Private ReadOnly ellipses As New List(Of GdiEllipse)()
		Private gdiRenderTarget As GdiInteropRenderTarget

		Public Sub New(ByVal initialRenderTarget As RenderTarget, ByVal random As Random, ByVal d2DFactory As D2DFactory, ByVal bitmap As D2DBitmap, ByVal count As Integer)
			MyBase.New(initialRenderTarget, random, d2DFactory, bitmap)
			For i As Integer = 0 To count - 1
				ellipses.Add(RandomGdiEllipse())
			Next i

			If RenderTargetSupportsGDI(RenderTarget) Then
                gdiRenderTarget = RenderTarget.GdiInteropRenderTarget
			End If
		End Sub

		Private Shared Function RenderTargetSupportsGDI(ByVal rt As RenderTarget) As Boolean
            Dim propertiesToSupport = New RenderTargetProperties(RenderTargetType.Default, New PixelFormat(Format.B8G8R8A8UNorm, AlphaMode.Ignore), 96, 96, RenderTargetUsages.GdiCompatible, FeatureLevel.Default)
			If rt.IsSupported(propertiesToSupport) Then
				Return True
			End If
            propertiesToSupport = New RenderTargetProperties(RenderTargetType.Default, New PixelFormat(Format.B8G8R8A8UNorm, AlphaMode.Premultiplied), 96, 96, RenderTargetUsages.GdiCompatible, FeatureLevel.Default)
			Return rt.IsSupported(propertiesToSupport)
		End Function

		Private Function RandomGdiEllipse() As GdiEllipse
			Return New GdiEllipse With {.pen = New Pen(Brushes.Black), .rect = RandomGdiRect()}
		End Function

		Private Function RandomGdiRect() As Rectangle
			Dim x1 As Integer = Random.Next(0, CanvasWidth)
			Dim x2 As Integer = Random.Next(0, CanvasWidth)
			Dim y1 As Integer = Random.Next(0, CanvasHeight)
			Dim y2 As Integer = Random.Next(0, CanvasHeight)
			Return New Rectangle(Math.Min(x1, x2), Math.Min(y1, y2), Math.Max(x1, x2) - Math.Min(x1, x2), Math.Max(y1, y2) - Math.Min(y1, y2))
		End Function

		Protected Friend Overrides Sub ChangeRenderTarget(ByVal newRenderTarget As RenderTarget)
			If RenderTargetSupportsGDI(newRenderTarget) Then
                gdiRenderTarget = newRenderTarget.GdiInteropRenderTarget
			ElseIf gdiRenderTarget IsNot Nothing Then
				gdiRenderTarget.Dispose()
				gdiRenderTarget = Nothing
			End If
		End Sub

		Protected Friend Overrides Sub Draw(ByVal renderTarget As RenderTarget)
			If gdiRenderTarget IsNot Nothing Then
                Dim dc As IntPtr = gdiRenderTarget.GetDC(DCInitializeMode.Copy)
				Dim g As Graphics = Graphics.FromHdc(dc)
				For Each ellipse In ellipses
					g.DrawEllipse(ellipse.pen, ellipse.rect)
				Next ellipse
				g.Dispose()
				gdiRenderTarget.ReleaseDC()
			End If
		End Sub

		Public Overrides Function HitTest(ByVal point As Point2F) As Boolean
			For Each ellipse In ellipses

                Dim g As EllipseGeometry = d2DFactory.CreateEllipseGeometry(New Ellipse(New Point2F((ellipse.rect.Left + ellipse.rect.Right) / 2.0F, (ellipse.rect.Top + ellipse.rect.Bottom) / 2.0F), ellipse.rect.Width / 2.0F, ellipse.rect.Height / 2.0F))
				Dim ret As Boolean = g.FillContainsPoint(point, 1)
				g.Dispose()
				If ret Then
					Return True
				End If
			Next ellipse
			Return False
		End Function

		Public Overrides Sub Dispose()
			If gdiRenderTarget IsNot Nothing Then
				gdiRenderTarget.Dispose()
			End If
			gdiRenderTarget = Nothing
			MyBase.Dispose()
		End Sub
	End Class
End Namespace
