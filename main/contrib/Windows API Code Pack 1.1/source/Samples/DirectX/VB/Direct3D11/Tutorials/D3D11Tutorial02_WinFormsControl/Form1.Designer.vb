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
            Me.directControl = New Microsoft.WindowsAPICodePack.DirectX.Controls.DirectControl
            Me.SuspendLayout()
            '
            'directControl
            '
            Me.directControl.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                        Or System.Windows.Forms.AnchorStyles.Left) _
                        Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.directControl.Location = New System.Drawing.Point(12, 12)
            Me.directControl.Name = "directControl"
            Me.directControl.Size = New System.Drawing.Size(606, 426)
            Me.directControl.TabIndex = 0
            '
            'Form1
            '
            Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.ClientSize = New System.Drawing.Size(630, 450)
            Me.Controls.Add(Me.directControl)
            Me.Name = "Form1"
            Me.Text = "Direct3D 11 Tutorial 02"
            Me.ResumeLayout(False)

        End Sub

		#End Region

		Private WithEvents directControl As Microsoft.WindowsAPICodePack.DirectX.Controls.DirectControl
    End Class
End Namespace

