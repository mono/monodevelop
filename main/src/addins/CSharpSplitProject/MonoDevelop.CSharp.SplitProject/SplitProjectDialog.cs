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
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xwt;
using Mono.TextEditor;
using MonoDevelop.Projects;
using MonoDevelop.CSharp.Refactoring.CodeActions;
using MonoDevelop.Ide;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Core;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;

namespace MonoDevelop.CSharp.SplitProject
{
	public class SplitProjectDialog : Dialog
	{
		DotNetProject project;
		ProjectGraph graph;

		DataField<bool> selectedField = new DataField<bool> ();
		DataField<ProjectGraph.Node> nodeField = new DataField<ProjectGraph.Node> ();
		DataField<string> nameField = new DataField<string> ();

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

					if (navigator.GetValue (selectedField)) {
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

		public SplitProjectDialog (DotNetProject project, ProjectGraph graph)
		{
			this.project = project;
			this.graph = graph;

			VBox layout = new VBox ();

			var projectNameBox = new HBox ();
			projectNameBox.PackStart (new Label (GettextCatalog.GetString ("Class Library Name:")));
			newProjectNameField = new TextEntry ();
			newProjectNameField.Changed += (object sender, EventArgs e) => ValidateSplitProposal ();
			projectNameBox.PackStart (newProjectNameField);
			layout.PackStart (projectNameBox);
			var tree = BuildTree (graph);
			layout.PackStart (tree, true, true);

			Content = layout;

			Title = "Split Project";

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

			return true;

			return true;
		}

		TreeView BuildTree (ProjectGraph graph)
		{
			var dataSource = new TreeStore (selectedField, nodeField, nameField);

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
			}
			;
			cellView.Toggled += (object sender, WidgetEventArgs e) =>  {
				var rowPosition = tree.CurrentEventRow;
				if (rowPosition == null) {
					Console.WriteLine ("<null>");
					return;
				}
				var row = dataSource.GetNavigatorAt (rowPosition);
				var selected = row.GetValue (selectedField);
				var node = row.GetValue (nodeField);
				Console.WriteLine ("node={0}", node);
				graph.ResetVisitedNodes ();
				if (node == null) {
					//TODO: Handle folders
				}
				else {
					if (selected) {
						DeselectNode (node);
					}
					else {
						SelectNode (node);
					}
				}
				e.Handled = true;
			};
			tree.Columns.Add (new ListViewColumn ("Move to new project", cellView));
			tree.Columns.Add ("Name", nameField);
			tree.DataSource = dataSource;
			return tree;
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
				nodePositions [foundNode].SetValue (selectedField, false);
			});
		}

		void SelectNode (ProjectGraph.Node node)
		{
			Visit (node, foundNode =>  {
				nodePositions [foundNode].SetValue (selectedField, true);
			});
		}
		
		void Visit (ProjectGraph.Node node, Action<ProjectGraph.Node> callback)
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

