namespace D2DShapes
{
    partial class D2DShapesControlWithButtons
    {

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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(D2DShapesControlWithButtons));
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.comboBoxRenderMode = new System.Windows.Forms.ComboBox();
            this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
            this.buttonAddLines = new System.Windows.Forms.Button();
            this.buttonAddRectangles = new System.Windows.Forms.Button();
            this.buttonUnpeel = new System.Windows.Forms.Button();
            this.buttonAddRoundRects = new System.Windows.Forms.Button();
            this.buttonPeelShape = new System.Windows.Forms.Button();
            this.buttonAddEllipses = new System.Windows.Forms.Button();
            this.buttonClear = new System.Windows.Forms.Button();
            this.buttonAddTexts = new System.Windows.Forms.Button();
            this.buttonAddLayer = new System.Windows.Forms.Button();
            this.buttonAddBitmaps = new System.Windows.Forms.Button();
            this.buttonAddGDI = new System.Windows.Forms.Button();
            this.buttonAddGeometries = new System.Windows.Forms.Button();
            this.buttonAddMeshes = new System.Windows.Forms.Button();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.textBoxStats = new System.Windows.Forms.TextBox();
            this.tabPageShapes = new System.Windows.Forms.TabPage();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.tabControl2 = new System.Windows.Forms.TabControl();
            this.tabPageShapesAtPoint = new System.Windows.Forms.TabPage();
            this.treeViewShapesAtPoint = new System.Windows.Forms.TreeView();
            this.tabPageAllShapes = new System.Windows.Forms.TabPage();
            this.treeViewAllShapes = new System.Windows.Forms.TreeView();
            this.propertyGridShapeInfo = new System.Windows.Forms.PropertyGrid();
            this.labelFPS = new System.Windows.Forms.Label();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.d2dShapesControl = new D2DShapes.D2DShapesControl(this.components);
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
            this.tabPage2.SuspendLayout();
            this.tabPageShapes.SuspendLayout();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.tabControl2.SuspendLayout();
            this.tabPageShapesAtPoint.SuspendLayout();
            this.tabPageAllShapes.SuspendLayout();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPageShapes);
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(175, 444);
            this.tabControl1.TabIndex = 4;
            // 
            // tabPage1
            // 
            this.tabPage1.BackColor = System.Drawing.SystemColors.Window;
            this.tabPage1.Controls.Add(this.numericUpDown1);
            this.tabPage1.Controls.Add(this.buttonAddLines);
            this.tabPage1.Controls.Add(this.buttonAddRectangles);
            this.tabPage1.Controls.Add(this.buttonAddRoundRects);
            this.tabPage1.Controls.Add(this.buttonAddEllipses);
            this.tabPage1.Controls.Add(this.buttonAddTexts);
            this.tabPage1.Controls.Add(this.buttonAddLayer);
            this.tabPage1.Controls.Add(this.buttonAddBitmaps);
            this.tabPage1.Controls.Add(this.buttonAddGDI);
            this.tabPage1.Controls.Add(this.buttonAddGeometries);
            this.tabPage1.Controls.Add(this.buttonAddMeshes);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(167, 418);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Edit";
            // 
            // comboBoxRenderMode
            // 
            this.comboBoxRenderMode.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxRenderMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxRenderMode.FormattingEnabled = true;
            this.comboBoxRenderMode.Items.AddRange(new object[] {
            "DCRenderTarget",
            "BitmapRenderTarget OnPaint",
            "BitmapRenderTarget Real Time",
            "HwndRenderTarget"});
            this.comboBoxRenderMode.Location = new System.Drawing.Point(7, 508);
            this.comboBoxRenderMode.Name = "comboBoxRenderMode";
            this.comboBoxRenderMode.Size = new System.Drawing.Size(164, 21);
            this.comboBoxRenderMode.TabIndex = 4;
            this.toolTip1.SetToolTip(this.comboBoxRenderMode, resources.GetString("comboBoxRenderMode.ToolTip"));
            this.comboBoxRenderMode.SelectedIndexChanged += new System.EventHandler(this.comboBoxRenderMode_SelectedIndexChanged);
            // 
            // numericUpDown1
            // 
            this.numericUpDown1.Location = new System.Drawing.Point(3, 6);
            this.numericUpDown1.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numericUpDown1.Name = "numericUpDown1";
            this.numericUpDown1.Size = new System.Drawing.Size(164, 20);
            this.numericUpDown1.TabIndex = 3;
            this.toolTip1.SetToolTip(this.numericUpDown1, "Number of shapes to add at a time.");
            this.numericUpDown1.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // buttonAddLines
            // 
            this.buttonAddLines.Location = new System.Drawing.Point(3, 32);
            this.buttonAddLines.Name = "buttonAddLines";
            this.buttonAddLines.Size = new System.Drawing.Size(164, 23);
            this.buttonAddLines.TabIndex = 0;
            this.buttonAddLines.Text = "Add Lines";
            this.buttonAddLines.UseVisualStyleBackColor = true;
            this.buttonAddLines.Click += new System.EventHandler(this.buttonAddLines_Click);
            // 
            // buttonAddRectangles
            // 
            this.buttonAddRectangles.Location = new System.Drawing.Point(3, 61);
            this.buttonAddRectangles.Name = "buttonAddRectangles";
            this.buttonAddRectangles.Size = new System.Drawing.Size(164, 23);
            this.buttonAddRectangles.TabIndex = 0;
            this.buttonAddRectangles.Text = "Add Rectangles";
            this.buttonAddRectangles.UseVisualStyleBackColor = true;
            this.buttonAddRectangles.Click += new System.EventHandler(this.buttonAddRectangles_Click);
            // 
            // buttonUnpeel
            // 
            this.buttonUnpeel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonUnpeel.Location = new System.Drawing.Point(89, 450);
            this.buttonUnpeel.Name = "buttonUnpeel";
            this.buttonUnpeel.Size = new System.Drawing.Size(82, 23);
            this.buttonUnpeel.TabIndex = 0;
            this.buttonUnpeel.Text = "Unpeel";
            this.toolTip1.SetToolTip(this.buttonUnpeel, "Takes a shape from the top of the stack of \"peelings\" and puts it back onto the c" +
                    "anvas");
            this.buttonUnpeel.UseVisualStyleBackColor = true;
            this.buttonUnpeel.Click += new System.EventHandler(this.buttonUnpeel_Click);
            // 
            // buttonAddRoundRects
            // 
            this.buttonAddRoundRects.Location = new System.Drawing.Point(3, 90);
            this.buttonAddRoundRects.Name = "buttonAddRoundRects";
            this.buttonAddRoundRects.Size = new System.Drawing.Size(164, 23);
            this.buttonAddRoundRects.TabIndex = 0;
            this.buttonAddRoundRects.Text = "Add Rounded Rectangles";
            this.buttonAddRoundRects.UseVisualStyleBackColor = true;
            this.buttonAddRoundRects.Click += new System.EventHandler(this.buttonAddRoundRects_Click);
            // 
            // buttonPeelShape
            // 
            this.buttonPeelShape.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonPeelShape.Location = new System.Drawing.Point(7, 450);
            this.buttonPeelShape.Name = "buttonPeelShape";
            this.buttonPeelShape.Size = new System.Drawing.Size(76, 23);
            this.buttonPeelShape.TabIndex = 0;
            this.buttonPeelShape.Text = "Peel Shape";
            this.toolTip1.SetToolTip(this.buttonPeelShape, "Removes the top shape from the canvas and puts it at the top of a stack of \"peeli" +
                    "ngs\"");
            this.buttonPeelShape.UseVisualStyleBackColor = true;
            this.buttonPeelShape.Click += new System.EventHandler(this.buttonPeelShape_Click);
            // 
            // buttonAddEllipses
            // 
            this.buttonAddEllipses.Location = new System.Drawing.Point(3, 119);
            this.buttonAddEllipses.Name = "buttonAddEllipses";
            this.buttonAddEllipses.Size = new System.Drawing.Size(164, 23);
            this.buttonAddEllipses.TabIndex = 0;
            this.buttonAddEllipses.Text = "Add Ellipses";
            this.buttonAddEllipses.UseVisualStyleBackColor = true;
            this.buttonAddEllipses.Click += new System.EventHandler(this.buttonAddEllipses_Click);
            // 
            // buttonClear
            // 
            this.buttonClear.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonClear.Location = new System.Drawing.Point(7, 479);
            this.buttonClear.Name = "buttonClear";
            this.buttonClear.Size = new System.Drawing.Size(164, 23);
            this.buttonClear.TabIndex = 0;
            this.buttonClear.Text = "Clear Shapes";
            this.toolTip1.SetToolTip(this.buttonClear, "Clears all shapes from the canvas (peelings remain).");
            this.buttonClear.UseVisualStyleBackColor = true;
            this.buttonClear.Click += new System.EventHandler(this.buttonClear_Click);
            // 
            // buttonAddTexts
            // 
            this.buttonAddTexts.Location = new System.Drawing.Point(3, 148);
            this.buttonAddTexts.Name = "buttonAddTexts";
            this.buttonAddTexts.Size = new System.Drawing.Size(164, 23);
            this.buttonAddTexts.TabIndex = 0;
            this.buttonAddTexts.Text = "Add Texts";
            this.buttonAddTexts.UseVisualStyleBackColor = true;
            this.buttonAddTexts.Click += new System.EventHandler(this.buttonAddTexts_Click);
            // 
            // buttonAddLayer
            // 
            this.buttonAddLayer.Location = new System.Drawing.Point(3, 293);
            this.buttonAddLayer.Name = "buttonAddLayer";
            this.buttonAddLayer.Size = new System.Drawing.Size(164, 23);
            this.buttonAddLayer.TabIndex = 0;
            this.buttonAddLayer.Text = "Add Layer";
            this.toolTip1.SetToolTip(this.buttonAddLayer, "A layer provides masking and grouping capability.");
            this.buttonAddLayer.UseVisualStyleBackColor = true;
            this.buttonAddLayer.Click += new System.EventHandler(this.buttonAddLayer_Click);
            // 
            // buttonAddBitmaps
            // 
            this.buttonAddBitmaps.Location = new System.Drawing.Point(3, 177);
            this.buttonAddBitmaps.Name = "buttonAddBitmaps";
            this.buttonAddBitmaps.Size = new System.Drawing.Size(164, 23);
            this.buttonAddBitmaps.TabIndex = 0;
            this.buttonAddBitmaps.Text = "Add Bitmaps";
            this.buttonAddBitmaps.UseVisualStyleBackColor = true;
            this.buttonAddBitmaps.Click += new System.EventHandler(this.buttonAddBitmaps_Click);
            // 
            // buttonAddGDI
            // 
            this.buttonAddGDI.Location = new System.Drawing.Point(3, 264);
            this.buttonAddGDI.Name = "buttonAddGDI";
            this.buttonAddGDI.Size = new System.Drawing.Size(164, 23);
            this.buttonAddGDI.TabIndex = 0;
            this.buttonAddGDI.Text = "Add GDI Ellipses";
            this.toolTip1.SetToolTip(this.buttonAddGDI, "Ellipses drawn using GDI+ on a Direct2D render target.");
            this.buttonAddGDI.UseVisualStyleBackColor = true;
            this.buttonAddGDI.Click += new System.EventHandler(this.buttonAddGDI_Click);
            // 
            // buttonAddGeometries
            // 
            this.buttonAddGeometries.Location = new System.Drawing.Point(3, 206);
            this.buttonAddGeometries.Name = "buttonAddGeometries";
            this.buttonAddGeometries.Size = new System.Drawing.Size(164, 23);
            this.buttonAddGeometries.TabIndex = 0;
            this.buttonAddGeometries.Text = "Add Geometries";
            this.buttonAddGeometries.UseVisualStyleBackColor = true;
            this.buttonAddGeometries.Click += new System.EventHandler(this.buttonAddGeometries_Click);
            // 
            // buttonAddMeshes
            // 
            this.buttonAddMeshes.Location = new System.Drawing.Point(3, 235);
            this.buttonAddMeshes.Name = "buttonAddMeshes";
            this.buttonAddMeshes.Size = new System.Drawing.Size(164, 23);
            this.buttonAddMeshes.TabIndex = 0;
            this.buttonAddMeshes.Text = "Add Meshes";
            this.toolTip1.SetToolTip(this.buttonAddMeshes, "Meshes consist of triangles. Here - they are either tesselated from random geomet" +
                    "ries or assembled from random triangles.");
            this.buttonAddMeshes.UseVisualStyleBackColor = true;
            this.buttonAddMeshes.Click += new System.EventHandler(this.buttonAddMeshes_Click);
            // 
            // tabPage2
            // 
            this.tabPage2.BackColor = System.Drawing.SystemColors.Window;
            this.tabPage2.Controls.Add(this.textBoxStats);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(167, 418);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Stats";
            // 
            // textBoxStats
            // 
            this.textBoxStats.AcceptsReturn = true;
            this.textBoxStats.AcceptsTab = true;
            this.textBoxStats.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxStats.Location = new System.Drawing.Point(3, 3);
            this.textBoxStats.Multiline = true;
            this.textBoxStats.Name = "textBoxStats";
            this.textBoxStats.ReadOnly = true;
            this.textBoxStats.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxStats.Size = new System.Drawing.Size(161, 412);
            this.textBoxStats.TabIndex = 3;
            this.textBoxStats.Text = "Stats:";
            this.textBoxStats.WordWrap = false;
            // 
            // tabPageShapes
            // 
            this.tabPageShapes.Controls.Add(this.splitContainer2);
            this.tabPageShapes.Location = new System.Drawing.Point(4, 22);
            this.tabPageShapes.Name = "tabPageShapes";
            this.tabPageShapes.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageShapes.Size = new System.Drawing.Size(167, 418);
            this.tabPageShapes.TabIndex = 2;
            this.tabPageShapes.Text = "Shapes";
            this.tabPageShapes.UseVisualStyleBackColor = true;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(3, 3);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.tabControl2);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.propertyGridShapeInfo);
            this.splitContainer2.Size = new System.Drawing.Size(161, 412);
            this.splitContainer2.SplitterDistance = 205;
            this.splitContainer2.SplitterWidth = 6;
            this.splitContainer2.TabIndex = 4;
            // 
            // tabControl2
            // 
            this.tabControl2.Controls.Add(this.tabPageShapesAtPoint);
            this.tabControl2.Controls.Add(this.tabPageAllShapes);
            this.tabControl2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl2.Location = new System.Drawing.Point(0, 0);
            this.tabControl2.Name = "tabControl2";
            this.tabControl2.SelectedIndex = 0;
            this.tabControl2.Size = new System.Drawing.Size(161, 205);
            this.tabControl2.TabIndex = 3;
            // 
            // tabPageShapesAtPoint
            // 
            this.tabPageShapesAtPoint.BackColor = System.Drawing.SystemColors.Window;
            this.tabPageShapesAtPoint.Controls.Add(this.treeViewShapesAtPoint);
            this.tabPageShapesAtPoint.Location = new System.Drawing.Point(4, 22);
            this.tabPageShapesAtPoint.Name = "tabPageShapesAtPoint";
            this.tabPageShapesAtPoint.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageShapesAtPoint.Size = new System.Drawing.Size(153, 179);
            this.tabPageShapesAtPoint.TabIndex = 0;
            this.tabPageShapesAtPoint.Text = "Shapes Clicked";
            // 
            // treeViewShapesAtPoint
            // 
            this.treeViewShapesAtPoint.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeViewShapesAtPoint.Location = new System.Drawing.Point(3, 3);
            this.treeViewShapesAtPoint.Name = "treeViewShapesAtPoint";
            this.treeViewShapesAtPoint.Size = new System.Drawing.Size(147, 173);
            this.treeViewShapesAtPoint.TabIndex = 2;
            this.toolTip1.SetToolTip(this.treeViewShapesAtPoint, "List of shapes and hierarchies of shapes (for layers) at point clicked. Select to" +
                    " view shape properties. Right click to peel specific shape.");
            this.treeViewShapesAtPoint.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeViewShapes_AfterSelect);
            this.treeViewShapesAtPoint.MouseDown += new System.Windows.Forms.MouseEventHandler(this.treeViewShapes_MouseDown);
            // 
            // tabPageAllShapes
            // 
            this.tabPageAllShapes.Controls.Add(this.treeViewAllShapes);
            this.tabPageAllShapes.Location = new System.Drawing.Point(4, 22);
            this.tabPageAllShapes.Name = "tabPageAllShapes";
            this.tabPageAllShapes.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageAllShapes.Size = new System.Drawing.Size(153, 179);
            this.tabPageAllShapes.TabIndex = 1;
            this.tabPageAllShapes.Text = "All Shapes";
            this.tabPageAllShapes.UseVisualStyleBackColor = true;
            // 
            // treeViewAllShapes
            // 
            this.treeViewAllShapes.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeViewAllShapes.Location = new System.Drawing.Point(3, 3);
            this.treeViewAllShapes.Name = "treeViewAllShapes";
            this.treeViewAllShapes.Size = new System.Drawing.Size(147, 173);
            this.treeViewAllShapes.TabIndex = 3;
            this.toolTip1.SetToolTip(this.treeViewAllShapes, "List of all shapes and hierarchies of shapes (for layers). Select to view shape p" +
                    "roperties. Right click to peel specific shape.");
            this.treeViewAllShapes.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeViewShapes_AfterSelect);
            this.treeViewAllShapes.MouseDown += new System.Windows.Forms.MouseEventHandler(this.treeViewShapes_MouseDown);
            // 
            // propertyGridShapeInfo
            // 
            this.propertyGridShapeInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propertyGridShapeInfo.Location = new System.Drawing.Point(0, 0);
            this.propertyGridShapeInfo.Name = "propertyGridShapeInfo";
            this.propertyGridShapeInfo.Size = new System.Drawing.Size(161, 201);
            this.propertyGridShapeInfo.TabIndex = 0;
            this.propertyGridShapeInfo.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.propertyGridShapeInfo_PropertyValueChanged);
            // 
            // labelFPS
            // 
            this.labelFPS.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelFPS.AutoSize = true;
            this.labelFPS.Location = new System.Drawing.Point(3, 516);
            this.labelFPS.Name = "labelFPS";
            this.labelFPS.Size = new System.Drawing.Size(27, 13);
            this.labelFPS.TabIndex = 1;
            this.labelFPS.Text = "FPS";
            this.toolTip1.SetToolTip(this.labelFPS, "Frames Per Second - only shows for real time render mode (HwndRenderTarget)");
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
            this.splitContainer1.Panel1.Controls.Add(this.labelFPS);
            this.splitContainer1.Panel1.Controls.Add(this.d2dShapesControl);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.BackColor = System.Drawing.SystemColors.Window;
            this.splitContainer1.Panel2.Controls.Add(this.comboBoxRenderMode);
            this.splitContainer1.Panel2.Controls.Add(this.tabControl1);
            this.splitContainer1.Panel2.Controls.Add(this.buttonPeelShape);
            this.splitContainer1.Panel2.Controls.Add(this.buttonClear);
            this.splitContainer1.Panel2.Controls.Add(this.buttonUnpeel);
            this.splitContainer1.Size = new System.Drawing.Size(670, 533);
            this.splitContainer1.SplitterDistance = 487;
            this.splitContainer1.SplitterWidth = 6;
            this.splitContainer1.TabIndex = 5;
            // 
            // d2dShapesControl
            // 
            this.d2dShapesControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.d2dShapesControl.Location = new System.Drawing.Point(0, 0);
            this.d2dShapesControl.Name = "d2dShapesControl";
            this.d2dShapesControl.Render = null;
            this.d2dShapesControl.RenderMode = D2DShapes.D2DShapesControl.RenderModes.BitmapRenderTargetOnPaint;
            this.d2dShapesControl.Size = new System.Drawing.Size(487, 533);
            this.d2dShapesControl.TabIndex = 0;
            this.toolTip1.SetToolTip(this.d2dShapesControl, "Left click to view details of the shape. Right click to peel shape.");
            this.d2dShapesControl.StatsChanged += new System.EventHandler(this.d2dShapesControl_StatsChanged);
            this.d2dShapesControl.MouseUp += new System.Windows.Forms.MouseEventHandler(this.d2dShapesControl_MouseUp);
            this.d2dShapesControl.FpsChanged += new System.EventHandler(this.d2dShapesControl_FpsChanged);
            // 
            // D2DShapesControlWithButtons
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Name = "D2DShapesControlWithButtons";
            this.Size = new System.Drawing.Size(670, 533);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.tabPageShapes.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            this.splitContainer2.ResumeLayout(false);
            this.tabControl2.ResumeLayout(false);
            this.tabPageShapesAtPoint.ResumeLayout(false);
            this.tabPageAllShapes.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        #endregion

        private D2DShapesControl d2dShapesControl;
        private System.Windows.Forms.Button buttonAddBitmaps;
        private System.Windows.Forms.Button buttonAddTexts;
        private System.Windows.Forms.Button buttonAddEllipses;
        private System.Windows.Forms.Button buttonAddRoundRects;
        private System.Windows.Forms.Button buttonAddRectangles;
        private System.Windows.Forms.Button buttonAddLines;
        private System.Windows.Forms.Label labelFPS;
        private System.Windows.Forms.Button buttonClear;
        private System.Windows.Forms.Button buttonAddGeometries;
        private System.Windows.Forms.Button buttonPeelShape;
        private System.Windows.Forms.Button buttonAddMeshes;
        private System.Windows.Forms.NumericUpDown numericUpDown1;
        private System.Windows.Forms.Button buttonAddLayer;
        private System.Windows.Forms.Button buttonAddGDI;
        private System.Windows.Forms.Button buttonUnpeel;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TextBox textBoxStats;
        private System.Windows.Forms.TabPage tabPageShapes;
        private System.Windows.Forms.ComboBox comboBoxRenderMode;
        private System.Windows.Forms.PropertyGrid propertyGridShapeInfo;
        private System.Windows.Forms.TreeView treeViewShapesAtPoint;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.TabControl tabControl2;
        private System.Windows.Forms.TabPage tabPageShapesAtPoint;
        private System.Windows.Forms.TabPage tabPageAllShapes;
        private System.Windows.Forms.TreeView treeViewAllShapes;
        private System.Windows.Forms.Timer timer1;
        private System.ComponentModel.IContainer components;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}
