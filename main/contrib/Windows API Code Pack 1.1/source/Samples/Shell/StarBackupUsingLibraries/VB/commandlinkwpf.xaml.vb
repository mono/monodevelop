'Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Data
Imports System.Windows.Documents
Imports System.Windows.Media
Imports System.Windows.Media.Imaging
Imports System.Windows.Navigation
Imports System.Windows.Shapes
Imports System.Windows.Input
Imports System.ComponentModel

Namespace Microsoft.WindowsAPICodePack.Samples.StarBackupSample
	''' <summary>
	''' Implements a CommandLink button that can be used in WPF user interfaces.
	''' </summary>

	Partial Public Class CommandLinkWPF
		Inherits UserControl
		Implements INotifyPropertyChanged
		Public Sub New()
			Me.DataContext = Me
			InitializeComponent()
			AddHandler button.Click, AddressOf button_Click
		End Sub

		Private Sub button_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
			e.Source = Me
			RaiseEvent Click(sender, e)
		End Sub

        Private command_Renamed As RoutedUICommand

		Public Property Command() As RoutedUICommand
			Get
				Return command_Renamed
			End Get
			Set(ByVal value As RoutedUICommand)
				command_Renamed = value
			End Set
		End Property

		Public Event Click As RoutedEventHandler

        Private link_Renamed As String

		Public Property Link() As String
			Get
				Return link_Renamed
			End Get
			Set(ByVal value As String)
				link_Renamed = value

				RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("Link"))
			End Set
		End Property
        Private note_Renamed As String

		Public Property Note() As String
			Get
				Return note_Renamed
			End Get
			Set(ByVal value As String)
				note_Renamed = value
				RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("Note"))
			End Set
		End Property
        Private icon_Renamed As ImageSource

		Public Property Icon() As ImageSource
			Get
				Return icon_Renamed
			End Get
			Set(ByVal value As ImageSource)
				icon_Renamed = value
				RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("Icon"))
			End Set
		End Property

		Public Property IsCheck() As Boolean?
			Get
				Return button.IsChecked
			End Get
			Set(ByVal value? As Boolean)
				button.IsChecked = value
			End Set
		End Property


		#Region "INotifyPropertyChanged Members"

		Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

		#End Region
	End Class
End Namespace