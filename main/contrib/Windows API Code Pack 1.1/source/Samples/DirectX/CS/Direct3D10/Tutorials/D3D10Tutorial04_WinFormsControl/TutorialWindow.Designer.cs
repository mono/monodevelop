namespace D3D10Tutorial04_WinFormsControl
{
    partial class TutorialWindow
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TutorialWindow));
            this.directControl = new Microsoft.WindowsAPICodePack.DirectX.Controls.DirectControl();
            this.SuspendLayout();
            // 
            // directControl
            // 
            this.directControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.directControl.Location = new System.Drawing.Point(0, 0);
            this.directControl.Name = "directControl";
            this.directControl.Size = new System.Drawing.Size(624, 442);
            this.directControl.TabIndex = 4;
            this.directControl.SizeChanged += new System.EventHandler(this.directControl_SizeChanged);
            // 
            // TutorialWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(624, 442);
            this.Controls.Add(this.directControl);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "TutorialWindow";
            this.Text = "Direct3D 10 Tutorial 4: 3D Spaces";
            this.Load += new System.EventHandler(this.TutorialWindow_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.TutorialWindow_FormClosing);
            this.ResumeLayout(false);

        }

        #endregion

        private Microsoft.WindowsAPICodePack.DirectX.Controls.DirectControl directControl;
    }
}

