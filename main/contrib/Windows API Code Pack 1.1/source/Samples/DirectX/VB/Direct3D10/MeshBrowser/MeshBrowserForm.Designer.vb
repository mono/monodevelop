Imports Microsoft.VisualBasic
Imports System
Namespace MeshBrowser
	Partial Public Class MeshBrowserForm
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
            Me.directControl = New Microsoft.WindowsAPICodePack.DirectX.Controls.DirectControl
            Me.buttonOpen = New System.Windows.Forms.Button
            Me.openFileDialog1 = New System.Windows.Forms.OpenFileDialog
            Me.splitContainer1 = New System.Windows.Forms.SplitContainer
            Me.cbRotate = New System.Windows.Forms.CheckBox
            Me.cbWireframe = New System.Windows.Forms.CheckBox
            Me.buttonScanDXSDK = New System.Windows.Forms.Button
            Me.splitContainer2 = New System.Windows.Forms.SplitContainer
            Me.listBoxValid = New System.Windows.Forms.ListBox
            Me.label1 = New System.Windows.Forms.Label
            Me.listBoxInvalid = New System.Windows.Forms.ListBox
            Me.label2 = New System.Windows.Forms.Label
            Me.splitContainer1.Panel1.SuspendLayout()
            Me.splitContainer1.Panel2.SuspendLayout()
            Me.splitContainer1.SuspendLayout()
            Me.splitContainer2.Panel1.SuspendLayout()
            Me.splitContainer2.Panel2.SuspendLayout()
            Me.splitContainer2.SuspendLayout()
            Me.SuspendLayout()
            '
            'directControl
            '
            Me.directControl.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                        Or System.Windows.Forms.AnchorStyles.Left) _
                        Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.directControl.Location = New System.Drawing.Point(3, 3)
            Me.directControl.Name = "directControl"
            Me.directControl.Size = New System.Drawing.Size(640, 480)
            Me.directControl.TabIndex = 4
            '
            'buttonOpen
            '
            Me.buttonOpen.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
            Me.buttonOpen.Location = New System.Drawing.Point(12, 489)
            Me.buttonOpen.Name = "buttonOpen"
            Me.buttonOpen.Size = New System.Drawing.Size(75, 23)
            Me.buttonOpen.TabIndex = 5
            Me.buttonOpen.Text = "&Open"
            Me.buttonOpen.UseVisualStyleBackColor = True
            '
            'openFileDialog1
            '
            Me.openFileDialog1.FileName = "openFileDialog1"
            Me.openFileDialog1.Filter = ".x files|*.x|All files|*.*"
            Me.openFileDialog1.RestoreDirectory = True
            '
            'splitContainer1
            '
            Me.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill
            Me.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel2
            Me.splitContainer1.Location = New System.Drawing.Point(0, 0)
            Me.splitContainer1.Name = "splitContainer1"
            '
            'splitContainer1.Panel1
            '
            Me.splitContainer1.Panel1.Controls.Add(Me.cbRotate)
            Me.splitContainer1.Panel1.Controls.Add(Me.cbWireframe)
            Me.splitContainer1.Panel1.Controls.Add(Me.buttonScanDXSDK)
            Me.splitContainer1.Panel1.Controls.Add(Me.buttonOpen)
            Me.splitContainer1.Panel1.Controls.Add(Me.directControl)
            '
            'splitContainer1.Panel2
            '
            Me.splitContainer1.Panel2.Controls.Add(Me.splitContainer2)
            Me.splitContainer1.Size = New System.Drawing.Size(838, 522)
            Me.splitContainer1.SplitterDistance = 645
            Me.splitContainer1.TabIndex = 6
            '
            'cbRotate
            '
            Me.cbRotate.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
            Me.cbRotate.AutoSize = True
            Me.cbRotate.Checked = True
            Me.cbRotate.CheckState = System.Windows.Forms.CheckState.Checked
            Me.cbRotate.Location = New System.Drawing.Point(324, 493)
            Me.cbRotate.Name = "cbRotate"
            Me.cbRotate.Size = New System.Drawing.Size(58, 17)
            Me.cbRotate.TabIndex = 7
            Me.cbRotate.Text = "Rotate"
            Me.cbRotate.UseVisualStyleBackColor = True
            '
            'cbWireframe
            '
            Me.cbWireframe.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
            Me.cbWireframe.AutoSize = True
            Me.cbWireframe.Location = New System.Drawing.Point(244, 493)
            Me.cbWireframe.Name = "cbWireframe"
            Me.cbWireframe.Size = New System.Drawing.Size(74, 17)
            Me.cbWireframe.TabIndex = 7
            Me.cbWireframe.Text = "Wireframe"
            Me.cbWireframe.UseVisualStyleBackColor = True
            '
            'buttonScanDXSDK
            '
            Me.buttonScanDXSDK.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
            Me.buttonScanDXSDK.Location = New System.Drawing.Point(93, 489)
            Me.buttonScanDXSDK.Name = "buttonScanDXSDK"
            Me.buttonScanDXSDK.Size = New System.Drawing.Size(145, 23)
            Me.buttonScanDXSDK.TabIndex = 6
            Me.buttonScanDXSDK.Text = "Scan DX SDK Samples"
            Me.buttonScanDXSDK.UseVisualStyleBackColor = True
            '
            'splitContainer2
            '
            Me.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill
            Me.splitContainer2.Location = New System.Drawing.Point(0, 0)
            Me.splitContainer2.Name = "splitContainer2"
            Me.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal
            '
            'splitContainer2.Panel1
            '
            Me.splitContainer2.Panel1.Controls.Add(Me.listBoxValid)
            Me.splitContainer2.Panel1.Controls.Add(Me.label1)
            '
            'splitContainer2.Panel2
            '
            Me.splitContainer2.Panel2.Controls.Add(Me.listBoxInvalid)
            Me.splitContainer2.Panel2.Controls.Add(Me.label2)
            Me.splitContainer2.Size = New System.Drawing.Size(189, 522)
            Me.splitContainer2.SplitterDistance = 357
            Me.splitContainer2.TabIndex = 0
            '
            'listBoxValid
            '
            Me.listBoxValid.Dock = System.Windows.Forms.DockStyle.Fill
            Me.listBoxValid.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed
            Me.listBoxValid.FormattingEnabled = True
            Me.listBoxValid.HorizontalScrollbar = True
            Me.listBoxValid.IntegralHeight = False
            Me.listBoxValid.Location = New System.Drawing.Point(0, 13)
            Me.listBoxValid.Name = "listBoxValid"
            Me.listBoxValid.Size = New System.Drawing.Size(189, 344)
            Me.listBoxValid.TabIndex = 1
            '
            'label1
            '
            Me.label1.AutoSize = True
            Me.label1.Dock = System.Windows.Forms.DockStyle.Top
            Me.label1.Location = New System.Drawing.Point(0, 0)
            Me.label1.Name = "label1"
            Me.label1.Size = New System.Drawing.Size(51, 13)
            Me.label1.TabIndex = 0
            Me.label1.Text = "Valid files"
            '
            'listBoxInvalid
            '
            Me.listBoxInvalid.Dock = System.Windows.Forms.DockStyle.Fill
            Me.listBoxInvalid.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed
            Me.listBoxInvalid.FormattingEnabled = True
            Me.listBoxInvalid.HorizontalScrollbar = True
            Me.listBoxInvalid.IntegralHeight = False
            Me.listBoxInvalid.Location = New System.Drawing.Point(0, 13)
            Me.listBoxInvalid.Name = "listBoxInvalid"
            Me.listBoxInvalid.Size = New System.Drawing.Size(189, 148)
            Me.listBoxInvalid.TabIndex = 1
            '
            'label2
            '
            Me.label2.AutoSize = True
            Me.label2.Dock = System.Windows.Forms.DockStyle.Top
            Me.label2.Location = New System.Drawing.Point(0, 0)
            Me.label2.Name = "label2"
            Me.label2.Size = New System.Drawing.Size(59, 13)
            Me.label2.TabIndex = 0
            Me.label2.Text = "Invalid files"
            '
            'MeshBrowserForm
            '
            Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.ClientSize = New System.Drawing.Size(838, 522)
            Me.Controls.Add(Me.splitContainer1)
            Me.Name = "MeshBrowserForm"
            Me.Text = "Mesh Browser"
            Me.splitContainer1.Panel1.ResumeLayout(False)
            Me.splitContainer1.Panel1.PerformLayout()
            Me.splitContainer1.Panel2.ResumeLayout(False)
            Me.splitContainer1.ResumeLayout(False)
            Me.splitContainer2.Panel1.ResumeLayout(False)
            Me.splitContainer2.Panel1.PerformLayout()
            Me.splitContainer2.Panel2.ResumeLayout(False)
            Me.splitContainer2.Panel2.PerformLayout()
            Me.splitContainer2.ResumeLayout(False)
            Me.ResumeLayout(False)

        End Sub

		#End Region

		Private WithEvents directControl As Microsoft.WindowsAPICodePack.DirectX.Controls.DirectControl
		Private WithEvents buttonOpen As System.Windows.Forms.Button
		Private openFileDialog1 As System.Windows.Forms.OpenFileDialog
		Private splitContainer1 As System.Windows.Forms.SplitContainer
		Private splitContainer2 As System.Windows.Forms.SplitContainer
		Private WithEvents listBoxValid As System.Windows.Forms.ListBox
		Private label1 As System.Windows.Forms.Label
		Private WithEvents listBoxInvalid As System.Windows.Forms.ListBox
		Private label2 As System.Windows.Forms.Label
		Private WithEvents buttonScanDXSDK As System.Windows.Forms.Button
		Private WithEvents cbWireframe As System.Windows.Forms.CheckBox
		Private WithEvents cbRotate As System.Windows.Forms.CheckBox
	End Class
End Namespace