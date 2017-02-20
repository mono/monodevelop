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
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.PackageManagement.Commands;
using MonoDevelop.Projects;
using NuGet.Packaging;
using NuGet.Packaging.Core;

namespace MonoDevelop.PackageManagement.NodeBuilders
{
	internal class ProjectPackagesFolderNodeBuilder : TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(ProjectPackagesFolderNode); }
		}

		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ProjectPackagesFolderNode.NodeName;
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

		public override int GetSortIndex (ITreeNavigator node)
		{
			return -500;
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return GetPackageReferencesNodes (dataObject).Any ();
		}

		IEnumerable<PackageReferenceNode> GetPackageReferencesNodes (object dataObject)
		{
			var projectPackagesNode = (ProjectPackagesFolderNode)dataObject;

			List<PackageReferenceNode> nodes = projectPackagesNode.GetPackageReferencesNodes ().ToList ();

			foreach (IInstallNuGetPackageAction installAction in GetPendingInstallActions (projectPackagesNode.DotNetProject)) {
				if (!nodes.Any (node => node.Id == installAction.PackageId)) {
					nodes.Add (CreatePackageReferenceNode (projectPackagesNode, installAction));
				}
			}

			foreach (PackageReferenceNode node in nodes) {
				yield return node;
			}
		}

		IEnumerable<IInstallNuGetPackageAction> GetPendingInstallActions (DotNetProject project)
		{
			return PackageManagementServices.BackgroundPackageActionRunner.PendingInstallActionsForProject (project);
		}

		PackageReferenceNode CreatePackageReferenceNode (ProjectPackagesFolderNode parentNode, IInstallNuGetPackageAction installAction)
		{
			return new PackageReferenceNode (
				parentNode,
				new PackageReference (new PackageIdentity (installAction.PackageId, installAction.Version), null),
				false,
				true);
		}

		public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
			treeBuilder.AddChildren (GetPackageReferencesNodes (dataObject));
		}

		public override void OnNodeAdded (object dataObject)
		{
			var projectPackagesNode = (ProjectPackagesFolderNode)dataObject;
			projectPackagesNode.PackageReferencesChanged += OnPackageReferencesChanged;
		}

		public override void OnNodeRemoved (object dataObject)
		{
			var projectPackagesNode = (ProjectPackagesFolderNode)dataObject;
			projectPackagesNode.PackageReferencesChanged -= OnPackageReferencesChanged;
		}

		void OnPackageReferencesChanged (object sender, EventArgs e)
		{
			var projectPackagesNode = (ProjectPackagesFolderNode)sender;
			ITreeBuilder builder = Context.GetTreeBuilder (projectPackagesNode);
			if (builder != null) {
				builder.UpdateAll ();
				builder.MoveToParent ();

				if (builder.MoveToChild ("References", typeof (ProjectReferenceCollection))) {
					builder.UpdateAll ();
				}
			}
		}
	}
}

