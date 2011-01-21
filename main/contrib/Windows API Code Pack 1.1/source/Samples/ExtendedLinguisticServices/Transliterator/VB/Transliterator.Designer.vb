Imports Microsoft.VisualBasic
Imports System
Namespace Transliterator
    Partial Friend Class Transliterator
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
            Me.tableLayoutPanel1 = New System.Windows.Forms.TableLayoutPanel()
            Me.tableLayoutPanel2 = New System.Windows.Forms.TableLayoutPanel()
            Me.textBoxResult = New ScrollbarTextBox()
            Me.textBoxSource = New ScrollbarTextBox()
            Me.label1 = New System.Windows.Forms.Label()
            Me.label2 = New System.Windows.Forms.Label()
            Me.tableLayoutPanel3 = New System.Windows.Forms.TableLayoutPanel()
            Me.tableLayoutPanel4 = New System.Windows.Forms.TableLayoutPanel()
            Me.btnBrowse = New System.Windows.Forms.Button()
            Me.btnConvert = New System.Windows.Forms.Button()
            Me.btnHelp = New System.Windows.Forms.Button()
            Me.btnClose = New System.Windows.Forms.Button()
            Me.tableLayoutPanel5 = New System.Windows.Forms.TableLayoutPanel()
            Me.groupBox1 = New System.Windows.Forms.GroupBox()
            Me.comboBoxServices = New System.Windows.Forms.ComboBox()
            Me.tableLayoutPanel6 = New System.Windows.Forms.TableLayoutPanel()
            Me.label3 = New System.Windows.Forms.Label()
            Me.textBoxSourceFile = New System.Windows.Forms.TextBox()
            Me.tableLayoutPanel1.SuspendLayout()
            Me.tableLayoutPanel2.SuspendLayout()
            Me.tableLayoutPanel3.SuspendLayout()
            Me.tableLayoutPanel4.SuspendLayout()
            Me.tableLayoutPanel5.SuspendLayout()
            Me.groupBox1.SuspendLayout()
            Me.tableLayoutPanel6.SuspendLayout()
            Me.SuspendLayout()
            ' 
            ' tableLayoutPanel1
            ' 
            Me.tableLayoutPanel1.Anchor = (CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) Or System.Windows.Forms.AnchorStyles.Left) Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles))
            Me.tableLayoutPanel1.ColumnCount = 1
            Me.tableLayoutPanel1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0F))
            Me.tableLayoutPanel1.Controls.Add(Me.tableLayoutPanel2, 0, 1)
            Me.tableLayoutPanel1.Controls.Add(Me.tableLayoutPanel3, 0, 0)
            Me.tableLayoutPanel1.Location = New System.Drawing.Point(12, 12)
            Me.tableLayoutPanel1.Name = "tableLayoutPanel1"
            Me.tableLayoutPanel1.RowCount = 2
            Me.tableLayoutPanel1.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 160.0F))
            Me.tableLayoutPanel1.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100.0F))
            Me.tableLayoutPanel1.Size = New System.Drawing.Size(679, 471)
            Me.tableLayoutPanel1.TabIndex = 0
            ' 
            ' tableLayoutPanel2
            ' 
            Me.tableLayoutPanel2.Anchor = (CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) Or System.Windows.Forms.AnchorStyles.Left) Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles))
            Me.tableLayoutPanel2.ColumnCount = 3
            Me.tableLayoutPanel2.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.0F))
            Me.tableLayoutPanel2.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20.0F))
            Me.tableLayoutPanel2.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.0F))
            Me.tableLayoutPanel2.Controls.Add(Me.textBoxResult, 2, 1)
            Me.tableLayoutPanel2.Controls.Add(Me.textBoxSource, 0, 1)
            Me.tableLayoutPanel2.Controls.Add(Me.label1, 0, 0)
            Me.tableLayoutPanel2.Controls.Add(Me.label2, 2, 0)
            Me.tableLayoutPanel2.Location = New System.Drawing.Point(3, 163)
            Me.tableLayoutPanel2.Name = "tableLayoutPanel2"
            Me.tableLayoutPanel2.RowCount = 2
            Me.tableLayoutPanel2.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20.0F))
            Me.tableLayoutPanel2.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100.0F))
            Me.tableLayoutPanel2.Size = New System.Drawing.Size(673, 305)
            Me.tableLayoutPanel2.TabIndex = 0
            ' 
            ' textBoxResult
            ' 
            Me.textBoxResult.Anchor = (CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) Or System.Windows.Forms.AnchorStyles.Left) Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles))
            Me.textBoxResult.HideSelection = False
            Me.textBoxResult.Location = New System.Drawing.Point(349, 23)
            Me.textBoxResult.Multiline = True
            Me.textBoxResult.Name = "textBoxResult"
            Me.textBoxResult.ScrollBars = System.Windows.Forms.ScrollBars.Both
            Me.textBoxResult.Size = New System.Drawing.Size(321, 279)
            Me.textBoxResult.TabIndex = 7
            '			Me.textBoxResult.OnVerticalScroll += New System.Windows.Forms.ScrollEventHandler(Me.textBoxResult_OnVerticalScroll)
            '			Me.textBoxResult.MouseMove += New System.Windows.Forms.MouseEventHandler(Me.textBoxResult_MouseMove)
            '			Me.textBoxResult.MouseDown += New System.Windows.Forms.MouseEventHandler(Me.textBoxResult_MouseDown)
            ' 
            ' textBoxSource
            ' 
            Me.textBoxSource.Anchor = (CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) Or System.Windows.Forms.AnchorStyles.Left) Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles))
            Me.textBoxSource.HideSelection = False
            Me.textBoxSource.Location = New System.Drawing.Point(3, 23)
            Me.textBoxSource.Multiline = True
            Me.textBoxSource.Name = "textBoxSource"
            Me.textBoxSource.ScrollBars = System.Windows.Forms.ScrollBars.Both
            Me.textBoxSource.Size = New System.Drawing.Size(320, 279)
            Me.textBoxSource.TabIndex = 5
            '			Me.textBoxSource.OnVerticalScroll += New System.Windows.Forms.ScrollEventHandler(Me.textBoxSource_OnVerticalScroll)
            '			Me.textBoxSource.MouseMove += New System.Windows.Forms.MouseEventHandler(Me.textBoxSource_MouseMove)
            '			Me.textBoxSource.MouseDown += New System.Windows.Forms.MouseEventHandler(Me.textBoxSource_MouseDown)
            '			Me.textBoxSource.TextChanged += New System.EventHandler(Me.textBoxSource_TextChanged)
            ' 
            ' label1
            ' 
            Me.label1.AutoSize = True
            Me.label1.Location = New System.Drawing.Point(3, 0)
            Me.label1.Name = "label1"
            Me.label1.Size = New System.Drawing.Size(101, 13)
            Me.label1.TabIndex = 4
            Me.label1.Text = "Text for conversion:"
            ' 
            ' label2
            ' 
            Me.label2.AutoSize = True
            Me.label2.Location = New System.Drawing.Point(349, 0)
            Me.label2.Name = "label2"
            Me.label2.Size = New System.Drawing.Size(91, 13)
            Me.label2.TabIndex = 6
            Me.label2.Text = "Conversion result:"
            ' 
            ' tableLayoutPanel3
            ' 
            Me.tableLayoutPanel3.Anchor = (CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles))
            Me.tableLayoutPanel3.ColumnCount = 2
            Me.tableLayoutPanel3.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0F))
            Me.tableLayoutPanel3.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100.0F))
            Me.tableLayoutPanel3.Controls.Add(Me.tableLayoutPanel4, 1, 0)
            Me.tableLayoutPanel3.Controls.Add(Me.tableLayoutPanel5, 0, 0)
            Me.tableLayoutPanel3.Location = New System.Drawing.Point(3, 3)
            Me.tableLayoutPanel3.Name = "tableLayoutPanel3"
            Me.tableLayoutPanel3.RowCount = 1
            Me.tableLayoutPanel3.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100.0F))
            Me.tableLayoutPanel3.Size = New System.Drawing.Size(673, 145)
            Me.tableLayoutPanel3.TabIndex = 1
            ' 
            ' tableLayoutPanel4
            ' 
            Me.tableLayoutPanel4.Anchor = (CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) Or System.Windows.Forms.AnchorStyles.Left) Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles))
            Me.tableLayoutPanel4.ColumnCount = 1
            Me.tableLayoutPanel4.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0F))
            Me.tableLayoutPanel4.Controls.Add(Me.btnBrowse, 0, 0)
            Me.tableLayoutPanel4.Controls.Add(Me.btnConvert, 0, 1)
            Me.tableLayoutPanel4.Controls.Add(Me.btnHelp, 0, 2)
            Me.tableLayoutPanel4.Controls.Add(Me.btnClose, 0, 3)
            Me.tableLayoutPanel4.Location = New System.Drawing.Point(576, 3)
            Me.tableLayoutPanel4.Name = "tableLayoutPanel4"
            Me.tableLayoutPanel4.RowCount = 4
            Me.tableLayoutPanel4.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25.0F))
            Me.tableLayoutPanel4.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25.0F))
            Me.tableLayoutPanel4.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25.0F))
            Me.tableLayoutPanel4.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25.0F))
            Me.tableLayoutPanel4.Size = New System.Drawing.Size(94, 139)
            Me.tableLayoutPanel4.TabIndex = 0
            ' 
            ' btnBrowse
            ' 
            Me.btnBrowse.Anchor = (CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles))
            Me.btnBrowse.Location = New System.Drawing.Point(16, 3)
            Me.btnBrowse.Name = "btnBrowse"
            Me.btnBrowse.Size = New System.Drawing.Size(75, 23)
            Me.btnBrowse.TabIndex = 8
            Me.btnBrowse.Text = "Browse..."
            Me.btnBrowse.UseVisualStyleBackColor = True
            '			Me.btnBrowse.Click += New System.EventHandler(Me.btnBrowse_Click)
            ' 
            ' btnConvert
            ' 
            Me.btnConvert.Anchor = System.Windows.Forms.AnchorStyles.Right
            Me.btnConvert.Location = New System.Drawing.Point(16, 39)
            Me.btnConvert.Name = "btnConvert"
            Me.btnConvert.Size = New System.Drawing.Size(75, 23)
            Me.btnConvert.TabIndex = 9
            Me.btnConvert.Text = "Convert"
            Me.btnConvert.UseVisualStyleBackColor = True
            '			Me.btnConvert.Click += New System.EventHandler(Me.btnConvert_Click)
            ' 
            ' btnHelp
            ' 
            Me.btnHelp.Anchor = System.Windows.Forms.AnchorStyles.Right
            Me.btnHelp.Location = New System.Drawing.Point(16, 73)
            Me.btnHelp.Name = "btnHelp"
            Me.btnHelp.Size = New System.Drawing.Size(75, 23)
            Me.btnHelp.TabIndex = 10
            Me.btnHelp.Text = "Help"
            Me.btnHelp.UseVisualStyleBackColor = True
            '			Me.btnHelp.Click += New System.EventHandler(Me.btnHelp_Click)
            ' 
            ' btnClose
            ' 
            Me.btnClose.Anchor = System.Windows.Forms.AnchorStyles.Right
            Me.btnClose.Location = New System.Drawing.Point(16, 109)
            Me.btnClose.Name = "btnClose"
            Me.btnClose.Size = New System.Drawing.Size(75, 23)
            Me.btnClose.TabIndex = 11
            Me.btnClose.Text = "Close"
            Me.btnClose.UseVisualStyleBackColor = True
            '			Me.btnClose.Click += New System.EventHandler(Me.btnClose_Click)
            ' 
            ' tableLayoutPanel5
            ' 
            Me.tableLayoutPanel5.Anchor = (CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) Or System.Windows.Forms.AnchorStyles.Left) Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles))
            Me.tableLayoutPanel5.ColumnCount = 1
            Me.tableLayoutPanel5.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.0F))
            Me.tableLayoutPanel5.Controls.Add(Me.groupBox1, 0, 1)
            Me.tableLayoutPanel5.Controls.Add(Me.tableLayoutPanel6, 0, 0)
            Me.tableLayoutPanel5.Location = New System.Drawing.Point(3, 3)
            Me.tableLayoutPanel5.Name = "tableLayoutPanel5"
            Me.tableLayoutPanel5.RowCount = 2
            Me.tableLayoutPanel5.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 40.28777F))
            Me.tableLayoutPanel5.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 59.71223F))
            Me.tableLayoutPanel5.Size = New System.Drawing.Size(567, 139)
            Me.tableLayoutPanel5.TabIndex = 1
            ' 
            ' groupBox1
            ' 
            Me.groupBox1.Anchor = (CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) Or System.Windows.Forms.AnchorStyles.Left) Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles))
            Me.groupBox1.Controls.Add(Me.comboBoxServices)
            Me.groupBox1.Location = New System.Drawing.Point(3, 58)
            Me.groupBox1.Name = "groupBox1"
            Me.groupBox1.Size = New System.Drawing.Size(561, 78)
            Me.groupBox1.TabIndex = 2
            Me.groupBox1.TabStop = False
            Me.groupBox1.Text = "Tranliteration service"
            ' 
            ' comboBoxServices
            ' 
            Me.comboBoxServices.Anchor = (CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles))
            Me.comboBoxServices.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.comboBoxServices.FormattingEnabled = True
            Me.comboBoxServices.Location = New System.Drawing.Point(39, 31)
            Me.comboBoxServices.Name = "comboBoxServices"
            Me.comboBoxServices.Size = New System.Drawing.Size(488, 21)
            Me.comboBoxServices.TabIndex = 3
            '			Me.comboBoxServices.SelectedIndexChanged += New System.EventHandler(Me.comboBoxServices_SelectedIndexChanged)
            ' 
            ' tableLayoutPanel6
            ' 
            Me.tableLayoutPanel6.Anchor = (CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) Or System.Windows.Forms.AnchorStyles.Left) Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles))
            Me.tableLayoutPanel6.ColumnCount = 2
            Me.tableLayoutPanel6.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80.0F))
            Me.tableLayoutPanel6.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0F))
            Me.tableLayoutPanel6.Controls.Add(Me.label3, 0, 0)
            Me.tableLayoutPanel6.Controls.Add(Me.textBoxSourceFile, 1, 0)
            Me.tableLayoutPanel6.Location = New System.Drawing.Point(3, 3)
            Me.tableLayoutPanel6.Name = "tableLayoutPanel6"
            Me.tableLayoutPanel6.RowCount = 1
            Me.tableLayoutPanel6.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100.0F))
            Me.tableLayoutPanel6.Size = New System.Drawing.Size(561, 49)
            Me.tableLayoutPanel6.TabIndex = 1
            ' 
            ' label3
            ' 
            Me.label3.AutoSize = True
            Me.label3.Location = New System.Drawing.Point(3, 0)
            Me.label3.Name = "label3"
            Me.label3.Padding = New System.Windows.Forms.Padding(0, 6, 0, 0)
            Me.label3.Size = New System.Drawing.Size(60, 19)
            Me.label3.TabIndex = 0
            Me.label3.Text = "Source file:"
            ' 
            ' textBoxSourceFile
            ' 
            Me.textBoxSourceFile.Anchor = (CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles))
            Me.textBoxSourceFile.Location = New System.Drawing.Point(83, 3)
            Me.textBoxSourceFile.Name = "textBoxSourceFile"
            Me.textBoxSourceFile.Size = New System.Drawing.Size(475, 20)
            Me.textBoxSourceFile.TabIndex = 1
            '			Me.textBoxSourceFile.KeyDown += New System.Windows.Forms.KeyEventHandler(Me.textBoxSourceFile_KeyDown)
            ' 
            ' Transliterator
            ' 
            Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0F, 13.0F)
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.ClientSize = New System.Drawing.Size(703, 495)
            Me.Controls.Add(Me.tableLayoutPanel1)
            Me.Name = "Transliterator"
            Me.Text = "Transliterator"
            Me.tableLayoutPanel1.ResumeLayout(False)
            Me.tableLayoutPanel2.ResumeLayout(False)
            Me.tableLayoutPanel2.PerformLayout()
            Me.tableLayoutPanel3.ResumeLayout(False)
            Me.tableLayoutPanel4.ResumeLayout(False)
            Me.tableLayoutPanel5.ResumeLayout(False)
            Me.groupBox1.ResumeLayout(False)
            Me.tableLayoutPanel6.ResumeLayout(False)
            Me.tableLayoutPanel6.PerformLayout()
            Me.ResumeLayout(False)

        End Sub

#End Region

        Private tableLayoutPanel1 As System.Windows.Forms.TableLayoutPanel
        Private tableLayoutPanel2 As System.Windows.Forms.TableLayoutPanel
        Private WithEvents textBoxResult As ScrollbarTextBox
        Private WithEvents textBoxSource As ScrollbarTextBox
        Private label1 As System.Windows.Forms.Label
        Private label2 As System.Windows.Forms.Label
        Private tableLayoutPanel3 As System.Windows.Forms.TableLayoutPanel
        Private tableLayoutPanel4 As System.Windows.Forms.TableLayoutPanel
        Private WithEvents btnBrowse As System.Windows.Forms.Button
        Private WithEvents btnConvert As System.Windows.Forms.Button
        Private WithEvents btnHelp As System.Windows.Forms.Button
        Private WithEvents btnClose As System.Windows.Forms.Button
        Private tableLayoutPanel5 As System.Windows.Forms.TableLayoutPanel
        Private groupBox1 As System.Windows.Forms.GroupBox
        Private WithEvents comboBoxServices As System.Windows.Forms.ComboBox
        Private tableLayoutPanel6 As System.Windows.Forms.TableLayoutPanel
        Private label3 As System.Windows.Forms.Label
        Private WithEvents textBoxSourceFile As System.Windows.Forms.TextBox

    End Class
End Namespace

