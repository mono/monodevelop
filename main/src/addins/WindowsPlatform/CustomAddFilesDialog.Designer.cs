namespace MonoDevelop.Platform
{
    partial class CustomAddFilesDialog
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.checkOverride = new System.Windows.Forms.CheckBox();
            this.comboActions = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // checkOverride
            // 
            this.checkOverride.AutoSize = true;
            this.checkOverride.Location = new System.Drawing.Point(10, 13);
            this.checkOverride.Name = "checkOverride";
            this.checkOverride.Size = new System.Drawing.Size(158, 17);
            this.checkOverride.TabIndex = 0;
            this.checkOverride.Text = "Override default build action";
            this.checkOverride.UseVisualStyleBackColor = true;
            this.checkOverride.CheckedChanged += new System.EventHandler(this.checkOverride_CheckedChanged);
            // 
            // comboActions
            // 
            this.comboActions.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboActions.Enabled = false;
            this.comboActions.FormattingEnabled = true;
            this.comboActions.Location = new System.Drawing.Point(174, 11);
            this.comboActions.Name = "comboActions";
            this.comboActions.Size = new System.Drawing.Size(192, 21);
            this.comboActions.TabIndex = 1;
            // 
            // CustomAddFilesDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.comboActions);
            this.Controls.Add(this.checkOverride);
            this.Name = "CustomAddFilesDialog";
            this.Padding = new System.Windows.Forms.Padding(7);
            this.Size = new System.Drawing.Size(384, 46);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox checkOverride;
        private System.Windows.Forms.ComboBox comboActions;

    }
}
