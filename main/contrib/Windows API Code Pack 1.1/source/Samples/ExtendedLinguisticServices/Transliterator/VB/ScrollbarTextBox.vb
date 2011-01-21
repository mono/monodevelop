' Copyright (c) Microsoft Corporation.  All rights reserved.

Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.Windows.Forms
Imports System.Diagnostics

Namespace Transliterator
	Public Class ScrollbarTextBox
		Inherits TextBox
		Private Const WM_HSCROLL As Integer = &H114
		Private Const WM_VSCROLL As Integer = &H115

		Public Event OnHorizontalScroll As ScrollEventHandler
		Public Event OnVerticalScroll As ScrollEventHandler

        Private Shared ScrollEventTypeArray() As ScrollEventType = {ScrollEventType.SmallDecrement, ScrollEventType.SmallIncrement, ScrollEventType.LargeDecrement, ScrollEventType.LargeIncrement, ScrollEventType.ThumbPosition, ScrollEventType.ThumbTrack, ScrollEventType.First, ScrollEventType.Last, ScrollEventType.EndScroll}

		Private Function GetEventType(ByVal wParam As UInteger) As ScrollEventType
            If wParam < ScrollEventTypeArray.Length Then
                Return ScrollEventTypeArray(CInt(wParam))
            Else
                Return ScrollEventType.EndScroll
            End If
		End Function

		Protected Overrides Sub WndProc(ByRef m As Message)
			MyBase.WndProc(m)

			If m.Msg = WM_HSCROLL Then
				If OnHorizontalScrollEvent IsNot Nothing Then
					Dim wParam As UInteger = CUInt(m.WParam.ToInt32())
                    RaiseEvent OnHorizontalScroll(Me, New ScrollEventArgs(GetEventType(CUInt(wParam And &HFFFF)), CInt(Fix(wParam >> 16))))
				End If
			ElseIf m.Msg = WM_VSCROLL Then
				If OnVerticalScrollEvent IsNot Nothing Then
					Dim wParam As UInteger = CUInt(m.WParam.ToInt32())
                    RaiseEvent OnVerticalScroll(Me, New ScrollEventArgs(GetEventType(CUInt(wParam And &HFFFF)), CInt(Fix(wParam >> 16))))
				End If
			End If
		End Sub
	End Class
End Namespace
