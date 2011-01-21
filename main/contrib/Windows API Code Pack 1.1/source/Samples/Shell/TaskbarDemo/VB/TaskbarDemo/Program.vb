'Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Windows.Forms
Imports Microsoft.WindowsAPICodePack
Imports Microsoft.WindowsAPICodePack.Taskbar

Namespace TaskbarDemo
	Friend NotInheritable Class Program
		''' <summary>
		''' The main entry point for the application.
		''' </summary>
		Private Sub New()
		End Sub
		<STAThread> _
		Shared Sub Main()
			If Not TaskbarManager.IsPlatformSupported Then
				MessageBox.Show("This demo requires to be run on Windows 7", "Demo needs Windows 7", MessageBoxButtons.OK, MessageBoxIcon.Error)
				System.Environment.Exit(0)
				Return
			End If

			Application.EnableVisualStyles()
			Application.SetCompatibleTextRenderingDefault(False)
			Application.Run(New TaskbarDemoMainForm())
		End Sub
	End Class
End Namespace
