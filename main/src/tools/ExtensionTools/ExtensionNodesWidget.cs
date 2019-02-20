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

		public ExtensionNodesWidget (string path, Addin[] addins = null)
		{
			addins = addins ?? AddinManager.Registry.GetAllAddins ();

			treeStore = new TreeStore (labelField);
			treeView = new TreeView (treeStore);

			var col = treeView.Columns.Add ("Name", labelField);
			col.Expands = true;

			FillData (path, addins);
			treeView.ExpandAll ();

			var vbox = new VBox ();
			vbox.PackStart (summary, false);
			vbox.PackStart (treeView, true);
			Content = vbox;
		}

		void FillData (string path, Addin[] addins)
		{
			// TODO: add group by addin support.
			var allNodes = addins
				.SelectMany (x => x.Description.AllModules)
				.SelectMany (x => x.Extensions)
				.Where (x => x.Path == path)
				.Select (x => x.ExtensionNodes);

			var nav = treeStore.AddNode ();

			int maxDepth = 0;
			int count = 0;
			foreach (var node in allNodes) {
				int depth = BuildTree (nav, node, 1, ref count);
				maxDepth = Math.Max (maxDepth, depth);
			}

			summary.Text = $"'{path}' Count: {count} Depth: {maxDepth}";
		}

		int BuildTree (TreeNavigator currentPosition, ExtensionNodeDescriptionCollection nodes, int currentDepth, ref int count)
		{
			int maxDepth = currentDepth;

			// TODO: insertbefore/after

			foreach (ExtensionNodeDescription node in nodes) {
				count++;
				var pos = currentPosition.Clone ().AddChild ();

				var label = GetLabelForNode (node);
				pos.SetValue (labelField, label);

				var childDepth = BuildTree (pos, node.ChildNodes, currentDepth + 1, ref count);
				maxDepth = Math.Max (maxDepth, childDepth);
			}

			return maxDepth;
		}

		string GetLabelForNode (ExtensionNodeDescription node)
		{
			if (node.IsCondition) {
				var value = node.GetAttribute ("value");
				if (!string.IsNullOrEmpty (value))
					return $"Condition: {node.Id} == {value}";
				return $"Condition: {node.Id}";
			}

			var type = node.GetAttribute ("class");
			if (!string.IsNullOrEmpty (type))
				return type;

			return node.Id;
		}

		// TODO: add full attribute visualization
	}
}
