'Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Windows

Namespace Microsoft.WindowsAPICodePack.Samples.StarBackupSample

	Partial Public Class MainWindow
		Inherits Window
		Public Sub New()
			InitializeComponent()

			Dim wizard As New WizardDialogBox()
			Dim dialogResult As Boolean = CBool(wizard.ShowDialog())

			If dialogResult = True Then

			Else

			End If

			' shutdown
			Application.Current.Shutdown()
		End Sub

		Private Sub runWizardButton_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
		End Sub
	End Class
End Namespace