//
// AddinDependencyTreeWidget.cs
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
using Mono.Addins;
using Mono.Addins.Description;
using Xwt;
namespace MonoDevelop.ExtensionTools
{
	class AddinDependencyTreeWidget : Widget
	{
		readonly TreeStore treeStore;
		readonly TreeView treeView;
		readonly DataField<string> labelField = new DataField<string> ();
		readonly Label summary = new Label ();

		public AddinDependencyTreeWidget ()
		{
			treeStore = new TreeStore (labelField);
			treeView = new TreeView (treeStore);

			var col = treeView.Columns.Add ("Name", labelField);
			col.Expands = true;

			FillData ();
			treeView.ExpandAll ();

			var vbox = new VBox ();
			vbox.PackStart (summary);
			vbox.PackStart (treeView, true);
			Content = vbox;
		}

		void FillData()
		{
			var roots = MakeDependencyTree ();
			var node = treeStore.AddNode ();

			node.SetValue (labelField, "Root");
			int depth = BuildTree (node, roots, new HashSet<AddinNode> (), 1);

			summary.Text = $"Depth: {depth}";
		}

		int BuildTree (TreeNavigator currentPosition, IEnumerable<AddinNode> addins, HashSet<AddinNode> visited, int currentDepth)
		{
			int maxDepth = currentDepth;

			foreach (var addinNode in addins) {
				if (!visited.Add (addinNode))
					continue;

				var node = currentPosition.Clone ().AddChild ();
				node.SetValue (labelField, addinNode.Label);

				var childDepth = BuildTree (node, addinNode.Children, visited, currentDepth + 1);
				maxDepth = Math.Max (maxDepth, childDepth);
			}

			return maxDepth;
		}

		List<AddinNode> MakeDependencyTree ()
		{
			var cache = new Dictionary<Addin, AddinNode> ();
			var roots = new List<AddinNode> ();

			foreach (var addin in AddinManager.Registry.GetAllAddins ()) {
				var addinNode = GetOrCreateNode (addin);

				if (addin.Description.IsRoot)
					roots.Add (addinNode);
			}

			foreach (var kvp in cache) {
				var addin = kvp.Key;
				var addinNode = kvp.Value;

				// TODO: handle optional dependencies and other modules
				foreach (Dependency dep in addin.Description.MainModule.Dependencies) {
					if (dep is AddinDependency adep) {
						string adepid = Addin.GetFullId (addin.Namespace, adep.AddinId, adep.Version);

						var addinDep = AddinManager.Registry.GetAddin (adepid);

						cache [addinDep].Children.Add (addinNode);
					}
				}
			}

			return roots;

			AddinNode GetOrCreateNode (Addin addin)
			{
				if (!cache.TryGetValue (addin, out var addinNode)) {
					var addinLabel = Addin.GetIdName (addin.Id);
					cache [addin] = addinNode = new AddinNode (addinLabel);
				}
				return addinNode;
			}
		}

		class AddinNode : IEquatable<AddinNode>
		{
			public HashSet<AddinNode> Children { get; } = new HashSet<AddinNode> ();
			public string Label { get; }

			public AddinNode (string label)
			{
				Label = label;
			}

			public bool Equals (AddinNode other)
			{
				return Label == other.Label;
			}
		}
	}
}
