'Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports System.Windows.Media
Imports System.Windows.Threading

Namespace Microsoft.WindowsAPICodePack.Samples.SearchApp
	Public Enum SearchMode
		Instant
		Delayed
	End Enum

	Public Class SearchTextBox
		Inherits TextBox
		Public Shared LabelTextProperty As DependencyProperty = DependencyProperty.Register("LabelText", GetType(String), GetType(SearchTextBox))

		Public Shared LabelTextColorProperty As DependencyProperty = DependencyProperty.Register("LabelTextColor", GetType(Brush), GetType(SearchTextBox))

		Public Shared SearchModeProperty As DependencyProperty = DependencyProperty.Register("SearchMode", GetType(SearchMode), GetType(SearchTextBox), New PropertyMetadata(SearchMode.Instant))

		Private Shared HasTextPropertyKey As DependencyPropertyKey = DependencyProperty.RegisterReadOnly("HasText", GetType(Boolean), GetType(SearchTextBox), New PropertyMetadata())

		Public Shared HasTextProperty As DependencyProperty = HasTextPropertyKey.DependencyProperty

		Private Shared IsMouseLeftButtonDownPropertyKey As DependencyPropertyKey = DependencyProperty.RegisterReadOnly("IsMouseLeftButtonDown", GetType(Boolean), GetType(SearchTextBox), New PropertyMetadata())
		Public Shared IsMouseLeftButtonDownProperty As DependencyProperty = IsMouseLeftButtonDownPropertyKey.DependencyProperty

		Public Shared SearchEventTimeDelayProperty As DependencyProperty = DependencyProperty.Register("SearchEventTimeDelay", GetType(Duration), GetType(SearchTextBox), New FrameworkPropertyMetadata(New Duration(New TimeSpan(0, 0, 0, 0, 500)), New PropertyChangedCallback(AddressOf OnSearchEventTimeDelayChanged)))

		Public Shared ReadOnly SearchEvent As RoutedEvent = EventManager.RegisterRoutedEvent("Search", RoutingStrategy.Bubble, GetType(RoutedEventHandler), GetType(SearchTextBox))

		Shared Sub New()
			DefaultStyleKeyProperty.OverrideMetadata(GetType(SearchTextBox), New FrameworkPropertyMetadata(GetType(SearchTextBox)))
		End Sub

		Private searchEventDelayTimer As DispatcherTimer

		Public Sub New()
			MyBase.New()
			searchEventDelayTimer = New DispatcherTimer()
			searchEventDelayTimer.Interval = SearchEventTimeDelay.TimeSpan
			AddHandler searchEventDelayTimer.Tick, AddressOf OnSeachEventDelayTimerTick
		End Sub

		Private Sub OnSeachEventDelayTimerTick(ByVal o As Object, ByVal e As EventArgs)
			searchEventDelayTimer.Stop()
			RaiseSearchEvent()
		End Sub

		Private Shared Sub OnSearchEventTimeDelayChanged(ByVal o As DependencyObject, ByVal e As DependencyPropertyChangedEventArgs)
			Dim stb As SearchTextBox = TryCast(o, SearchTextBox)
			If stb IsNot Nothing Then
				stb.searchEventDelayTimer.Interval = (CType(e.NewValue, Duration)).TimeSpan
				stb.searchEventDelayTimer.Stop()
			End If
		End Sub

		Protected Overrides Sub OnTextChanged(ByVal e As TextChangedEventArgs)
			MyBase.OnTextChanged(e)

			HasText = Text.Length <> 0

			If SearchMode = SearchMode.Instant Then
				searchEventDelayTimer.Stop()
				searchEventDelayTimer.Start()
			End If
		End Sub

		Public Overrides Sub OnApplyTemplate()
			MyBase.OnApplyTemplate()

			Dim iconBorder As Border = TryCast(GetTemplateChild("PART_SearchIconBorder"), Border)

			If iconBorder IsNot Nothing Then
				AddHandler iconBorder.MouseLeftButtonDown, AddressOf IconBorder_MouseLeftButtonDown
				AddHandler iconBorder.MouseLeftButtonUp, AddressOf IconBorder_MouseLeftButtonUp
				AddHandler iconBorder.MouseLeave, AddressOf IconBorder_MouseLeave
			End If
		End Sub

		Private Sub IconBorder_MouseLeftButtonDown(ByVal obj As Object, ByVal e As MouseButtonEventArgs)
			IsMouseLeftButtonDown = True
		End Sub

		Private Sub IconBorder_MouseLeftButtonUp(ByVal obj As Object, ByVal e As MouseButtonEventArgs)
			If Not IsMouseLeftButtonDown Then
				Return
			End If

			If HasText AndAlso SearchMode = SearchMode.Instant Then
				Me.Text = ""
			End If

			If HasText AndAlso SearchMode = SearchMode.Delayed Then
				RaiseSearchEvent()
			End If

			IsMouseLeftButtonDown = False
		End Sub

		Private Sub IconBorder_MouseLeave(ByVal obj As Object, ByVal e As MouseEventArgs)
			IsMouseLeftButtonDown = False
		End Sub

		Protected Overrides Sub OnKeyDown(ByVal e As KeyEventArgs)
			If e.Key = Key.Escape AndAlso SearchMode = SearchMode.Instant Then
				Me.Text = ""
			ElseIf (e.Key = Key.Return OrElse e.Key = Key.Enter) AndAlso SearchMode = SearchMode.Delayed Then
				RaiseSearchEvent()
			Else
				MyBase.OnKeyDown(e)
			End If
		End Sub

		Private Sub RaiseSearchEvent()
			Dim args As New RoutedEventArgs(SearchEvent)
			MyBase.RaiseEvent(args)
		End Sub

		Public Property LabelText() As String
			Get
				Return CStr(GetValue(LabelTextProperty))
			End Get
			Set(ByVal value As String)
				SetValue(LabelTextProperty, value)
			End Set
		End Property

		Public Property LabelTextColor() As Brush
			Get
				Return CType(GetValue(LabelTextColorProperty), Brush)
			End Get
			Set(ByVal value As Brush)
				SetValue(LabelTextColorProperty, value)
			End Set
		End Property

		Public Property SearchMode() As SearchMode
			Get
				Return CType(GetValue(SearchModeProperty), SearchMode)
			End Get
			Set(ByVal value As SearchMode)
				SetValue(SearchModeProperty, value)
			End Set
		End Property

		Public Property HasText() As Boolean
			Get
				Return CBool(GetValue(HasTextProperty))
			End Get
			Private Set(ByVal value As Boolean)
				SetValue(HasTextPropertyKey, value)
			End Set
		End Property

		Public Property SearchEventTimeDelay() As Duration
			Get
				Return CType(GetValue(SearchEventTimeDelayProperty), Duration)
			End Get
			Set(ByVal value As Duration)
				SetValue(SearchEventTimeDelayProperty, value)
			End Set
		End Property

		Public Property IsMouseLeftButtonDown() As Boolean
			Get
				Return CBool(GetValue(IsMouseLeftButtonDownProperty))
			End Get
			Private Set(ByVal value As Boolean)
				SetValue(IsMouseLeftButtonDownPropertyKey, value)
			End Set
		End Property

		Public Custom Event Search As RoutedEventHandler
			AddHandler(ByVal value As RoutedEventHandler)
				MyBase.AddHandler(SearchEvent, value)
			End AddHandler
			RemoveHandler(ByVal value As RoutedEventHandler)
				MyBase.RemoveHandler(SearchEvent, value)
			End RemoveHandler
			RaiseEvent(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
			End RaiseEvent
		End Event
	End Class
End Namespace
