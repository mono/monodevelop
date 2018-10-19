//
// ExtensionNodesWidget.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
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
using Mono.Addins;
using Mono.Addins.Description;
using Xwt;

namespace MonoDevelop.ExtensionTools
{
	class ExtensionNodesWidget : Widget
	{
		readonly TreeStore treeStore;
		readonly TreeView treeView;
		readonly DataField<string> labelField = new DataField<string> ();
		readonly Label summary = new Label ();

		public ExtensionNodesWidget (string path)
		{
			treeStore = new TreeStore (labelField);
			treeView = new TreeView (treeStore);

			var col = treeView.Columns.Add ("Name", labelField);
			col.Expands = true;

			FillData (path);
			treeView.ExpandAll ();

			var vbox = new VBox ();
			vbox.PackStart (summary, false);
			vbox.PackStart (treeView, true);
			Content = vbox;
		}

		void FillData (string path)
		{
			var nodes = AddinManager.GetExtensionNodes (path);

			var nav = treeStore.AddNode ();
			int depth = BuildTree (nav, nodes, 1);

			summary.Text = $"'{path}' Count: {nodes.Count} Depth: {depth}";
		}

		int BuildTree (TreeNavigator currentPosition, ExtensionNodeList nodes, int currentDepth)
		{
			int maxDepth = currentDepth;

			foreach (ExtensionNode node in nodes) {
				var pos = currentPosition.Clone ().AddChild ();
				pos.SetValue (labelField, node.Id);

				var childDepth = BuildTree (pos, node.ChildNodes, currentDepth + 1);
				maxDepth = Math.Max (maxDepth, childDepth);
			}

			return maxDepth;
		}
	}
}
