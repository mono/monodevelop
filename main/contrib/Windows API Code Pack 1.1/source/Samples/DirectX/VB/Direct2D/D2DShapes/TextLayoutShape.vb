' Copyright (c) Microsoft Corporation.  All rights reserved.

Imports Microsoft.VisualBasic
Imports System
Imports System.ComponentModel
Imports Microsoft.WindowsAPICodePack.DirectX.Direct2D1
Imports Microsoft.WindowsAPICodePack.DirectX.DirectWrite
Imports System.Text

Namespace D2DShapes
	Friend Class TextLayoutShape
		Inherits TextShape
		Private privateTextLayout As TextLayout
		<TypeConverter(GetType(ExpandableObjectConverter))> _
		Public Property TextLayout() As TextLayout
			Get
				Return privateTextLayout
			End Get
			Set(ByVal value As TextLayout)
				privateTextLayout = value
			End Set
		End Property

		Private privatePoint0 As Point2F
		<TypeConverter(GetType(ExpandableObjectConverter))> _
		Public Property Point0() As Point2F
			Get
				Return privatePoint0
			End Get
			Set(ByVal value As Point2F)
				privatePoint0 = value
			End Set
		End Property

		Public Sub New(ByVal initialRenderTarget As RenderTarget, ByVal random As Random, ByVal d2DFactory As D2DFactory, ByVal bitmap As D2DBitmap, ByVal dwriteFactory As DWriteFactory)
			MyBase.New(initialRenderTarget, random, d2DFactory, bitmap, dwriteFactory)
			RandomizeTextLayout()
			Point0 = RandomPoint()
		End Sub

		Private Sub RandomizeTextLayout()
			TextLayout = dwriteFactory.CreateTextLayout(Text, TextFormat, Random.Next(50, Math.Max(100, CanvasWidth - CInt(Fix(Point0.X)))), Random.Next(50, Math.Max(100, CanvasHeight - CInt(Fix(Point0.Y)))))
			If CoinFlip Then
				TextLayout.SetUnderline(True, RandomTextRange())
			End If
			If CoinFlip Then
				TextLayout.SetStrikethrough(True, RandomTextRange())
			End If
			If CoinFlip Then
				TextLayout.LineSpacing = RandomLineSpacing(TextFormat.FontSize)
            End If
            If NiceGabriola Then
                Dim t As TypographySettingCollection
                t = dwriteFactory.CreateTypography()
                t.Add(New FontFeature(FontFeatureTag.StylisticSet07, 1))
                TextLayout.SetTypography(t, New TextRange(0, CUInt(Text.Length)))
            End If
        End Sub

		Private Function RandomTextRange() As TextRange
			Dim start = Random.Next(0, Text.Length - 5)
			Dim length = Random.Next(1, Text.Length - start)
			Return New TextRange(CUInt(start), CUInt(length))
		End Function

		Protected Friend Overrides Sub Draw(ByVal renderTarget As RenderTarget)
			Dim stateBlock As DrawingStateBlock = d2DFactory.CreateDrawingStateBlock()
			renderTarget.SaveDrawingState(stateBlock)
            renderTarget.TextRenderingParams = RenderingParams

			If Options.HasValue Then
				renderTarget.DrawTextLayout(Point0, TextLayout, FillBrush, Options.Value)
			Else
				renderTarget.DrawTextLayout(Point0, TextLayout, FillBrush)
			End If
			renderTarget.RestoreDrawingState(stateBlock)
			stateBlock.Dispose()
		End Sub

		Public Overrides Function HitTest(ByVal point As Point2F) As Boolean
			'bool isTrailingHit, isInside;
			'TextLayout.HitTestPoint(point.X, point.Y, out isTrailingHit, out isInside);
			'return (isTrailingHit || isInside);
			'the method below checks the layout box hit test instead of the DirectWrite method
			Return point.X >= Point0.X AndAlso point.Y >= Point0.Y AndAlso point.X <= Point0.X + TextLayout.MaxWidth AndAlso point.Y <= Point0.Y + TextLayout.MaxHeight
		End Function

		Public Overrides Sub Dispose()
			If TextLayout IsNot Nothing Then
				TextLayout.Dispose()
			End If
			TextLayout = Nothing
			MyBase.Dispose()
		End Sub
	End Class
End Namespace
