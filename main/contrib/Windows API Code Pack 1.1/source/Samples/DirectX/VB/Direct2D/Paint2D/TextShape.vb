' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports Microsoft.WindowsAPICodePack.DirectX.Direct2D1
Imports Microsoft.WindowsAPICodePack.DirectX.DirectWrite

Namespace D2DPaint
	Friend Class TextShape
		Inherits DrawingShape
		Friend _maxX As Single
		Friend _maxY As Single
		Friend _point0 As Point2F
		Friend _textLayout As TextLayout
		Friend _selectedBrushIndex As Integer

		Private _drawBorder As Boolean = True

		Friend Sub New(ByVal parent As Paint2DForm, ByVal textLayout As TextLayout, ByVal startPoint As Point2F, ByVal maxX As Single, ByVal maxY As Single, ByVal selectedBrush As Integer)
			MyBase.New(parent)
			_maxX = maxX
			_maxY = maxY
			_textLayout = textLayout
			_selectedBrushIndex = selectedBrush
			_point0 = startPoint
		End Sub

		Protected Friend Overrides Sub Draw(ByVal renderTarget As RenderTarget)
			If _drawBorder Then

				renderTarget.DrawRectangle(New RectF(_point0.X, _point0.Y, _point0.X + _textLayout.MaxWidth, _point0.Y + _textLayout.MaxHeight), _parent.brushes(0), 1.5f, _parent.TextBoxStroke)
			End If

			renderTarget.DrawTextLayout(_point0, _textLayout, _parent.brushes(_selectedBrushIndex), DrawTextOptions.Clip)
		End Sub

		Protected Friend Overrides Sub EndDraw()
			_drawBorder = False
		End Sub

		Protected Friend Overrides WriteOnly Property EndPoint() As Point2F
			Set(ByVal value As Point2F)
				_textLayout.MaxWidth = Math.Max(5, value.X - _point0.X)
				_textLayout.MaxHeight = Math.Max(5, value.Y - _point0.Y)
			End Set
		End Property
	End Class
End Namespace