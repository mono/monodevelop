using Microsoft.WindowsAPICodePack.Controls.WindowsForms;
namespace Microsoft.WindowsAPICodePack.Samples.TabbedThumbnailDemo
{
    partial class FavoritesWindow
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
            this.explorerBrowser1 = new ExplorerBrowser();
            this.SuspendLayout();
            // 
            // explorerBrowser1
            // 
            this.explorerBrowser1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.explorerBrowser1.Location = new System.Drawing.Point(0, 0);
            this.explorerBrowser1.Name = "explorerBrowser1";
            this.explorerBrowser1.Size = new System.Drawing.Size(215, 378);
            this.explorerBrowser1.TabIndex = 0;
            this.explorerBrowser1.Text = "explorerBrowser1";
            // 
            // FavoritesWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(215, 378);
            this.Controls.Add(this.explorerBrowser1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Name = "FavoritesWindow";
            this.Text = "Favorites";
            this.TopMost = true;
            this.ResumeLayout(false);

        }

        #endregion

        private Microsoft.WindowsAPICodePack.Controls.WindowsForms.ExplorerBrowser explorerBrowser1;
    }
}