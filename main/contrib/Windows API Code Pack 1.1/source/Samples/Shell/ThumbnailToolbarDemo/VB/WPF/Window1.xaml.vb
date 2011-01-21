'Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Windows
Imports System.Windows.Interop
Imports System.Windows.Media
Imports Microsoft.WindowsAPICodePack.Dialogs
Imports Microsoft.WindowsAPICodePack.Shell
Imports Microsoft.WindowsAPICodePack.Taskbar

Namespace Microsoft.WindowsAPICodePack.Samples.ImageViewerDemo
	''' <summary>
	''' Interaction logic for Window1.xaml
	''' </summary>
	Partial Public Class Window1
		Inherits Window
		Private buttonPrevious As ThumbnailToolbarButton
		Private buttonNext As ThumbnailToolbarButton
		Private buttonFirst As ThumbnailToolbarButton
		Private buttonLast As ThumbnailToolbarButton
		Private picturesList As List(Of ShellFile)

		Public Sub New()
			If Not TaskbarManager.IsPlatformSupported Then
				MessageBox.Show("This demo application interacts with the Windows 7 Taskbar. The current operating system does not support this feature.")
				Application.Current.Shutdown()
			End If

			InitializeComponent()
			DataContext = Me

			AddHandler ImageList.SelectionChanged, AddressOf ImageList_SelectionChanged
			AddHandler Loaded, AddressOf Window1_Loaded

			' When the LayoutUpdated event is raised, we are sure that the picturebox is rendered
			' (i.e. we'll be able to get the height and width of that control)
			AddHandler pictureBox1.LayoutUpdated, AddressOf pictureBox1_LayoutUpdated
		End Sub

		Private Sub pictureBox1_LayoutUpdated(ByVal sender As Object, ByVal e As EventArgs)
			' On LayoutUpdated, get the offset of the pictureBox with repsect to its parent.
			' Form a clip rectangle (offset + size of the control) and pass it to Taskbar
			' for DWM to clip only the specific porition of the app window. 
			' This allows us to not include the "misc" controls from the app window - scroll bars, 
			' list view on the right, any toolbars, etc.

			' Get the offset for picturebox
			Dim v As Vector = VisualTreeHelper.GetOffset(pictureBox1)

			' Set the thumbnail clip
			TaskbarManager.Instance.TabbedThumbnail.SetThumbnailClip((New WindowInteropHelper(Me)).Handle, New System.Drawing.Rectangle(CInt(Fix(v.X)), CInt(Fix(v.Y)), CInt(Fix(pictureBox1.RenderSize.Width)), CInt(Fix(pictureBox1.RenderSize.Height))))
		End Sub

		Private Sub ImageList_SelectionChanged(ByVal sender As Object, ByVal e As System.Windows.Controls.SelectionChangedEventArgs)
			' Update the button states
			If ImageList.SelectedIndex = 0 Then
				buttonFirst.Enabled = False
				buttonPrevious.Enabled = False
			ElseIf ImageList.SelectedIndex > 0 Then
				buttonFirst.Enabled = True
				buttonPrevious.Enabled = True
			End If

			If ImageList.SelectedIndex = ImageList.Items.Count - 1 Then
				buttonLast.Enabled = False
				buttonNext.Enabled = False
			ElseIf ImageList.SelectedIndex < ImageList.Items.Count - 1 Then
				buttonLast.Enabled = True
				buttonNext.Enabled = True
			End If
		End Sub

		Private Sub Window1_Loaded(ByVal sender As Object, ByVal e As RoutedEventArgs)
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

			TaskbarManager.Instance.ThumbnailToolbars.AddButtons(New WindowInteropHelper(Me).Handle, buttonFirst, buttonPrevious, buttonNext, buttonLast)

			' Set our selection
			ImageList.SelectedIndex = 0
			ImageList.Focus()

			If ImageList.SelectedItem IsNot Nothing Then
				ImageList.ScrollIntoView(ImageList.SelectedItem)
			End If
		End Sub

		Private Sub buttonPrevious_Click(ByVal sender As Object, ByVal e As EventArgs)
			Dim newIndex As Integer = ImageList.SelectedIndex - 1

			If newIndex > -1 Then
				ImageList.SelectedIndex = newIndex
			End If

			ImageList.Focus()

			If ImageList.SelectedItem IsNot Nothing Then
				ImageList.ScrollIntoView(ImageList.SelectedItem)
			End If
		End Sub

		Private Sub buttonNext_Click(ByVal sender As Object, ByVal e As EventArgs)
			Dim newIndex As Integer = ImageList.SelectedIndex + 1

			If newIndex < ImageList.Items.Count Then
				ImageList.SelectedIndex = newIndex
			End If

			ImageList.Focus()

			If ImageList.SelectedItem IsNot Nothing Then
				ImageList.ScrollIntoView(ImageList.SelectedItem)
			End If
		End Sub

		Private Sub buttonFirst_Click(ByVal sender As Object, ByVal e As EventArgs)
			ImageList.SelectedIndex = 0
			ImageList.Focus()

			If ImageList.SelectedItem IsNot Nothing Then
				ImageList.ScrollIntoView(ImageList.SelectedItem)
			End If
		End Sub

		Private Sub buttonLast_Click(ByVal sender As Object, ByVal e As EventArgs)
			ImageList.SelectedIndex = ImageList.Items.Count - 1
			ImageList.Focus()

			If ImageList.SelectedItem IsNot Nothing Then
				ImageList.ScrollIntoView(ImageList.SelectedItem)
			End If
		End Sub

		Public Class MyImage
			Public Sub New(ByVal sourceImage As ImageSource, ByVal imageName As String)
				Image = sourceImage
				Name = imageName
			End Sub

			Public Overrides Function ToString() As String
				Return Name
			End Function

			Private privateImage As ImageSource
			Public Property Image() As ImageSource
				Get
					Return privateImage
				End Get
				Set(ByVal value As ImageSource)
					privateImage = value
				End Set
			End Property
			Private privateName As String
			Public Property Name() As String
				Get
					Return privateName
				End Get
				Set(ByVal value As String)
					privateName = value
				End Set
			End Property
		End Class

		Public ReadOnly Property AllImages() As List(Of ShellFile)
			Get
				Dim pics As ShellContainer = CType(KnownFolders.Pictures, ShellContainer)

				If ShellLibrary.IsPlatformSupported Then
					pics = CType(KnownFolders.PicturesLibrary, ShellContainer)
				End If

				If picturesList Is Nothing Then
					picturesList = New List(Of ShellFile)()
				Else
					picturesList.Clear()
				End If

				' Recursively get the pictures
				GetPictures(pics)

				If picturesList.Count = 0 Then
					If TypeOf pics Is ShellLibrary Then
						TaskDialog.Show("Pictures library is empty", "Please add some pictures to the library", "No pictures found")
					Else
						TaskDialog.Show("Pictures folder is empty", "Please add some pictures to your pictures folder", "No pictures found")
					End If
				End If

				Return picturesList
			End Get
		End Property

		Private Sub GetPictures(ByVal folder As ShellContainer)
			' Just for demo purposes, stop at 20 pics
			If picturesList.Count >= 20 Then
				Return
			End If

			' First get the pictures in this folder
			For Each sf As ShellFile In folder.OfType(Of ShellFile)()
				Dim ext As String = Path.GetExtension(sf.Path).ToLower()

				If ext = ".jpg" OrElse ext = ".jpeg" OrElse ext = ".png" OrElse ext = ".bmp" Then
					picturesList.Add(sf)
				End If
			Next sf

			' Then recurse into each subfolder
			For Each subFolder As ShellContainer In folder.OfType(Of ShellContainer)()
				GetPictures(subFolder)
			Next subFolder
		End Sub
	End Class
End Namespace