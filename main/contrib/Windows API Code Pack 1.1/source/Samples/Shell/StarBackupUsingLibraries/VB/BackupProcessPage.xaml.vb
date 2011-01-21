'Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System.Collections
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Threading
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Navigation
Imports Microsoft.WindowsAPICodePack.Shell

Namespace Microsoft.WindowsAPICodePack.Samples.StarBackupSample
	Partial Public Class BackupProcessPage
		Inherits PageFunction(Of WizardResult)
		Private backupList As IEnumerable
		Private bw As BackgroundWorker

		Public Sub New(ByVal list As IEnumerable)
			InitializeComponent()

			' The list of folders from the previous page
			backupList = list

			' Add the list of folders to our listbox. This won't actually start the backup
			UpdateList(backupList)

			' Create a BackgroundBorker thread to do the actual backup
			bw = New BackgroundWorker()
			bw.WorkerReportsProgress = True
			bw.WorkerSupportsCancellation = True
			AddHandler bw.ProgressChanged, AddressOf bw_ProgressChanged
			AddHandler bw.DoWork, AddressOf bw_DoWork
			AddHandler bw.RunWorkerCompleted, AddressOf bw_RunWorkerCompleted
			bw.RunWorkerAsync()
		End Sub

		''' <summary>
		''' Gets called when the actual backup process is completed.
		''' </summary>
		''' <param name="sender"></param>
		''' <param name="e"></param>
		Private Sub bw_RunWorkerCompleted(ByVal sender As Object, ByVal e As RunWorkerCompletedEventArgs)
			' if finished, change button text to done.
			If Not e.Cancelled Then
				buttonStopBackup.Content = "Backup Done!"
			Else
				buttonStopBackup.Content = "Backup Cancelled!"
			End If

			' Disable the start backup button as files are already backed up.
			buttonStopBackup.IsEnabled = False
		End Sub

		''' <summary>
		''' The method that does the real work. 
		''' </summary>
		''' <param name="sender"></param>
		''' <param name="e"></param>
		Private Sub bw_DoWork(ByVal sender As Object, ByVal e As DoWorkEventArgs)
			' Our counter for the folder that we are currently backing up
			Dim current As Integer = 1

			' Loop through all the items and back each folder up
			' Since the item is just a string path, we could create a ShellFolder (using ShellFolder.FromPath)
			' and then enumerate all the subitems in that folder.
			For Each lbi As ListBoxItem In listBox1.Items
				' If user has requested a cancel, set our event arg
				If (CType(sender, BackgroundWorker)).CancellationPending Then
					e.Cancel = True
				Else
					' Do a fake copy/backup of folder ...

					' Sleep two seconds
					Thread.Sleep(2000)

					' Once the copy has been done, report progress back to the Background Worker.
					' This could be used for a ProgressBar, or in our case, show "check" icon next
					' to each folder that was backed up.
                    CType(sender, BackgroundWorker).ReportProgress(CInt((current / listBox1.Items.Count) * 100), lbi)

					' Increment our counter for folders.
					current += 1
				End If
			Next lbi
		End Sub

		''' <summary>
		''' When each folder is backed up, some progress is reported. This method will get called each time.
		''' </summary>
		''' <param name="sender"></param>
		''' <param name="e"></param>
		Private Sub bw_ProgressChanged(ByVal sender As Object, ByVal e As ProgressChangedEventArgs)
			' The item we get passed in is the actual ListBoxItem 
			' (contains the StackPanel and label for the folder name)
			Dim lbi As ListBoxItem = TryCast(e.UserState, ListBoxItem)

			If lbi IsNot Nothing Then
				' Get the stack panel so we can get to it's contents
				Dim sp As StackPanel = TryCast(lbi.Content, StackPanel)

				If sp IsNot Nothing Then
					' Get the image control and set our checked state.
					Dim img As Image = TryCast(sp.Children(0), Image)
					If img IsNot Nothing Then
						img.Source = StarBackupHelper.ConvertGDI_To_WPF(My.Resources.Check)
					End If
				End If

				' Select the item and make sure its in view. This will give good feedback to the user
				' as we are going down the list and performing some operation on the items.
				listBox1.SelectedItem = lbi
				listBox1.ScrollIntoView(lbi)
			End If
		End Sub

		''' <summary>
		''' Goes through the list of folders to backup and adds each folder name (and an empty image control)
		''' to the listbox.
		''' </summary>
		''' <param name="backupList"></param>
		Private Sub UpdateList(ByVal backupList As IEnumerable)
			listBox1.Items.Clear()

			For Each item As Object In backupList
				' Start creating our listbox items..
				Dim lbi As New ListBoxItem()

				' Create a stackpanel to hold our image and textblock
				Dim sp As New StackPanel()
				sp.Orientation = Orientation.Horizontal
				Dim img As New Image()
				Dim tb As New TextBlock()
				tb.Margin = New Thickness(3)
				tb.Text = item.ToString()
				sp.Children.Add(img)
				sp.Children.Add(tb)

				' Set the StackPanel as the content of the listbox Item.
				lbi.Content = sp

				'
				listBox1.Items.Add(lbi)
			Next item
		End Sub

		''' <summary>
		''' 
		''' </summary>
		''' <param name="sender"></param>
		''' <param name="e"></param>
		Public Sub wizardPage_Return(ByVal sender As Object, ByVal e As ReturnEventArgs(Of WizardResult))
			CancelBackup()

			' If returning, wizard was completed (finished or canceled),
			' so continue returning to calling page
			OnReturn(e)
		End Sub

		''' <summary>
		''' 
		''' </summary>
		''' <param name="sender"></param>
		''' <param name="e"></param>
		Private Sub buttonStopBackup_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
			CancelBackup()
		End Sub

		''' <summary>
		''' Cancel the backup operation
		''' </summary>
		Private Sub CancelBackup()
			bw.CancelAsync()
			buttonStopBackup.IsEnabled = False
		End Sub
	End Class
End Namespace