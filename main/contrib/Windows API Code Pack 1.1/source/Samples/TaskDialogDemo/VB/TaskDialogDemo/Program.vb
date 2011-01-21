'Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Diagnostics
Imports System.Windows.Forms
Imports Microsoft.WindowsAPICodePack
Imports Microsoft.WindowsAPICodePack.Dialogs

Namespace TaskDialogDemo
	Friend NotInheritable Class Program
		Private Shared MaxRange As Integer = 5000

		' used by the event handlers incase they need to access the parent taskdialog
		Private Shared currentTaskDialog As TaskDialog = Nothing

		''' <summary>
		''' The main entry point for the application.
		''' </summary>
		Private Sub New()
		End Sub
		<STAThread> _
		Shared Sub Main()
			Application.EnableVisualStyles()
			CreateTaskDialogDemo()
		End Sub

		Private Shared Sub CreateTaskDialogDemo()
			Dim taskDialogMain As New TaskDialog()
			taskDialogMain.Caption = "TaskDialog Samples"
			taskDialogMain.InstructionText = "Pick a sample to try:"
			taskDialogMain.FooterText = "Demo application as part of <a href=""http://code.msdn.microsoft.com/WindowsAPICodePack"">Windows API Code Pack for .NET Framework</a>"
			taskDialogMain.Cancelable = True

			' Enable the hyperlinks
			taskDialogMain.HyperlinksEnabled = True
			AddHandler taskDialogMain.HyperlinkClick, AddressOf taskDialogMain_HyperlinkClick

			' Add a close button so user can close our dialog
			taskDialogMain.StandardButtons = TaskDialogStandardButtons.Close

'			#Region "Creating and adding command link buttons"

			Dim buttonTestHarness As New TaskDialogCommandLink("test_harness", "TaskDialog Test Harness")
			AddHandler buttonTestHarness.Click, AddressOf buttonTestHarness_Click

			Dim buttonCommon As New TaskDialogCommandLink("common_buttons", "Common Buttons Sample")
			AddHandler buttonCommon.Click, AddressOf buttonCommon_Click

            Dim buttonElevation As New TaskDialogCommandLink("elevation", "Elevation Required Sample")
			AddHandler buttonElevation.Click, AddressOf buttonElevation_Click

			Dim buttonError As New TaskDialogCommandLink("error", "Error Sample")
			AddHandler buttonError.Click, AddressOf buttonError_Click

			Dim buttonIcons As New TaskDialogCommandLink("icons", "Icons Sample")
			AddHandler buttonIcons.Click, AddressOf buttonIcons_Click

			Dim buttonProgress As New TaskDialogCommandLink("progress", "Progress Sample")
			AddHandler buttonProgress.Click, AddressOf buttonProgress_Click

			Dim buttonProgressEffects As New TaskDialogCommandLink("progress_effects", "Progress Effects Sample")
			AddHandler buttonProgressEffects.Click, AddressOf buttonProgressEffects_Click

			Dim buttonTimer As New TaskDialogCommandLink("timer", "Timer Sample")
			AddHandler buttonTimer.Click, AddressOf buttonTimer_Click

			Dim buttonCustomButtons As New TaskDialogCommandLink("customButtons", "Custom Buttons Sample")
			AddHandler buttonCustomButtons.Click, AddressOf buttonCustomButtons_Click

			Dim buttonEnableDisable As New TaskDialogCommandLink("enableDisable", "Enable/Disable sample")
			AddHandler buttonEnableDisable.Click, AddressOf buttonEnableDisable_Click

			taskDialogMain.Controls.Add(buttonTestHarness)
			taskDialogMain.Controls.Add(buttonCommon)
			taskDialogMain.Controls.Add(buttonCustomButtons)
			taskDialogMain.Controls.Add(buttonEnableDisable)
			taskDialogMain.Controls.Add(buttonElevation)
			taskDialogMain.Controls.Add(buttonError)
			taskDialogMain.Controls.Add(buttonIcons)
			taskDialogMain.Controls.Add(buttonProgress)
			taskDialogMain.Controls.Add(buttonProgressEffects)
			taskDialogMain.Controls.Add(buttonTimer)

'			#End Region

			' Show the taskdialog
			taskDialogMain.Show()
		End Sub


		Private Shared enableDisableRadioButton As TaskDialogRadioButton = Nothing
		Private Shared enableButton As TaskDialogButton = Nothing
		Private Shared disableButton As TaskDialogButton = Nothing

		Private Shared Sub buttonEnableDisable_Click(ByVal sender As Object, ByVal e As EventArgs)
			' Enable/disable sample
			Dim tdEnableDisable As New TaskDialog()
			tdEnableDisable.Cancelable = True
			tdEnableDisable.Caption = "Enable/Disable Sample"
			tdEnableDisable.InstructionText = "Click on the buttons to enable or disable the radiobutton."

			enableButton = New TaskDialogButton("enableButton", "Enable")
			enableButton.Default = True
			AddHandler enableButton.Click, AddressOf enableButton_Click

			disableButton = New TaskDialogButton("disableButton", "Disable")
			AddHandler disableButton.Click, AddressOf disableButton_Click

			enableDisableRadioButton = New TaskDialogRadioButton("enableDisableRadioButton", "Sample Radio button")
			enableDisableRadioButton.Enabled = False

			tdEnableDisable.Controls.Add(enableDisableRadioButton)
			tdEnableDisable.Controls.Add(enableButton)
			tdEnableDisable.Controls.Add(disableButton)

			Dim tdr As TaskDialogResult = tdEnableDisable.Show()

			enableDisableRadioButton = Nothing
			RemoveHandler enableButton.Click, AddressOf enableButton_Click
			RemoveHandler disableButton.Click, AddressOf disableButton_Click
			enableButton = Nothing
			disableButton = Nothing
		End Sub

		Private Shared Sub disableButton_Click(ByVal sender As Object, ByVal e As EventArgs)
			If enableDisableRadioButton IsNot Nothing Then
				enableDisableRadioButton.Enabled = False
			End If

			If enableButton IsNot Nothing Then
				enableButton.Enabled = True
			End If

			If disableButton IsNot Nothing Then
				disableButton.Enabled = False
			End If
		End Sub

		Private Shared Sub enableButton_Click(ByVal sender As Object, ByVal e As EventArgs)
			If enableDisableRadioButton IsNot Nothing Then
				enableDisableRadioButton.Enabled = True
			End If

			If enableButton IsNot Nothing Then
				enableButton.Enabled = False
			End If

			If disableButton IsNot Nothing Then
				disableButton.Enabled = True
			End If
		End Sub

		Private Shared tdCustomButtons As TaskDialog = Nothing
		Private Shared Sub buttonCustomButtons_Click(ByVal sender As Object, ByVal e As EventArgs)
			' Custom buttons sample
			tdCustomButtons = New TaskDialog()
			tdCustomButtons.Cancelable = True
			tdCustomButtons.Caption = "Custom Buttons Sample"
			tdCustomButtons.InstructionText = "Click on any of the custom buttons to get a specific message box"

			Dim button1 As New TaskDialogButton("button1", "Custom Button 1")
			AddHandler button1.Click, AddressOf button1_Click
			button1.Default = True
			tdCustomButtons.Controls.Add(button1)

			Dim button2 As New TaskDialogButton("button2", "Custom Button 2")
			AddHandler button2.Click, AddressOf button2_Click
			tdCustomButtons.Controls.Add(button2)

			Dim button3 As New TaskDialogButton("button3", "Custom Close Button")
			AddHandler button3.Click, AddressOf button3_Click
			tdCustomButtons.Controls.Add(button3)

			Dim result As TaskDialogResult = tdCustomButtons.Show()

			tdCustomButtons = Nothing
		End Sub

		Private Shared Sub button3_Click(ByVal sender As Object, ByVal e As EventArgs)
			MessageBox.Show("Custom close button was clicked. Closing the dialog...", "Custom Buttons Sample")

			If tdCustomButtons IsNot Nothing Then
				tdCustomButtons.Close(TaskDialogResult.CustomButtonClicked)
			End If
		End Sub

		Private Shared Sub button2_Click(ByVal sender As Object, ByVal e As EventArgs)
			MessageBox.Show("Custom button 2 was clicked", "Custom Buttons Sample")

		End Sub

		Private Shared Sub button1_Click(ByVal sender As Object, ByVal e As EventArgs)
			MessageBox.Show("Custom button 1 was clicked", "Custom Buttons Sample")
		End Sub

		Private Shared Sub taskDialogMain_HyperlinkClick(ByVal sender As Object, ByVal e As TaskDialogHyperlinkClickedEventArgs)
			' Launch the application associated with http links
			Process.Start(e.LinkText)
		End Sub

		Private Shared Sub buttonTestHarness_Click(ByVal sender As Object, ByVal e As EventArgs)
			Dim th As New TestHarness()
			th.ShowDialog()
		End Sub

		Private Shared Sub buttonCommon_Click(ByVal sender As Object, ByVal e As EventArgs)
			' Common buttons sample
			Dim tdCommonButtons As New TaskDialog()
			tdCommonButtons.Cancelable = True
			tdCommonButtons.Caption = "Common Buttons Sample"
			tdCommonButtons.InstructionText = "Click on any of the buttons to get a specific message box"

			tdCommonButtons.StandardButtons = TaskDialogStandardButtons.Ok Or TaskDialogStandardButtons.Cancel Or TaskDialogStandardButtons.Yes Or TaskDialogStandardButtons.No Or TaskDialogStandardButtons.Retry Or TaskDialogStandardButtons.Cancel Or TaskDialogStandardButtons.Close

			Dim tdr As TaskDialogResult = tdCommonButtons.Show()

            MessageBox.Show(String.Format("The ""{0}"" button was clicked", If(tdr = TaskDialogResult.Ok, "OK", tdr.ToString())), "Common Buttons Sample")
		End Sub

		Private Shared tdElevation As TaskDialog = Nothing
		Private Shared Sub buttonElevation_Click(ByVal sender As Object, ByVal e As EventArgs)
			' Show a dialog with elevation button
			tdElevation = New TaskDialog()
			tdElevation.Cancelable = True
			tdElevation.InstructionText = "Elevated task example"

            Dim adminTaskButton As New TaskDialogCommandLink("adminTaskButton", "Admin stuff", "Run some admin tasks")
            adminTaskButton.UseElevationIcon = True
			AddHandler adminTaskButton.Click, AddressOf adminTaskButton_Click
			adminTaskButton.Default = True

			tdElevation.Controls.Add(adminTaskButton)

            tdElevation.Show()

			tdElevation = Nothing
		End Sub

		Private Shared Sub adminTaskButton_Click(ByVal sender As Object, ByVal e As EventArgs)
			If tdElevation IsNot Nothing Then
				tdElevation.Close(TaskDialogResult.Ok)
			End If
		End Sub

		Private Shared tdError As TaskDialog = Nothing

		Private Shared Sub buttonError_Click(ByVal sender As Object, ByVal e As EventArgs)
			' Error dialog
			tdError = New TaskDialog()
			tdError.DetailsExpanded = False
			tdError.Cancelable = True
			tdError.Icon = TaskDialogStandardIcon.Error

			tdError.Caption = "Error Sample 1"
			tdError.InstructionText = "An unexpected error occured. Please send feedback now!"
			tdError.Text = "Error message goes here..."
			tdError.DetailsExpandedLabel = "Hide details"
			tdError.DetailsCollapsedLabel = "Show details"
			tdError.DetailsExpandedText = "Stack trace goes here..."

			tdError.FooterCheckBoxChecked = True
			tdError.FooterCheckBoxText = "Don't ask me again"

			tdError.ExpansionMode = TaskDialogExpandedDetailsLocation.ExpandFooter

			Dim sendButton As New TaskDialogCommandLink("sendButton", "Send Feedback" & Constants.vbLf & "I'm in a giving mood")
			AddHandler sendButton.Click, AddressOf sendButton_Click

			Dim dontSendButton As New TaskDialogCommandLink("dontSendButton", "No Thanks" & Constants.vbLf & "I don't feel like being helpful")
			AddHandler dontSendButton.Click, AddressOf dontSendButton_Click

			tdError.Controls.Add(sendButton)
			tdError.Controls.Add(dontSendButton)

			tdError.Show()

			tdError = Nothing
		End Sub

		Private Shared Sub dontSendButton_Click(ByVal sender As Object, ByVal e As EventArgs)
			If tdError IsNot Nothing Then
				tdError.Close(TaskDialogResult.Ok)
			End If
		End Sub

		Private Shared sendFeedbackProgressBar As TaskDialogProgressBar

		Private Shared Sub sendButton_Click(ByVal sender As Object, ByVal e As EventArgs)
			' Send feedback button
			Dim tdSendFeedback As New TaskDialog()
			tdSendFeedback.Cancelable = True

			tdSendFeedback.Caption = "Send Feedback Dialog"
			tdSendFeedback.Text = "Sending your feedback ....."

			' Show a progressbar
			sendFeedbackProgressBar = New TaskDialogProgressBar(0, MaxRange, 0)
			tdSendFeedback.ProgressBar = sendFeedbackProgressBar

			' Subscribe to the tick event, so we can update the title/caption also close the dialog when done
			AddHandler tdSendFeedback.Tick, AddressOf tdSendFeedback_Tick
			tdSendFeedback.Show()

			If tdError IsNot Nothing Then
				tdError.Close(TaskDialogResult.Ok)
			End If
		End Sub

		Private Shared Sub tdSendFeedback_Tick(ByVal sender As Object, ByVal e As TaskDialogTickEventArgs)
			If MaxRange >= e.Ticks Then
				CType(sender, TaskDialog).InstructionText = String.Format("Sending ....{0}", e.Ticks)
				CType(sender, TaskDialog).ProgressBar.Value = e.Ticks
			Else
				CType(sender, TaskDialog).InstructionText = "Thanks for the feedback!"
				CType(sender, TaskDialog).Text = "Our developers will get right on that..."
				CType(sender, TaskDialog).ProgressBar.Value = MaxRange
			End If
		End Sub

		Private Shared Sub buttonIcons_Click(ByVal sender As Object, ByVal e As EventArgs)
			' Show icons on the taskdialog

			Dim tdIcons As New TaskDialog()
			currentTaskDialog = tdIcons
			tdIcons.Cancelable = True

			tdIcons.Caption = "Icons Sample"
			tdIcons.InstructionText = "Main Instructions"
			tdIcons.FooterText = "Footer Text"

			Dim radioNone As New TaskDialogRadioButton("radioNone", "None")
			radioNone.Default = True ' default is no icons
			AddHandler radioNone.Click, AddressOf iconsRadioButton_Click

			Dim radioError As New TaskDialogRadioButton("radioError", "Error")
			AddHandler radioError.Click, AddressOf iconsRadioButton_Click

			Dim radioWarning As New TaskDialogRadioButton("radioWarning", "Warning")
			AddHandler radioWarning.Click, AddressOf iconsRadioButton_Click

			Dim radioInformation As New TaskDialogRadioButton("radioInformation", "Information")
			AddHandler radioInformation.Click, AddressOf iconsRadioButton_Click

			Dim radioShield As New TaskDialogRadioButton("radioShield", "Shield")
			AddHandler radioShield.Click, AddressOf iconsRadioButton_Click

			tdIcons.Controls.Add(radioNone)
			tdIcons.Controls.Add(radioError)
			tdIcons.Controls.Add(radioWarning)
			tdIcons.Controls.Add(radioInformation)
			tdIcons.Controls.Add(radioShield)

			tdIcons.Show()

			currentTaskDialog = Nothing
		End Sub

		Private Shared Sub iconsRadioButton_Click(ByVal sender As Object, ByVal e As EventArgs)
			Dim radioButton As TaskDialogRadioButton = TryCast(sender, TaskDialogRadioButton)

			If radioButton IsNot Nothing AndAlso currentTaskDialog IsNot Nothing Then
				Select Case radioButton.Name
					Case "radioNone"
						currentTaskDialog.FooterIcon = TaskDialogStandardIcon.None
						currentTaskDialog.Icon = currentTaskDialog.FooterIcon
					Case "radioError"
						currentTaskDialog.FooterIcon = TaskDialogStandardIcon.Error
						currentTaskDialog.Icon = currentTaskDialog.FooterIcon
					Case "radioWarning"
						currentTaskDialog.FooterIcon = TaskDialogStandardIcon.Warning
						currentTaskDialog.Icon = currentTaskDialog.FooterIcon
					Case "radioInformation"
						currentTaskDialog.FooterIcon = TaskDialogStandardIcon.Information
						currentTaskDialog.Icon = currentTaskDialog.FooterIcon
					Case "radioShield"
						currentTaskDialog.FooterIcon = TaskDialogStandardIcon.Shield
						currentTaskDialog.Icon = currentTaskDialog.FooterIcon
				End Select
			End If
		End Sub

		Private Shared progressTDProgressBar As TaskDialogProgressBar

		Private Shared Sub buttonProgress_Click(ByVal sender As Object, ByVal e As EventArgs)
			Dim tdProgressSample As New TaskDialog()
			currentTaskDialog = tdProgressSample
			tdProgressSample.Cancelable = True
			tdProgressSample.Caption = "Progress Sample"

			progressTDProgressBar = New TaskDialogProgressBar(0, MaxRange, 0)
			tdProgressSample.ProgressBar = progressTDProgressBar

			AddHandler tdProgressSample.Tick, AddressOf tdProgressSample_Tick

			tdProgressSample.Show()

			currentTaskDialog = Nothing
		End Sub

		Private Shared Sub tdProgressSample_Tick(ByVal sender As Object, ByVal e As TaskDialogTickEventArgs)
			If MaxRange >= e.Ticks Then
				CType(sender, TaskDialog).InstructionText = String.Format("Progress = {0}", e.Ticks)
				CType(sender, TaskDialog).ProgressBar.Value = e.Ticks
			Else
				CType(sender, TaskDialog).InstructionText = "Progress = Done"
				CType(sender, TaskDialog).ProgressBar.Value = MaxRange
			End If
		End Sub

		Private Shared Sub buttonProgressEffects_Click(ByVal sender As Object, ByVal e As EventArgs)
			Dim tdProgressEffectsSample As New TaskDialog()
			currentTaskDialog = tdProgressEffectsSample
			tdProgressEffectsSample.Cancelable = True
			tdProgressEffectsSample.Caption = "Progress Effects Sample"
			tdProgressEffectsSample.InstructionText = "Shows a dialog with Marquee style"

			Dim progressBarMarquee As New TaskDialogProgressBar()
			progressBarMarquee.State = TaskDialogProgressBarState.Marquee

			tdProgressEffectsSample.ProgressBar = progressBarMarquee

			tdProgressEffectsSample.Show()

			currentTaskDialog = Nothing
		End Sub

		Private Shared Sub buttonTimer_Click(ByVal sender As Object, ByVal e As EventArgs)
			' Timer example dialog
			Dim tdTimer As New TaskDialog()
			tdTimer.Cancelable = True
			AddHandler tdTimer.Tick, AddressOf tdTimer_Tick

			tdTimer.Caption = "Timer Sample"
			tdTimer.InstructionText = "Time elapsed: 0 seconds"

			tdTimer.Show()
		End Sub


		Private Shared Sub tdTimer_Tick(ByVal sender As Object, ByVal e As TaskDialogTickEventArgs)
            CType(sender, TaskDialog).InstructionText = String.Format("Time elapsed: {0} seconds", CType(e.Ticks / 1000, Integer))
		End Sub
	End Class
End Namespace

