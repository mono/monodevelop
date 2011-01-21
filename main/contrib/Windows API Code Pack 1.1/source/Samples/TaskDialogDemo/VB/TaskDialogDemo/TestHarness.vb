'Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Windows.Forms
Imports Microsoft.WindowsAPICodePack
Imports Microsoft.WindowsAPICodePack.Dialogs

Namespace TaskDialogDemo
	Partial Public Class TestHarness
		Inherits Form
		Public Sub New()
			InitializeComponent()
		End Sub

		Private Sub cmdShow_Click(ByVal sender As Object, ByVal e As EventArgs) Handles cmdShow.Click
			Dim td As New TaskDialog()

'			#Region "Button(s)"

			Dim button As TaskDialogStandardButtons = TaskDialogStandardButtons.None

			If chkOK.Checked Then
				button = button Or TaskDialogStandardButtons.Ok
			End If
			If chkCancel.Checked Then
				button = button Or TaskDialogStandardButtons.Cancel
			End If

			If chkYes.Checked Then
				button = button Or TaskDialogStandardButtons.Yes
			End If
			If chkNo.Checked Then
				button = button Or TaskDialogStandardButtons.No
			End If

			If chkClose.Checked Then
				button = button Or TaskDialogStandardButtons.Close
			End If
			If chkRetry.Checked Then
				button = button Or TaskDialogStandardButtons.Retry
			End If

'			#End Region

'			#Region "Icon"

			If rdoError.Checked Then
				td.Icon = TaskDialogStandardIcon.Error
			ElseIf rdoInformation.Checked Then
				td.Icon = TaskDialogStandardIcon.Information
			ElseIf rdoShield.Checked Then
				td.Icon = TaskDialogStandardIcon.Shield
			ElseIf rdoWarning.Checked Then
				td.Icon = TaskDialogStandardIcon.Warning
			End If

'			#End Region

'			#Region "Prompts"

			Dim title As String = txtTitle.Text
			Dim instruction As String = txtInstruction.Text
			Dim content As String = txtContent.Text

'			#End Region

			td.StandardButtons = button
			td.InstructionText = instruction
			td.Caption = title
            td.Text = content
            td.OwnerWindowHandle = Me.Handle

			Dim res As TaskDialogResult = td.Show()

			Me.resultLbl.Text = "Result = " & res.ToString()
		End Sub
	End Class
End Namespace
