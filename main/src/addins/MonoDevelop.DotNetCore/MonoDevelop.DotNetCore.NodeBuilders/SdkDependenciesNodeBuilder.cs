//
// SdkDependenciesNodeBuilder.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.DotNetCore.NodeBuilders
{
	class SdkDependenciesNodeBuilder : TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(SdkDependenciesNode); }
		}

		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return SdkDependenciesNode.NodeName;
		}

		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			var node = (SdkDependenciesNode)dataObject;
			nodeInfo.Label = node.GetLabel ();
			nodeInfo.SecondaryLabel = node.GetSecondaryLabel ();
			nodeInfo.Icon = Context.GetIcon (node.Icon);
			nodeInfo.ClosedIcon = Context.GetIcon (node.ClosedIcon);
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}

		public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
			var dependenciesNode = (SdkDependenciesNode)dataObject;
			if (dependenciesNode.LoadedDependencies) {
				AddLoadedDependencyNodes (treeBuilder, dependenciesNode);
			} else {
				treeBuilder.AddChildren (dependenciesNode.GetDefaultNodes ());
			}
		}

		void AddLoadedDependencyNodes (ITreeBuilder treeBuilder, SdkDependenciesNode dependenciesNode)
		{
			var frameworkNodes = GetTargetFrameworkNodes (dependenciesNode).ToList ();
			if (frameworkNodes.Count > 1) {
				treeBuilder.AddChildren (frameworkNodes);
			} else if (frameworkNodes.Any ()) {
				var frameworkNode = frameworkNodes.First ();
				treeBuilder.AddChildren (frameworkNode.GetDependencyNodes ());
			} else {
				// Projects sometimes return no dependencies from MSBuild initially so
				// add the default nodes until the dependencies are updated.
				treeBuilder.AddChildren (dependenciesNode.GetDefaultNodes ());
			}
		}

		IEnumerable<TargetFrameworkNode> GetTargetFrameworkNodes (object dataObject)
		{
			var dependenciesNode = (SdkDependenciesNode)dataObject;
			return dependenciesNode.GetTargetFrameworkNodes ();
		}

		public override void OnNodeAdded (object dataObject)
		{
			var dependenciesNode = (SdkDependenciesNode)dataObject;
			dependenciesNode.ParentNode.PackageDependencyCache.PackageDependenciesChanged += OnPackageDependenciesChanged;
		}

		public override void OnNodeRemoved (object dataObject)
		{
			var dependenciesNode = (SdkDependenciesNode)dataObject;
			dependenciesNode.ParentNode.PackageDependencyCache.PackageDependenciesChanged -= OnPackageDependenciesChanged;
		}

		void OnPackageDependenciesChanged (object sender, EventArgs e)
		{
			var cache = (PackageDependencyNodeCache)sender;
			var project = cache.Project;
			ITreeBuilder builder = Context.GetTreeBuilder (project);
			if (builder == null)
				return;

			if (builder.MoveToChild (DependenciesNode.NodeName, typeof(DependenciesNode))) {
				if (builder.MoveToChild (SdkDependenciesNode.NodeName, typeof(SdkDependenciesNode))) {
					builder.UpdateAll ();
				}
			}
		}
	}
}
