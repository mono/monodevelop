Imports Microsoft.VisualBasic
Imports System
Namespace Microsoft.WindowsAPICodePack.Samples.AppRestartRecoveryDemo
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
            Me.components = New System.ComponentModel.Container
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Form1))
            Me.textBox1 = New System.Windows.Forms.TextBox
            Me.menuStrip1 = New System.Windows.Forms.MenuStrip
            Me.fileToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
            Me.newToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
            Me.openToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
            Me.toolStripSeparator = New System.Windows.Forms.ToolStripSeparator
            Me.saveToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
            Me.toolStripSeparator2 = New System.Windows.Forms.ToolStripSeparator
            Me.exitToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
            Me.editToolStripMenuItem1 = New System.Windows.Forms.ToolStripMenuItem
            Me.undoToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
            Me.toolStripSeparator8 = New System.Windows.Forms.ToolStripSeparator
            Me.cutToolStripMenuItem1 = New System.Windows.Forms.ToolStripMenuItem
            Me.copyToolStripMenuItem1 = New System.Windows.Forms.ToolStripMenuItem
            Me.pasteToolStripMenuItem1 = New System.Windows.Forms.ToolStripMenuItem
            Me.toolStripSeparator9 = New System.Windows.Forms.ToolStripSeparator
            Me.selectAllToolStripMenuItem1 = New System.Windows.Forms.ToolStripMenuItem
            Me.appRestartRecoveryToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
            Me.crashToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
            Me.helpToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
            Me.toolStripSeparator5 = New System.Windows.Forms.ToolStripSeparator
            Me.aboutToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
            Me.statusStrip1 = New System.Windows.Forms.StatusStrip
            Me.timerLabel = New System.Windows.Forms.ToolStripStatusLabel
            Me.statusLabel = New System.Windows.Forms.ToolStripStatusLabel
            Me.timer1 = New System.Windows.Forms.Timer(Me.components)
            Me.menuStrip1.SuspendLayout()
            Me.statusStrip1.SuspendLayout()
            Me.SuspendLayout()
            '
            'textBox1
            '
            Me.textBox1.Dock = System.Windows.Forms.DockStyle.Fill
            Me.textBox1.Location = New System.Drawing.Point(0, 24)
            Me.textBox1.Multiline = True
            Me.textBox1.Name = "textBox1"
            Me.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Both
            Me.textBox1.Size = New System.Drawing.Size(675, 261)
            Me.textBox1.TabIndex = 0
            '
            'menuStrip1
            '
            Me.menuStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.fileToolStripMenuItem, Me.editToolStripMenuItem1, Me.appRestartRecoveryToolStripMenuItem, Me.helpToolStripMenuItem})
            Me.menuStrip1.Location = New System.Drawing.Point(0, 0)
            Me.menuStrip1.Name = "menuStrip1"
            Me.menuStrip1.Size = New System.Drawing.Size(675, 24)
            Me.menuStrip1.TabIndex = 1
            Me.menuStrip1.Text = "menuStrip1"
            '
            'fileToolStripMenuItem
            '
            Me.fileToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.newToolStripMenuItem, Me.openToolStripMenuItem, Me.toolStripSeparator, Me.saveToolStripMenuItem, Me.toolStripSeparator2, Me.exitToolStripMenuItem})
            Me.fileToolStripMenuItem.Name = "fileToolStripMenuItem"
            Me.fileToolStripMenuItem.Size = New System.Drawing.Size(37, 20)
            Me.fileToolStripMenuItem.Text = "&File"
            '
            'newToolStripMenuItem
            '
            Me.newToolStripMenuItem.Image = CType(resources.GetObject("newToolStripMenuItem.Image"), System.Drawing.Image)
            Me.newToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta
            Me.newToolStripMenuItem.Name = "newToolStripMenuItem"
            Me.newToolStripMenuItem.ShortcutKeys = CType((System.Windows.Forms.Keys.Control Or System.Windows.Forms.Keys.N), System.Windows.Forms.Keys)
            Me.newToolStripMenuItem.Size = New System.Drawing.Size(152, 22)
            Me.newToolStripMenuItem.Text = "&New"
            '
            'openToolStripMenuItem
            '
            Me.openToolStripMenuItem.Image = CType(resources.GetObject("openToolStripMenuItem.Image"), System.Drawing.Image)
            Me.openToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta
            Me.openToolStripMenuItem.Name = "openToolStripMenuItem"
            Me.openToolStripMenuItem.ShortcutKeys = CType((System.Windows.Forms.Keys.Control Or System.Windows.Forms.Keys.O), System.Windows.Forms.Keys)
            Me.openToolStripMenuItem.Size = New System.Drawing.Size(152, 22)
            Me.openToolStripMenuItem.Text = "&Open"
            '
            'toolStripSeparator
            '
            Me.toolStripSeparator.Name = "toolStripSeparator"
            Me.toolStripSeparator.Size = New System.Drawing.Size(149, 6)
            '
            'saveToolStripMenuItem
            '
            Me.saveToolStripMenuItem.Image = CType(resources.GetObject("saveToolStripMenuItem.Image"), System.Drawing.Image)
            Me.saveToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta
            Me.saveToolStripMenuItem.Name = "saveToolStripMenuItem"
            Me.saveToolStripMenuItem.ShortcutKeys = CType((System.Windows.Forms.Keys.Control Or System.Windows.Forms.Keys.S), System.Windows.Forms.Keys)
            Me.saveToolStripMenuItem.Size = New System.Drawing.Size(152, 22)
            Me.saveToolStripMenuItem.Text = "&Save"
            '
            'toolStripSeparator2
            '
            Me.toolStripSeparator2.Name = "toolStripSeparator2"
            Me.toolStripSeparator2.Size = New System.Drawing.Size(149, 6)
            '
            'exitToolStripMenuItem
            '
            Me.exitToolStripMenuItem.Name = "exitToolStripMenuItem"
            Me.exitToolStripMenuItem.Size = New System.Drawing.Size(152, 22)
            Me.exitToolStripMenuItem.Text = "E&xit"
            '
            'editToolStripMenuItem1
            '
            Me.editToolStripMenuItem1.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.undoToolStripMenuItem, Me.toolStripSeparator8, Me.cutToolStripMenuItem1, Me.copyToolStripMenuItem1, Me.pasteToolStripMenuItem1, Me.toolStripSeparator9, Me.selectAllToolStripMenuItem1})
            Me.editToolStripMenuItem1.Name = "editToolStripMenuItem1"
            Me.editToolStripMenuItem1.Size = New System.Drawing.Size(39, 20)
            Me.editToolStripMenuItem1.Text = "&Edit"
            '
            'undoToolStripMenuItem
            '
            Me.undoToolStripMenuItem.Name = "undoToolStripMenuItem"
            Me.undoToolStripMenuItem.ShortcutKeys = CType((System.Windows.Forms.Keys.Control Or System.Windows.Forms.Keys.Z), System.Windows.Forms.Keys)
            Me.undoToolStripMenuItem.Size = New System.Drawing.Size(144, 22)
            Me.undoToolStripMenuItem.Text = "&Undo"
            '
            'toolStripSeparator8
            '
            Me.toolStripSeparator8.Name = "toolStripSeparator8"
            Me.toolStripSeparator8.Size = New System.Drawing.Size(141, 6)
            '
            'cutToolStripMenuItem1
            '
            Me.cutToolStripMenuItem1.Image = CType(resources.GetObject("cutToolStripMenuItem1.Image"), System.Drawing.Image)
            Me.cutToolStripMenuItem1.ImageTransparentColor = System.Drawing.Color.Magenta
            Me.cutToolStripMenuItem1.Name = "cutToolStripMenuItem1"
            Me.cutToolStripMenuItem1.Size = New System.Drawing.Size(144, 22)
            Me.cutToolStripMenuItem1.Text = "Cu&t"
            '
            'copyToolStripMenuItem1
            '
            Me.copyToolStripMenuItem1.Image = CType(resources.GetObject("copyToolStripMenuItem1.Image"), System.Drawing.Image)
            Me.copyToolStripMenuItem1.ImageTransparentColor = System.Drawing.Color.Magenta
            Me.copyToolStripMenuItem1.Name = "copyToolStripMenuItem1"
            Me.copyToolStripMenuItem1.Size = New System.Drawing.Size(144, 22)
            Me.copyToolStripMenuItem1.Text = "&Copy"
            '
            'pasteToolStripMenuItem1
            '
            Me.pasteToolStripMenuItem1.Image = CType(resources.GetObject("pasteToolStripMenuItem1.Image"), System.Drawing.Image)
            Me.pasteToolStripMenuItem1.ImageTransparentColor = System.Drawing.Color.Magenta
            Me.pasteToolStripMenuItem1.Name = "pasteToolStripMenuItem1"
            Me.pasteToolStripMenuItem1.Size = New System.Drawing.Size(144, 22)
            Me.pasteToolStripMenuItem1.Text = "&Paste"
            '
            'toolStripSeparator9
            '
            Me.toolStripSeparator9.Name = "toolStripSeparator9"
            Me.toolStripSeparator9.Size = New System.Drawing.Size(141, 6)
            '
            'selectAllToolStripMenuItem1
            '
            Me.selectAllToolStripMenuItem1.Name = "selectAllToolStripMenuItem1"
            Me.selectAllToolStripMenuItem1.Size = New System.Drawing.Size(144, 22)
            Me.selectAllToolStripMenuItem1.Text = "Select &All"
            '
            'appRestartRecoveryToolStripMenuItem
            '
            Me.appRestartRecoveryToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.crashToolStripMenuItem})
            Me.appRestartRecoveryToolStripMenuItem.Name = "appRestartRecoveryToolStripMenuItem"
            Me.appRestartRecoveryToolStripMenuItem.Size = New System.Drawing.Size(131, 20)
            Me.appRestartRecoveryToolStripMenuItem.Text = "&App Restart Recovery"
            '
            'crashToolStripMenuItem
            '
            Me.crashToolStripMenuItem.Name = "crashToolStripMenuItem"
            Me.crashToolStripMenuItem.Size = New System.Drawing.Size(107, 22)
            Me.crashToolStripMenuItem.Text = "&Crash!"
            '
            'helpToolStripMenuItem
            '
            Me.helpToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.toolStripSeparator5, Me.aboutToolStripMenuItem})
            Me.helpToolStripMenuItem.Name = "helpToolStripMenuItem"
            Me.helpToolStripMenuItem.Size = New System.Drawing.Size(44, 20)
            Me.helpToolStripMenuItem.Text = "&Help"
            '
            'toolStripSeparator5
            '
            Me.toolStripSeparator5.Name = "toolStripSeparator5"
            Me.toolStripSeparator5.Size = New System.Drawing.Size(113, 6)
            '
            'aboutToolStripMenuItem
            '
            Me.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem"
            Me.aboutToolStripMenuItem.Size = New System.Drawing.Size(116, 22)
            Me.aboutToolStripMenuItem.Text = "&About..."
            '
            'statusStrip1
            '
            Me.statusStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.timerLabel, Me.statusLabel})
            Me.statusStrip1.Location = New System.Drawing.Point(0, 285)
            Me.statusStrip1.Name = "statusStrip1"
            Me.statusStrip1.Size = New System.Drawing.Size(675, 24)
            Me.statusStrip1.TabIndex = 2
            Me.statusStrip1.Text = "statusStrip1"
            '
            'timerLabel
            '
            Me.timerLabel.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right
            Me.timerLabel.Name = "timerLabel"
            Me.timerLabel.Size = New System.Drawing.Size(42, 19)
            Me.timerLabel.Text = "Timer"
            '
            'statusLabel
            '
            Me.statusLabel.Name = "statusLabel"
            Me.statusLabel.Size = New System.Drawing.Size(39, 19)
            Me.statusLabel.Text = "Status"
            '
            'timer1
            '
            Me.timer1.Enabled = True
            Me.timer1.Interval = 1000
            '
            'Form1
            '
            Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.ClientSize = New System.Drawing.Size(675, 309)
            Me.Controls.Add(Me.textBox1)
            Me.Controls.Add(Me.menuStrip1)
            Me.Controls.Add(Me.statusStrip1)
            Me.MainMenuStrip = Me.menuStrip1
            Me.Name = "Form1"
            Me.Text = "Form1"
            Me.menuStrip1.ResumeLayout(False)
            Me.menuStrip1.PerformLayout()
            Me.statusStrip1.ResumeLayout(False)
            Me.statusStrip1.PerformLayout()
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub

		#End Region

		Private WithEvents textBox1 As System.Windows.Forms.TextBox
		Private menuStrip1 As System.Windows.Forms.MenuStrip
		Private fileToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
        Private WithEvents newToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
		Private WithEvents openToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
		Private toolStripSeparator As System.Windows.Forms.ToolStripSeparator
		Private WithEvents saveToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
		Private toolStripSeparator2 As System.Windows.Forms.ToolStripSeparator
		Private WithEvents exitToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
		Private helpToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
		Private toolStripSeparator5 As System.Windows.Forms.ToolStripSeparator
		Private WithEvents aboutToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
		Private statusStrip1 As System.Windows.Forms.StatusStrip
		Private statusLabel As System.Windows.Forms.ToolStripStatusLabel
		Private appRestartRecoveryToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
		Private WithEvents crashToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
		Private editToolStripMenuItem1 As System.Windows.Forms.ToolStripMenuItem
		Private WithEvents undoToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
		Private toolStripSeparator8 As System.Windows.Forms.ToolStripSeparator
		Private WithEvents cutToolStripMenuItem1 As System.Windows.Forms.ToolStripMenuItem
		Private WithEvents copyToolStripMenuItem1 As System.Windows.Forms.ToolStripMenuItem
		Private WithEvents pasteToolStripMenuItem1 As System.Windows.Forms.ToolStripMenuItem
		Private toolStripSeparator9 As System.Windows.Forms.ToolStripSeparator
		Private WithEvents selectAllToolStripMenuItem1 As System.Windows.Forms.ToolStripMenuItem
		Private timerLabel As System.Windows.Forms.ToolStripStatusLabel
		Private WithEvents timer1 As System.Windows.Forms.Timer

	End Class
End Namespace

