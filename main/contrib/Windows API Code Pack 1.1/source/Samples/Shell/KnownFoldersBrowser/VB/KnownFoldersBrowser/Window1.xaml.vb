'Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System.Collections.ObjectModel
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Data
Imports Microsoft.WindowsAPICodePack.Shell
Imports Microsoft.WindowsAPICodePack.Controls.WindowsPresentationFoundation

Namespace Microsoft.WindowsAPICodePack.Samples.KnownFoldersBrowser
	''' <summary>
	''' Interaction logic for Window1.xaml
	''' </summary>
	Partial Public Class Window1
		Inherits Window
		Public Sub New()
            InitializeComponent()

            Dim binding As New Binding With _
            { _
                .Source = knownFoldersListBox, _
                .Path = New PropertyPath(ListBox.SelectedItemProperty), _
                .Mode = BindingMode.OneWay, _
                .TargetNullValue = ShellFileSystemFolder.FromParsingName(KnownFolders.Desktop.ParsingName) _
            }

            BindingOperations.SetBinding(explorerBrowser1, ExplorerBrowser.NavigationTargetProperty, binding)
        End Sub

		Private Sub NavigateExplorerBrowser(ByVal sender As Object, ByVal args As SelectionChangedEventArgs)
            Dim folder As IKnownFolder = CType(CType(sender, ListBox).SelectedItem, IKnownFolder)

            If folder Is Nothing Then
                folder = CType(ShellFileSystemFolder.FromParsingName(KnownFolders.Desktop.ParsingName), IKnownFolder)
            End If

            UpdateProperties(folder)
        End Sub

        Private Sub UpdateProperties(ByVal folder As IKnownFolder)
            ' TODO - Make XAML only
            ' There is currently no way to get all the KnownFolder properties in a collection
            ' that can be use for binding to a listbox. Create our own properties collection with name/value pairs

            Dim properties As New Collection(Of KnownFolderProperty)()
            properties.Add(New KnownFolderProperty("Canonical Name", folder.CanonicalName))
            properties.Add(New KnownFolderProperty("Category", folder.Category.ToString()))
            properties.Add(New KnownFolderProperty("Definition Options", folder.DefinitionOptions.ToString()))
            properties.Add(New KnownFolderProperty("Description", folder.Description))
            properties.Add(New KnownFolderProperty("File Attributes", folder.FileAttributes.ToString()))
            properties.Add(New KnownFolderProperty("Folder Id", folder.FolderId.ToString()))
            properties.Add(New KnownFolderProperty("Folder Type", folder.FolderType))
            properties.Add(New KnownFolderProperty("Folder Type Id", folder.FolderTypeId.ToString()))
            properties.Add(New KnownFolderProperty("Localized Name", folder.LocalizedName))
            properties.Add(New KnownFolderProperty("Localized Name Resource Id", folder.LocalizedNameResourceId))
            properties.Add(New KnownFolderProperty("Parent Id", folder.ParentId.ToString()))
            properties.Add(New KnownFolderProperty("ParsingName", folder.ParsingName))
            properties.Add(New KnownFolderProperty("Path", folder.Path))
            properties.Add(New KnownFolderProperty("Relative Path", folder.RelativePath))
            properties.Add(New KnownFolderProperty("Redirection", folder.Redirection.ToString()))
            properties.Add(New KnownFolderProperty("Security", folder.Security))
            properties.Add(New KnownFolderProperty("Tooltip", folder.Tooltip))
            properties.Add(New KnownFolderProperty("Tooltip Resource Id", folder.TooltipResourceId))

            ' Bind the collection to the properties listbox.
            PropertiesListBox.ItemsSource = properties
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
