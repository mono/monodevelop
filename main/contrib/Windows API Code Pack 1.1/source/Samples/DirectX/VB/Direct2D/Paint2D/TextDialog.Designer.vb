Imports Microsoft.VisualBasic
Imports System
Namespace D2DPaint
	Partial Public Class TextDialog
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
            Dim CancelTextButton As System.Windows.Forms.Button
            Me.AddTextButton = New System.Windows.Forms.Button
            Me.underLineCheckBox = New System.Windows.Forms.CheckBox
            Me.label1 = New System.Windows.Forms.Label
            Me.label2 = New System.Windows.Forms.Label
            Me.label3 = New System.Windows.Forms.Label
            Me.label4 = New System.Windows.Forms.Label
            Me.label5 = New System.Windows.Forms.Label
            Me.sizeCombo = New System.Windows.Forms.ComboBox
            Me.styleCombo = New System.Windows.Forms.ComboBox
            Me.weightCombo = New System.Windows.Forms.ComboBox
            Me.stretchCombo = New System.Windows.Forms.ComboBox
            Me.textBox = New System.Windows.Forms.TextBox
            Me.strikethroughCheckBox = New System.Windows.Forms.CheckBox
            Me.fontFamilyCombo = New D2DPaint.FontEnumComboBox
            CancelTextButton = New System.Windows.Forms.Button
            Me.SuspendLayout()
            '
            'CancelTextButton
            '
            CancelTextButton.Location = New System.Drawing.Point(287, 221)
            CancelTextButton.Name = "CancelTextButton"
            CancelTextButton.Size = New System.Drawing.Size(138, 31)
            CancelTextButton.TabIndex = 1
            CancelTextButton.Text = "Cancel"
            CancelTextButton.UseVisualStyleBackColor = True
            AddHandler CancelTextButton.Click, AddressOf Me.CancelTextButton_Click
            '
            'AddTextButton
            '
            Me.AddTextButton.Location = New System.Drawing.Point(99, 221)
            Me.AddTextButton.Name = "AddTextButton"
            Me.AddTextButton.Size = New System.Drawing.Size(138, 31)
            Me.AddTextButton.TabIndex = 0
            Me.AddTextButton.Text = "Add Text"
            Me.AddTextButton.UseVisualStyleBackColor = True
            '
            'underLineCheckBox
            '
            Me.underLineCheckBox.AutoSize = True
            Me.underLineCheckBox.Location = New System.Drawing.Point(37, 183)
            Me.underLineCheckBox.Name = "underLineCheckBox"
            Me.underLineCheckBox.Size = New System.Drawing.Size(71, 17)
            Me.underLineCheckBox.TabIndex = 2
            Me.underLineCheckBox.Text = "Underline"
            Me.underLineCheckBox.UseVisualStyleBackColor = True
            '
            'label1
            '
            Me.label1.AutoSize = True
            Me.label1.Location = New System.Drawing.Point(34, 30)
            Me.label1.Name = "label1"
            Me.label1.Size = New System.Drawing.Size(59, 13)
            Me.label1.TabIndex = 3
            Me.label1.Text = "Font Name"
            '
            'label2
            '
            Me.label2.AutoSize = True
            Me.label2.Location = New System.Drawing.Point(34, 58)
            Me.label2.Name = "label2"
            Me.label2.Size = New System.Drawing.Size(51, 13)
            Me.label2.TabIndex = 4
            Me.label2.Text = "Font Size"
            '
            'label3
            '
            Me.label3.AutoSize = True
            Me.label3.Location = New System.Drawing.Point(34, 86)
            Me.label3.Name = "label3"
            Me.label3.Size = New System.Drawing.Size(54, 13)
            Me.label3.TabIndex = 4
            Me.label3.Text = "Font Style"
            '
            'label4
            '
            Me.label4.AutoSize = True
            Me.label4.Location = New System.Drawing.Point(34, 114)
            Me.label4.Name = "label4"
            Me.label4.Size = New System.Drawing.Size(65, 13)
            Me.label4.TabIndex = 4
            Me.label4.Text = "Font Weight"
            '
            'label5
            '
            Me.label5.AutoSize = True
            Me.label5.Location = New System.Drawing.Point(34, 142)
            Me.label5.Name = "label5"
            Me.label5.Size = New System.Drawing.Size(65, 13)
            Me.label5.TabIndex = 4
            Me.label5.Text = "Font Stretch"
            '
            'sizeCombo
            '
            Me.sizeCombo.FormattingEnabled = True
            Me.sizeCombo.Items.AddRange(New Object() {"4", "6", "8", "10", "12", "14", "20", "24", "32", "36", "42", "60", ""})
            Me.sizeCombo.Location = New System.Drawing.Point(116, 58)
            Me.sizeCombo.Name = "sizeCombo"
            Me.sizeCombo.Size = New System.Drawing.Size(121, 21)
            Me.sizeCombo.TabIndex = 5
            '
            'styleCombo
            '
            Me.styleCombo.FormattingEnabled = True
            Me.styleCombo.Items.AddRange(New Object() {"Normal", "Oblique", "Italic"})
            Me.styleCombo.Location = New System.Drawing.Point(116, 86)
            Me.styleCombo.Name = "styleCombo"
            Me.styleCombo.Size = New System.Drawing.Size(121, 21)
            Me.styleCombo.TabIndex = 5
            '
            'weightCombo
            '
            Me.weightCombo.FormattingEnabled = True
            Me.weightCombo.Items.AddRange(New Object() {"Thin", "Extra Light", "Light", "Normal", "Medium", "Semi Bold", "Bold", "Extra Bold", "Black"})
            Me.weightCombo.Location = New System.Drawing.Point(116, 114)
            Me.weightCombo.Name = "weightCombo"
            Me.weightCombo.Size = New System.Drawing.Size(121, 21)
            Me.weightCombo.TabIndex = 5
            '
            'stretchCombo
            '
            Me.stretchCombo.FormattingEnabled = True
            Me.stretchCombo.Items.AddRange(New Object() {"None", "Ultra Condensed", "Extra Condensed", "Condensed", "Semi Condensed", "Normal", "Semi Expanded", "Expanded", "Extra Expanded", "Ultra Expanded"})
            Me.stretchCombo.Location = New System.Drawing.Point(116, 142)
            Me.stretchCombo.Name = "stretchCombo"
            Me.stretchCombo.Size = New System.Drawing.Size(121, 21)
            Me.stretchCombo.TabIndex = 5
            '
            'textBox
            '
            Me.textBox.AcceptsReturn = True
            Me.textBox.Location = New System.Drawing.Point(268, 30)
            Me.textBox.Multiline = True
            Me.textBox.Name = "textBox"
            Me.textBox.Size = New System.Drawing.Size(200, 133)
            Me.textBox.TabIndex = 6
            Me.textBox.Text = "Add Text Here"
            '
            'strikethroughCheckBox
            '
            Me.strikethroughCheckBox.AutoSize = True
            Me.strikethroughCheckBox.Location = New System.Drawing.Point(131, 183)
            Me.strikethroughCheckBox.Name = "strikethroughCheckBox"
            Me.strikethroughCheckBox.Size = New System.Drawing.Size(89, 17)
            Me.strikethroughCheckBox.TabIndex = 2
            Me.strikethroughCheckBox.Text = "Strikethrough"
            Me.strikethroughCheckBox.UseVisualStyleBackColor = True

            ' 
            ' fontFamilyCombo
            ' 
            Me.fontFamilyCombo.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable
            Me.fontFamilyCombo.DropDownHeight = 206
            Me.fontFamilyCombo.FormattingEnabled = True
            Me.fontFamilyCombo.IntegralHeight = False
            Me.fontFamilyCombo.Location = New System.Drawing.Point(116, 27)
            Me.fontFamilyCombo.Name = "fontFamilyCombo"
            Me.fontFamilyCombo.Size = New System.Drawing.Size(121, 21)
            Me.fontFamilyCombo.TabIndex = 7
            '
            'TextDialog
            '
            Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.ClientSize = New System.Drawing.Size(512, 292)
            Me.Controls.Add(fontFamilyCombo)
            Me.Controls.Add(Me.textBox)
            Me.Controls.Add(Me.stretchCombo)
            Me.Controls.Add(Me.weightCombo)
            Me.Controls.Add(Me.styleCombo)
            Me.Controls.Add(Me.sizeCombo)
            Me.Controls.Add(Me.label5)
            Me.Controls.Add(Me.label4)
            Me.Controls.Add(Me.label3)
            Me.Controls.Add(Me.label2)
            Me.Controls.Add(Me.label1)
            Me.Controls.Add(Me.strikethroughCheckBox)
            Me.Controls.Add(Me.underLineCheckBox)
            Me.Controls.Add(CancelTextButton)
            Me.Controls.Add(Me.AddTextButton)
            Me.Name = "TextDialog"
            Me.Text = "TextDialog"
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub

		#End Region

		Private WithEvents AddTextButton As System.Windows.Forms.Button
		Private underLineCheckBox As System.Windows.Forms.CheckBox
		Private label1 As System.Windows.Forms.Label
		Private label2 As System.Windows.Forms.Label
		Private label3 As System.Windows.Forms.Label
		Private label4 As System.Windows.Forms.Label
		Private label5 As System.Windows.Forms.Label
		Private sizeCombo As System.Windows.Forms.ComboBox
		Private styleCombo As System.Windows.Forms.ComboBox
		Private weightCombo As System.Windows.Forms.ComboBox
		Private stretchCombo As System.Windows.Forms.ComboBox
		Private textBox As System.Windows.Forms.TextBox
		Private strikethroughCheckBox As System.Windows.Forms.CheckBox
		Private fontFamilyCombo As FontEnumComboBox
	End Class
End Namespace