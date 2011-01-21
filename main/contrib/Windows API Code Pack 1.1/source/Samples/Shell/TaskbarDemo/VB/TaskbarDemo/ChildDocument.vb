' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.IO
Imports System.Windows.Forms
Imports Microsoft.WindowsAPICodePack.Shell
Imports Microsoft.WindowsAPICodePack.Taskbar
Imports System.Reflection

Namespace TaskbarDemo
	Partial Public Class ChildDocument
		Inherits Form
		' Keep a reference to the Taskbar instance
		Private windowsTaskbar As TaskbarManager = TaskbarManager.Instance

		Private childWindowJumpList As JumpList
		Private childWindowAppId As String

		Public Sub New(ByVal count As Integer)
			childWindowAppId = "TaskbarDemo.ChildWindow" & count

			InitializeComponent()

			' Progress Bar
			For Each state As String In System.Enum.GetNames(GetType(TaskbarProgressBarState))
				comboBoxProgressBarStates.Items.Add(state)
			Next state

			'
			comboBoxProgressBarStates.SelectedItem = "NoProgress"

            AddHandler Shown, AddressOf ChildDocument_Shown

            HighlightOverlaySelection(labelNoIconOverlay)
		End Sub

		Private Sub ChildDocument_Shown(ByVal sender As Object, ByVal e As EventArgs)
			' Set our default
			windowsTaskbar.SetProgressState(TaskbarProgressBarState.NoProgress, Me.Handle)
		End Sub

		#Region "Progress Bar"

		Private Sub trackBar1_Scroll(ByVal sender As Object, ByVal e As EventArgs) Handles trackBar1.Scroll
			' When the user changes the trackBar value,
			' update the progress bar in our UI as well as Taskbar
			progressBar1.Value = trackBar1.Value

			windowsTaskbar.SetProgressValue(trackBar1.Value, 100, Me.Handle)
		End Sub


		Private Sub comboBoxProgressBarStates_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) Handles comboBoxProgressBarStates.SelectedIndexChanged
			' Update the status of the taskbar progress bar

			Dim state As TaskbarProgressBarState = CType(System.Enum.Parse(GetType(TaskbarProgressBarState), CStr(comboBoxProgressBarStates.SelectedItem)), TaskbarProgressBarState)

			windowsTaskbar.SetProgressState(state, Me.Handle)

			' Update the application progress bar,
			' as well disable the trackbar in some cases
			Select Case state
				Case TaskbarProgressBarState.Normal
					If trackBar1.Value = 0 Then
						trackBar1.Value = 20
						progressBar1.Value = trackBar1.Value
					End If

					progressBar1.Style = ProgressBarStyle.Continuous
					windowsTaskbar.SetProgressValue(trackBar1.Value, 100, Me.Handle)
					trackBar1.Enabled = True
				Case TaskbarProgressBarState.Paused
					If trackBar1.Value = 0 Then
						trackBar1.Value = 20
						progressBar1.Value = trackBar1.Value
					End If

					progressBar1.Style = ProgressBarStyle.Continuous
					windowsTaskbar.SetProgressValue(trackBar1.Value, 100, Me.Handle)
					trackBar1.Enabled = True
				Case TaskbarProgressBarState.Error
					If trackBar1.Value = 0 Then
						trackBar1.Value = 20
						progressBar1.Value = trackBar1.Value
					End If

					progressBar1.Style = ProgressBarStyle.Continuous
					windowsTaskbar.SetProgressValue(trackBar1.Value, 100, Me.Handle)
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

		#End Region

		#Region "Icon Overlay"

        Private Sub HighlightOverlaySelection(ByVal ctlOverlay As Control)
            TaskbarDemoMainForm.CheckOverlaySelection(ctlOverlay, labelNoIconOverlay)
            TaskbarDemoMainForm.CheckOverlaySelection(ctlOverlay, pictureIconOverlay1)
            TaskbarDemoMainForm.CheckOverlaySelection(ctlOverlay, pictureIconOverlay2)
            TaskbarDemoMainForm.CheckOverlaySelection(ctlOverlay, pictureIconOverlay3)
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

		Private Sub buttonRefreshTaskbarList_Click(ByVal sender As Object, ByVal e As EventArgs) Handles buttonRefreshTaskbarList.Click
            ' Start from an empty list for user tasks
            childWindowJumpList.ClearAllUserTasks()

            ' Path to Windows system folder
            Dim systemFolder As String = Environment.GetFolderPath(Environment.SpecialFolder.System)

            ' Path to the Program Files folder
            Dim programFilesFolder As String = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)

            ' Path to Windows folder
            Dim windowsFolder As String = Environment.GetEnvironmentVariable("windir")

            For Each item As Object In listBox1.SelectedItems
                Select Case item.ToString()
                    Case "Notepad"
                        childWindowJumpList.AddUserTasks( _
                            New JumpListLink(Path.Combine(systemFolder, "notepad.exe"), "Open Notepad") With _
                            { _
                                .IconReference = New IconReference(Path.Combine(systemFolder, "notepad.exe"), 0) _
                            })
                    Case "Calculator"
                        childWindowJumpList.AddUserTasks( _
                            New JumpListLink(Path.Combine(systemFolder, "calc.exe"), "Open Calculator") With _
                            { _
                                .IconReference = New IconReference(Path.Combine(systemFolder, "calc.exe"), 0) _
                            })
                    Case "Paint"
                        childWindowJumpList.AddUserTasks( _
                            New JumpListLink(Path.Combine(systemFolder, "mspaint.exe"), "Open Paint") With _
                            { _
                                .IconReference = New IconReference(Path.Combine(systemFolder, "mspaint.exe"), 0) _
                            })
                    Case "WordPad"
                        childWindowJumpList.AddUserTasks( _
                            New JumpListLink(Path.Combine(programFilesFolder, "Windows NT\Accessories\wordpad.exe"), "Open WordPad") With _
                            { _
                                .IconReference = New IconReference(Path.Combine(programFilesFolder, "Windows NT\Accessories\wordpad.exe"), 0) _
                            })
                    Case "Windows Explorer"
                        childWindowJumpList.AddUserTasks( _
                            New JumpListLink(Path.Combine(windowsFolder, "explorer.exe"), "Open Windows Explorer") With _
                            { _
                                .IconReference = New IconReference(Path.Combine(windowsFolder, "explorer.exe"), 0) _
                            })
                    Case "Internet Explorer"
                        childWindowJumpList.AddUserTasks( _
                            New JumpListLink(Path.Combine(programFilesFolder, "Internet Explorer\iexplore.exe"), "Open Internet Explorer") With _
                            { _
                                .IconReference = New IconReference(Path.Combine(programFilesFolder, "Internet Explorer\iexplore.exe"), 0) _
                            })
                    Case "Control Panel"
                        childWindowJumpList.AddUserTasks( _
                            New JumpListLink((CType(KnownFolders.ControlPanel, ShellObject)).ParsingName, "Open Control Panel") With _
                            { _
                                .IconReference = New IconReference(Path.Combine(windowsFolder, "explorer.exe"), 0) _
                            })
                    Case "Documents Library"
                        If ShellLibrary.IsPlatformSupported Then
                            childWindowJumpList.AddUserTasks( _
                                New JumpListLink(KnownFolders.DocumentsLibrary.Path, "Open Documents Library") With _
                                { _
                                    .IconReference = New IconReference(Path.Combine(windowsFolder, "explorer.exe"), 0) _
                                })
                        End If
                End Select
            Next item

            childWindowJumpList.Refresh()
		End Sub

		Private Sub button1_Click(ByVal sender As Object, ByVal e As EventArgs) Handles button1.Click
			childWindowJumpList = JumpList.CreateJumpListForIndividualWindow(childWindowAppId, Me.Handle)

			CType(sender, Button).Enabled = False
			groupBoxCustomCategories.Enabled = True
			buttonRefreshTaskbarList.Enabled = True
		End Sub
    End Class
End Namespace
