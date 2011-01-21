Imports Microsoft.VisualBasic
Imports System
Namespace D2DPaint
	Partial Public Class BrushDialog
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
            Dim gammaLabel As System.Windows.Forms.Label
            Dim label6 As System.Windows.Forms.Label
            Dim label5 As System.Windows.Forms.Label
            Dim label7 As System.Windows.Forms.Label
            Me.transparencyValues = New System.Windows.Forms.ComboBox
            Me.transparency = New System.Windows.Forms.Label
            Me.solidColorButton = New System.Windows.Forms.Button
            Me.colorLabel = New System.Windows.Forms.Label
            Me.colorDialog1 = New System.Windows.Forms.ColorDialog
            Me.addBrushButton = New System.Windows.Forms.Button
            Me.brushesTabs = New System.Windows.Forms.TabControl
            Me.solidColorPage = New System.Windows.Forms.TabPage
            Me.bitmapBrushPage = New System.Windows.Forms.TabPage
            Me.imageFileLabel = New System.Windows.Forms.Label
            Me.extendedModeYComboBox = New System.Windows.Forms.ComboBox
            Me.extendedModeXComboBox = New System.Windows.Forms.ComboBox
            Me.comboBox2 = New System.Windows.Forms.ComboBox
            Me.label4 = New System.Windows.Forms.Label
            Me.label3 = New System.Windows.Forms.Label
            Me.comboBox1 = New System.Windows.Forms.ComboBox
            Me.label2 = New System.Windows.Forms.Label
            Me.label1 = New System.Windows.Forms.Label
            Me.addBitmapBrushBotton = New System.Windows.Forms.Button
            Me.button2 = New System.Windows.Forms.Button
            Me.linearBrushPage = New System.Windows.Forms.TabPage
            Me.button3 = New System.Windows.Forms.Button
            Me.gradBrushExtendModeCombo = New System.Windows.Forms.ComboBox
            Me.gammaComboBox = New System.Windows.Forms.ComboBox
            Me.gradBrushColor2Label = New System.Windows.Forms.Label
            Me.gradBrushColor1Label = New System.Windows.Forms.Label
            Me.gradiantBrushColor2Button = New System.Windows.Forms.Button
            Me.gradiantBrushColor1button = New System.Windows.Forms.Button
            Me.graidantBrushPage = New System.Windows.Forms.TabPage
            Me.button4 = New System.Windows.Forms.Button
            Me.radialExtendCombo = New System.Windows.Forms.ComboBox
            Me.radialGammaCombo = New System.Windows.Forms.ComboBox
            Me.radialBrushColor2Label = New System.Windows.Forms.Label
            Me.radialBrushColor1Label = New System.Windows.Forms.Label
            Me.SelectRadialColor2 = New System.Windows.Forms.Button
            Me.SelectRadialColor1 = New System.Windows.Forms.Button
            Me.brushesList = New System.Windows.Forms.ListBox
            Me.button1 = New System.Windows.Forms.Button
            gammaLabel = New System.Windows.Forms.Label
            label6 = New System.Windows.Forms.Label
            label5 = New System.Windows.Forms.Label
            label7 = New System.Windows.Forms.Label
            Me.brushesTabs.SuspendLayout()
            Me.solidColorPage.SuspendLayout()
            Me.bitmapBrushPage.SuspendLayout()
            Me.linearBrushPage.SuspendLayout()
            Me.graidantBrushPage.SuspendLayout()
            Me.SuspendLayout()
            '
            'gammaLabel
            '
            gammaLabel.AutoSize = True
            gammaLabel.Location = New System.Drawing.Point(16, 97)
            gammaLabel.Name = "gammaLabel"
            gammaLabel.Size = New System.Drawing.Size(43, 13)
            gammaLabel.TabIndex = 4
            gammaLabel.Text = "Gamma"
            '
            'label6
            '
            label6.AutoSize = True
            label6.Location = New System.Drawing.Point(16, 128)
            label6.Name = "label6"
            label6.Size = New System.Drawing.Size(70, 13)
            label6.TabIndex = 5
            label6.Text = "Extend Mode"
            '
            'label5
            '
            label5.AutoSize = True
            label5.Location = New System.Drawing.Point(43, 136)
            label5.Name = "label5"
            label5.Size = New System.Drawing.Size(70, 13)
            label5.TabIndex = 13
            label5.Text = "Extend Mode"
            '
            'label7
            '
            label7.AutoSize = True
            label7.Location = New System.Drawing.Point(43, 105)
            label7.Name = "label7"
            label7.Size = New System.Drawing.Size(43, 13)
            label7.TabIndex = 12
            label7.Text = "Gamma"
            '
            'transparencyValues
            '
            Me.transparencyValues.FormattingEnabled = True
            Me.transparencyValues.Items.AddRange(New Object() {"0.00", "0.10", "0.25", "0.40", "0.50", "0.60", "0.75", "0.90", "0.95", "1.00"})
            Me.transparencyValues.Location = New System.Drawing.Point(161, 67)
            Me.transparencyValues.Name = "transparencyValues"
            Me.transparencyValues.Size = New System.Drawing.Size(121, 21)
            Me.transparencyValues.TabIndex = 3
            Me.transparencyValues.Text = "1.00"
            '
            'transparency
            '
            Me.transparency.AutoSize = True
            Me.transparency.BackColor = System.Drawing.SystemColors.ButtonHighlight
            Me.transparency.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
            Me.transparency.Location = New System.Drawing.Point(24, 65)
            Me.transparency.MinimumSize = New System.Drawing.Size(113, 23)
            Me.transparency.Name = "transparency"
            Me.transparency.Size = New System.Drawing.Size(113, 23)
            Me.transparency.TabIndex = 2
            Me.transparency.Text = "Transparency"
            '
            'solidColorButton
            '
            Me.solidColorButton.Location = New System.Drawing.Point(24, 32)
            Me.solidColorButton.Name = "solidColorButton"
            Me.solidColorButton.Size = New System.Drawing.Size(113, 23)
            Me.solidColorButton.TabIndex = 1
            Me.solidColorButton.Text = "Select Color..."
            Me.solidColorButton.UseVisualStyleBackColor = True
            '
            'colorLabel
            '
            Me.colorLabel.AutoSize = True
            Me.colorLabel.BackColor = System.Drawing.SystemColors.ButtonHighlight
            Me.colorLabel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
            Me.colorLabel.Location = New System.Drawing.Point(161, 32)
            Me.colorLabel.MinimumSize = New System.Drawing.Size(130, 23)
            Me.colorLabel.Name = "colorLabel"
            Me.colorLabel.Size = New System.Drawing.Size(130, 23)
            Me.colorLabel.TabIndex = 0
            Me.colorLabel.Text = "R = 0, G = 0, B = 0, A = 1"
            '
            'addBrushButton
            '
            Me.addBrushButton.Location = New System.Drawing.Point(114, 193)
            Me.addBrushButton.Name = "addBrushButton"
            Me.addBrushButton.Size = New System.Drawing.Size(75, 23)
            Me.addBrushButton.TabIndex = 2
            Me.addBrushButton.Text = "Add Brush"
            Me.addBrushButton.UseVisualStyleBackColor = True
            '
            'brushesTabs
            '
            Me.brushesTabs.Controls.Add(Me.solidColorPage)
            Me.brushesTabs.Controls.Add(Me.bitmapBrushPage)
            Me.brushesTabs.Controls.Add(Me.linearBrushPage)
            Me.brushesTabs.Controls.Add(Me.graidantBrushPage)
            Me.brushesTabs.Location = New System.Drawing.Point(12, 12)
            Me.brushesTabs.Name = "brushesTabs"
            Me.brushesTabs.SelectedIndex = 0
            Me.brushesTabs.Size = New System.Drawing.Size(361, 248)
            Me.brushesTabs.TabIndex = 4
            '
            'solidColorPage
            '
            Me.solidColorPage.BackColor = System.Drawing.SystemColors.Control
            Me.solidColorPage.Controls.Add(Me.transparencyValues)
            Me.solidColorPage.Controls.Add(Me.transparency)
            Me.solidColorPage.Controls.Add(Me.addBrushButton)
            Me.solidColorPage.Controls.Add(Me.colorLabel)
            Me.solidColorPage.Controls.Add(Me.solidColorButton)
            Me.solidColorPage.Location = New System.Drawing.Point(4, 22)
            Me.solidColorPage.Name = "solidColorPage"
            Me.solidColorPage.Padding = New System.Windows.Forms.Padding(3)
            Me.solidColorPage.Size = New System.Drawing.Size(353, 222)
            Me.solidColorPage.TabIndex = 0
            Me.solidColorPage.Text = "Solid Color"
            '
            'bitmapBrushPage
            '
            Me.bitmapBrushPage.BackColor = System.Drawing.SystemColors.Control
            Me.bitmapBrushPage.Controls.Add(Me.imageFileLabel)
            Me.bitmapBrushPage.Controls.Add(Me.extendedModeYComboBox)
            Me.bitmapBrushPage.Controls.Add(Me.extendedModeXComboBox)
            Me.bitmapBrushPage.Controls.Add(Me.comboBox2)
            Me.bitmapBrushPage.Controls.Add(Me.label4)
            Me.bitmapBrushPage.Controls.Add(Me.label3)
            Me.bitmapBrushPage.Controls.Add(Me.comboBox1)
            Me.bitmapBrushPage.Controls.Add(Me.label2)
            Me.bitmapBrushPage.Controls.Add(Me.label1)
            Me.bitmapBrushPage.Controls.Add(Me.addBitmapBrushBotton)
            Me.bitmapBrushPage.Controls.Add(Me.button2)
            Me.bitmapBrushPage.Location = New System.Drawing.Point(4, 22)
            Me.bitmapBrushPage.Name = "bitmapBrushPage"
            Me.bitmapBrushPage.Padding = New System.Windows.Forms.Padding(3)
            Me.bitmapBrushPage.Size = New System.Drawing.Size(353, 222)
            Me.bitmapBrushPage.TabIndex = 1
            Me.bitmapBrushPage.Text = "Bitmap"
            '
            'imageFileLabel
            '
            Me.imageFileLabel.AutoSize = True
            Me.imageFileLabel.BackColor = System.Drawing.SystemColors.ButtonHighlight
            Me.imageFileLabel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
            Me.imageFileLabel.Location = New System.Drawing.Point(158, 29)
            Me.imageFileLabel.MinimumSize = New System.Drawing.Size(130, 23)
            Me.imageFileLabel.Name = "imageFileLabel"
            Me.imageFileLabel.Size = New System.Drawing.Size(130, 23)
            Me.imageFileLabel.TabIndex = 7
            '
            'extendedModeYComboBox
            '
            Me.extendedModeYComboBox.FormattingEnabled = True
            Me.extendedModeYComboBox.Items.AddRange(New Object() {"Clamp", "Wrap", "Mirror"})
            Me.extendedModeYComboBox.Location = New System.Drawing.Point(158, 152)
            Me.extendedModeYComboBox.Name = "extendedModeYComboBox"
            Me.extendedModeYComboBox.Size = New System.Drawing.Size(121, 21)
            Me.extendedModeYComboBox.TabIndex = 6
            Me.extendedModeYComboBox.Text = "Mirror"
            '
            'extendedModeXComboBox
            '
            Me.extendedModeXComboBox.FormattingEnabled = True
            Me.extendedModeXComboBox.Items.AddRange(New Object() {"Clamp", "Wrap", "Mirror"})
            Me.extendedModeXComboBox.Location = New System.Drawing.Point(158, 114)
            Me.extendedModeXComboBox.Name = "extendedModeXComboBox"
            Me.extendedModeXComboBox.Size = New System.Drawing.Size(121, 21)
            Me.extendedModeXComboBox.TabIndex = 6
            Me.extendedModeXComboBox.Text = "Mirror"
            '
            'comboBox2
            '
            Me.comboBox2.FormattingEnabled = True
            Me.comboBox2.Items.AddRange(New Object() {"0.00", "0.10", "0.25", "0.40", "0.50", "0.60", "0.75", "0.90", "0.95", "1.00"})
            Me.comboBox2.Location = New System.Drawing.Point(158, 77)
            Me.comboBox2.Name = "comboBox2"
            Me.comboBox2.Size = New System.Drawing.Size(121, 21)
            Me.comboBox2.TabIndex = 6
            Me.comboBox2.Text = "1.00"
            '
            'label4
            '
            Me.label4.AutoSize = True
            Me.label4.BackColor = System.Drawing.SystemColors.ButtonHighlight
            Me.label4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
            Me.label4.Location = New System.Drawing.Point(21, 150)
            Me.label4.MinimumSize = New System.Drawing.Size(113, 23)
            Me.label4.Name = "label4"
            Me.label4.Size = New System.Drawing.Size(113, 23)
            Me.label4.TabIndex = 5
            Me.label4.Text = "Extend Mode Y"
            '
            'label3
            '
            Me.label3.AutoSize = True
            Me.label3.BackColor = System.Drawing.SystemColors.ButtonHighlight
            Me.label3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
            Me.label3.Location = New System.Drawing.Point(21, 112)
            Me.label3.MinimumSize = New System.Drawing.Size(113, 23)
            Me.label3.Name = "label3"
            Me.label3.Size = New System.Drawing.Size(113, 23)
            Me.label3.TabIndex = 5
            Me.label3.Text = "Extend Mode X"
            '
            'comboBox1
            '
            Me.comboBox1.FormattingEnabled = True
            Me.comboBox1.Items.AddRange(New Object() {"0.00", "0.10", "0.25", "0.40", "0.50", "0.60", "0.75", "0.90", "0.95", "1.00"})
            Me.comboBox1.Location = New System.Drawing.Point(158, 77)
            Me.comboBox1.Name = "comboBox1"
            Me.comboBox1.Size = New System.Drawing.Size(121, 21)
            Me.comboBox1.TabIndex = 6
            '
            'label2
            '
            Me.label2.AutoSize = True
            Me.label2.BackColor = System.Drawing.SystemColors.ButtonHighlight
            Me.label2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
            Me.label2.Location = New System.Drawing.Point(21, 75)
            Me.label2.MinimumSize = New System.Drawing.Size(113, 23)
            Me.label2.Name = "label2"
            Me.label2.Size = New System.Drawing.Size(113, 23)
            Me.label2.TabIndex = 5
            Me.label2.Text = "Transparency"
            '
            'label1
            '
            Me.label1.AutoSize = True
            Me.label1.BackColor = System.Drawing.SystemColors.ButtonHighlight
            Me.label1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
            Me.label1.Location = New System.Drawing.Point(21, 75)
            Me.label1.MinimumSize = New System.Drawing.Size(113, 23)
            Me.label1.Name = "label1"
            Me.label1.Size = New System.Drawing.Size(113, 23)
            Me.label1.TabIndex = 5
            Me.label1.Text = "Transparency"
            '
            'addBitmapBrushBotton
            '
            Me.addBitmapBrushBotton.Location = New System.Drawing.Point(121, 193)
            Me.addBitmapBrushBotton.Name = "addBitmapBrushBotton"
            Me.addBitmapBrushBotton.Size = New System.Drawing.Size(75, 23)
            Me.addBitmapBrushBotton.TabIndex = 4
            Me.addBitmapBrushBotton.Text = "Add Brush"
            Me.addBitmapBrushBotton.UseVisualStyleBackColor = True
            '
            'button2
            '
            Me.button2.Location = New System.Drawing.Point(21, 29)
            Me.button2.Name = "button2"
            Me.button2.Size = New System.Drawing.Size(130, 23)
            Me.button2.TabIndex = 0
            Me.button2.Text = "Select Image File..."
            Me.button2.UseVisualStyleBackColor = True
            '
            'linearBrushPage
            '
            Me.linearBrushPage.BackColor = System.Drawing.SystemColors.Control
            Me.linearBrushPage.Controls.Add(Me.button3)
            Me.linearBrushPage.Controls.Add(Me.gradBrushExtendModeCombo)
            Me.linearBrushPage.Controls.Add(Me.gammaComboBox)
            Me.linearBrushPage.Controls.Add(label6)
            Me.linearBrushPage.Controls.Add(gammaLabel)
            Me.linearBrushPage.Controls.Add(Me.gradBrushColor2Label)
            Me.linearBrushPage.Controls.Add(Me.gradBrushColor1Label)
            Me.linearBrushPage.Controls.Add(Me.gradiantBrushColor2Button)
            Me.linearBrushPage.Controls.Add(Me.gradiantBrushColor1button)
            Me.linearBrushPage.Location = New System.Drawing.Point(4, 22)
            Me.linearBrushPage.Name = "linearBrushPage"
            Me.linearBrushPage.Size = New System.Drawing.Size(353, 222)
            Me.linearBrushPage.TabIndex = 2
            Me.linearBrushPage.Text = "Linear Gradiant"
            '
            'button3
            '
            Me.button3.Location = New System.Drawing.Point(101, 161)
            Me.button3.Name = "button3"
            Me.button3.Size = New System.Drawing.Size(75, 23)
            Me.button3.TabIndex = 7
            Me.button3.Text = "Add Brush"
            Me.button3.UseVisualStyleBackColor = True
            '
            'gradBrushExtendModeCombo
            '
            Me.gradBrushExtendModeCombo.FormattingEnabled = True
            Me.gradBrushExtendModeCombo.Items.AddRange(New Object() {"Clamp", "Wrap", "Mirror"})
            Me.gradBrushExtendModeCombo.Location = New System.Drawing.Point(153, 125)
            Me.gradBrushExtendModeCombo.Name = "gradBrushExtendModeCombo"
            Me.gradBrushExtendModeCombo.Size = New System.Drawing.Size(121, 21)
            Me.gradBrushExtendModeCombo.TabIndex = 6
            Me.gradBrushExtendModeCombo.Text = "Wrap"
            '
            'gammaComboBox
            '
            Me.gammaComboBox.FormattingEnabled = True
            Me.gammaComboBox.Items.AddRange(New Object() {"Linear (1.0)", "StandardRgb (2.2)"})
            Me.gammaComboBox.Location = New System.Drawing.Point(153, 94)
            Me.gammaComboBox.Name = "gammaComboBox"
            Me.gammaComboBox.Size = New System.Drawing.Size(121, 21)
            Me.gammaComboBox.TabIndex = 6
            Me.gammaComboBox.Text = "Linear (1.0)"
            '
            'gradBrushColor2Label
            '
            Me.gradBrushColor2Label.AutoSize = True
            Me.gradBrushColor2Label.BackColor = System.Drawing.SystemColors.ButtonHighlight
            Me.gradBrushColor2Label.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
            Me.gradBrushColor2Label.Location = New System.Drawing.Point(153, 51)
            Me.gradBrushColor2Label.MinimumSize = New System.Drawing.Size(130, 23)
            Me.gradBrushColor2Label.Name = "gradBrushColor2Label"
            Me.gradBrushColor2Label.Size = New System.Drawing.Size(130, 23)
            Me.gradBrushColor2Label.TabIndex = 2
            Me.gradBrushColor2Label.Text = "R = 1, G = 1, B = 1, A = 1"
            '
            'gradBrushColor1Label
            '
            Me.gradBrushColor1Label.AutoSize = True
            Me.gradBrushColor1Label.BackColor = System.Drawing.SystemColors.ButtonHighlight
            Me.gradBrushColor1Label.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
            Me.gradBrushColor1Label.Location = New System.Drawing.Point(153, 22)
            Me.gradBrushColor1Label.MinimumSize = New System.Drawing.Size(130, 23)
            Me.gradBrushColor1Label.Name = "gradBrushColor1Label"
            Me.gradBrushColor1Label.Size = New System.Drawing.Size(130, 23)
            Me.gradBrushColor1Label.TabIndex = 2
            Me.gradBrushColor1Label.Text = "R = 0, G = 0, B = 0, A = 1"
            '
            'gradiantBrushColor2Button
            '
            Me.gradiantBrushColor2Button.Location = New System.Drawing.Point(16, 51)
            Me.gradiantBrushColor2Button.Name = "gradiantBrushColor2Button"
            Me.gradiantBrushColor2Button.Size = New System.Drawing.Size(113, 23)
            Me.gradiantBrushColor2Button.TabIndex = 3
            Me.gradiantBrushColor2Button.Text = "Select Color 2..."
            Me.gradiantBrushColor2Button.UseVisualStyleBackColor = True
            '
            'gradiantBrushColor1button
            '
            Me.gradiantBrushColor1button.Location = New System.Drawing.Point(16, 22)
            Me.gradiantBrushColor1button.Name = "gradiantBrushColor1button"
            Me.gradiantBrushColor1button.Size = New System.Drawing.Size(113, 23)
            Me.gradiantBrushColor1button.TabIndex = 3
            Me.gradiantBrushColor1button.Text = "Select Color 1..."
            Me.gradiantBrushColor1button.UseVisualStyleBackColor = True
            '
            'graidantBrushPage
            '
            Me.graidantBrushPage.BackColor = System.Drawing.SystemColors.Control
            Me.graidantBrushPage.Controls.Add(Me.button4)
            Me.graidantBrushPage.Controls.Add(Me.radialExtendCombo)
            Me.graidantBrushPage.Controls.Add(Me.radialGammaCombo)
            Me.graidantBrushPage.Controls.Add(label5)
            Me.graidantBrushPage.Controls.Add(label7)
            Me.graidantBrushPage.Controls.Add(Me.radialBrushColor2Label)
            Me.graidantBrushPage.Controls.Add(Me.radialBrushColor1Label)
            Me.graidantBrushPage.Controls.Add(Me.SelectRadialColor2)
            Me.graidantBrushPage.Controls.Add(Me.SelectRadialColor1)
            Me.graidantBrushPage.Location = New System.Drawing.Point(4, 22)
            Me.graidantBrushPage.Name = "graidantBrushPage"
            Me.graidantBrushPage.Size = New System.Drawing.Size(353, 222)
            Me.graidantBrushPage.TabIndex = 3
            Me.graidantBrushPage.Text = "Radial  Gradiant"
            '
            'button4
            '
            Me.button4.Location = New System.Drawing.Point(128, 169)
            Me.button4.Name = "button4"
            Me.button4.Size = New System.Drawing.Size(75, 23)
            Me.button4.TabIndex = 16
            Me.button4.Text = "Add Brush"
            Me.button4.UseVisualStyleBackColor = True
            '
            'radialExtendCombo
            '
            Me.radialExtendCombo.FormattingEnabled = True
            Me.radialExtendCombo.Items.AddRange(New Object() {"Clamp", "Wrap", "Mirror"})
            Me.radialExtendCombo.Location = New System.Drawing.Point(180, 133)
            Me.radialExtendCombo.Name = "radialExtendCombo"
            Me.radialExtendCombo.Size = New System.Drawing.Size(121, 21)
            Me.radialExtendCombo.TabIndex = 14
            Me.radialExtendCombo.Text = "Wrap"
            '
            'radialGammaCombo
            '
            Me.radialGammaCombo.FormattingEnabled = True
            Me.radialGammaCombo.Items.AddRange(New Object() {"Linear (1.0)", "StandardRgb (2.2)"})
            Me.radialGammaCombo.Location = New System.Drawing.Point(180, 102)
            Me.radialGammaCombo.Name = "radialGammaCombo"
            Me.radialGammaCombo.Size = New System.Drawing.Size(121, 21)
            Me.radialGammaCombo.TabIndex = 15
            Me.radialGammaCombo.Text = "Linear (1.0)"
            '
            'radialBrushColor2Label
            '
            Me.radialBrushColor2Label.AutoSize = True
            Me.radialBrushColor2Label.BackColor = System.Drawing.SystemColors.ButtonHighlight
            Me.radialBrushColor2Label.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
            Me.radialBrushColor2Label.Location = New System.Drawing.Point(180, 59)
            Me.radialBrushColor2Label.MinimumSize = New System.Drawing.Size(130, 23)
            Me.radialBrushColor2Label.Name = "radialBrushColor2Label"
            Me.radialBrushColor2Label.Size = New System.Drawing.Size(130, 23)
            Me.radialBrushColor2Label.TabIndex = 9
            Me.radialBrushColor2Label.Text = "R = 1, G = 1, B = 1, A = 1"
            '
            'radialBrushColor1Label
            '
            Me.radialBrushColor1Label.AutoSize = True
            Me.radialBrushColor1Label.BackColor = System.Drawing.SystemColors.ButtonHighlight
            Me.radialBrushColor1Label.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
            Me.radialBrushColor1Label.Location = New System.Drawing.Point(180, 30)
            Me.radialBrushColor1Label.MinimumSize = New System.Drawing.Size(130, 23)
            Me.radialBrushColor1Label.Name = "radialBrushColor1Label"
            Me.radialBrushColor1Label.Size = New System.Drawing.Size(130, 23)
            Me.radialBrushColor1Label.TabIndex = 8
            Me.radialBrushColor1Label.Text = "R = 0, G = 0, B = 0, A = 1"
            '
            'SelectRadialColor2
            '
            Me.SelectRadialColor2.Location = New System.Drawing.Point(43, 59)
            Me.SelectRadialColor2.Name = "SelectRadialColor2"
            Me.SelectRadialColor2.Size = New System.Drawing.Size(113, 23)
            Me.SelectRadialColor2.TabIndex = 11
            Me.SelectRadialColor2.Text = "Select Color 2..."
            Me.SelectRadialColor2.UseVisualStyleBackColor = True
            '
            'SelectRadialColor1
            '
            Me.SelectRadialColor1.Location = New System.Drawing.Point(43, 30)
            Me.SelectRadialColor1.Name = "SelectRadialColor1"
            Me.SelectRadialColor1.Size = New System.Drawing.Size(113, 23)
            Me.SelectRadialColor1.TabIndex = 10
            Me.SelectRadialColor1.Text = "Select Color 1..."
            Me.SelectRadialColor1.UseVisualStyleBackColor = True
            '
            'brushesList
            '
            Me.brushesList.FormattingEnabled = True
            Me.brushesList.Location = New System.Drawing.Point(16, 266)
            Me.brushesList.Name = "brushesList"
            Me.brushesList.Size = New System.Drawing.Size(353, 173)
            Me.brushesList.TabIndex = 5
            '
            'button1
            '
            Me.button1.Location = New System.Drawing.Point(114, 459)
            Me.button1.Name = "button1"
            Me.button1.Size = New System.Drawing.Size(157, 23)
            Me.button1.TabIndex = 6
            Me.button1.Text = "Close"
            Me.button1.UseVisualStyleBackColor = True
            '
            'BrushDialog
            '
            Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.ClientSize = New System.Drawing.Size(385, 494)
            Me.Controls.Add(Me.button1)
            Me.Controls.Add(Me.brushesList)
            Me.Controls.Add(Me.brushesTabs)
            Me.Name = "BrushDialog"
            Me.RightToLeftLayout = True
            Me.Text = "Select Brush"
            Me.brushesTabs.ResumeLayout(False)
            Me.solidColorPage.ResumeLayout(False)
            Me.solidColorPage.PerformLayout()
            Me.bitmapBrushPage.ResumeLayout(False)
            Me.bitmapBrushPage.PerformLayout()
            Me.linearBrushPage.ResumeLayout(False)
            Me.linearBrushPage.PerformLayout()
            Me.graidantBrushPage.ResumeLayout(False)
            Me.graidantBrushPage.PerformLayout()
            Me.ResumeLayout(False)

        End Sub

		#End Region

		Private colorDialog1 As System.Windows.Forms.ColorDialog
		Private colorLabel As System.Windows.Forms.Label
		Private WithEvents solidColorButton As System.Windows.Forms.Button
		Private WithEvents addBrushButton As System.Windows.Forms.Button
		Private WithEvents transparencyValues As System.Windows.Forms.ComboBox
		Private transparency As System.Windows.Forms.Label
		Private brushesTabs As System.Windows.Forms.TabControl
		Private solidColorPage As System.Windows.Forms.TabPage
		Private bitmapBrushPage As System.Windows.Forms.TabPage
		Private linearBrushPage As System.Windows.Forms.TabPage
		Private graidantBrushPage As System.Windows.Forms.TabPage
		Private WithEvents brushesList As System.Windows.Forms.ListBox
		Private WithEvents button1 As System.Windows.Forms.Button
		Private WithEvents button2 As System.Windows.Forms.Button
		Private imageFileLabel As System.Windows.Forms.Label
		Private comboBox1 As System.Windows.Forms.ComboBox
		Private label1 As System.Windows.Forms.Label
		Private WithEvents addBitmapBrushBotton As System.Windows.Forms.Button
		Private extendedModeYComboBox As System.Windows.Forms.ComboBox
		Private extendedModeXComboBox As System.Windows.Forms.ComboBox
		Private WithEvents comboBox2 As System.Windows.Forms.ComboBox
		Private label4 As System.Windows.Forms.Label
		Private label3 As System.Windows.Forms.Label
		Private label2 As System.Windows.Forms.Label
		Private gradBrushColor2Label As System.Windows.Forms.Label
		Private gradBrushColor1Label As System.Windows.Forms.Label
		Private WithEvents gradiantBrushColor2Button As System.Windows.Forms.Button
		Private WithEvents gradiantBrushColor1button As System.Windows.Forms.Button
		Private WithEvents button3 As System.Windows.Forms.Button
		Private gradBrushExtendModeCombo As System.Windows.Forms.ComboBox
		Private gammaComboBox As System.Windows.Forms.ComboBox
		Private WithEvents button4 As System.Windows.Forms.Button
		Private radialExtendCombo As System.Windows.Forms.ComboBox
		Private radialGammaCombo As System.Windows.Forms.ComboBox
		Private radialBrushColor2Label As System.Windows.Forms.Label
		Private radialBrushColor1Label As System.Windows.Forms.Label
		Private WithEvents SelectRadialColor2 As System.Windows.Forms.Button
		Private WithEvents SelectRadialColor1 As System.Windows.Forms.Button
	End Class
End Namespace