' Copyright (c) Microsoft Corporation.  All rights reserved.

Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports Microsoft.WindowsAPICodePack.DirectX.Direct2D1

Namespace D2DShapes
	Friend Class GeometryShape
		Inherits DrawingShape
'INSTANT VB NOTE: The variable geometry was renamed since Visual Basic does not allow class members with the same name:
		Private geometry_Renamed As Geometry
		Friend geometryOutlined As PathGeometry
		Friend geometrySimplified As PathGeometry
		Friend geometryWidened As PathGeometry
		Friend worldTransform? As Matrix3x2F

		Public Sub New(ByVal initialRenderTarget As RenderTarget, ByVal random As Random, ByVal d2DFactory As D2DFactory, ByVal bitmap As D2DBitmap)
			MyBase.New(initialRenderTarget, random, d2DFactory, bitmap)
			coolStrokes = CoinFlip
			Dim which As Double = Random.NextDouble()
			If which < 0.67 OrElse coolStrokes Then
				PenBrush = RandomBrush()
			End If
			If (Not coolStrokes) AndAlso which > 0.33 Then
				FillBrush = RandomBrush()
			End If
			If coolStrokes OrElse CoinFlip Then
				StrokeStyle = RandomStrokeStyle()
			End If
			If CoinFlip Then
				worldTransform = RandomMatrix3x2()
			End If
			StrokeWidth = RandomStrokeWidth()
			geometry_Renamed = RandomGeometry()
			If coolStrokes OrElse Random.NextDouble() < 0.3 Then
				ModifyGeometry()
			End If
			If coolStrokes AndAlso CoinFlip Then
				ModifyGeometry()
			End If
		End Sub

		<TypeConverter(GetType(ExpandableObjectConverter))> _
		Public Property Geometry() As Geometry
			Get
				Return geometry_Renamed
			End Get
			Set(ByVal value As Geometry)
				geometry_Renamed = value
			End Set
		End Property

		Private Sub ModifyGeometry()
			Dim geometrySink As GeometrySink
			Dim which As Double = Random.NextDouble()
			If which < 0.33 Then
				geometryOutlined = d2DFactory.CreatePathGeometry()
				geometrySink = geometryOutlined.Open()
                geometry_Renamed.Outline(geometrySink, FlatteningTolerance)
				geometrySink.Close()
				geometrySink.Dispose()
				geometry_Renamed.Dispose()
				geometry_Renamed = geometryOutlined
			ElseIf which < 0.67 Then
				geometrySimplified = d2DFactory.CreatePathGeometry()
				geometrySink = geometrySimplified.Open()
                geometry_Renamed.Simplify(If(CoinFlip, GeometrySimplificationOption.Lines, GeometrySimplificationOption.CubicsAndLines), geometrySink, FlatteningTolerance)
				geometrySink.Close()
				geometrySink.Dispose()
				geometry_Renamed.Dispose()
				geometry_Renamed = geometrySimplified
			Else
				geometryWidened = d2DFactory.CreatePathGeometry()
				geometrySink = geometryWidened.Open()
                geometry_Renamed.Widen(RandomStrokeWidth(), RandomStrokeStyle(), geometrySink, FlatteningTolerance)
				geometrySink.Close()
				geometrySink.Dispose()
				geometry_Renamed.Dispose()
				geometry_Renamed = geometryWidened
			End If
		End Sub

        'this could be called recursively
        Public Function RandomGeometry() As Geometry
            Return RandomGeometry(0)
        End Function

        Public Function RandomGeometry(ByVal level As Integer) As Geometry
            Dim which As Double = Random.NextDouble()
            Dim g As Geometry = Nothing

            While g Is Nothing
                If which < 0.2 Then
                    g = RandomEllipseGeometry()
                ElseIf which < 0.4 Then
                    g = RandomRoundRectGeometry()
                ElseIf which < 0.6 Then
                    g = RandomRectangleGeometry()
                ElseIf which < 0.8 Then
                    g = RandomPathGeometry()
                ElseIf level < 3 Then
                    g = RandomGeometryGroup(level + 1)
                End If
            End While

            If worldTransform.HasValue Then
                g = d2DFactory.CreateTransformedGeometry(g, worldTransform.Value)
            End If
            Return g
        End Function

		Private Function RandomTransformedGeometry() As Geometry
			Dim start, ret As Geometry
			start = RandomGeometry()
			ret = d2DFactory.CreateTransformedGeometry(start, RandomMatrix3x2())
			start.Dispose()
			Return ret
		End Function

        Private Function RandomGeometryGroup(ByVal level As Integer) As Geometry
            Dim geometries = New List(Of Geometry)()
            Dim count As Integer = Random.Next(1, 5)
            For i As Integer = 0 To count - 1
                geometries.Add(RandomGeometry(level))
            Next i
            Dim ret As GeometryGroup = d2DFactory.CreateGeometryGroup(If(Random.NextDouble() < 0.5, FillMode.Winding, FillMode.Alternate), geometries)
            For Each g In geometries
                g.Dispose()
            Next g
            Return ret
        End Function

		Private Function RandomPathGeometry() As PathGeometry
			Dim g As PathGeometry = d2DFactory.CreatePathGeometry()
			Dim totalSegmentCount As Integer = 0
			Dim figureCount As Integer = Random.Next(1, 2)
            Using sink As GeometrySink = g.Open()
                For f As Integer = 0 To figureCount - 1
                    Dim segmentCount As Integer = Random.Next(2, 20)
                    AddRandomFigure(sink, segmentCount)
                    totalSegmentCount += segmentCount
                Next f
                sink.Close()
            End Using
            System.Diagnostics.Debug.Assert(g.SegmentCount = totalSegmentCount)
            System.Diagnostics.Debug.Assert(g.FigureCount = figureCount)
            Return g
        End Function

		Private Sub AddRandomFigure(ByVal sink As IGeometrySink, ByVal segmentCount As Integer)
			previousPoint = Nothing
			sink.BeginFigure(RandomNearPoint(),If(CoinFlip, FigureBegin.Filled, FigureBegin.Hollow))
			Dim [end] As FigureEnd = If(CoinFlip, FigureEnd.Closed, FigureEnd.Closed)
            If [end] = FigureEnd.Closed Then
                segmentCount -= 1
            End If
			If CoinFlip Then
				For i As Integer = 0 To segmentCount - 1
					AddRandomSegment(sink)
				Next i
			Else
				Dim which As Double = Random.NextDouble()
				If which < 0.33 Then
					sink.AddLines(RandomLines(segmentCount))
				ElseIf which < 0.67 Then
					sink.AddQuadraticBeziers(RandomQuadraticBeziers(segmentCount))
				Else
					sink.AddBeziers(RandomBeziers(segmentCount))
				End If
			End If
			sink.EndFigure([end])
		End Sub

		Private Function RandomLines(ByVal segmentCount As Integer) As IEnumerable(Of Point2F)
			Dim lines = New List(Of Point2F)()
			For i As Integer = 0 To segmentCount - 1
				lines.Add(RandomNearPoint())
			Next i
			Return lines
		End Function

		Private Function RandomQuadraticBeziers(ByVal segmentCount As Integer) As IEnumerable(Of QuadraticBezierSegment)
			Dim beziers = New List(Of QuadraticBezierSegment)()
			For i As Integer = 0 To segmentCount - 1
				beziers.Add(New QuadraticBezierSegment(RandomNearPoint(), RandomNearPoint()))
			Next i
			Return beziers
		End Function

		Private Function RandomBeziers(ByVal segmentCount As Integer) As IEnumerable(Of BezierSegment)
			Dim beziers = New List(Of BezierSegment)()
			For i As Integer = 0 To segmentCount - 1
				beziers.Add(New BezierSegment(RandomNearPoint(), RandomNearPoint(), RandomNearPoint()))
			Next i
			Return beziers
		End Function

		Private Sub AddRandomSegment(ByVal sink As IGeometrySink)
			Dim which As Double = Random.NextDouble()
			If which < 0.25 Then
				sink.AddLine(RandomNearPoint())
			ElseIf which < 0.5 Then
				sink.AddArc(RandomArc())
			ElseIf which < 0.75 Then
				sink.AddBezier(RandomBezier())
			ElseIf which < 1.0 Then
			sink.AddQuadraticBezier(RandomQuadraticBezier())
			End If
		End Sub

		Private Function RandomQuadraticBezier() As QuadraticBezierSegment
			Return New QuadraticBezierSegment(RandomNearPoint(), RandomNearPoint())
		End Function

		Private Function RandomBezier() As BezierSegment
			Return New BezierSegment(RandomNearPoint(), RandomNearPoint(), RandomNearPoint())
		End Function

		Private Function RandomArc() As ArcSegment
			Return New ArcSegment(RandomNearPoint(), RandomSize(), CSng(Random.NextDouble()) * 360,If(CoinFlip, SweepDirection.Clockwise, SweepDirection.CounterClockwise),If(CoinFlip, ArcSize.Large, ArcSize.Small))
		End Function

		Private Function RandomSize() As SizeF
			Return New SizeF(CSng(Random.NextDouble()) * CanvasWidth, CSng(Random.NextDouble()) * CanvasHeight)
		End Function

		Private Function RandomRoundRectGeometry() As RoundedRectangleGeometry
			Return d2DFactory.CreateRoundedRectangleGeometry(RandomRoundedRect())
		End Function

		Private Function RandomRectangleGeometry() As RectangleGeometry
			Return d2DFactory.CreateRectangleGeometry(RandomRect(CanvasWidth, CanvasHeight))
		End Function

		Private Function RandomEllipseGeometry() As EllipseGeometry
			Return d2DFactory.CreateEllipseGeometry(RandomEllipse())
		End Function

		Protected Friend Overrides Sub ChangeRenderTarget(ByVal newRenderTarget As RenderTarget)
			PenBrush = CopyBrushToRenderTarget(PenBrush, newRenderTarget)
			FillBrush = CopyBrushToRenderTarget(FillBrush, newRenderTarget)
		End Sub

		Protected Friend Overrides Sub Draw(ByVal renderTarget As RenderTarget)
			Dim g As Geometry = geometry_Renamed
			If FillBrush IsNot Nothing Then
				renderTarget.FillGeometry(g, FillBrush, Nothing)
			End If
			If PenBrush IsNot Nothing Then
				If StrokeStyle IsNot Nothing Then
					renderTarget.DrawGeometry(g, PenBrush, StrokeWidth, StrokeStyle)
				Else
					renderTarget.DrawGeometry(g, PenBrush, StrokeWidth)
				End If
			End If
		End Sub

		Public Overrides Function HitTest(ByVal point As Point2F) As Boolean
			Return geometry_Renamed.FillContainsPoint(point, FlatteningTolerance)
		End Function

		Public Overrides Sub Dispose()
			If geometry_Renamed IsNot Nothing Then
				geometry_Renamed.Dispose()
			End If
			geometry_Renamed = Nothing
			MyBase.Dispose()
		End Sub
	End Class
End Namespace
