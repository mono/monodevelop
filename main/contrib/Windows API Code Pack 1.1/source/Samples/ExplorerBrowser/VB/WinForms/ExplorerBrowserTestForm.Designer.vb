'Copyright (c) Microsoft Corporation.  All rights reserved.



Imports Microsoft.VisualBasic
Imports System
Namespace Microsoft.WindowsAPICodePack.Samples
	Partial Public Class ExplorerBrowserTestForm
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
            Me.label2 = New System.Windows.Forms.Label
            Me.label3 = New System.Windows.Forms.Label
            Me.itemsTextBox = New System.Windows.Forms.RichTextBox
            Me.label7 = New System.Windows.Forms.Label
            Me.filePathNavigate = New System.Windows.Forms.Button
            Me.filePathEdit = New System.Windows.Forms.TextBox
            Me.knownFolderNavigate = New System.Windows.Forms.Button
            Me.label6 = New System.Windows.Forms.Label
            Me.knownFolderCombo = New System.Windows.Forms.ComboBox
            Me.navigateButton = New System.Windows.Forms.Button
            Me.label5 = New System.Windows.Forms.Label
            Me.pathEdit = New System.Windows.Forms.TextBox
            Me.splitContainer1 = New System.Windows.Forms.SplitContainer
            Me.visibilityPropertyGrid = New System.Windows.Forms.PropertyGrid
            Me.contentPropertyGrid = New System.Windows.Forms.PropertyGrid
            Me.navigationPropertyGrid = New System.Windows.Forms.PropertyGrid
            Me.splitContainer2 = New System.Windows.Forms.SplitContainer
            Me.clearHistoryButton = New System.Windows.Forms.Button
            Me.forwardButton = New System.Windows.Forms.Button
            Me.navigationHistoryCombo = New System.Windows.Forms.ComboBox
            Me.backButton = New System.Windows.Forms.Button
            Me.failNavigationCheckBox = New System.Windows.Forms.CheckBox
            Me.splitContainer3 = New System.Windows.Forms.SplitContainer
            Me.explorerBrowser = New Microsoft.WindowsAPICodePack.Controls.WindowsForms.ExplorerBrowser
            Me.itemsTabControl = New System.Windows.Forms.TabControl
            Me.tabPage1 = New System.Windows.Forms.TabPage
            Me.tabPage2 = New System.Windows.Forms.TabPage
            Me.selectedItemsTextBox = New System.Windows.Forms.RichTextBox
            Me.tabPage3 = New System.Windows.Forms.TabPage
            Me.eventHistoryTextBox = New System.Windows.Forms.RichTextBox
            Me.Label1 = New System.Windows.Forms.Label
            Me.splitContainer1.Panel1.SuspendLayout()
            Me.splitContainer1.Panel2.SuspendLayout()
            Me.splitContainer1.SuspendLayout()
            Me.splitContainer2.Panel1.SuspendLayout()
            Me.splitContainer2.Panel2.SuspendLayout()
            Me.splitContainer2.SuspendLayout()
            Me.splitContainer3.Panel1.SuspendLayout()
            Me.splitContainer3.Panel2.SuspendLayout()
            Me.splitContainer3.SuspendLayout()
            Me.itemsTabControl.SuspendLayout()
            Me.tabPage1.SuspendLayout()
            Me.tabPage2.SuspendLayout()
            Me.tabPage3.SuspendLayout()
            Me.SuspendLayout()
            '
            'label2
            '
            Me.label2.AutoSize = True
            Me.label2.Location = New System.Drawing.Point(3, 494)
            Me.label2.Name = "label2"
            Me.label2.Size = New System.Drawing.Size(83, 13)
            Me.label2.TabIndex = 8
            Me.label2.Text = "Content Options"
            '
            'label3
            '
            Me.label3.AutoSize = True
            Me.label3.Location = New System.Drawing.Point(3, 12)
            Me.label3.Name = "label3"
            Me.label3.Size = New System.Drawing.Size(97, 13)
            Me.label3.TabIndex = 9
            Me.label3.Text = "Navigation Options"
            '
            'itemsTextBox
            '
            Me.itemsTextBox.Dock = System.Windows.Forms.DockStyle.Fill
            Me.itemsTextBox.Location = New System.Drawing.Point(3, 3)
            Me.itemsTextBox.Name = "itemsTextBox"
            Me.itemsTextBox.Size = New System.Drawing.Size(602, 158)
            Me.itemsTextBox.TabIndex = 0
            Me.itemsTextBox.Text = ""
            '
            'label7
            '
            Me.label7.AutoSize = True
            Me.label7.Location = New System.Drawing.Point(30, 11)
            Me.label7.Name = "label7"
            Me.label7.Size = New System.Drawing.Size(51, 13)
            Me.label7.TabIndex = 8
            Me.label7.Text = "File Path:"
            '
            'filePathNavigate
            '
            Me.filePathNavigate.Enabled = False
            Me.filePathNavigate.Location = New System.Drawing.Point(464, 5)
            Me.filePathNavigate.Name = "filePathNavigate"
            Me.filePathNavigate.Size = New System.Drawing.Size(126, 22)
            Me.filePathNavigate.TabIndex = 7
            Me.filePathNavigate.Text = "Navigate File"
            Me.filePathNavigate.UseVisualStyleBackColor = True
            '
            'filePathEdit
            '
            Me.filePathEdit.Location = New System.Drawing.Point(87, 7)
            Me.filePathEdit.Name = "filePathEdit"
            Me.filePathEdit.Size = New System.Drawing.Size(359, 20)
            Me.filePathEdit.TabIndex = 6
            '
            'knownFolderNavigate
            '
            Me.knownFolderNavigate.Enabled = False
            Me.knownFolderNavigate.Location = New System.Drawing.Point(464, 60)
            Me.knownFolderNavigate.Name = "knownFolderNavigate"
            Me.knownFolderNavigate.Size = New System.Drawing.Size(126, 23)
            Me.knownFolderNavigate.TabIndex = 5
            Me.knownFolderNavigate.Text = "Navigate Known Folder"
            Me.knownFolderNavigate.UseVisualStyleBackColor = True
            '
            'label6
            '
            Me.label6.AutoSize = True
            Me.label6.Location = New System.Drawing.Point(6, 66)
            Me.label6.Name = "label6"
            Me.label6.Size = New System.Drawing.Size(75, 13)
            Me.label6.TabIndex = 4
            Me.label6.Text = "Known Folder:"
            '
            'knownFolderCombo
            '
            Me.knownFolderCombo.FormattingEnabled = True
            Me.knownFolderCombo.Location = New System.Drawing.Point(87, 63)
            Me.knownFolderCombo.Name = "knownFolderCombo"
            Me.knownFolderCombo.Size = New System.Drawing.Size(359, 21)
            Me.knownFolderCombo.TabIndex = 3
            '
            'navigateButton
            '
            Me.navigateButton.Enabled = False
            Me.navigateButton.Location = New System.Drawing.Point(464, 32)
            Me.navigateButton.Name = "navigateButton"
            Me.navigateButton.Size = New System.Drawing.Size(127, 23)
            Me.navigateButton.TabIndex = 2
            Me.navigateButton.Text = "Navigate Path"
            Me.navigateButton.UseVisualStyleBackColor = True
            '
            'label5
            '
            Me.label5.AutoSize = True
            Me.label5.Location = New System.Drawing.Point(17, 38)
            Me.label5.Name = "label5"
            Me.label5.Size = New System.Drawing.Size(64, 13)
            Me.label5.TabIndex = 1
            Me.label5.Text = "Folder Path:"
            '
            'pathEdit
            '
            Me.pathEdit.Location = New System.Drawing.Point(87, 34)
            Me.pathEdit.Name = "pathEdit"
            Me.pathEdit.Size = New System.Drawing.Size(359, 20)
            Me.pathEdit.TabIndex = 0
            '
            'splitContainer1
            '
            Me.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill
            Me.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1
            Me.splitContainer1.IsSplitterFixed = True
            Me.splitContainer1.Location = New System.Drawing.Point(0, 0)
            Me.splitContainer1.Name = "splitContainer1"
            '
            'splitContainer1.Panel1
            '
            Me.splitContainer1.Panel1.Controls.Add(Me.Label1)
            Me.splitContainer1.Panel1.Controls.Add(Me.visibilityPropertyGrid)
            Me.splitContainer1.Panel1.Controls.Add(Me.contentPropertyGrid)
            Me.splitContainer1.Panel1.Controls.Add(Me.navigationPropertyGrid)
            Me.splitContainer1.Panel1.Controls.Add(Me.label2)
            Me.splitContainer1.Panel1.Controls.Add(Me.label3)
            '
            'splitContainer1.Panel2
            '
            Me.splitContainer1.Panel2.Controls.Add(Me.splitContainer2)
            Me.splitContainer1.Size = New System.Drawing.Size(841, 904)
            Me.splitContainer1.SplitterDistance = 221
            Me.splitContainer1.TabIndex = 11
            '
            'visibilityPropertyGrid
            '
            Me.visibilityPropertyGrid.Location = New System.Drawing.Point(6, 230)
            Me.visibilityPropertyGrid.Name = "visibilityPropertyGrid"
            Me.visibilityPropertyGrid.Size = New System.Drawing.Size(213, 248)
            Me.visibilityPropertyGrid.TabIndex = 13
            '
            'contentPropertyGrid
            '
            Me.contentPropertyGrid.Location = New System.Drawing.Point(6, 510)
            Me.contentPropertyGrid.Name = "contentPropertyGrid"
            Me.contentPropertyGrid.Size = New System.Drawing.Size(212, 391)
            Me.contentPropertyGrid.TabIndex = 12
            '
            'navigationPropertyGrid
            '
            Me.navigationPropertyGrid.Location = New System.Drawing.Point(6, 28)
            Me.navigationPropertyGrid.Name = "navigationPropertyGrid"
            Me.navigationPropertyGrid.Size = New System.Drawing.Size(212, 183)
            Me.navigationPropertyGrid.TabIndex = 11
            '
            'splitContainer2
            '
            Me.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill
            Me.splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel1
            Me.splitContainer2.IsSplitterFixed = True
            Me.splitContainer2.Location = New System.Drawing.Point(0, 0)
            Me.splitContainer2.Name = "splitContainer2"
            Me.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal
            '
            'splitContainer2.Panel1
            '
            Me.splitContainer2.Panel1.Controls.Add(Me.clearHistoryButton)
            Me.splitContainer2.Panel1.Controls.Add(Me.forwardButton)
            Me.splitContainer2.Panel1.Controls.Add(Me.navigationHistoryCombo)
            Me.splitContainer2.Panel1.Controls.Add(Me.backButton)
            Me.splitContainer2.Panel1.Controls.Add(Me.failNavigationCheckBox)
            Me.splitContainer2.Panel1.Controls.Add(Me.label5)
            Me.splitContainer2.Panel1.Controls.Add(Me.pathEdit)
            Me.splitContainer2.Panel1.Controls.Add(Me.label7)
            Me.splitContainer2.Panel1.Controls.Add(Me.navigateButton)
            Me.splitContainer2.Panel1.Controls.Add(Me.filePathNavigate)
            Me.splitContainer2.Panel1.Controls.Add(Me.knownFolderCombo)
            Me.splitContainer2.Panel1.Controls.Add(Me.filePathEdit)
            Me.splitContainer2.Panel1.Controls.Add(Me.label6)
            Me.splitContainer2.Panel1.Controls.Add(Me.knownFolderNavigate)
            '
            'splitContainer2.Panel2
            '
            Me.splitContainer2.Panel2.Controls.Add(Me.splitContainer3)
            Me.splitContainer2.Size = New System.Drawing.Size(616, 904)
            Me.splitContainer2.SplitterDistance = 139
            Me.splitContainer2.TabIndex = 0
            '
            'clearHistoryButton
            '
            Me.clearHistoryButton.Location = New System.Drawing.Point(466, 112)
            Me.clearHistoryButton.Name = "clearHistoryButton"
            Me.clearHistoryButton.Size = New System.Drawing.Size(125, 23)
            Me.clearHistoryButton.TabIndex = 14
            Me.clearHistoryButton.Text = "Clear History"
            Me.clearHistoryButton.UseVisualStyleBackColor = True
            '
            'forwardButton
            '
            Me.forwardButton.Font = New System.Drawing.Font("Comic Sans MS", 18.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
            Me.forwardButton.Location = New System.Drawing.Point(62, 87)
            Me.forwardButton.Name = "forwardButton"
            Me.forwardButton.Size = New System.Drawing.Size(48, 48)
            Me.forwardButton.TabIndex = 13
            Me.forwardButton.Text = ">"
            Me.forwardButton.UseVisualStyleBackColor = True
            '
            'navigationHistoryCombo
            '
            Me.navigationHistoryCombo.FormattingEnabled = True
            Me.navigationHistoryCombo.Location = New System.Drawing.Point(129, 99)
            Me.navigationHistoryCombo.Name = "navigationHistoryCombo"
            Me.navigationHistoryCombo.Size = New System.Drawing.Size(317, 21)
            Me.navigationHistoryCombo.TabIndex = 12
            '
            'backButton
            '
            Me.backButton.Font = New System.Drawing.Font("Comic Sans MS", 18.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
            Me.backButton.Location = New System.Drawing.Point(8, 87)
            Me.backButton.Name = "backButton"
            Me.backButton.Size = New System.Drawing.Size(48, 48)
            Me.backButton.TabIndex = 10
            Me.backButton.Text = "<"
            Me.backButton.UseVisualStyleBackColor = True
            '
            'failNavigationCheckBox
            '
            Me.failNavigationCheckBox.AutoSize = True
            Me.failNavigationCheckBox.Location = New System.Drawing.Point(466, 92)
            Me.failNavigationCheckBox.Name = "failNavigationCheckBox"
            Me.failNavigationCheckBox.Size = New System.Drawing.Size(138, 17)
            Me.failNavigationCheckBox.TabIndex = 9
            Me.failNavigationCheckBox.Text = "Force Navigation to Fail"
            Me.failNavigationCheckBox.UseVisualStyleBackColor = True
            '
            'splitContainer3
            '
            Me.splitContainer3.Dock = System.Windows.Forms.DockStyle.Fill
            Me.splitContainer3.FixedPanel = System.Windows.Forms.FixedPanel.Panel2
            Me.splitContainer3.IsSplitterFixed = True
            Me.splitContainer3.Location = New System.Drawing.Point(0, 0)
            Me.splitContainer3.Name = "splitContainer3"
            Me.splitContainer3.Orientation = System.Windows.Forms.Orientation.Horizontal
            '
            'splitContainer3.Panel1
            '
            Me.splitContainer3.Panel1.Controls.Add(Me.explorerBrowser)
            '
            'splitContainer3.Panel2
            '
            Me.splitContainer3.Panel2.Controls.Add(Me.itemsTabControl)
            Me.splitContainer3.Size = New System.Drawing.Size(616, 761)
            Me.splitContainer3.SplitterDistance = 567
            Me.splitContainer3.TabIndex = 0
            '
            'explorerBrowser
            '
            Me.explorerBrowser.Dock = System.Windows.Forms.DockStyle.Fill
            Me.explorerBrowser.Location = New System.Drawing.Point(0, 0)
            Me.explorerBrowser.Name = "explorerBrowser"
            Me.explorerBrowser.PropertyBagName = "Microsoft.WindowsAPICodePack.Controls.WindowsForms.ExplorerBrowser"
            Me.explorerBrowser.Size = New System.Drawing.Size(616, 567)
            Me.explorerBrowser.TabIndex = 0
            '
            'itemsTabControl
            '
            Me.itemsTabControl.Controls.Add(Me.tabPage1)
            Me.itemsTabControl.Controls.Add(Me.tabPage2)
            Me.itemsTabControl.Controls.Add(Me.tabPage3)
            Me.itemsTabControl.Dock = System.Windows.Forms.DockStyle.Fill
            Me.itemsTabControl.Location = New System.Drawing.Point(0, 0)
            Me.itemsTabControl.Name = "itemsTabControl"
            Me.itemsTabControl.SelectedIndex = 0
            Me.itemsTabControl.Size = New System.Drawing.Size(616, 190)
            Me.itemsTabControl.TabIndex = 1
            '
            'tabPage1
            '
            Me.tabPage1.Controls.Add(Me.itemsTextBox)
            Me.tabPage1.Location = New System.Drawing.Point(4, 22)
            Me.tabPage1.Name = "tabPage1"
            Me.tabPage1.Padding = New System.Windows.Forms.Padding(3)
            Me.tabPage1.Size = New System.Drawing.Size(608, 164)
            Me.tabPage1.TabIndex = 0
            Me.tabPage1.Text = "Items (Count=0)"
            Me.tabPage1.UseVisualStyleBackColor = True
            '
            'tabPage2
            '
            Me.tabPage2.Controls.Add(Me.selectedItemsTextBox)
            Me.tabPage2.Location = New System.Drawing.Point(4, 22)
            Me.tabPage2.Name = "tabPage2"
            Me.tabPage2.Padding = New System.Windows.Forms.Padding(3)
            Me.tabPage2.Size = New System.Drawing.Size(608, 164)
            Me.tabPage2.TabIndex = 1
            Me.tabPage2.Text = "Selected Items (Count=0)"
            Me.tabPage2.UseVisualStyleBackColor = True
            '
            'selectedItemsTextBox
            '
            Me.selectedItemsTextBox.Dock = System.Windows.Forms.DockStyle.Fill
            Me.selectedItemsTextBox.Location = New System.Drawing.Point(3, 3)
            Me.selectedItemsTextBox.Name = "selectedItemsTextBox"
            Me.selectedItemsTextBox.Size = New System.Drawing.Size(602, 158)
            Me.selectedItemsTextBox.TabIndex = 0
            Me.selectedItemsTextBox.Text = ""
            '
            'tabPage3
            '
            Me.tabPage3.Controls.Add(Me.eventHistoryTextBox)
            Me.tabPage3.Location = New System.Drawing.Point(4, 22)
            Me.tabPage3.Name = "tabPage3"
            Me.tabPage3.Size = New System.Drawing.Size(608, 164)
            Me.tabPage3.TabIndex = 2
            Me.tabPage3.Text = "Event History"
            Me.tabPage3.UseVisualStyleBackColor = True
            '
            'eventHistoryTextBox
            '
            Me.eventHistoryTextBox.Dock = System.Windows.Forms.DockStyle.Fill
            Me.eventHistoryTextBox.Location = New System.Drawing.Point(0, 0)
            Me.eventHistoryTextBox.Name = "eventHistoryTextBox"
            Me.eventHistoryTextBox.Size = New System.Drawing.Size(608, 164)
            Me.eventHistoryTextBox.TabIndex = 0
            Me.eventHistoryTextBox.Text = ""
            '
            'Label1
            '
            Me.Label1.AutoSize = True
            Me.Label1.Location = New System.Drawing.Point(3, 214)
            Me.Label1.Name = "Label1"
            Me.Label1.Size = New System.Drawing.Size(71, 13)
            Me.Label1.TabIndex = 14
            Me.Label1.Text = "Pane Options"
            '
            'ExplorerBrowserTestForm
            '
            Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.ClientSize = New System.Drawing.Size(841, 904)
            Me.Controls.Add(Me.splitContainer1)
            Me.Name = "ExplorerBrowserTestForm"
            Me.Text = "Explorer Browser Demo"
            Me.splitContainer1.Panel1.ResumeLayout(False)
            Me.splitContainer1.Panel1.PerformLayout()
            Me.splitContainer1.Panel2.ResumeLayout(False)
            Me.splitContainer1.ResumeLayout(False)
            Me.splitContainer2.Panel1.ResumeLayout(False)
            Me.splitContainer2.Panel1.PerformLayout()
            Me.splitContainer2.Panel2.ResumeLayout(False)
            Me.splitContainer2.ResumeLayout(False)
            Me.splitContainer3.Panel1.ResumeLayout(False)
            Me.splitContainer3.Panel2.ResumeLayout(False)
            Me.splitContainer3.ResumeLayout(False)
            Me.itemsTabControl.ResumeLayout(False)
            Me.tabPage1.ResumeLayout(False)
            Me.tabPage2.ResumeLayout(False)
            Me.tabPage3.ResumeLayout(False)
            Me.ResumeLayout(False)

        End Sub

		#End Region

		Private label2 As System.Windows.Forms.Label
		Private label3 As System.Windows.Forms.Label
		Private WithEvents navigateButton As System.Windows.Forms.Button
		Private label5 As System.Windows.Forms.Label
        Private WithEvents pathEdit As System.Windows.Forms.TextBox
		Private WithEvents knownFolderNavigate As System.Windows.Forms.Button
		Private label6 As System.Windows.Forms.Label
        Private WithEvents knownFolderCombo As System.Windows.Forms.ComboBox
		Private label7 As System.Windows.Forms.Label
		Private WithEvents filePathNavigate As System.Windows.Forms.Button
        Private WithEvents filePathEdit As System.Windows.Forms.TextBox
		Private itemsTextBox As System.Windows.Forms.RichTextBox
		Private splitContainer1 As System.Windows.Forms.SplitContainer
		Private splitContainer2 As System.Windows.Forms.SplitContainer
		Private splitContainer3 As System.Windows.Forms.SplitContainer
		Private explorerBrowser As Microsoft.WindowsAPICodePack.Controls.WindowsForms.ExplorerBrowser
		Private navigationPropertyGrid As System.Windows.Forms.PropertyGrid
		Private contentPropertyGrid As System.Windows.Forms.PropertyGrid
		Private visibilityPropertyGrid As System.Windows.Forms.PropertyGrid
		Private failNavigationCheckBox As System.Windows.Forms.CheckBox
		Private itemsTabControl As System.Windows.Forms.TabControl
		Private tabPage1 As System.Windows.Forms.TabPage
		Private tabPage2 As System.Windows.Forms.TabPage
		Private selectedItemsTextBox As System.Windows.Forms.RichTextBox
		Private WithEvents forwardButton As System.Windows.Forms.Button
		Private WithEvents navigationHistoryCombo As System.Windows.Forms.ComboBox
		Private WithEvents backButton As System.Windows.Forms.Button
		Private tabPage3 As System.Windows.Forms.TabPage
		Private eventHistoryTextBox As System.Windows.Forms.RichTextBox
        Private WithEvents clearHistoryButton As System.Windows.Forms.Button
        Friend WithEvents Label1 As System.Windows.Forms.Label

	End Class
End Namespace

