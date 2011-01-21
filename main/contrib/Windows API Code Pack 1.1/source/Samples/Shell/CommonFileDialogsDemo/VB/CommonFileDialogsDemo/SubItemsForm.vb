' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Data
Imports System.Drawing
Imports System.Linq
Imports System.Text
Imports System.Windows.Forms

Namespace Microsoft.WindowsAPICodePack.Samples.ShellObjectCFDBrowser
	Partial Public Class SubItemsForm
		Inherits Form
		Private itemsCount As Integer = 0
		Public Sub New()
			InitializeComponent()

			listView1.LargeImageList = imageList1
		End Sub

		Public Sub AddItem(ByVal name As String, ByVal image As Image)
			imageList1.Images.Add(image)
			listView1.Items.Add(New ListViewItem(name, itemsCount))
			itemsCount += 1
		End Sub
	End Class
End Namespace
