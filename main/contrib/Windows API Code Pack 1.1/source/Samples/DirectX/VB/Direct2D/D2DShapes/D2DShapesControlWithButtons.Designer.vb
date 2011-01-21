Imports Microsoft.VisualBasic
Imports System
Namespace D2DShapes
	Partial Public Class D2DShapesControlWithButtons

		''' <summary> 
		''' Clean up any resources being used.
		''' </summary>
		''' <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		Protected Overrides Sub Dispose(ByVal disposing As Boolean)
			If disposing AndAlso (components IsNot Nothing) Then
				components.Dispose()
			End If
			MyBase.Dispose(disposing)
		End Sub

		#Region "Component Designer generated code"

		''' <summary> 
		''' Required method for Designer support - do not modify 
		''' the contents of this method with the code editor.
		''' </summary>
		Private Sub InitializeComponent()
			Me.components = New System.ComponentModel.Container()
			Dim resources As New System.ComponentModel.ComponentResourceManager(GetType(D2DShapesControlWithButtons))
			Me.tabControl1 = New System.Windows.Forms.TabControl()
			Me.tabPage1 = New System.Windows.Forms.TabPage()
			Me.comboBoxRenderMode = New System.Windows.Forms.ComboBox()
			Me.numericUpDown1 = New System.Windows.Forms.NumericUpDown()
			Me.buttonAddLines = New System.Windows.Forms.Button()
			Me.buttonAddRectangles = New System.Windows.Forms.Button()
			Me.buttonUnpeel = New System.Windows.Forms.Button()
			Me.buttonAddRoundRects = New System.Windows.Forms.Button()
			Me.buttonPeelShape = New System.Windows.Forms.Button()
			Me.buttonAddEllipses = New System.Windows.Forms.Button()
			Me.buttonClear = New System.Windows.Forms.Button()
			Me.buttonAddTexts = New System.Windows.Forms.Button()
			Me.buttonAddLayer = New System.Windows.Forms.Button()
			Me.buttonAddBitmaps = New System.Windows.Forms.Button()
			Me.buttonAddGDI = New System.Windows.Forms.Button()
			Me.buttonAddGeometries = New System.Windows.Forms.Button()
			Me.buttonAddMeshes = New System.Windows.Forms.Button()
			Me.tabPage2 = New System.Windows.Forms.TabPage()
			Me.textBoxStats = New System.Windows.Forms.TextBox()
			Me.tabPageShapes = New System.Windows.Forms.TabPage()
			Me.splitContainer2 = New System.Windows.Forms.SplitContainer()
			Me.tabControl2 = New System.Windows.Forms.TabControl()
			Me.tabPageShapesAtPoint = New System.Windows.Forms.TabPage()
			Me.treeViewShapesAtPoint = New System.Windows.Forms.TreeView()
			Me.tabPageAllShapes = New System.Windows.Forms.TabPage()
			Me.treeViewAllShapes = New System.Windows.Forms.TreeView()
			Me.propertyGridShapeInfo = New System.Windows.Forms.PropertyGrid()
			Me.labelFPS = New System.Windows.Forms.Label()
			Me.splitContainer1 = New System.Windows.Forms.SplitContainer()
			Me.timer1 = New System.Windows.Forms.Timer(Me.components)
			Me.toolTip1 = New System.Windows.Forms.ToolTip(Me.components)
			Me.d2dShapesControl = New D2DShapes.D2DShapesControl(Me.components)
			Me.tabControl1.SuspendLayout()
			Me.tabPage1.SuspendLayout()
			CType(Me.numericUpDown1, System.ComponentModel.ISupportInitialize).BeginInit()
			Me.tabPage2.SuspendLayout()
			Me.tabPageShapes.SuspendLayout()
			Me.splitContainer2.Panel1.SuspendLayout()
			Me.splitContainer2.Panel2.SuspendLayout()
			Me.splitContainer2.SuspendLayout()
			Me.tabControl2.SuspendLayout()
			Me.tabPageShapesAtPoint.SuspendLayout()
			Me.tabPageAllShapes.SuspendLayout()
			Me.splitContainer1.Panel1.SuspendLayout()
			Me.splitContainer1.Panel2.SuspendLayout()
			Me.splitContainer1.SuspendLayout()
			Me.SuspendLayout()
			' 
			' tabControl1
			' 
			Me.tabControl1.Anchor = (CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) Or System.Windows.Forms.AnchorStyles.Left) Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles))
			Me.tabControl1.Controls.Add(Me.tabPage1)
			Me.tabControl1.Controls.Add(Me.tabPage2)
			Me.tabControl1.Controls.Add(Me.tabPageShapes)
			Me.tabControl1.Location = New System.Drawing.Point(0, 0)
			Me.tabControl1.Name = "tabControl1"
			Me.tabControl1.SelectedIndex = 0
			Me.tabControl1.Size = New System.Drawing.Size(175, 444)
			Me.tabControl1.TabIndex = 4
			' 
			' tabPage1
			' 
			Me.tabPage1.BackColor = System.Drawing.SystemColors.Window
			Me.tabPage1.Controls.Add(Me.numericUpDown1)
			Me.tabPage1.Controls.Add(Me.buttonAddLines)
			Me.tabPage1.Controls.Add(Me.buttonAddRectangles)
			Me.tabPage1.Controls.Add(Me.buttonAddRoundRects)
			Me.tabPage1.Controls.Add(Me.buttonAddEllipses)
			Me.tabPage1.Controls.Add(Me.buttonAddTexts)
			Me.tabPage1.Controls.Add(Me.buttonAddLayer)
			Me.tabPage1.Controls.Add(Me.buttonAddBitmaps)
			Me.tabPage1.Controls.Add(Me.buttonAddGDI)
			Me.tabPage1.Controls.Add(Me.buttonAddGeometries)
			Me.tabPage1.Controls.Add(Me.buttonAddMeshes)
			Me.tabPage1.Location = New System.Drawing.Point(4, 22)
			Me.tabPage1.Name = "tabPage1"
			Me.tabPage1.Padding = New System.Windows.Forms.Padding(3)
			Me.tabPage1.Size = New System.Drawing.Size(167, 418)
			Me.tabPage1.TabIndex = 0
			Me.tabPage1.Text = "Edit"
			' 
			' comboBoxRenderMode
			' 
			Me.comboBoxRenderMode.Anchor = (CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles))
			Me.comboBoxRenderMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
			Me.comboBoxRenderMode.FormattingEnabled = True
			Me.comboBoxRenderMode.Items.AddRange(New Object() { "DCRenderTarget", "BitmapRenderTarget OnPaint", "BitmapRenderTarget Real Time", "HwndRenderTarget"})
			Me.comboBoxRenderMode.Location = New System.Drawing.Point(7, 508)
			Me.comboBoxRenderMode.Name = "comboBoxRenderMode"
			Me.comboBoxRenderMode.Size = New System.Drawing.Size(164, 21)
			Me.comboBoxRenderMode.TabIndex = 4
			Me.toolTip1.SetToolTip(Me.comboBoxRenderMode, resources.GetString("comboBoxRenderMode.ToolTip"))
'			Me.comboBoxRenderMode.SelectedIndexChanged += New System.EventHandler(Me.comboBoxRenderMode_SelectedIndexChanged)
			' 
			' numericUpDown1
			' 
			Me.numericUpDown1.Location = New System.Drawing.Point(3, 6)
			Me.numericUpDown1.Maximum = New Decimal(New Integer() { 1000, 0, 0, 0})
			Me.numericUpDown1.Name = "numericUpDown1"
			Me.numericUpDown1.Size = New System.Drawing.Size(164, 20)
			Me.numericUpDown1.TabIndex = 3
			Me.toolTip1.SetToolTip(Me.numericUpDown1, "Number of shapes to add at a time.")
			Me.numericUpDown1.Value = New Decimal(New Integer() { 1, 0, 0, 0})
			' 
			' buttonAddLines
			' 
			Me.buttonAddLines.Location = New System.Drawing.Point(3, 32)
			Me.buttonAddLines.Name = "buttonAddLines"
			Me.buttonAddLines.Size = New System.Drawing.Size(164, 23)
			Me.buttonAddLines.TabIndex = 0
			Me.buttonAddLines.Text = "Add Lines"
			Me.buttonAddLines.UseVisualStyleBackColor = True
'			Me.buttonAddLines.Click += New System.EventHandler(Me.buttonAddLines_Click)
			' 
			' buttonAddRectangles
			' 
			Me.buttonAddRectangles.Location = New System.Drawing.Point(3, 61)
			Me.buttonAddRectangles.Name = "buttonAddRectangles"
			Me.buttonAddRectangles.Size = New System.Drawing.Size(164, 23)
			Me.buttonAddRectangles.TabIndex = 0
			Me.buttonAddRectangles.Text = "Add Rectangles"
			Me.buttonAddRectangles.UseVisualStyleBackColor = True
'			Me.buttonAddRectangles.Click += New System.EventHandler(Me.buttonAddRectangles_Click)
			' 
			' buttonUnpeel
			' 
			Me.buttonUnpeel.Anchor = (CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles))
			Me.buttonUnpeel.Location = New System.Drawing.Point(89, 450)
			Me.buttonUnpeel.Name = "buttonUnpeel"
			Me.buttonUnpeel.Size = New System.Drawing.Size(82, 23)
			Me.buttonUnpeel.TabIndex = 0
			Me.buttonUnpeel.Text = "Unpeel"
			Me.toolTip1.SetToolTip(Me.buttonUnpeel, "Takes a shape from the top of the stack of ""peelings"" and puts it back onto the c" & "anvas")
			Me.buttonUnpeel.UseVisualStyleBackColor = True
'			Me.buttonUnpeel.Click += New System.EventHandler(Me.buttonUnpeel_Click)
			' 
			' buttonAddRoundRects
			' 
			Me.buttonAddRoundRects.Location = New System.Drawing.Point(3, 90)
			Me.buttonAddRoundRects.Name = "buttonAddRoundRects"
			Me.buttonAddRoundRects.Size = New System.Drawing.Size(164, 23)
			Me.buttonAddRoundRects.TabIndex = 0
			Me.buttonAddRoundRects.Text = "Add Rounded Rectangles"
			Me.buttonAddRoundRects.UseVisualStyleBackColor = True
'			Me.buttonAddRoundRects.Click += New System.EventHandler(Me.buttonAddRoundRects_Click)
			' 
			' buttonPeelShape
			' 
			Me.buttonPeelShape.Anchor = (CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles))
			Me.buttonPeelShape.Location = New System.Drawing.Point(7, 450)
			Me.buttonPeelShape.Name = "buttonPeelShape"
			Me.buttonPeelShape.Size = New System.Drawing.Size(76, 23)
			Me.buttonPeelShape.TabIndex = 0
			Me.buttonPeelShape.Text = "Peel Shape"
			Me.toolTip1.SetToolTip(Me.buttonPeelShape, "Removes the top shape from the canvas and puts it at the top of a stack of ""peeli" & "ngs""")
			Me.buttonPeelShape.UseVisualStyleBackColor = True
'			Me.buttonPeelShape.Click += New System.EventHandler(Me.buttonPeelShape_Click)
			' 
			' buttonAddEllipses
			' 
			Me.buttonAddEllipses.Location = New System.Drawing.Point(3, 119)
			Me.buttonAddEllipses.Name = "buttonAddEllipses"
			Me.buttonAddEllipses.Size = New System.Drawing.Size(164, 23)
			Me.buttonAddEllipses.TabIndex = 0
			Me.buttonAddEllipses.Text = "Add Ellipses"
			Me.buttonAddEllipses.UseVisualStyleBackColor = True
'			Me.buttonAddEllipses.Click += New System.EventHandler(Me.buttonAddEllipses_Click)
			' 
			' buttonClear
			' 
			Me.buttonClear.Anchor = (CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles))
			Me.buttonClear.Location = New System.Drawing.Point(7, 479)
			Me.buttonClear.Name = "buttonClear"
			Me.buttonClear.Size = New System.Drawing.Size(164, 23)
			Me.buttonClear.TabIndex = 0
			Me.buttonClear.Text = "Clear Shapes"
			Me.toolTip1.SetToolTip(Me.buttonClear, "Clears all shapes from the canvas (peelings remain).")
			Me.buttonClear.UseVisualStyleBackColor = True
'			Me.buttonClear.Click += New System.EventHandler(Me.buttonClear_Click)
			' 
			' buttonAddTexts
			' 
			Me.buttonAddTexts.Location = New System.Drawing.Point(3, 148)
			Me.buttonAddTexts.Name = "buttonAddTexts"
			Me.buttonAddTexts.Size = New System.Drawing.Size(164, 23)
			Me.buttonAddTexts.TabIndex = 0
			Me.buttonAddTexts.Text = "Add Texts"
			Me.buttonAddTexts.UseVisualStyleBackColor = True
'			Me.buttonAddTexts.Click += New System.EventHandler(Me.buttonAddTexts_Click)
			' 
			' buttonAddLayer
			' 
			Me.buttonAddLayer.Location = New System.Drawing.Point(3, 293)
			Me.buttonAddLayer.Name = "buttonAddLayer"
			Me.buttonAddLayer.Size = New System.Drawing.Size(164, 23)
			Me.buttonAddLayer.TabIndex = 0
			Me.buttonAddLayer.Text = "Add Layer"
			Me.toolTip1.SetToolTip(Me.buttonAddLayer, "A layer provides masking and grouping capability.")
			Me.buttonAddLayer.UseVisualStyleBackColor = True
'			Me.buttonAddLayer.Click += New System.EventHandler(Me.buttonAddLayer_Click)
			' 
			' buttonAddBitmaps
			' 
			Me.buttonAddBitmaps.Location = New System.Drawing.Point(3, 177)
			Me.buttonAddBitmaps.Name = "buttonAddBitmaps"
			Me.buttonAddBitmaps.Size = New System.Drawing.Size(164, 23)
			Me.buttonAddBitmaps.TabIndex = 0
			Me.buttonAddBitmaps.Text = "Add Bitmaps"
			Me.buttonAddBitmaps.UseVisualStyleBackColor = True
'			Me.buttonAddBitmaps.Click += New System.EventHandler(Me.buttonAddBitmaps_Click)
			' 
			' buttonAddGDI
			' 
			Me.buttonAddGDI.Location = New System.Drawing.Point(3, 264)
			Me.buttonAddGDI.Name = "buttonAddGDI"
			Me.buttonAddGDI.Size = New System.Drawing.Size(164, 23)
			Me.buttonAddGDI.TabIndex = 0
			Me.buttonAddGDI.Text = "Add GDI Ellipses"
			Me.toolTip1.SetToolTip(Me.buttonAddGDI, "Ellipses drawn using GDI+ on a Direct2D render target.")
			Me.buttonAddGDI.UseVisualStyleBackColor = True
'			Me.buttonAddGDI.Click += New System.EventHandler(Me.buttonAddGDI_Click)
			' 
			' buttonAddGeometries
			' 
			Me.buttonAddGeometries.Location = New System.Drawing.Point(3, 206)
			Me.buttonAddGeometries.Name = "buttonAddGeometries"
			Me.buttonAddGeometries.Size = New System.Drawing.Size(164, 23)
			Me.buttonAddGeometries.TabIndex = 0
			Me.buttonAddGeometries.Text = "Add Geometries"
			Me.buttonAddGeometries.UseVisualStyleBackColor = True
'			Me.buttonAddGeometries.Click += New System.EventHandler(Me.buttonAddGeometries_Click)
			' 
			' buttonAddMeshes
			' 
			Me.buttonAddMeshes.Location = New System.Drawing.Point(3, 235)
			Me.buttonAddMeshes.Name = "buttonAddMeshes"
			Me.buttonAddMeshes.Size = New System.Drawing.Size(164, 23)
			Me.buttonAddMeshes.TabIndex = 0
			Me.buttonAddMeshes.Text = "Add Meshes"
			Me.toolTip1.SetToolTip(Me.buttonAddMeshes, "Meshes consist of triangles. Here - they are either tesselated from random geomet" & "ries or assembled from random triangles.")
			Me.buttonAddMeshes.UseVisualStyleBackColor = True
'			Me.buttonAddMeshes.Click += New System.EventHandler(Me.buttonAddMeshes_Click)
			' 
			' tabPage2
			' 
			Me.tabPage2.BackColor = System.Drawing.SystemColors.Window
			Me.tabPage2.Controls.Add(Me.textBoxStats)
			Me.tabPage2.Location = New System.Drawing.Point(4, 22)
			Me.tabPage2.Name = "tabPage2"
			Me.tabPage2.Padding = New System.Windows.Forms.Padding(3)
			Me.tabPage2.Size = New System.Drawing.Size(167, 418)
			Me.tabPage2.TabIndex = 1
			Me.tabPage2.Text = "Stats"
			' 
			' textBoxStats
			' 
			Me.textBoxStats.AcceptsReturn = True
			Me.textBoxStats.AcceptsTab = True
			Me.textBoxStats.Dock = System.Windows.Forms.DockStyle.Fill
			Me.textBoxStats.Location = New System.Drawing.Point(3, 3)
			Me.textBoxStats.Multiline = True
			Me.textBoxStats.Name = "textBoxStats"
			Me.textBoxStats.ReadOnly = True
			Me.textBoxStats.ScrollBars = System.Windows.Forms.ScrollBars.Both
			Me.textBoxStats.Size = New System.Drawing.Size(161, 412)
			Me.textBoxStats.TabIndex = 3
			Me.textBoxStats.Text = "Stats:"
			Me.textBoxStats.WordWrap = False
			' 
			' tabPageShapes
			' 
			Me.tabPageShapes.Controls.Add(Me.splitContainer2)
			Me.tabPageShapes.Location = New System.Drawing.Point(4, 22)
			Me.tabPageShapes.Name = "tabPageShapes"
			Me.tabPageShapes.Padding = New System.Windows.Forms.Padding(3)
			Me.tabPageShapes.Size = New System.Drawing.Size(167, 418)
			Me.tabPageShapes.TabIndex = 2
			Me.tabPageShapes.Text = "Shapes"
			Me.tabPageShapes.UseVisualStyleBackColor = True
			' 
			' splitContainer2
			' 
			Me.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill
			Me.splitContainer2.Location = New System.Drawing.Point(3, 3)
			Me.splitContainer2.Name = "splitContainer2"
			Me.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal
			' 
			' splitContainer2.Panel1
			' 
			Me.splitContainer2.Panel1.Controls.Add(Me.tabControl2)
			' 
			' splitContainer2.Panel2
			' 
			Me.splitContainer2.Panel2.Controls.Add(Me.propertyGridShapeInfo)
			Me.splitContainer2.Size = New System.Drawing.Size(161, 412)
			Me.splitContainer2.SplitterDistance = 205
			Me.splitContainer2.SplitterWidth = 6
			Me.splitContainer2.TabIndex = 4
			' 
			' tabControl2
			' 
			Me.tabControl2.Controls.Add(Me.tabPageShapesAtPoint)
			Me.tabControl2.Controls.Add(Me.tabPageAllShapes)
			Me.tabControl2.Dock = System.Windows.Forms.DockStyle.Fill
			Me.tabControl2.Location = New System.Drawing.Point(0, 0)
			Me.tabControl2.Name = "tabControl2"
			Me.tabControl2.SelectedIndex = 0
			Me.tabControl2.Size = New System.Drawing.Size(161, 205)
			Me.tabControl2.TabIndex = 3
			' 
			' tabPageShapesAtPoint
			' 
			Me.tabPageShapesAtPoint.BackColor = System.Drawing.SystemColors.Window
			Me.tabPageShapesAtPoint.Controls.Add(Me.treeViewShapesAtPoint)
			Me.tabPageShapesAtPoint.Location = New System.Drawing.Point(4, 22)
			Me.tabPageShapesAtPoint.Name = "tabPageShapesAtPoint"
			Me.tabPageShapesAtPoint.Padding = New System.Windows.Forms.Padding(3)
			Me.tabPageShapesAtPoint.Size = New System.Drawing.Size(153, 179)
			Me.tabPageShapesAtPoint.TabIndex = 0
			Me.tabPageShapesAtPoint.Text = "Shapes Clicked"
			' 
			' treeViewShapesAtPoint
			' 
			Me.treeViewShapesAtPoint.Dock = System.Windows.Forms.DockStyle.Fill
			Me.treeViewShapesAtPoint.Location = New System.Drawing.Point(3, 3)
			Me.treeViewShapesAtPoint.Name = "treeViewShapesAtPoint"
			Me.treeViewShapesAtPoint.Size = New System.Drawing.Size(147, 173)
			Me.treeViewShapesAtPoint.TabIndex = 2
			Me.toolTip1.SetToolTip(Me.treeViewShapesAtPoint, "List of shapes and hierarchies of shapes (for layers) at point clicked. Select to" & " view shape properties. Right click to peel specific shape.")
'			Me.treeViewShapesAtPoint.AfterSelect += New System.Windows.Forms.TreeViewEventHandler(Me.treeViewShapes_AfterSelect)
'			Me.treeViewShapesAtPoint.MouseDown += New System.Windows.Forms.MouseEventHandler(Me.treeViewShapes_MouseDown)
			' 
			' tabPageAllShapes
			' 
			Me.tabPageAllShapes.Controls.Add(Me.treeViewAllShapes)
			Me.tabPageAllShapes.Location = New System.Drawing.Point(4, 22)
			Me.tabPageAllShapes.Name = "tabPageAllShapes"
			Me.tabPageAllShapes.Padding = New System.Windows.Forms.Padding(3)
			Me.tabPageAllShapes.Size = New System.Drawing.Size(153, 179)
			Me.tabPageAllShapes.TabIndex = 1
			Me.tabPageAllShapes.Text = "All Shapes"
			Me.tabPageAllShapes.UseVisualStyleBackColor = True
			' 
			' treeViewAllShapes
			' 
			Me.treeViewAllShapes.Dock = System.Windows.Forms.DockStyle.Fill
			Me.treeViewAllShapes.Location = New System.Drawing.Point(3, 3)
			Me.treeViewAllShapes.Name = "treeViewAllShapes"
			Me.treeViewAllShapes.Size = New System.Drawing.Size(147, 173)
			Me.treeViewAllShapes.TabIndex = 3
			Me.toolTip1.SetToolTip(Me.treeViewAllShapes, "List of all shapes and hierarchies of shapes (for layers). Select to view shape p" & "roperties. Right click to peel specific shape.")
'			Me.treeViewAllShapes.AfterSelect += New System.Windows.Forms.TreeViewEventHandler(Me.treeViewShapes_AfterSelect)
'			Me.treeViewAllShapes.MouseDown += New System.Windows.Forms.MouseEventHandler(Me.treeViewShapes_MouseDown)
			' 
			' propertyGridShapeInfo
			' 
			Me.propertyGridShapeInfo.Dock = System.Windows.Forms.DockStyle.Fill
			Me.propertyGridShapeInfo.Location = New System.Drawing.Point(0, 0)
			Me.propertyGridShapeInfo.Name = "propertyGridShapeInfo"
			Me.propertyGridShapeInfo.Size = New System.Drawing.Size(161, 201)
			Me.propertyGridShapeInfo.TabIndex = 0
'			Me.propertyGridShapeInfo.PropertyValueChanged += New System.Windows.Forms.PropertyValueChangedEventHandler(Me.propertyGridShapeInfo_PropertyValueChanged)
			' 
			' labelFPS
			' 
			Me.labelFPS.Anchor = (CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles))
			Me.labelFPS.AutoSize = True
			Me.labelFPS.Location = New System.Drawing.Point(3, 516)
			Me.labelFPS.Name = "labelFPS"
			Me.labelFPS.Size = New System.Drawing.Size(27, 13)
			Me.labelFPS.TabIndex = 1
			Me.labelFPS.Text = "FPS"
			Me.toolTip1.SetToolTip(Me.labelFPS, "Frames Per Second - only shows for real time render mode (HwndRenderTarget)")
			' 
			' splitContainer1
			' 
			Me.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill
			Me.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel2
			Me.splitContainer1.Location = New System.Drawing.Point(0, 0)
			Me.splitContainer1.Name = "splitContainer1"
			' 
			' splitContainer1.Panel1
			' 
			Me.splitContainer1.Panel1.Controls.Add(Me.labelFPS)
			Me.splitContainer1.Panel1.Controls.Add(Me.d2dShapesControl)
			' 
			' splitContainer1.Panel2
			' 
			Me.splitContainer1.Panel2.BackColor = System.Drawing.SystemColors.Window
			Me.splitContainer1.Panel2.Controls.Add(Me.comboBoxRenderMode)
			Me.splitContainer1.Panel2.Controls.Add(Me.tabControl1)
			Me.splitContainer1.Panel2.Controls.Add(Me.buttonPeelShape)
			Me.splitContainer1.Panel2.Controls.Add(Me.buttonClear)
			Me.splitContainer1.Panel2.Controls.Add(Me.buttonUnpeel)
			Me.splitContainer1.Size = New System.Drawing.Size(670, 533)
			Me.splitContainer1.SplitterDistance = 487
			Me.splitContainer1.SplitterWidth = 6
			Me.splitContainer1.TabIndex = 5
			' 
			' d2dShapesControl
			' 
			Me.d2dShapesControl.Dock = System.Windows.Forms.DockStyle.Fill
			Me.d2dShapesControl.Location = New System.Drawing.Point(0, 0)
			Me.d2dShapesControl.Name = "d2dShapesControl"
			Me.d2dShapesControl.Render = Nothing
			Me.d2dShapesControl.RenderMode = D2DShapes.D2DShapesControl.RenderModes.BitmapRenderTargetOnPaint
			Me.d2dShapesControl.Size = New System.Drawing.Size(487, 533)
			Me.d2dShapesControl.TabIndex = 0
			Me.toolTip1.SetToolTip(Me.d2dShapesControl, "Left click to view details of the shape. Right click to peel shape.")
'			Me.d2dShapesControl.StatsChanged += New System.EventHandler(Me.d2dShapesControl_StatsChanged)
'			Me.d2dShapesControl.MouseUp += New System.Windows.Forms.MouseEventHandler(Me.d2dShapesControl_MouseUp)
'			Me.d2dShapesControl.FpsChanged += New System.EventHandler(Me.d2dShapesControl_FpsChanged)
			' 
			' D2DShapesControlWithButtons
			' 
			Me.AutoScaleDimensions = New System.Drawing.SizeF(6F, 13F)
			Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
			Me.Controls.Add(Me.splitContainer1)
			Me.Name = "D2DShapesControlWithButtons"
			Me.Size = New System.Drawing.Size(670, 533)
			Me.tabControl1.ResumeLayout(False)
			Me.tabPage1.ResumeLayout(False)
			CType(Me.numericUpDown1, System.ComponentModel.ISupportInitialize).EndInit()
			Me.tabPage2.ResumeLayout(False)
			Me.tabPage2.PerformLayout()
			Me.tabPageShapes.ResumeLayout(False)
			Me.splitContainer2.Panel1.ResumeLayout(False)
			Me.splitContainer2.Panel2.ResumeLayout(False)
			Me.splitContainer2.ResumeLayout(False)
			Me.tabControl2.ResumeLayout(False)
			Me.tabPageShapesAtPoint.ResumeLayout(False)
			Me.tabPageAllShapes.ResumeLayout(False)
			Me.splitContainer1.Panel1.ResumeLayout(False)
			Me.splitContainer1.Panel1.PerformLayout()
			Me.splitContainer1.Panel2.ResumeLayout(False)
			Me.splitContainer1.ResumeLayout(False)
			Me.ResumeLayout(False)

		End Sub
		#End Region

		Private WithEvents d2dShapesControl As D2DShapesControl
		Private WithEvents buttonAddBitmaps As System.Windows.Forms.Button
		Private WithEvents buttonAddTexts As System.Windows.Forms.Button
		Private WithEvents buttonAddEllipses As System.Windows.Forms.Button
		Private WithEvents buttonAddRoundRects As System.Windows.Forms.Button
		Private WithEvents buttonAddRectangles As System.Windows.Forms.Button
		Private WithEvents buttonAddLines As System.Windows.Forms.Button
		Private labelFPS As System.Windows.Forms.Label
		Private WithEvents buttonClear As System.Windows.Forms.Button
		Private WithEvents buttonAddGeometries As System.Windows.Forms.Button
		Private WithEvents buttonPeelShape As System.Windows.Forms.Button
		Private WithEvents buttonAddMeshes As System.Windows.Forms.Button
		Private numericUpDown1 As System.Windows.Forms.NumericUpDown
		Private WithEvents buttonAddLayer As System.Windows.Forms.Button
		Private WithEvents buttonAddGDI As System.Windows.Forms.Button
		Private WithEvents buttonUnpeel As System.Windows.Forms.Button
		Private tabControl1 As System.Windows.Forms.TabControl
		Private tabPage1 As System.Windows.Forms.TabPage
		Private tabPage2 As System.Windows.Forms.TabPage
		Private textBoxStats As System.Windows.Forms.TextBox
		Private tabPageShapes As System.Windows.Forms.TabPage
		Private WithEvents comboBoxRenderMode As System.Windows.Forms.ComboBox
		Private WithEvents propertyGridShapeInfo As System.Windows.Forms.PropertyGrid
		Private WithEvents treeViewShapesAtPoint As System.Windows.Forms.TreeView
		Private splitContainer1 As System.Windows.Forms.SplitContainer
		Private splitContainer2 As System.Windows.Forms.SplitContainer
		Private tabControl2 As System.Windows.Forms.TabControl
		Private tabPageShapesAtPoint As System.Windows.Forms.TabPage
		Private tabPageAllShapes As System.Windows.Forms.TabPage
		Private WithEvents treeViewAllShapes As System.Windows.Forms.TreeView
		Private timer1 As System.Windows.Forms.Timer
		Private components As System.ComponentModel.IContainer
		Private toolTip1 As System.Windows.Forms.ToolTip
	End Class
End Namespace
