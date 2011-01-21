' Copyright (c) Microsoft Corporation.  All rights reserved.

Imports Microsoft.VisualBasic
Imports System.Windows.Forms
Imports System.ComponentModel
Imports Microsoft.WindowsAPICodePack.DirectX.Direct2D1

Namespace D2DShapes
	Partial Public Class D2DShapesControlWithButtons
		Inherits UserControl
		#Region "NumberOfShapesToAdd"
		<DefaultValue(1)> _
		Public Property NumberOfShapesToAdd() As Integer
			Get
				Return CInt(Fix(numericUpDown1.Value))
			End Get
			Set(ByVal value As Integer)
				numericUpDown1.Value = value
			End Set
		End Property
		#End Region

		#Region "D2DShapesControlWithButtons() - CTOR"
		Public Sub New()
			InitializeComponent()
			comboBoxRenderMode.SelectedIndex = CInt(Fix(d2dShapesControl.RenderMode))
		End Sub
		#End Region

		#Region "Initialize()"
		Public Sub Initialize()
			d2dShapesControl.Initialize()
		End Sub
		#End Region

		#Region "buttonAdd~ event handlers"
		Private Sub buttonAddLines_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles buttonAddLines.Click
			For i As Integer = 0 To CInt(Fix(numericUpDown1.Value)) - 1
				AddToTree(d2dShapesControl.AddLine())
			Next i
		End Sub

		Private Sub buttonAddRectangles_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles buttonAddRectangles.Click
			For i As Integer = 0 To CInt(Fix(numericUpDown1.Value)) - 1
				AddToTree(d2dShapesControl.AddRectangle())
			Next i
		End Sub

		Private Sub buttonAddRoundRects_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles buttonAddRoundRects.Click
			For i As Integer = 0 To CInt(Fix(numericUpDown1.Value)) - 1
				AddToTree(d2dShapesControl.AddRoundRect())
			Next i
		End Sub

		Private Sub buttonAddEllipses_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles buttonAddEllipses.Click
			For i As Integer = 0 To CInt(Fix(numericUpDown1.Value)) - 1
				AddToTree(d2dShapesControl.AddEllipse())
			Next i
		End Sub

		Private Sub buttonAddTexts_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles buttonAddTexts.Click
			For i As Integer = 0 To CInt(Fix(numericUpDown1.Value)) - 1
				AddToTree(d2dShapesControl.AddText())
			Next i
		End Sub

		Private Sub buttonAddBitmaps_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles buttonAddBitmaps.Click
			For i As Integer = 0 To CInt(Fix(numericUpDown1.Value)) - 1
				AddToTree(d2dShapesControl.AddBitmap())
			Next i
		End Sub

		Private Sub buttonAddGeometries_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles buttonAddGeometries.Click
			For i As Integer = 0 To CInt(Fix(numericUpDown1.Value)) - 1
				AddToTree(d2dShapesControl.AddGeometry())
			Next i
		End Sub

		Private Sub buttonAddMeshes_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles buttonAddMeshes.Click
			For i As Integer = 0 To CInt(Fix(numericUpDown1.Value)) - 1
				AddToTree(d2dShapesControl.AddMesh())
			Next i
		End Sub

		Private Sub buttonAddGDI_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles buttonAddGDI.Click
			AddToTree(d2dShapesControl.AddGDIEllipses(CInt(Fix(numericUpDown1.Value))))
		End Sub

		Private Sub buttonAddLayer_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles buttonAddLayer.Click
			AddToTree(d2dShapesControl.AddLayer(CInt(Fix(numericUpDown1.Value))))
		End Sub
		#End Region

		#Region "d2dShapesControl_FpsChanged"
		Private Sub d2dShapesControl_FpsChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles d2dShapesControl.FpsChanged
			labelFPS.Text = "FPS: " & d2dShapesControl.Fps
		End Sub
		#End Region

		#Region "d2dShapesControl_MouseUp"
		Private Sub d2dShapesControl_MouseUp(ByVal sender As Object, ByVal e As MouseEventArgs) Handles d2dShapesControl.MouseUp
			If e.Button = MouseButtons.Right Then
				Dim shape As DrawingShape = d2dShapesControl.PeelAt(New Point2F(e.Location.X, e.Location.Y))
				If shape IsNot Nothing Then
					RemoveFromTree(shape, treeViewAllShapes.Nodes)
				End If
			End If
			treeViewShapesAtPoint.Nodes.Clear()
			treeViewShapesAtPoint.Nodes.Add(d2dShapesControl.GetTreeAt(New Point2F(e.Location.X, e.Location.Y)))
			treeViewShapesAtPoint.ExpandAll()
			If treeViewShapesAtPoint.Nodes.Count > 0 Then
				Dim nodeToSelect As TreeNode = treeViewShapesAtPoint.Nodes(0)
				Do While nodeToSelect.Nodes.Count > 0
					nodeToSelect = nodeToSelect.Nodes(0)
				Loop
				treeViewShapesAtPoint.SelectedNode = nodeToSelect
				If e.Button = MouseButtons.Left Then
					tabControl1.SelectedTab = tabPageShapes
					tabControl2.SelectedTab = tabPageShapesAtPoint
					treeViewShapesAtPoint.Focus()
				End If
			End If
		End Sub
		#End Region

		#Region "d2dShapesControl_StatsChanged"
		Private Sub d2dShapesControl_StatsChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles d2dShapesControl.StatsChanged
            textBoxStats.Text = "Stats:" & System.Environment.NewLine & d2dShapesControl.StatsString
		End Sub
		#End Region

		#Region "buttonClear_Click"
		Private Sub buttonClear_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles buttonClear.Click
			d2dShapesControl.ClearShapes()
			treeViewAllShapes.Nodes.Clear()
		End Sub
		#End Region

		#Region "buttonPeelShape_Click"
		Private Sub buttonPeelShape_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles buttonPeelShape.Click
			RemoveFromTree(d2dShapesControl.PeelShape(), treeViewAllShapes.Nodes)
		End Sub
		#End Region

		#Region "buttonUnpeel_Click"
		Private Sub buttonUnpeel_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles buttonUnpeel.Click
			Dim shape As DrawingShape = d2dShapesControl.UnpeelShape()
			If shape IsNot Nothing Then
				AddToTree(shape)
			End If
		End Sub
		#End Region

		#Region "comboBoxRenderMode_SelectedIndexChanged"
		Private Sub comboBoxRenderMode_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles comboBoxRenderMode.SelectedIndexChanged
			If comboBoxRenderMode.SelectedIndex >= 0 Then
				d2dShapesControl.RenderMode = CType(comboBoxRenderMode.SelectedIndex, D2DShapesControl.RenderModes)
				labelFPS.Visible = d2dShapesControl.RenderMode = D2DShapesControl.RenderModes.HwndRenderTarget
			End If
		End Sub
		#End Region

		#Region "treeViewShapes_AfterSelect"
		Private Sub treeViewShapes_AfterSelect(ByVal sender As Object, ByVal e As TreeViewEventArgs) Handles treeViewShapesAtPoint.AfterSelect, treeViewAllShapes.AfterSelect
			Dim tree = CType(sender, TreeView)
			If tree.SelectedNode IsNot Nothing AndAlso TypeOf tree.SelectedNode.Tag Is DrawingShape Then
				propertyGridShapeInfo.SelectedObject = tree.SelectedNode.Tag
			End If
		End Sub
		#End Region

		#Region "treeViewShapes_MouseDown"
		Private Sub treeViewShapes_MouseDown(ByVal sender As Object, ByVal e As MouseEventArgs) Handles treeViewShapesAtPoint.MouseDown, treeViewAllShapes.MouseDown
			Dim tree = CType(sender, TreeView)
			Dim node As TreeNode = tree.HitTest(e.Location).Node
			tree.SelectedNode = node
			If e.Button = MouseButtons.Right AndAlso node IsNot Nothing Then
				Dim shape = TryCast(node.Tag, DrawingShape)
				If shape IsNot Nothing Then
					RemoveFromTree(shape, treeViewAllShapes.Nodes)
					RemoveFromTree(shape, treeViewShapesAtPoint.Nodes)
					d2dShapesControl.PeelShape(shape)
				End If
			End If
		End Sub
		#End Region

		#Region "AddToTree"
		Private Sub AddToTree(ByVal shape As DrawingShape)
			AddToTreeRecursive(shape, treeViewAllShapes.Nodes)
		End Sub
		#End Region

		#Region "AddToTreeRecursive"
		Private Shared Sub AddToTreeRecursive(ByVal shape As DrawingShape, ByVal treeNodeCollection As TreeNodeCollection)
			Dim node = New TreeNode(shape.ToString()) With {.Tag = shape}
			node.Expand()
			treeNodeCollection.Add(node)
			If shape.ChildShapes IsNot Nothing AndAlso shape.ChildShapes.Count > 0 Then
				For Each s As DrawingShape In shape.ChildShapes
					AddToTreeRecursive(s, node.Nodes)
				Next s
			End If
		End Sub
		#End Region

		#Region "RemoveFromTree"
		''' <summary>
		''' Remove shape from the tree node collection
		''' </summary>
		''' <param name="shape"></param>
		''' <param name="treeNodes"></param>
		''' <returns>true if removed</returns>
		Private Shared Function RemoveFromTree(ByVal shape As DrawingShape, ByVal treeNodes As TreeNodeCollection) As Boolean
			For Each node As TreeNode In treeNodes
				If node.Tag Is shape Then
					treeNodes.Remove(node)
					Return True
				End If
				If node.Nodes.Count > 0 AndAlso RemoveFromTree(shape, node.Nodes) Then
					Return True
				End If
			Next node
			Return False
		End Function
		#End Region

		#Region "propertyGridShapeInfo_PropertyValueChanged()"
		Private Sub propertyGridShapeInfo_PropertyValueChanged(ByVal s As Object, ByVal e As PropertyValueChangedEventArgs) Handles propertyGridShapeInfo.PropertyValueChanged
			d2dShapesControl.RefreshAll()
		End Sub
		#End Region
	End Class
End Namespace
