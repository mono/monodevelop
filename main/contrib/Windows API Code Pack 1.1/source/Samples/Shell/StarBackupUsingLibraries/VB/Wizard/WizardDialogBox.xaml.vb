'Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Windows
Imports System.Windows.Navigation

Namespace Microsoft.WindowsAPICodePack.Samples.StarBackupSample
	Partial Public Class WizardDialogBox
		Inherits NavigationWindow
		Public Sub New()
			InitializeComponent()

			' Launch the wizard
			Dim wizardLauncher As New WizardLauncher()
			AddHandler wizardLauncher.WizardReturn, AddressOf wizardLauncher_WizardReturn
			Me.Navigate(wizardLauncher)
		End Sub

		Private Sub wizardLauncher_WizardReturn(ByVal sender As Object, ByVal e As WizardReturnEventArgs)
			' Handle wizard return
			If Me.DialogResult Is Nothing Then
				Me.DialogResult = (e.Result = WizardResult.Finished)
			End If
		End Sub
	End Class
End Namespace
