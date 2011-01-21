' Copyright (c) Microsoft Corporation.  All rights reserved.

Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Globalization
Imports System.Windows.Forms
Imports Microsoft.WindowsAPICodePack.DirectX.Direct2D1
Imports Microsoft.WindowsAPICodePack.DirectX.DirectWrite
Imports Graphics = Microsoft.WindowsAPICodePack.DirectX.Graphics
Imports FontFamily = Microsoft.WindowsAPICodePack.DirectX.DirectWrite.FontFamily
Imports FontStyle = Microsoft.WindowsAPICodePack.DirectX.DirectWrite.FontStyle

Namespace D2DPaint
	Public Class FontEnumComboBox
		Inherits ComboBox
		#Region "Fields"
		Private ReadOnly enUSCulture As New CultureInfo("en-US")
		Private d2DFactory As D2DFactory
		Private dwriteFactory As DWriteFactory
		Private dcRenderTarget As DCRenderTarget
		Private brush As SolidColorBrush
		Private primaryNames As New List(Of String)()
		Private layouts As Dictionary(Of String, TextLayout)
		Private maxHeight_Renamed As Single
		#End Region

		#Region "Properties"
        Private dropDownFontSize_Renamed As Single = 18
        ''' <summary>
        ''' Gets or sets the size of the font used in the drop down.
        ''' </summary>
        ''' <value>The size of the drop down font.</value>
        <DefaultValue(18.0F)> _
        Public Property DropDownFontSize() As Single
            Get
                Return dropDownFontSize_Renamed
            End Get
            Set(ByVal value As Single)
                dropDownFontSize_Renamed = value
            End Set
        End Property

        ''' <summary>
        ''' Gets or sets a value indicating whether all items should be of the same height (the height of the tallest font) or whether they should use the minimum size required for each font.
        ''' </summary>
        ''' <value><c>true</c> if item height should be fixed; otherwise, <c>false</c>.</value>
        Private privateFixedItemHeight As Boolean
        <DefaultValue(True)> _
        Public Property FixedItemHeight() As Boolean
            Get
                Return privateFixedItemHeight
            End Get
            Set(ByVal value As Boolean)
                privateFixedItemHeight = value
            End Set
        End Property
#End Region

		#Region "FontEnumComboBox()"
		Public Sub New()
			FixedItemHeight = True
		End Sub
		#End Region 

		#Region "Initialize()"
		Public Sub Initialize()
			d2DFactory = D2DFactory.CreateFactory(D2DFactoryType.MultiThreaded)
			dwriteFactory = DWriteFactory.CreateFactory()
			InitializeRenderTarget()
			FillFontFamilies()
			If FixedItemHeight Then
				DropDownHeight = CInt(Fix(maxHeight_Renamed)) * 10
			End If
			DrawMode = DrawMode.OwnerDrawVariable
			AddHandler MeasureItem, AddressOf FontEnumComboBox_MeasureItem
			AddHandler DrawItem, AddressOf FontEnumComboBox_DrawItem
		End Sub
		#End Region

		#Region "InitializeRenderTarget()"
		Private Sub InitializeRenderTarget()
			If dcRenderTarget Is Nothing Then
                Dim props = New RenderTargetProperties With {.PixelFormat = New PixelFormat(Graphics.Format.B8G8R8A8UNorm, AlphaMode.Ignore), .Usage = RenderTargetUsages.GdiCompatible}
				dcRenderTarget = d2DFactory.CreateDCRenderTarget(props)
				brush = dcRenderTarget.CreateSolidColorBrush(New ColorF(ForeColor.R / 256f, ForeColor.G / 256f, ForeColor.B / 256f, 1))
			End If
		End Sub
		#End Region

		#Region "FillFontFamilies()"
		Private Sub FillFontFamilies()
			maxHeight_Renamed = 0
			primaryNames = New List(Of String)()
			layouts = New Dictionary(Of String, TextLayout)()
			For Each family As FontFamily In dwriteFactory.SystemFontFamilyCollection
				AddFontFamily(family)
			Next family
			primaryNames.Sort()
			Items.Clear()
			Items.AddRange(primaryNames.ToArray())
		End Sub
		#End Region

		#Region "AddFontFamily()"
		Private Sub AddFontFamily(ByVal family As FontFamily)
			Dim familyName As String
			Dim familyCulture As CultureInfo

			' First try getting a name in the user's language.
            familyCulture = CultureInfo.CurrentUICulture
            familyName = Nothing
            family.FamilyNames.TryGetValue(familyCulture, familyName)

			If familyName Is Nothing Then
				' Fall back to en-US culture. This is somewhat arbitrary, but most fonts have English
				' strings so this at least yields predictable fallback behavior in most cases.
				familyCulture = enUSCulture
				family.FamilyNames.TryGetValue(familyCulture, familyName)
			End If

			If familyName Is Nothing Then
				' As a last resort, use the first name we find. This will just be the name associated
				' with whatever locale name sorts first alphabetically.
				For Each entry As KeyValuePair(Of CultureInfo, String) In family.FamilyNames
					familyCulture = entry.Key
					familyName = entry.Value
				Next entry
			End If

			If familyName Is Nothing Then
				Return
			End If

			'add info to list of structs used as a cache of text layouts
			Dim displayFormats = New List(Of TextLayout)()
			Dim format = dwriteFactory.CreateTextFormat(If(family.Fonts(0).IsSymbolFont, Font.FontFamily.Name, familyName), DropDownFontSize, FontWeight.Normal, FontStyle.Normal, FontStretch.Normal, familyCulture)
			format.WordWrapping = WordWrapping.NoWrap
			Dim layout = dwriteFactory.CreateTextLayout(familyName, format, 10000, 10000)
			DropDownWidth = Math.Max(DropDownWidth, CInt(Fix(layout.Metrics.Width)))
			maxHeight_Renamed = Math.Max(maxHeight_Renamed, layout.Metrics.Height)
			displayFormats.Add(layout)
			'add name to list
			primaryNames.Add(familyName)
			layouts.Add(familyName, layout)
		End Sub
		#End Region

		#Region "FontEnumComboBox_MeasureItem()"
		Private Sub FontEnumComboBox_MeasureItem(ByVal sender As Object, ByVal e As MeasureItemEventArgs)
			'initialize the DC Render Target and a brush before first use
			InitializeRenderTarget()
			Dim fontName = CStr(Items(e.Index))
			e.ItemWidth = CInt(Fix(layouts(fontName).Metrics.Width)) + 10
			e.ItemHeight = If(FixedItemHeight, CInt(Fix(maxHeight_Renamed)), CInt(Fix(layouts(fontName).Metrics.Height)))
		End Sub
		#End Region

		#Region "FontEnumComboBox_DrawItem()"
		Private Sub FontEnumComboBox_DrawItem(ByVal sender As Object, ByVal e As DrawItemEventArgs)
			'initialize the DC Render Target and a brush before first use
			InitializeRenderTarget()

			'draw the background of the combo item
			e.DrawBackground()

			'set section of the DC to draw on
			Dim subRect = New Rect(e.Bounds.Left, e.Bounds.Top, e.Bounds.Right, e.Bounds.Bottom)

			'bind the render target with the DC
			dcRenderTarget.BindDC(e.Graphics.GetHdc(), subRect)

			'draw the text using D2D/DWrite
			dcRenderTarget.BeginDraw()

			Dim fontName = CStr(Items(e.Index))
			'if ((e.State & DrawItemState.Selected & ~DrawItemState.NoFocusRect) != DrawItemState.None)
			dcRenderTarget.DrawTextLayout(New Point2F(5, (e.Bounds.Height - layouts(fontName).Metrics.Height) / 2), layouts(fontName), brush, DrawTextOptions.Clip)

			dcRenderTarget.EndDraw()
			'release the DC
			e.Graphics.ReleaseHdc()
			'drow focus rect for a focused item
			e.DrawFocusRectangle()
		End Sub
		#End Region

		#Region "Dispose()"
		Protected Overrides Overloads Sub Dispose(ByVal disposing As Boolean)
			If disposing Then
				'dispose of all layouts
				Do While layouts.Keys.Count > 0
					For Each key As String In layouts.Keys
						layouts(key).Dispose()
						layouts.Remove(key)
						Exit For
					Next key
				Loop

				If brush IsNot Nothing Then
					brush.Dispose()
				End If
				brush = Nothing
				If dcRenderTarget IsNot Nothing Then
					dcRenderTarget.Dispose()
				End If
				dcRenderTarget = Nothing
				If dwriteFactory IsNot Nothing Then
					dwriteFactory.Dispose()
				End If
				dwriteFactory = Nothing
				If d2DFactory IsNot Nothing Then
					d2DFactory.Dispose()
				End If
				d2DFactory = Nothing
			End If
			MyBase.Dispose(disposing)
		End Sub
		#End Region
	End Class
End Namespace
