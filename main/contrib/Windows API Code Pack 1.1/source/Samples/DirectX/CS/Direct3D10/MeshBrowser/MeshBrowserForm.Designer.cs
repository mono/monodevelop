namespace MeshBrowser
{
    partial class MeshBrowserForm
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
            this.buttonOpen = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.cbRotate = new System.Windows.Forms.CheckBox();
            this.cbWireframe = new System.Windows.Forms.CheckBox();
            this.buttonScanDXSDK = new System.Windows.Forms.Button();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.listBoxValid = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.listBoxInvalid = new System.Windows.Forms.ListBox();
            this.label2 = new System.Windows.Forms.Label();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.SuspendLayout();
            // 
            // directControl
            // 
            this.directControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.directControl.Location = new System.Drawing.Point(3, 3);
            this.directControl.Name = "directControl";
            this.directControl.Size = new System.Drawing.Size(640, 480);
            this.directControl.TabIndex = 4;
            this.directControl.MouseMove += new System.Windows.Forms.MouseEventHandler(this.directControl_MouseMove);
            this.directControl.MouseDown += new System.Windows.Forms.MouseEventHandler(this.directControl_MouseDown);
            this.directControl.MouseUp += new System.Windows.Forms.MouseEventHandler(this.directControl_MouseUp);
            this.directControl.SizeChanged += new System.EventHandler(this.directControl_SizeChanged);
            // 
            // buttonOpen
            // 
            this.buttonOpen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonOpen.Location = new System.Drawing.Point(12, 489);
            this.buttonOpen.Name = "buttonOpen";
            this.buttonOpen.Size = new System.Drawing.Size(75, 23);
            this.buttonOpen.TabIndex = 5;
            this.buttonOpen.Text = "&Open";
            this.buttonOpen.UseVisualStyleBackColor = true;
            this.buttonOpen.Click += new System.EventHandler(this.buttonOpen_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            this.openFileDialog1.Filter = ".x files|*.x|All files|*.*";
            this.openFileDialog1.RestoreDirectory = true;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.cbRotate);
            this.splitContainer1.Panel1.Controls.Add(this.cbWireframe);
            this.splitContainer1.Panel1.Controls.Add(this.buttonScanDXSDK);
            this.splitContainer1.Panel1.Controls.Add(this.buttonOpen);
            this.splitContainer1.Panel1.Controls.Add(this.directControl);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
            this.splitContainer1.Size = new System.Drawing.Size(838, 522);
            this.splitContainer1.SplitterDistance = 645;
            this.splitContainer1.TabIndex = 6;
            // 
            // cbRotate
            // 
            this.cbRotate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cbRotate.AutoSize = true;
            this.cbRotate.Checked = true;
            this.cbRotate.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbRotate.Location = new System.Drawing.Point(324, 493);
            this.cbRotate.Name = "cbRotate";
            this.cbRotate.Size = new System.Drawing.Size(58, 17);
            this.cbRotate.TabIndex = 7;
            this.cbRotate.Text = "Rotate";
            this.cbRotate.UseVisualStyleBackColor = true;
            this.cbRotate.CheckedChanged += new System.EventHandler(this.cbWireframe_CheckedChanged);
            // 
            // cbWireframe
            // 
            this.cbWireframe.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cbWireframe.AutoSize = true;
            this.cbWireframe.Location = new System.Drawing.Point(244, 493);
            this.cbWireframe.Name = "cbWireframe";
            this.cbWireframe.Size = new System.Drawing.Size(74, 17);
            this.cbWireframe.TabIndex = 7;
            this.cbWireframe.Text = "Wireframe";
            this.cbWireframe.UseVisualStyleBackColor = true;
            this.cbWireframe.CheckedChanged += new System.EventHandler(this.cbWireframe_CheckedChanged);
            // 
            // buttonScanDXSDK
            // 
            this.buttonScanDXSDK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonScanDXSDK.Location = new System.Drawing.Point(93, 489);
            this.buttonScanDXSDK.Name = "buttonScanDXSDK";
            this.buttonScanDXSDK.Size = new System.Drawing.Size(145, 23);
            this.buttonScanDXSDK.TabIndex = 6;
            this.buttonScanDXSDK.Text = "Scan DX SDK Samples";
            this.buttonScanDXSDK.UseVisualStyleBackColor = true;
            this.buttonScanDXSDK.Click += new System.EventHandler(this.buttonScanDXSDK_Click);
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.listBoxValid);
            this.splitContainer2.Panel1.Controls.Add(this.label1);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.listBoxInvalid);
            this.splitContainer2.Panel2.Controls.Add(this.label2);
            this.splitContainer2.Size = new System.Drawing.Size(189, 522);
            this.splitContainer2.SplitterDistance = 357;
            this.splitContainer2.TabIndex = 0;
            // 
            // listBoxValid
            // 
            this.listBoxValid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBoxValid.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.listBoxValid.FormattingEnabled = true;
            this.listBoxValid.HorizontalScrollbar = true;
            this.listBoxValid.IntegralHeight = false;
            this.listBoxValid.Location = new System.Drawing.Point(0, 13);
            this.listBoxValid.Name = "listBoxValid";
            this.listBoxValid.Size = new System.Drawing.Size(189, 344);
            this.listBoxValid.TabIndex = 1;
            this.listBoxValid.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.listBox_DrawItem);
            this.listBoxValid.SelectedIndexChanged += new System.EventHandler(this.listBoxValid_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(51, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Valid files";
            // 
            // listBoxInvalid
            // 
            this.listBoxInvalid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBoxInvalid.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.listBoxInvalid.FormattingEnabled = true;
            this.listBoxInvalid.HorizontalScrollbar = true;
            this.listBoxInvalid.IntegralHeight = false;
            this.listBoxInvalid.Location = new System.Drawing.Point(0, 13);
            this.listBoxInvalid.Name = "listBoxInvalid";
            this.listBoxInvalid.Size = new System.Drawing.Size(189, 148);
            this.listBoxInvalid.TabIndex = 1;
            this.listBoxInvalid.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.listBox_DrawItem);
            this.listBoxInvalid.SelectedIndexChanged += new System.EventHandler(this.listBoxInvalid_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Dock = System.Windows.Forms.DockStyle.Top;
            this.label2.Location = new System.Drawing.Point(0, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(59, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Invalid files";
            // 
            // MeshBrowserForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(838, 522);
            this.Controls.Add(this.splitContainer1);
            this.Name = "MeshBrowserForm";
            this.Text = "Mesh Browser";
            this.Load += new System.EventHandler(this.Window_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel1.PerformLayout();
            this.splitContainer2.Panel2.ResumeLayout(false);
            this.splitContainer2.Panel2.PerformLayout();
            this.splitContainer2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private Microsoft.WindowsAPICodePack.DirectX.Controls.DirectControl directControl;
        private System.Windows.Forms.Button buttonOpen;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.ListBox listBoxValid;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListBox listBoxInvalid;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button buttonScanDXSDK;
        private System.Windows.Forms.CheckBox cbWireframe;
        private System.Windows.Forms.CheckBox cbRotate;
    }
}