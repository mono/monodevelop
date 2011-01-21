'Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Navigation

Namespace Microsoft.WindowsAPICodePack.Samples.StarBackupSample
	Public Class WizardLauncher
		Inherits PageFunction(Of WizardResult)
		Public Event WizardReturn As WizardReturnEventHandler

		Protected Overrides Sub Start()
			MyBase.Start()

			' So we remember the WizardCompleted event registration
			Me.KeepAlive = True

			' Launch the wizard
			Dim StarBackupMain As New StarBackupMain()
			AddHandler StarBackupMain.Return, AddressOf wizardPage_Return
			Me.NavigationService.Navigate(StarBackupMain)
		End Sub

		Public Sub wizardPage_Return(ByVal sender As Object, ByVal e As ReturnEventArgs(Of WizardResult))
			' Notify client that wizard has completed
			' NOTE: We need this custom event because the Return event cannot be
			' registered by window code - if WizardDialogBox registers an event handler with
			' the WizardLauncher's Return event, the event is not raised.
			If Me.WizardReturnEvent IsNot Nothing Then
				RaiseEvent WizardReturn(Me, New WizardReturnEventArgs(e.Result, Nothing))
			End If
			OnReturn(Nothing)
		End Sub
	End Class
End Namespace
