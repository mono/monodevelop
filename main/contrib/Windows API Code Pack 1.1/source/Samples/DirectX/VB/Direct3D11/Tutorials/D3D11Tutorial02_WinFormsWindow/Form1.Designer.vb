Imports Microsoft.VisualBasic
Imports System
Namespace Microsoft.WindowsAPICodePack.Samples.Direct3D11
	Partial Public Class Form1
		''' <summary>
		''' Required designer variable.
		''' </summary>
		Private components As System.ComponentModel.IContainer = Nothing

		''' <summary>
		''' Clean up any resources being used.
		''' </summary>
		''' <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		Protected Overrides Sub Dispose(ByVal disposing As Boolean)
			If disposing AndAlso (components IsNot Nothing) Then
				components.Dispose()
			End If
			MyBase.Dispose(disposing)
		End Sub

		#Region "Windows Form Designer generated code"

		''' <summary>
		''' Required method for Designer support - do not modify
		''' the contents of this method with the code editor.
		''' </summary>
		Private Sub InitializeComponent()
            Me.SuspendLayout()
            '
            'Form1
            '
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None
            Me.ClientSize = New System.Drawing.Size(630, 450)
            Me.Name = "Form1"
            Me.Text = "Direct3D 11 Tutorial 02 (Double click inside window to set Full Screen mode)"
            Me.ResumeLayout(False)

        End Sub

		#End Region
	End Class
End Namespace

