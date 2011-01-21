Imports Microsoft.VisualBasic
Imports System
Namespace Microsoft.WindowsAPICodePack.Samples.ShellObjectCFDBrowser
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
			Me.knownFoldersComboBox = New System.Windows.Forms.ComboBox()
			Me.label1 = New System.Windows.Forms.Label()
			Me.label2 = New System.Windows.Forms.Label()
			Me.librariesComboBox = New System.Windows.Forms.ComboBox()
			Me.cfdKFButton = New System.Windows.Forms.Button()
			Me.cfdLibraryButton = New System.Windows.Forms.Button()
			Me.cfdFileButton = New System.Windows.Forms.Button()
			Me.label3 = New System.Windows.Forms.Label()
			Me.selectedFileTextBox = New System.Windows.Forms.TextBox()
			Me.selectedFolderTextBox = New System.Windows.Forms.TextBox()
			Me.label4 = New System.Windows.Forms.Label()
			Me.cfdFolderButton = New System.Windows.Forms.Button()
			Me.label5 = New System.Windows.Forms.Label()
			Me.pictureBox1 = New System.Windows.Forms.PictureBox()
			Me.savedSearchButton = New System.Windows.Forms.Button()
			Me.savedSearchComboBox = New System.Windows.Forms.ComboBox()
			Me.label6 = New System.Windows.Forms.Label()
			Me.searchConnectorButton = New System.Windows.Forms.Button()
			Me.searchConnectorComboBox = New System.Windows.Forms.ComboBox()
			Me.label7 = New System.Windows.Forms.Label()
			Me.showChildItemsButton = New System.Windows.Forms.Button()
			Me.detailsListView = New System.Windows.Forms.ListView()
			Me.propertyColumn = New System.Windows.Forms.ColumnHeader()
			Me.columnValue = New System.Windows.Forms.ColumnHeader()
			Me.label8 = New System.Windows.Forms.Label()
			Me.saveFileTextBox = New System.Windows.Forms.TextBox()
			Me.saveFileButton = New System.Windows.Forms.Button()
			CType(Me.pictureBox1, System.ComponentModel.ISupportInitialize).BeginInit()
			Me.SuspendLayout()
			' 
			' knownFoldersComboBox
			' 
			Me.knownFoldersComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
			Me.knownFoldersComboBox.FormattingEnabled = True
			Me.knownFoldersComboBox.Location = New System.Drawing.Point(148, 8)
			Me.knownFoldersComboBox.Name = "knownFoldersComboBox"
			Me.knownFoldersComboBox.Size = New System.Drawing.Size(370, 21)
			Me.knownFoldersComboBox.TabIndex = 0
			' 
			' label1
			' 
			Me.label1.AutoSize = True
			Me.label1.Location = New System.Drawing.Point(34, 11)
			Me.label1.Name = "label1"
			Me.label1.Size = New System.Drawing.Size(80, 13)
			Me.label1.TabIndex = 1
			Me.label1.Text = "Known Folders:"
			' 
			' label2
			' 
			Me.label2.AutoSize = True
			Me.label2.Location = New System.Drawing.Point(34, 40)
			Me.label2.Name = "label2"
			Me.label2.Size = New System.Drawing.Size(49, 13)
			Me.label2.TabIndex = 2
			Me.label2.Text = "Libraries:"
			' 
			' librariesComboBox
			' 
			Me.librariesComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
			Me.librariesComboBox.FormattingEnabled = True
			Me.librariesComboBox.Location = New System.Drawing.Point(148, 37)
			Me.librariesComboBox.Name = "librariesComboBox"
			Me.librariesComboBox.Size = New System.Drawing.Size(370, 21)
			Me.librariesComboBox.TabIndex = 3
			' 
			' cfdKFButton
			' 
			Me.cfdKFButton.Location = New System.Drawing.Point(534, 6)
			Me.cfdKFButton.Name = "cfdKFButton"
			Me.cfdKFButton.Size = New System.Drawing.Size(160, 23)
			Me.cfdKFButton.TabIndex = 4
			Me.cfdKFButton.Text = "Open Dialog (selected KF)"
			Me.cfdKFButton.UseVisualStyleBackColor = True
'			Me.cfdKFButton.Click += New System.EventHandler(Me.cfdKFButton_Click)
			' 
			' cfdLibraryButton
			' 
			Me.cfdLibraryButton.Location = New System.Drawing.Point(534, 35)
			Me.cfdLibraryButton.Name = "cfdLibraryButton"
			Me.cfdLibraryButton.Size = New System.Drawing.Size(160, 23)
			Me.cfdLibraryButton.TabIndex = 5
			Me.cfdLibraryButton.Text = "Open Dialog (selected library)"
			Me.cfdLibraryButton.UseVisualStyleBackColor = True
'			Me.cfdLibraryButton.Click += New System.EventHandler(Me.cfdLibraryButton_Click)
			' 
			' cfdFileButton
			' 
			Me.cfdFileButton.Location = New System.Drawing.Point(534, 122)
			Me.cfdFileButton.Name = "cfdFileButton"
			Me.cfdFileButton.Size = New System.Drawing.Size(160, 23)
			Me.cfdFileButton.TabIndex = 6
			Me.cfdFileButton.Text = "Open Dialog (select a file)"
			Me.cfdFileButton.UseVisualStyleBackColor = True
'			Me.cfdFileButton.Click += New System.EventHandler(Me.cfdFileButton_Click)
			' 
			' label3
			' 
			Me.label3.AutoSize = True
			Me.label3.Location = New System.Drawing.Point(34, 127)
			Me.label3.Name = "label3"
			Me.label3.Size = New System.Drawing.Size(68, 13)
			Me.label3.TabIndex = 7
			Me.label3.Text = "Selected file:"
			' 
			' selectedFileTextBox
			' 
			Me.selectedFileTextBox.Location = New System.Drawing.Point(148, 124)
			Me.selectedFileTextBox.Name = "selectedFileTextBox"
			Me.selectedFileTextBox.ReadOnly = True
			Me.selectedFileTextBox.Size = New System.Drawing.Size(370, 20)
			Me.selectedFileTextBox.TabIndex = 8
			Me.selectedFileTextBox.Text = "(none)"
			' 
			' selectedFolderTextBox
			' 
			Me.selectedFolderTextBox.Location = New System.Drawing.Point(148, 153)
			Me.selectedFolderTextBox.Name = "selectedFolderTextBox"
			Me.selectedFolderTextBox.ReadOnly = True
			Me.selectedFolderTextBox.Size = New System.Drawing.Size(370, 20)
			Me.selectedFolderTextBox.TabIndex = 11
			Me.selectedFolderTextBox.Text = "(none)"
			' 
			' label4
			' 
			Me.label4.AutoSize = True
			Me.label4.Location = New System.Drawing.Point(34, 156)
			Me.label4.Name = "label4"
			Me.label4.Size = New System.Drawing.Size(81, 13)
			Me.label4.TabIndex = 10
			Me.label4.Text = "Selected folder:"
			' 
			' cfdFolderButton
			' 
			Me.cfdFolderButton.Location = New System.Drawing.Point(534, 151)
			Me.cfdFolderButton.Name = "cfdFolderButton"
			Me.cfdFolderButton.Size = New System.Drawing.Size(160, 23)
			Me.cfdFolderButton.TabIndex = 9
			Me.cfdFolderButton.Text = "Open Dialog (select a folder)"
			Me.cfdFolderButton.UseVisualStyleBackColor = True
'			Me.cfdFolderButton.Click += New System.EventHandler(Me.cfdFolderButton_Click)
			' 
			' label5
			' 
			Me.label5.AutoSize = True
			Me.label5.Location = New System.Drawing.Point(13, 229)
			Me.label5.Name = "label5"
			Me.label5.Size = New System.Drawing.Size(117, 13)
			Me.label5.TabIndex = 13
			Me.label5.Text = "Details about selection:"
			' 
			' pictureBox1
			' 
			Me.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
			Me.pictureBox1.Location = New System.Drawing.Point(12, 249)
			Me.pictureBox1.Name = "pictureBox1"
			Me.pictureBox1.Size = New System.Drawing.Size(128, 128)
			Me.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage
			Me.pictureBox1.TabIndex = 14
			Me.pictureBox1.TabStop = False
			' 
			' savedSearchButton
			' 
			Me.savedSearchButton.Location = New System.Drawing.Point(534, 64)
			Me.savedSearchButton.Name = "savedSearchButton"
			Me.savedSearchButton.Size = New System.Drawing.Size(160, 23)
			Me.savedSearchButton.TabIndex = 17
			Me.savedSearchButton.Text = "Open Dialog (selected search)"
			Me.savedSearchButton.UseVisualStyleBackColor = True
'			Me.savedSearchButton.Click += New System.EventHandler(Me.savedSearchButton_Click)
			' 
			' savedSearchComboBox
			' 
			Me.savedSearchComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
			Me.savedSearchComboBox.FormattingEnabled = True
			Me.savedSearchComboBox.Location = New System.Drawing.Point(148, 66)
			Me.savedSearchComboBox.Name = "savedSearchComboBox"
			Me.savedSearchComboBox.Size = New System.Drawing.Size(370, 21)
			Me.savedSearchComboBox.TabIndex = 16
			' 
			' label6
			' 
			Me.label6.AutoSize = True
			Me.label6.Location = New System.Drawing.Point(34, 69)
			Me.label6.Name = "label6"
			Me.label6.Size = New System.Drawing.Size(89, 13)
			Me.label6.TabIndex = 15
			Me.label6.Text = "Saved Searches:"
			' 
			' searchConnectorButton
			' 
			Me.searchConnectorButton.Location = New System.Drawing.Point(534, 93)
			Me.searchConnectorButton.Name = "searchConnectorButton"
			Me.searchConnectorButton.Size = New System.Drawing.Size(160, 23)
			Me.searchConnectorButton.TabIndex = 20
			Me.searchConnectorButton.Text = "Open Dialog (connector)"
			Me.searchConnectorButton.UseVisualStyleBackColor = True
'			Me.searchConnectorButton.Click += New System.EventHandler(Me.searchConnectorButton_Click)
			' 
			' searchConnectorComboBox
			' 
			Me.searchConnectorComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
			Me.searchConnectorComboBox.FormattingEnabled = True
			Me.searchConnectorComboBox.Location = New System.Drawing.Point(148, 95)
			Me.searchConnectorComboBox.Name = "searchConnectorComboBox"
			Me.searchConnectorComboBox.Size = New System.Drawing.Size(370, 21)
			Me.searchConnectorComboBox.TabIndex = 19
			' 
			' label7
			' 
			Me.label7.AutoSize = True
			Me.label7.Location = New System.Drawing.Point(34, 98)
			Me.label7.Name = "label7"
			Me.label7.Size = New System.Drawing.Size(101, 13)
			Me.label7.TabIndex = 18
			Me.label7.Text = "Search Connectors:"
			' 
			' showChildItemsButton
			' 
			Me.showChildItemsButton.Enabled = False
			Me.showChildItemsButton.Location = New System.Drawing.Point(590, 479)
			Me.showChildItemsButton.Name = "showChildItemsButton"
			Me.showChildItemsButton.Size = New System.Drawing.Size(104, 23)
			Me.showChildItemsButton.TabIndex = 21
			Me.showChildItemsButton.Text = "Show Children"
			Me.showChildItemsButton.UseVisualStyleBackColor = True
'			Me.showChildItemsButton.Click += New System.EventHandler(Me.showChildItemsButton_Click)
			' 
			' detailsListView
			' 
			Me.detailsListView.Columns.AddRange(New System.Windows.Forms.ColumnHeader() { Me.propertyColumn, Me.columnValue})
			Me.detailsListView.FullRowSelect = True
			Me.detailsListView.Location = New System.Drawing.Point(146, 249)
			Me.detailsListView.MultiSelect = False
			Me.detailsListView.Name = "detailsListView"
			Me.detailsListView.Size = New System.Drawing.Size(546, 224)
			Me.detailsListView.TabIndex = 22
			Me.detailsListView.UseCompatibleStateImageBehavior = False
			Me.detailsListView.View = System.Windows.Forms.View.Details
			' 
			' propertyColumn
			' 
			Me.propertyColumn.Text = "Property"
			Me.propertyColumn.Width = 167
			' 
			' columnValue
			' 
			Me.columnValue.Text = "Value"
			Me.columnValue.Width = 179
			' 
			' label8
			' 
			Me.label8.AutoSize = True
			Me.label8.Location = New System.Drawing.Point(34, 182)
			Me.label8.Name = "label8"
			Me.label8.Size = New System.Drawing.Size(60, 13)
			Me.label8.TabIndex = 23
			Me.label8.Text = "Save a file:"
			' 
			' saveFileTextBox
			' 
			Me.saveFileTextBox.Location = New System.Drawing.Point(148, 182)
			Me.saveFileTextBox.Multiline = True
			Me.saveFileTextBox.Name = "saveFileTextBox"
			Me.saveFileTextBox.ReadOnly = True
			Me.saveFileTextBox.Size = New System.Drawing.Size(370, 35)
			Me.saveFileTextBox.TabIndex = 24
			Me.saveFileTextBox.Text = "Click the Save button to save a file, as well as edit properties for it via the C" & "ommonFileSaveDialog."
			' 
			' saveFileButton
			' 
			Me.saveFileButton.Location = New System.Drawing.Point(534, 180)
			Me.saveFileButton.Name = "saveFileButton"
			Me.saveFileButton.Size = New System.Drawing.Size(160, 23)
			Me.saveFileButton.TabIndex = 25
			Me.saveFileButton.Text = "Save file via Save Dialog"
			Me.saveFileButton.UseVisualStyleBackColor = True
'			Me.saveFileButton.Click += New System.EventHandler(Me.saveFileButton_Click)
			' 
			' Form1
			' 
			Me.AutoScaleDimensions = New System.Drawing.SizeF(6F, 13F)
			Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
			Me.ClientSize = New System.Drawing.Size(706, 514)
			Me.Controls.Add(Me.saveFileButton)
			Me.Controls.Add(Me.saveFileTextBox)
			Me.Controls.Add(Me.label8)
			Me.Controls.Add(Me.detailsListView)
			Me.Controls.Add(Me.showChildItemsButton)
			Me.Controls.Add(Me.searchConnectorButton)
			Me.Controls.Add(Me.searchConnectorComboBox)
			Me.Controls.Add(Me.label7)
			Me.Controls.Add(Me.savedSearchButton)
			Me.Controls.Add(Me.savedSearchComboBox)
			Me.Controls.Add(Me.label6)
			Me.Controls.Add(Me.pictureBox1)
			Me.Controls.Add(Me.label5)
			Me.Controls.Add(Me.selectedFolderTextBox)
			Me.Controls.Add(Me.label4)
			Me.Controls.Add(Me.cfdFolderButton)
			Me.Controls.Add(Me.selectedFileTextBox)
			Me.Controls.Add(Me.label3)
			Me.Controls.Add(Me.cfdFileButton)
			Me.Controls.Add(Me.cfdLibraryButton)
			Me.Controls.Add(Me.cfdKFButton)
			Me.Controls.Add(Me.librariesComboBox)
			Me.Controls.Add(Me.label2)
			Me.Controls.Add(Me.label1)
			Me.Controls.Add(Me.knownFoldersComboBox)
			Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
			Me.MaximizeBox = False
			Me.Name = "Form1"
			Me.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide
			Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
			Me.Text = "CommonFileDialog Demo"
			CType(Me.pictureBox1, System.ComponentModel.ISupportInitialize).EndInit()
			Me.ResumeLayout(False)
			Me.PerformLayout()

		End Sub

		#End Region

		Private knownFoldersComboBox As System.Windows.Forms.ComboBox
		Private label1 As System.Windows.Forms.Label
		Private label2 As System.Windows.Forms.Label
		Private librariesComboBox As System.Windows.Forms.ComboBox
		Private WithEvents cfdKFButton As System.Windows.Forms.Button
		Private WithEvents cfdLibraryButton As System.Windows.Forms.Button
		Private WithEvents cfdFileButton As System.Windows.Forms.Button
		Private label3 As System.Windows.Forms.Label
		Private selectedFileTextBox As System.Windows.Forms.TextBox
		Private selectedFolderTextBox As System.Windows.Forms.TextBox
		Private label4 As System.Windows.Forms.Label
		Private WithEvents cfdFolderButton As System.Windows.Forms.Button
		Private label5 As System.Windows.Forms.Label
		Private pictureBox1 As System.Windows.Forms.PictureBox
		Private WithEvents savedSearchButton As System.Windows.Forms.Button
		Private savedSearchComboBox As System.Windows.Forms.ComboBox
		Private label6 As System.Windows.Forms.Label
		Private WithEvents searchConnectorButton As System.Windows.Forms.Button
		Private searchConnectorComboBox As System.Windows.Forms.ComboBox
		Private label7 As System.Windows.Forms.Label
		Private WithEvents showChildItemsButton As System.Windows.Forms.Button
		Private detailsListView As System.Windows.Forms.ListView
		Private propertyColumn As System.Windows.Forms.ColumnHeader
		Private columnValue As System.Windows.Forms.ColumnHeader
		Private label8 As System.Windows.Forms.Label
		Private saveFileTextBox As System.Windows.Forms.TextBox
		Private WithEvents saveFileButton As System.Windows.Forms.Button
	End Class
End Namespace

