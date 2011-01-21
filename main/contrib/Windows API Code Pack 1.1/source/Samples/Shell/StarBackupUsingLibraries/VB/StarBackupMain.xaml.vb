'Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System.Windows
Imports System.Windows.Media.Imaging
Imports System.Windows.Navigation

Namespace Microsoft.WindowsAPICodePack.Samples.StarBackupSample
	Partial Public Class StarBackupMain
		Inherits PageFunction(Of WizardResult)
		Public Sub New()
			InitializeComponent()

			' Images for the command link buttons
			Dim backupBitmapSource As BitmapSource = StarBackupHelper.ConvertGDI_To_WPF(My.Resources.Backup)
			Dim restoreBitmapSource As BitmapSource = StarBackupHelper.ConvertGDI_To_WPF(My.Resources.Restore)

			commandLink1.Icon = StarBackupHelper.CreateResizedImage(backupBitmapSource, 32, 32)
			commandLink2.Icon = StarBackupHelper.CreateResizedImage(restoreBitmapSource, 32, 32)

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

		Private Sub BackupClicked(ByVal sender As Object, ByVal e As RoutedEventArgs)
			' Go to next wizard page
			Dim backupPage As New StartBackupPage()
			AddHandler backupPage.Return, AddressOf wizardPage_Return
			Me.NavigationService.Navigate(backupPage)
		End Sub

		Private Sub RestoreClicked(ByVal sender As Object, ByVal e As RoutedEventArgs)
            MessageBox.Show("Backup application example: This will perform the restore operation in a real backup application.", "Star Backup Wizard")
		End Sub
	End Class
End Namespace