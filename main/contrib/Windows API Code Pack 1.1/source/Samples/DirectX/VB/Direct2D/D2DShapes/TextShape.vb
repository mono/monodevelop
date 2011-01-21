' Copyright (c) Microsoft Corporation.  All rights reserved.

Imports Microsoft.VisualBasic
Imports System
Imports System.ComponentModel
Imports System.Globalization
Imports Microsoft.WindowsAPICodePack.DirectX.Direct2D1
Imports Microsoft.WindowsAPICodePack.DirectX.DirectWrite
Imports System.Text

Namespace D2DShapes
	Friend Class TextShape
		Inherits DrawingShape
		Private layoutRect As RectF
        Protected Friend dwriteFactory As DWriteFactory
        Protected Friend NiceGabriola As Boolean

		Private privateRenderingParams As RenderingParams
		<TypeConverter(GetType(ExpandableObjectConverter))> _
		Public Property RenderingParams() As RenderingParams
			Get
				Return privateRenderingParams
			End Get
			Set(ByVal value As RenderingParams)
				privateRenderingParams = value
			End Set
		End Property

		Private privateOptions? As DrawTextOptions
		<TypeConverter(GetType(ExpandableObjectConverter))> _
		Public Property Options() As DrawTextOptions?
			Get
				Return privateOptions
			End Get
			Set(ByVal value? As DrawTextOptions)
				privateOptions = value
			End Set
		End Property

		Private privateTextFormat As TextFormat
		<TypeConverter(GetType(ExpandableObjectConverter))> _
		Public Property TextFormat() As TextFormat
			Get
				Return privateTextFormat
			End Get
			Set(ByVal value As TextFormat)
				privateTextFormat = value
			End Set
		End Property

		Private privateText As String
		Public Property Text() As String
			Get
				Return privateText
			End Get
			Set(ByVal value As String)
				privateText = value
			End Set
		End Property

		Public Sub New(ByVal initialRenderTarget As RenderTarget, ByVal random As Random, ByVal d2DFactory As D2DFactory, ByVal bitmap As D2DBitmap, ByVal dwriteFactory As DWriteFactory)
			MyBase.New(initialRenderTarget, random, d2DFactory, bitmap)
			Me.dwriteFactory = dwriteFactory
            layoutRect = RandomRect(CanvasWidth, CanvasHeight)
            NiceGabriola = random.NextDouble() < 0.25 And dwriteFactory.SystemFontFamilyCollection.Contains("Gabriola")
			TextFormat = dwriteFactory.CreateTextFormat(RandomFontFamily(), RandomFontSize(), RandomFontWeight(), RandomFontStyle(), RandomFontStretch(), System.Globalization.CultureInfo.CurrentUICulture)
			If CoinFlip Then
				TextFormat.LineSpacing = RandomLineSpacing(TextFormat.FontSize)
			End If
			Text = RandomString(Random.Next(1000, 1000))

			FillBrush = RandomBrush()
			RenderingParams = RandomRenderingParams()

			If CoinFlip Then
				Options = DrawTextOptions.None
				If CoinFlip Then
					Options = Options Or DrawTextOptions.Clip
				End If
				If CoinFlip Then
					Options = Options Or DrawTextOptions.NoSnap
				End If
			End If
		End Sub

		Protected Friend Function RandomRenderingParams() As RenderingParams
			Dim rp As RenderingParams = dwriteFactory.CreateCustomRenderingParams(CSng(Math.Max(0.001, Math.Min(1, Random.NextDouble() * 2))), CSng(Math.Max(0, Math.Min(1, Random.NextDouble() * 3 - 1))), CSng(Math.Max(0, Math.Min(1, Random.NextDouble() * 3 - 1))), RandomPixelGeometry(), RandomRenderingMode())
			Return rp
		End Function

		Private Function RandomPixelGeometry() As PixelGeometry
			Return CType(Random.Next(0, 2), PixelGeometry)
		End Function

		Private Function RandomRenderingMode() As RenderingMode
			Return CType(Random.Next(0, 6), RenderingMode)
		End Function

		Protected Friend Function RandomLineSpacing(ByVal fontSize As Single) As LineSpacing
			Dim method As LineSpacingMethod = If(CoinFlip, LineSpacingMethod.Default, LineSpacingMethod.Uniform)
			Dim spacing = CSng(Random.NextDouble()*fontSize*4 + 0.5)
			Dim baseline = CSng(Random.NextDouble())
			Return New LineSpacing(method, spacing, baseline)
		End Function

		Private Function RandomFontFamily() As String
            If (NiceGabriola) Then
                Return "Gabriola"
            End If
            If CoinFlip Then
                'get random font out of the list of installed fonts
                Dim i As Integer = Random.Next(0, dwriteFactory.SystemFontFamilyCollection.Count - 1)
                Dim f As FontFamily = dwriteFactory.SystemFontFamilyCollection(i)
                Dim ret As String = Nothing
                If (f.FamilyNames.ContainsKey(CultureInfo.CurrentUICulture)) Then
                    ret = f.FamilyNames(CultureInfo.CurrentUICulture)
                ElseIf (f.FamilyNames.ContainsKey(CultureInfo.InvariantCulture)) Then
                    ret = f.FamilyNames(CultureInfo.InvariantCulture)
                ElseIf (f.FamilyNames.ContainsKey(CultureInfo.GetCultureInfo("EN-us"))) Then
                    ret = f.FamilyNames(CultureInfo.GetCultureInfo("EN-us"))
                Else
                    For Each c In f.FamilyNames.Keys
                        ret = f.FamilyNames(c)
                        Exit For
                    Next
                End If

                f.Dispose()
                Return ret
            End If
            'get one of the common fonts
            Return New String() {"Arial", "Times New Roman", "Courier New", "Impact", "Tahoma", "Calibri", "Consolas", "Segoe", "Cambria"}(Random.Next(0, 8))
        End Function

		Private Function RandomFontSize() As Single
			Return 6 + CSng(138 * Random.NextDouble() * Random.NextDouble())
		End Function

		Private Function RandomFontWeight() As FontWeight
			Return CType(Math.Min(950, 100 * Random.Next(1, 10)), FontWeight)
		End Function

		Private Function RandomFontStyle() As FontStyle
			Return CType(Random.Next(0, 2), FontStyle)
		End Function

		Private Function RandomFontStretch() As FontStretch
			Return CType(Random.Next(1, 9), FontStretch)
		End Function

		Private Function RandomString(ByVal size As Integer) As String
			Dim builder = New StringBuilder(size + 1) With {.Length = size}
			builder(0) = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * Random.NextDouble() + 65)))
			For i As Integer = 1 To size - 2
				builder(i) = If(Random.NextDouble() < 0.2, " "c, Convert.ToChar(Convert.ToInt32(Math.Floor(26 * Random.NextDouble() + 97))))
			Next i
			builder(size - 1) = "."c
			Return builder.ToString()
		End Function

		Protected Friend Overrides Sub ChangeRenderTarget(ByVal newRenderTarget As RenderTarget)
			FillBrush = CopyBrushToRenderTarget(FillBrush, newRenderTarget)
		End Sub

		Protected Friend Overrides Sub Draw(ByVal renderTarget As RenderTarget)
			Dim stateBlock As DrawingStateBlock = d2DFactory.CreateDrawingStateBlock()
			renderTarget.SaveDrawingState(stateBlock)
            renderTarget.TextRenderingParams = RenderingParams

			If Options.HasValue Then
				renderTarget.DrawText(Text, TextFormat, layoutRect, FillBrush, Options.Value)
			Else
				renderTarget.DrawText(Text, TextFormat, layoutRect, FillBrush)
			End If

			renderTarget.RestoreDrawingState(stateBlock)
			stateBlock.Dispose()
		End Sub

		Public Overrides Function HitTest(ByVal point As Point2F) As Boolean
			Return point.X >= layoutRect.Left AndAlso point.Y >= layoutRect.Top AndAlso point.X <= layoutRect.Right AndAlso point.Y <= layoutRect.Bottom
		End Function

		Public Overrides Sub Dispose()
			If TextFormat IsNot Nothing Then
				TextFormat.Dispose()
			End If
			TextFormat = Nothing
			If RenderingParams IsNot Nothing Then
				RenderingParams.Dispose()
			End If
			RenderingParams = Nothing
			MyBase.Dispose()
		End Sub
	End Class
End Namespace
