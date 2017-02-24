//
// DependenciesNodeBuilder.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.DotNetCore.NodeBuilders
{
	public class DependenciesNodeBuilder : TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(DependenciesNode); }
		}

		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return DependenciesNode.NodeName;
		}

		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			var node = (DependenciesNode)dataObject;
			nodeInfo.Label = node.GetLabel ();
			nodeInfo.SecondaryLabel = node.GetSecondaryLabel ();
			nodeInfo.Icon = Context.GetIcon (node.Icon);
			nodeInfo.ClosedIcon = Context.GetIcon (node.ClosedIcon);
		}

		public override int GetSortIndex (ITreeNavigator node)
		{
			return -600;
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}

		public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
			var node = (DependenciesNode)dataObject;
			node.PackageDependencyCache.Refresh ();

			var packagesNode = new PackageDependenciesNode (node);
			if (packagesNode.HasChildNodes ())
				treeBuilder.AddChild (packagesNode);

			var sdkNode = new SdkDependenciesNode (node);
			treeBuilder.AddChild (sdkNode);

			var assembliesNode = new AssemblyDependenciesNode (node.Project);
			if (assembliesNode.HasChildNodes ())
				treeBuilder.AddChild (assembliesNode);

			var projectsNode = new ProjectDependenciesNode (node.Project);
			if (projectsNode.HasChildNodes ())
				treeBuilder.AddChild (projectsNode);
		}
	}
}
