'Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.IO
Imports System.Linq
Imports System.Windows.Forms
Imports Microsoft.WindowsAPICodePack.Shell
Imports Microsoft.WindowsAPICodePack.Taskbar
Imports Microsoft.WindowsAPICodePack.Dialogs

Namespace Microsoft.WindowsAPICodePack.Samples.ImageViewerDemoWinforms
	Partial Public Class Form1
		Inherits Form
		Private buttonPrevious As ThumbnailToolbarButton
		Private buttonNext As ThumbnailToolbarButton
		Private buttonFirst As ThumbnailToolbarButton
		Private buttonLast As ThumbnailToolbarButton
		Private picturesList As List(Of ListViewItem)
		Private imgListCount As Integer = 0
		Private imgList As ImageList = Nothing

		Public Sub New()
			InitializeComponent()
			listView1.MultiSelect = False

			InitListView()

			AddHandler Shown, AddressOf Form1_Shown

			'
			toolStrip1.ImageList = imageList1
			toolStrip1.ImageScalingSize = New Size(32, 32)
			toolStripButtonFirst.ImageIndex = 0
			toolStripButtonPrevious.ImageIndex = 1
			toolStripButtonNext.ImageIndex = 2
			toolStripButtonLast.ImageIndex = 3

		End Sub

		Private Sub Form1_Shown(ByVal sender As Object, ByVal e As System.EventArgs)
			AddHandler listView1.SelectedIndexChanged, AddressOf listView1_SelectedIndexChanged

			buttonFirst = New ThumbnailToolbarButton(My.Resources.first, "First Image")
			buttonFirst.Enabled = False
			AddHandler buttonFirst.Click, AddressOf buttonFirst_Click

			buttonPrevious = New ThumbnailToolbarButton(My.Resources.prevArrow, "Previous Image")
			buttonPrevious.Enabled = False
			AddHandler buttonPrevious.Click, AddressOf buttonPrevious_Click

			buttonNext = New ThumbnailToolbarButton(My.Resources.nextArrow, "Next Image")
			AddHandler buttonNext.Click, AddressOf buttonNext_Click

			buttonLast = New ThumbnailToolbarButton(My.Resources.last, "Last Image")
			AddHandler buttonLast.Click, AddressOf buttonLast_Click

			TaskbarManager.Instance.ThumbnailToolbars.AddButtons(Me.Handle, buttonFirst, buttonPrevious, buttonNext, buttonLast)

			If listView1.Items.Count > 0 Then
				listView1.Items(0).Selected = True
			End If

			'
			TaskbarManager.Instance.TabbedThumbnail.SetThumbnailClip(Me.Handle, New Rectangle(pictureBox1.Location, pictureBox1.Size))
		End Sub

		Private Sub buttonPrevious_Click(ByVal sender As Object, ByVal e As EventArgs) Handles toolStripButtonPrevious.Click
			Dim newIndex As Integer = listView1.SelectedIndices(0) - 1

			If newIndex > -1 Then
				listView1.Items(newIndex).Selected = True
				listView1.Items(newIndex).EnsureVisible()
			End If

			listView1.Focus()
		End Sub

		Private Sub buttonNext_Click(ByVal sender As Object, ByVal e As EventArgs) Handles toolStripButtonNext.Click
			Dim newIndex As Integer = listView1.SelectedIndices(0) + 1

			If newIndex < listView1.Items.Count Then
				listView1.Items(newIndex).Selected = True
				listView1.Items(newIndex).EnsureVisible()
			End If

			listView1.Focus()
		End Sub

		Private Sub buttonFirst_Click(ByVal sender As Object, ByVal e As EventArgs) Handles toolStripButtonFirst.Click
			listView1.Items(0).Selected = True
			listView1.Items(0).EnsureVisible()
			listView1.Focus()
		End Sub

		Private Sub buttonLast_Click(ByVal sender As Object, ByVal e As EventArgs) Handles toolStripButtonLast.Click
			listView1.Items(listView1.Items.Count - 1).Selected = True
			listView1.Items(listView1.Items.Count - 1).EnsureVisible()
			listView1.Focus()
		End Sub

		Private Sub listView1_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs)
			' Update the picture
			If listView1.SelectedItems.Count > 0 Then
				pictureBox1.Image = Image.FromFile((CType(listView1.SelectedItems(0), ListViewItem)).Tag.ToString())
			End If

			' Update the button states
			If listView1.SelectedIndices.Count > 0 AndAlso listView1.SelectedIndices(0) = 0 Then
				buttonFirst.Enabled = False
				toolStripButtonFirst.Enabled = False
				buttonPrevious.Enabled = False
				toolStripButtonPrevious.Enabled = False
			ElseIf listView1.SelectedIndices.Count > 0 AndAlso listView1.SelectedIndices(0) > 0 Then
				buttonFirst.Enabled = True
				toolStripButtonFirst.Enabled = True
				buttonPrevious.Enabled = True
				toolStripButtonPrevious.Enabled = True
			End If

			If listView1.SelectedIndices.Count > 0 AndAlso listView1.SelectedIndices(0) = listView1.Items.Count - 1 Then
				buttonLast.Enabled = False
				toolStripButtonLast.Enabled = False
				buttonNext.Enabled = False
				toolStripButtonNext.Enabled = False
			ElseIf listView1.SelectedIndices.Count > 0 AndAlso listView1.SelectedIndices(0) < listView1.Items.Count - 1 Then
				buttonLast.Enabled = True
				toolStripButtonLast.Enabled = True
				buttonNext.Enabled = True
				toolStripButtonNext.Enabled = True
			End If
		End Sub

		Private Sub InitListView()
			imgList = New ImageList()
            imgList.ImageSize = New Size(96, 96)
            imgList.ColorDepth = ColorDepth.Depth32Bit

			listView1.LargeImageList = imgList

			Dim pics As ShellContainer = CType(KnownFolders.Pictures, ShellContainer)

			If ShellLibrary.IsPlatformSupported Then
				pics = CType(KnownFolders.PicturesLibrary, ShellContainer)
			End If

			If picturesList Is Nothing Then
				picturesList = New List(Of ListViewItem)()
			Else
				picturesList.Clear()
			End If

			' Recursively get the pictures
			GetPictures(pics)

			If picturesList.Count = 0 Then
				If TypeOf pics Is ShellLibrary Then
					TaskDialog.Show("Please add some pictures to the library", "Pictures library is empty", "No pictures found")
				Else
					TaskDialog.Show("Please add some pictures to your pictures folder", "Pictures folder is empty", "No pictures found")
				End If
			End If

			listView1.Items.AddRange(picturesList.ToArray())
		End Sub

		Private Sub GetPictures(ByVal folder As ShellContainer)
			' Just for demo purposes, stop at 20 pics
			If picturesList.Count >= 20 Then
				Return
			End If

			' First get the pictures in this folder
			For Each sf As ShellFile In folder.OfType(Of ShellFile)()
				Dim ext As String = Path.GetExtension(sf.Path).ToLower()

				If ext = ".jpg" OrElse ext = ".jpeg" OrElse ext = ".png" OrElse ext = ".bmp" Then
					Dim item As New ListViewItem()
					item.Text = sf.Name
					item.ImageIndex = imgListCount
					item.Tag = sf.Path
					imgList.Images.Add(Image.FromFile(sf.Path))

					picturesList.Add(item)
					imgListCount += 1
				End If
			Next sf

			' Then recurse into each subfolder
			For Each subFolder As ShellContainer In folder.OfType(Of ShellContainer)()
				GetPictures(subFolder)
			Next subFolder
		End Sub

        Private Sub pictureBox1_SizeChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles pictureBox1.SizeChanged
            TaskbarManager.Instance.TabbedThumbnail.SetThumbnailClip(Me.Handle, New Rectangle(pictureBox1.Location, pictureBox1.Size))
        End Sub
    End Class
End Namespace
