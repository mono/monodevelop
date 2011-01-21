'Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Data
Imports System.Windows.Documents
Imports System.Windows.Media
Imports System.Windows.Shapes
Imports System.Collections.ObjectModel
Imports Microsoft.WindowsAPICodePack.Shell
Imports System.IO
Imports System.Windows.Input

Namespace ShellHierarchyTreeDemo
	''' <summary>
	''' This application demonstrates how to navigate the Shell namespace 
	''' starting from the Desktop folder (Shell.Desktop).
	''' </summary>
	Partial Public Class Window1
		Inherits Window
		Public Shared DesktopKnownFolder As IKnownFolder = KnownFolders.Desktop

		Public Sub New()
			InitializeComponent()

			' After everything is initialized, selected the header (Desktop)
			treeViewHeader.IsSelected = True
			treeViewHeader.Focus()
		End Sub

		Private Sub treeViewHeader_Selected(ByVal sender As Object, ByVal e As RoutedEventArgs)
			' Whenever the user selects this header, show the Desktop data
			If treeViewHeader.IsSelected Then
				ShowDesktopData()
			End If
		End Sub

		Private Sub ShowDesktopData()
			DesktopCollection.ShowObjectData(Me, TryCast(DesktopKnownFolder, ShellObject))
			DesktopCollection.ShowThumbnail(Me, TryCast(DesktopKnownFolder, ShellObject))
			DesktopCollection.ShowProperties(Me, TryCast(DesktopKnownFolder, ShellObject))
		End Sub

		Private Sub MenuItemRefresh_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
			Dim selectedItem As TreeViewItem = TryCast(ExplorerTreeView.SelectedItem, TreeViewItem)

            If TypeOf selectedItem.ItemsSource Is DesktopCollection Then
                selectedItem.ItemsSource = New DesktopCollection()
            Else
                selectedItem.IsExpanded = False
                selectedItem.Items.Clear()
                selectedItem.Items.Add(":::")
                selectedItem.IsExpanded = True
            End If
		End Sub
	End Class

	Public Class DesktopCollection
		Inherits Collection(Of Object)
		Public Sub New()
			For Each obj As ShellObject In KnownFolders.Desktop
				Dim item As New TreeViewItem()
				item.Header = obj
				If TypeOf obj Is ShellContainer Then
					item.Items.Add(":::")
					AddHandler item.Expanded, AddressOf ExplorerTreeView_Expanded
				End If
				AddHandler item.Selected, AddressOf item_Selected
				Add(item)
			Next obj
		End Sub

		Friend Sub ExplorerTreeView_Expanded(ByVal sender As Object, ByVal e As RoutedEventArgs)
			Dim sourceItem As TreeViewItem = TryCast(sender, TreeViewItem)
			Dim shellContainer As ShellContainer = TryCast(sourceItem.Header, ShellContainer)

			If sourceItem.Items.Count >= 1 AndAlso sourceItem.Items(0).Equals(":::") Then
				sourceItem.Items.Clear()
				Try
					For Each obj As ShellObject In shellContainer
						Dim item As New TreeViewItem()
						item.Header = obj
						If TypeOf obj Is ShellContainer Then
							item.Items.Add(":::")
							AddHandler item.Expanded, AddressOf ExplorerTreeView_Expanded
						End If
						AddHandler item.Selected, AddressOf item_Selected
						sourceItem.Items.Add(item)
					Next obj
				Catch e1 As FileNotFoundException
					' Device might not be ready
					MessageBox.Show("The device or directory is not ready.", "Shell Hierarchy Tree Demo")
				Catch e2 As ArgumentException
					' Device might not be ready
					MessageBox.Show("The directory is currently not accessible.", "Shell Hierarchy Tree Demo")
                Catch e3 As UnauthorizedAccessException
                    MessageBox.Show("You don't currently have permission to access this folder.", "Shell Hierarchy Tree Demo")
                End Try

			End If
		End Sub

		Friend Sub item_Selected(ByVal sender As Object, ByVal e As RoutedEventArgs)
			If TypeOf sender Is TreeViewItem Then

				Dim wnd As Window1 = CType(Application.Current.MainWindow, Window1)

				Dim sourceItem As TreeViewItem = TryCast(wnd.ExplorerTreeView.SelectedItem, TreeViewItem)
				If sourceItem Is Nothing Then
					Return
				End If

				Dim shellObj As ShellObject = TryCast(sourceItem.Header, ShellObject)

				If shellObj Is Nothing Then
					Return
				End If

				ShowObjectData(wnd, shellObj)
                ShowThumbnail(wnd, shellObj)
                Try
                    ShowProperties(wnd, shellObj)
                Catch ex As Exception
                    MessageBox.Show(String.Format("Cannot show properties for ""{0}"": {1}", shellObj, ex.Message))
                    wnd.PropertiesListBox.ItemsSource = Nothing

                    wnd.FolderPropsListBox.Visibility = Visibility.Hidden
                    wnd.PropertiesGrid.RowDefinitions(1).Height = New GridLength(0)
                End Try


            End If
		End Sub

		Friend Shared Sub ShowProperties(ByVal wnd As Window1, ByVal shellObj As ShellObject)
			wnd.PropertiesListBox.ItemsSource = shellObj.Properties.DefaultPropertyCollection

			If TypeOf shellObj Is IKnownFolder Then
				ShowKnownFolderProperties(wnd, TryCast(shellObj, IKnownFolder))
			ElseIf TypeOf shellObj Is ShellLibrary Then
				ShowLibraryProperties(wnd, TryCast(shellObj, ShellLibrary))
			Else
				wnd.FolderPropsListBox.Visibility = Visibility.Hidden
				wnd.PropertiesGrid.RowDefinitions(1).Height = New GridLength(0)
			End If
		End Sub

		Friend Shared Sub ShowKnownFolderProperties(ByVal wnd As Window1, ByVal kf As IKnownFolder)
			wnd.FolderPropsListBox.Visibility = Visibility.Visible
			wnd.PropertiesGrid.RowDefinitions(1).Height = New GridLength(150)

			Dim properties As New Collection(Of KnownFolderProperty)()

            properties.Add(New KnownFolderProperty("Canonical Name", kf.CanonicalName))
            properties.Add(New KnownFolderProperty("Category", kf.Category.ToString()))
            properties.Add(New KnownFolderProperty("Definition Options", kf.DefinitionOptions.ToString()))
            properties.Add(New KnownFolderProperty("Description", kf.Description))
            properties.Add(New KnownFolderProperty("File Attributes", kf.FileAttributes.ToString()))
            properties.Add(New KnownFolderProperty("Folder Id", kf.FolderId.ToString()))
            properties.Add(New KnownFolderProperty("Folder Type", kf.FolderType))
            properties.Add(New KnownFolderProperty("Folder Type Id", kf.FolderTypeId.ToString()))
            properties.Add(New KnownFolderProperty("Path", kf.Path))
            properties.Add(New KnownFolderProperty("Relative Path", kf.RelativePath))
            properties.Add(New KnownFolderProperty("Security", kf.Security))
            properties.Add(New KnownFolderProperty("Tooltip", kf.Tooltip))

			wnd.FolderPropsListBox.ItemsSource = properties
		End Sub

		Friend Shared Sub ShowLibraryProperties(ByVal wnd As Window1, ByVal [lib] As ShellLibrary)
			wnd.FolderPropsListBox.Visibility = Visibility.Visible
			wnd.PropertiesGrid.RowDefinitions(1).Height = New GridLength(150)

			Dim properties As New Collection(Of KnownFolderProperty)()

            properties.Add(New KnownFolderProperty("Name", [lib].Name))
			Dim value As Object = Nothing

			Try
				value = [lib].LibraryType
			Catch
			End Try
            properties.Add(New KnownFolderProperty("Library Type", value.ToString()))

			Try
				value = [lib].LibraryTypeId
			Catch
			End Try
            properties.Add(New KnownFolderProperty("Library Type Id", value.ToString()))

            properties.Add(New KnownFolderProperty("Path", [lib].ParsingName))
            properties.Add(New KnownFolderProperty("Is Pinned To NavigationPane", [lib].IsPinnedToNavigationPane.ToString()))

			wnd.FolderPropsListBox.ItemsSource = properties
		End Sub

		Friend Shared Sub ShowObjectData(ByVal wnd As Window1, ByVal shellObj As ShellObject)
			wnd.PropertiesTextBox.Text = String.Format("Name = {0}{1}Path/ParsingName = {2}{1}Type = {3}{4} ({5}File System)", shellObj.Name, Environment.NewLine, shellObj.ParsingName, shellObj.GetType().Name,If(shellObj.IsLink, " (Shortcut)", ""),If(shellObj.IsFileSystemObject, "", "Non "))
		End Sub

		Friend Shared Sub ShowThumbnail(ByVal wnd As Window1, ByVal shellObj As ShellObject)
			Try
				wnd.ThumbnailPreview.Source = shellObj.Thumbnail.LargeBitmapSource
			Catch
				wnd.ThumbnailPreview.Source = Nothing
			End Try
		End Sub

	End Class

    Friend Structure KnownFolderProperty

        Public Sub New(ByVal prop As String, ByVal val As String)
            PropertyName = prop
            Value = val
        End Sub

        Private privateProperty As String
        Public Property PropertyName() As String
            Get
                Return privateProperty
            End Get
            Set(ByVal value As String)
                privateProperty = value
            End Set
        End Property
        Private privateValue As Object
        Public Property Value() As Object
            Get
                Return privateValue
            End Get
            Set(ByVal value As Object)
                privateValue = value
            End Set
        End Property
    End Structure
End Namespace
