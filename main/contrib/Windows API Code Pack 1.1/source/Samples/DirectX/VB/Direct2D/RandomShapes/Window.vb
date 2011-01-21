' Copyright (c) Microsoft Corporation.  All rights reserved.

Imports Microsoft.VisualBasic
Imports System

Imports System.Windows.Forms

Namespace RandomShapes
	Partial Public Class Window
		Inherits Form
		Public Sub New()
			InitializeComponent()
		End Sub

		Private Sub Window_Load(ByVal sender As Object, ByVal e As EventArgs) Handles MyBase.Load
			d2DShapesControlWithButtons1.Initialize()
		End Sub
	End Class
End Namespace
