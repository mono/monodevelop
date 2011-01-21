Imports Microsoft.VisualBasic
Imports System
Namespace WindowsFormsGlassDemo
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
			Me.panel1 = New System.Windows.Forms.Panel()
			Me.splitContainer1 = New System.Windows.Forms.SplitContainer()
			Me.explorerBrowser1 = New Microsoft.WindowsAPICodePack.Controls.WindowsForms.ExplorerBrowser()
			Me.compositionEnabled = New System.Windows.Forms.CheckBox()
			Me.panel1.SuspendLayout()
			Me.splitContainer1.Panel1.SuspendLayout()
			Me.splitContainer1.Panel2.SuspendLayout()
			Me.splitContainer1.SuspendLayout()
			Me.SuspendLayout()
			' 
			' panel1
			' 
			Me.panel1.BackColor = System.Drawing.Color.PaleTurquoise
			Me.panel1.Controls.Add(Me.splitContainer1)
			Me.panel1.Location = New System.Drawing.Point(46, 35)
			Me.panel1.Name = "panel1"
			Me.panel1.Size = New System.Drawing.Size(328, 275)
			Me.panel1.TabIndex = 0
			' 
			' splitContainer1
			' 
			Me.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill
			Me.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel2
			Me.splitContainer1.Location = New System.Drawing.Point(0, 0)
			Me.splitContainer1.Name = "splitContainer1"
			Me.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal
			' 
			' splitContainer1.Panel1
			' 
			Me.splitContainer1.Panel1.Controls.Add(Me.explorerBrowser1)
			' 
			' splitContainer1.Panel2
			' 
			Me.splitContainer1.Panel2.Controls.Add(Me.compositionEnabled)
			Me.splitContainer1.Size = New System.Drawing.Size(328, 275)
			Me.splitContainer1.SplitterDistance = 228
			Me.splitContainer1.TabIndex = 0
			' 
			' explorerBrowser1
			' 
			Me.explorerBrowser1.Dock = System.Windows.Forms.DockStyle.Fill
			Me.explorerBrowser1.Location = New System.Drawing.Point(0, 0)
			Me.explorerBrowser1.Name = "explorerBrowser1"
			Me.explorerBrowser1.PropertyBagName = "Microsoft.WindowsAPICodePack.Controls.WindowsForms.ExplorerBrowser"
			Me.explorerBrowser1.Size = New System.Drawing.Size(328, 228)
			Me.explorerBrowser1.TabIndex = 0
			' 
			' compositionEnabled
			' 
			Me.compositionEnabled.AutoSize = True
			Me.compositionEnabled.Location = New System.Drawing.Point(4, 15)
			Me.compositionEnabled.Name = "compositionEnabled"
			Me.compositionEnabled.Size = New System.Drawing.Size(164, 17)
			Me.compositionEnabled.TabIndex = 0
			Me.compositionEnabled.Text = "desktop composition enabled"
			Me.compositionEnabled.UseVisualStyleBackColor = True
'			Me.compositionEnabled.CheckedChanged += New System.EventHandler(Me.compositionEnabled_CheckedChanged)
			' 
			' Form1
			' 
			Me.AutoScaleDimensions = New System.Drawing.SizeF(6F, 13F)
			Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
			Me.ClientSize = New System.Drawing.Size(438, 341)
			Me.Controls.Add(Me.panel1)
			Me.Name = "Form1"
			Me.Text = "WinForms AeroGlass Demo"
'			Me.Resize += New System.EventHandler(Me.Form1_Resize)
			Me.panel1.ResumeLayout(False)
			Me.splitContainer1.Panel1.ResumeLayout(False)
			Me.splitContainer1.Panel2.ResumeLayout(False)
			Me.splitContainer1.Panel2.PerformLayout()
			Me.splitContainer1.ResumeLayout(False)
			Me.ResumeLayout(False)

		End Sub

		#End Region

		Private panel1 As System.Windows.Forms.Panel
		Private splitContainer1 As System.Windows.Forms.SplitContainer
		Private explorerBrowser1 As Microsoft.WindowsAPICodePack.Controls.WindowsForms.ExplorerBrowser
		Private WithEvents compositionEnabled As System.Windows.Forms.CheckBox
	End Class
End Namespace

