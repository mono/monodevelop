'Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.Windows
Imports System.Windows.Data
Imports System.Windows.Controls
Imports System.Linq

Imports Microsoft.WindowsAPICodePack.Shell
Imports Microsoft.WindowsAPICodePack.Controls

Namespace Microsoft.WindowsAPICodePack.Samples
	''' <summary>
	''' Interaction logic for Window1.xaml
	''' </summary>
	Partial Public Class ExplorerBrowserTestWindow
		Inherits Window
		Public Sub New()
			InitializeComponent()

			Dim sortedKnownFolders = From folder In KnownFolders.All _
			                         Where (folder.CanonicalName IsNot Nothing AndAlso folder.CanonicalName.Length > 0 AndAlso (CType(folder, ShellObject)).Thumbnail.BitmapSource IsNot Nothing AndAlso folder.CanonicalName.CompareTo("Network") <> 0 AndAlso folder.CanonicalName.CompareTo("NetHood") <> 0) _
			                         Order By folder.CanonicalName _
			                         Select folder
			knownFoldersCombo.ItemsSource = sortedKnownFolders

			Dim viewModes = From mode In Enums.Get(Of ExplorerBrowserViewMode)() _
			                Order By mode.ToString() _
			                Select mode
			ViewModeCombo.ItemsSource = viewModes
			ViewModeCombo.Text = "Auto"

			AddHandler Loaded, AddressOf ExplorerBrowserTestWindow_Loaded
		End Sub

		Private Sub ExplorerBrowserTestWindow_Loaded(ByVal sender As Object, ByVal e As RoutedEventArgs)
			' Navigate to initial folder
			eb.ExplorerBrowserControl.Navigate(CType(KnownFolders.Desktop, ShellObject))
		End Sub

		Private Sub navigateFileButton_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
			Try
				Dim sf As ShellFile = ShellFile.FromFilePath(navigateFileTextBox.Text)
				eb.ExplorerBrowserControl.Navigate(sf)
			Catch
				MessageBox.Show("Navigation not possible!")
			End Try
		End Sub

		Private Sub navigateFolderButton_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
			Try
				Dim sf As ShellFileSystemFolder = ShellFileSystemFolder.FromFolderPath(navigateFolderTextBox.Text)
				eb.ExplorerBrowserControl.Navigate(sf)
			Catch
				MessageBox.Show("Navigation not possible!")
			End Try
		End Sub

		Private Sub navigateKnownFolderButton_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
			Try
				Dim kf As IKnownFolder = TryCast(knownFoldersCombo.Items(knownFoldersCombo.SelectedIndex), IKnownFolder)
				eb.ExplorerBrowserControl.Navigate(CType(kf, ShellObject))
			Catch
				MessageBox.Show("Navigation not possible!")
			End Try
		End Sub

		Private Sub ClearNavigationLog_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
			eb.ExplorerBrowserControl.NavigationLog.ClearLog()
		End Sub
	End Class

	Public NotInheritable Class Enums
		Private Sub New()
		End Sub
		Public Shared Function [Get](Of T)() As IEnumerable(Of T)
			Return System.Enum.GetValues(GetType(T)).Cast(Of T)()
		End Function
	End Class

	Public Class TriCheckToPaneVisibilityState
		Implements IValueConverter
		#Region "IValueConverter Members"

		Public Function Convert(ByVal value As Object, ByVal targetType As Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements IValueConverter.Convert
			If value Is Nothing Then
                Return PaneVisibilityState.DoNotCare
			ElseIf CBool(value) = True Then
				Return PaneVisibilityState.Show
			Else
				Return PaneVisibilityState.Hide
			End If
		End Function

		Public Function ConvertBack(ByVal value As Object, ByVal targetType As Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements IValueConverter.ConvertBack
            If CType(value, PaneVisibilityState) = PaneVisibilityState.DoNotCare Then
                Return Nothing
            ElseIf CType(value, PaneVisibilityState) = PaneVisibilityState.Show Then
                Return True
            Else
                Return False
            End If
		End Function

		#End Region
	End Class

End Namespace
