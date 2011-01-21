' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports Microsoft.WindowsAPICodePack.DirectX.Direct2D1

Namespace D2DPaint
	Friend Class RoundRectangleShape
		Inherits DrawingShape
		Friend _rect As RoundedRect
		Friend _strokeWidth As Single
		Friend _selectedBrushIndex As Integer


		Friend Sub New(ByVal parent As Paint2DForm, ByVal rect As RoundedRect, ByVal strokeWidth As Single, ByVal selectedBrush As Integer, ByVal fill As Boolean)
			MyBase.New(parent, fill)
			_rect = rect
			_strokeWidth = strokeWidth
			_selectedBrushIndex = selectedBrush
		End Sub

		Protected Friend Overrides Sub Draw(ByVal renderTarget As RenderTarget)
			If _fill Then
				renderTarget.FillRoundedRectangle(_rect, _parent.brushes(_selectedBrushIndex))
			Else
				renderTarget.DrawRoundedRectangle(_rect, _parent.brushes(_selectedBrushIndex), _strokeWidth)
			End If
		End Sub

		Protected Friend Overrides WriteOnly Property EndPoint() As Point2F
			Set(ByVal value As Point2F)
                _rect.Rect = New RectF(_rect.Rect.Left, _rect.Rect.Top, value.X, value.Y)
			End Set
		End Property
	End Class
End Namespace