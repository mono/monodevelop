' Copyright (c) Microsoft Corporation.  All rights reserved.

Imports Microsoft.VisualBasic
Imports System
Imports System.ComponentModel
Imports Microsoft.WindowsAPICodePack.DirectX.Direct2D1

Namespace D2DShapes
	Friend Class BitmapShape
		Inherits DrawingShape
		#Region "Fields"
'INSTANT VB NOTE: The variable destRect was renamed since Visual Basic does not allow class members with the same name:
		Private destRect_Renamed As RectF
'INSTANT VB NOTE: The variable sourceRect was renamed since Visual Basic does not allow class members with the same name:
		Private sourceRect_Renamed As RectF
'INSTANT VB NOTE: The variable drawSection was renamed since Visual Basic does not allow class members with the same name:
		Private drawSection_Renamed As Boolean
'INSTANT VB NOTE: The variable opacity was renamed since Visual Basic does not allow class members with the same name:
		Private opacity_Renamed As Single
		#End Region

		#Region "Properties"
		<TypeConverter(GetType(ExpandableObjectConverter))> _
		Public Property DestRect() As RectF
			Get
				Return destRect_Renamed
			End Get
			Set(ByVal value As RectF)
				destRect_Renamed = value
			End Set
		End Property

		<TypeConverter(GetType(ExpandableObjectConverter))> _
		Public Property SourceRect() As RectF
			Get
				Return sourceRect_Renamed
			End Get
			Set(ByVal value As RectF)
				sourceRect_Renamed = value
			End Set
		End Property

		Public Property DrawSection() As Boolean
			Get
				Return drawSection_Renamed
			End Get
			Set(ByVal value As Boolean)
				drawSection_Renamed = value
			End Set
		End Property

		Public Property Opacity() As Single
			Get
				Return opacity_Renamed
			End Get
			Set(ByVal value As Single)
				opacity_Renamed = value
			End Set
		End Property
		#End Region

		Public Sub New(ByVal initialRenderTarget As RenderTarget, ByVal random As Random, ByVal d2DFactory As D2DFactory, ByVal bitmap As D2DBitmap)
			MyBase.New(initialRenderTarget, random, d2DFactory, bitmap)
			DestRect = RandomRect(CanvasWidth, CanvasHeight)
			opacity_Renamed = RandomOpacity()
			DrawSection = Random.NextDouble() < 0.25
			If drawSection_Renamed Then
				SourceRect = RandomRect(Me.Bitmap.PixelSize.Width, Me.Bitmap.PixelSize.Height)
			End If
		End Sub

		Protected Friend Overrides Sub ChangeRenderTarget(ByVal newRenderTarget As RenderTarget)
			'no rendertarget dependent members
		End Sub

		Protected Friend Overrides Sub Draw(ByVal renderTarget As RenderTarget)
			If DrawSection Then
				renderTarget.DrawBitmap(Bitmap, opacity_Renamed, BitmapInterpolationMode.Linear, DestRect, SourceRect)
			Else
				renderTarget.DrawBitmap(Bitmap, opacity_Renamed, BitmapInterpolationMode.Linear, DestRect)
			End If
		End Sub

		Public Overrides Function HitTest(ByVal point As Point2F) As Boolean
			Return DestRect.Top <= point.Y AndAlso DestRect.Bottom >= point.Y AndAlso DestRect.Left <= point.X AndAlso DestRect.Right >= point.X
		End Function
	End Class
End Namespace
