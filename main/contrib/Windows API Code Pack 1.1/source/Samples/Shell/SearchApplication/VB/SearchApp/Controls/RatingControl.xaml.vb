' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Controls.Primitives

Namespace Microsoft.WindowsAPICodePack.Samples.SearchApp
	''' <summary>
	''' Interaction logic for RatingControl.xaml
	''' </summary>
	Partial Public Class RatingControl
		Inherits UserControl
		Public Shared ReadOnly RatingValueProperty As DependencyProperty = DependencyProperty.Register("RatingValue", GetType(Integer), GetType(RatingControl), New FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, New PropertyChangedCallback(AddressOf RatingValueChanged)))


		Private maxValue As Integer = 99

		Public Property RatingValue() As Integer
			Get
				Return CInt(Fix(GetValue(RatingValueProperty)))
			End Get
			Set(ByVal value As Integer)
				If value < 0 Then
					SetValue(RatingValueProperty, 0)
				ElseIf value > maxValue Then
					SetValue(RatingValueProperty, maxValue)
				Else
					SetValue(RatingValueProperty, value)
				End If
			End Set
		End Property

		Private Shared Sub RatingValueChanged(ByVal sender As DependencyObject, ByVal e As DependencyPropertyChangedEventArgs)
			Dim parent As RatingControl = TryCast(sender, RatingControl)
			Dim ratingValue As Integer = CInt(Fix(e.NewValue))
			Dim children As UIElementCollection = (CType(parent.Content, Grid)).Children
			Dim button As ToggleButton = Nothing

			For i As Integer = 0 To ratingValue - 1
				button = TryCast(children(i), ToggleButton)
				If button IsNot Nothing Then
					button.IsChecked = True
				End If
			Next i

			For i As Integer = ratingValue To children.Count - 1
				button = TryCast(children(i), ToggleButton)
				If button IsNot Nothing Then
					button.IsChecked = False
				End If
			Next i
		End Sub

		Public Sub New()
			InitializeComponent()
		End Sub

		Private Sub RatingButtonClickEventHandler(ByVal sender As Object, ByVal e As RoutedEventArgs)
			Dim button As ToggleButton = TryCast(sender, ToggleButton)

			Dim newRating As Integer = Integer.Parse(CType(button.Tag, String))

			If CBool(button.IsChecked) OrElse newRating < RatingValue Then
				RatingValue = newRating
			Else
				RatingValue = newRating - 1
			End If

			e.Handled = True
		End Sub
	End Class
End Namespace
