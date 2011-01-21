'Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports Microsoft.Win32

Namespace TaskbarDemo
	Friend Class RegistrationHelperMain
		Shared Sub Main(ByVal args() As String)
			If args.Length < 6 Then
				Console.WriteLine("Usage: <ProgId> <Register in HKCU: true|false> <AppId> <OpenWithSwitch> <Unregister: true|false> <Ext1> [Ext2 [Ext3] ...]")
				Console.ReadLine()
				Return
			End If
			Try

				Dim progId As String = args(0)
				Dim registerInHKCU As Boolean = Boolean.Parse(args(1))
				Dim appId As String = args(2)
				Dim openWith As String = args(3)
				Dim unregister As Boolean = Boolean.Parse(args(4))

				Dim associationsToRegister() As String = args.Skip(5).ToArray()

				If registerInHKCU Then
					classesRoot = Registry.CurrentUser.OpenSubKey("Software\Classes")
				Else
					classesRoot = Registry.ClassesRoot
				End If

                'First of all, unregister:
                For Each assoc In associationsToRegister
                    UnregisterFileAssociation(progId, assoc)
                Next

                If (Not unregister) Then
                    RegisterProgId(progId, appId, openWith)
                    For Each assoc In associationsToRegister
                        RegisterFileAssociation(progId, assoc)
                    Next
                End If
            Catch e As Exception
                Console.WriteLine(e)
                Console.ReadLine()
			End Try
		End Sub

		Private Shared classesRoot As RegistryKey

		Private Shared Sub RegisterProgId(ByVal progId As String, ByVal appId As String, ByVal openWith As String)
			Dim progIdKey As RegistryKey = classesRoot.CreateSubKey(progId)
			progIdKey.SetValue("FriendlyTypeName", "@shell32.dll,-8975")
			progIdKey.SetValue("DefaultIcon", "@shell32.dll,-47")
			progIdKey.SetValue("CurVer", progId)
			progIdKey.SetValue("AppUserModelID", appId)
			Dim shell As RegistryKey = progIdKey.CreateSubKey("shell")
			shell.SetValue(String.Empty, "Open")
			shell = shell.CreateSubKey("Open")
			shell = shell.CreateSubKey("Command")
			shell.SetValue(String.Empty, openWith)

			shell.Close()
			progIdKey.Close()
		End Sub
		Private Shared Sub UnregisterProgId(ByVal progId As String)
			Try
				classesRoot.DeleteSubKeyTree(progId)
			Catch
			End Try
		End Sub
		Private Shared Sub RegisterFileAssociation(ByVal progId As String, ByVal extension As String)

			Dim openWithKey As RegistryKey = classesRoot.CreateSubKey(Path.Combine(extension, "OpenWithProgIds"))
			openWithKey.SetValue(progId, String.Empty)
			openWithKey.Close()
		End Sub
		Private Shared Sub UnregisterFileAssociation(ByVal progId As String, ByVal extension As String)
			Try
				Dim openWithKey As RegistryKey = classesRoot.CreateSubKey(Path.Combine(extension, "OpenWithProgIds"))
				openWithKey.DeleteValue(progId)
				openWithKey.Close()
			Catch e As Exception
				Debug.WriteLine("Error while unregistering file association: " & e.Message)
			End Try
		End Sub
	End Class
End Namespace
