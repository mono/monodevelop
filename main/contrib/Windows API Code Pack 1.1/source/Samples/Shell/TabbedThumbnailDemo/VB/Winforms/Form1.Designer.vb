'Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Namespace Microsoft.WindowsAPICodePack.Samples.TabbedThumbnailDemo
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
            Me.panel1 = New System.Windows.Forms.Panel
            Me.button4 = New System.Windows.Forms.Button
            Me.button3 = New System.Windows.Forms.Button
            Me.button2 = New System.Windows.Forms.Button
            Me.comboBox1 = New System.Windows.Forms.ComboBox
            Me.button1 = New System.Windows.Forms.Button
            Me.tabControl1 = New System.Windows.Forms.TabControl
            Me.statusStrip1 = New System.Windows.Forms.StatusStrip
            Me.toolStripProgressBar1 = New System.Windows.Forms.ToolStripProgressBar
            Me.toolTip1 = New System.Windows.Forms.ToolTip(Me.components)
            Me.panel1.SuspendLayout()
            Me.statusStrip1.SuspendLayout()
            Me.SuspendLayout()
            '
            'panel1
            '
            Me.panel1.Controls.Add(Me.button4)
            Me.panel1.Controls.Add(Me.button3)
            Me.panel1.Controls.Add(Me.button2)
            Me.panel1.Controls.Add(Me.comboBox1)
            Me.panel1.Controls.Add(Me.button1)
            Me.panel1.Dock = System.Windows.Forms.DockStyle.Top
            Me.panel1.Location = New System.Drawing.Point(0, 0)
            Me.panel1.Name = "panel1"
            Me.panel1.Size = New System.Drawing.Size(956, 35)
            Me.panel1.TabIndex = 0
            '
            'button4
            '
            Me.button4.Anchor = System.Windows.Forms.AnchorStyles.Right
            Me.button4.Enabled = False
            Me.button4.Location = New System.Drawing.Point(797, 6)
            Me.button4.Name = "button4"
            Me.button4.Size = New System.Drawing.Size(82, 23)
            Me.button4.TabIndex = 4
            Me.button4.Text = "F&ull thumbnail"
            Me.toolTip1.SetToolTip(Me.button4, resources.GetString("button4.ToolTip"))
            Me.button4.UseVisualStyleBackColor = True
            '
            'button3
            '
            Me.button3.Anchor = System.Windows.Forms.AnchorStyles.Right
            Me.button3.Location = New System.Drawing.Point(715, 6)
            Me.button3.Name = "button3"
            Me.button3.Size = New System.Drawing.Size(75, 23)
            Me.button3.TabIndex = 3
            Me.button3.Text = "Open &File"
            Me.toolTip1.SetToolTip(Me.button3, resources.GetString("button3.ToolTip"))
            Me.button3.UseVisualStyleBackColor = True
            '
            'button2
            '
            Me.button2.Anchor = System.Windows.Forms.AnchorStyles.Right
            Me.button2.Enabled = False
            Me.button2.Location = New System.Drawing.Point(885, 6)
            Me.button2.Name = "button2"
            Me.button2.Size = New System.Drawing.Size(68, 23)
            Me.button2.TabIndex = 2
            Me.button2.Text = "&Close Tab"
            Me.toolTip1.SetToolTip(Me.button2, "Close the currently selected tab. " & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "This removes the tab from the application UI " & _
                    "(TabControl), " & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "as well as from the taskbar's tabbed thumbnail list. See code " & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & _
                    "for details.")
            Me.button2.UseVisualStyleBackColor = True
            '
            'comboBox1
            '
            Me.comboBox1.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.comboBox1.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest
            Me.comboBox1.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.HistoryList
            Me.comboBox1.FormattingEnabled = True
            Me.comboBox1.Location = New System.Drawing.Point(3, 8)
            Me.comboBox1.Name = "comboBox1"
            Me.comboBox1.Size = New System.Drawing.Size(589, 21)
            Me.comboBox1.TabIndex = 1
            Me.comboBox1.Text = "http://code.msdn.com/WindowsAPICodePack"
            '
            'button1
            '
            Me.button1.Anchor = System.Windows.Forms.AnchorStyles.Right
            Me.button1.Location = New System.Drawing.Point(598, 6)
            Me.button1.Name = "button1"
            Me.button1.Size = New System.Drawing.Size(111, 23)
            Me.button1.TabIndex = 0
            Me.button1.Text = "&Navigate (new tab)"
            Me.toolTip1.SetToolTip(Me.button1, "Navigate to the URL specified in the addressbar. " & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "This button will open a new ta" & _
                    "b." & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "The thumbnail displayed on the taskbar for this" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & " tab is updated after sever" & _
                    "al events (see code for details).")
            Me.button1.UseVisualStyleBackColor = True
            '
            'tabControl1
            '
            Me.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill
            Me.tabControl1.Location = New System.Drawing.Point(0, 35)
            Me.tabControl1.Name = "tabControl1"
            Me.tabControl1.SelectedIndex = 0
            Me.tabControl1.Size = New System.Drawing.Size(956, 514)
            Me.tabControl1.TabIndex = 1
            '
            'statusStrip1
            '
            Me.statusStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.toolStripProgressBar1})
            Me.statusStrip1.Location = New System.Drawing.Point(0, 549)
            Me.statusStrip1.Name = "statusStrip1"
            Me.statusStrip1.Size = New System.Drawing.Size(956, 22)
            Me.statusStrip1.TabIndex = 2
            Me.statusStrip1.Text = "statusStrip1"
            '
            'toolStripProgressBar1
            '
            Me.toolStripProgressBar1.Name = "toolStripProgressBar1"
            Me.toolStripProgressBar1.Size = New System.Drawing.Size(100, 16)
            '
            'Form1
            '
            Me.AcceptButton = Me.button1
            Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.ClientSize = New System.Drawing.Size(956, 571)
            Me.Controls.Add(Me.tabControl1)
            Me.Controls.Add(Me.panel1)
            Me.Controls.Add(Me.statusStrip1)
            Me.Name = "Form1"
            Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
            Me.Text = "Tabbed Thumbnail Demo"
            Me.panel1.ResumeLayout(False)
            Me.statusStrip1.ResumeLayout(False)
            Me.statusStrip1.PerformLayout()
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub

		#End Region

		Private panel1 As System.Windows.Forms.Panel
		Private comboBox1 As System.Windows.Forms.ComboBox
		Private WithEvents button1 As System.Windows.Forms.Button
		Private tabControl1 As System.Windows.Forms.TabControl
		Private WithEvents button2 As System.Windows.Forms.Button
		Private statusStrip1 As System.Windows.Forms.StatusStrip
		Private toolStripProgressBar1 As System.Windows.Forms.ToolStripProgressBar
		Private WithEvents button3 As System.Windows.Forms.Button
		Private WithEvents button4 As System.Windows.Forms.Button
		Private toolTip1 As System.Windows.Forms.ToolTip

	End Class
End Namespace

