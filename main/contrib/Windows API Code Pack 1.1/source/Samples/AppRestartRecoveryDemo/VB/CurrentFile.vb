' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.IO

Namespace Microsoft.WindowsAPICodePack.Samples.AppRestartRecoveryDemo
	Public Class FileSettings
		Public Sub New()

		End Sub

		Private privateFilename As String
		Public Property Filename() As String
			Get
				Return privateFilename
			End Get
			Set(ByVal value As String)
				privateFilename = value
			End Set
		End Property

		Private privateContents As String
		Public Property Contents() As String
			Get
				Return privateContents
			End Get
			Set(ByVal value As String)
				privateContents = value
			End Set
		End Property

		Private privateIsDirty As Boolean
		Public Property IsDirty() As Boolean
			Get
				Return privateIsDirty
			End Get
			Set(ByVal value As Boolean)
				privateIsDirty = value
			End Set
		End Property

		Public Sub Load(ByVal path As String)
			Contents = File.ReadAllText(path)
			Filename = path
			IsDirty = False
		End Sub

		Public Sub Save(ByVal path As String)
			File.WriteAllText(path, Contents)
			Filename = path
			IsDirty = False
		End Sub
	End Class
End Namespace
