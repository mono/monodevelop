//Copyright (c) Microsoft Corporation.  All rights reserved.


namespace Microsoft.WindowsAPICodePack.Samples
{
    partial class ExplorerBrowserTestForm
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
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.itemsTextBox = new System.Windows.Forms.RichTextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.filePathNavigate = new System.Windows.Forms.Button();
            this.filePathEdit = new System.Windows.Forms.TextBox();
            this.knownFolderNavigate = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.knownFolderCombo = new System.Windows.Forms.ComboBox();
            this.navigateButton = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.pathEdit = new System.Windows.Forms.TextBox();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.visibilityPropertyGrid = new System.Windows.Forms.PropertyGrid();
            this.contentPropertyGrid = new System.Windows.Forms.PropertyGrid();
            this.navigationPropertyGrid = new System.Windows.Forms.PropertyGrid();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.clearHistoryButton = new System.Windows.Forms.Button();
            this.forwardButton = new System.Windows.Forms.Button();
            this.navigationHistoryCombo = new System.Windows.Forms.ComboBox();
            this.backButton = new System.Windows.Forms.Button();
            this.failNavigationCheckBox = new System.Windows.Forms.CheckBox();
            this.splitContainer3 = new System.Windows.Forms.SplitContainer();
            this.explorerBrowser = new Microsoft.WindowsAPICodePack.Controls.WindowsForms.ExplorerBrowser();
            this.itemsTabControl = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.selectedItemsTextBox = new System.Windows.Forms.RichTextBox();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.eventHistoryTextBox = new System.Windows.Forms.RichTextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.splitContainer3.Panel1.SuspendLayout();
            this.splitContainer3.Panel2.SuspendLayout();
            this.splitContainer3.SuspendLayout();
            this.itemsTabControl.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 494);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(83, 13);
            this.label2.TabIndex = 8;
            this.label2.Text = "Content Options";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 12);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(97, 13);
            this.label3.TabIndex = 9;
            this.label3.Text = "Navigation Options";
            // 
            // itemsTextBox
            // 
            this.itemsTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.itemsTextBox.Location = new System.Drawing.Point(3, 3);
            this.itemsTextBox.Name = "itemsTextBox";
            this.itemsTextBox.Size = new System.Drawing.Size(602, 158);
            this.itemsTextBox.TabIndex = 0;
            this.itemsTextBox.Text = "";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(30, 11);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(51, 13);
            this.label7.TabIndex = 8;
            this.label7.Text = "File Path:";
            // 
            // filePathNavigate
            // 
            this.filePathNavigate.Enabled = false;
            this.filePathNavigate.Location = new System.Drawing.Point(464, 5);
            this.filePathNavigate.Name = "filePathNavigate";
            this.filePathNavigate.Size = new System.Drawing.Size(126, 22);
            this.filePathNavigate.TabIndex = 7;
            this.filePathNavigate.Text = "Navigate File";
            this.filePathNavigate.UseVisualStyleBackColor = true;
            this.filePathNavigate.Click += new System.EventHandler(this.filePathNavigate_Click);
            // 
            // filePathEdit
            // 
            this.filePathEdit.Location = new System.Drawing.Point(87, 7);
            this.filePathEdit.Name = "filePathEdit";
            this.filePathEdit.Size = new System.Drawing.Size(359, 20);
            this.filePathEdit.TabIndex = 6;
            this.filePathEdit.TextChanged += new System.EventHandler(this.filePathEdit_TextChanged);
            // 
            // knownFolderNavigate
            // 
            this.knownFolderNavigate.Enabled = false;
            this.knownFolderNavigate.Location = new System.Drawing.Point(464, 60);
            this.knownFolderNavigate.Name = "knownFolderNavigate";
            this.knownFolderNavigate.Size = new System.Drawing.Size(126, 23);
            this.knownFolderNavigate.TabIndex = 5;
            this.knownFolderNavigate.Text = "Navigate Known Folder";
            this.knownFolderNavigate.UseVisualStyleBackColor = true;
            this.knownFolderNavigate.Click += new System.EventHandler(this.knownFolderNavigate_Click);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(6, 66);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(75, 13);
            this.label6.TabIndex = 4;
            this.label6.Text = "Known Folder:";
            // 
            // knownFolderCombo
            // 
            this.knownFolderCombo.FormattingEnabled = true;
            this.knownFolderCombo.Location = new System.Drawing.Point(87, 63);
            this.knownFolderCombo.Name = "knownFolderCombo";
            this.knownFolderCombo.Size = new System.Drawing.Size(359, 21);
            this.knownFolderCombo.TabIndex = 3;
            this.knownFolderCombo.SelectedIndexChanged += new System.EventHandler(this.knownFolderCombo_SelectedIndexChanged);
            // 
            // navigateButton
            // 
            this.navigateButton.Enabled = false;
            this.navigateButton.Location = new System.Drawing.Point(464, 32);
            this.navigateButton.Name = "navigateButton";
            this.navigateButton.Size = new System.Drawing.Size(127, 23);
            this.navigateButton.TabIndex = 2;
            this.navigateButton.Text = "Navigate Path";
            this.navigateButton.UseVisualStyleBackColor = true;
            this.navigateButton.Click += new System.EventHandler(this.navigateButton_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(17, 38);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(64, 13);
            this.label5.TabIndex = 1;
            this.label5.Text = "Folder Path:";
            // 
            // pathEdit
            // 
            this.pathEdit.Location = new System.Drawing.Point(87, 34);
            this.pathEdit.Name = "pathEdit";
            this.pathEdit.Size = new System.Drawing.Size(359, 20);
            this.pathEdit.TabIndex = 0;
            this.pathEdit.TextChanged += new System.EventHandler(this.pathEdit_TextChanged);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer1.IsSplitterFixed = true;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.label1);
            this.splitContainer1.Panel1.Controls.Add(this.visibilityPropertyGrid);
            this.splitContainer1.Panel1.Controls.Add(this.contentPropertyGrid);
            this.splitContainer1.Panel1.Controls.Add(this.navigationPropertyGrid);
            this.splitContainer1.Panel1.Controls.Add(this.label2);
            this.splitContainer1.Panel1.Controls.Add(this.label3);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
            this.splitContainer1.Size = new System.Drawing.Size(841, 904);
            this.splitContainer1.SplitterDistance = 221;
            this.splitContainer1.TabIndex = 11;
            // 
            // visibilityPropertyGrid
            // 
            this.visibilityPropertyGrid.Location = new System.Drawing.Point(6, 230);
            this.visibilityPropertyGrid.Name = "visibilityPropertyGrid";
            this.visibilityPropertyGrid.Size = new System.Drawing.Size(213, 248);
            this.visibilityPropertyGrid.TabIndex = 13;
            // 
            // contentPropertyGrid
            // 
            this.contentPropertyGrid.Location = new System.Drawing.Point(6, 510);
            this.contentPropertyGrid.Name = "contentPropertyGrid";
            this.contentPropertyGrid.Size = new System.Drawing.Size(212, 391);
            this.contentPropertyGrid.TabIndex = 12;
            // 
            // navigationPropertyGrid
            // 
            this.navigationPropertyGrid.Location = new System.Drawing.Point(6, 28);
            this.navigationPropertyGrid.Name = "navigationPropertyGrid";
            this.navigationPropertyGrid.Size = new System.Drawing.Size(212, 183);
            this.navigationPropertyGrid.TabIndex = 11;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer2.IsSplitterFixed = true;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.clearHistoryButton);
            this.splitContainer2.Panel1.Controls.Add(this.forwardButton);
            this.splitContainer2.Panel1.Controls.Add(this.navigationHistoryCombo);
            this.splitContainer2.Panel1.Controls.Add(this.backButton);
            this.splitContainer2.Panel1.Controls.Add(this.failNavigationCheckBox);
            this.splitContainer2.Panel1.Controls.Add(this.label5);
            this.splitContainer2.Panel1.Controls.Add(this.pathEdit);
            this.splitContainer2.Panel1.Controls.Add(this.label7);
            this.splitContainer2.Panel1.Controls.Add(this.navigateButton);
            this.splitContainer2.Panel1.Controls.Add(this.filePathNavigate);
            this.splitContainer2.Panel1.Controls.Add(this.knownFolderCombo);
            this.splitContainer2.Panel1.Controls.Add(this.filePathEdit);
            this.splitContainer2.Panel1.Controls.Add(this.label6);
            this.splitContainer2.Panel1.Controls.Add(this.knownFolderNavigate);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.splitContainer3);
            this.splitContainer2.Size = new System.Drawing.Size(616, 904);
            this.splitContainer2.SplitterDistance = 139;
            this.splitContainer2.TabIndex = 0;
            // 
            // clearHistoryButton
            // 
            this.clearHistoryButton.Location = new System.Drawing.Point(466, 112);
            this.clearHistoryButton.Name = "clearHistoryButton";
            this.clearHistoryButton.Size = new System.Drawing.Size(125, 23);
            this.clearHistoryButton.TabIndex = 14;
            this.clearHistoryButton.Text = "Clear History";
            this.clearHistoryButton.UseVisualStyleBackColor = true;
            this.clearHistoryButton.Click += new System.EventHandler(this.clearHistoryButton_Click);
            // 
            // forwardButton
            // 
            this.forwardButton.Font = new System.Drawing.Font("Comic Sans MS", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.forwardButton.Location = new System.Drawing.Point(62, 87);
            this.forwardButton.Name = "forwardButton";
            this.forwardButton.Size = new System.Drawing.Size(48, 48);
            this.forwardButton.TabIndex = 13;
            this.forwardButton.Text = ">";
            this.forwardButton.UseVisualStyleBackColor = true;
            this.forwardButton.Click += new System.EventHandler(this.forwardButton_Click);
            // 
            // navigationHistoryCombo
            // 
            this.navigationHistoryCombo.FormattingEnabled = true;
            this.navigationHistoryCombo.Location = new System.Drawing.Point(129, 99);
            this.navigationHistoryCombo.Name = "navigationHistoryCombo";
            this.navigationHistoryCombo.Size = new System.Drawing.Size(317, 21);
            this.navigationHistoryCombo.TabIndex = 12;
            this.navigationHistoryCombo.SelectedIndexChanged += new System.EventHandler(this.navigationHistoryCombo_SelectedIndexChanged);
            // 
            // backButton
            // 
            this.backButton.Font = new System.Drawing.Font("Comic Sans MS", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.backButton.Location = new System.Drawing.Point(8, 87);
            this.backButton.Name = "backButton";
            this.backButton.Size = new System.Drawing.Size(48, 48);
            this.backButton.TabIndex = 10;
            this.backButton.Text = "<";
            this.backButton.UseVisualStyleBackColor = true;
            this.backButton.Click += new System.EventHandler(this.backButton_Click);
            // 
            // failNavigationCheckBox
            // 
            this.failNavigationCheckBox.AutoSize = true;
            this.failNavigationCheckBox.Location = new System.Drawing.Point(466, 92);
            this.failNavigationCheckBox.Name = "failNavigationCheckBox";
            this.failNavigationCheckBox.Size = new System.Drawing.Size(138, 17);
            this.failNavigationCheckBox.TabIndex = 9;
            this.failNavigationCheckBox.Text = "Force Navigation to Fail";
            this.failNavigationCheckBox.UseVisualStyleBackColor = true;
            // 
            // splitContainer3
            // 
            this.splitContainer3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer3.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer3.IsSplitterFixed = true;
            this.splitContainer3.Location = new System.Drawing.Point(0, 0);
            this.splitContainer3.Name = "splitContainer3";
            this.splitContainer3.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer3.Panel1
            // 
            this.splitContainer3.Panel1.Controls.Add(this.explorerBrowser);
            // 
            // splitContainer3.Panel2
            // 
            this.splitContainer3.Panel2.Controls.Add(this.itemsTabControl);
            this.splitContainer3.Size = new System.Drawing.Size(616, 761);
            this.splitContainer3.SplitterDistance = 567;
            this.splitContainer3.TabIndex = 0;
            // 
            // explorerBrowser
            // 
            this.explorerBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.explorerBrowser.Location = new System.Drawing.Point(0, 0);
            this.explorerBrowser.Name = "explorerBrowser";
            this.explorerBrowser.PropertyBagName = "Microsoft.WindowsAPICodePack.Controls.WindowsForms.ExplorerBrowser";
            this.explorerBrowser.Size = new System.Drawing.Size(616, 567);
            this.explorerBrowser.TabIndex = 0;
            // 
            // itemsTabControl
            // 
            this.itemsTabControl.Controls.Add(this.tabPage1);
            this.itemsTabControl.Controls.Add(this.tabPage2);
            this.itemsTabControl.Controls.Add(this.tabPage3);
            this.itemsTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.itemsTabControl.Location = new System.Drawing.Point(0, 0);
            this.itemsTabControl.Name = "itemsTabControl";
            this.itemsTabControl.SelectedIndex = 0;
            this.itemsTabControl.Size = new System.Drawing.Size(616, 190);
            this.itemsTabControl.TabIndex = 1;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.itemsTextBox);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(608, 164);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Items (Count=0)";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.selectedItemsTextBox);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(608, 164);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Selected Items (Count=0)";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // selectedItemsTextBox
            // 
            this.selectedItemsTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.selectedItemsTextBox.Location = new System.Drawing.Point(3, 3);
            this.selectedItemsTextBox.Name = "selectedItemsTextBox";
            this.selectedItemsTextBox.Size = new System.Drawing.Size(602, 158);
            this.selectedItemsTextBox.TabIndex = 0;
            this.selectedItemsTextBox.Text = "";
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.eventHistoryTextBox);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Size = new System.Drawing.Size(608, 164);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Event History";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // eventHistoryTextBox
            // 
            this.eventHistoryTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.eventHistoryTextBox.Location = new System.Drawing.Point(0, 0);
            this.eventHistoryTextBox.Name = "eventHistoryTextBox";
            this.eventHistoryTextBox.Size = new System.Drawing.Size(608, 164);
            this.eventHistoryTextBox.TabIndex = 0;
            this.eventHistoryTextBox.Text = "";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 214);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(71, 13);
            this.label1.TabIndex = 14;
            this.label1.Text = "Pane Options";
            // 
            // ExplorerBrowserTestForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(841, 904);
            this.Controls.Add(this.splitContainer1);
            this.Name = "ExplorerBrowserTestForm";
            this.Text = "Explorer Browser Demo";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel1.PerformLayout();
            this.splitContainer2.Panel2.ResumeLayout(false);
            this.splitContainer2.ResumeLayout(false);
            this.splitContainer3.Panel1.ResumeLayout(false);
            this.splitContainer3.Panel2.ResumeLayout(false);
            this.splitContainer3.ResumeLayout(false);
            this.itemsTabControl.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.tabPage3.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button navigateButton;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox pathEdit;
        private System.Windows.Forms.Button knownFolderNavigate;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox knownFolderCombo;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Button filePathNavigate;
        private System.Windows.Forms.TextBox filePathEdit;
        private System.Windows.Forms.RichTextBox itemsTextBox;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.SplitContainer splitContainer3;
        private Microsoft.WindowsAPICodePack.Controls.WindowsForms.ExplorerBrowser explorerBrowser;
        private System.Windows.Forms.PropertyGrid navigationPropertyGrid;
        private System.Windows.Forms.PropertyGrid contentPropertyGrid;
        private System.Windows.Forms.PropertyGrid visibilityPropertyGrid;
        private System.Windows.Forms.CheckBox failNavigationCheckBox;
        private System.Windows.Forms.TabControl itemsTabControl;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.RichTextBox selectedItemsTextBox;
        private System.Windows.Forms.Button forwardButton;
        private System.Windows.Forms.ComboBox navigationHistoryCombo;
        private System.Windows.Forms.Button backButton;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.RichTextBox eventHistoryTextBox;
        private System.Windows.Forms.Button clearHistoryButton;
        private System.Windows.Forms.Label label1;

    }
}

