' Copyright (c) Microsoft Corporation.  All rights reserved.

Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
#If _D2DTRACE Then
Imports System.Diagnostics
#End If
Imports Microsoft.WindowsAPICodePack.DirectX.Direct2D1

Namespace D2DShapes
	Public MustInherit Class DrawingShape
		Implements IDisposable
		#Region "Properties and Fields"
		Private Shared shapesCreated As Integer
		Private shapeID As Integer
		Private privateRandom As Random
		Protected Friend Property Random() As Random
			Get
				Return privateRandom
			End Get
			Set(ByVal value As Random)
				privateRandom = value
			End Set
		End Property
		Protected Friend d2DFactory As D2DFactory

'INSTANT VB NOTE: The variable bitmap was renamed since Visual Basic does not allow class members with the same name:
		Protected bitmap_Renamed As D2DBitmap
		Friend Overridable Property Bitmap() As D2DBitmap
			Get
				Return bitmap_Renamed
			End Get
			Set(ByVal value As D2DBitmap)
				bitmap_Renamed = value
			End Set
		End Property

		Private privateFillBrush As Brush
		<TypeConverter(GetType(ExpandableObjectConverter))> _
		Public Property FillBrush() As Brush
			Get
				Return privateFillBrush
			End Get
			Set(ByVal value As Brush)
				privateFillBrush = value
			End Set
		End Property

		Private privatePenBrush As Brush
		<TypeConverter(GetType(ExpandableObjectConverter))> _
		Public Property PenBrush() As Brush
			Get
				Return privatePenBrush
			End Get
			Set(ByVal value As Brush)
				privatePenBrush = value
			End Set
		End Property

		Private privateStrokeWidth As Single
		Public Property StrokeWidth() As Single
			Get
				Return privateStrokeWidth
			End Get
			Set(ByVal value As Single)
				privateStrokeWidth = value
			End Set
		End Property

		Private privateStrokeStyle As StrokeStyle
		<TypeConverter(GetType(ExpandableObjectConverter))> _
		Public Property StrokeStyle() As StrokeStyle
			Get
				Return privateStrokeStyle
			End Get
			Set(ByVal value As StrokeStyle)
				privateStrokeStyle = value
			End Set
		End Property

		Protected Const FlatteningTolerance As Single = 5

'INSTANT VB NOTE: The variable renderTarget was renamed since Visual Basic does not allow class members with the same name:
		Private renderTarget_Renamed As RenderTarget
		<TypeConverter(GetType(ExpandableObjectConverter))> _
		Public Property RenderTarget() As RenderTarget
			Get
				Return renderTarget_Renamed
			End Get
			Set(ByVal value As RenderTarget)
				If renderTarget_Renamed IsNot value Then
					ChangeRenderTarget(value)
					renderTarget_Renamed = value
				End If
			End Set
		End Property

		Protected coolStrokes As Boolean 'used by GeometryShape to create geometries from modified dashed strokes

		Public Overridable ReadOnly Property ChildShapes() As List(Of DrawingShape)
			Get
				Return Nothing
			End Get
		End Property

		Protected ReadOnly Property CanvasWidth() As Integer
			Get
				Return CInt(Fix(RenderTarget.PixelSize.Width))
			End Get
		End Property

		Protected ReadOnly Property CanvasHeight() As Integer
			Get
				Return CInt(Fix(RenderTarget.PixelSize.Height))
			End Get
		End Property
		#End Region

		#Region "DrawingShape() - CTOR"
		Protected Sub New(ByVal initialRenderTarget As RenderTarget, ByVal random As Random, ByVal d2DFactory As D2DFactory, ByVal bitmap As D2DBitmap)
			renderTarget_Renamed = initialRenderTarget
            Me.Random = random
			Me.d2DFactory = d2DFactory
			Me.bitmap_Renamed = bitmap
			shapesCreated += 1
			shapeID = shapesCreated
		End Sub
		#End Region

		Public Overrides Function ToString() As String
			Return shapeID & ":" & Me.GetType().Name
		End Function

		#Region "Virtual methods"
		''' <summary>
		''' Draws the shape to the specified render target.
		''' </summary>
		''' <param name="renderTarget">The render target.</param>
'INSTANT VB NOTE: The variable renderTarget was renamed since Visual Basic does not allow class members with the same name:
		Protected Friend MustOverride Sub Draw(ByVal renderTarget_Renamed As RenderTarget)
		''' <summary>
		''' Changes the render target of the shape - need to create copies of all render target-dependent properties to the new render target. 
		''' </summary>
		''' <param name="newRenderTarget">The render target.</param>
		Protected Friend MustOverride Sub ChangeRenderTarget(ByVal newRenderTarget As RenderTarget)
		''' <summary>
		''' Hit test of the shape.
		''' </summary>
		''' <param name="point">The point.</param>
		''' <returns>true if the given point belongs to the shape</returns>
		Public Overridable Function HitTest(ByVal point As Point2F) As Boolean
			Return False
		End Function
		#End Region

		#Region "Methods to randomize properties of shapes"
		#Region "CoinFlip"
		Protected Friend ReadOnly Property CoinFlip() As Boolean
			Get
				Return Random.NextDouble() < 0.5
			End Get
		End Property
		#End Region

		#Region "RandomStrokeWidth()"
		Protected Function RandomStrokeWidth() As Single
			Dim ret As Single = CSng(32 * Random.NextDouble() * Random.NextDouble())
#If _D2DTRACE Then
			Trace.WriteLine("Stroke width: " & ret)
#End If
			Return ret
		End Function
		#End Region

		#Region "RandomOpacity()"
		Protected Function RandomOpacity() As Single
			Dim ret As Single = Math.Min(1f, Math.Max(0f, 1.2f - CSng(Random.NextDouble() * 1.2)))
#If _D2DTRACE Then
			Trace.WriteLine("Opacity: " & ret)
#End If
			Return ret
		End Function
		#End Region

		#Region "RandomPoint()"
		Protected Friend Function RandomPoint() As Point2F
			previousPoint = New Point2F(CSng(Random.NextDouble()) * CanvasWidth, CSng(Random.NextDouble()) * CanvasHeight)
#If _D2DTRACE Then
			Trace.WriteLine("Point: " & previousPoint.Value.X & "," & previousPoint.Value.Y)
#End If
			Return previousPoint.Value
		End Function
		#End Region

		#Region "RandomNearPoint()"
		Protected Friend previousPoint? As Point2F
		Protected Friend Function RandomNearPoint() As Point2F
			Dim ret As Point2F
			If Not previousPoint.HasValue OrElse CoinFlip Then
				ret = RandomPoint()
			Else
				ret = New Point2F(previousPoint.Value.X + CSng(Random.NextDouble()) * 100 - 50, previousPoint.Value.Y + CSng(Random.NextDouble()) * 100 - 50)
			End If
			previousPoint = ret
#If _D2DTRACE Then
			Trace.WriteLine("Point: " & previousPoint.Value.X & "," & previousPoint.Value.Y)
#End If
			Return ret
		End Function
		#End Region

		#Region "RandomRect()"
		Protected Friend Function RandomRect(ByVal maxWidth As Single, ByVal maxHeight As Single) As RectF
			Dim x1 As Single = CSng(Random.NextDouble()) * maxWidth
			Dim x2 As Single = CSng(Random.NextDouble()) * maxWidth
			Dim y1 As Single = CSng(Random.NextDouble()) * maxHeight
			Dim y2 As Single = CSng(Random.NextDouble()) * maxHeight
			'return new RectF(x1, y1, x2, y2);
			Dim ret As New RectF(Math.Min(x1, x2), Math.Min(y1, y2), Math.Max(x1, x2), Math.Max(y1, y2))
#If _D2DTRACE Then
			Trace.WriteLine("RectF: Left:" & ret.Left & ", Top:" & ret.Top & ", Right:" & ret.Right & ", Bottom:" & ret.Bottom)
#End If
			Return ret
		End Function
		#End Region

		#Region "RandomBrush()"
		Protected Friend Function RandomBrush() As Brush
			Dim which As Double = Random.NextDouble()
			If which < 0.5 Then
				Return RandomSolidBrush()
			End If
			If which < 0.7 Then
				Return RandomGradientBrush()
			End If
			If which < 0.9 Then
				Return RandomRadialBrush()
			End If
			Return RandomBitmapBrush()
		End Function
		#End Region

		#Region "RandomSolidBrush()"
		Protected Friend Function RandomSolidBrush() As SolidColorBrush
#If _D2DTRACE Then
			Trace.WriteLine("SolidBrush:")
#End If
			Return RenderTarget.CreateSolidColorBrush(RandomColor(), RandomBrushProperties())
		End Function
		#End Region

		#Region "RandomGradientBrush()"
		Protected Friend Function RandomGradientBrush() As LinearGradientBrush
#If _D2DTRACE Then
			Trace.WriteLine("LinearGradientBrush:")
#End If
			Return RenderTarget.CreateLinearGradientBrush(New LinearGradientBrushProperties(RandomPoint(), RandomPoint()), RandomGradientStopCollection(), RandomBrushProperties())
		End Function
		#End Region

		#Region "RandomRadialBrush()"
		Protected Friend Function RandomRadialBrush() As RadialGradientBrush
#If _D2DTRACE Then
			Trace.WriteLine("RadialGradientBrush:")
#End If
			Dim radiusX As Single = CSng(Random.NextDouble() * CanvasWidth)
			Dim radiusY As Single = CSng(Random.NextDouble() * CanvasHeight)
#If _D2DTRACE Then
			Trace.WriteLine("Radius: " & radiusX & "," & radiusY)
#End If
			Return RenderTarget.CreateRadialGradientBrush(New RadialGradientBrushProperties(RandomPoint(), RandomPoint(), radiusX, radiusY), RandomGradientStopCollection(), RandomBrushProperties())
		End Function
		#End Region

		#Region "RandomBitmapBrush()"
		Private Function RandomBitmapBrush() As BitmapBrush
#If _D2DTRACE Then
			Trace.WriteLine("SolidBrush:")
#End If
			Dim interpolationMode As BitmapInterpolationMode = If(Random.NextDouble() < 0.25, BitmapInterpolationMode.Linear, BitmapInterpolationMode.NearestNeighbor)
#If _D2DTRACE Then
			Trace.WriteLine("BitmapInterpolationMode: " & interpolationMode)
#End If
			Dim ret As BitmapBrush = RenderTarget.CreateBitmapBrush(Bitmap, New BitmapBrushProperties(RandomExtendMode(), RandomExtendMode(), interpolationMode), New BrushProperties(RandomOpacity(), RandomMatrix3x2()))
			Return ret
		End Function
		#End Region

		#Region "RandomGradientStopCollection()"
		Private Function RandomGradientStopCollection() As GradientStopCollection
			Dim stopsCount As Integer = Random.Next(2, 16)
			Dim stopPoints = New List(Of Single)()
			For i As Integer = 0 To stopsCount - 1
				stopPoints.Add(CSng(Random.NextDouble()))
			Next i
			stopPoints.Sort()
			Dim stops = New GradientStop(stopsCount - 1){}
			For i As Integer = 0 To stopsCount - 1
				stops(i) = New GradientStop(stopPoints(i), RandomColor())
			Next i
            Dim gamma As Gamma = If(Random.NextDouble() < 0.7, gamma.StandardRgb, gamma.Linear)
#If _D2DTRACE Then
			Trace.WriteLine("GradientStopCollection:")
			Trace.WriteLine("  Gamma: " & gamma)
			For Each [stop] In stops
				Trace.WriteLine(String.Format("  GradientStop: Stop: {0}, Color (RGBA): {1},{2},{3},{4}", [stop].Position, [stop].Color.R, [stop].Color.G, [stop].Color.B, [stop].Color.A))
			Next [stop]
#End If
			Return RenderTarget.CreateGradientStopCollection(stops, gamma, RandomExtendMode())
		End Function
		#End Region

		#Region "RandomExtendMode()"
		Private Function RandomExtendMode() As ExtendMode
			Dim which As Double = Random.NextDouble()
			Dim ret As ExtendMode = If(which < 0.33, ExtendMode.Wrap, If(which < 0.65, ExtendMode.Mirror, ExtendMode.Clamp))
#If _D2DTRACE Then
			Trace.WriteLine("  ExtendMode:" & ret)
#End If
			Return ret
		End Function
		#End Region

		#Region "RandomBrushProperties()"
		Private Function RandomBrushProperties() As BrushProperties
			Dim opacity As Single = CSng(Random.NextDouble())
#If _D2DTRACE Then
			Trace.WriteLine("BrushProperties: Opacity: " & opacity)
#End If
			Return New BrushProperties(opacity, RandomMatrix3x2())
		End Function
		#End Region

		#Region "RandomMatrix3x2()"
		Protected Friend Function RandomMatrix3x2() As Matrix3x2F
			Dim which = Random.NextDouble()
			'return Matrix3x2F.Skew(90, 0); //check for bug 730701
			Dim ret As Matrix3x2F
			If which < 0.5 Then
				ret = New Matrix3x2F(1.0f - CSng(Random.NextDouble())*CSng(Random.NextDouble()), CSng(Random.NextDouble())*CSng(Random.NextDouble()), CSng(Random.NextDouble())*CSng(Random.NextDouble()), 1.0f - CSng(Random.NextDouble())*CSng(Random.NextDouble()), CSng(Random.NextDouble())*CSng(Random.NextDouble()), CSng(Random.NextDouble())*CSng(Random.NextDouble()))
				TraceMatrix(ret)
				Return ret
			End If
			If which < 0.8 Then
				ret = Matrix3x2F.Identity
				TraceMatrix(ret)
				Return ret
			End If
			If which < 0.85 Then
				ret = Matrix3x2F.Translation(Random.Next(-20, 20), Random.Next(-20, 20))
				TraceMatrix(ret)
				Return ret
			End If
			If which < 0.90 Then
				ret = Matrix3x2F.Skew(CSng(Random.NextDouble() * Random.NextDouble() * 89), CSng(Random.NextDouble() * Random.NextDouble() * 89),If(CoinFlip, New Point2F(0, 0), RandomPoint()))
				TraceMatrix(ret)
				Return ret
			End If
			If which < 0.95 Then
				ret = Matrix3x2F.Scale(1 + CSng((Random.NextDouble() - 0.5) * Random.NextDouble()), 1 + CSng((Random.NextDouble() - 0.5) * Random.NextDouble()),If(CoinFlip, New Point2F(0, 0), RandomPoint()))
				TraceMatrix(ret)
				Return ret
			End If
			ret = Matrix3x2F.Rotation(CSng((Random.NextDouble() - 0.5) * Random.NextDouble() * 720),If(CoinFlip, New Point2F(0,0), RandomPoint()))
			TraceMatrix(ret)
			Return ret
		End Function

		Private Shared Sub TraceMatrix(ByVal matrix As Matrix3x2F)
#If _D2DTRACE Then
			Trace.WriteLine(String.Format("  Matrix3x2: {0}, {1}", matrix.M11, matrix.M12))
			Trace.WriteLine(String.Format("             {0}, {1}", matrix.M21, matrix.M22))
			Trace.WriteLine(String.Format("             {0}, {1}", matrix.M31, matrix.M32))
#End If
		End Sub
		#End Region

		#Region "RandomColor()"
		Protected Friend Function RandomColor() As ColorF
			Dim ret As New ColorF(CSng(Random.NextDouble()), CSng(Random.NextDouble()), CSng(Random.NextDouble()), RandomOpacity())
#If _D2DTRACE Then
			Trace.WriteLine(String.Format("ColorF (RGBA): {0},{1},{2},{3}", ret.R, ret.G, ret.B, ret.A))
#End If
			Return ret
		End Function
		#End Region

		#Region "RandomStrokeStyle()"
		Protected Friend Function RandomStrokeStyle() As StrokeStyle
			Dim strokeStyleProperties = New StrokeStyleProperties(RandomCapStyle(), RandomCapStyle(), RandomCapStyle(), RandomLineJoin(), 1.0f + 2.0f * CSng(Random.NextDouble()), RandomDashStyle(), 5.0f * CSng(Random.NextDouble()))
			If strokeStyleProperties.DashStyle = DashStyle.Custom Then
				Return d2DFactory.CreateStrokeStyle(strokeStyleProperties, RandomDashes())
			Else
				Return d2DFactory.CreateStrokeStyle(strokeStyleProperties)
			End If
		End Function
		#End Region

		#Region "RandomDashes()"
		Private Function RandomDashes() As Single()
			Dim dashes = New Single(Random.Next(2, 20) - 1){}
			For i As Integer = 0 To dashes.Length - 1
				dashes(i) = 3.0f * CSng(Random.NextDouble())
			Next i
			Return dashes
		End Function
		#End Region

		#Region "RandomDashStyle()"
		Private Function RandomDashStyle() As DashStyle
			Dim which As Double = Random.NextDouble()
			If (Not coolStrokes) AndAlso which < 0.5 Then
				Return DashStyle.Solid
			End If
			If which < 0.75 Then
				Return DashStyle.Custom
			End If
			Return CType(Random.Next(CInt(DashStyle.Dash), CInt(DashStyle.DashDotDot)), DashStyle)
		End Function
		#End Region

		#Region "RandomLineJoin()"
		Private Function RandomLineJoin() As LineJoin
			Return CType(Random.Next(0, 3), LineJoin)
		End Function
		#End Region

		#Region "RandomCapStyle()"
		Private Function RandomCapStyle() As CapStyle
			Return CType(Random.Next(0, 3), CapStyle)
		End Function
		#End Region

		#Region "RandomEllipse()"
		Protected Friend Function RandomEllipse() As Ellipse
			Return New Ellipse(RandomPoint(), CSng(0.5 * CanvasWidth * Random.NextDouble()), CSng(0.5 * CanvasHeight * Random.NextDouble()))
		End Function
		#End Region

		#Region "RandomRoundedRect()"
		Protected Friend Function RandomRoundedRect() As RoundedRect
			Return New RoundedRect(RandomRect(CanvasWidth, CanvasHeight), CSng(32 * Random.NextDouble()), CSng(32 * Random.NextDouble()))
		End Function
		#End Region 
		#End Region

		#Region "CopyBrushToRenderTarget()"
		''' <summary>
		''' Creates and returns a copy of the brush in the new render target.
		''' Used for changing render targets.
		''' A brush belongs to a render target, so when you want to draw with same brush in another render target
		''' - you need to create a copy of the brush in the new render target.
		''' </summary>
		''' <param name="sourceBrush">The brush.</param>
		''' <param name="newRenderTarget">The new render target.</param>
		''' <returns></returns>
		Protected Friend Function CopyBrushToRenderTarget(ByVal sourceBrush As Brush, ByVal newRenderTarget As RenderTarget) As Brush
			If sourceBrush Is Nothing OrElse newRenderTarget Is Nothing Then
				Return Nothing
			End If
			Dim newBrush As Brush
			If TypeOf sourceBrush Is SolidColorBrush Then
				newBrush = newRenderTarget.CreateSolidColorBrush((CType(sourceBrush, SolidColorBrush)).Color, New BrushProperties(sourceBrush.Opacity, sourceBrush.Transform))
				sourceBrush.Dispose()
				Return newBrush
			End If
			If TypeOf sourceBrush Is LinearGradientBrush Then
                Dim oldGSC = (CType(sourceBrush, LinearGradientBrush)).GradientStops
                Dim newGSC = newRenderTarget.CreateGradientStopCollection(oldGSC, oldGSC.ColorInterpolationGamma, oldGSC.ExtendMode)
				oldGSC.Dispose()
				newBrush = newRenderTarget.CreateLinearGradientBrush(New LinearGradientBrushProperties((CType(sourceBrush, LinearGradientBrush)).StartPoint, (CType(sourceBrush, LinearGradientBrush)).EndPoint), newGSC, New BrushProperties(sourceBrush.Opacity, sourceBrush.Transform))
				sourceBrush.Dispose()
				Return newBrush
			End If
			If TypeOf sourceBrush Is RadialGradientBrush Then
                Dim oldGSC = (CType(sourceBrush, RadialGradientBrush)).GradientStops
                Dim newGSC = newRenderTarget.CreateGradientStopCollection(oldGSC, oldGSC.ColorInterpolationGamma, oldGSC.ExtendMode)
				oldGSC.Dispose()
				newBrush = newRenderTarget.CreateRadialGradientBrush(New RadialGradientBrushProperties((CType(sourceBrush, RadialGradientBrush)).Center, (CType(sourceBrush, RadialGradientBrush)).GradientOriginOffset, (CType(sourceBrush, RadialGradientBrush)).RadiusX, (CType(sourceBrush, RadialGradientBrush)).RadiusY), newGSC, New BrushProperties(sourceBrush.Opacity, sourceBrush.Transform))
				sourceBrush.Dispose()
				Return newBrush
			End If
			If TypeOf sourceBrush Is BitmapBrush Then
				newBrush = newRenderTarget.CreateBitmapBrush(Bitmap, New BitmapBrushProperties((CType(sourceBrush, BitmapBrush)).ExtendModeX, (CType(sourceBrush, BitmapBrush)).ExtendModeY, (CType(sourceBrush, BitmapBrush)).InterpolationMode), New BrushProperties(sourceBrush.Opacity, sourceBrush.Transform))
				sourceBrush.Dispose()
				Return newBrush
			End If
			Throw New NotImplementedException("Unknown brush type used")
		End Function
		#End Region

		#Region "IDisposable.Dispose()"
		Protected disposed As Boolean
		Public Overridable Sub Dispose() Implements IDisposable.Dispose
			If Not disposed Then
				If FillBrush IsNot Nothing Then
                    FillBrush.Dispose()
					FillBrush = Nothing
				End If
				If PenBrush IsNot Nothing Then
                    PenBrush.Dispose()
					PenBrush = Nothing
				End If
				If StrokeStyle IsNot Nothing Then
					StrokeStyle.Dispose()
					StrokeStyle = Nothing
				End If
			End If
		End Sub
		#End Region
	End Class
End Namespace
