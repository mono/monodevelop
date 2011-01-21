' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports Microsoft.WindowsAPICodePack.DirectX.Direct2D1

Namespace D2DPaint
	Friend Class EllipseShape
		Inherits DrawingShape
		Friend _ellipse As Ellipse
		Friend _strokeWidth As Single
		Friend _selectedBrushIndex As Integer
		Private _startPoint As Point2F


		Friend Sub New(ByVal parent As Paint2DForm, ByVal ellipse As Ellipse, ByVal strokeWidth As Single, ByVal selectedBrush As Integer, ByVal fill As Boolean)
			MyBase.New(parent, fill)
			_startPoint = ellipse.Point
			_ellipse = ellipse
			_strokeWidth = strokeWidth
			_selectedBrushIndex = selectedBrush
		End Sub

		Protected Friend Overrides Sub Draw(ByVal renderTarget As RenderTarget)
			If _fill Then
				renderTarget.FillEllipse(_ellipse, _parent.brushes(_selectedBrushIndex))
			Else
				renderTarget.DrawEllipse(_ellipse, _parent.brushes(_selectedBrushIndex), _strokeWidth)
			End If
		End Sub
		Protected Friend Overrides WriteOnly Property EndPoint() As Point2F
			Set(ByVal value As Point2F)
				_ellipse.RadiusX = (value.X - _startPoint.X) / 2f
				_ellipse.RadiusY = (value.Y - _startPoint.Y) / 2f

                _ellipse.Point = New Point2F(_startPoint.X + ((value.X - _startPoint.X) / 2.0F), _startPoint.Y + ((value.Y - _startPoint.Y) / 2.0F))
			End Set
		End Property
	End Class
End Namespace
