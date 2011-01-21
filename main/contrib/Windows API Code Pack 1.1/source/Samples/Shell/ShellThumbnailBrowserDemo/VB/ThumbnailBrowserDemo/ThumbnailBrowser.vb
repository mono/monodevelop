'Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Windows.Forms
Imports Microsoft.WindowsAPICodePack.Shell
Imports Microsoft.WindowsAPICodePack.Dialogs
Imports Microsoft.WindowsAPICodePack.Controls

Namespace Microsoft.WindowsAPICodePack.Samples.ThumbnailBrowserDemo
	Partial Public Class ThumbnailBrowser
		Inherits Form
		''' <summary>
		''' Different views the picture browser supports
		''' </summary>
		Private Enum Views
			Small
			Medium
			Large
			ExtraLarge
		End Enum

		''' <summary>
		''' Preview mode (thumbnails or icons)
		''' </summary>
		Private Enum Mode
			ThumbnailOrIcon
			ThumbnailOnly
			IconOnly
		End Enum

		''' <summary>
		''' Our current view (defaults to Views.Large)
		''' </summary>
		Private currentView As Views = Views.Large

		''' <summary>
		''' Our current mode (defaults to Thumbnail view)
		''' </summary>
		Private currentMode As Mode = Mode.ThumbnailOrIcon

		''' <summary>
		''' Our current ShellObject.
		''' </summary>
		Private currentItem As ShellObject = Nothing

		''' <summary>
		''' If the user checks the "do not show.." checkbox, then don't display
		''' the error dialog again.
		''' </summary>
		Private showErrorTaskDialog As Boolean = True

		''' <summary>
		''' This is the state what we should be doing if the user gets the error.
		''' By default change the mode.
		''' </summary>
		Private onErrorChangeMode As Boolean = True

		''' <summary>
		''' Task dialog to be shown to the user when error occurs.
		''' </summary>
		Private td As TaskDialog = Nothing

		Public Sub New()
			InitializeComponent()

			' Set some ExplorerBrowser properties
			explorerBrowser1.ContentOptions.SingleSelection = True
			explorerBrowser1.ContentOptions.ViewMode = ExplorerBrowserViewMode.List
			explorerBrowser1.NavigationOptions.PaneVisibility.Navigation = PaneVisibilityState.Hide
			explorerBrowser1.NavigationOptions.PaneVisibility.CommandsView = PaneVisibilityState.Hide
			explorerBrowser1.NavigationOptions.PaneVisibility.CommandsOrganize = PaneVisibilityState.Hide
			explorerBrowser1.NavigationOptions.PaneVisibility.Commands = PaneVisibilityState.Hide

			' set our initial state CurrentView == large
			toolStripSplitButton1.Image = My.Resources.large
			smallToolStripMenuItem.Checked = False
			mediumToolStripMenuItem.Checked = False
			largeToolStripMenuItem.Checked = True
			extraLargeToolStripMenuItem.Checked = False

			'
			comboBox1.SelectedIndex = 0

			'
			AddHandler explorerBrowser1.SelectionChanged, AddressOf explorerBrowser1_SelectionChanged

			' Create our Task Dialog for displaying the error to the user
			' when they are asking for Thumbnail Only and the selected item doesn't have a thumbnail.
			td = New TaskDialog()
			td.OwnerWindowHandle = Me.Handle
			td.InstructionText = "Error displaying thumbnail"
			td.Text = "The selected item does not have a thumbnail and you have selected the viewing mode to be thumbnail only. Please select one of the following options:"
			td.StartupLocation = TaskDialogStartupLocation.CenterOwner
			td.Icon = TaskDialogStandardIcon.Error
			td.Cancelable = True
			td.FooterCheckBoxText = "Do not show this dialog again"
			td.FooterCheckBoxChecked = False

			Dim button1 As New TaskDialogCommandLink("changeModeButton", "Change mode to Thumbnail Or Icon", "Change the viewing mode to Thumbnail or Icon. If the selected item does not have a thumbnail, it's associated icon will be displayed.")
			AddHandler button1.Click, AddressOf button1_Click

			Dim button2 As New TaskDialogCommandLink("noChangeButton", "Keep the current mode", "Keep the currently selected mode (Thumbnail Only). If the current mode is Thumbnail Only and the selected item does not have a thumbnail, nothing will be shown in the preview panel.")
			AddHandler button2.Click, AddressOf button2_Click

			td.Controls.Add(button1)
			td.Controls.Add(button2)

		End Sub

		Private Sub button1_Click(ByVal sender As Object, ByVal e As EventArgs)
			onErrorChangeMode = True
			td.Close(TaskDialogResult.Ok)
		End Sub

		Private Sub button2_Click(ByVal sender As Object, ByVal e As EventArgs)
			onErrorChangeMode = False
			td.Close(TaskDialogResult.Ok)
		End Sub

		Private Sub explorerBrowser1_SelectionChanged(ByVal sender As Object, ByVal e As EventArgs)
			If Me.explorerBrowser1.SelectedItems IsNot Nothing AndAlso Me.explorerBrowser1.SelectedItems.Count = 1 Then
				' Set our new current item
				currentItem = explorerBrowser1.SelectedItems(0)

				' Update preview
				UpdatePreview()
			End If
		End Sub

		''' <summary>
		''' Updates the thumbnail preview for currently selected item and current view
		''' </summary>
		Private Sub UpdatePreview()
			If currentItem IsNot Nothing Then
				' Set the appropiate FormatOption
				If currentMode = Mode.ThumbnailOrIcon Then
                    currentItem.Thumbnail.FormatOption = ShellThumbnailFormatOption.Default
				ElseIf currentMode = Mode.ThumbnailOnly Then
                    currentItem.Thumbnail.FormatOption = ShellThumbnailFormatOption.ThumbnailOnly
				Else
                    currentItem.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly
				End If

				' Get the correct bitmap
				Try
					If currentView = Views.Small Then
						pictureBox1.Image = currentItem.Thumbnail.SmallBitmap
					ElseIf currentView = Views.Medium Then
						pictureBox1.Image = currentItem.Thumbnail.MediumBitmap
					ElseIf currentView = Views.Large Then
						pictureBox1.Image = currentItem.Thumbnail.LargeBitmap
					ElseIf currentView = Views.ExtraLarge Then
						pictureBox1.Image = currentItem.Thumbnail.ExtraLargeBitmap
					End If
				Catch e1 As NotSupportedException
					Dim tdThumbnailHandlerError As New TaskDialog()
					tdThumbnailHandlerError.Caption = "Error getting the thumbnail"
					tdThumbnailHandlerError.InstructionText = "The selected file does not have a valid thumbnail or thumbnail handler."
					tdThumbnailHandlerError.Icon = TaskDialogStandardIcon.Error
					tdThumbnailHandlerError.StandardButtons = TaskDialogStandardButtons.Ok
					tdThumbnailHandlerError.Show()
				Catch e2 As InvalidOperationException
					If currentMode = Mode.ThumbnailOnly Then
						' If we get an InvalidOperationException and our mode is Mode.ThumbnailOnly,
						' then we have a ShellItem that doesn't have a thumbnail (icon only).
						' Let the user know this and if they want, change the mode.
						If showErrorTaskDialog Then
							Dim tdr As TaskDialogResult = td.Show()

							showErrorTaskDialog = Not td.FooterCheckBoxChecked.Value
						End If

						' If the user picked the first option, change the mode...
						If onErrorChangeMode Then
							' Change the mode to ThumbnailOrIcon
							comboBox1.SelectedIndex = 0
							UpdatePreview()
						Else ' else, ignore and display nothing.
							pictureBox1.Image = Nothing
						End If
					Else
						pictureBox1.Image = Nothing
					End If
				End Try
			Else
				pictureBox1.Image = Nothing
			End If
		End Sub

		Private Sub browseLocationButton_Click(ByVal sender As Object, ByVal e As EventArgs) Handles browseLocationButton.Click
			' Create a new CommonOpenFileDialog to allow users to select a folder/library
			Dim cfd As New CommonOpenFileDialog()

			' Set options to allow libraries and non filesystem items to be selected
			cfd.IsFolderPicker = True
			cfd.AllowNonFileSystemItems = True

			' Show the dialog
			Dim result As CommonFileDialogResult = cfd.ShowDialog()

			' if the user didn't cancel
			If result = CommonFileDialogResult.OK Then
				' Update the location on the ExplorerBrowser
				Dim resultItem As ShellObject = cfd.FileAsShellObject
				explorerBrowser1.Navigate(resultItem)
			End If
		End Sub

		Private Sub toolStripSplitButton1_ButtonClick(ByVal sender As Object, ByVal e As EventArgs) Handles toolStripSplitButton1.ButtonClick
			ToggleViews()
		End Sub

		''' <summary>
		''' Toggle the different views for the thumbnail image.
		''' Includes: Small, Medium, Large (default), and Extra Large.
		''' </summary>
		Private Sub ToggleViews()
			' Toggle the views
			' Update our current view, as well as the image shown
			' on the "Views" menu.

			If currentView = Views.Small Then
				currentView = Views.Medium
				toolStripSplitButton1.Image = My.Resources.medium
				smallToolStripMenuItem.Checked = False
				mediumToolStripMenuItem.Checked = True
				largeToolStripMenuItem.Checked = False
				extraLargeToolStripMenuItem.Checked = False
			ElseIf currentView = Views.Medium Then
				currentView = Views.Large
				toolStripSplitButton1.Image = My.Resources.large
				smallToolStripMenuItem.Checked = False
				mediumToolStripMenuItem.Checked = False
				largeToolStripMenuItem.Checked = True
				extraLargeToolStripMenuItem.Checked = False
			ElseIf currentView = Views.Large Then
				currentView = Views.ExtraLarge
				toolStripSplitButton1.Image = My.Resources.extralarge
				smallToolStripMenuItem.Checked = False
				mediumToolStripMenuItem.Checked = False
				largeToolStripMenuItem.Checked = False
				extraLargeToolStripMenuItem.Checked = True
			ElseIf currentView = Views.ExtraLarge Then
				currentView = Views.Small
				toolStripSplitButton1.Image = My.Resources.small
				smallToolStripMenuItem.Checked = True
				mediumToolStripMenuItem.Checked = False
				largeToolStripMenuItem.Checked = False
				extraLargeToolStripMenuItem.Checked = False
			End If

			' Update the image
			UpdatePreview()
		End Sub

		Private Sub smallToolStripMenuItem_Click(ByVal sender As Object, ByVal e As EventArgs) Handles smallToolStripMenuItem.Click
			' Update current view
			currentView = Views.Small

			' Update the menu item states
			smallToolStripMenuItem.Checked = True
			mediumToolStripMenuItem.Checked = False
			largeToolStripMenuItem.Checked = False
			extraLargeToolStripMenuItem.Checked = False

			' Update the main splitbutton image
			toolStripSplitButton1.Image = My.Resources.small

			' Update the image
			UpdatePreview()
		End Sub

		Private Sub mediumToolStripMenuItem_Click(ByVal sender As Object, ByVal e As EventArgs) Handles mediumToolStripMenuItem.Click
			' Update current view
			currentView = Views.Medium

			' Update the menu item states
			smallToolStripMenuItem.Checked = False
			mediumToolStripMenuItem.Checked = True
			largeToolStripMenuItem.Checked = False
			extraLargeToolStripMenuItem.Checked = False

			' Update the main splitbutton image
			toolStripSplitButton1.Image = My.Resources.medium

			' Update the image
			UpdatePreview()
		End Sub

		Private Sub largeToolStripMenuItem_Click(ByVal sender As Object, ByVal e As EventArgs) Handles largeToolStripMenuItem.Click
			' Update current view
			currentView = Views.Large

			' Update the menu item states
			smallToolStripMenuItem.Checked = False
			mediumToolStripMenuItem.Checked = False
			largeToolStripMenuItem.Checked = True
			extraLargeToolStripMenuItem.Checked = False

			' Update the main splitbutton image
			toolStripSplitButton1.Image = My.Resources.large

			' Update the image
			UpdatePreview()
		End Sub

		Private Sub extraLargeToolStripMenuItem_Click(ByVal sender As Object, ByVal e As EventArgs) Handles extraLargeToolStripMenuItem.Click
			' Update current view
			currentView = Views.ExtraLarge

			' Update the menu item states
			smallToolStripMenuItem.Checked = False
			mediumToolStripMenuItem.Checked = False
			largeToolStripMenuItem.Checked = False
			extraLargeToolStripMenuItem.Checked = True

			' Update the main splitbutton image
			toolStripSplitButton1.Image = My.Resources.extralarge

			' Update the image
			UpdatePreview()

		End Sub

		Private Sub comboBox1_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) Handles comboBox1.SelectedIndexChanged
			If comboBox1.SelectedIndex = 0 Then
				currentMode = Mode.ThumbnailOrIcon
			ElseIf comboBox1.SelectedIndex = 1 Then
				currentMode = Mode.ThumbnailOnly
			Else
				currentMode = Mode.IconOnly
			End If

			UpdatePreview()
		End Sub
	End Class
End Namespace
