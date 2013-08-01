//
// SplitProjectDialog.cs
//
// Author:
//       Luís Reis <luiscubal@gmail.com>
//
// Copyright (c) 2013 Luís Reis
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Linq;
using Xwt;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using System.Collections.ObjectModel;

namespace MonoDevelop.CSharp.SplitProject
{
	public class SplitProjectDialog : Dialog
	{
		ProjectGraph graph;

		DataField<CheckBoxState> selectedField = new DataField<CheckBoxState> ();
		DataField<ProjectGraph.Node> nodeField = new DataField<ProjectGraph.Node> ();
		DataField<string> nameField = new DataField<string> ();
		DataField<string> dependentsField = new DataField<string>();

		TextEntry newProjectNameField;
		TreeStore dataSource;
		DialogButton okButton;

		Dictionary<ProjectGraph.Node, TreeNavigator> nodePositions;

		public ReadOnlyCollection<ProjectGraph.Node> SelectedNodes
		{
			get {
				var nodes = new List<ProjectGraph.Node>();
				foreach (var keyValue in nodePositions) {
					var node = keyValue.Key;
					var navigator = keyValue.Value;

					if (navigator.GetValue (selectedField) != CheckBoxState.Off) {
						nodes.Add (node);
					}
				}

				return nodes.AsReadOnly();
			}
		}

		public string NewProjectName
		{
			get {
				return newProjectNameField.Text;
			}
		}

		public SplitProjectDialog (ProjectGraph graph)
		{
			this.graph = graph;

			VBox layout = new VBox ();

			var projectNameBox = new HBox ();
			projectNameBox.PackStart (new Label (GettextCatalog.GetString ("Class Library Name:")));
			newProjectNameField = new TextEntry ();
			newProjectNameField.Changed += (object sender, EventArgs e) => ValidateSplitProposal ();
			projectNameBox.PackStart (newProjectNameField, true, true);
			layout.PackStart (projectNameBox);

			layout.PackStart (new Label(GettextCatalog.GetString ("Files to move to new class library project")));

			var tree = BuildTree (graph);
			layout.PackStart (tree, true, true);

			Content = layout;

			Title = GettextCatalog.GetString("Split Project");
			Size = new Size (400, 300);

			Buttons.Add (okButton = new Xwt.DialogButton (Command.Ok));
			Buttons.Add (new Xwt.DialogButton (Command.Cancel));

			ValidateSplitProposal ();
		}

		void ValidateSplitProposal ()
		{
			okButton.Sensitive = IsSplitProposalValid ();
		}

		bool IsSplitProposalValid ()
		{
			if (!FileService.IsValidFileName (NewProjectName))
				return false;

			if (SelectedNodes.Count == graph.Nodes.Count)
				return false;

			if (SelectedNodes.Count == 0)
				return false;

			return true;
		}

		TreeView BuildTree (ProjectGraph graph)
		{
			dataSource = new TreeStore (selectedField, nodeField, nameField, dependentsField);

			nodePositions = new Dictionary<ProjectGraph.Node, TreeNavigator> ();
			var navigators = new Dictionary<string, TreeNavigator> ();

			foreach (var node in graph.Nodes) {
				var virtualPath = node.File.ProjectVirtualPath;
				var parentNavigator = BuildDirectories (dataSource, navigators, virtualPath.ParentDirectory);
				TreeNavigator nodeNavigator;
				if (navigators.TryGetValue (virtualPath, out nodeNavigator)) {
					nodeNavigator.SetValue (nodeField, node);
				}
				else {
					nodeNavigator = parentNavigator == null ? dataSource.AddNode () : parentNavigator.Clone ().AddChild ();
					nodeNavigator.SetValue (nodeField, node).SetValue (nameField, node.ToString ());
					nodePositions.Add (node, nodeNavigator);
				}
				navigators.Add (virtualPath, nodeNavigator);
			}

			TreeView tree = new TreeView ();
			var cellView = new CheckBoxCellView (selectedField) {
				Editable = true
			};

			cellView.Toggled += (object sender, WidgetEventArgs e) =>  {
				var rowPosition = tree.CurrentEventRow;
				if (rowPosition == null) {
					return;
				}
				var row = dataSource.GetNavigatorAt (rowPosition);
				var checkState = row.GetValue (selectedField);
				var node = row.GetValue (nodeField);

				ISet<ProjectGraph.Node> descendentNodes = GetNodeAndDescendents(row);

				if (checkState == CheckBoxState.On) {
					foreach (var descendent in descendentNodes) {
						graph.ResetVisitedNodes();

						VisitReversed (descendent, visitedNode => {
							nodePositions[visitedNode].SetValue(selectedField, CheckBoxState.Off);
						});
					}

					foreach (var nodeToUpdate in graph.Nodes) {
						nodeToUpdate.ActiveDependentNodes.RemoveAll(candidateToRemove => nodePositions[candidateToRemove].GetValue(selectedField) == CheckBoxState.Off);
						UpdateDependentsText(nodeToUpdate, nodePositions[nodeToUpdate]);
					}
				}
				else {
					foreach (var descendent in descendentNodes) {
						var descendentPosition = nodePositions[descendent];
						descendentPosition.SetValue(selectedField, CheckBoxState.On);

						graph.ResetVisitedNodes ();

						Visit (descendent, visitedNode => {
							if (!visitedNode.Equals (descendent)) {
								AddActiveDependentNode(visitedNode, descendent);
							}
						});
					}
				}

				UpdateFolderStates();
				ValidateSplitProposal();
				e.Handled = true;
			};
			tree.Columns.Add (new ListViewColumn (GettextCatalog.GetString("Move"), cellView));
			tree.Columns.Add (GettextCatalog.GetString("Name"), nameField);
			tree.Columns.Add (GettextCatalog.GetString("Dependents"), dependentsField);
			tree.DataSource = dataSource;
			return tree;
		}

		void UpdateFolderStates ()
		{
			TreeNavigator navigator = dataSource.GetFirstNode ();
			if (navigator.CurrentPosition == null) {
				return;
			}

			do {
				UpdateFolderState (navigator);
			} while (navigator.MoveNext());
		}

		CheckBoxState UpdateFolderState (TreeNavigator navigator) {
			if (!navigator.MoveToChild()) {
				//Already a leaf (file)
				return navigator.GetValue (selectedField);
			}

			bool allSelected = true;
			bool allDeselected = true;

			do {
				var childState = UpdateFolderState(navigator);

				if (childState != CheckBoxState.On)
					allSelected = false;

				if (childState != CheckBoxState.Off)
					allDeselected = false;

			} while (navigator.MoveNext());

			navigator.MoveToParent ();

			var newState = allSelected ? CheckBoxState.On :
				allDeselected ? CheckBoxState.Off : CheckBoxState.Mixed;

			navigator.SetValue (selectedField, newState);

			return newState;
		}

		void AddActiveDependentNode (ProjectGraph.Node dependency, ProjectGraph.Node dependent)
		{
			if (dependency == dependent) {
				return;
			}

			dependency.ActiveDependentNodes.Add (dependent);

			var dependencyPosition = nodePositions [dependency];

			dependencyPosition.SetValue (selectedField, CheckBoxState.On);
			UpdateDependentsText (dependency, dependencyPosition);
		}

		void UpdateDependentsText (ProjectGraph.Node dependency, TreeNavigator dependencyPosition)
		{
			dependencyPosition.SetValue (dependentsField, string.Join (", ", dependency.ActiveDependentNodes));
		}

		/// <summary>
		/// Gets the set of all nodes that are in the given position or descendents
		/// of the given position.
		/// </summary>
		/// <returns>The set of nodes in the position or descendent positions.</returns>
		/// <param name="navigator">The starting position to search.</param>
		/// <remarks>
		/// If navigator is set to the root of the tree, then this method will return all nodes in the tree.
		/// If navigator is a leaf, then this method will return a set with only the node in that leaf.
		/// Project tree nodes without associated ProjectGraph.Node instances (folders) are excluded from
		/// the result.
		/// </remarks>
		ISet<ProjectGraph.Node> GetNodeAndDescendents (TreeNavigator navigator)
		{
			ISet<ProjectGraph.Node> nodesToSelect = new HashSet<ProjectGraph.Node> ();

			int depth = 0;

			//Depth-first search of the project tree
			// depth is zero when navigator is in the starting position
			do {
				//Step 1. Add the current node
				var traversalNode = navigator.GetValue (nodeField);
				if (traversalNode != null) {
					nodesToSelect.Add (traversalNode);
				}

				//Step 2. Move to the first child (and continue to the first step)
				if (navigator.MoveToChild ()) {
					depth++;
				} else {
					//Step 3. Try to move to the next unvisited sibling.
					// if the node has no unvisited siblings, then move to unvisited sibling
					// of ancestor.
					//Then continue to the first step
					//If depth == 0, then we've reached our base node, so we won't try
					// to visit siblings nor ancestors anymore
					while (depth > 0 && !navigator.MoveNext()) {
						navigator.MoveToParent ();
						depth--;
					}
				}
			} while (depth > 0);

			return nodesToSelect;
		}

		TreeNavigator BuildDirectories (TreeStore dataSource, Dictionary<string, TreeNavigator> directories, FilePath directory)
		{
			if (directory == "") {
				return null;
			}

			if (directories.ContainsKey (directory.ToString ())) {
				return directories[directory.ToString ()];
			}

			var parentNavigator = BuildDirectories (dataSource, directories, directory.ParentDirectory);

			var directoryNavigator = parentNavigator == null ? dataSource.AddNode () : parentNavigator.Clone().AddChild();
			directoryNavigator.SetValue (nameField, directory.FileName);

			directories.Add (directory.ToString (), directoryNavigator);

			return directoryNavigator;
		}

		void DeselectNode (ProjectGraph.Node node)
		{
			VisitReversed (node, foundNode =>  {
				nodePositions [foundNode].SetValue (selectedField, CheckBoxState.Off);
			});
		}
		
		void Visit (ProjectGraph.Node node,Action<ProjectGraph.Node> callback)
		{
			callback (node);

			node.Visited = true;

			foreach (var destinationNode in node.DestinationNodes) {
				if (!destinationNode.Visited) {
					Visit (destinationNode, callback);
				}
			}
		}

		void VisitReversed (ProjectGraph.Node node, Action<ProjectGraph.Node> callback)
		{
			callback (node);

			node.Visited = true;

			foreach (var destinationNode in node.SourceNodes) {
				if (!destinationNode.Visited) {
					VisitReversed (destinationNode, callback);
				}
			}
		}
	}
}

