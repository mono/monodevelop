namespace TaskDialogDemo
{
    partial class TestHarness
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
            this.rdoInformation = new System.Windows.Forms.RadioButton();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.txtTitle = new System.Windows.Forms.TextBox();
            this.txtInstruction = new System.Windows.Forms.TextBox();
            this.txtContent = new System.Windows.Forms.TextBox();
            this.rdoShield = new System.Windows.Forms.RadioButton();
            this.rdoWarning = new System.Windows.Forms.RadioButton();
            this.rdoError = new System.Windows.Forms.RadioButton();
            this.rdoNone = new System.Windows.Forms.RadioButton();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.chkYes = new System.Windows.Forms.CheckBox();
            this.chkRetry = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.chkNo = new System.Windows.Forms.CheckBox();
            this.chkCancel = new System.Windows.Forms.CheckBox();
            this.chkOK = new System.Windows.Forms.CheckBox();
            this.chkClose = new System.Windows.Forms.CheckBox();
            this.cmdShow = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.resultLbl = new System.Windows.Forms.TextBox();
            this.groupBox3.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // rdoInformation
            // 
            this.rdoInformation.AutoSize = true;
            this.rdoInformation.Location = new System.Drawing.Point(130, 49);
            this.rdoInformation.Name = "rdoInformation";
            this.rdoInformation.Size = new System.Drawing.Size(77, 17);
            this.rdoInformation.TabIndex = 3;
            this.rdoInformation.Text = "Information";
            this.rdoInformation.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.label3);
            this.groupBox3.Controls.Add(this.label2);
            this.groupBox3.Controls.Add(this.label1);
            this.groupBox3.Controls.Add(this.txtTitle);
            this.groupBox3.Controls.Add(this.txtInstruction);
            this.groupBox3.Controls.Add(this.txtContent);
            this.groupBox3.Location = new System.Drawing.Point(254, 7);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(304, 97);
            this.groupBox3.TabIndex = 23;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Texts";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 71);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(71, 13);
            this.label3.TabIndex = 18;
            this.label3.Text = "Content Text:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 47);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(85, 13);
            this.label2.TabIndex = 17;
            this.label2.Text = "Main Instruction:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 21);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(30, 13);
            this.label1.TabIndex = 16;
            this.label1.Text = "Title:";
            // 
            // txtTitle
            // 
            this.txtTitle.Location = new System.Drawing.Point(102, 18);
            this.txtTitle.Name = "txtTitle";
            this.txtTitle.Size = new System.Drawing.Size(191, 20);
            this.txtTitle.TabIndex = 13;
            this.txtTitle.Text = "Enter your title in here";
            // 
            // txtInstruction
            // 
            this.txtInstruction.Location = new System.Drawing.Point(102, 44);
            this.txtInstruction.Name = "txtInstruction";
            this.txtInstruction.Size = new System.Drawing.Size(191, 20);
            this.txtInstruction.TabIndex = 14;
            this.txtInstruction.Text = "Enter your main instruction here";
            // 
            // txtContent
            // 
            this.txtContent.Location = new System.Drawing.Point(102, 68);
            this.txtContent.Name = "txtContent";
            this.txtContent.Size = new System.Drawing.Size(191, 20);
            this.txtContent.TabIndex = 15;
            this.txtContent.Text = "Enter your content text in here";
            // 
            // rdoShield
            // 
            this.rdoShield.AutoSize = true;
            this.rdoShield.Location = new System.Drawing.Point(14, 71);
            this.rdoShield.Name = "rdoShield";
            this.rdoShield.Size = new System.Drawing.Size(54, 17);
            this.rdoShield.TabIndex = 5;
            this.rdoShield.Text = "Shield";
            this.rdoShield.UseVisualStyleBackColor = true;
            // 
            // rdoWarning
            // 
            this.rdoWarning.AutoSize = true;
            this.rdoWarning.Location = new System.Drawing.Point(15, 48);
            this.rdoWarning.Name = "rdoWarning";
            this.rdoWarning.Size = new System.Drawing.Size(65, 17);
            this.rdoWarning.TabIndex = 2;
            this.rdoWarning.Text = "Warning";
            this.rdoWarning.UseVisualStyleBackColor = true;
            // 
            // rdoError
            // 
            this.rdoError.AutoSize = true;
            this.rdoError.Location = new System.Drawing.Point(130, 26);
            this.rdoError.Name = "rdoError";
            this.rdoError.Size = new System.Drawing.Size(47, 17);
            this.rdoError.TabIndex = 1;
            this.rdoError.Text = "Error";
            this.rdoError.UseVisualStyleBackColor = true;
            // 
            // rdoNone
            // 
            this.rdoNone.AutoSize = true;
            this.rdoNone.Checked = true;
            this.rdoNone.Location = new System.Drawing.Point(15, 25);
            this.rdoNone.Name = "rdoNone";
            this.rdoNone.Size = new System.Drawing.Size(51, 17);
            this.rdoNone.TabIndex = 0;
            this.rdoNone.TabStop = true;
            this.rdoNone.Text = "None";
            this.rdoNone.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.rdoShield);
            this.groupBox2.Controls.Add(this.rdoInformation);
            this.groupBox2.Controls.Add(this.rdoWarning);
            this.groupBox2.Controls.Add(this.rdoError);
            this.groupBox2.Controls.Add(this.rdoNone);
            this.groupBox2.Location = new System.Drawing.Point(9, 7);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(235, 97);
            this.groupBox2.TabIndex = 21;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Icon";
            // 
            // chkYes
            // 
            this.chkYes.AutoSize = true;
            this.chkYes.Location = new System.Drawing.Point(14, 73);
            this.chkYes.Name = "chkYes";
            this.chkYes.Size = new System.Drawing.Size(44, 17);
            this.chkYes.TabIndex = 2;
            this.chkYes.Text = "Yes";
            this.chkYes.UseVisualStyleBackColor = true;
            // 
            // chkRetry
            // 
            this.chkRetry.AutoSize = true;
            this.chkRetry.Location = new System.Drawing.Point(126, 27);
            this.chkRetry.Name = "chkRetry";
            this.chkRetry.Size = new System.Drawing.Size(51, 17);
            this.chkRetry.TabIndex = 10;
            this.chkRetry.Text = "Retry";
            this.chkRetry.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.chkYes);
            this.groupBox1.Controls.Add(this.chkRetry);
            this.groupBox1.Controls.Add(this.chkNo);
            this.groupBox1.Controls.Add(this.chkCancel);
            this.groupBox1.Controls.Add(this.chkOK);
            this.groupBox1.Controls.Add(this.chkClose);
            this.groupBox1.Location = new System.Drawing.Point(9, 106);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(235, 124);
            this.groupBox1.TabIndex = 20;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Buttons";
            // 
            // chkNo
            // 
            this.chkNo.AutoSize = true;
            this.chkNo.Location = new System.Drawing.Point(14, 96);
            this.chkNo.Name = "chkNo";
            this.chkNo.Size = new System.Drawing.Size(40, 17);
            this.chkNo.TabIndex = 8;
            this.chkNo.Text = "No";
            this.chkNo.UseVisualStyleBackColor = true;
            // 
            // chkCancel
            // 
            this.chkCancel.AutoSize = true;
            this.chkCancel.Location = new System.Drawing.Point(14, 50);
            this.chkCancel.Name = "chkCancel";
            this.chkCancel.Size = new System.Drawing.Size(59, 17);
            this.chkCancel.TabIndex = 4;
            this.chkCancel.Text = "Cancel";
            this.chkCancel.UseVisualStyleBackColor = true;
            // 
            // chkOK
            // 
            this.chkOK.AutoSize = true;
            this.chkOK.Checked = true;
            this.chkOK.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkOK.Location = new System.Drawing.Point(14, 27);
            this.chkOK.Name = "chkOK";
            this.chkOK.Size = new System.Drawing.Size(41, 17);
            this.chkOK.TabIndex = 7;
            this.chkOK.Text = "OK";
            this.chkOK.UseVisualStyleBackColor = true;
            // 
            // chkClose
            // 
            this.chkClose.AutoSize = true;
            this.chkClose.Location = new System.Drawing.Point(125, 50);
            this.chkClose.Name = "chkClose";
            this.chkClose.Size = new System.Drawing.Size(52, 17);
            this.chkClose.TabIndex = 6;
            this.chkClose.Text = "Close";
            this.chkClose.UseVisualStyleBackColor = true;
            // 
            // cmdShow
            // 
            this.cmdShow.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmdShow.Location = new System.Drawing.Point(368, 277);
            this.cmdShow.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.cmdShow.Name = "cmdShow";
            this.cmdShow.Size = new System.Drawing.Size(190, 55);
            this.cmdShow.TabIndex = 19;
            this.cmdShow.Text = "TaskDialog.Show()";
            this.cmdShow.UseVisualStyleBackColor = true;
            this.cmdShow.Click += new System.EventHandler(this.cmdShow_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(251, 117);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(182, 13);
            this.label4.TabIndex = 24;
            this.label4.Text = "Result from TaskDialog.Show()";
            // 
            // resultLbl
            // 
            this.resultLbl.Location = new System.Drawing.Point(254, 133);
            this.resultLbl.Multiline = true;
            this.resultLbl.Name = "resultLbl";
            this.resultLbl.ReadOnly = true;
            this.resultLbl.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.resultLbl.Size = new System.Drawing.Size(293, 123);
            this.resultLbl.TabIndex = 25;
            // 
            // TestHarness
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(566, 345);
            this.Controls.Add(this.resultLbl);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.cmdShow);
            this.Name = "TestHarness";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "TaskDialog Test Harness";
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RadioButton rdoInformation;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.TextBox txtTitle;
        private System.Windows.Forms.TextBox txtInstruction;
        private System.Windows.Forms.TextBox txtContent;
        private System.Windows.Forms.RadioButton rdoShield;
        private System.Windows.Forms.RadioButton rdoWarning;
        private System.Windows.Forms.RadioButton rdoError;
        private System.Windows.Forms.RadioButton rdoNone;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.CheckBox chkYes;
        private System.Windows.Forms.CheckBox chkRetry;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox chkNo;
        private System.Windows.Forms.CheckBox chkCancel;
        private System.Windows.Forms.CheckBox chkOK;
        private System.Windows.Forms.CheckBox chkClose;
        private System.Windows.Forms.Button cmdShow;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox resultLbl;
    }
}