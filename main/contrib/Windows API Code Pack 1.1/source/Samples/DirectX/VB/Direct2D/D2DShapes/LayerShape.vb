' Copyright (c) Microsoft Corporation.  All rights reserved.

Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports Microsoft.WindowsAPICodePack.DirectX.Direct2D1

Namespace D2DShapes
	Friend Class LayerShape
		Inherits DrawingShape
		#Region "Properties"
		Private ReadOnly shapes As New List(Of DrawingShape)()
		Public Overrides ReadOnly Property ChildShapes() As List(Of DrawingShape)
			Get
				Return shapes
			End Get
		End Property

		Friend Overrides NotOverridable Property Bitmap() As D2DBitmap
			Get
				Return MyBase.Bitmap
			End Get
			Set(ByVal value As D2DBitmap)
				MyBase.Bitmap = value
				For Each shape In ChildShapes
					shape.Bitmap = value
				Next shape
			End Set
		End Property

'INSTANT VB NOTE: The variable parameters was renamed since Visual Basic does not allow class members with the same name:
		Private parameters_Renamed As LayerParameters
		<TypeConverter(GetType(ExpandableObjectConverter))> _
		Public Property Parameters() As LayerParameters
			Get
				Return parameters_Renamed
			End Get
			Set(ByVal value As LayerParameters)
				parameters_Renamed = value
			End Set
		End Property

		Private privateLayer As Layer
		<TypeConverter(GetType(ExpandableObjectConverter))> _
		Public Property Layer() As Layer
			Get
				Return privateLayer
			End Get
			Set(ByVal value As Layer)
				privateLayer = value
			End Set
		End Property

		Private privateGeometricMaskShape As GeometryShape
		<TypeConverter(GetType(ExpandableObjectConverter))> _
		Public Property GeometricMaskShape() As GeometryShape
			Get
				Return privateGeometricMaskShape
			End Get
			Set(ByVal value As GeometryShape)
				privateGeometricMaskShape = value
			End Set
		End Property
		#End Region

		Public Sub New(ByVal initialRenderTarget As RenderTarget, ByVal random As Random, ByVal d2DFactory As D2DFactory, ByVal bitmap As D2DBitmap, ByVal count As Integer)
			MyBase.New(initialRenderTarget, random, d2DFactory, bitmap)
			Parameters = New LayerParameters()
			parameters_Renamed.ContentBounds = If(CoinFlip, RandomRect(CanvasWidth, CanvasHeight), New RectF(0, 0, CanvasWidth, CanvasHeight))
			If CoinFlip Then
				GeometricMaskShape = New GeometryShape(initialRenderTarget, random, d2DFactory, Me.Bitmap)
				parameters_Renamed.GeometricMask = GeometricMaskShape.Geometry
			End If
			parameters_Renamed.MaskAntialiasMode = If(CoinFlip, AntialiasMode.Aliased, AntialiasMode.PerPrimitive)
			parameters_Renamed.MaskTransform = RandomMatrix3x2()
			parameters_Renamed.Opacity = RandomOpacity()
			If CoinFlip Then
				parameters_Renamed.OpacityBrush = RandomOpacityBrush()
			End If
			parameters_Renamed.Options = If(CoinFlip, LayerOptions.InitializeForClearType, LayerOptions.None)

			For i As Integer = 0 To count - 1
				shapes.Add(RandomShape())
			Next i
		End Sub

		Public Overrides Function HitTest(ByVal point As Point2F) As Boolean
			Return parameters_Renamed.ContentBounds.Top <= point.Y AndAlso parameters_Renamed.ContentBounds.Bottom >= point.Y AndAlso parameters_Renamed.ContentBounds.Left <= point.X AndAlso parameters_Renamed.ContentBounds.Right >= point.X AndAlso (If(GeometricMaskShape IsNot Nothing, GeometricMaskShape.Geometry.FillContainsPoint(point, 5), True)) AndAlso parameters_Renamed.Opacity > 0
		End Function

		Private Function RandomShape() As DrawingShape
			Dim which As Double = Random.NextDouble()
			'GDI does not work in layers
			'return new GDIEllipsesShape(RenderTarget, Random, d2DFactory, Bitmap, 1);
			'layers inside of layers can be really slow
			'if (which < 0.01)
			'    return new LayerShape(RenderTarget, Random, d2DFactory, Bitmap, 1);
			If which < 0.1 Then
				Return New LineShape(RenderTarget, Random, d2DFactory, Bitmap)
			End If
			If which < 0.3 Then
				Return New RectangleShape(RenderTarget, Random, d2DFactory, Bitmap)
			End If
			If which < 0.5 Then
				Return New RoundRectangleShape(RenderTarget, Random, d2DFactory, Bitmap)
			End If
			If which < 0.6 Then
				Return New BitmapShape(RenderTarget, Random, d2DFactory, Bitmap)
			End If
			If which < 0.8 Then
				Return New EllipseShape(RenderTarget, Random, d2DFactory, Bitmap)
			End If
			Return New GeometryShape(RenderTarget, Random, d2DFactory, Bitmap)
		End Function

		Private Function RandomOpacityBrush() As Brush
			Return If(CoinFlip, CType(RandomRadialBrush(), Brush), RandomGradientBrush())
		End Function

		Protected Friend Overrides Sub ChangeRenderTarget(ByVal newRenderTarget As RenderTarget)
			If GeometricMaskShape IsNot Nothing Then
				GeometricMaskShape.Bitmap = Bitmap
				GeometricMaskShape.RenderTarget = newRenderTarget
			End If
			If parameters_Renamed.OpacityBrush IsNot Nothing Then
				parameters_Renamed.OpacityBrush = CopyBrushToRenderTarget(parameters_Renamed.OpacityBrush, newRenderTarget)
			End If
			For Each shape In ChildShapes
				shape.Bitmap = Bitmap
				shape.RenderTarget = newRenderTarget
			Next shape
			Layer = Nothing
		End Sub

		Protected Friend Overrides Sub Draw(ByVal renderTarget As RenderTarget)
			If Layer Is Nothing OrElse Layer.Size.Width <> renderTarget.Size.Width OrElse Layer.Size.Height <> renderTarget.Size.Height Then
				If Layer IsNot Nothing Then
					Layer.Dispose()
				End If
				Layer = renderTarget.CreateLayer(renderTarget.Size)
			End If
			renderTarget.PushLayer(Parameters, Layer)
			For Each shape As DrawingShape In shapes
				shape.Draw(renderTarget)
			Next shape
			renderTarget.PopLayer()
		End Sub

		Public Overrides Sub Dispose()
			For Each shape In ChildShapes
				shape.Dispose()
			Next shape
			ChildShapes.Clear()
			If parameters_Renamed.OpacityBrush IsNot Nothing Then
                parameters_Renamed.OpacityBrush.Dispose()
			End If
			If GeometricMaskShape IsNot Nothing Then
				GeometricMaskShape.Dispose()
			End If
			GeometricMaskShape = Nothing
			parameters_Renamed.OpacityBrush = Nothing
			If Layer IsNot Nothing Then
				Layer.Dispose()
			End If
			Layer = Nothing
			MyBase.Dispose()
		End Sub
	End Class
End Namespace
