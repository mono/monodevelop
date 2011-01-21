'Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Windows
Imports System.Windows.Input
Imports Microsoft.WindowsAPICodePack.Shell

Namespace ShellObjectDragAndDropDemo
	''' <summary>
	''' WPF ShellObject Drag and Drop demonstration window
	''' </summary>
	Partial Public Class DragAndDropWindow
		Inherits Window
		#Region "implmentation data"
		Private dragStart As Point
		Private dataObject As DataObject = Nothing
		Private inDragDrop As Boolean = False
		#End Region

		#Region "construction"
		Public Sub New()
			InitializeComponent()
		End Sub
		#End Region

		#Region "message handlers"
		Private Sub Window_Loaded(ByVal sender As Object, ByVal e As RoutedEventArgs)
			AddHandler Drop, AddressOf OnDrop
			AddHandler DropSource.MouseLeftButtonDown, AddressOf OnMouseLeftButtonDown
			AddHandler DropSource.MouseLeftButtonUp, AddressOf OnMouseLeftButtonUp
			AddHandler DropDataList.MouseMove, AddressOf OnMouseMove
		End Sub

		Private Overloads Sub OnMouseMove(ByVal sender As Object, ByVal e As MouseEventArgs)
			If Not inDragDrop Then
				Dim currentPos As Point = e.GetPosition(Me)

				If (Math.Abs(currentPos.X - dragStart.X) > 5) OrElse (Math.Abs(currentPos.Y - dragStart.Y) > 5) Then
					If dataObject IsNot Nothing Then
						inDragDrop = True
						Dim de As DragDropEffects = DragDrop.DoDragDrop(Me.DropSource, dataObject, DragDropEffects.Copy)
						inDragDrop = False
						dataObject = Nothing
					End If
				End If
			End If
		End Sub

		Private Overloads Sub OnMouseLeftButtonUp(ByVal sender As Object, ByVal e As MouseButtonEventArgs)
			dataObject = Nothing
		End Sub

		Private Overloads Sub OnMouseLeftButtonDown(ByVal sender As Object, ByVal e As MouseButtonEventArgs)
			If Not IsMouseCaptured Then
				dragStart = e.GetPosition(Me)
				Dim collection As New ShellObjectCollection()
				Dim list As System.Collections.IList = If((DropDataList.SelectedItems.Count > 0), DropDataList.SelectedItems, DropDataList.Items)

				For Each shellObject As ShellObject In list
					collection.Add(shellObject)
				Next shellObject

				If collection.Count > 0 Then
					' This builds a DataObject from a "Shell IDList Array" formatted memory stream.
					' This allows drag/clipboard operations with non-file based ShellObjects (i.e., 
					' control panel, libraries, search query results)
					dataObject = New DataObject("Shell IDList Array", collection.BuildShellIDList())

					' Also build a file drop list
					Dim paths As New System.Collections.Specialized.StringCollection()
					For Each shellObject As ShellObject In collection
						If shellObject.IsFileSystemObject Then
							paths.Add(shellObject.ParsingName)
						End If
					Next shellObject
					If paths.Count > 0 Then
						dataObject.SetFileDropList(paths)
					End If
				End If
			End If
		End Sub

		Private Overloads Sub OnDrop(ByVal sender As Object, ByVal e As DragEventArgs)
			If Not inDragDrop Then
				Dim formats() As String = e.Data.GetFormats()
				For Each format As String In formats
					' Shell items are passed using the "Shell IDList Array" format. 
					If format = "Shell IDList Array" Then
						' Retrieve the ShellObjects from the data object
                        DropDataList.ItemsSource = ShellObjectCollection.FromDataObject(CType(e.Data, System.Runtime.InteropServices.ComTypes.IDataObject))

						e.Handled = True
						Return
					End If
				Next format
			End If

			e.Handled = False
		End Sub
		#End Region
	End Class
End Namespace
