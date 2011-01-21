'Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Windows.Data

Namespace Microsoft.WindowsAPICodePack.Samples.PowerMgmtDemoApp
	<ValueConversion(GetType(Boolean), GetType(String))> _
	Public Class YesNoConverter
		Implements IValueConverter
		#Region "IValueConverter Members"

		Public Function Convert(ByVal value As Object, ByVal targetType As Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements IValueConverter.Convert
			If targetType IsNot GetType(String) Then
				Return Nothing
			End If

			Dim what As Boolean = CBool(value)
			If what = True Then
				Return "Yes"
			Else
				Return "No"
			End If
		End Function

		' We only support one way binding
		Public Function ConvertBack(ByVal value As Object, ByVal targetType As Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements IValueConverter.ConvertBack
			Throw New NotImplementedException()
		End Function

		#End Region
	End Class
End Namespace
