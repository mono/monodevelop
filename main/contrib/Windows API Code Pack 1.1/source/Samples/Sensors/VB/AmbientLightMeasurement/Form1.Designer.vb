Imports Microsoft.VisualBasic
Imports System
Namespace AmbientLightMeasurement
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
			Me.panel = New System.Windows.Forms.Panel()
			Me.SuspendLayout()
			' 
			' panel
			' 
			Me.panel.AutoScroll = True
			Me.panel.Dock = System.Windows.Forms.DockStyle.Fill
			Me.panel.Location = New System.Drawing.Point(0, 0)
			Me.panel.Name = "panel"
			Me.panel.Size = New System.Drawing.Size(670, 77)
			Me.panel.TabIndex = 3
			' 
			' Form1
			' 
			Me.AutoScaleDimensions = New System.Drawing.SizeF(6F, 13F)
			Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
			Me.ClientSize = New System.Drawing.Size(670, 77)
			Me.Controls.Add(Me.panel)
			Me.Name = "Form1"
			Me.Text = "Ambient Light Level"
'			Me.Shown += New System.EventHandler(Me.Form1_Shown)
			Me.ResumeLayout(False)

		End Sub

		#End Region

		Private panel As System.Windows.Forms.Panel

	End Class
End Namespace

