Imports Microsoft.VisualBasic
Imports System
Imports Microsoft.WindowsAPICodePack.Controls.WindowsForms
Namespace Microsoft.WindowsAPICodePack.Samples.ThumbnailBrowserDemo
	Partial Public Class ThumbnailBrowser
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
			Dim resources As New System.ComponentModel.ComponentResourceManager(GetType(ThumbnailBrowser))
			Me.splitContainer1 = New System.Windows.Forms.SplitContainer()
			Me.explorerBrowser1 = New ExplorerBrowser()
			Me.flowLayoutPanel = New System.Windows.Forms.FlowLayoutPanel()
			Me.pictureBox1 = New System.Windows.Forms.PictureBox()
			Me.topNavPanel = New System.Windows.Forms.Panel()
			Me.comboBox1 = New System.Windows.Forms.ComboBox()
			Me.browseLocationButton = New System.Windows.Forms.Button()
			Me.toolStrip1 = New System.Windows.Forms.ToolStrip()
			Me.toolStripSplitButton1 = New System.Windows.Forms.ToolStripSplitButton()
			Me.smallToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
			Me.mediumToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
			Me.largeToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
			Me.extraLargeToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
			Me.panel3 = New System.Windows.Forms.Panel()
			Me.splitContainer1.Panel1.SuspendLayout()
			Me.splitContainer1.Panel2.SuspendLayout()
			Me.splitContainer1.SuspendLayout()
			Me.flowLayoutPanel.SuspendLayout()
			CType(Me.pictureBox1, System.ComponentModel.ISupportInitialize).BeginInit()
			Me.topNavPanel.SuspendLayout()
			Me.toolStrip1.SuspendLayout()
			Me.panel3.SuspendLayout()
			Me.SuspendLayout()
			' 
			' splitContainer1
			' 
			Me.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill
			Me.splitContainer1.Location = New System.Drawing.Point(0, 26)
			Me.splitContainer1.Name = "splitContainer1"
			' 
			' splitContainer1.Panel1
			' 
			Me.splitContainer1.Panel1.Controls.Add(Me.explorerBrowser1)
			' 
			' splitContainer1.Panel2
			' 
			Me.splitContainer1.Panel2.AutoScroll = True
			Me.splitContainer1.Panel2.Controls.Add(Me.flowLayoutPanel)
			Me.splitContainer1.Size = New System.Drawing.Size(881, 567)
			Me.splitContainer1.SplitterDistance = 292
			Me.splitContainer1.TabIndex = 0
			' 
			' explorerBrowser1
			' 
			Me.explorerBrowser1.Dock = System.Windows.Forms.DockStyle.Fill
			Me.explorerBrowser1.Location = New System.Drawing.Point(0, 0)
			Me.explorerBrowser1.Name = "explorerBrowser1"
			Me.explorerBrowser1.Size = New System.Drawing.Size(292, 567)
			Me.explorerBrowser1.TabIndex = 0
			Me.explorerBrowser1.Text = "explorerBrowser1"
			' 
			' flowLayoutPanel
			' 
			Me.flowLayoutPanel.AutoScroll = True
			Me.flowLayoutPanel.Controls.Add(Me.pictureBox1)
			Me.flowLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill
			Me.flowLayoutPanel.Location = New System.Drawing.Point(0, 0)
			Me.flowLayoutPanel.Name = "flowLayoutPanel"
			Me.flowLayoutPanel.Size = New System.Drawing.Size(585, 567)
			Me.flowLayoutPanel.TabIndex = 0
			' 
			' pictureBox1
			' 
			Me.pictureBox1.Location = New System.Drawing.Point(3, 3)
			Me.pictureBox1.Name = "pictureBox1"
			Me.pictureBox1.Size = New System.Drawing.Size(360, 279)
			Me.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize
			Me.pictureBox1.TabIndex = 1
			Me.pictureBox1.TabStop = False
			' 
			' topNavPanel
			' 
			Me.topNavPanel.BackgroundImage = My.Resources.ToolBarGradient
			Me.topNavPanel.Controls.Add(Me.comboBox1)
			Me.topNavPanel.Controls.Add(Me.browseLocationButton)
			Me.topNavPanel.Dock = System.Windows.Forms.DockStyle.Fill
			Me.topNavPanel.Location = New System.Drawing.Point(0, 0)
			Me.topNavPanel.Name = "topNavPanel"
			Me.topNavPanel.Size = New System.Drawing.Size(881, 26)
			Me.topNavPanel.TabIndex = 1
			' 
			' comboBox1
			' 
			Me.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
			Me.comboBox1.FormattingEnabled = True
			Me.comboBox1.Items.AddRange(New Object() { "Thumbnail or Icon", "Thumbnail only", "Icon only"})
			Me.comboBox1.Location = New System.Drawing.Point(73, 2)
			Me.comboBox1.Name = "comboBox1"
			Me.comboBox1.Size = New System.Drawing.Size(121, 21)
			Me.comboBox1.TabIndex = 1
'			Me.comboBox1.SelectedIndexChanged += New System.EventHandler(Me.comboBox1_SelectedIndexChanged)
			' 
			' browseLocationButton
			' 
			Me.browseLocationButton.Anchor = (CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles))
			Me.browseLocationButton.Location = New System.Drawing.Point(803, 3)
			Me.browseLocationButton.Name = "browseLocationButton"
			Me.browseLocationButton.Size = New System.Drawing.Size(75, 23)
			Me.browseLocationButton.TabIndex = 0
			Me.browseLocationButton.Text = "Browse..."
			Me.browseLocationButton.UseVisualStyleBackColor = True
'			Me.browseLocationButton.Click += New System.EventHandler(Me.browseLocationButton_Click)
			' 
			' toolStrip1
			' 
			Me.toolStrip1.Dock = System.Windows.Forms.DockStyle.Left
			Me.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden
			Me.toolStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() { Me.toolStripSplitButton1})
			Me.toolStrip1.Location = New System.Drawing.Point(0, 0)
			Me.toolStrip1.Name = "toolStrip1"
			Me.toolStrip1.Size = New System.Drawing.Size(70, 26)
			Me.toolStrip1.TabIndex = 2
			Me.toolStrip1.Text = "toolStrip1"
			' 
			' toolStripSplitButton1
			' 
			Me.toolStripSplitButton1.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() { Me.smallToolStripMenuItem, Me.mediumToolStripMenuItem, Me.largeToolStripMenuItem, Me.extraLargeToolStripMenuItem})
			Me.toolStripSplitButton1.Image = My.Resources.large
			Me.toolStripSplitButton1.ImageTransparentColor = System.Drawing.Color.Magenta
			Me.toolStripSplitButton1.Name = "toolStripSplitButton1"
			Me.toolStripSplitButton1.Size = New System.Drawing.Size(67, 20)
			Me.toolStripSplitButton1.Text = "Views"
'			Me.toolStripSplitButton1.ButtonClick += New System.EventHandler(Me.toolStripSplitButton1_ButtonClick)
			' 
			' smallToolStripMenuItem
			' 
			Me.smallToolStripMenuItem.Image = My.Resources.small
			Me.smallToolStripMenuItem.Name = "smallToolStripMenuItem"
			Me.smallToolStripMenuItem.Size = New System.Drawing.Size(131, 22)
			Me.smallToolStripMenuItem.Text = "Small"
'			Me.smallToolStripMenuItem.Click += New System.EventHandler(Me.smallToolStripMenuItem_Click)
			' 
			' mediumToolStripMenuItem
			' 
			Me.mediumToolStripMenuItem.Image = My.Resources.medium
			Me.mediumToolStripMenuItem.Name = "mediumToolStripMenuItem"
			Me.mediumToolStripMenuItem.Size = New System.Drawing.Size(131, 22)
			Me.mediumToolStripMenuItem.Text = "Medium"
'			Me.mediumToolStripMenuItem.Click += New System.EventHandler(Me.mediumToolStripMenuItem_Click)
			' 
			' largeToolStripMenuItem
			' 
			Me.largeToolStripMenuItem.Image = My.Resources.large
			Me.largeToolStripMenuItem.Name = "largeToolStripMenuItem"
			Me.largeToolStripMenuItem.Size = New System.Drawing.Size(131, 22)
			Me.largeToolStripMenuItem.Text = "Large"
'			Me.largeToolStripMenuItem.Click += New System.EventHandler(Me.largeToolStripMenuItem_Click)
			' 
			' extraLargeToolStripMenuItem
			' 
			Me.extraLargeToolStripMenuItem.Image = My.Resources.extralarge
			Me.extraLargeToolStripMenuItem.Name = "extraLargeToolStripMenuItem"
			Me.extraLargeToolStripMenuItem.Size = New System.Drawing.Size(131, 22)
			Me.extraLargeToolStripMenuItem.Text = "Extra Large"
'			Me.extraLargeToolStripMenuItem.Click += New System.EventHandler(Me.extraLargeToolStripMenuItem_Click)
			' 
			' panel3
			' 
			Me.panel3.Controls.Add(Me.toolStrip1)
			Me.panel3.Controls.Add(Me.topNavPanel)
			Me.panel3.Dock = System.Windows.Forms.DockStyle.Top
			Me.panel3.Location = New System.Drawing.Point(0, 0)
			Me.panel3.Name = "panel3"
			Me.panel3.Size = New System.Drawing.Size(881, 26)
			Me.panel3.TabIndex = 2
			' 
			' ThumbnailBrowser
			' 
			Me.AutoScaleDimensions = New System.Drawing.SizeF(6F, 13F)
			Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
			Me.ClientSize = New System.Drawing.Size(881, 593)
			Me.Controls.Add(Me.splitContainer1)
			Me.Controls.Add(Me.panel3)
			Me.Icon = (CType(resources.GetObject("$this.Icon"), System.Drawing.Icon))
			Me.Name = "ThumbnailBrowser"
			Me.Text = "Thumbnail/Icon Browser Demo"
			Me.splitContainer1.Panel1.ResumeLayout(False)
			Me.splitContainer1.Panel2.ResumeLayout(False)
			Me.splitContainer1.ResumeLayout(False)
			Me.flowLayoutPanel.ResumeLayout(False)
			Me.flowLayoutPanel.PerformLayout()
			CType(Me.pictureBox1, System.ComponentModel.ISupportInitialize).EndInit()
			Me.topNavPanel.ResumeLayout(False)
			Me.toolStrip1.ResumeLayout(False)
			Me.toolStrip1.PerformLayout()
			Me.panel3.ResumeLayout(False)
			Me.panel3.PerformLayout()
			Me.ResumeLayout(False)

		End Sub

		#End Region

		Private splitContainer1 As System.Windows.Forms.SplitContainer
		Private explorerBrowser1 As Microsoft.WindowsAPICodePack.Controls.WindowsForms.ExplorerBrowser
		Private topNavPanel As System.Windows.Forms.Panel
		Private WithEvents browseLocationButton As System.Windows.Forms.Button
		Private toolStrip1 As System.Windows.Forms.ToolStrip
		Private WithEvents toolStripSplitButton1 As System.Windows.Forms.ToolStripSplitButton
		Private WithEvents smallToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
		Private WithEvents mediumToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
		Private WithEvents largeToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
		Private WithEvents extraLargeToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
		Private panel3 As System.Windows.Forms.Panel
		Private flowLayoutPanel As System.Windows.Forms.FlowLayoutPanel
		Private pictureBox1 As System.Windows.Forms.PictureBox
		Private WithEvents comboBox1 As System.Windows.Forms.ComboBox
	End Class
End Namespace

