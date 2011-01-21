namespace Microsoft.WindowsAPICodePack.Samples.ShellObjectCFDBrowser
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.knownFoldersComboBox = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.librariesComboBox = new System.Windows.Forms.ComboBox();
            this.cfdKFButton = new System.Windows.Forms.Button();
            this.cfdLibraryButton = new System.Windows.Forms.Button();
            this.cfdFileButton = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.selectedFileTextBox = new System.Windows.Forms.TextBox();
            this.selectedFolderTextBox = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.cfdFolderButton = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.savedSearchButton = new System.Windows.Forms.Button();
            this.savedSearchComboBox = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.searchConnectorButton = new System.Windows.Forms.Button();
            this.searchConnectorComboBox = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.showChildItemsButton = new System.Windows.Forms.Button();
            this.detailsListView = new System.Windows.Forms.ListView();
            this.propertyColumn = new System.Windows.Forms.ColumnHeader();
            this.columnValue = new System.Windows.Forms.ColumnHeader();
            this.label8 = new System.Windows.Forms.Label();
            this.saveFileTextBox = new System.Windows.Forms.TextBox();
            this.saveFileButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // knownFoldersComboBox
            // 
            this.knownFoldersComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.knownFoldersComboBox.FormattingEnabled = true;
            this.knownFoldersComboBox.Location = new System.Drawing.Point(148, 8);
            this.knownFoldersComboBox.Name = "knownFoldersComboBox";
            this.knownFoldersComboBox.Size = new System.Drawing.Size(370, 21);
            this.knownFoldersComboBox.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(34, 11);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(80, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Known Folders:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(34, 40);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(49, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Libraries:";
            // 
            // librariesComboBox
            // 
            this.librariesComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.librariesComboBox.FormattingEnabled = true;
            this.librariesComboBox.Location = new System.Drawing.Point(148, 37);
            this.librariesComboBox.Name = "librariesComboBox";
            this.librariesComboBox.Size = new System.Drawing.Size(370, 21);
            this.librariesComboBox.TabIndex = 3;
            // 
            // cfdKFButton
            // 
            this.cfdKFButton.Location = new System.Drawing.Point(534, 6);
            this.cfdKFButton.Name = "cfdKFButton";
            this.cfdKFButton.Size = new System.Drawing.Size(160, 23);
            this.cfdKFButton.TabIndex = 4;
            this.cfdKFButton.Text = "Open Dialog (selected KF)";
            this.cfdKFButton.UseVisualStyleBackColor = true;
            this.cfdKFButton.Click += new System.EventHandler(this.cfdKFButton_Click);
            // 
            // cfdLibraryButton
            // 
            this.cfdLibraryButton.Location = new System.Drawing.Point(534, 35);
            this.cfdLibraryButton.Name = "cfdLibraryButton";
            this.cfdLibraryButton.Size = new System.Drawing.Size(160, 23);
            this.cfdLibraryButton.TabIndex = 5;
            this.cfdLibraryButton.Text = "Open Dialog (selected library)";
            this.cfdLibraryButton.UseVisualStyleBackColor = true;
            this.cfdLibraryButton.Click += new System.EventHandler(this.cfdLibraryButton_Click);
            // 
            // cfdFileButton
            // 
            this.cfdFileButton.Location = new System.Drawing.Point(534, 122);
            this.cfdFileButton.Name = "cfdFileButton";
            this.cfdFileButton.Size = new System.Drawing.Size(160, 23);
            this.cfdFileButton.TabIndex = 6;
            this.cfdFileButton.Text = "Open Dialog (select a file)";
            this.cfdFileButton.UseVisualStyleBackColor = true;
            this.cfdFileButton.Click += new System.EventHandler(this.cfdFileButton_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(34, 127);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(68, 13);
            this.label3.TabIndex = 7;
            this.label3.Text = "Selected file:";
            // 
            // selectedFileTextBox
            // 
            this.selectedFileTextBox.Location = new System.Drawing.Point(148, 124);
            this.selectedFileTextBox.Name = "selectedFileTextBox";
            this.selectedFileTextBox.ReadOnly = true;
            this.selectedFileTextBox.Size = new System.Drawing.Size(370, 20);
            this.selectedFileTextBox.TabIndex = 8;
            this.selectedFileTextBox.Text = "(none)";
            // 
            // selectedFolderTextBox
            // 
            this.selectedFolderTextBox.Location = new System.Drawing.Point(148, 153);
            this.selectedFolderTextBox.Name = "selectedFolderTextBox";
            this.selectedFolderTextBox.ReadOnly = true;
            this.selectedFolderTextBox.Size = new System.Drawing.Size(370, 20);
            this.selectedFolderTextBox.TabIndex = 11;
            this.selectedFolderTextBox.Text = "(none)";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(34, 156);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(81, 13);
            this.label4.TabIndex = 10;
            this.label4.Text = "Selected folder:";
            // 
            // cfdFolderButton
            // 
            this.cfdFolderButton.Location = new System.Drawing.Point(534, 151);
            this.cfdFolderButton.Name = "cfdFolderButton";
            this.cfdFolderButton.Size = new System.Drawing.Size(160, 23);
            this.cfdFolderButton.TabIndex = 9;
            this.cfdFolderButton.Text = "Open Dialog (select a folder)";
            this.cfdFolderButton.UseVisualStyleBackColor = true;
            this.cfdFolderButton.Click += new System.EventHandler(this.cfdFolderButton_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(13, 229);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(117, 13);
            this.label5.TabIndex = 13;
            this.label5.Text = "Details about selection:";
            // 
            // pictureBox1
            // 
            this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBox1.Location = new System.Drawing.Point(12, 249);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(128, 128);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pictureBox1.TabIndex = 14;
            this.pictureBox1.TabStop = false;
            // 
            // savedSearchButton
            // 
            this.savedSearchButton.Location = new System.Drawing.Point(534, 64);
            this.savedSearchButton.Name = "savedSearchButton";
            this.savedSearchButton.Size = new System.Drawing.Size(160, 23);
            this.savedSearchButton.TabIndex = 17;
            this.savedSearchButton.Text = "Open Dialog (selected search)";
            this.savedSearchButton.UseVisualStyleBackColor = true;
            this.savedSearchButton.Click += new System.EventHandler(this.savedSearchButton_Click);
            // 
            // savedSearchComboBox
            // 
            this.savedSearchComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.savedSearchComboBox.FormattingEnabled = true;
            this.savedSearchComboBox.Location = new System.Drawing.Point(148, 66);
            this.savedSearchComboBox.Name = "savedSearchComboBox";
            this.savedSearchComboBox.Size = new System.Drawing.Size(370, 21);
            this.savedSearchComboBox.TabIndex = 16;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(34, 69);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(89, 13);
            this.label6.TabIndex = 15;
            this.label6.Text = "Saved Searches:";
            // 
            // searchConnectorButton
            // 
            this.searchConnectorButton.Location = new System.Drawing.Point(534, 93);
            this.searchConnectorButton.Name = "searchConnectorButton";
            this.searchConnectorButton.Size = new System.Drawing.Size(160, 23);
            this.searchConnectorButton.TabIndex = 20;
            this.searchConnectorButton.Text = "Open Dialog (connector)";
            this.searchConnectorButton.UseVisualStyleBackColor = true;
            this.searchConnectorButton.Click += new System.EventHandler(this.searchConnectorButton_Click);
            // 
            // searchConnectorComboBox
            // 
            this.searchConnectorComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.searchConnectorComboBox.FormattingEnabled = true;
            this.searchConnectorComboBox.Location = new System.Drawing.Point(148, 95);
            this.searchConnectorComboBox.Name = "searchConnectorComboBox";
            this.searchConnectorComboBox.Size = new System.Drawing.Size(370, 21);
            this.searchConnectorComboBox.TabIndex = 19;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(34, 98);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(101, 13);
            this.label7.TabIndex = 18;
            this.label7.Text = "Search Connectors:";
            // 
            // showChildItemsButton
            // 
            this.showChildItemsButton.Enabled = false;
            this.showChildItemsButton.Location = new System.Drawing.Point(590, 479);
            this.showChildItemsButton.Name = "showChildItemsButton";
            this.showChildItemsButton.Size = new System.Drawing.Size(104, 23);
            this.showChildItemsButton.TabIndex = 21;
            this.showChildItemsButton.Text = "Show Children";
            this.showChildItemsButton.UseVisualStyleBackColor = true;
            this.showChildItemsButton.Click += new System.EventHandler(this.showChildItemsButton_Click);
            // 
            // detailsListView
            // 
            this.detailsListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.propertyColumn,
            this.columnValue});
            this.detailsListView.FullRowSelect = true;
            this.detailsListView.Location = new System.Drawing.Point(146, 249);
            this.detailsListView.MultiSelect = false;
            this.detailsListView.Name = "detailsListView";
            this.detailsListView.Size = new System.Drawing.Size(546, 224);
            this.detailsListView.TabIndex = 22;
            this.detailsListView.UseCompatibleStateImageBehavior = false;
            this.detailsListView.View = System.Windows.Forms.View.Details;
            // 
            // propertyColumn
            // 
            this.propertyColumn.Text = "Property";
            this.propertyColumn.Width = 167;
            // 
            // columnValue
            // 
            this.columnValue.Text = "Value";
            this.columnValue.Width = 179;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(34, 182);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(60, 13);
            this.label8.TabIndex = 23;
            this.label8.Text = "Save a file:";
            // 
            // saveFileTextBox
            // 
            this.saveFileTextBox.Location = new System.Drawing.Point(148, 182);
            this.saveFileTextBox.Multiline = true;
            this.saveFileTextBox.Name = "saveFileTextBox";
            this.saveFileTextBox.ReadOnly = true;
            this.saveFileTextBox.Size = new System.Drawing.Size(370, 35);
            this.saveFileTextBox.TabIndex = 24;
            this.saveFileTextBox.Text = "Click the Save button to save a file, as well as edit properties for it via the C" +
                "ommonFileSaveDialog.";
            // 
            // saveFileButton
            // 
            this.saveFileButton.Location = new System.Drawing.Point(534, 180);
            this.saveFileButton.Name = "saveFileButton";
            this.saveFileButton.Size = new System.Drawing.Size(160, 23);
            this.saveFileButton.TabIndex = 25;
            this.saveFileButton.Text = "Save file via Save Dialog";
            this.saveFileButton.UseVisualStyleBackColor = true;
            this.saveFileButton.Click += new System.EventHandler(this.saveFileButton_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(706, 514);
            this.Controls.Add(this.saveFileButton);
            this.Controls.Add(this.saveFileTextBox);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.detailsListView);
            this.Controls.Add(this.showChildItemsButton);
            this.Controls.Add(this.searchConnectorButton);
            this.Controls.Add(this.searchConnectorComboBox);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.savedSearchButton);
            this.Controls.Add(this.savedSearchComboBox);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.selectedFolderTextBox);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.cfdFolderButton);
            this.Controls.Add(this.selectedFileTextBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.cfdFileButton);
            this.Controls.Add(this.cfdLibraryButton);
            this.Controls.Add(this.cfdKFButton);
            this.Controls.Add(this.librariesComboBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.knownFoldersComboBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "CommonFileDialog Demo";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox knownFoldersComboBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox librariesComboBox;
        private System.Windows.Forms.Button cfdKFButton;
        private System.Windows.Forms.Button cfdLibraryButton;
        private System.Windows.Forms.Button cfdFileButton;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox selectedFileTextBox;
        private System.Windows.Forms.TextBox selectedFolderTextBox;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button cfdFolderButton;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button savedSearchButton;
        private System.Windows.Forms.ComboBox savedSearchComboBox;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button searchConnectorButton;
        private System.Windows.Forms.ComboBox searchConnectorComboBox;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Button showChildItemsButton;
        private System.Windows.Forms.ListView detailsListView;
        private System.Windows.Forms.ColumnHeader propertyColumn;
        private System.Windows.Forms.ColumnHeader columnValue;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox saveFileTextBox;
        private System.Windows.Forms.Button saveFileButton;
    }
}

