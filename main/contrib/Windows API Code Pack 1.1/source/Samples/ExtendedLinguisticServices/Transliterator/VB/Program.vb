' Copyright (c) Microsoft Corporation.  All rights reserved.

Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Windows.Forms
Imports Microsoft.WindowsAPICodePack.ExtendedLinguisticServices

Namespace Transliterator
    Friend NotInheritable Class Program
        ''' <summary>
        ''' The main entry point for the application.
        ''' </summary>
        Private Sub New()
        End Sub
        <STAThread()> _
        Shared Sub Main()
            Application.EnableVisualStyles()
            Application.SetCompatibleTextRenderingDefault(False)

            If MappingService.IsPlatformSupported <> True Then
                MessageBox.Show("This demo requires to be run on Windows 7", "Demo needs Windows 7", MessageBoxButtons.OK, MessageBoxIcon.Error)
                System.Environment.Exit(0)
                Return
            End If


            Application.Run(New Transliterator())
        End Sub
    End Class
End Namespace
