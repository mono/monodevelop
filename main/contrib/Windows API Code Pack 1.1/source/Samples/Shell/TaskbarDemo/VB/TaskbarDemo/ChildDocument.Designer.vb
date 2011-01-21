Imports Microsoft.VisualBasic
Imports System
Namespace TaskbarDemo
	Partial Public Class ChildDocument
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
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(ChildDocument))
            Me.groupBoxIconOverlay = New System.Windows.Forms.GroupBox
            Me.labelNoIconOverlay = New System.Windows.Forms.Label
            Me.pictureIconOverlay3 = New System.Windows.Forms.PictureBox
            Me.pictureIconOverlay2 = New System.Windows.Forms.PictureBox
            Me.label7 = New System.Windows.Forms.Label
            Me.pictureIconOverlay1 = New System.Windows.Forms.PictureBox
            Me.groupBoxCustomCategories = New System.Windows.Forms.GroupBox
            Me.listBox1 = New System.Windows.Forms.ListBox
            Me.label5 = New System.Windows.Forms.Label
            Me.buttonRefreshTaskbarList = New System.Windows.Forms.Button
            Me.button1 = New System.Windows.Forms.Button
            Me.groupBoxProgressBar = New System.Windows.Forms.GroupBox
            Me.trackBar1 = New System.Windows.Forms.TrackBar
            Me.label8 = New System.Windows.Forms.Label
            Me.label6 = New System.Windows.Forms.Label
            Me.comboBoxProgressBarStates = New System.Windows.Forms.ComboBox
            Me.progressBar1 = New System.Windows.Forms.ProgressBar
            Me.groupBoxIconOverlay.SuspendLayout()
            CType(Me.pictureIconOverlay3, System.ComponentModel.ISupportInitialize).BeginInit()
            CType(Me.pictureIconOverlay2, System.ComponentModel.ISupportInitialize).BeginInit()
            CType(Me.pictureIconOverlay1, System.ComponentModel.ISupportInitialize).BeginInit()
            Me.groupBoxCustomCategories.SuspendLayout()
            Me.groupBoxProgressBar.SuspendLayout()
            CType(Me.trackBar1, System.ComponentModel.ISupportInitialize).BeginInit()
            Me.SuspendLayout()
            '
            'groupBoxIconOverlay
            '
            Me.groupBoxIconOverlay.Controls.Add(Me.labelNoIconOverlay)
            Me.groupBoxIconOverlay.Controls.Add(Me.pictureIconOverlay3)
            Me.groupBoxIconOverlay.Controls.Add(Me.pictureIconOverlay2)
            Me.groupBoxIconOverlay.Controls.Add(Me.label7)
            Me.groupBoxIconOverlay.Controls.Add(Me.pictureIconOverlay1)
            Me.groupBoxIconOverlay.Location = New System.Drawing.Point(13, 342)
            Me.groupBoxIconOverlay.Name = "groupBoxIconOverlay"
            Me.groupBoxIconOverlay.Size = New System.Drawing.Size(305, 88)
            Me.groupBoxIconOverlay.TabIndex = 12
            Me.groupBoxIconOverlay.TabStop = False
            Me.groupBoxIconOverlay.Text = "Icon Overlay"
            '
            'labelNoIconOverlay
            '
            Me.labelNoIconOverlay.Location = New System.Drawing.Point(9, 50)
            Me.labelNoIconOverlay.Name = "labelNoIconOverlay"
            Me.labelNoIconOverlay.Size = New System.Drawing.Size(35, 13)
            Me.labelNoIconOverlay.TabIndex = 0
            Me.labelNoIconOverlay.Text = "None"
            '
            'pictureIconOverlay3
            '
            Me.pictureIconOverlay3.Image = CType(resources.GetObject("pictureIconOverlay3.Image"), System.Drawing.Image)
            Me.pictureIconOverlay3.Location = New System.Drawing.Point(126, 41)
            Me.pictureIconOverlay3.Name = "pictureIconOverlay3"
            Me.pictureIconOverlay3.Size = New System.Drawing.Size(32, 32)
            Me.pictureIconOverlay3.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage
            Me.pictureIconOverlay3.TabIndex = 3
            Me.pictureIconOverlay3.TabStop = False
            '
            'pictureIconOverlay2
            '
            Me.pictureIconOverlay2.Image = CType(resources.GetObject("pictureIconOverlay2.Image"), System.Drawing.Image)
            Me.pictureIconOverlay2.Location = New System.Drawing.Point(88, 41)
            Me.pictureIconOverlay2.Name = "pictureIconOverlay2"
            Me.pictureIconOverlay2.Size = New System.Drawing.Size(32, 32)
            Me.pictureIconOverlay2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage
            Me.pictureIconOverlay2.TabIndex = 2
            Me.pictureIconOverlay2.TabStop = False
            '
            'label7
            '
            Me.label7.AutoSize = True
            Me.label7.Location = New System.Drawing.Point(9, 20)
            Me.label7.Name = "label7"
            Me.label7.Size = New System.Drawing.Size(209, 13)
            Me.label7.TabIndex = 1
            Me.label7.Text = "Select an image to overlay on the task bar:"
            '
            'pictureIconOverlay1
            '
            Me.pictureIconOverlay1.Image = CType(resources.GetObject("pictureIconOverlay1.Image"), System.Drawing.Image)
            Me.pictureIconOverlay1.Location = New System.Drawing.Point(50, 41)
            Me.pictureIconOverlay1.Name = "pictureIconOverlay1"
            Me.pictureIconOverlay1.Size = New System.Drawing.Size(32, 32)
            Me.pictureIconOverlay1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage
            Me.pictureIconOverlay1.TabIndex = 0
            Me.pictureIconOverlay1.TabStop = False
            '
            'groupBoxCustomCategories
            '
            Me.groupBoxCustomCategories.Controls.Add(Me.listBox1)
            Me.groupBoxCustomCategories.Controls.Add(Me.buttonRefreshTaskbarList)
            Me.groupBoxCustomCategories.Controls.Add(Me.label5)
            Me.groupBoxCustomCategories.Enabled = False
            Me.groupBoxCustomCategories.Location = New System.Drawing.Point(13, 12)
            Me.groupBoxCustomCategories.Name = "groupBoxCustomCategories"
            Me.groupBoxCustomCategories.Size = New System.Drawing.Size(304, 174)
            Me.groupBoxCustomCategories.TabIndex = 13
            Me.groupBoxCustomCategories.TabStop = False
            Me.groupBoxCustomCategories.Text = "Custom JumpList"
            '
            'listBox1
            '
            Me.listBox1.FormattingEnabled = True
            Me.listBox1.Items.AddRange(New Object() {"Notepad", "Calculator", "Paint", "WordPad", "Windows Explorer", "Internet Explorer", "Control Panel", "Documents Library"})
            Me.listBox1.Location = New System.Drawing.Point(87, 30)
            Me.listBox1.Name = "listBox1"
            Me.listBox1.SelectionMode = System.Windows.Forms.SelectionMode.MultiSimple
            Me.listBox1.Size = New System.Drawing.Size(210, 108)
            Me.listBox1.TabIndex = 11
            '
            'label5
            '
            Me.label5.AutoSize = True
            Me.label5.Location = New System.Drawing.Point(8, 30)
            Me.label5.Name = "label5"
            Me.label5.Size = New System.Drawing.Size(64, 13)
            Me.label5.TabIndex = 8
            Me.label5.Text = "User Tasks:"
            '
            'buttonRefreshTaskbarList
            '
            Me.buttonRefreshTaskbarList.Enabled = False
            Me.buttonRefreshTaskbarList.Location = New System.Drawing.Point(143, 144)
            Me.buttonRefreshTaskbarList.Name = "buttonRefreshTaskbarList"
            Me.buttonRefreshTaskbarList.Size = New System.Drawing.Size(123, 23)
            Me.buttonRefreshTaskbarList.TabIndex = 14
            Me.buttonRefreshTaskbarList.Text = "Refresh JumpList"
            Me.buttonRefreshTaskbarList.UseVisualStyleBackColor = True
            '
            'button1
            '
            Me.button1.Location = New System.Drawing.Point(11, 445)
            Me.button1.Name = "button1"
            Me.button1.Size = New System.Drawing.Size(132, 23)
            Me.button1.TabIndex = 15
            Me.button1.Text = "Add separate JumpList"
            Me.button1.UseVisualStyleBackColor = True
            '
            'groupBoxProgressBar
            '
            Me.groupBoxProgressBar.Controls.Add(Me.trackBar1)
            Me.groupBoxProgressBar.Controls.Add(Me.label8)
            Me.groupBoxProgressBar.Controls.Add(Me.label6)
            Me.groupBoxProgressBar.Controls.Add(Me.comboBoxProgressBarStates)
            Me.groupBoxProgressBar.Controls.Add(Me.progressBar1)
            Me.groupBoxProgressBar.Location = New System.Drawing.Point(13, 192)
            Me.groupBoxProgressBar.Name = "groupBoxProgressBar"
            Me.groupBoxProgressBar.Size = New System.Drawing.Size(305, 144)
            Me.groupBoxProgressBar.TabIndex = 16
            Me.groupBoxProgressBar.TabStop = False
            Me.groupBoxProgressBar.Text = "Progress Bar"
            '
            'trackBar1
            '
            Me.trackBar1.LargeChange = 25
            Me.trackBar1.Location = New System.Drawing.Point(92, 59)
            Me.trackBar1.Maximum = 100
            Me.trackBar1.Name = "trackBar1"
            Me.trackBar1.Size = New System.Drawing.Size(203, 45)
            Me.trackBar1.SmallChange = 10
            Me.trackBar1.TabIndex = 12
            Me.trackBar1.TickStyle = System.Windows.Forms.TickStyle.None
            '
            'label8
            '
            Me.label8.AutoSize = True
            Me.label8.Location = New System.Drawing.Point(6, 71)
            Me.label8.Name = "label8"
            Me.label8.Size = New System.Drawing.Size(77, 13)
            Me.label8.TabIndex = 11
            Me.label8.Text = "Update value: "
            '
            'label6
            '
            Me.label6.AutoSize = True
            Me.label6.Location = New System.Drawing.Point(6, 25)
            Me.label6.Name = "label6"
            Me.label6.Size = New System.Drawing.Size(35, 13)
            Me.label6.TabIndex = 3
            Me.label6.Text = "State:"
            '
            'comboBoxProgressBarStates
            '
            Me.comboBoxProgressBarStates.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.comboBoxProgressBarStates.FormattingEnabled = True
            Me.comboBoxProgressBarStates.Location = New System.Drawing.Point(47, 22)
            Me.comboBoxProgressBarStates.Name = "comboBoxProgressBarStates"
            Me.comboBoxProgressBarStates.Size = New System.Drawing.Size(154, 21)
            Me.comboBoxProgressBarStates.TabIndex = 2
            '
            'progressBar1
            '
            Me.progressBar1.Location = New System.Drawing.Point(11, 110)
            Me.progressBar1.Name = "progressBar1"
            Me.progressBar1.Size = New System.Drawing.Size(285, 23)
            Me.progressBar1.Step = 5
            Me.progressBar1.TabIndex = 0
            '
            'ChildDocument
            '
            Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.ClientSize = New System.Drawing.Size(327, 510)
            Me.Controls.Add(Me.groupBoxProgressBar)
            Me.Controls.Add(Me.button1)
            Me.Controls.Add(Me.groupBoxCustomCategories)
            Me.Controls.Add(Me.groupBoxIconOverlay)
            Me.Name = "ChildDocument"
            Me.Text = "Child Document Window"
            Me.groupBoxIconOverlay.ResumeLayout(False)
            Me.groupBoxIconOverlay.PerformLayout()
            CType(Me.pictureIconOverlay3, System.ComponentModel.ISupportInitialize).EndInit()
            CType(Me.pictureIconOverlay2, System.ComponentModel.ISupportInitialize).EndInit()
            CType(Me.pictureIconOverlay1, System.ComponentModel.ISupportInitialize).EndInit()
            Me.groupBoxCustomCategories.ResumeLayout(False)
            Me.groupBoxCustomCategories.PerformLayout()
            Me.groupBoxProgressBar.ResumeLayout(False)
            Me.groupBoxProgressBar.PerformLayout()
            CType(Me.trackBar1, System.ComponentModel.ISupportInitialize).EndInit()
            Me.ResumeLayout(False)

        End Sub

		#End Region

		Private groupBoxIconOverlay As System.Windows.Forms.GroupBox
		Private WithEvents labelNoIconOverlay As System.Windows.Forms.Label
		Private WithEvents pictureIconOverlay3 As System.Windows.Forms.PictureBox
		Private WithEvents pictureIconOverlay2 As System.Windows.Forms.PictureBox
		Private label7 As System.Windows.Forms.Label
		Private WithEvents pictureIconOverlay1 As System.Windows.Forms.PictureBox
		Private groupBoxCustomCategories As System.Windows.Forms.GroupBox
		Private label5 As System.Windows.Forms.Label
		Private WithEvents buttonRefreshTaskbarList As System.Windows.Forms.Button
		Private WithEvents button1 As System.Windows.Forms.Button
		Private listBox1 As System.Windows.Forms.ListBox
        Private groupBoxProgressBar As System.Windows.Forms.GroupBox
		Private WithEvents trackBar1 As System.Windows.Forms.TrackBar
		Private label8 As System.Windows.Forms.Label
		Private label6 As System.Windows.Forms.Label
		Private WithEvents comboBoxProgressBarStates As System.Windows.Forms.ComboBox
		Private progressBar1 As System.Windows.Forms.ProgressBar
	End Class
End Namespace