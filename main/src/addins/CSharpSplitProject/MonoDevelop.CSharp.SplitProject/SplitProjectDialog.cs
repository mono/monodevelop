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

namespace MonoDevelop.CSharp.SplitProject
{
	public class SplitProjectDialog : Dialog
	{
		DotNetProject project;

		CancellationTokenSource cancellationTokenSource;

		public SplitProjectDialog (DotNetProject project, ProjectGraph graph)
		{
			this.project = project;

			cancellationTokenSource = new CancellationTokenSource ();

			SetupDialog (graph);

			Title = "Split Project";

			Buttons.Add (new Xwt.DialogButton (Command.Ok));
			Buttons.Add (new Xwt.DialogButton (Command.Cancel));
		}

		protected override void Dispose (bool disposing)
		{
			base.Dispose (disposing);

			cancellationTokenSource.Dispose ();
		}

		void SetupDialog (ProjectGraph result)
		{
			DataField<bool> selectedField = new DataField<bool> ();
			DataField<ProjectGraph.Node> nodeField = new DataField<ProjectGraph.Node> ();
			DataField<string> nameField = new DataField<string> ();

			var dataSource = new TreeStore (selectedField, nodeField, nameField);

			Dictionary<ProjectGraph.Node, TreePosition> nodePositions = new Dictionary<ProjectGraph.Node, TreePosition> ();

			foreach (var node in result.Nodes) {
				var position = dataSource.AddNode ().SetValue (nodeField, node).SetValue (nameField, node.ToString ()).CurrentPosition;
				nodePositions.Add (node, position);
			}

			TreeView tree = new TreeView ();
			var cellView = new CheckBoxCellView (selectedField) { Editable = true };

			bool propagatingChanges = false;

			cellView.Toggled += (object sender, WidgetEventArgs e) => {
				if (propagatingChanges)
				{
					return;
				}

				propagatingChanges = true;

				var rowPosition = tree.SelectedRow;
				if (rowPosition == null) {
					return;
				}

				var row = dataSource.GetNavigatorAt(rowPosition);

				var selected = row.GetValue(selectedField);
				var node = row.GetValue(nodeField);

				if (selected) {
					//Deselect

					VisitReversed (node, (foundNode) => {
						dataSource.GetNavigatorAt(nodePositions[node]).SetValue(selectedField, false);
					});
				}
				else {
					//Select
					
					Visit (node, (foundNode) => {
						dataSource.GetNavigatorAt(nodePositions[node]).SetValue(selectedField, true);
					});
				}

				e.Handled = true;
				propagatingChanges = false;
			};
			tree.Columns.Add (new ListViewColumn("Move to new project", cellView));
			tree.Columns.Add ("Name", nameField);

			tree.DataSource = dataSource;
			Content = tree;
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

