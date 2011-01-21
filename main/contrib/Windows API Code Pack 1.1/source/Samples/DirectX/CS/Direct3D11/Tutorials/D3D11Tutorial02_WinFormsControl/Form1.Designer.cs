namespace Microsoft.WindowsAPICodePack.Samples.Direct3D11
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
            this.directControl = new Microsoft.WindowsAPICodePack.DirectX.Controls.DirectControl();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // directControl
            // 
            this.directControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.directControl.Location = new System.Drawing.Point(12, 12);
            this.directControl.Name = "directControl";
            this.directControl.Size = new System.Drawing.Size(606, 426);
            this.directControl.TabIndex = 0;
            this.directControl.Load += new System.EventHandler(this.directControl_Load);
            this.directControl.SizeChanged += new System.EventHandler(this.directControl_SizeChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(30, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "FPS:";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(630, 450);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.directControl);
            this.Name = "Form1";
            this.Text = "Direct3D 11 Tutorial 02";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Microsoft.WindowsAPICodePack.DirectX.Controls.DirectControl directControl;
        private System.Windows.Forms.Label label1;
    }
}

