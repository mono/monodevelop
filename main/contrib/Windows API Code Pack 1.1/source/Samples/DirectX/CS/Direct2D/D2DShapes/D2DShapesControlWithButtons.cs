// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Windows.Forms;
using System.ComponentModel;
using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;

namespace D2DShapes
{
    public partial class D2DShapesControlWithButtons : UserControl
    {
        #region NumberOfShapesToAdd
        [DefaultValue(1)]
        public int NumberOfShapesToAdd
        {
            get { return (int)numericUpDown1.Value; }
            set { numericUpDown1.Value = value; }
        } 
        #endregion

        #region D2DShapesControlWithButtons() - CTOR
        public D2DShapesControlWithButtons()
        {
            InitializeComponent();
            comboBoxRenderMode.SelectedIndex = (int)d2dShapesControl.RenderMode;
        } 
        #endregion

        #region Initialize()
        public void Initialize()
        {
            d2dShapesControl.Initialize();
        } 
        #endregion

        #region buttonAdd~ event handlers
        private void buttonAddLines_Click(object sender, System.EventArgs e)
        {
            for (int i = 0; i < (int)numericUpDown1.Value; i++)
            {
                AddToTree(d2dShapesControl.AddLine());
            }
        }

        private void buttonAddRectangles_Click(object sender, System.EventArgs e)
        {
            for (int i = 0; i < (int)numericUpDown1.Value; i++)
                AddToTree(d2dShapesControl.AddRectangle());
        }

        private void buttonAddRoundRects_Click(object sender, System.EventArgs e)
        {
            for (int i = 0; i < (int)numericUpDown1.Value; i++)
                AddToTree(d2dShapesControl.AddRoundRect());
        }

        private void buttonAddEllipses_Click(object sender, System.EventArgs e)
        {
            for (int i = 0; i < (int)numericUpDown1.Value; i++)
                AddToTree(d2dShapesControl.AddEllipse());
        }

        private void buttonAddTexts_Click(object sender, System.EventArgs e)
        {
            for (int i = 0; i < (int)numericUpDown1.Value; i++)
                AddToTree(d2dShapesControl.AddText());
        }

        private void buttonAddBitmaps_Click(object sender, System.EventArgs e)
        {
            for (int i = 0; i < (int)numericUpDown1.Value; i++)
                AddToTree(d2dShapesControl.AddBitmap());
        }

        private void buttonAddGeometries_Click(object sender, System.EventArgs e)
        {
            for (int i = 0; i < (int)numericUpDown1.Value; i++)
                AddToTree(d2dShapesControl.AddGeometry());
        }

        private void buttonAddMeshes_Click(object sender, System.EventArgs e)
        {
            for (int i = 0; i < (int)numericUpDown1.Value; i++)
                AddToTree(d2dShapesControl.AddMesh());
        }

        private void buttonAddGDI_Click(object sender, System.EventArgs e)
        {
            AddToTree(d2dShapesControl.AddGDIEllipses((int)numericUpDown1.Value));
        }

        private void buttonAddLayer_Click(object sender, System.EventArgs e)
        {
            AddToTree(d2dShapesControl.AddLayer((int)numericUpDown1.Value));
        } 
        #endregion

        #region d2dShapesControl_FpsChanged
        private void d2dShapesControl_FpsChanged(object sender, System.EventArgs e)
        {
            labelFPS.Text = "FPS: " + d2dShapesControl.Fps;
        } 
        #endregion

        #region d2dShapesControl_MouseUp
        private void d2dShapesControl_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                DrawingShape shape = d2dShapesControl.PeelAt(new Point2F(e.Location.X, e.Location.Y));
                if (shape != null)
                    RemoveFromTree(shape, treeViewAllShapes.Nodes);
            }
            treeViewShapesAtPoint.Nodes.Clear();
            treeViewShapesAtPoint.Nodes.Add(d2dShapesControl.GetTreeAt(new Point2F(e.Location.X, e.Location.Y)));
            treeViewShapesAtPoint.ExpandAll();
            if (treeViewShapesAtPoint.Nodes.Count > 0)
            {
                TreeNode nodeToSelect = treeViewShapesAtPoint.Nodes[0];
                while (nodeToSelect.Nodes.Count > 0)
                    nodeToSelect = nodeToSelect.Nodes[0];
                treeViewShapesAtPoint.SelectedNode = nodeToSelect;
                if (e.Button == MouseButtons.Left)
                {
                    tabControl1.SelectedTab = tabPageShapes;
                    tabControl2.SelectedTab = tabPageShapesAtPoint;
                    treeViewShapesAtPoint.Focus();
                }
            }
        }
        #endregion

        #region d2dShapesControl_StatsChanged
        private void d2dShapesControl_StatsChanged(object sender, System.EventArgs e)
        {
            textBoxStats.Text = "Stats:" + System.Environment.NewLine + d2dShapesControl.Stats;
        } 
        #endregion

        #region buttonClear_Click
        private void buttonClear_Click(object sender, System.EventArgs e)
        {
            d2dShapesControl.ClearShapes();
            treeViewAllShapes.Nodes.Clear();
        }
        #endregion

        #region buttonPeelShape_Click
        private void buttonPeelShape_Click(object sender, System.EventArgs e)
        {
            RemoveFromTree(d2dShapesControl.PeelShape(), treeViewAllShapes.Nodes);
        }
        #endregion

        #region buttonUnpeel_Click
        private void buttonUnpeel_Click(object sender, System.EventArgs e)
        {
            DrawingShape shape = d2dShapesControl.UnpeelShape();
            if (shape != null)
                AddToTree(shape);
        } 
        #endregion

        #region comboBoxRenderMode_SelectedIndexChanged
        private void comboBoxRenderMode_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (comboBoxRenderMode.SelectedIndex >= 0)
            {
                d2dShapesControl.RenderMode = (D2DShapesControl.RenderModes) comboBoxRenderMode.SelectedIndex;
                labelFPS.Visible = d2dShapesControl.RenderMode == D2DShapesControl.RenderModes.HwndRenderTarget;
            }
        } 
        #endregion

        #region treeViewShapes_AfterSelect
        private void treeViewShapes_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var tree = (TreeView)sender;
            if (tree.SelectedNode != null && tree.SelectedNode.Tag is DrawingShape)
                propertyGridShapeInfo.SelectedObject = tree.SelectedNode.Tag;
        } 
        #endregion

        #region treeViewShapes_MouseDown
        private void treeViewShapes_MouseDown(object sender, MouseEventArgs e)
        {
            var tree = (TreeView)sender;
            TreeNode node = tree.HitTest(e.Location).Node;
            tree.SelectedNode = node;
            if (e.Button == MouseButtons.Right && node != null)
            {
                var shape = node.Tag as DrawingShape;
                if (shape != null)
                {
                    RemoveFromTree(shape, treeViewAllShapes.Nodes);
                    RemoveFromTree(shape, treeViewShapesAtPoint.Nodes);
                    d2dShapesControl.PeelShape(shape);
                }
            }
        } 
        #endregion

        #region AddToTree
        private void AddToTree(DrawingShape shape)
        {
            AddToTreeRecursive(shape, treeViewAllShapes.Nodes);
        } 
        #endregion

        #region AddToTreeRecursive
        private static void AddToTreeRecursive(DrawingShape shape, TreeNodeCollection treeNodeCollection)
        {
            var node = new TreeNode(shape.ToString()) { Tag = shape};
            node.Expand();
            treeNodeCollection.Add(node);
            if (shape.ChildShapes != null && shape.ChildShapes.Count > 0)
                foreach (DrawingShape s in shape.ChildShapes)
                {
                    AddToTreeRecursive(s, node.Nodes);
                }
        } 
        #endregion

        #region RemoveFromTree
        /// <summary>
        /// Remove shape from the tree node collection
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="treeNodes"></param>
        /// <returns>true if removed</returns>
        private static bool RemoveFromTree(DrawingShape shape, TreeNodeCollection treeNodes)
        {
            foreach (TreeNode node in treeNodes)
            {
                if (node.Tag == shape)
                {
                    treeNodes.Remove(node);
                    return true;
                }
                if (node.Nodes.Count > 0 && RemoveFromTree(shape, node.Nodes))
                    return true;
            }
            return false;
        } 
        #endregion

        #region propertyGridShapeInfo_PropertyValueChanged()
        private void propertyGridShapeInfo_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            d2dShapesControl.RefreshAll();
        }
        #endregion
    }
}
