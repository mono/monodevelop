'Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Navigation
Imports Microsoft.WindowsAPICodePack.Shell
Imports System.Collections.Generic
Imports System.Collections
Imports Microsoft.WindowsAPICodePack.Dialogs

Namespace Microsoft.WindowsAPICodePack.Samples.StarBackupSample
	Partial Public Class StartBackupPage
		Inherits PageFunction(Of WizardResult)
		Public Sub New()
			InitializeComponent()
		End Sub

		Private Sub cancelButton_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
			' Cancel the wizard and don't return any data
			OnReturn(New ReturnEventArgs(Of WizardResult)(WizardResult.Canceled))
		End Sub

		Public Sub wizardPage_Return(ByVal sender As Object, ByVal e As ReturnEventArgs(Of WizardResult))
			' If returning, wizard was completed (finished or canceled),
			' so continue returning to calling page
			OnReturn(e)
		End Sub

		Private Sub buttonAddFolders_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
			' Show an Open File Dialog
			Dim cfd As New CommonOpenFileDialog()

			' Allow users to select folders and non-filesystem items such as Libraries
			cfd.AllowNonFileSystemItems = True
			cfd.IsFolderPicker = True

			' MultiSelect = true will allow mutliple selection of folders/libraries.
			cfd.Multiselect = True

            If cfd.ShowDialog() = CommonFileDialogResult.Ok Then
                Dim items As ICollection(Of ShellObject) = cfd.FilesAsShellObject

                For Each item As ShellObject In items
                    ' If it's a library, need to add the actual folders (scopes)
                    If TypeOf item Is ShellLibrary Then
                        For Each folder As ShellFileSystemFolder In (CType(item, ShellLibrary))
                            listBox1.Items.Add(folder.Path)
                        Next folder
                    ElseIf TypeOf item Is ShellFileSystemFolder Then
                        ' else, just add it...
                        listBox1.Items.Add((CType(item, ShellFileSystemFolder)).Path)
                    Else
                        ' For unsupported locations, display an error message.
                        ' The above code could be expanded to backup Known Folders that are not virtual,
                        ' Search Folders, etc.
                        MessageBox.Show(String.Format("The {0} folder was skipped because it cannot be backed up.", item.Name), "Star Backup")
                    End If
                Next item
            End If

			' If we added something, Enable the "Start Backup" button
			If listBox1.Items.Count > 0 Then
				buttonStartBackup.IsEnabled = True
			End If
		End Sub

		Private Sub buttonStartBackup_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
			' Go to next wizard page (Processing or doing the actual backup)
			Dim processPage As New BackupProcessPage(listBox1.Items)
			AddHandler processPage.Return, AddressOf wizardPage_Return
			Me.NavigationService.Navigate(processPage)
		End Sub
	End Class
End Namespace
