'Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.Text

Namespace Microsoft.WindowsAPICodePack.Samples.StarBackupSample
	Public Class WizardReturnEventArgs
        Private result_Renamed As WizardResult
        Private data_Renamed As Object

		Public Sub New(ByVal result As WizardResult, ByVal data As Object)
			Me.result_Renamed = result
			Me.data_Renamed = data
		End Sub

		Public ReadOnly Property Result() As WizardResult
			Get
				Return Me.result_Renamed
			End Get
		End Property

		Public ReadOnly Property Data() As Object
			Get
				Return Me.data_Renamed
			End Get
		End Property
	End Class
End Namespace
