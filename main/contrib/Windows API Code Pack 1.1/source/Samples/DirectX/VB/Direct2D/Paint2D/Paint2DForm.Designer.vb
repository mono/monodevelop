Imports Microsoft.VisualBasic
Imports System
Namespace D2DPaint
	Partial Public Class Paint2DForm
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
            Dim toolStripLabel1 As System.Windows.Forms.ToolStripLabel
            Dim toolStripLabel2 As System.Windows.Forms.ToolStripLabel
            Me.renderControl = New Microsoft.WindowsAPICodePack.DirectX.Controls.RenderControl
            Me.toolStrip1 = New System.Windows.Forms.ToolStrip
            Me.arrowButton = New System.Windows.Forms.ToolStripButton
            Me.toolStripSeparator7 = New System.Windows.Forms.ToolStripSeparator
            Me.strokeWidths = New System.Windows.Forms.ToolStripComboBox
            Me.toolStripSeparator6 = New System.Windows.Forms.ToolStripSeparator
            Me.fillButton = New System.Windows.Forms.ToolStripButton
            Me.toolStripSeparator2 = New System.Windows.Forms.ToolStripSeparator
            Me.lineButton = New System.Windows.Forms.ToolStripButton
            Me.rectButton = New System.Windows.Forms.ToolStripButton
            Me.roundrectButton = New System.Windows.Forms.ToolStripButton
            Me.ellipseButton = New System.Windows.Forms.ToolStripButton
            Me.geometryButton = New System.Windows.Forms.ToolStripButton
            Me.toolStripSeparator4 = New System.Windows.Forms.ToolStripSeparator
            Me.textButton = New System.Windows.Forms.ToolStripButton
            Me.toolStripSeparator3 = New System.Windows.Forms.ToolStripSeparator
            Me.bitmapButton = New System.Windows.Forms.ToolStripButton
            Me.traparencyList = New System.Windows.Forms.ToolStripComboBox
            Me.toolStripSeparator5 = New System.Windows.Forms.ToolStripSeparator
            Me.brushButton = New System.Windows.Forms.ToolStripButton
            Me.toolStripSeparator1 = New System.Windows.Forms.ToolStripSeparator
            Me.clearButton = New System.Windows.Forms.ToolStripButton
            Me.ToolStripSeparator8 = New System.Windows.Forms.ToolStripSeparator
            Me.saveButton = New System.Windows.Forms.ToolStripButton
            toolStripLabel1 = New System.Windows.Forms.ToolStripLabel
            toolStripLabel2 = New System.Windows.Forms.ToolStripLabel
            Me.toolStrip1.SuspendLayout()
            Me.SuspendLayout()
            '
            'toolStripLabel1
            '
            toolStripLabel1.Name = "toolStripLabel1"
            toolStripLabel1.Size = New System.Drawing.Size(43, 22)
            toolStripLabel1.Text = "Stroke:"
            '
            'toolStripLabel2
            '
            toolStripLabel2.Name = "toolStripLabel2"
            toolStripLabel2.Size = New System.Drawing.Size(81, 22)
            toolStripLabel2.Text = "Transparency:"
            '
            'renderControl
            '
            Me.renderControl.AutoSize = True
            Me.renderControl.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
            Me.renderControl.Dock = System.Windows.Forms.DockStyle.Fill
            Me.renderControl.Location = New System.Drawing.Point(0, 0)
            Me.renderControl.Name = "renderControl"
            Me.renderControl.Size = New System.Drawing.Size(869, 605)
            Me.renderControl.TabIndex = 0
            '
            'toolStrip1
            '
            Me.toolStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.arrowButton, Me.toolStripSeparator7, toolStripLabel1, Me.strokeWidths, Me.toolStripSeparator6, Me.fillButton, Me.toolStripSeparator2, Me.lineButton, Me.rectButton, Me.roundrectButton, Me.ellipseButton, Me.geometryButton, Me.toolStripSeparator4, Me.textButton, Me.toolStripSeparator3, Me.bitmapButton, toolStripLabel2, Me.traparencyList, Me.toolStripSeparator5, Me.brushButton, Me.toolStripSeparator1, Me.clearButton, Me.ToolStripSeparator8, Me.saveButton})
            Me.toolStrip1.Location = New System.Drawing.Point(0, 0)
            Me.toolStrip1.Name = "toolStrip1"
            Me.toolStrip1.Size = New System.Drawing.Size(869, 25)
            Me.toolStrip1.TabIndex = 1
            Me.toolStrip1.Text = "toolStrip1"
            '
            'arrowButton
            '
            Me.arrowButton.Checked = True
            Me.arrowButton.CheckState = System.Windows.Forms.CheckState.Checked
            Me.arrowButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
            Me.arrowButton.Image = Global.Resources.arrow
            Me.arrowButton.ImageTransparentColor = System.Drawing.Color.Magenta
            Me.arrowButton.Name = "arrowButton"
            Me.arrowButton.Size = New System.Drawing.Size(23, 22)
            '
            'toolStripSeparator7
            '
            Me.toolStripSeparator7.Name = "toolStripSeparator7"
            Me.toolStripSeparator7.Size = New System.Drawing.Size(6, 25)
            '
            'strokeWidths
            '
            Me.strokeWidths.AutoSize = False
            Me.strokeWidths.DropDownHeight = 110
            Me.strokeWidths.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.strokeWidths.DropDownWidth = 50
            Me.strokeWidths.IntegralHeight = False
            Me.strokeWidths.Items.AddRange(New Object() {"1", "2", "4", "6", "8", "10", "12", "16", "24", "36", "42"})
            Me.strokeWidths.Name = "strokeWidths"
            Me.strokeWidths.Size = New System.Drawing.Size(42, 23)
            '
            'toolStripSeparator6
            '
            Me.toolStripSeparator6.Name = "toolStripSeparator6"
            Me.toolStripSeparator6.Size = New System.Drawing.Size(6, 25)
            '
            'fillButton
            '
            Me.fillButton.CheckOnClick = True
            Me.fillButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
            Me.fillButton.ImageTransparentColor = System.Drawing.Color.Magenta
            Me.fillButton.Name = "fillButton"
            Me.fillButton.Size = New System.Drawing.Size(39, 22)
            Me.fillButton.Text = "Filled"
            '
            'toolStripSeparator2
            '
            Me.toolStripSeparator2.Name = "toolStripSeparator2"
            Me.toolStripSeparator2.Size = New System.Drawing.Size(6, 25)
            '
            'lineButton
            '
            Me.lineButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
            Me.lineButton.ImageTransparentColor = System.Drawing.Color.Magenta
            Me.lineButton.Name = "lineButton"
            Me.lineButton.Size = New System.Drawing.Size(33, 22)
            Me.lineButton.Text = "Line"
            '
            'rectButton
            '
            Me.rectButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
            Me.rectButton.ImageTransparentColor = System.Drawing.Color.Magenta
            Me.rectButton.Name = "rectButton"
            Me.rectButton.Size = New System.Drawing.Size(34, 22)
            Me.rectButton.Text = "Rect"
            '
            'roundrectButton
            '
            Me.roundrectButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
            Me.roundrectButton.ImageTransparentColor = System.Drawing.Color.Magenta
            Me.roundrectButton.Name = "roundrectButton"
            Me.roundrectButton.Size = New System.Drawing.Size(69, 22)
            Me.roundrectButton.Text = "RoundRect"
            '
            'ellipseButton
            '
            Me.ellipseButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
            Me.ellipseButton.ImageTransparentColor = System.Drawing.Color.Magenta
            Me.ellipseButton.Name = "ellipseButton"
            Me.ellipseButton.Size = New System.Drawing.Size(44, 22)
            Me.ellipseButton.Text = "Ellipse"
            '
            'geometryButton
            '
            Me.geometryButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
            Me.geometryButton.ImageTransparentColor = System.Drawing.Color.Magenta
            Me.geometryButton.Name = "geometryButton"
            Me.geometryButton.Size = New System.Drawing.Size(63, 22)
            Me.geometryButton.Text = "Geometry"
            '
            'toolStripSeparator4
            '
            Me.toolStripSeparator4.Name = "toolStripSeparator4"
            Me.toolStripSeparator4.Size = New System.Drawing.Size(6, 25)
            '
            'textButton
            '
            Me.textButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
            Me.textButton.ImageTransparentColor = System.Drawing.Color.Magenta
            Me.textButton.Name = "textButton"
            Me.textButton.Size = New System.Drawing.Size(33, 22)
            Me.textButton.Text = "Text"
            '
            'toolStripSeparator3
            '
            Me.toolStripSeparator3.Name = "toolStripSeparator3"
            Me.toolStripSeparator3.Size = New System.Drawing.Size(6, 25)
            '
            'bitmapButton
            '
            Me.bitmapButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
            Me.bitmapButton.ImageTransparentColor = System.Drawing.Color.Magenta
            Me.bitmapButton.Name = "bitmapButton"
            Me.bitmapButton.Size = New System.Drawing.Size(49, 22)
            Me.bitmapButton.Text = "Bitmap"
            '
            'traparencyList
            '
            Me.traparencyList.AutoSize = False
            Me.traparencyList.Items.AddRange(New Object() {"1.0", "0.9", "0.75", "0.5", "0.25", "0.2", "0.1", "0.1"})
            Me.traparencyList.Name = "traparencyList"
            Me.traparencyList.Size = New System.Drawing.Size(42, 23)
            '
            'toolStripSeparator5
            '
            Me.toolStripSeparator5.Name = "toolStripSeparator5"
            Me.toolStripSeparator5.Size = New System.Drawing.Size(6, 25)
            '
            'brushButton
            '
            Me.brushButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
            Me.brushButton.ImageTransparentColor = System.Drawing.Color.Magenta
            Me.brushButton.Name = "brushButton"
            Me.brushButton.Size = New System.Drawing.Size(52, 22)
            Me.brushButton.Text = "Brushes"
            '
            'toolStripSeparator1
            '
            Me.toolStripSeparator1.Name = "toolStripSeparator1"
            Me.toolStripSeparator1.Size = New System.Drawing.Size(6, 25)
            '
            'clearButton
            '
            Me.clearButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
            Me.clearButton.ImageTransparentColor = System.Drawing.Color.Magenta
            Me.clearButton.Name = "clearButton"
            Me.clearButton.Size = New System.Drawing.Size(38, 22)
            Me.clearButton.Text = "Clear"
            Me.clearButton.ToolTipText = "Clear All"
            '
            'ToolStripSeparator8
            '
            Me.ToolStripSeparator8.Name = "ToolStripSeparator8"
            Me.ToolStripSeparator8.Size = New System.Drawing.Size(6, 25)
            '
            'saveButton
            '
            Me.saveButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
            Me.saveButton.ImageTransparentColor = System.Drawing.Color.Magenta
            Me.saveButton.Name = "saveButton"
            Me.saveButton.Size = New System.Drawing.Size(44, 22)
            Me.saveButton.Text = "Save..."
            '
            'Paint2DForm
            '
            Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.ClientSize = New System.Drawing.Size(869, 605)
            Me.Controls.Add(Me.toolStrip1)
            Me.Controls.Add(Me.renderControl)
            Me.Name = "Paint2DForm"
            Me.Text = "D2D Paint Demo"
            Me.toolStrip1.ResumeLayout(False)
            Me.toolStrip1.PerformLayout()
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub

#End Region

        Private WithEvents renderControl As Microsoft.WindowsAPICodePack.DirectX.Controls.RenderControl
        Private WithEvents toolStrip1 As System.Windows.Forms.ToolStrip
        Private WithEvents brushButton As System.Windows.Forms.ToolStripButton
        Private WithEvents lineButton As System.Windows.Forms.ToolStripButton
        Private WithEvents bitmapButton As System.Windows.Forms.ToolStripButton
        Private WithEvents roundrectButton As System.Windows.Forms.ToolStripButton
        Private toolStripSeparator1 As System.Windows.Forms.ToolStripSeparator
        Private WithEvents strokeWidths As System.Windows.Forms.ToolStripComboBox
        Private WithEvents traparencyList As System.Windows.Forms.ToolStripComboBox
        Private WithEvents clearButton As System.Windows.Forms.ToolStripButton
        Private WithEvents rectButton As System.Windows.Forms.ToolStripButton
        Private WithEvents ellipseButton As System.Windows.Forms.ToolStripButton
        Private WithEvents textButton As System.Windows.Forms.ToolStripButton
        Private toolStripSeparator3 As System.Windows.Forms.ToolStripSeparator
        Private WithEvents fillButton As System.Windows.Forms.ToolStripButton
        Private WithEvents geometryButton As System.Windows.Forms.ToolStripButton
        Private WithEvents arrowButton As System.Windows.Forms.ToolStripButton
        Private toolStripSeparator2 As System.Windows.Forms.ToolStripSeparator
        Private toolStripSeparator4 As System.Windows.Forms.ToolStripSeparator
        Private toolStripSeparator5 As System.Windows.Forms.ToolStripSeparator
        Private toolStripSeparator6 As System.Windows.Forms.ToolStripSeparator
        Private toolStripSeparator7 As System.Windows.Forms.ToolStripSeparator
        Friend WithEvents saveButton As System.Windows.Forms.ToolStripButton
        Friend WithEvents ToolStripSeparator8 As System.Windows.Forms.ToolStripSeparator
	End Class
End Namespace

