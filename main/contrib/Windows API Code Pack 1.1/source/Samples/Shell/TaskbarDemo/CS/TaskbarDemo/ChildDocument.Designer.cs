namespace TaskbarDemo
{
    partial class ChildDocument
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ChildDocument));
            this.groupBoxIconOverlay = new System.Windows.Forms.GroupBox();
            this.labelNoIconOverlay = new System.Windows.Forms.Label();
            this.pictureIconOverlay3 = new System.Windows.Forms.PictureBox();
            this.pictureIconOverlay2 = new System.Windows.Forms.PictureBox();
            this.label7 = new System.Windows.Forms.Label();
            this.pictureIconOverlay1 = new System.Windows.Forms.PictureBox();
            this.groupBoxCustomCategories = new System.Windows.Forms.GroupBox();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.label5 = new System.Windows.Forms.Label();
            this.buttonRefreshTaskbarList = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.groupBoxProgressBar = new System.Windows.Forms.GroupBox();
            this.trackBar1 = new System.Windows.Forms.TrackBar();
            this.label8 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.comboBoxProgressBarStates = new System.Windows.Forms.ComboBox();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.groupBoxIconOverlay.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureIconOverlay3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureIconOverlay2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureIconOverlay1)).BeginInit();
            this.groupBoxCustomCategories.SuspendLayout();
            this.groupBoxProgressBar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBoxIconOverlay
            // 
            this.groupBoxIconOverlay.Controls.Add(this.labelNoIconOverlay);
            this.groupBoxIconOverlay.Controls.Add(this.pictureIconOverlay3);
            this.groupBoxIconOverlay.Controls.Add(this.pictureIconOverlay2);
            this.groupBoxIconOverlay.Controls.Add(this.label7);
            this.groupBoxIconOverlay.Controls.Add(this.pictureIconOverlay1);
            this.groupBoxIconOverlay.Location = new System.Drawing.Point(13, 342);
            this.groupBoxIconOverlay.Name = "groupBoxIconOverlay";
            this.groupBoxIconOverlay.Size = new System.Drawing.Size(305, 88);
            this.groupBoxIconOverlay.TabIndex = 12;
            this.groupBoxIconOverlay.TabStop = false;
            this.groupBoxIconOverlay.Text = "Icon Overlay";
            // 
            // labelNoIconOverlay
            // 
            this.labelNoIconOverlay.Location = new System.Drawing.Point(9, 50);
            this.labelNoIconOverlay.Name = "labelNoIconOverlay";
            this.labelNoIconOverlay.Size = new System.Drawing.Size(35, 13);
            this.labelNoIconOverlay.TabIndex = 0;
            this.labelNoIconOverlay.Text = "None";
            this.labelNoIconOverlay.Click += new System.EventHandler(this.labelNoIconOverlay_Click);
            // 
            // pictureIconOverlay3
            // 
            this.pictureIconOverlay3.Image = ((System.Drawing.Image)(resources.GetObject("pictureIconOverlay3.Image")));
            this.pictureIconOverlay3.Location = new System.Drawing.Point(126, 41);
            this.pictureIconOverlay3.Name = "pictureIconOverlay3";
            this.pictureIconOverlay3.Size = new System.Drawing.Size(32, 32);
            this.pictureIconOverlay3.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureIconOverlay3.TabIndex = 3;
            this.pictureIconOverlay3.TabStop = false;
            this.pictureIconOverlay3.Click += new System.EventHandler(this.pictureIconOverlay3_Click);
            // 
            // pictureIconOverlay2
            // 
            this.pictureIconOverlay2.Image = ((System.Drawing.Image)(resources.GetObject("pictureIconOverlay2.Image")));
            this.pictureIconOverlay2.Location = new System.Drawing.Point(88, 41);
            this.pictureIconOverlay2.Name = "pictureIconOverlay2";
            this.pictureIconOverlay2.Size = new System.Drawing.Size(32, 32);
            this.pictureIconOverlay2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureIconOverlay2.TabIndex = 2;
            this.pictureIconOverlay2.TabStop = false;
            this.pictureIconOverlay2.Click += new System.EventHandler(this.pictureIconOverlay2_Click);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(9, 20);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(209, 13);
            this.label7.TabIndex = 1;
            this.label7.Text = "Select an image to overlay on the task bar:";
            // 
            // pictureIconOverlay1
            // 
            this.pictureIconOverlay1.Image = ((System.Drawing.Image)(resources.GetObject("pictureIconOverlay1.Image")));
            this.pictureIconOverlay1.Location = new System.Drawing.Point(50, 41);
            this.pictureIconOverlay1.Name = "pictureIconOverlay1";
            this.pictureIconOverlay1.Size = new System.Drawing.Size(32, 32);
            this.pictureIconOverlay1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureIconOverlay1.TabIndex = 0;
            this.pictureIconOverlay1.TabStop = false;
            this.pictureIconOverlay1.Click += new System.EventHandler(this.pictureIconOverlay1_Click);
            // 
            // groupBoxCustomCategories
            // 
            this.groupBoxCustomCategories.Controls.Add(this.listBox1);
            this.groupBoxCustomCategories.Controls.Add(this.buttonRefreshTaskbarList);
            this.groupBoxCustomCategories.Controls.Add(this.label5);
            this.groupBoxCustomCategories.Enabled = false;
            this.groupBoxCustomCategories.Location = new System.Drawing.Point(13, 12);
            this.groupBoxCustomCategories.Name = "groupBoxCustomCategories";
            this.groupBoxCustomCategories.Size = new System.Drawing.Size(304, 174);
            this.groupBoxCustomCategories.TabIndex = 13;
            this.groupBoxCustomCategories.TabStop = false;
            this.groupBoxCustomCategories.Text = "Custom JumpList";
            // 
            // listBox1
            // 
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Items.AddRange(new object[] {
            "Notepad",
            "Calculator",
            "Paint",
            "WordPad",
            "Windows Explorer",
            "Internet Explorer",
            "Control Panel",
            "Documents Library"});
            this.listBox1.Location = new System.Drawing.Point(87, 30);
            this.listBox1.Name = "listBox1";
            this.listBox1.SelectionMode = System.Windows.Forms.SelectionMode.MultiSimple;
            this.listBox1.Size = new System.Drawing.Size(210, 108);
            this.listBox1.TabIndex = 11;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(8, 30);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(64, 13);
            this.label5.TabIndex = 8;
            this.label5.Text = "User Tasks:";
            // 
            // buttonRefreshTaskbarList
            // 
            this.buttonRefreshTaskbarList.Enabled = false;
            this.buttonRefreshTaskbarList.Location = new System.Drawing.Point(143, 144);
            this.buttonRefreshTaskbarList.Name = "buttonRefreshTaskbarList";
            this.buttonRefreshTaskbarList.Size = new System.Drawing.Size(123, 23);
            this.buttonRefreshTaskbarList.TabIndex = 14;
            this.buttonRefreshTaskbarList.Text = "Refresh JumpList";
            this.buttonRefreshTaskbarList.UseVisualStyleBackColor = true;
            this.buttonRefreshTaskbarList.Click += new System.EventHandler(this.buttonRefreshTaskbarList_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(11, 445);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(132, 23);
            this.button1.TabIndex = 15;
            this.button1.Text = "Add separate JumpList";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // groupBoxProgressBar
            // 
            this.groupBoxProgressBar.Controls.Add(this.trackBar1);
            this.groupBoxProgressBar.Controls.Add(this.label8);
            this.groupBoxProgressBar.Controls.Add(this.label6);
            this.groupBoxProgressBar.Controls.Add(this.comboBoxProgressBarStates);
            this.groupBoxProgressBar.Controls.Add(this.progressBar1);
            this.groupBoxProgressBar.Location = new System.Drawing.Point(13, 192);
            this.groupBoxProgressBar.Name = "groupBoxProgressBar";
            this.groupBoxProgressBar.Size = new System.Drawing.Size(305, 144);
            this.groupBoxProgressBar.TabIndex = 16;
            this.groupBoxProgressBar.TabStop = false;
            this.groupBoxProgressBar.Text = "Progress Bar";
            // 
            // trackBar1
            // 
            this.trackBar1.LargeChange = 25;
            this.trackBar1.Location = new System.Drawing.Point(92, 59);
            this.trackBar1.Maximum = 100;
            this.trackBar1.Name = "trackBar1";
            this.trackBar1.Size = new System.Drawing.Size(203, 45);
            this.trackBar1.SmallChange = 10;
            this.trackBar1.TabIndex = 12;
            this.trackBar1.TickStyle = System.Windows.Forms.TickStyle.None;
            this.trackBar1.Scroll += new System.EventHandler(this.trackBar1_Scroll);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(6, 71);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(77, 13);
            this.label8.TabIndex = 11;
            this.label8.Text = "Update value: ";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(6, 25);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(35, 13);
            this.label6.TabIndex = 3;
            this.label6.Text = "State:";
            // 
            // comboBoxProgressBarStates
            // 
            this.comboBoxProgressBarStates.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxProgressBarStates.FormattingEnabled = true;
            this.comboBoxProgressBarStates.Location = new System.Drawing.Point(47, 22);
            this.comboBoxProgressBarStates.Name = "comboBoxProgressBarStates";
            this.comboBoxProgressBarStates.Size = new System.Drawing.Size(154, 21);
            this.comboBoxProgressBarStates.TabIndex = 2;
            this.comboBoxProgressBarStates.SelectedIndexChanged += new System.EventHandler(this.comboBoxProgressBarStates_SelectedIndexChanged);
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(11, 110);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(285, 23);
            this.progressBar1.Step = 5;
            this.progressBar1.TabIndex = 0;
            // 
            // ChildDocument
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(327, 510);
            this.Controls.Add(this.groupBoxProgressBar);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.groupBoxCustomCategories);
            this.Controls.Add(this.groupBoxIconOverlay);
            this.Name = "ChildDocument";
            this.Text = "Child Document Window";
            this.groupBoxIconOverlay.ResumeLayout(false);
            this.groupBoxIconOverlay.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureIconOverlay3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureIconOverlay2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureIconOverlay1)).EndInit();
            this.groupBoxCustomCategories.ResumeLayout(false);
            this.groupBoxCustomCategories.PerformLayout();
            this.groupBoxProgressBar.ResumeLayout(false);
            this.groupBoxProgressBar.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBoxIconOverlay;
        private System.Windows.Forms.Label labelNoIconOverlay;
        private System.Windows.Forms.PictureBox pictureIconOverlay3;
        private System.Windows.Forms.PictureBox pictureIconOverlay2;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.PictureBox pictureIconOverlay1;
        private System.Windows.Forms.GroupBox groupBoxCustomCategories;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button buttonRefreshTaskbarList;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.GroupBox groupBoxProgressBar;
        private System.Windows.Forms.TrackBar trackBar1;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox comboBoxProgressBarStates;
        private System.Windows.Forms.ProgressBar progressBar1;
    }
}