' Copyright (c) Microsoft Corporation.  All rights reserved.

Imports Microsoft.VisualBasic
Imports System
Imports System.Windows.Forms
Imports System.Globalization
Imports Microsoft.WindowsAPICodePack.DirectX.DirectWrite

Namespace D2DPaint
	Partial Public Class TextDialog
		Inherits Form
        Private parent_Renamed As Paint2DForm
		#Region "TextLayout"
        Private textLayout_Renamed As TextLayout
        Friend Property TextLayout() As TextLayout
            Get
                Dim textFormat As TextFormat = parent_Renamed.dwriteFactory.CreateTextFormat(fontFamilyCombo.Text, Single.Parse(sizeCombo.Text), CType((weightCombo.SelectedIndex + 1) * 100, FontWeight), CType(styleCombo.SelectedIndex, FontStyle), CType(stretchCombo.SelectedIndex, FontStretch), CultureInfo.CurrentUICulture)
                textLayout_Renamed = parent_Renamed.dwriteFactory.CreateTextLayout(textBox.Text, textFormat, 100, 100)
                If underLineCheckBox.Checked Then
                    textLayout_Renamed.SetUnderline(True, New TextRange(0, CUInt(textBox.Text.Length)))
                End If
                If strikethroughCheckBox.Checked Then
                    textLayout_Renamed.SetStrikethrough(True, New TextRange(0, CUInt(textBox.Text.Length)))
                End If
                Return textLayout_Renamed
            End Get
            Set(ByVal value As TextLayout)
                textLayout_Renamed = value
            End Set
        End Property
#End Region

		Public Sub New(ByVal parent As Paint2DForm)
			InitializeComponent()

			Me.parent_Renamed = parent
            If fontFamilyCombo Is Nothing Then
                fontFamilyCombo = New FontEnumComboBox
            End If

            If Not DesignMode Then
                fontFamilyCombo.Initialize()
            End If
            fontFamilyCombo.SelectedIndex = 0 ' First Choice
            sizeCombo.SelectedIndex = 7 ' 24.0
            weightCombo.SelectedIndex = 3 ' Normal
            styleCombo.SelectedIndex = 0 ' Normal
            stretchCombo.SelectedIndex = 5 ' Normal
        End Sub

		Private Sub AddTextButton_Click(ByVal sender As Object, ByVal e As EventArgs) Handles AddTextButton.Click
			DialogResult = System.Windows.Forms.DialogResult.OK
			Close()
		End Sub


        Private Sub CancelTextButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
            DialogResult = System.Windows.Forms.DialogResult.Cancel
            Close()
        End Sub
    End Class
End Namespace
