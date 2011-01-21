using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.ShellExtensions;

namespace HandlerSamples
{
    public class PreviewHandlerWinformsDemoControl : UserControl
    {
        public PreviewHandlerWinformsDemoControl()
        {
            InitializeComponent();

        }

        public void Populate(XyzFileDefinition definition)
        {
            lblName.Text = definition.Properties.Name;
            txtContent.Text = definition.Content;

            using (MemoryStream stream = new MemoryStream(Convert.FromBase64String(definition.EncodedImage)))
            {
                imageEncoded.Image = Image.FromStream(stream);
            }
        }

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
            this.txtContent = new System.Windows.Forms.TextBox();
            this.lblName = new System.Windows.Forms.Label();
            this.imageEncoded = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.imageEncoded)).BeginInit();
            this.SuspendLayout();
            // 
            // txtContent
            // 
            this.txtContent.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.txtContent.Location = new System.Drawing.Point(12, 173);
            this.txtContent.Multiline = true;
            this.txtContent.Name = "txtContent";
            this.txtContent.ReadOnly = true;
            this.txtContent.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtContent.Size = new System.Drawing.Size(269, 157);
            this.txtContent.TabIndex = 0;
            // 
            // lblName
            // 
            this.lblName.AutoSize = true;
            this.lblName.Location = new System.Drawing.Point(9, 9);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(49, 13);
            this.lblName.TabIndex = 1;
            this.lblName.Text = "file name";
            // 
            // imageEncoded
            // 
            this.imageEncoded.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.imageEncoded.Location = new System.Drawing.Point(12, 25);
            this.imageEncoded.Name = "imageEncoded";
            this.imageEncoded.Size = new System.Drawing.Size(269, 142);
            this.imageEncoded.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.imageEncoded.TabIndex = 2;
            this.imageEncoded.TabStop = false;
            // 
            // PreviewHandlerWinformsDemoControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.imageEncoded);
            this.Controls.Add(this.lblName);
            this.Controls.Add(this.txtContent);
            this.Name = "PreviewHandlerWinformsDemoControl";
            this.Size = new System.Drawing.Size(293, 342);
            ((System.ComponentModel.ISupportInitialize)(this.imageEncoded)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtContent;
        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.PictureBox imageEncoded;
    }


    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("HandlerSamples.XYZPreviewerWinforms")]
    [Guid("05BAC26E-94A8-4441-BA69-9894FE6BBFC4")]
    [PreviewHandler("PreviewHandlerWinformsDemo", ".xyz2", "{5F877EE5-2317-4131-B8D5-6FF965881295}")]
    public class WinformsPreviewHandlerDemo : WinFormsPreviewHandler, IPreviewFromShellObject, IPreviewFromStream
    {
        public WinformsPreviewHandlerDemo()
        {
            Control = new PreviewHandlerWinformsDemoControl();
        }

        #region IPreviewFromFile Members

        public void Load(FileInfo info)
        {
            using (FileStream stream = new FileStream(info.FullName, FileMode.Open, FileAccess.Read))
            {
                Load(stream);
            }
        }

        #endregion

        #region IPreviewFromStream Members

        public void Load(Stream stream)
        {
            XyzFileDefinition definition = new XyzFileDefinition(stream);
            ((PreviewHandlerWinformsDemoControl)Control).Populate(definition);
        }

        #endregion

        #region IPreviewFromShellObject Members

        public void Load(Microsoft.WindowsAPICodePack.Shell.ShellObject shellObject)
        {
            Load(new FileInfo(shellObject.ParsingName));
        }

        #endregion
    }


}
