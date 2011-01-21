Imports Microsoft.VisualBasic
Imports System
Namespace WindowsFlag
	Partial Public Class Window
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
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Window))
            Me.directControl = New Microsoft.WindowsAPICodePack.DirectX.Controls.DirectControl
            Me.label1 = New System.Windows.Forms.Label
            Me.label2 = New System.Windows.Forms.Label
            Me.SuspendLayout()
            '
            'directControl
            '
            Me.directControl.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                        Or System.Windows.Forms.AnchorStyles.Left) _
                        Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.directControl.Location = New System.Drawing.Point(0, 0)
            Me.directControl.Name = "directControl"
            Me.directControl.Size = New System.Drawing.Size(624, 442)
            Me.directControl.TabIndex = 4
            '
            'label1
            '
            Me.label1.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
            Me.label1.AutoSize = True
            Me.label1.Location = New System.Drawing.Point(12, 447)
            Me.label1.Name = "label1"
            Me.label1.Size = New System.Drawing.Size(0, 13)
            Me.label1.TabIndex = 5
            '
            'label2
            '
            Me.label2.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.label2.AutoSize = True
            Me.label2.Location = New System.Drawing.Point(278, 447)
            Me.label2.Name = "label2"
            Me.label2.Size = New System.Drawing.Size(138, 13)
            Me.label2.TabIndex = 6
            Me.label2.Text = "Drag/scroll to move camera"
            '
            'Window
            '
            Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.ClientSize = New System.Drawing.Size(624, 469)
            Me.Controls.Add(Me.label2)
            Me.Controls.Add(Me.label1)
            Me.Controls.Add(Me.directControl)
            Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
            Me.Name = "Window"
            Me.Text = "Windows Flag"
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub

		#End Region

		Private WithEvents directControl As Microsoft.WindowsAPICodePack.DirectX.Controls.DirectControl
		Private label1 As System.Windows.Forms.Label
		Private label2 As System.Windows.Forms.Label
	End Class
End Namespace

