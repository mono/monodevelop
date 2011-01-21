namespace D2DPaint
{
    partial class TextDialog
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
            System.Windows.Forms.Button CancelTextButton;
            this.AddTextButton = new System.Windows.Forms.Button();
            this.underLineCheckBox = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.sizeCombo = new System.Windows.Forms.ComboBox();
            this.styleCombo = new System.Windows.Forms.ComboBox();
            this.weightCombo = new System.Windows.Forms.ComboBox();
            this.stretchCombo = new System.Windows.Forms.ComboBox();
            this.textBox = new System.Windows.Forms.TextBox();
            this.strikethroughCheckBox = new System.Windows.Forms.CheckBox();
            this.fontFamilyCombo = new D2DPaint.FontEnumComboBox();
            CancelTextButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // CancelTextButton
            // 
            CancelTextButton.Location = new System.Drawing.Point(287, 221);
            CancelTextButton.Name = "CancelTextButton";
            CancelTextButton.Size = new System.Drawing.Size(138, 31);
            CancelTextButton.TabIndex = 1;
            CancelTextButton.Text = "Cancel";
            CancelTextButton.UseVisualStyleBackColor = true;
            CancelTextButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // AddTextButton
            // 
            this.AddTextButton.Location = new System.Drawing.Point(99, 221);
            this.AddTextButton.Name = "AddTextButton";
            this.AddTextButton.Size = new System.Drawing.Size(138, 31);
            this.AddTextButton.TabIndex = 0;
            this.AddTextButton.Text = "Add Text";
            this.AddTextButton.UseVisualStyleBackColor = true;
            this.AddTextButton.Click += new System.EventHandler(this.AddTextButton_Click);
            // 
            // underLineCheckBox
            // 
            this.underLineCheckBox.AutoSize = true;
            this.underLineCheckBox.Location = new System.Drawing.Point(37, 183);
            this.underLineCheckBox.Name = "underLineCheckBox";
            this.underLineCheckBox.Size = new System.Drawing.Size(71, 17);
            this.underLineCheckBox.TabIndex = 2;
            this.underLineCheckBox.Text = "Underline";
            this.underLineCheckBox.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(34, 30);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(59, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Font Name";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(34, 58);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(51, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Font Size";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(34, 86);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(54, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Font Style";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(34, 114);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(65, 13);
            this.label4.TabIndex = 4;
            this.label4.Text = "Font Weight";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(34, 142);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(65, 13);
            this.label5.TabIndex = 4;
            this.label5.Text = "Font Stretch";
            // 
            // sizeCombo
            // 
            this.sizeCombo.FormattingEnabled = true;
            this.sizeCombo.Items.AddRange(new object[] {
            "4",
            "6",
            "8",
            "10",
            "12",
            "14",
            "20",
            "24",
            "32",
            "36",
            "42",
            "60",
            ""});
            this.sizeCombo.Location = new System.Drawing.Point(116, 58);
            this.sizeCombo.Name = "sizeCombo";
            this.sizeCombo.Size = new System.Drawing.Size(121, 21);
            this.sizeCombo.TabIndex = 5;
            // 
            // styleCombo
            // 
            this.styleCombo.FormattingEnabled = true;
            this.styleCombo.Items.AddRange(new object[] {
            "Normal",
            "Oblique",
            "Italic"});
            this.styleCombo.Location = new System.Drawing.Point(116, 86);
            this.styleCombo.Name = "styleCombo";
            this.styleCombo.Size = new System.Drawing.Size(121, 21);
            this.styleCombo.TabIndex = 5;
            // 
            // weightCombo
            // 
            this.weightCombo.FormattingEnabled = true;
            this.weightCombo.Items.AddRange(new object[] {
            "Thin",
            "Extra Light",
            "Light",
            "Normal",
            "Medium",
            "Semi Bold",
            "Bold",
            "Extra Bold",
            "Black"});
            this.weightCombo.Location = new System.Drawing.Point(116, 114);
            this.weightCombo.Name = "weightCombo";
            this.weightCombo.Size = new System.Drawing.Size(121, 21);
            this.weightCombo.TabIndex = 5;
            // 
            // stretchCombo
            // 
            this.stretchCombo.FormattingEnabled = true;
            this.stretchCombo.Items.AddRange(new object[] {
            "None",
            "Ultra Condensed",
            "Extra Condensed",
            "Condensed",
            "Semi Condensed",
            "Normal",
            "Semi Expanded",
            "Expanded",
            "Extra Expanded",
            "Ultra Expanded"});
            this.stretchCombo.Location = new System.Drawing.Point(116, 142);
            this.stretchCombo.Name = "stretchCombo";
            this.stretchCombo.Size = new System.Drawing.Size(121, 21);
            this.stretchCombo.TabIndex = 5;
            // 
            // textBox
            // 
            this.textBox.AcceptsReturn = true;
            this.textBox.Location = new System.Drawing.Point(268, 30);
            this.textBox.Multiline = true;
            this.textBox.Name = "textBox";
            this.textBox.Size = new System.Drawing.Size(200, 133);
            this.textBox.TabIndex = 6;
            this.textBox.Text = "Add Text Here";
            // 
            // strikethroughCheckBox
            // 
            this.strikethroughCheckBox.AutoSize = true;
            this.strikethroughCheckBox.Location = new System.Drawing.Point(131, 183);
            this.strikethroughCheckBox.Name = "strikethroughCheckBox";
            this.strikethroughCheckBox.Size = new System.Drawing.Size(89, 17);
            this.strikethroughCheckBox.TabIndex = 2;
            this.strikethroughCheckBox.Text = "Strikethrough";
            this.strikethroughCheckBox.UseVisualStyleBackColor = true;
            // 
            // fontFamilyCombo
            // 
            this.fontFamilyCombo.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
            this.fontFamilyCombo.DropDownHeight = 206;
            this.fontFamilyCombo.FormattingEnabled = true;
            this.fontFamilyCombo.IntegralHeight = false;
            this.fontFamilyCombo.Location = new System.Drawing.Point(116, 27);
            this.fontFamilyCombo.Name = "fontFamilyCombo";
            this.fontFamilyCombo.Size = new System.Drawing.Size(121, 21);
            this.fontFamilyCombo.TabIndex = 7;
            // 
            // TextDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(512, 292);
            this.Controls.Add(this.fontFamilyCombo);
            this.Controls.Add(this.textBox);
            this.Controls.Add(this.stretchCombo);
            this.Controls.Add(this.weightCombo);
            this.Controls.Add(this.styleCombo);
            this.Controls.Add(this.sizeCombo);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.strikethroughCheckBox);
            this.Controls.Add(this.underLineCheckBox);
            this.Controls.Add(CancelTextButton);
            this.Controls.Add(this.AddTextButton);
            this.Name = "TextDialog";
            this.Text = "TextDialog";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button AddTextButton;
        private System.Windows.Forms.CheckBox underLineCheckBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox sizeCombo;
        private System.Windows.Forms.ComboBox styleCombo;
        private System.Windows.Forms.ComboBox weightCombo;
        private System.Windows.Forms.ComboBox stretchCombo;
        private System.Windows.Forms.TextBox textBox;
        private System.Windows.Forms.CheckBox strikethroughCheckBox;
        private FontEnumComboBox fontFamilyCombo;
    }
}