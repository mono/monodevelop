Imports Microsoft.VisualBasic
Imports System
Namespace RandomShapes
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
			Me.d2DShapesControlWithButtons1 = New D2DShapes.D2DShapesControlWithButtons()
			Me.SuspendLayout()
			' 
			' d2DShapesControlWithButtons1
			' 
			Me.d2DShapesControlWithButtons1.BackColor = System.Drawing.Color.Bisque
			Me.d2DShapesControlWithButtons1.Dock = System.Windows.Forms.DockStyle.Fill
			Me.d2DShapesControlWithButtons1.Location = New System.Drawing.Point(0, 0)
			Me.d2DShapesControlWithButtons1.Name = "d2DShapesControlWithButtons1"
			Me.d2DShapesControlWithButtons1.NumberOfShapesToAdd = 2
			Me.d2DShapesControlWithButtons1.Size = New System.Drawing.Size(728, 465)
			Me.d2DShapesControlWithButtons1.TabIndex = 0
			' 
			' Window
			' 
			Me.AutoScaleDimensions = New System.Drawing.SizeF(6F, 13F)
			Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
			Me.ClientSize = New System.Drawing.Size(728, 465)
			Me.Controls.Add(Me.d2DShapesControlWithButtons1)
			Me.Name = "Window"
			Me.Text = "Random Shapes"
			Me.WindowState = System.Windows.Forms.FormWindowState.Maximized
'			Me.Load += New System.EventHandler(Me.Window_Load)
			Me.ResumeLayout(False)

		End Sub

		#End Region

		Private d2DShapesControlWithButtons1 As D2DShapes.D2DShapesControlWithButtons
	End Class
End Namespace

