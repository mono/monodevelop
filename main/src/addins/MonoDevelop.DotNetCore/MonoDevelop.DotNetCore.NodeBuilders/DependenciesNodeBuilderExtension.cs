//
// DependenciesNodeBuilderExtension.cs
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
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.PackageManagement;
using MonoDevelop.Projects;

namespace MonoDevelop.DotNetCore.NodeBuilders
{
	class DependenciesNodeBuilderExtension : NodeBuilderExtension
	{
		IPackageManagementEvents packageManagementEvents;

		protected override void Initialize ()
		{
			base.Initialize ();

			packageManagementEvents = PackageManagementServices.PackageManagementEvents;
			packageManagementEvents.PackageOperationsFinished += PackageOperationsFinished;
			packageManagementEvents.UpdatedPackagesAvailable += UpdatePackagesAvailable;

			IdeApp.Workspace.ReferenceAddedToProject += OnReferencesChanged;
			IdeApp.Workspace.ReferenceRemovedFromProject += OnReferencesChanged;
		}

		public override void Dispose ()
		{
			packageManagementEvents.PackageOperationsFinished -= PackageOperationsFinished;
			packageManagementEvents.UpdatedPackagesAvailable -= UpdatePackagesAvailable;

			IdeApp.Workspace.ReferenceAddedToProject -= OnReferencesChanged;
			IdeApp.Workspace.ReferenceRemovedFromProject -= OnReferencesChanged;

			base.Dispose ();
		}

		public override bool CanBuildNode (Type dataType)
		{
			return typeof(DotNetProject).IsAssignableFrom (dataType);
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			var project = (DotNetProject)dataObject;
			return project.IsDotNetCoreProject ();
		}

		void PackageOperationsFinished (object sender, EventArgs e)
		{
			RefreshAllChildNodes (packages: true);
		}

		public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
			var project = (DotNetProject)dataObject;
			if (!project.IsDotNetCoreProject ())
				return;

			var folderNode = new DependenciesNode (project);
			treeBuilder.AddChild (folderNode);
			folderNode.PackageDependencyCache.Refresh ();
		}

		void RefreshAllChildNodes (bool packages = false)
		{
			Runtime.RunInMainThread (() => {
				foreach (DotNetProject project in IdeApp.Workspace.GetAllItems<DotNetProject> ()) {
					if (project.IsDotNetCoreProject ())
						RefreshChildNodes (project, packages);
				}
			}).Ignore ();
		}

		void RefreshChildNodes (DotNetProject project, bool packages)
		{
			ITreeBuilder builder = Context.GetTreeBuilder (project);
			if (builder != null) {
				if (builder.MoveToChild (DependenciesNode.NodeName, typeof (DependenciesNode))) {
					if (packages) {
						var dependenciesNode = (DependenciesNode)builder.DataItem;
						dependenciesNode.PackageDependencyCache.Refresh ();
					} else {
						builder.UpdateAll ();
					}
				}
			}
		}

		void OnReferencesChanged (object sender, ProjectReferenceEventArgs e)
		{
			RefreshAllChildNodes (e.Project as DotNetProject);
		}

		void RefreshAllChildNodes (DotNetProject project)
		{
			if (project == null)
				return;

			Runtime.RunInMainThread (() => {
				if (project.IsDotNetCoreProject ())
					RefreshChildNodes (project, packages: false);
			}).Ignore ();
		}

		void UpdatePackagesAvailable (object sender, EventArgs e)
		{
			RefreshAllChildNodes ();
		}
	}
}
