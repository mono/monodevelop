'Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.IO
Imports System.Reflection
Imports System.Windows.Forms
Imports Microsoft.WindowsAPICodePack.Shell
Imports Microsoft.WindowsAPICodePack.Taskbar
Imports Microsoft.Win32
Imports Microsoft.WindowsAPICodePack
Imports System.Diagnostics
Imports Microsoft.WindowsAPICodePack.Dialogs

Namespace TaskbarDemo
	''' <summary>
	''' A word about known/custom categories.  In order for an application
	''' to have known/custom categories, a file type must be registered with
	''' that application.  This demo provides two menu items that allows you
	''' to register and unregister .txt files with this demo. By default
	''' shell displays the 'Recent' category for an application with a
	''' registered file type.
	''' 
	''' An exception will be thrown if you try to add a shell item to
	''' 'Custom Category 1' before registering a file type with this demo
	''' application.
	''' 
	''' Also, once a file type has been registered with this demo, setting
	''' jumpList.KnownCategoryToDisplay = KnownCategoryType.Neither will have
	''' no effect until at least one custom category or user task has been
	''' added to the taskbar jump list.
	''' </summary>    
	Partial Public Class TaskbarDemoMainForm
		Inherits Form
		Private Const appId As String = "TaskbarDemo"
		Private Const progId As String = "TaskbarDemo"

		Private category1 As New JumpListCustomCategory("Custom Category 1")
		Private category2 As New JumpListCustomCategory("Custom Category 2")

		Private jumpList As JumpList

		Private executableFolder As String
		Private ReadOnly executablePath As String

		Private td As TaskDialog = Nothing

		' Keep a reference to the Taskbar instance
		Private windowsTaskbar As TaskbarManager = TaskbarManager.Instance

		Private childCount As Integer = 0

		#Region "Form Initialize"

		Public Sub New()
			InitializeComponent()

			AddHandler Shown, AddressOf TaskbarDemoMainForm_Shown

			' Set the application specific id
			windowsTaskbar.ApplicationId = appId

			' Save current folder and path of running executable
			executablePath = System.Reflection.Assembly.GetEntryAssembly().Location
			executableFolder = Path.GetDirectoryName(executablePath)

			' Sanity check - will avoid throwing exceptions if the file type is not registered.
			CheckFileRegistration()

			' Set our title if we were launched from the Taskbar
			Dim args() As String = Environment.GetCommandLineArgs()

			If args.Length > 2 AndAlso args(1) = "/doc" Then
				Dim fileName As String = String.Join(" ", args, 2, args.Length - 2)
				Me.Text = String.Format("{0} - Taskbar Demo", Path.GetFileName(fileName))
			Else
				Me.Text = "Taskbar Demo"
            End If

            HighlightOverlaySelection(labelNoIconOverlay)
		End Sub

		Private Sub TaskbarDemoMainForm_Shown(ByVal sender As Object, ByVal e As EventArgs)
			' create a new taskbar jump list for the main window
			jumpList = JumpList.CreateJumpList()

			' Add custom categories
			jumpList.AddCustomCategories(category1, category2)

			' Default values for jump lists
			comboBoxKnownCategoryType.SelectedItem = "Recent"

			' Progress Bar
			For Each state As String In System.Enum.GetNames(GetType(TaskbarProgressBarState))
				comboBoxProgressBarStates.Items.Add(state)
			Next state

			'
			comboBoxProgressBarStates.SelectedItem = "NoProgress"

			' Update UI
			UpdateStatusBar("Application ready...")

			' Set our default
			TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress)
		End Sub

		Private Sub CheckFileRegistration()
			Dim registered As Boolean = False

			Try
				Dim openWithKey As RegistryKey = Registry.ClassesRoot.OpenSubKey(Path.Combine(".txt", "OpenWithProgIds"))
				Dim value As String = TryCast(openWithKey.GetValue(progId, Nothing), String)

				If value Is Nothing Then
					registered = False
				Else
					registered = True
				End If
			Finally
				' Let the user know
				If Not registered Then
					td = New TaskDialog()

					td.Text = "File type is not registered"
					td.InstructionText = "This demo application needs to register .txt files as associated files to properly execute the Taskbar related features."
					td.Icon = TaskDialogStandardIcon.Information
					td.Cancelable = True

					Dim button1 As New TaskDialogCommandLink("registerButton", "Register file type for this application", "Register .txt files with this application to run this demo application correctly.")

					AddHandler button1.Click, AddressOf button1_Click
                    ' Show UAC shield as this task requires elevation
                    button1.UseElevationIcon = True

					td.Controls.Add(button1)

					Dim tdr As TaskDialogResult = td.Show()
				End If
			End Try
		End Sub

		Private Sub button1_Click(ByVal sender As Object, ByVal e As EventArgs)
			registerFileTypeToolStripMenuItem_Click(Nothing, EventArgs.Empty)
			td.Close()
		End Sub

		#End Region

		#Region "File Registration Helpers"

		Private Sub registerFileTypeToolStripMenuItem_Click(ByVal sender As Object, ByVal e As EventArgs) Handles registerFileTypeToolStripMenuItem1.Click, registerFileTypeToolStripMenuItem.Click
			RegistrationHelper.RegisterFileAssociations(progId, False, appId, executablePath & " /doc %1", ".txt")
		End Sub

		Private Sub unregisterFileTypeToolStripMenuItem_Click(ByVal sender As Object, ByVal e As EventArgs) Handles unregisterFileTypeToolStripMenuItem1.Click, unregisterFileTypeToolStripMenuItem.Click
			RegistrationHelper.UnregisterFileAssociations(progId, False, appId, executablePath & " /doc %1", ".txt")
		End Sub

		#End Region

		#Region "Menu Open/Close"

		Private Sub openToolStripMenuItem_Click(ByVal sender As Object, ByVal e As EventArgs) Handles openToolStripMenuItem1.Click, openToolStripMenuItem.Click
			Dim dialog As New CommonOpenFileDialog()
			dialog.Title = "Select a text document to load"
			dialog.Filters.Add(New CommonFileDialogFilter("Text files (*.txt)", "*.txt"))

			Dim result As CommonFileDialogResult = dialog.ShowDialog()

			If result = CommonFileDialogResult.OK Then
				ReportUsage(dialog.FileName)
				Process.Start(executablePath, "/doc " & dialog.FileName)
			End If
		End Sub

		Private Sub saveToolStripMenuItem_Click(ByVal sender As Object, ByVal e As EventArgs) Handles saveToolStripMenuItem1.Click, saveToolStripMenuItem.Click
			Dim dialog As New CommonSaveFileDialog()
			dialog.Title = "Select where to save your file"
			dialog.Filters.Add(New CommonFileDialogFilter("Text files (*.txt)", "*.txt"))

			Dim result As CommonFileDialogResult = dialog.ShowDialog()

			If result = CommonFileDialogResult.OK Then
				ReportUsage(dialog.FileName)
			End If
		End Sub

		Private Sub ReportUsage(ByVal fileName As String)
			' Report file usage to shell.  Note: The dialog box automatically
			' reports usage to shell, but it's still recommeneded that the user
			' explicitly calls AddToRecent. Shell will automatically handle
			' duplicate additions.
			jumpList.AddToRecent(fileName)

			UpdateStatusBar("File added to recent documents")
		End Sub

		#End Region ';

		#Region "Known Categories"

		Private Sub comboBoxKnownCategoryType_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) Handles comboBoxKnownCategoryType.SelectedIndexChanged
			Select Case TryCast(comboBoxKnownCategoryType.SelectedItem, String)
				Case "None"
					jumpList.KnownCategoryToDisplay = JumpListKnownCategoryType.Neither
				Case "Recent"
					jumpList.KnownCategoryToDisplay = JumpListKnownCategoryType.Recent
				Case "Frequent"
					jumpList.KnownCategoryToDisplay = JumpListKnownCategoryType.Frequent
			End Select
		End Sub

		#End Region

		#Region "Custom Categories"

		Private category1ItemsCount As Integer = 0
		Private category2ItemsCount As Integer = 0

		Private Sub buttonCategoryOneAddLink_Click(ByVal sender As Object, ByVal e As EventArgs) Handles buttonCategoryOneAddLink.Click
			category1ItemsCount += 1

			' Specify path for shell item
			Dim path As String = String.Format("{0}\test{1}.txt", executableFolder, category1ItemsCount)

			' Make sure this file exists
			EnsureFile(path)

			' Add shell item to custom category
			category1.AddJumpListItems(New JumpListItem(path))

			' Update status
            UpdateStatusBar(System.IO.Path.GetFileName(path) & " added to 'Custom Category 1'")
		End Sub

		Private Sub buttonCategoryTwoAddLink_Click(ByVal sender As Object, ByVal e As EventArgs) Handles buttonCategoryTwoAddLink.Click
			category2ItemsCount += 1

			' Specify path for file
			Dim path As String = String.Format("{0}\test{1}.txt", executableFolder, category2ItemsCount)

			' Make sure this file exists
			EnsureFile(path)

			' Add jump list item to custom category
			category2.AddJumpListItems(New JumpListItem(path))

			' Update status
            UpdateStatusBar(System.IO.Path.GetFileName(path) & " added to 'Custom Category 2'")
		End Sub

		Private Sub EnsureFile(ByVal path As String)
			If File.Exists(path) Then
				Return
			End If

			' Simply create an empty file with the specified path
			Dim fileStream As FileStream = File.Create(path)
			fileStream.Close()
		End Sub

		Private Sub buttonUserTasksAddTasks_Click(ByVal sender As Object, ByVal e As EventArgs) Handles buttonUserTasksAddTasks.Click
			' Path to Windows system folder
			Dim systemFolder As String = Environment.GetFolderPath(Environment.SpecialFolder.System)

			' Add our user tasks
			jumpList.AddUserTasks(New JumpListLink(Path.Combine(systemFolder, "notepad.exe"), "Open Notepad") With {.IconReference = New IconReference(Path.Combine(systemFolder, "notepad.exe"), 0)})

			jumpList.AddUserTasks(New JumpListLink(Path.Combine(systemFolder, "mspaint.exe"), "Open Paint") With {.IconReference = New IconReference(Path.Combine(systemFolder, "mspaint.exe"), 0)})

			jumpList.AddUserTasks(New JumpListSeparator())

			jumpList.AddUserTasks(New JumpListLink(Path.Combine(systemFolder, "calc.exe"), "Open Calculator") With {.IconReference = New IconReference(Path.Combine(systemFolder, "calc.exe"), 0)})

			' Update status
			UpdateStatusBar("Three user tasks added to jump list")
		End Sub

		Private Sub buttonCategoryOneRename_Click(ByVal sender As Object, ByVal e As EventArgs) Handles buttonCategoryOneRename.Click
			category1.Name = "Updated Category Name"
		End Sub

		#End Region

		#Region "Progress Bar"

		Private Sub trackBar1_Scroll(ByVal sender As Object, ByVal e As EventArgs) Handles trackBar1.Scroll
			' When the user changes the trackBar value,
			' update the progress bar in our UI as well as Taskbar
			progressBar1.Value = trackBar1.Value

			TaskbarManager.Instance.SetProgressValue(trackBar1.Value, 100)
		End Sub


		Private Sub comboBoxProgressBarStates_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) Handles comboBoxProgressBarStates.SelectedIndexChanged
			' Update the status of the taskbar progress bar

			Dim state As TaskbarProgressBarState = CType(System.Enum.Parse(GetType(TaskbarProgressBarState), CStr(comboBoxProgressBarStates.SelectedItem)), TaskbarProgressBarState)

			windowsTaskbar.SetProgressState(state)

			' Update the application progress bar,
			' as well disable the trackbar in some cases
			Select Case state
				Case TaskbarProgressBarState.Normal
					If trackBar1.Value = 0 Then
						trackBar1.Value = 20
						progressBar1.Value = trackBar1.Value
					End If

					progressBar1.Style = ProgressBarStyle.Continuous
					windowsTaskbar.SetProgressValue(trackBar1.Value, 100)
					trackBar1.Enabled = True
				Case TaskbarProgressBarState.Paused
					If trackBar1.Value = 0 Then
						trackBar1.Value = 20
						progressBar1.Value = trackBar1.Value
					End If

					progressBar1.Style = ProgressBarStyle.Continuous
					windowsTaskbar.SetProgressValue(trackBar1.Value, 100)
					trackBar1.Enabled = True
				Case TaskbarProgressBarState.Error
					If trackBar1.Value = 0 Then
						trackBar1.Value = 20
						progressBar1.Value = trackBar1.Value
					End If

					progressBar1.Style = ProgressBarStyle.Continuous
					windowsTaskbar.SetProgressValue(trackBar1.Value, 100)
					trackBar1.Enabled = True
				Case TaskbarProgressBarState.Indeterminate
					progressBar1.Style = ProgressBarStyle.Marquee
					progressBar1.MarqueeAnimationSpeed = 30
					trackBar1.Enabled = False
				Case TaskbarProgressBarState.NoProgress
					progressBar1.Value = 0
					trackBar1.Value = 0
					progressBar1.Style = ProgressBarStyle.Continuous
					trackBar1.Enabled = False
			End Select
		End Sub

		#End Region ';

		#Region "Icon Overlay"

        Private Sub HighlightOverlaySelection(ByVal ctlOverlay As Control)
            TaskbarDemoMainForm.CheckOverlaySelection(ctlOverlay, labelNoIconOverlay)
            TaskbarDemoMainForm.CheckOverlaySelection(ctlOverlay, pictureIconOverlay1)
            TaskbarDemoMainForm.CheckOverlaySelection(ctlOverlay, pictureIconOverlay2)
            TaskbarDemoMainForm.CheckOverlaySelection(ctlOverlay, pictureIconOverlay3)
        End Sub

        Friend Shared Sub CheckOverlaySelection(ByVal ctlOverlay As Control, ByVal ctlCheck As Label)
            ctlCheck.BorderStyle = If(ctlCheck Is ctlOverlay, BorderStyle.Fixed3D, BorderStyle.None)
        End Sub

        Friend Shared Sub CheckOverlaySelection(ByVal ctlOverlay As Control, ByVal ctlCheck As PictureBox)
            ctlCheck.BorderStyle = If(ctlCheck Is ctlOverlay, BorderStyle.Fixed3D, BorderStyle.None)
        End Sub

        Private Sub labelNoIconOverlay_Click(ByVal sender As Object, ByVal e As EventArgs) Handles labelNoIconOverlay.Click
            windowsTaskbar.SetOverlayIcon(Me.Handle, Nothing, Nothing)

            HighlightOverlaySelection(labelNoIconOverlay)
        End Sub

		Private Sub pictureIconOverlay1_Click(ByVal sender As Object, ByVal e As EventArgs) Handles pictureIconOverlay1.Click
			windowsTaskbar.SetOverlayIcon(Me.Handle, My.Resources.Green, "Green")

            HighlightOverlaySelection(pictureIconOverlay1)
        End Sub

		Private Sub pictureIconOverlay2_Click(ByVal sender As Object, ByVal e As EventArgs) Handles pictureIconOverlay2.Click
			windowsTaskbar.SetOverlayIcon(Me.Handle, My.Resources.Yellow, "Yellow")

            HighlightOverlaySelection(pictureIconOverlay2)
        End Sub

		Private Sub pictureIconOverlay3_Click(ByVal sender As Object, ByVal e As EventArgs) Handles pictureIconOverlay3.Click
			windowsTaskbar.SetOverlayIcon(Me.Handle, My.Resources.Red, "Red")

            HighlightOverlaySelection(pictureIconOverlay3)
        End Sub

		#End Region

		Private Sub UpdateStatusBar(ByVal status As String)
			toolStripStatusLabel1.Text = status
		End Sub

		Private Sub numericUpDownKnownCategoryLocation_ValueChanged(ByVal sender As Object, ByVal e As EventArgs) Handles numericUpDownKnownCategoryLocation.ValueChanged
			jumpList.KnownCategoryOrdinalPosition = Convert.ToInt32(numericUpDownKnownCategoryLocation.Value)
		End Sub

		Private Sub buttonRefreshTaskbarList_Click(ByVal sender As Object, ByVal e As EventArgs) Handles buttonRefreshTaskbarList.Click
			jumpList.Refresh()
		End Sub

		Private Sub newToolStripMenuItem_Click(ByVal sender As Object, ByVal e As EventArgs) Handles newToolStripMenuItem.Click
			childCount += 1
			Dim childWindow As New ChildDocument(childCount)
			childWindow.Text = String.Format("Child Document Window ({0})", childCount)
			childWindow.Show()
		End Sub

	End Class
End Namespace