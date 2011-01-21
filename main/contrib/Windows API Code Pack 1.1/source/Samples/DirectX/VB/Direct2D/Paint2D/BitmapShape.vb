' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports Microsoft.WindowsAPICodePack.DirectX.Direct2D1

Namespace D2DPaint
	Friend Class BitmapShape
		Inherits DrawingShape
		Friend _rect As RectF
		Friend _bitmap As D2DBitmap
		Friend _transparency As Single

		Friend Sub New(ByVal parent As Paint2DForm, ByVal rect As RectF, ByVal bitmap As D2DBitmap, ByVal transparency As Single)
			MyBase.New(parent)
			_rect = rect
			_bitmap = bitmap
			_transparency = transparency
		End Sub

		Protected Friend Overrides Sub Draw(ByVal renderTarget As RenderTarget)
			renderTarget.DrawBitmap(_bitmap, _transparency, BitmapInterpolationMode.Linear, _rect)
		End Sub

		Protected Friend Overrides WriteOnly Property EndPoint() As Point2F
			Set(ByVal value As Point2F)
				_rect.Right = Math.Max(_rect.Left + 5, value.X)
				_rect.Bottom = Math.Max(_rect.Top + 5, value.Y)
			End Set
		End Property
	End Class
End Namespace