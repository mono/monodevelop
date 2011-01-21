namespace D2DPaint
{
    partial class Paint2DForm
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
            System.Windows.Forms.ToolStripLabel toolStripLabel1;
            System.Windows.Forms.ToolStripLabel toolStripLabel2;
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Paint2DForm));
            this.renderControl = new Microsoft.WindowsAPICodePack.DirectX.Controls.RenderControl();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.arrowButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
            this.strokeWidths = new System.Windows.Forms.ToolStripComboBox();
            this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
            this.fillButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.lineButton = new System.Windows.Forms.ToolStripButton();
            this.rectButton = new System.Windows.Forms.ToolStripButton();
            this.roundrectButton = new System.Windows.Forms.ToolStripButton();
            this.ellipseButton = new System.Windows.Forms.ToolStripButton();
            this.geometryButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.textButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.bitmapButton = new System.Windows.Forms.ToolStripButton();
            this.traparencyList = new System.Windows.Forms.ToolStripComboBox();
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.brushButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.clearButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
            this.saveButton = new System.Windows.Forms.ToolStripButton();
            toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStripLabel1
            // 
            toolStripLabel1.Name = "toolStripLabel1";
            toolStripLabel1.Size = new System.Drawing.Size(43, 22);
            toolStripLabel1.Text = "Stroke:";
            // 
            // toolStripLabel2
            // 
            toolStripLabel2.Name = "toolStripLabel2";
            toolStripLabel2.Size = new System.Drawing.Size(81, 22);
            toolStripLabel2.Text = "Transparency:";
            // 
            // renderControl
            // 
            this.renderControl.AutoSize = true;
            this.renderControl.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.renderControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.renderControl.Location = new System.Drawing.Point(0, 0);
            this.renderControl.Name = "renderControl";
            this.renderControl.Size = new System.Drawing.Size(869, 605);
            this.renderControl.TabIndex = 0;
            this.renderControl.Load += new System.EventHandler(this.renderControl_Load);
            this.renderControl.MouseMove += new System.Windows.Forms.MouseEventHandler(this.renderControl_MouseMove);
            this.renderControl.MouseDown += new System.Windows.Forms.MouseEventHandler(this.renderControl_MouseDown);
            this.renderControl.MouseUp += new System.Windows.Forms.MouseEventHandler(this.renderControl_MouseUp);
            this.renderControl.SizeChanged += new System.EventHandler(this.renderControl_SizeChanged);
            this.renderControl.MouseEnter += new System.EventHandler(this.renderControl_MouseEnter);
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.arrowButton,
            this.toolStripSeparator7,
            toolStripLabel1,
            this.strokeWidths,
            this.toolStripSeparator6,
            this.fillButton,
            this.toolStripSeparator2,
            this.lineButton,
            this.rectButton,
            this.roundrectButton,
            this.ellipseButton,
            this.geometryButton,
            this.toolStripSeparator4,
            this.textButton,
            this.toolStripSeparator3,
            this.bitmapButton,
            toolStripLabel2,
            this.traparencyList,
            this.toolStripSeparator5,
            this.brushButton,
            this.toolStripSeparator1,
            this.clearButton,
            this.toolStripSeparator8,
            this.saveButton});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(869, 25);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            this.toolStrip1.MouseEnter += new System.EventHandler(this.toolStrip1_MouseEnter);
            // 
            // arrowButton
            // 
            this.arrowButton.Checked = true;
            this.arrowButton.CheckState = System.Windows.Forms.CheckState.Checked;
            this.arrowButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.arrowButton.Image = ((System.Drawing.Image)(resources.GetObject("arrowButton.Image")));
            this.arrowButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.arrowButton.Name = "arrowButton";
            this.arrowButton.Size = new System.Drawing.Size(23, 22);
            this.arrowButton.Click += new System.EventHandler(this.toolStripButton1_Click);
            // 
            // toolStripSeparator7
            // 
            this.toolStripSeparator7.Name = "toolStripSeparator7";
            this.toolStripSeparator7.Size = new System.Drawing.Size(6, 25);
            // 
            // strokeWidths
            // 
            this.strokeWidths.AutoSize = false;
            this.strokeWidths.DropDownHeight = 110;
            this.strokeWidths.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.strokeWidths.DropDownWidth = 50;
            this.strokeWidths.IntegralHeight = false;
            this.strokeWidths.Items.AddRange(new object[] {
            "1",
            "2",
            "4",
            "6",
            "8",
            "10",
            "12",
            "16",
            "24",
            "36",
            "42"});
            this.strokeWidths.Name = "strokeWidths";
            this.strokeWidths.Size = new System.Drawing.Size(42, 23);
            this.strokeWidths.SelectedIndexChanged += new System.EventHandler(this.strokeWidths_SelectedIndexChanged);
            // 
            // toolStripSeparator6
            // 
            this.toolStripSeparator6.Name = "toolStripSeparator6";
            this.toolStripSeparator6.Size = new System.Drawing.Size(6, 25);
            // 
            // fillButton
            // 
            this.fillButton.CheckOnClick = true;
            this.fillButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.fillButton.Image = ((System.Drawing.Image)(resources.GetObject("fillButton.Image")));
            this.fillButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.fillButton.Name = "fillButton";
            this.fillButton.Size = new System.Drawing.Size(39, 22);
            this.fillButton.Text = "Filled";
            this.fillButton.Click += new System.EventHandler(this.fillButton_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // lineButton
            // 
            this.lineButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.lineButton.Image = ((System.Drawing.Image)(resources.GetObject("lineButton.Image")));
            this.lineButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.lineButton.Name = "lineButton";
            this.lineButton.Size = new System.Drawing.Size(33, 22);
            this.lineButton.Text = "Line";
            this.lineButton.Click += new System.EventHandler(this.lineButton_Click);
            // 
            // rectButton
            // 
            this.rectButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.rectButton.Image = ((System.Drawing.Image)(resources.GetObject("rectButton.Image")));
            this.rectButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.rectButton.Name = "rectButton";
            this.rectButton.Size = new System.Drawing.Size(34, 22);
            this.rectButton.Text = "Rect";
            this.rectButton.Click += new System.EventHandler(this.rectButton_Click);
            // 
            // roundrectButton
            // 
            this.roundrectButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.roundrectButton.Image = ((System.Drawing.Image)(resources.GetObject("roundrectButton.Image")));
            this.roundrectButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.roundrectButton.Name = "roundrectButton";
            this.roundrectButton.Size = new System.Drawing.Size(69, 22);
            this.roundrectButton.Text = "RoundRect";
            this.roundrectButton.Click += new System.EventHandler(this.roundrectButton_Click);
            // 
            // ellipseButton
            // 
            this.ellipseButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.ellipseButton.Image = ((System.Drawing.Image)(resources.GetObject("ellipseButton.Image")));
            this.ellipseButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ellipseButton.Name = "ellipseButton";
            this.ellipseButton.Size = new System.Drawing.Size(44, 22);
            this.ellipseButton.Text = "Ellipse";
            this.ellipseButton.Click += new System.EventHandler(this.ellipseButton_Click);
            // 
            // geometryButton
            // 
            this.geometryButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.geometryButton.Image = ((System.Drawing.Image)(resources.GetObject("geometryButton.Image")));
            this.geometryButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.geometryButton.Name = "geometryButton";
            this.geometryButton.Size = new System.Drawing.Size(63, 22);
            this.geometryButton.Text = "Geometry";
            this.geometryButton.Click += new System.EventHandler(this.geometryButton_Click);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(6, 25);
            // 
            // textButton
            // 
            this.textButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.textButton.Image = ((System.Drawing.Image)(resources.GetObject("textButton.Image")));
            this.textButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.textButton.Name = "textButton";
            this.textButton.Size = new System.Drawing.Size(33, 22);
            this.textButton.Text = "Text";
            this.textButton.Click += new System.EventHandler(this.textButton_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
            // 
            // bitmapButton
            // 
            this.bitmapButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.bitmapButton.Image = ((System.Drawing.Image)(resources.GetObject("bitmapButton.Image")));
            this.bitmapButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.bitmapButton.Name = "bitmapButton";
            this.bitmapButton.Size = new System.Drawing.Size(49, 22);
            this.bitmapButton.Text = "Bitmap";
            this.bitmapButton.Click += new System.EventHandler(this.bitmapButton_Click);
            // 
            // traparencyList
            // 
            this.traparencyList.AutoSize = false;
            this.traparencyList.Items.AddRange(new object[] {
            "1.0",
            "0.9",
            "0.75",
            "0.5",
            "0.25",
            "0.2",
            "0.1"});
            this.traparencyList.Name = "traparencyList";
            this.traparencyList.Size = new System.Drawing.Size(42, 23);
            this.traparencyList.SelectedIndexChanged += new System.EventHandler(this.traparencyList_SelectedIndexChanged);
            // 
            // toolStripSeparator5
            // 
            this.toolStripSeparator5.Name = "toolStripSeparator5";
            this.toolStripSeparator5.Size = new System.Drawing.Size(6, 25);
            // 
            // brushButton
            // 
            this.brushButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.brushButton.Image = ((System.Drawing.Image)(resources.GetObject("brushButton.Image")));
            this.brushButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.brushButton.Name = "brushButton";
            this.brushButton.Size = new System.Drawing.Size(52, 22);
            this.brushButton.Text = "Brushes";
            this.brushButton.Click += new System.EventHandler(this.brushButton_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // clearButton
            // 
            this.clearButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.clearButton.Image = ((System.Drawing.Image)(resources.GetObject("clearButton.Image")));
            this.clearButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.clearButton.Name = "clearButton";
            this.clearButton.Size = new System.Drawing.Size(38, 22);
            this.clearButton.Text = "Clear";
            this.clearButton.ToolTipText = "Clear All";
            this.clearButton.Click += new System.EventHandler(this.clearButton_Click);
            // 
            // toolStripSeparator8
            // 
            this.toolStripSeparator8.Name = "toolStripSeparator8";
            this.toolStripSeparator8.Size = new System.Drawing.Size(6, 25);
            // 
            // saveButton
            // 
            this.saveButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.saveButton.Image = ((System.Drawing.Image)(resources.GetObject("saveButton.Image")));
            this.saveButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(44, 22);
            this.saveButton.Text = "Save...";
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            // 
            // Paint2DForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(869, 605);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.renderControl);
            this.Name = "Paint2DForm";
            this.Text = "D2D Paint Demo";
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Microsoft.WindowsAPICodePack.DirectX.Controls.RenderControl renderControl;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton brushButton;
        private System.Windows.Forms.ToolStripButton lineButton;
        private System.Windows.Forms.ToolStripButton bitmapButton;
        private System.Windows.Forms.ToolStripButton roundrectButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripComboBox strokeWidths;
        private System.Windows.Forms.ToolStripComboBox traparencyList;
        private System.Windows.Forms.ToolStripButton clearButton;
        private System.Windows.Forms.ToolStripButton rectButton;
        private System.Windows.Forms.ToolStripButton ellipseButton;
        private System.Windows.Forms.ToolStripButton textButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripButton fillButton;
        private System.Windows.Forms.ToolStripButton geometryButton;
        private System.Windows.Forms.ToolStripButton arrowButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
        private System.Windows.Forms.ToolStripButton saveButton;
    }
}

