' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Data
Imports System.Drawing
Imports System.Linq
Imports System.Text
Imports System.Windows.Forms
Imports Microsoft.WindowsAPICodePack.Shell
Imports Microsoft.WindowsAPICodePack.Taskbar
Imports System.IO
Imports Microsoft.WindowsAPICodePack.Controls

Namespace Microsoft.WindowsAPICodePack.Samples.TabbedThumbnailDemo
	Partial Public Class FavoritesWindow
		Inherits Form
        Private parentForm_Renamed As Form1 = Nothing

		Public Sub New(ByVal parent As Form1)
			parentForm_Renamed = parent

			InitializeComponent()

			explorerBrowser1.NavigationOptions.PaneVisibility.AdvancedQuery = PaneVisibilityState.Hide
			explorerBrowser1.NavigationOptions.PaneVisibility.Commands= PaneVisibilityState.Hide
			explorerBrowser1.NavigationOptions.PaneVisibility.CommandsOrganize= PaneVisibilityState.Hide
			explorerBrowser1.NavigationOptions.PaneVisibility.CommandsView= PaneVisibilityState.Hide
			explorerBrowser1.NavigationOptions.PaneVisibility.Details = PaneVisibilityState.Hide
			explorerBrowser1.NavigationOptions.PaneVisibility.Navigation= PaneVisibilityState.Hide
			explorerBrowser1.NavigationOptions.PaneVisibility.Preview = PaneVisibilityState.Hide
			explorerBrowser1.NavigationOptions.PaneVisibility.Query= PaneVisibilityState.Hide

			explorerBrowser1.ContentOptions.NoSubfolders = True
			explorerBrowser1.ContentOptions.NoColumnHeader = True
			explorerBrowser1.ContentOptions.NoHeaderInAllViews = True

			AddHandler explorerBrowser1.SelectionChanged, AddressOf explorerBrowser1_SelectionChanged
			AddHandler Load, AddressOf FavoritesWindow_Load
		End Sub

		Private Sub explorerBrowser1_SelectionChanged(ByVal sender As Object, ByVal e As EventArgs)
			If explorerBrowser1.SelectedItems.Count > 0 AndAlso TypeOf explorerBrowser1.SelectedItems(0) Is ShellFile Then
				Dim path As String = (CType(explorerBrowser1.SelectedItems(0), ShellFile)).Path

                If System.IO.Path.GetExtension(path).ToLower() = ".url" Then
                    If parentForm_Renamed IsNot Nothing Then
                        parentForm_Renamed.Navigate(path)
                    End If
                End If
			End If
		End Sub

		Private Sub FavoritesWindow_Load(ByVal sender As Object, ByVal e As EventArgs)
			explorerBrowser1.ContentOptions.ViewMode = ExplorerBrowserViewMode.List

			explorerBrowser1.Navigate(CType(KnownFolders.Favorites, ShellObject))
		End Sub
	End Class
End Namespace
