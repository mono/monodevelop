Imports Microsoft.VisualBasic
Imports System
Namespace AccelerationMeasurement
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
			Me.accelX = New AccelerationMeasurement.AccelerationBar()
			Me.accelY = New AccelerationMeasurement.AccelerationBar()
			Me.accelZ = New AccelerationMeasurement.AccelerationBar()
			Me.label1 = New System.Windows.Forms.Label()
			Me.label2 = New System.Windows.Forms.Label()
			Me.label3 = New System.Windows.Forms.Label()
			Me.availabilityLabel = New System.Windows.Forms.Label()
			Me.SuspendLayout()
			' 
			' accelX
			' 
			Me.accelX.Acceleration = 0F
			Me.accelX.BackColor = System.Drawing.Color.White
			Me.accelX.Location = New System.Drawing.Point(50, 28)
			Me.accelX.Name = "accelX"
			Me.accelX.Size = New System.Drawing.Size(213, 28)
			Me.accelX.TabIndex = 0
			Me.accelX.Text = "accelerationBar1"
			' 
			' accelY
			' 
			Me.accelY.Acceleration = 0F
			Me.accelY.BackColor = System.Drawing.Color.White
			Me.accelY.Location = New System.Drawing.Point(50, 62)
			Me.accelY.Name = "accelY"
			Me.accelY.Size = New System.Drawing.Size(213, 28)
			Me.accelY.TabIndex = 1
			Me.accelY.Text = "accelerationBar1"
			' 
			' accelZ
			' 
			Me.accelZ.Acceleration = 0F
			Me.accelZ.BackColor = System.Drawing.Color.White
			Me.accelZ.Location = New System.Drawing.Point(50, 96)
			Me.accelZ.Name = "accelZ"
			Me.accelZ.Size = New System.Drawing.Size(213, 28)
			Me.accelZ.TabIndex = 2
			Me.accelZ.Text = "accelerationBar1"
			' 
			' label1
			' 
			Me.label1.AutoSize = True
			Me.label1.Location = New System.Drawing.Point(12, 37)
			Me.label1.Name = "label1"
			Me.label1.Size = New System.Drawing.Size(14, 13)
			Me.label1.TabIndex = 6
			Me.label1.Text = "X"
			' 
			' label2
			' 
			Me.label2.AutoSize = True
			Me.label2.Location = New System.Drawing.Point(12, 71)
			Me.label2.Name = "label2"
			Me.label2.Size = New System.Drawing.Size(14, 13)
			Me.label2.TabIndex = 7
			Me.label2.Text = "Y"
			' 
			' label3
			' 
			Me.label3.AutoSize = True
			Me.label3.Location = New System.Drawing.Point(12, 105)
			Me.label3.Name = "label3"
			Me.label3.Size = New System.Drawing.Size(14, 13)
			Me.label3.TabIndex = 8
			Me.label3.Text = "Z"
			' 
			' availabilityLabel
			' 
			Me.availabilityLabel.AutoSize = True
			Me.availabilityLabel.Location = New System.Drawing.Point(47, 9)
			Me.availabilityLabel.Name = "availabilityLabel"
			Me.availabilityLabel.Size = New System.Drawing.Size(143, 13)
			Me.availabilityLabel.TabIndex = 9
			Me.availabilityLabel.Text = "Accelerometers available = 0"
			' 
			' Form1
			' 
			Me.AutoScaleDimensions = New System.Drawing.SizeF(6F, 13F)
			Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
			Me.ClientSize = New System.Drawing.Size(289, 139)
			Me.Controls.Add(Me.availabilityLabel)
			Me.Controls.Add(Me.label3)
			Me.Controls.Add(Me.label2)
			Me.Controls.Add(Me.label1)
			Me.Controls.Add(Me.accelZ)
			Me.Controls.Add(Me.accelY)
			Me.Controls.Add(Me.accelX)
			Me.Name = "Form1"
			Me.Text = "Acceleration Measurement"
'			Me.Shown += New System.EventHandler(Me.Form1_Shown)
			Me.ResumeLayout(False)
			Me.PerformLayout()

		End Sub

		#End Region

		Private accelX As AccelerationBar
		Private accelY As AccelerationBar
		Private accelZ As AccelerationBar
		Private label1 As System.Windows.Forms.Label
		Private label2 As System.Windows.Forms.Label
		Private label3 As System.Windows.Forms.Label
		Private availabilityLabel As System.Windows.Forms.Label


	End Class
End Namespace

