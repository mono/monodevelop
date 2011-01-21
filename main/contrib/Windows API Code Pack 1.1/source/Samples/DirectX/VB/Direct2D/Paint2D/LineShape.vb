' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports Microsoft.WindowsAPICodePack.DirectX.Direct2D1

Namespace D2DPaint
	Friend Class LineShape
		Inherits DrawingShape
		Friend _point0, _point1 As Point2F
		Friend _strokeWidth As Single
		Friend _selectedBrushIndex As Integer

		Friend Sub New(ByVal parent As Paint2DForm, ByVal point0 As Point2F, ByVal point1 As Point2F, ByVal strokeWidth As Single, ByVal selectedBrush As Integer)
			MyBase.New(parent)
			_point0 = point0
			_point1 = point1
			_strokeWidth = strokeWidth
			_selectedBrushIndex = selectedBrush

		End Sub

		Protected Friend Overrides Sub Draw(ByVal renderTarget As RenderTarget)
			renderTarget.DrawLine(_point0, _point1, _parent.brushes(_selectedBrushIndex), _strokeWidth)
		End Sub

		Protected Friend Overrides WriteOnly Property EndPoint() As Point2F
			Set(ByVal value As Point2F)
				_point1 = value
			End Set
		End Property
	End Class
End Namespace