//
// PackageDependenciesNodeBuilder.cs
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
using System.Collections;
using MonoDevelop.DotNetCore.Commands;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.DotNetCore.NodeBuilders
{
	class PackageDependenciesNodeBuilder : TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(PackageDependenciesNode); }
		}

		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return PackageDependenciesNode.NodeName;
		}

		public override Type CommandHandlerType {
			get { return typeof(PackageDependenciesNodeCommandHandler); }
		}

		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			var node = (PackageDependenciesNode)dataObject;
			nodeInfo.Label = node.GetLabel ();
			nodeInfo.SecondaryLabel = node.GetSecondaryLabel ();
			nodeInfo.Icon = Context.GetIcon (node.Icon);
			nodeInfo.ClosedIcon = Context.GetIcon (node.ClosedIcon);
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			var node = (PackageDependenciesNode)dataObject;
			return node.HasChildNodes ();
		}

		public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
			var dependenciesNode = (PackageDependenciesNode)dataObject;
			AddChildren (treeBuilder, dependenciesNode.GetChildNodes ());
		}

		protected virtual void AddChildren (ITreeBuilder treeBuilder, IEnumerable dataObjects)
		{
			treeBuilder.AddChildren (dataObjects);
		}
	}
}
