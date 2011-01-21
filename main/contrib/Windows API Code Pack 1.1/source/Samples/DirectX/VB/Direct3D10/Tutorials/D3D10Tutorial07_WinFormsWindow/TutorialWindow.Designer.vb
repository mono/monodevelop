Imports Microsoft.VisualBasic
Imports System
Namespace D3D10Tutorial07
	Partial Public Class TutorialWindow
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
            'TutorialWindow
            '
            Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.ClientSize = New System.Drawing.Size(639, 474)
            Me.Name = "TutorialWindow"
            Me.Text = "D3D 10 Tutorial 7: Texture Mapping and Constant Buffers (Double Click inside wind" & _
                "ow to set Full Screen)"
            Me.ResumeLayout(False)

        End Sub

		#End Region
	End Class
End Namespace

