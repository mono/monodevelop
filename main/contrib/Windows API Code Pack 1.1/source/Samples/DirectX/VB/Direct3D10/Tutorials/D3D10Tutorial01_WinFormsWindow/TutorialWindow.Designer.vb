Imports Microsoft.VisualBasic
Imports System
Namespace D3D10Tutorial01_WinFormsWindow
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
			Dim resources As New System.ComponentModel.ComponentResourceManager(GetType(TutorialWindow))
			Me.SuspendLayout()
			' 
			' TutorialWindow
			' 
			Me.AutoScaleDimensions = New System.Drawing.SizeF(6F, 13F)
			Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
			Me.ClientSize = New System.Drawing.Size(624, 442)
			Me.Icon = (CType(resources.GetObject("$this.Icon"), System.Drawing.Icon))
			Me.Name = "TutorialWindow"
			Me.Text = "Direct3D 10 Tutorial 1: Direct3D 10 Basics"
'			Me.Load += New System.EventHandler(Me.TutorialWindow_Load)
'			Me.FormClosing += New System.Windows.Forms.FormClosingEventHandler(Me.TutorialWindow_FormClosing)
			Me.ResumeLayout(False)

		End Sub

		#End Region

	End Class
End Namespace

