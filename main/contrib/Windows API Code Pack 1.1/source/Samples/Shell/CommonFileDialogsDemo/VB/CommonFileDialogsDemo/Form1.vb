'Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.IO
Imports System.Windows.Forms
Imports Microsoft.WindowsAPICodePack.Shell
Imports Microsoft.WindowsAPICodePack.Dialogs
Imports Microsoft.WindowsAPICodePack.Shell.PropertySystem
Imports System.Reflection

Namespace Microsoft.WindowsAPICodePack.Samples.ShellObjectCFDBrowser
	Partial Public Class Form1
		Inherits Form
		Private currentlySelected As ShellObject = Nothing

		Public Sub New()
			InitializeComponent()

			LoadKnownFolders()
			LoadSavedSearches()

			If ShellLibrary.IsPlatformSupported Then
				LoadKnownLibraries()
			Else
				librariesComboBox.Enabled = False
				cfdLibraryButton.Enabled = librariesComboBox.Enabled
				label2.Enabled = cfdLibraryButton.Enabled
			End If

			If ShellSearchConnector.IsPlatformSupported Then
				LoadSearchConnectors()
			Else
				searchConnectorComboBox.Enabled = False
				searchConnectorButton.Enabled = searchConnectorComboBox.Enabled
				label7.Enabled = searchConnectorButton.Enabled
			End If
		End Sub

		''' <summary>
		''' Load all the Saved Searches on the current system
		''' </summary>
		Private Sub LoadSavedSearches()
			For Each so As ShellObject In KnownFolders.SavedSearches
				If TypeOf so Is ShellSavedSearchCollection Then
					savedSearchComboBox.Items.Add(Path.GetFileName(so.ParsingName))
				End If
			Next so

			If savedSearchComboBox.Items.Count > 0 Then
				savedSearchComboBox.SelectedIndex = 0
			Else
				savedSearchButton.Enabled = False
			End If
		End Sub

		''' <summary>
		''' Load all the Search Connectors on the current system
		''' </summary>
		Private Sub LoadSearchConnectors()
			For Each so As ShellObject In KnownFolders.SavedSearches
				If TypeOf so Is ShellSearchConnector Then
					searchConnectorComboBox.Items.Add(Path.GetFileName(so.ParsingName))
				End If
			Next so

			If searchConnectorComboBox.Items.Count > 0 Then
				searchConnectorComboBox.SelectedIndex = 0
			Else
				searchConnectorButton.Enabled = False
			End If
		End Sub

		''' <summary>
		''' Load the known Shell Libraries
		''' </summary>
		Private Sub LoadKnownLibraries()
			' Load all the known libraries.
			' (There's currently no easy way to get all the known libraries in the system, 
			' so hard-code them here.)

			' Make sure we are clear
			librariesComboBox.Items.Clear()

			' 
			librariesComboBox.Items.Add("Documents")
			librariesComboBox.Items.Add("Music")
			librariesComboBox.Items.Add("Pictures")
			librariesComboBox.Items.Add("Videos")

			' Set initial selection
			librariesComboBox.SelectedIndex = 0
		End Sub

		''' <summary>
		''' Load all the knownfolders into the combobox
		''' </summary>
		Private Sub LoadKnownFolders()
			' Make sure we are clear
			knownFoldersComboBox.Items.Clear()

			' Get a list of all the known folders
			For Each kf As IKnownFolder In KnownFolders.All
				If kf IsNot Nothing AndAlso kf.CanonicalName IsNot Nothing Then
					knownFoldersComboBox.Items.Add(kf.CanonicalName)
				End If
			Next kf

			' Set our initial selection
			If knownFoldersComboBox.Items.Count > 0 Then
				knownFoldersComboBox.SelectedIndex = 0
			End If
		End Sub

		Private Sub cfdKFButton_Click(ByVal sender As Object, ByVal e As EventArgs) Handles cfdKFButton.Click
			' Initialize
			detailsListView.Items.Clear()
			pictureBox1.Image = Nothing

			' Create a new CFD
			Dim cfd As New CommonOpenFileDialog()

			' Allow users to select non-filesystem objects
			cfd.AllowNonFileSystemItems = True

			' Get the known folder selected
			Dim kfString As String = TryCast(knownFoldersComboBox.SelectedItem, String)

			If Not String.IsNullOrEmpty(kfString) Then
				Try
					' Try to get a known folder using the selected item (string).
					Dim kf As IKnownFolder = KnownFolderHelper.FromCanonicalName(kfString)

					' Set the knownfolder in the CFD.
					cfd.InitialDirectoryShellContainer = TryCast(kf, ShellContainer)

                    If cfd.ShowDialog() = CommonFileDialogResult.Ok Then
                        Dim selectedSO As ShellObject = Nothing

                        Try
                            ' Get the selection from the user.
                            selectedSO = cfd.FileAsShellObject
                        Catch
                            ' In some cases the user might select an object that cannot be wrapped
                            ' by ShellObject.
                            MessageBox.Show("Could not create a ShellObject from the selected item.")
                        End Try


                        currentlySelected = selectedSO

                        DisplayProperties(selectedSO)

                        showChildItemsButton.Enabled = If(TypeOf selectedSO Is ShellContainer, True, False)
                    End If
                Catch
                    MessageBox.Show("Could not create a KnownFolder object for the selected item")
                End Try
            Else
                MessageBox.Show("Invalid KnownFolder set.")
            End If

            ' Dispose our dialog in the end
            cfd.Dispose()
        End Sub

        Private Sub cfdLibraryButton_Click(ByVal sender As Object, ByVal e As EventArgs) Handles cfdLibraryButton.Click
            ' Initialize
            detailsListView.Items.Clear()
            pictureBox1.Image = Nothing

            ' If the user has selected a library,
            ' try to get the known folder path (Libraries are also known folders, so this will work)
            If librariesComboBox.SelectedIndex > -1 Then
                Dim selection As String = TryCast(librariesComboBox.SelectedItem, String)
                Dim selectedFolder As ShellContainer = Nothing

                Select Case selection
                    Case "Documents"
                        selectedFolder = TryCast(KnownFolders.DocumentsLibrary, ShellContainer)
                    Case "Music"
                        selectedFolder = TryCast(KnownFolders.MusicLibrary, ShellContainer)
                    Case "Pictures"
                        selectedFolder = TryCast(KnownFolders.PicturesLibrary, ShellContainer)
                    Case "Videos"
                        selectedFolder = TryCast(KnownFolders.VideosLibrary, ShellContainer)
                        Exit Select
                End Select

                ' Create a CommonOpenFileDialog
                Dim cfd As New CommonOpenFileDialog()
                cfd.EnsureReadOnly = True

                ' Set the initial location as the path of the library
                cfd.InitialDirectoryShellContainer = selectedFolder

                If cfd.ShowDialog() = CommonFileDialogResult.Ok Then
                    ' Get the selection from the user.
                    Dim so As ShellObject = cfd.FileAsShellObject

                    currentlySelected = so

                    showChildItemsButton.Enabled = If(TypeOf so Is ShellContainer, True, False)

                    DisplayProperties(so)
                End If
            End If
        End Sub

        Private Sub cfdFileButton_Click(ByVal sender As Object, ByVal e As EventArgs) Handles cfdFileButton.Click
            ' Initialize
            detailsListView.Items.Clear()
            pictureBox1.Image = Nothing

            ' Create a CommonOpenFileDialog to select files
            Dim cfd As New CommonOpenFileDialog()
            cfd.AllowNonFileSystemItems = True
            cfd.EnsureReadOnly = True

            If cfd.ShowDialog() = CommonFileDialogResult.Ok Then
                Dim selectedSO As ShellObject = Nothing

                Try
                    ' Try to get the selected item
                    selectedSO = cfd.FileAsShellObject
                Catch
                    MessageBox.Show("Could not create a ShellObject from the selected item")
                End Try

                currentlySelected = selectedSO

                ' Set the path in our filename textbox
                selectedFileTextBox.Text = selectedSO.ParsingName

                DisplayProperties(selectedSO)

                showChildItemsButton.Enabled = If(TypeOf selectedSO Is ShellContainer, True, False)

            End If
        End Sub

        Private Sub cfdFolderButton_Click(ByVal sender As Object, ByVal e As EventArgs) Handles cfdFolderButton.Click
            ' Initialize
            detailsListView.Items.Clear()
            pictureBox1.Image = Nothing

            ' Display a CommonOpenFileDialog to select only folders 
            Dim cfd As New CommonOpenFileDialog()
            cfd.EnsureReadOnly = True
            cfd.IsFolderPicker = True
            cfd.AllowNonFileSystemItems = True

            If cfd.ShowDialog() = CommonFileDialogResult.Ok Then
                Dim selectedSO As ShellContainer = Nothing

                Try
                    ' Try to get a valid selected item
                    selectedSO = TryCast(cfd.FileAsShellObject, ShellContainer)
                Catch
                    MessageBox.Show("Could not create a ShellObject from the selected item")
                End Try

                currentlySelected = selectedSO

                ' Set the path in our filename textbox
                selectedFolderTextBox.Text = selectedSO.ParsingName

                DisplayProperties(selectedSO)

                showChildItemsButton.Enabled = If(TypeOf selectedSO Is ShellContainer, True, False)
            End If
        End Sub

        Private Sub savedSearchButton_Click(ByVal sender As Object, ByVal e As EventArgs) Handles savedSearchButton.Click
            ' Initialize
            detailsListView.Items.Clear()
            pictureBox1.Image = Nothing

            ' Display a CommonOpenFileDialog to select only folders 
            Dim cfd As New CommonOpenFileDialog()
            cfd.InitialDirectory = Path.Combine(KnownFolders.SavedSearches.Path, savedSearchComboBox.SelectedItem.ToString())
            cfd.EnsureReadOnly = True

            If cfd.ShowDialog() = CommonFileDialogResult.Ok Then
                Dim selectedSO As ShellObject = Nothing

                Try
                    ' Try to get a valid selected item
                    selectedSO = cfd.FileAsShellObject
                Catch
                    MessageBox.Show("Could not create a ShellObject from the selected item")
                End Try

                currentlySelected = selectedSO

                DisplayProperties(selectedSO)

                showChildItemsButton.Enabled = If(TypeOf selectedSO Is ShellContainer, True, False)

            End If
        End Sub

        Private Sub DisplayProperties(ByVal selectedSO As ShellObject)
            ' Display some basic properties 
            If selectedSO IsNot Nothing Then
                ' display properties for this folder, as well as a thumbnail image.
                selectedSO.Thumbnail.CurrentSize = New System.Windows.Size(128, 128)
                pictureBox1.Image = selectedSO.Thumbnail.Bitmap

                ' show the properties
                AddProperty("Name", selectedSO.Name)
                AddProperty("Path", selectedSO.ParsingName)
                AddProperty("Type of ShellObject", selectedSO.GetType().Name)

                For Each prop As IShellProperty In selectedSO.Properties.DefaultPropertyCollection
                    If prop.ValueAsObject IsNot Nothing Then
                        Try
                            If prop.ValueType Is GetType(String()) Then
                                Dim arr() As String = CType(prop.ValueAsObject, String())
                                Dim value As String = ""
                                If arr IsNot Nothing AndAlso arr.Length > 0 Then
                                    For Each s As String In arr
                                        value = value & s & "; "
                                    Next s

                                    If value.EndsWith("; ") Then
                                        value = value.Remove(value.Length - 2)
                                    End If
                                End If

                                AddProperty(prop.CanonicalName, value)
                            Else
                                AddProperty(prop.CanonicalName, prop.ValueAsObject.ToString())
                            End If
                        Catch
                            ' Ignore
                            ' Accessing some properties might throw exception.
                        End Try
                    End If
                Next prop
            End If

        End Sub

        Private Sub AddProperty(ByVal [property] As String, ByVal value As String)
            If Not String.IsNullOrEmpty([property]) Then
                ' Create the property ListViewItem
                Dim lvi As New ListViewItem([property])

                ' Add a subitem for the value
                Dim subItemValue As New ListViewItem.ListViewSubItem(lvi, value)
                lvi.SubItems.Add(subItemValue)

                ' Add the ListViewItem to our list
                detailsListView.Items.Add(lvi)
            End If
        End Sub

        Private Sub searchConnectorButton_Click(ByVal sender As Object, ByVal e As EventArgs) Handles searchConnectorButton.Click
            ' Initialize
            detailsListView.Items.Clear()
            pictureBox1.Image = Nothing

            ' Display a CommonOpenFileDialog to select only folders 
            Dim cfd As New CommonOpenFileDialog()
            cfd.InitialDirectory = Path.Combine(KnownFolders.SavedSearches.Path, searchConnectorComboBox.SelectedItem.ToString())
            cfd.EnsureReadOnly = True

            If cfd.ShowDialog() = CommonFileDialogResult.Ok Then
                Dim selectedSO As ShellObject = Nothing

                Try
                    ' Try to get a valid selected item
                    selectedSO = cfd.FileAsShellObject
                Catch
                    MessageBox.Show("Could not create a ShellObject from the selected item")
                End Try

                currentlySelected = selectedSO

                DisplayProperties(selectedSO)

                showChildItemsButton.Enabled = If(TypeOf selectedSO Is ShellContainer, True, False)

            End If
        End Sub

        Private Sub showChildItemsButton_Click(ByVal sender As Object, ByVal e As EventArgs) Handles showChildItemsButton.Click
            Dim container As ShellContainer = TryCast(currentlySelected, ShellContainer)

            If container Is Nothing Then
                Return
            End If

            Dim subItems As New SubItemsForm()

            ' Populate
            For Each so As ShellObject In container
                subItems.AddItem(so.Name, so.Thumbnail.SmallBitmap)
            Next so

            subItems.ShowDialog()
        End Sub

        Private Sub saveFileButton_Click(ByVal sender As Object, ByVal e As EventArgs) Handles saveFileButton.Click
            ' Initialize
            detailsListView.Items.Clear()
            pictureBox1.Image = Nothing

            ' Show a CommonSaveFileDialog with couple of file filters.
            ' Also show some properties (specific to the filter selected) 
            ' that the user can update from the dialog itself.
            Dim saveCFD As New CommonSaveFileDialog()
            saveCFD.AlwaysAppendDefaultExtension = True
            saveCFD.DefaultExtension = ".docx"

            ' When the file type changes, we will add the specific properties
            ' to be collected from the dialog (refer to the saveCFD_FileTypeChanged event handler)
            AddHandler saveCFD.FileTypeChanged, AddressOf saveCFD_FileTypeChanged

            saveCFD.Filters.Add(New CommonFileDialogFilter("Word Documents", "*.docx"))
            saveCFD.Filters.Add(New CommonFileDialogFilter("JPEG Files", "*.jpg"))

            If saveCFD.ShowDialog() = CommonFileDialogResult.Ok Then
                ' Get the selected file (this is what we'll save...)
                ' Save it to disk, so we can read/write properties for it

                ' Because we can't really create a Office file or Picture file, just copying
                ' an existing file to show the properties
                If saveCFD.SelectedFileTypeIndex = 1 Then
                    File.Copy(Path.Combine(Directory.GetCurrentDirectory(), "sample files\test.docx"), saveCFD.FileName, True)
                Else
                    File.Copy(Path.Combine(Directory.GetCurrentDirectory(), "sample files\test.jpg"), saveCFD.FileName, True)
                End If

                ' Get the ShellObject for this file
                Dim selectedSO As ShellObject = ShellFile.FromFilePath(saveCFD.FileName)

                ' Get the properties from the dialog (user might have updated the properties)
                Dim propColl As ShellPropertyCollection = saveCFD.CollectedProperties

                ' Write the properties on our shell object
                Using propertyWriter As ShellPropertyWriter = selectedSO.Properties.GetPropertyWriter()
                    If propColl.Contains(SystemProperties.System.Title) Then
                        propertyWriter.WriteProperty(SystemProperties.System.Title, propColl(SystemProperties.System.Title).ValueAsObject)
                    End If

                    If propColl.Contains(SystemProperties.System.Author) Then
                        propertyWriter.WriteProperty(SystemProperties.System.Author, propColl(SystemProperties.System.Author).ValueAsObject)
                    End If

                    If propColl.Contains(SystemProperties.System.Keywords) Then
                        propertyWriter.WriteProperty(SystemProperties.System.Keywords, propColl(SystemProperties.System.Keywords).ValueAsObject)
                    End If

                    If propColl.Contains(SystemProperties.System.Comment) Then
                        propertyWriter.WriteProperty(SystemProperties.System.Comment, propColl(SystemProperties.System.Comment).ValueAsObject)
                    End If

                    If propColl.Contains(SystemProperties.System.Category) Then
                        propertyWriter.WriteProperty(SystemProperties.System.Category, propColl(SystemProperties.System.Category).ValueAsObject)
                    End If

                    If propColl.Contains(SystemProperties.System.ContentStatus) Then
                        propertyWriter.WriteProperty(SystemProperties.System.Title, propColl(SystemProperties.System.Title).ValueAsObject)
                    End If

                    If propColl.Contains(SystemProperties.System.Photo.DateTaken) Then
                        propertyWriter.WriteProperty(SystemProperties.System.Photo.DateTaken, propColl(SystemProperties.System.Photo.DateTaken).ValueAsObject)
                    End If

                    If propColl.Contains(SystemProperties.System.Photo.CameraModel) Then
                        propertyWriter.WriteProperty(SystemProperties.System.Photo.CameraModel, propColl(SystemProperties.System.Photo.CameraModel).ValueAsObject)
                    End If

                    If propColl.Contains(SystemProperties.System.Rating) Then
                        propertyWriter.WriteProperty(SystemProperties.System.Rating, propColl(SystemProperties.System.Rating).ValueAsObject)
                    End If
                End Using

                currentlySelected = selectedSO
                DisplayProperties(selectedSO)

                showChildItemsButton.Enabled = If(TypeOf selectedSO Is ShellContainer, True, False)
            End If
        End Sub

		Private Sub saveCFD_FileTypeChanged(ByVal sender As Object, ByVal e As EventArgs)
			Dim cfd As CommonSaveFileDialog = TryCast(sender, CommonSaveFileDialog)

			If cfd.SelectedFileTypeIndex = 1 Then
                cfd.SetCollectedPropertyKeys(True, SystemProperties.System.Title, SystemProperties.System.Author)
				cfd.DefaultExtension = ".docx"
			ElseIf cfd.SelectedFileTypeIndex = 2 Then
                cfd.SetCollectedPropertyKeys(True, SystemProperties.System.Photo.DateTaken, SystemProperties.System.Photo.CameraModel)
				cfd.DefaultExtension = ".jpg"
			End If
		End Sub
	End Class
End Namespace