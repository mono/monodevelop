Imports Microsoft.VisualBasic
Imports System
Namespace TaskDialogDemo
	Partial Public Class TestHarness
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
			Me.rdoInformation = New System.Windows.Forms.RadioButton()
			Me.groupBox3 = New System.Windows.Forms.GroupBox()
			Me.label3 = New System.Windows.Forms.Label()
			Me.label2 = New System.Windows.Forms.Label()
			Me.label1 = New System.Windows.Forms.Label()
			Me.txtTitle = New System.Windows.Forms.TextBox()
			Me.txtInstruction = New System.Windows.Forms.TextBox()
			Me.txtContent = New System.Windows.Forms.TextBox()
			Me.rdoShield = New System.Windows.Forms.RadioButton()
			Me.rdoWarning = New System.Windows.Forms.RadioButton()
			Me.rdoError = New System.Windows.Forms.RadioButton()
			Me.rdoNone = New System.Windows.Forms.RadioButton()
			Me.groupBox2 = New System.Windows.Forms.GroupBox()
			Me.chkYes = New System.Windows.Forms.CheckBox()
			Me.chkRetry = New System.Windows.Forms.CheckBox()
			Me.groupBox1 = New System.Windows.Forms.GroupBox()
			Me.chkNo = New System.Windows.Forms.CheckBox()
			Me.chkCancel = New System.Windows.Forms.CheckBox()
			Me.chkOK = New System.Windows.Forms.CheckBox()
			Me.chkClose = New System.Windows.Forms.CheckBox()
			Me.cmdShow = New System.Windows.Forms.Button()
			Me.label4 = New System.Windows.Forms.Label()
			Me.resultLbl = New System.Windows.Forms.TextBox()
			Me.groupBox3.SuspendLayout()
			Me.groupBox2.SuspendLayout()
			Me.groupBox1.SuspendLayout()
			Me.SuspendLayout()
			' 
			' rdoInformation
			' 
			Me.rdoInformation.AutoSize = True
			Me.rdoInformation.Location = New System.Drawing.Point(130, 49)
			Me.rdoInformation.Name = "rdoInformation"
			Me.rdoInformation.Size = New System.Drawing.Size(77, 17)
			Me.rdoInformation.TabIndex = 3
			Me.rdoInformation.Text = "Information"
			Me.rdoInformation.UseVisualStyleBackColor = True
			' 
			' groupBox3
			' 
			Me.groupBox3.Controls.Add(Me.label3)
			Me.groupBox3.Controls.Add(Me.label2)
			Me.groupBox3.Controls.Add(Me.label1)
			Me.groupBox3.Controls.Add(Me.txtTitle)
			Me.groupBox3.Controls.Add(Me.txtInstruction)
			Me.groupBox3.Controls.Add(Me.txtContent)
			Me.groupBox3.Location = New System.Drawing.Point(254, 7)
			Me.groupBox3.Name = "groupBox3"
			Me.groupBox3.Size = New System.Drawing.Size(304, 97)
			Me.groupBox3.TabIndex = 23
			Me.groupBox3.TabStop = False
			Me.groupBox3.Text = "Texts"
			' 
			' label3
			' 
			Me.label3.AutoSize = True
			Me.label3.Location = New System.Drawing.Point(6, 71)
			Me.label3.Name = "label3"
			Me.label3.Size = New System.Drawing.Size(71, 13)
			Me.label3.TabIndex = 18
			Me.label3.Text = "Content Text:"
			' 
			' label2
			' 
			Me.label2.AutoSize = True
			Me.label2.Location = New System.Drawing.Point(6, 47)
			Me.label2.Name = "label2"
			Me.label2.Size = New System.Drawing.Size(85, 13)
			Me.label2.TabIndex = 17
			Me.label2.Text = "Main Instruction:"
			' 
			' label1
			' 
			Me.label1.AutoSize = True
			Me.label1.Location = New System.Drawing.Point(6, 21)
			Me.label1.Name = "label1"
			Me.label1.Size = New System.Drawing.Size(30, 13)
			Me.label1.TabIndex = 16
			Me.label1.Text = "Title:"
			' 
			' txtTitle
			' 
			Me.txtTitle.Location = New System.Drawing.Point(102, 18)
			Me.txtTitle.Name = "txtTitle"
			Me.txtTitle.Size = New System.Drawing.Size(191, 20)
			Me.txtTitle.TabIndex = 13
			Me.txtTitle.Text = "Enter your title in here"
			' 
			' txtInstruction
			' 
			Me.txtInstruction.Location = New System.Drawing.Point(102, 44)
			Me.txtInstruction.Name = "txtInstruction"
			Me.txtInstruction.Size = New System.Drawing.Size(191, 20)
			Me.txtInstruction.TabIndex = 14
			Me.txtInstruction.Text = "Enter your main instruction here"
			' 
			' txtContent
			' 
			Me.txtContent.Location = New System.Drawing.Point(102, 68)
			Me.txtContent.Name = "txtContent"
			Me.txtContent.Size = New System.Drawing.Size(191, 20)
			Me.txtContent.TabIndex = 15
			Me.txtContent.Text = "Enter your content text in here"
			' 
			' rdoShield
			' 
			Me.rdoShield.AutoSize = True
			Me.rdoShield.Location = New System.Drawing.Point(14, 71)
			Me.rdoShield.Name = "rdoShield"
			Me.rdoShield.Size = New System.Drawing.Size(54, 17)
			Me.rdoShield.TabIndex = 5
			Me.rdoShield.Text = "Shield"
			Me.rdoShield.UseVisualStyleBackColor = True
			' 
			' rdoWarning
			' 
			Me.rdoWarning.AutoSize = True
			Me.rdoWarning.Location = New System.Drawing.Point(15, 48)
			Me.rdoWarning.Name = "rdoWarning"
			Me.rdoWarning.Size = New System.Drawing.Size(65, 17)
			Me.rdoWarning.TabIndex = 2
			Me.rdoWarning.Text = "Warning"
			Me.rdoWarning.UseVisualStyleBackColor = True
			' 
			' rdoError
			' 
			Me.rdoError.AutoSize = True
			Me.rdoError.Location = New System.Drawing.Point(130, 26)
			Me.rdoError.Name = "rdoError"
			Me.rdoError.Size = New System.Drawing.Size(47, 17)
			Me.rdoError.TabIndex = 1
			Me.rdoError.Text = "Error"
			Me.rdoError.UseVisualStyleBackColor = True
			' 
			' rdoNone
			' 
			Me.rdoNone.AutoSize = True
			Me.rdoNone.Checked = True
			Me.rdoNone.Location = New System.Drawing.Point(15, 25)
			Me.rdoNone.Name = "rdoNone"
			Me.rdoNone.Size = New System.Drawing.Size(51, 17)
			Me.rdoNone.TabIndex = 0
			Me.rdoNone.TabStop = True
			Me.rdoNone.Text = "None"
			Me.rdoNone.UseVisualStyleBackColor = True
			' 
			' groupBox2
			' 
			Me.groupBox2.Controls.Add(Me.rdoShield)
			Me.groupBox2.Controls.Add(Me.rdoInformation)
			Me.groupBox2.Controls.Add(Me.rdoWarning)
			Me.groupBox2.Controls.Add(Me.rdoError)
			Me.groupBox2.Controls.Add(Me.rdoNone)
			Me.groupBox2.Location = New System.Drawing.Point(9, 7)
			Me.groupBox2.Name = "groupBox2"
			Me.groupBox2.Size = New System.Drawing.Size(235, 97)
			Me.groupBox2.TabIndex = 21
			Me.groupBox2.TabStop = False
			Me.groupBox2.Text = "Icon"
			' 
			' chkYes
			' 
			Me.chkYes.AutoSize = True
			Me.chkYes.Location = New System.Drawing.Point(14, 73)
			Me.chkYes.Name = "chkYes"
			Me.chkYes.Size = New System.Drawing.Size(44, 17)
			Me.chkYes.TabIndex = 2
			Me.chkYes.Text = "Yes"
			Me.chkYes.UseVisualStyleBackColor = True
			' 
			' chkRetry
			' 
			Me.chkRetry.AutoSize = True
			Me.chkRetry.Location = New System.Drawing.Point(126, 27)
			Me.chkRetry.Name = "chkRetry"
			Me.chkRetry.Size = New System.Drawing.Size(51, 17)
			Me.chkRetry.TabIndex = 10
			Me.chkRetry.Text = "Retry"
			Me.chkRetry.UseVisualStyleBackColor = True
			' 
			' groupBox1
			' 
			Me.groupBox1.Controls.Add(Me.chkYes)
			Me.groupBox1.Controls.Add(Me.chkRetry)
			Me.groupBox1.Controls.Add(Me.chkNo)
			Me.groupBox1.Controls.Add(Me.chkCancel)
			Me.groupBox1.Controls.Add(Me.chkOK)
			Me.groupBox1.Controls.Add(Me.chkClose)
			Me.groupBox1.Location = New System.Drawing.Point(9, 106)
			Me.groupBox1.Name = "groupBox1"
			Me.groupBox1.Size = New System.Drawing.Size(235, 124)
			Me.groupBox1.TabIndex = 20
			Me.groupBox1.TabStop = False
			Me.groupBox1.Text = "Buttons"
			' 
			' chkNo
			' 
			Me.chkNo.AutoSize = True
			Me.chkNo.Location = New System.Drawing.Point(14, 96)
			Me.chkNo.Name = "chkNo"
			Me.chkNo.Size = New System.Drawing.Size(40, 17)
			Me.chkNo.TabIndex = 8
			Me.chkNo.Text = "No"
			Me.chkNo.UseVisualStyleBackColor = True
			' 
			' chkCancel
			' 
			Me.chkCancel.AutoSize = True
			Me.chkCancel.Location = New System.Drawing.Point(14, 50)
			Me.chkCancel.Name = "chkCancel"
			Me.chkCancel.Size = New System.Drawing.Size(59, 17)
			Me.chkCancel.TabIndex = 4
			Me.chkCancel.Text = "Cancel"
			Me.chkCancel.UseVisualStyleBackColor = True
			' 
			' chkOK
			' 
			Me.chkOK.AutoSize = True
			Me.chkOK.Checked = True
			Me.chkOK.CheckState = System.Windows.Forms.CheckState.Checked
			Me.chkOK.Location = New System.Drawing.Point(14, 27)
			Me.chkOK.Name = "chkOK"
			Me.chkOK.Size = New System.Drawing.Size(41, 17)
			Me.chkOK.TabIndex = 7
			Me.chkOK.Text = "OK"
			Me.chkOK.UseVisualStyleBackColor = True
			' 
			' chkClose
			' 
			Me.chkClose.AutoSize = True
			Me.chkClose.Location = New System.Drawing.Point(125, 50)
			Me.chkClose.Name = "chkClose"
			Me.chkClose.Size = New System.Drawing.Size(52, 17)
			Me.chkClose.TabIndex = 6
			Me.chkClose.Text = "Close"
			Me.chkClose.UseVisualStyleBackColor = True
			' 
			' cmdShow
			' 
			Me.cmdShow.Font = New System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, (CByte(0)))
			Me.cmdShow.Location = New System.Drawing.Point(368, 277)
			Me.cmdShow.Margin = New System.Windows.Forms.Padding(3, 4, 3, 4)
			Me.cmdShow.Name = "cmdShow"
			Me.cmdShow.Size = New System.Drawing.Size(190, 55)
			Me.cmdShow.TabIndex = 19
			Me.cmdShow.Text = "TaskDialog.Show()"
			Me.cmdShow.UseVisualStyleBackColor = True
'			Me.cmdShow.Click += New System.EventHandler(Me.cmdShow_Click)
			' 
			' label4
			' 
			Me.label4.AutoSize = True
			Me.label4.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, (CByte(0)))
			Me.label4.Location = New System.Drawing.Point(251, 117)
			Me.label4.Name = "label4"
			Me.label4.Size = New System.Drawing.Size(182, 13)
			Me.label4.TabIndex = 24
			Me.label4.Text = "Result from TaskDialog.Show()"
			' 
			' resultLbl
			' 
			Me.resultLbl.Location = New System.Drawing.Point(254, 133)
			Me.resultLbl.Multiline = True
			Me.resultLbl.Name = "resultLbl"
			Me.resultLbl.ReadOnly = True
			Me.resultLbl.ScrollBars = System.Windows.Forms.ScrollBars.Both
			Me.resultLbl.Size = New System.Drawing.Size(293, 123)
			Me.resultLbl.TabIndex = 25
			' 
			' TestHarness
			' 
			Me.AutoScaleDimensions = New System.Drawing.SizeF(6F, 13F)
			Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
			Me.ClientSize = New System.Drawing.Size(566, 345)
			Me.Controls.Add(Me.resultLbl)
			Me.Controls.Add(Me.label4)
			Me.Controls.Add(Me.groupBox3)
			Me.Controls.Add(Me.groupBox2)
			Me.Controls.Add(Me.groupBox1)
			Me.Controls.Add(Me.cmdShow)
			Me.Name = "TestHarness"
			Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
			Me.Text = "TaskDialog Test Harness"
			Me.groupBox3.ResumeLayout(False)
			Me.groupBox3.PerformLayout()
			Me.groupBox2.ResumeLayout(False)
			Me.groupBox2.PerformLayout()
			Me.groupBox1.ResumeLayout(False)
			Me.groupBox1.PerformLayout()
			Me.ResumeLayout(False)
			Me.PerformLayout()

		End Sub

		#End Region

		Private rdoInformation As System.Windows.Forms.RadioButton
		Private groupBox3 As System.Windows.Forms.GroupBox
		Private txtTitle As System.Windows.Forms.TextBox
		Private txtInstruction As System.Windows.Forms.TextBox
		Private txtContent As System.Windows.Forms.TextBox
		Private rdoShield As System.Windows.Forms.RadioButton
		Private rdoWarning As System.Windows.Forms.RadioButton
		Private rdoError As System.Windows.Forms.RadioButton
		Private rdoNone As System.Windows.Forms.RadioButton
		Private groupBox2 As System.Windows.Forms.GroupBox
		Private chkYes As System.Windows.Forms.CheckBox
		Private chkRetry As System.Windows.Forms.CheckBox
		Private groupBox1 As System.Windows.Forms.GroupBox
		Private chkNo As System.Windows.Forms.CheckBox
		Private chkCancel As System.Windows.Forms.CheckBox
		Private chkOK As System.Windows.Forms.CheckBox
		Private chkClose As System.Windows.Forms.CheckBox
		Private WithEvents cmdShow As System.Windows.Forms.Button
		Private label3 As System.Windows.Forms.Label
		Private label2 As System.Windows.Forms.Label
		Private label1 As System.Windows.Forms.Label
		Private label4 As System.Windows.Forms.Label
		Private resultLbl As System.Windows.Forms.TextBox
	End Class
End Namespace