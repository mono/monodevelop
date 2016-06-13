//
// ProjectPackagesFolderNodeBuilderExtension.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using Gdk;
using MonoDevelop.PackageManagement;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.PackageManagement.Commands;
using MonoDevelop.Projects;
using NuGet;

namespace MonoDevelop.PackageManagement.NodeBuilders
{
	internal class ProjectPackagesFolderNodeBuilder : TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(ProjectPackagesFolderNode); }
		}

		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return "Packages";
		}

		public override Type CommandHandlerType {
			get { return typeof(ProjectPackagesFolderNodeCommandHandler); }
		}

		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			var node = (ProjectPackagesFolderNode)dataObject;
			nodeInfo.Label = node.GetLabel ();
			nodeInfo.SecondaryLabel = node.GetSecondaryLabel ();
			nodeInfo.Icon = Context.GetIcon (node.Icon);
			nodeInfo.ClosedIcon = Context.GetIcon (node.ClosedIcon);
		}

		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			if (otherNode.DataItem is ProjectReferenceCollection) {
				return 1;
			}
			return -1;
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return GetPackageReferencesNodes (dataObject).Any ();
		}

		IEnumerable<PackageReferenceNode> GetPackageReferencesNodes (object dataObject)
		{
			var projectPackagesNode = (ProjectPackagesFolderNode)dataObject;

			List<PackageReferenceNode> nodes = projectPackagesNode.GetPackageReferencesNodes ().ToList ();

			foreach (InstallPackageAction installAction in GetPendingInstallActions (projectPackagesNode.DotNetProject)) {
				if (!nodes.Any (node => node.Id == installAction.GetPackageId ())) {
					nodes.Add (CreatePackageReferenceNode (projectPackagesNode, installAction));
				}
			}

			foreach (PackageReferenceNode node in nodes) {
				yield return node;
			}
		}

		IEnumerable<InstallPackageAction> GetPendingInstallActions (DotNetProject project)
		{
			return PackageManagementServices.BackgroundPackageActionRunner.PendingInstallActionsForProject (project);
		}

		PackageReferenceNode CreatePackageReferenceNode (ProjectPackagesFolderNode parentNode, InstallPackageAction installAction)
		{
			return new PackageReferenceNode (
				parentNode,
				new PackageReference (installAction.GetPackageId (), installAction.GetPackageVersion (), null, null, false),
				false,
				true);
		}

		public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
			treeBuilder.AddChildren (GetPackageReferencesNodes (dataObject));
		}
	}
}

