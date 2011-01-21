' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System.Windows.Forms
Imports System.Diagnostics
Imports System
Imports System.Threading
Imports System.Timers
Imports Microsoft.WindowsAPICodePack.Shell
Imports System.IO
Imports Microsoft.WindowsAPICodePack.ApplicationServices
Imports Microsoft.WindowsAPICodePack.Dialogs

Namespace Microsoft.WindowsAPICodePack.Samples.AppRestartRecoveryDemo
	Partial Public Class Form1
		Inherits Form
		Private Shared AppTitle As String = "Application Restart/Recovery Demo"
		Private Shared CurrentFile As New FileSettings()
		Private Shared RecoveryFile As String = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "AppRestartRecoveryDemoData.xml")
		Private Shared DataSeparatorString As String = "@@@@@@@@@@"

		'
		Private internalLoad As Boolean = False
		Private recovered As Boolean = False
		Private startTime As DateTime

		Public Sub New()
			Debug.WriteLine("ARR: Demo started")

			InitializeComponent()


			Form1.CurrentFile.IsDirty = False

			UpdateAppTitle()

			RegisterForRestart()
			RegisterForRecovery()


            statusLabel.Text = "Application successfully registered for restart / recovery. Wait 60s before crashing the application."

			' SetupTimerNotifyForRestart sets a timer to 
			' beep when 60 seconds have elapsed, indicating that
			' WER will restart the program after a crash.
			' WER will not restart applications that crash
			' within 60 seconds of startup.
			SetupTimerNotifyForRestart()

			' If we started with /restart command line argument 
			' then we were automatically restarted and should
			' try to resume the previous session.
			If System.Environment.GetCommandLineArgs().Length > 1 AndAlso System.Environment.GetCommandLineArgs()(1) = "/restart" Then
				recovered = True
				RecoverLastSession(System.Environment.GetCommandLineArgs()(1))
			End If
		End Sub

		Private Sub SetupTimerNotifyForRestart()
			' Beep when 60 seconds has elapsed.
			Dim notify As New System.Timers.Timer(60000)
			AddHandler notify.Elapsed, AddressOf NotifyUser
			notify.AutoReset = False ' Only beep once.
			notify.Enabled = True
		End Sub

		Private Sub NotifyUser(ByVal source As Object, ByVal e As ElapsedEventArgs)
			statusLabel.Text = "It is ""safe"" to crash now! (click App Restart Recovery->Crash!)"
		End Sub

		Private Sub Crash()
			Environment.FailFast("ARR Demo intentional crash.")
		End Sub

		Private Sub RegisterForRestart()
			' Register for automatic restart if the 
			' application was terminated for any reason
			' other than a system reboot or a system update.
			ApplicationRestartRecoveryManager.RegisterForApplicationRestart(New RestartSettings("/restart", RestartRestrictions.NotOnReboot Or RestartRestrictions.NotOnPatch))

			Debug.WriteLine("ARR: Registered for restart")
		End Sub

		Private Sub RegisterForRecovery()
			' Don't pass any state. We'll use our static variable "CurrentFile" to determine
			' the current state of the application.
			' Since this registration is being done on application startup, we don't have a state currently.
			' In some cases it might make sense to pass this initial state.
			' Another approach: When doing "auto-save", register for recovery everytime, and pass
			' the current state at that time. 
			Dim data As New RecoveryData(New RecoveryCallback(AddressOf RecoveryProcedure), Nothing)
			Dim settings As New RecoverySettings(data, 0)

			ApplicationRestartRecoveryManager.RegisterForApplicationRecovery(settings)

			Debug.WriteLine("ARR: Registered for recovery")
		End Sub

		' This method is invoked by WER. 
		Private Function RecoveryProcedure(ByVal state As Object) As Integer
			Debug.WriteLine("ARR: Recovery procedure called!!!")

			PingSystem()

			' Do recovery work here.
			' Signal to WER that the recovery
			' is still in progress.

			' Write the contents of the file, as well as some other data that we need
			File.WriteAllText(RecoveryFile, String.Format("{1}{0}{2}{0}{3}", DataSeparatorString, CurrentFile.Filename, CurrentFile.IsDirty, CurrentFile.Contents))

			Debug.WriteLine("File path: " & RecoveryFile)
			Debug.WriteLine("File exists: " & File.Exists(RecoveryFile))
			Debug.WriteLine("Application shutting down...")

			ApplicationRestartRecoveryManager.ApplicationRecoveryFinished(True)
			Return 0
		End Function

		' This method is called periodically to ensure
		' that WER knows that recovery is still in progress.
		Private Sub PingSystem()
			' Find out if the user canceled recovery.
			Dim isCanceled As Boolean = ApplicationRestartRecoveryManager.ApplicationRecoveryInProgress()

			If isCanceled Then
				Console.WriteLine("Recovery has been canceled by user.")
				Environment.Exit(2)
			End If
		End Sub

		' This method gets called by main when the 
		' commandline arguments indicate that this
		' application was automatically restarted 
		' by WER.
		Private Sub RecoverLastSession(ByVal command As String)
			If Not File.Exists(RecoveryFile) Then
				MessageBox.Show(Me, String.Format("Recovery file {0} does not exist", RecoveryFile))
				internalLoad = True
				textBox1.Text = "Could not recover the data. Recovery data file does not exist"
				internalLoad = False
				UpdateAppTitle()
				Return
			End If

			' Perform application state restoration 
			' actions here.
			Dim contents As String = File.ReadAllText(RecoveryFile)

			CurrentFile.Filename = contents.Remove(contents.IndexOf(Form1.DataSeparatorString))

			contents = contents.Remove(0, contents.IndexOf(Form1.DataSeparatorString) + Form1.DataSeparatorString.Length)

			CurrentFile.IsDirty = If(contents.Remove(contents.IndexOf(Form1.DataSeparatorString)) = "True", True, False)

			contents = contents.Remove(0, contents.IndexOf(Form1.DataSeparatorString) + Form1.DataSeparatorString.Length)

			CurrentFile.Contents = contents

			' Load our textbox
			textBox1.Text = CurrentFile.Contents

			' Update the title
			UpdateAppTitle()

			' Reset our variable so next title updates we don't show the "recovered" text
			recovered = False
		End Sub

		Private Sub exitToolStripMenuItem_Click(ByVal sender As Object, ByVal e As EventArgs) Handles exitToolStripMenuItem.Click
            If PromptForSave() Then
                Application.Exit()
            End If
        End Sub

		Private Sub openToolStripMenuItem_Click(ByVal sender As Object, ByVal e As EventArgs) Handles openToolStripMenuItem.Click
            If PromptForSave() Then
                Dim cfd As New CommonOpenFileDialog()
                cfd.Filters.Add(New CommonFileDialogFilter("Text files", ".txt"))

                Dim result As CommonFileDialogResult = cfd.ShowDialog()

                If result = CommonFileDialogResult.Ok Then
                    internalLoad = True
                    Form1.CurrentFile.Load(cfd.FileName)
                    textBox1.Text = CurrentFile.Contents
                    internalLoad = False

                    UpdateAppTitle()
                End If
            End If
        End Sub

        Private Function PromptForSave() As Boolean
            If Not CurrentFile.IsDirty Then
                Return True
            End If

            ' ask the user to save.
            Dim dr As DialogResult = MessageBox.Show(Me, "Current document has changed. Would you like to save?", "Save current document", MessageBoxButtons.YesNoCancel)

            If dr = System.Windows.Forms.DialogResult.Cancel Then
                Return False
            End If

            If dr = System.Windows.Forms.DialogResult.Yes Then
                ' Does the current file have a name?
                If String.IsNullOrEmpty(Form1.CurrentFile.Filename) Then
                    Dim saveAsCFD As New CommonSaveFileDialog()
                    saveAsCFD.Filters.Add(New CommonFileDialogFilter("Text files", ".txt"))
                    saveAsCFD.AlwaysAppendDefaultExtension = True

                    If saveAsCFD.ShowDialog() = CommonFileDialogResult.Ok Then
                        Form1.CurrentFile.Save(saveAsCFD.FileName)
                        UpdateAppTitle()
                    Else
                        Return False
                    End If
                Else
                    ' just save it
                    Form1.CurrentFile.Save(CurrentFile.Filename)
                    UpdateAppTitle()
                End If
            End If

            Return True
        End Function

        Private Sub textBox1_TextChanged(ByVal sender As Object, ByVal e As EventArgs) Handles textBox1.TextChanged
            If (Not internalLoad) AndAlso Form1.CurrentFile IsNot Nothing Then
                Form1.CurrentFile.IsDirty = True
                Form1.CurrentFile.Contents = textBox1.Text
                UpdateAppTitle()
            End If
        End Sub

        Private Sub saveToolStripMenuItem_Click(ByVal sender As Object, ByVal e As EventArgs) Handles saveToolStripMenuItem.Click
            ' Does the current file have a name?
            If String.IsNullOrEmpty(Form1.CurrentFile.Filename) Then
                Dim saveAsCFD As New CommonSaveFileDialog()
                saveAsCFD.Filters.Add(New CommonFileDialogFilter("Text files", ".txt"))

                If saveAsCFD.ShowDialog() = CommonFileDialogResult.Ok Then
                    Form1.CurrentFile.Save(saveAsCFD.FileName)
                    UpdateAppTitle()
                Else
                    Return
                End If
            Else
                ' just save it
                Form1.CurrentFile.Save(Form1.CurrentFile.Filename)
                UpdateAppTitle()
            End If
        End Sub

		Private Sub UpdateAppTitle()
            Dim dirtyState As String = If(Form1.CurrentFile.IsDirty, "*", "")
            Dim filename As String = If(String.IsNullOrEmpty(Form1.CurrentFile.Filename), _
                "Untitled", Path.GetFileName(Form1.CurrentFile.Filename))

            Me.Text = String.Format("{0}{1} - {2}", filename, dirtyState, AppTitle)

			If recovered Then
				Me.Text &= " (RECOVERED FROM CRASH)"
			End If
		End Sub

		Private Sub crashToolStripMenuItem_Click(ByVal sender As Object, ByVal e As EventArgs) Handles crashToolStripMenuItem.Click
			Crash()
		End Sub

		Private Sub undoToolStripMenuItem_Click(ByVal sender As Object, ByVal e As EventArgs) Handles undoToolStripMenuItem.Click
			textBox1.Undo()
		End Sub

		Private Sub cutToolStripMenuItem1_Click(ByVal sender As Object, ByVal e As EventArgs) Handles cutToolStripMenuItem1.Click
			textBox1.Cut()
		End Sub

		Private Sub copyToolStripMenuItem1_Click(ByVal sender As Object, ByVal e As EventArgs) Handles copyToolStripMenuItem1.Click
			textBox1.Copy()
		End Sub

		Private Sub pasteToolStripMenuItem1_Click(ByVal sender As Object, ByVal e As EventArgs) Handles pasteToolStripMenuItem1.Click
			textBox1.Paste()
		End Sub

		Private Sub selectAllToolStripMenuItem1_Click(ByVal sender As Object, ByVal e As EventArgs) Handles selectAllToolStripMenuItem1.Click
			textBox1.SelectAll()
		End Sub

		Private Sub aboutToolStripMenuItem_Click(ByVal sender As Object, ByVal e As EventArgs) Handles aboutToolStripMenuItem.Click
			MessageBox.Show(Me, "Application Restart and Recovery demo", "Windows API Code Pack for .NET Framework")
		End Sub

		Private Sub timer1_Tick(ByVal sender As Object, ByVal e As EventArgs) Handles timer1.Tick
			Dim span As TimeSpan = DateTime.Now.Subtract(startTime)
			timerLabel.Text = String.Format("App running for {0}s", CInt(Fix(span.TotalSeconds)))
		End Sub

		Private Sub Form1_Load(ByVal sender As Object, ByVal e As EventArgs) Handles MyBase.Load
			startTime = DateTime.Now
		End Sub

        Private Sub newToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles newToolStripMenuItem.Click
            If PromptForSave() Then
                textBox1.Clear()
                CurrentFile = New FileSettings()
                CurrentFile.IsDirty = False
                UpdateAppTitle()
            End If
        End Sub
    End Class
End Namespace
