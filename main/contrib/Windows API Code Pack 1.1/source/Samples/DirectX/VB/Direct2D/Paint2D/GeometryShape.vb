' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports Microsoft.WindowsAPICodePack.DirectX.Direct2D1

Namespace D2DPaint
	Friend Class GeometryShape
		Inherits DrawingShape
		Friend _points As List(Of Point2F)
		Friend _strokeWidth As Single
		Friend _selectedBrushIndex As Integer
		Friend _geometry As Geometry

		Friend Sub New(ByVal parent As Paint2DForm, ByVal point0 As Point2F, ByVal strokeWidth As Single, ByVal selectedBrush As Integer, ByVal fill As Boolean)
			MyBase.New(parent)
			_points = New List(Of Point2F) (New Point2F() {point0})
			_strokeWidth = strokeWidth
			_selectedBrushIndex = selectedBrush
			_fill = fill
		End Sub

		Protected Friend Overrides Sub Draw(ByVal renderTarget As RenderTarget)
			If _geometry IsNot Nothing Then
				If _fill Then
					renderTarget.FillGeometry(_geometry, _parent.brushes(_selectedBrushIndex))
				Else
					renderTarget.DrawGeometry(_geometry, _parent.brushes(_selectedBrushIndex), _strokeWidth, _parent.d2dFactory.CreateStrokeStyle(New StrokeStyleProperties(CapStyle.Round, CapStyle.Round, CapStyle.Round, LineJoin.Round, 1, DashStyle.Solid, 0)))
				End If
			End If
		End Sub

		Protected Friend Overrides WriteOnly Property EndPoint() As Point2F
			Set(ByVal value As Point2F)
				_points.Add(value)
				Dim g As PathGeometry = _parent.d2dFactory.CreatePathGeometry()
				Dim sink = g.Open()
				sink.BeginFigure(_points(0),If(_fill, FigureBegin.Filled, FigureBegin.Hollow))
				For i As Integer = 1 To _points.Count - 1
					'smoothing
					If i > 1 AndAlso i < _points.Count - 1 Then
						Dim cp1 As Point2F
						Dim cp2 As Point2F
						GetSmoothingPoints(i, cp1, cp2)
						sink.AddBezier(New BezierSegment(cp1, cp2, _points(i)))
					Else
						sink.AddLine(_points(i))
					End If
				Next i
				sink.EndFigure(If(_fill, FigureEnd.Closed, FigureEnd.Open))
				sink.Close()
				_geometry = g
			End Set
		End Property

		Private Sub GetSmoothingPoints(ByVal i As Integer, <System.Runtime.InteropServices.Out()> ByRef cp1 As Point2F, <System.Runtime.InteropServices.Out()> ByRef cp2 As Point2F)
			Dim smoothing As Single =.25f '0 - no smoothing
			Dim lx As Single = _points(i).X - _points(i - 1).X
			Dim ly As Single = _points(i).Y - _points(i - 1).Y
			Dim l As Single = CSng(Math.Sqrt(lx * lx + ly * ly)) ' distance from previous point
			Dim l1x As Single = _points(i).X - _points(i - 2).X
			Dim l1y As Single = _points(i).Y - _points(i - 2).Y
			Dim l1 As Single = CSng(Math.Sqrt(l1x * l1x + l1y * l1y)) ' distance between two points back and current point
			Dim l2x As Single = _points(i + 1).X - _points(i - 1).X
			Dim l2y As Single = _points(i + 1).Y - _points(i - 1).Y
			Dim l2 As Single = CSng(Math.Sqrt(l2x * l2x + l2y * l2y)) 'distance between previous point and the next point

			cp1 = New Point2F(_points(i - 1).X + (If(l1x = 0, 0, (smoothing * l * l1x / l1))), _points(i - 1).Y + (If(l1y = 0, 0, (smoothing * l * l1y / l1))))
			cp2 = New Point2F(_points(i).X - (If(l2x = 0, 0, (smoothing * l * l2x / l2))), _points(i).Y - (If(l2y = 0, 0, (smoothing * l * l2y / l2))))
		End Sub
	End Class
End Namespace