'Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Runtime.InteropServices
Imports System.Windows
Imports System.Windows.Interop
Imports System.Windows.Media

Namespace Microsoft.WindowsAPICodePack.Samples.SearchApp
	Public Class GlassHelper
        Public Structure Margins
            Public Sub New(ByVal t As Thickness)
                Left = CInt(Fix(t.Left))
                Right = CInt(Fix(t.Right))
                Top = CInt(Fix(t.Top))
                Bottom = CInt(Fix(t.Bottom))
            End Sub

            Public Left As Integer
            Public Right As Integer
            Public Top As Integer
            Public Bottom As Integer
        End Structure

        <DllImport("dwmapi.dll", PreserveSig:=False)> _
        Shared Sub DwmExtendFrameIntoClientArea(ByVal hwnd As IntPtr, ByRef pMargins As Margins)
        End Sub

		<DllImport("dwmapi.dll", PreserveSig := False)> _
		Shared Function DwmIsCompositionEnabled() As Boolean
		End Function

		Public Shared ReadOnly Property IsGlassEnabled() As Boolean
			Get
				Return DwmIsCompositionEnabled()
			End Get
		End Property

		Public Shared Function ExtendGlassFrame(ByVal window As Window, ByVal margin As Thickness) As Boolean
			If Not DwmIsCompositionEnabled() Then
				Return False
			End If

			Dim hwnd As IntPtr = New WindowInteropHelper(window).Handle
			If hwnd = IntPtr.Zero Then
				Throw New InvalidOperationException("The Window must be shown before extending glass.")
			End If

			' Set the background to transparent from both the WPF and Win32 perspectives
			Dim background As New SolidColorBrush(Colors.Red)
			background.Opacity = 0.5
			window.Background = Brushes.Transparent
			HwndSource.FromHwnd(hwnd).CompositionTarget.BackgroundColor = Colors.Transparent

			Dim margins As New Margins(margin)
			DwmExtendFrameIntoClientArea(hwnd, margins)
			Return True
		End Function

		Public Shared Sub DisableGlassFrame(ByVal window As Window1)
			Dim hwnd As IntPtr = New WindowInteropHelper(window).Handle
			If hwnd = IntPtr.Zero Then
				Throw New InvalidOperationException("The Window must be shown before extending glass.")
			End If

			HwndSource.FromHwnd(hwnd).CompositionTarget.BackgroundColor = Colors.White
		End Sub
	End Class
End Namespace
