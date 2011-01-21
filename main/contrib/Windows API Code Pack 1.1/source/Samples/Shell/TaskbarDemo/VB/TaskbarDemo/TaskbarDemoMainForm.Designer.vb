Imports Microsoft.VisualBasic
Imports System
Namespace TaskbarDemo
	Partial Public Class TaskbarDemoMainForm
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
			Dim resources As New System.ComponentModel.ComponentResourceManager(GetType(TaskbarDemoMainForm))
			Me.menuStrip1 = New System.Windows.Forms.MenuStrip()
			Me.fileToolStripMenuItem1 = New System.Windows.Forms.ToolStripMenuItem()
			Me.newToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
			Me.openToolStripMenuItem1 = New System.Windows.Forms.ToolStripMenuItem()
			Me.saveToolStripMenuItem1 = New System.Windows.Forms.ToolStripMenuItem()
			Me.administrationToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
			Me.registerFileTypeToolStripMenuItem1 = New System.Windows.Forms.ToolStripMenuItem()
			Me.unregisterFileTypeToolStripMenuItem1 = New System.Windows.Forms.ToolStripMenuItem()
			Me.fileToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
			Me.openToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
			Me.saveToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
			Me.administrativeToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
			Me.registerFileTypeToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
			Me.unregisterFileTypeToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
			Me.statusStrip1 = New System.Windows.Forms.StatusStrip()
			Me.toolStripStatusLabel1 = New System.Windows.Forms.ToolStripStatusLabel()
			Me.groupBoxKnownCategories = New System.Windows.Forms.GroupBox()
			Me.numericUpDownKnownCategoryLocation = New System.Windows.Forms.NumericUpDown()
			Me.comboBoxKnownCategoryType = New System.Windows.Forms.ComboBox()
			Me.label2 = New System.Windows.Forms.Label()
			Me.label1 = New System.Windows.Forms.Label()
			Me.groupBoxIconOverlay = New System.Windows.Forms.GroupBox()
			Me.labelNoIconOverlay = New System.Windows.Forms.Label()
			Me.pictureIconOverlay3 = New System.Windows.Forms.PictureBox()
			Me.pictureIconOverlay2 = New System.Windows.Forms.PictureBox()
			Me.label7 = New System.Windows.Forms.Label()
			Me.pictureIconOverlay1 = New System.Windows.Forms.PictureBox()
			Me.groupBoxCustomCategories = New System.Windows.Forms.GroupBox()
			Me.buttonCategoryOneRename = New System.Windows.Forms.Button()
			Me.label5 = New System.Windows.Forms.Label()
			Me.label4 = New System.Windows.Forms.Label()
			Me.label3 = New System.Windows.Forms.Label()
			Me.buttonCategoryTwoAddLink = New System.Windows.Forms.Button()
			Me.buttonCategoryOneAddLink = New System.Windows.Forms.Button()
			Me.buttonUserTasksAddTasks = New System.Windows.Forms.Button()
			Me.groupBoxProgressBar = New System.Windows.Forms.GroupBox()
			Me.trackBar1 = New System.Windows.Forms.TrackBar()
			Me.label8 = New System.Windows.Forms.Label()
			Me.label6 = New System.Windows.Forms.Label()
			Me.comboBoxProgressBarStates = New System.Windows.Forms.ComboBox()
			Me.progressBar1 = New System.Windows.Forms.ProgressBar()
			Me.buttonRefreshTaskbarList = New System.Windows.Forms.Button()
			Me.menuStrip1.SuspendLayout()
			Me.groupBoxKnownCategories.SuspendLayout()
			CType(Me.numericUpDownKnownCategoryLocation, System.ComponentModel.ISupportInitialize).BeginInit()
			Me.groupBoxIconOverlay.SuspendLayout()
			CType(Me.pictureIconOverlay3, System.ComponentModel.ISupportInitialize).BeginInit()
			CType(Me.pictureIconOverlay2, System.ComponentModel.ISupportInitialize).BeginInit()
			CType(Me.pictureIconOverlay1, System.ComponentModel.ISupportInitialize).BeginInit()
			Me.groupBoxCustomCategories.SuspendLayout()
			Me.groupBoxProgressBar.SuspendLayout()
			CType(Me.trackBar1, System.ComponentModel.ISupportInitialize).BeginInit()
			Me.SuspendLayout()
			' 
			' menuStrip1
			' 
			Me.menuStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() { Me.fileToolStripMenuItem1, Me.administrationToolStripMenuItem})
			Me.menuStrip1.Location = New System.Drawing.Point(0, 0)
			Me.menuStrip1.Name = "menuStrip1"
			Me.menuStrip1.Size = New System.Drawing.Size(327, 24)
			Me.menuStrip1.TabIndex = 0
			Me.menuStrip1.Text = "menuStrip1"
			' 
			' fileToolStripMenuItem1
			' 
			Me.fileToolStripMenuItem1.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() { Me.newToolStripMenuItem, Me.openToolStripMenuItem1, Me.saveToolStripMenuItem1})
			Me.fileToolStripMenuItem1.Name = "fileToolStripMenuItem1"
			Me.fileToolStripMenuItem1.Size = New System.Drawing.Size(37, 20)
			Me.fileToolStripMenuItem1.Text = "&File"
			' 
			' newToolStripMenuItem
			' 
			Me.newToolStripMenuItem.Name = "newToolStripMenuItem"
			Me.newToolStripMenuItem.Size = New System.Drawing.Size(176, 22)
			Me.newToolStripMenuItem.Text = "&New Child Window"
'			Me.newToolStripMenuItem.Click += New System.EventHandler(Me.newToolStripMenuItem_Click)
			' 
			' openToolStripMenuItem1
			' 
			Me.openToolStripMenuItem1.Name = "openToolStripMenuItem1"
			Me.openToolStripMenuItem1.Size = New System.Drawing.Size(176, 22)
			Me.openToolStripMenuItem1.Text = "&Open"
'			Me.openToolStripMenuItem1.Click += New System.EventHandler(Me.openToolStripMenuItem_Click)
			' 
			' saveToolStripMenuItem1
			' 
			Me.saveToolStripMenuItem1.Name = "saveToolStripMenuItem1"
			Me.saveToolStripMenuItem1.Size = New System.Drawing.Size(176, 22)
			Me.saveToolStripMenuItem1.Text = "&Save"
'			Me.saveToolStripMenuItem1.Click += New System.EventHandler(Me.saveToolStripMenuItem_Click)
			' 
			' administrationToolStripMenuItem
			' 
			Me.administrationToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() { Me.registerFileTypeToolStripMenuItem1, Me.unregisterFileTypeToolStripMenuItem1})
			Me.administrationToolStripMenuItem.Name = "administrationToolStripMenuItem"
			Me.administrationToolStripMenuItem.Size = New System.Drawing.Size(98, 20)
			Me.administrationToolStripMenuItem.Text = "&Administration"
			' 
			' registerFileTypeToolStripMenuItem1
			' 
			Me.registerFileTypeToolStripMenuItem1.Name = "registerFileTypeToolStripMenuItem1"
			Me.registerFileTypeToolStripMenuItem1.Size = New System.Drawing.Size(178, 22)
			Me.registerFileTypeToolStripMenuItem1.Text = "Register File Type"
'			Me.registerFileTypeToolStripMenuItem1.Click += New System.EventHandler(Me.registerFileTypeToolStripMenuItem_Click)
			' 
			' unregisterFileTypeToolStripMenuItem1
			' 
			Me.unregisterFileTypeToolStripMenuItem1.Name = "unregisterFileTypeToolStripMenuItem1"
			Me.unregisterFileTypeToolStripMenuItem1.Size = New System.Drawing.Size(178, 22)
			Me.unregisterFileTypeToolStripMenuItem1.Text = "Unregister File Type"
'			Me.unregisterFileTypeToolStripMenuItem1.Click += New System.EventHandler(Me.unregisterFileTypeToolStripMenuItem_Click)
			' 
			' fileToolStripMenuItem
			' 
			Me.fileToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() { Me.openToolStripMenuItem, Me.saveToolStripMenuItem})
			Me.fileToolStripMenuItem.Name = "fileToolStripMenuItem"
			Me.fileToolStripMenuItem.Size = New System.Drawing.Size(37, 20)
			Me.fileToolStripMenuItem.Text = "&File"
			' 
			' openToolStripMenuItem
			' 
			Me.openToolStripMenuItem.Name = "openToolStripMenuItem"
			Me.openToolStripMenuItem.Size = New System.Drawing.Size(103, 22)
			Me.openToolStripMenuItem.Text = "&Open"
'			Me.openToolStripMenuItem.Click += New System.EventHandler(Me.openToolStripMenuItem_Click)
			' 
			' saveToolStripMenuItem
			' 
			Me.saveToolStripMenuItem.Name = "saveToolStripMenuItem"
			Me.saveToolStripMenuItem.Size = New System.Drawing.Size(103, 22)
			Me.saveToolStripMenuItem.Text = "&Save"
'			Me.saveToolStripMenuItem.Click += New System.EventHandler(Me.saveToolStripMenuItem_Click)
			' 
			' administrativeToolStripMenuItem
			' 
			Me.administrativeToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() { Me.registerFileTypeToolStripMenuItem, Me.unregisterFileTypeToolStripMenuItem})
			Me.administrativeToolStripMenuItem.Name = "administrativeToolStripMenuItem"
			Me.administrativeToolStripMenuItem.Size = New System.Drawing.Size(96, 20)
			Me.administrativeToolStripMenuItem.Text = "&Administrative"
			' 
			' registerFileTypeToolStripMenuItem
			' 
			Me.registerFileTypeToolStripMenuItem.Name = "registerFileTypeToolStripMenuItem"
			Me.registerFileTypeToolStripMenuItem.Size = New System.Drawing.Size(178, 22)
			Me.registerFileTypeToolStripMenuItem.Text = "&Register File Type"
'			Me.registerFileTypeToolStripMenuItem.Click += New System.EventHandler(Me.registerFileTypeToolStripMenuItem_Click)
			' 
			' unregisterFileTypeToolStripMenuItem
			' 
			Me.unregisterFileTypeToolStripMenuItem.Name = "unregisterFileTypeToolStripMenuItem"
			Me.unregisterFileTypeToolStripMenuItem.Size = New System.Drawing.Size(178, 22)
			Me.unregisterFileTypeToolStripMenuItem.Text = "&Unregister File Type"
'			Me.unregisterFileTypeToolStripMenuItem.Click += New System.EventHandler(Me.unregisterFileTypeToolStripMenuItem_Click)
			' 
			' statusStrip1
			' 
			Me.statusStrip1.Location = New System.Drawing.Point(0, 488)
			Me.statusStrip1.Name = "statusStrip1"
			Me.statusStrip1.Size = New System.Drawing.Size(327, 22)
			Me.statusStrip1.TabIndex = 9
			Me.statusStrip1.Text = "statusStrip1"
			' 
			' toolStripStatusLabel1
			' 
			Me.toolStripStatusLabel1.Name = "toolStripStatusLabel1"
			Me.toolStripStatusLabel1.Size = New System.Drawing.Size(118, 17)
			Me.toolStripStatusLabel1.Text = "toolStripStatusLabel1"
			' 
			' groupBoxKnownCategories
			' 
			Me.groupBoxKnownCategories.Controls.Add(Me.numericUpDownKnownCategoryLocation)
			Me.groupBoxKnownCategories.Controls.Add(Me.comboBoxKnownCategoryType)
			Me.groupBoxKnownCategories.Controls.Add(Me.label2)
			Me.groupBoxKnownCategories.Controls.Add(Me.label1)
			Me.groupBoxKnownCategories.Location = New System.Drawing.Point(12, 27)
			Me.groupBoxKnownCategories.Name = "groupBoxKnownCategories"
			Me.groupBoxKnownCategories.Size = New System.Drawing.Size(305, 68)
			Me.groupBoxKnownCategories.TabIndex = 7
			Me.groupBoxKnownCategories.TabStop = False
			Me.groupBoxKnownCategories.Text = "Known Categories"
			' 
			' numericUpDownKnownCategoryLocation
			' 
			Me.numericUpDownKnownCategoryLocation.Location = New System.Drawing.Point(156, 38)
			Me.numericUpDownKnownCategoryLocation.Name = "numericUpDownKnownCategoryLocation"
			Me.numericUpDownKnownCategoryLocation.Size = New System.Drawing.Size(120, 20)
			Me.numericUpDownKnownCategoryLocation.TabIndex = 12
'			Me.numericUpDownKnownCategoryLocation.ValueChanged += New System.EventHandler(Me.numericUpDownKnownCategoryLocation_ValueChanged)
			' 
			' comboBoxKnownCategoryType
			' 
			Me.comboBoxKnownCategoryType.FormattingEnabled = True
			Me.comboBoxKnownCategoryType.Items.AddRange(New Object() { "None", "Recent", "Frequent"})
			Me.comboBoxKnownCategoryType.Location = New System.Drawing.Point(5, 37)
			Me.comboBoxKnownCategoryType.Name = "comboBoxKnownCategoryType"
			Me.comboBoxKnownCategoryType.Size = New System.Drawing.Size(121, 21)
			Me.comboBoxKnownCategoryType.TabIndex = 11
'			Me.comboBoxKnownCategoryType.SelectedIndexChanged += New System.EventHandler(Me.comboBoxKnownCategoryType_SelectedIndexChanged)
			' 
			' label2
			' 
			Me.label2.AutoSize = True
			Me.label2.Location = New System.Drawing.Point(5, 21)
			Me.label2.Name = "label2"
			Me.label2.Size = New System.Drawing.Size(37, 13)
			Me.label2.TabIndex = 9
			Me.label2.Text = "Show:"
			' 
			' label1
			' 
			Me.label1.AutoSize = True
			Me.label1.Location = New System.Drawing.Point(153, 20)
			Me.label1.Name = "label1"
			Me.label1.Size = New System.Drawing.Size(51, 13)
			Me.label1.TabIndex = 7
			Me.label1.Text = "Location:"
			' 
			' groupBoxIconOverlay
			' 
			Me.groupBoxIconOverlay.Controls.Add(Me.labelNoIconOverlay)
			Me.groupBoxIconOverlay.Controls.Add(Me.pictureIconOverlay3)
			Me.groupBoxIconOverlay.Controls.Add(Me.pictureIconOverlay2)
			Me.groupBoxIconOverlay.Controls.Add(Me.label7)
			Me.groupBoxIconOverlay.Controls.Add(Me.pictureIconOverlay1)
			Me.groupBoxIconOverlay.Location = New System.Drawing.Point(12, 361)
			Me.groupBoxIconOverlay.Name = "groupBoxIconOverlay"
			Me.groupBoxIconOverlay.Size = New System.Drawing.Size(305, 85)
			Me.groupBoxIconOverlay.TabIndex = 11
			Me.groupBoxIconOverlay.TabStop = False
			Me.groupBoxIconOverlay.Text = "Icon Overlay"
			' 
			' labelNoIconOverlay
			' 
			Me.labelNoIconOverlay.Location = New System.Drawing.Point(9, 50)
			Me.labelNoIconOverlay.Name = "labelNoIconOverlay"
			Me.labelNoIconOverlay.Size = New System.Drawing.Size(35, 13)
			Me.labelNoIconOverlay.TabIndex = 0
			Me.labelNoIconOverlay.Text = "None"
'			Me.labelNoIconOverlay.Click += New System.EventHandler(Me.labelNoIconOverlay_Click)
			' 
			' pictureIconOverlay3
			' 
			Me.pictureIconOverlay3.Image = (CType(resources.GetObject("pictureIconOverlay3.Image"), System.Drawing.Image))
			Me.pictureIconOverlay3.Location = New System.Drawing.Point(126, 41)
			Me.pictureIconOverlay3.Name = "pictureIconOverlay3"
			Me.pictureIconOverlay3.Size = New System.Drawing.Size(32, 32)
			Me.pictureIconOverlay3.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage
			Me.pictureIconOverlay3.TabIndex = 3
			Me.pictureIconOverlay3.TabStop = False
'			Me.pictureIconOverlay3.Click += New System.EventHandler(Me.pictureIconOverlay3_Click)
			' 
			' pictureIconOverlay2
			' 
			Me.pictureIconOverlay2.Image = (CType(resources.GetObject("pictureIconOverlay2.Image"), System.Drawing.Image))
			Me.pictureIconOverlay2.Location = New System.Drawing.Point(88, 41)
			Me.pictureIconOverlay2.Name = "pictureIconOverlay2"
			Me.pictureIconOverlay2.Size = New System.Drawing.Size(32, 32)
			Me.pictureIconOverlay2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage
			Me.pictureIconOverlay2.TabIndex = 2
			Me.pictureIconOverlay2.TabStop = False
'			Me.pictureIconOverlay2.Click += New System.EventHandler(Me.pictureIconOverlay2_Click)
			' 
			' label7
			' 
			Me.label7.AutoSize = True
			Me.label7.Location = New System.Drawing.Point(9, 20)
			Me.label7.Name = "label7"
			Me.label7.Size = New System.Drawing.Size(209, 13)
			Me.label7.TabIndex = 1
			Me.label7.Text = "Select an image to overlay on the task bar:"
			' 
			' pictureIconOverlay1
			' 
			Me.pictureIconOverlay1.Image = (CType(resources.GetObject("pictureIconOverlay1.Image"), System.Drawing.Image))
			Me.pictureIconOverlay1.Location = New System.Drawing.Point(50, 41)
			Me.pictureIconOverlay1.Name = "pictureIconOverlay1"
			Me.pictureIconOverlay1.Size = New System.Drawing.Size(32, 32)
			Me.pictureIconOverlay1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage
			Me.pictureIconOverlay1.TabIndex = 0
			Me.pictureIconOverlay1.TabStop = False
'			Me.pictureIconOverlay1.Click += New System.EventHandler(Me.pictureIconOverlay1_Click)
			' 
			' groupBoxCustomCategories
			' 
			Me.groupBoxCustomCategories.Controls.Add(Me.buttonCategoryOneRename)
			Me.groupBoxCustomCategories.Controls.Add(Me.label5)
			Me.groupBoxCustomCategories.Controls.Add(Me.label4)
			Me.groupBoxCustomCategories.Controls.Add(Me.label3)
			Me.groupBoxCustomCategories.Controls.Add(Me.buttonCategoryTwoAddLink)
			Me.groupBoxCustomCategories.Controls.Add(Me.buttonCategoryOneAddLink)
			Me.groupBoxCustomCategories.Controls.Add(Me.buttonUserTasksAddTasks)
			Me.groupBoxCustomCategories.Location = New System.Drawing.Point(14, 101)
			Me.groupBoxCustomCategories.Name = "groupBoxCustomCategories"
			Me.groupBoxCustomCategories.Size = New System.Drawing.Size(304, 104)
			Me.groupBoxCustomCategories.TabIndex = 8
			Me.groupBoxCustomCategories.TabStop = False
			Me.groupBoxCustomCategories.Text = "Custom Categories"
			' 
			' buttonCategoryOneRename
			' 
			Me.buttonCategoryOneRename.Location = New System.Drawing.Point(207, 16)
			Me.buttonCategoryOneRename.Name = "buttonCategoryOneRename"
			Me.buttonCategoryOneRename.Size = New System.Drawing.Size(88, 23)
			Me.buttonCategoryOneRename.TabIndex = 10
			Me.buttonCategoryOneRename.Text = "Change Name"
			Me.buttonCategoryOneRename.UseVisualStyleBackColor = True
'			Me.buttonCategoryOneRename.Click += New System.EventHandler(Me.buttonCategoryOneRename_Click)
			' 
			' label5
			' 
			Me.label5.AutoSize = True
			Me.label5.Location = New System.Drawing.Point(8, 79)
			Me.label5.Name = "label5"
			Me.label5.Size = New System.Drawing.Size(64, 13)
			Me.label5.TabIndex = 8
			Me.label5.Text = "User Tasks:"
			' 
			' label4
			' 
			Me.label4.AutoSize = True
			Me.label4.Location = New System.Drawing.Point(8, 50)
			Me.label4.Name = "label4"
			Me.label4.Size = New System.Drawing.Size(99, 13)
			Me.label4.TabIndex = 7
			Me.label4.Text = "Custom Category 2:"
			' 
			' label3
			' 
			Me.label3.AutoSize = True
			Me.label3.Location = New System.Drawing.Point(8, 21)
			Me.label3.Name = "label3"
			Me.label3.Size = New System.Drawing.Size(99, 13)
			Me.label3.TabIndex = 6
			Me.label3.Text = "Custom Category 1:"
			' 
			' buttonCategoryTwoAddLink
			' 
			Me.buttonCategoryTwoAddLink.Location = New System.Drawing.Point(113, 45)
			Me.buttonCategoryTwoAddLink.Name = "buttonCategoryTwoAddLink"
			Me.buttonCategoryTwoAddLink.Size = New System.Drawing.Size(88, 23)
			Me.buttonCategoryTwoAddLink.TabIndex = 4
			Me.buttonCategoryTwoAddLink.Text = "Add Item"
			Me.buttonCategoryTwoAddLink.UseVisualStyleBackColor = True
'			Me.buttonCategoryTwoAddLink.Click += New System.EventHandler(Me.buttonCategoryTwoAddLink_Click)
			' 
			' buttonCategoryOneAddLink
			' 
			Me.buttonCategoryOneAddLink.Location = New System.Drawing.Point(113, 16)
			Me.buttonCategoryOneAddLink.Name = "buttonCategoryOneAddLink"
			Me.buttonCategoryOneAddLink.Size = New System.Drawing.Size(88, 23)
			Me.buttonCategoryOneAddLink.TabIndex = 3
			Me.buttonCategoryOneAddLink.Text = "Add Item"
			Me.buttonCategoryOneAddLink.UseVisualStyleBackColor = True
'			Me.buttonCategoryOneAddLink.Click += New System.EventHandler(Me.buttonCategoryOneAddLink_Click)
			' 
			' buttonUserTasksAddTasks
			' 
			Me.buttonUserTasksAddTasks.Location = New System.Drawing.Point(113, 74)
			Me.buttonUserTasksAddTasks.Name = "buttonUserTasksAddTasks"
			Me.buttonUserTasksAddTasks.Size = New System.Drawing.Size(88, 23)
			Me.buttonUserTasksAddTasks.TabIndex = 5
			Me.buttonUserTasksAddTasks.Text = "Add Tasks"
			Me.buttonUserTasksAddTasks.UseVisualStyleBackColor = True
'			Me.buttonUserTasksAddTasks.Click += New System.EventHandler(Me.buttonUserTasksAddTasks_Click)
			' 
			' groupBoxProgressBar
			' 
			Me.groupBoxProgressBar.Controls.Add(Me.trackBar1)
			Me.groupBoxProgressBar.Controls.Add(Me.label8)
			Me.groupBoxProgressBar.Controls.Add(Me.label6)
			Me.groupBoxProgressBar.Controls.Add(Me.comboBoxProgressBarStates)
			Me.groupBoxProgressBar.Controls.Add(Me.progressBar1)
			Me.groupBoxProgressBar.Location = New System.Drawing.Point(14, 211)
			Me.groupBoxProgressBar.Name = "groupBoxProgressBar"
			Me.groupBoxProgressBar.Size = New System.Drawing.Size(305, 144)
			Me.groupBoxProgressBar.TabIndex = 10
			Me.groupBoxProgressBar.TabStop = False
			Me.groupBoxProgressBar.Text = "Progress Bar"
			' 
			' trackBar1
			' 
			Me.trackBar1.LargeChange = 25
			Me.trackBar1.Location = New System.Drawing.Point(92, 59)
			Me.trackBar1.Maximum = 100
			Me.trackBar1.Name = "trackBar1"
			Me.trackBar1.Size = New System.Drawing.Size(203, 45)
			Me.trackBar1.SmallChange = 10
			Me.trackBar1.TabIndex = 12
			Me.trackBar1.TickStyle = System.Windows.Forms.TickStyle.None
'			Me.trackBar1.Scroll += New System.EventHandler(Me.trackBar1_Scroll)
			' 
			' label8
			' 
			Me.label8.AutoSize = True
			Me.label8.Location = New System.Drawing.Point(6, 71)
			Me.label8.Name = "label8"
			Me.label8.Size = New System.Drawing.Size(77, 13)
			Me.label8.TabIndex = 11
			Me.label8.Text = "Update value: "
			' 
			' label6
			' 
			Me.label6.AutoSize = True
			Me.label6.Location = New System.Drawing.Point(6, 25)
			Me.label6.Name = "label6"
			Me.label6.Size = New System.Drawing.Size(35, 13)
			Me.label6.TabIndex = 3
			Me.label6.Text = "State:"
			' 
			' comboBoxProgressBarStates
			' 
			Me.comboBoxProgressBarStates.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
			Me.comboBoxProgressBarStates.FormattingEnabled = True
			Me.comboBoxProgressBarStates.Location = New System.Drawing.Point(47, 22)
			Me.comboBoxProgressBarStates.Name = "comboBoxProgressBarStates"
			Me.comboBoxProgressBarStates.Size = New System.Drawing.Size(154, 21)
			Me.comboBoxProgressBarStates.TabIndex = 2
'			Me.comboBoxProgressBarStates.SelectedIndexChanged += New System.EventHandler(Me.comboBoxProgressBarStates_SelectedIndexChanged)
			' 
			' progressBar1
			' 
			Me.progressBar1.Location = New System.Drawing.Point(11, 110)
			Me.progressBar1.Name = "progressBar1"
			Me.progressBar1.Size = New System.Drawing.Size(285, 23)
			Me.progressBar1.Step = 5
			Me.progressBar1.TabIndex = 0
			' 
			' buttonRefreshTaskbarList
			' 
			Me.buttonRefreshTaskbarList.Location = New System.Drawing.Point(100, 456)
			Me.buttonRefreshTaskbarList.Name = "buttonRefreshTaskbarList"
			Me.buttonRefreshTaskbarList.Size = New System.Drawing.Size(132, 23)
			Me.buttonRefreshTaskbarList.TabIndex = 12
			Me.buttonRefreshTaskbarList.Text = "Refresh JumpList"
			Me.buttonRefreshTaskbarList.UseVisualStyleBackColor = True
'			Me.buttonRefreshTaskbarList.Click += New System.EventHandler(Me.buttonRefreshTaskbarList_Click)
			' 
			' TaskbarDemoMainForm
			' 
			Me.AutoScaleDimensions = New System.Drawing.SizeF(6F, 13F)
			Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
			Me.ClientSize = New System.Drawing.Size(327, 510)
			Me.Controls.Add(Me.buttonRefreshTaskbarList)
			Me.Controls.Add(Me.groupBoxKnownCategories)
			Me.Controls.Add(Me.groupBoxIconOverlay)
			Me.Controls.Add(Me.groupBoxCustomCategories)
			Me.Controls.Add(Me.statusStrip1)
			Me.Controls.Add(Me.groupBoxProgressBar)
			Me.Controls.Add(Me.menuStrip1)
			Me.Icon = (CType(resources.GetObject("$this.Icon"), System.Drawing.Icon))
			Me.MainMenuStrip = Me.menuStrip1
			Me.Name = "TaskbarDemoMainForm"
			Me.Text = "Taskbar Demo"
			Me.menuStrip1.ResumeLayout(False)
			Me.menuStrip1.PerformLayout()
			Me.groupBoxKnownCategories.ResumeLayout(False)
			Me.groupBoxKnownCategories.PerformLayout()
			CType(Me.numericUpDownKnownCategoryLocation, System.ComponentModel.ISupportInitialize).EndInit()
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
			Me.PerformLayout()

		End Sub

		#End Region

		Private menuStrip1 As System.Windows.Forms.MenuStrip
		Private fileToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
		Private WithEvents openToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
		Private WithEvents saveToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
		Private administrativeToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
		Private WithEvents registerFileTypeToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
		Private WithEvents unregisterFileTypeToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
		Private statusStrip1 As System.Windows.Forms.StatusStrip
		Private toolStripStatusLabel1 As System.Windows.Forms.ToolStripStatusLabel
		Private groupBoxKnownCategories As System.Windows.Forms.GroupBox
		Private label2 As System.Windows.Forms.Label
		Private label1 As System.Windows.Forms.Label
		Private groupBoxIconOverlay As System.Windows.Forms.GroupBox
		Private WithEvents labelNoIconOverlay As System.Windows.Forms.Label
		Private WithEvents pictureIconOverlay3 As System.Windows.Forms.PictureBox
		Private WithEvents pictureIconOverlay2 As System.Windows.Forms.PictureBox
		Private label7 As System.Windows.Forms.Label
		Private WithEvents pictureIconOverlay1 As System.Windows.Forms.PictureBox
		Private groupBoxCustomCategories As System.Windows.Forms.GroupBox
		Private WithEvents buttonCategoryOneRename As System.Windows.Forms.Button
		Private label5 As System.Windows.Forms.Label
		Private label4 As System.Windows.Forms.Label
		Private label3 As System.Windows.Forms.Label
		Private WithEvents buttonCategoryTwoAddLink As System.Windows.Forms.Button
		Private WithEvents buttonCategoryOneAddLink As System.Windows.Forms.Button
		Private WithEvents buttonUserTasksAddTasks As System.Windows.Forms.Button
		Private groupBoxProgressBar As System.Windows.Forms.GroupBox
		Private label6 As System.Windows.Forms.Label
		Private WithEvents comboBoxProgressBarStates As System.Windows.Forms.ComboBox
		Private progressBar1 As System.Windows.Forms.ProgressBar
		Private WithEvents comboBoxKnownCategoryType As System.Windows.Forms.ComboBox
		Private fileToolStripMenuItem1 As System.Windows.Forms.ToolStripMenuItem
		Private WithEvents openToolStripMenuItem1 As System.Windows.Forms.ToolStripMenuItem
		Private WithEvents saveToolStripMenuItem1 As System.Windows.Forms.ToolStripMenuItem
		Private administrationToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
		Private WithEvents registerFileTypeToolStripMenuItem1 As System.Windows.Forms.ToolStripMenuItem
		Private WithEvents unregisterFileTypeToolStripMenuItem1 As System.Windows.Forms.ToolStripMenuItem
		Private WithEvents numericUpDownKnownCategoryLocation As System.Windows.Forms.NumericUpDown
		Private WithEvents buttonRefreshTaskbarList As System.Windows.Forms.Button
		Private WithEvents newToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
		Private WithEvents trackBar1 As System.Windows.Forms.TrackBar
		Private label8 As System.Windows.Forms.Label
	End Class
End Namespace

