'Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System.Diagnostics
Imports Microsoft.WindowsAPICodePack.Dialogs
Imports System.ComponentModel

Namespace TaskbarDemo
    ''' <summary>
    ''' Helper class for registering file associations.
    ''' </summary>
    Public NotInheritable Class RegistrationHelper
        Private Sub New()
        End Sub
        Private Shared Sub InternalRegisterFileAssociations(ByVal unregister As Boolean, ByVal progId As String, ByVal registerInHKCU As Boolean, ByVal appId As String, ByVal openWith As String, ByVal extensions() As String)
            Dim psi As New ProcessStartInfo("RegistrationHelper.exe")
            psi.Arguments = String.Format("{0} {1} {2} ""{3}"" {4} {5}", progId, registerInHKCU, appId, openWith, unregister, String.Join(" ", extensions))
            psi.UseShellExecute = True
            psi.Verb = "runas" 'Launch elevated
            psi.WindowStyle = ProcessWindowStyle.Hidden

            Try
                Process.Start(psi).WaitForExit()
                TaskDialog.Show("File associations were " & If(unregister, "un", "") & "registered")
            Catch e As Win32Exception
                If e.NativeErrorCode = 1223 Then ' 1223: The operation was canceled by the user.
                    TaskDialog.Show("The operation was canceled by the user.")
                End If
            End Try

        End Sub

        ''' <summary>
        ''' Registers file associations for an application.
        ''' </summary>
        ''' <param name="progId">The application's ProgID.</param>
        ''' <param name="registerInHKCU">Whether to register the
        ''' association per-user (in HKCU).  The only supported value
        ''' at this time is <b>false</b>.</param>
        ''' <param name="appId">The application's app-id.</param>
        ''' <param name="openWith">The command and arguments to be used
        ''' when opening a shortcut to a document.</param>
        ''' <param name="extensions">The extensions to register.</param>
        Public Shared Sub RegisterFileAssociations(ByVal progId As String, ByVal registerInHKCU As Boolean, ByVal appId As String, ByVal openWith As String, ByVal ParamArray extensions() As String)
            InternalRegisterFileAssociations(False, progId, registerInHKCU, appId, openWith, extensions)
        End Sub

        ''' <summary>
        ''' Unregisters file associations for an application.
        ''' </summary>
        ''' <param name="progId">The application's ProgID.</param>
        ''' <param name="registerInHKCU">Whether to register the
        ''' association per-user (in HKCU).  The only supported value
        ''' at this time is <b>false</b>.</param>
        ''' <param name="appId">The application's app-id.</param>
        ''' <param name="openWith">The command and arguments to be used
        ''' when opening a shortcut to a document.</param>
        ''' <param name="extensions">The extensions to unregister.</param>
        Public Shared Sub UnregisterFileAssociations(ByVal progId As String, ByVal registerInHKCU As Boolean, ByVal appId As String, ByVal openWith As String, ByVal ParamArray extensions() As String)
            InternalRegisterFileAssociations(True, progId, registerInHKCU, appId, openWith, extensions)
        End Sub
    End Class
End Namespace
