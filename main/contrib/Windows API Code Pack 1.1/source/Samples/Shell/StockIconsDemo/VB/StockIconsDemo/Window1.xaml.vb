'Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Media
Imports Microsoft.WindowsAPICodePack.Shell

Namespace StockIconsDemo
	''' <summary>
	''' Interaction logic for Window1.xaml
	''' </summary>
	Partial Public Class Window1
		Inherits Window
		Private stockIcons As StockIcons = Nothing

		Public Sub New()
			InitializeComponent()

			' Initialize our default collection of StockIcons.
			' We will reuse this collection and only change certain properties as needed
			stockIcons = New StockIcons()

			' Select large size
			comboBox1.SelectedIndex = 1
		End Sub

        Private Sub UpdateStockIcon(ByVal newSize As StockIconSize, ByVal linkOverlay? As Boolean, ByVal selected? As Boolean)
            ' Clear any existing items in the wrap panel
            ' Using the updated UI settings, get all the stock icons and show them in an Image control
            wrapPanel1.Children.Clear()

            '
            Dim isLinkOverlay As Boolean = (linkOverlay.GetValueOrDefault() = True)
            Dim isSelected As Boolean = (selected.GetValueOrDefault() = True)

            ' Update all the stock icons with these latest settings
            UpdateStockIconSettings(newSize, isLinkOverlay, isSelected)

            ' Get the new bitmap source
            For Each icon As StockIcon In stockIcons.AllStockIcons
                Dim img As New Image()
                img.Tag = icon
                img.Stretch = Stretch.None
                img.Source = icon.BitmapSource
                img.Margin = New Thickness(10)
                AddHandler img.MouseLeftButtonDown, AddressOf img_MouseLeftButtonDown
                wrapPanel1.Children.Add(img)
            Next icon

            stockIconsCount.Text = stockIcons.AllStockIcons.Count.ToString()
        End Sub

		Private Sub img_MouseLeftButtonDown(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs)
			Dim img As Image = TryCast(sender, Image)

			' Get the stock icon object that we stored in the tag property
			If img.Tag IsNot Nothing Then
				' Toggle the selection (i.e. get a new bitmapsource)
				Dim selected As Boolean = (CType(img.Tag, StockIcon)).Selected
				CType(img.Tag, StockIcon).Selected = Not selected
				img.Source = (CType(img.Tag, StockIcon)).BitmapSource
			End If
		End Sub

        Private Sub UpdateStockIconSettings(ByVal newSize As StockIconSize, ByVal isLinkOverlay As Boolean, ByVal isSelected As Boolean)
            ' Update all the stock icons in the collection with the latest settings
            For Each icon As StockIcon In stockIcons.AllStockIcons
                icon.CurrentSize = newSize
                icon.LinkOverlay = isLinkOverlay
                icon.Selected = isSelected
            Next icon
        End Sub

		Private Sub linkOverlayCheckBox_Checked(ByVal sender As Object, ByVal e As RoutedEventArgs)
            UpdateStockIcon(CType(comboBox1.SelectedIndex, StockIconSize), linkOverlayCheckBox.IsChecked, selectedCheckBox.IsChecked)
		End Sub

		Private Sub selectedCheckBox_Checked(ByVal sender As Object, ByVal e As RoutedEventArgs)
            UpdateStockIcon(CType(comboBox1.SelectedIndex, StockIconSize), linkOverlayCheckBox.IsChecked, selectedCheckBox.IsChecked)
		End Sub

		Private Sub comboBox1_SelectionChanged(ByVal sender As Object, ByVal e As SelectionChangedEventArgs)
            UpdateStockIcon(CType(comboBox1.SelectedIndex, StockIconSize), linkOverlayCheckBox.IsChecked, selectedCheckBox.IsChecked)
		End Sub
	End Class
End Namespace
