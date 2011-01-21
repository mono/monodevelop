' Copyright (c) Microsoft Corporation.  All rights reserved.

Imports Microsoft.VisualBasic
Imports System
Imports Microsoft.WindowsAPICodePack.DirectX.Direct2D1

Namespace D2DShapes
	Friend Class LineShape
		Inherits DrawingShape
		Friend point0, point1 As Point2F

		Public Sub New(ByVal initialRenderTarget As RenderTarget, ByVal random As Random, ByVal d2DFactory As D2DFactory, ByVal bitmap As D2DBitmap)
			MyBase.New(initialRenderTarget, random, d2DFactory, bitmap)
			point0 = RandomPoint()
			point1 = RandomPoint()
			PenBrush = RandomBrush()
			StrokeWidth = RandomStrokeWidth()
			If CoinFlip Then
				StrokeStyle = RandomStrokeStyle()
			End If
		End Sub

		Protected Friend Overrides Sub ChangeRenderTarget(ByVal newRenderTarget As RenderTarget)
			PenBrush = CopyBrushToRenderTarget(PenBrush, newRenderTarget)
		End Sub

		Protected Friend Overrides Sub Draw(ByVal renderTarget As RenderTarget)
			If StrokeStyle IsNot Nothing Then
				renderTarget.DrawLine(point0, point1, PenBrush, StrokeWidth, StrokeStyle)
			Else
				renderTarget.DrawLine(point0, point1, PenBrush, StrokeWidth)
			End If
		End Sub
	End Class
End Namespace
