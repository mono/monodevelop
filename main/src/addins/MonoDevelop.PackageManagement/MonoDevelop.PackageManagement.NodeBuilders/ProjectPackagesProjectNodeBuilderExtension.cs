﻿//
// ProjectPackagesNodeBuilderExtension.cs
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
using System.Linq;
using ICSharpCode.PackageManagement;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Projects;
using NuGet;

namespace MonoDevelop.PackageManagement.NodeBuilders
{
	public class ProjectPackagesProjectNodeBuilderExtension : NodeBuilderExtension
	{
		IPackageManagementEvents packageManagementEvents;

		public ProjectPackagesProjectNodeBuilderExtension ()
		{
			packageManagementEvents = PackageManagementServices.PackageManagementEvents;

			packageManagementEvents.PackagesRestored += PackagesRestored;
			packageManagementEvents.PackageOperationsStarting += PackageOperationsStarting;
			packageManagementEvents.PackageOperationError += PackageOperationError;

			FileService.FileChanged += FileChanged;
		}

		void PackagesRestored (object sender, EventArgs e)
		{
			RefreshAllChildNodes ();
		}

		void PackageOperationsStarting (object sender, EventArgs e)
		{
			RefreshAllChildNodes ();
		}

		void PackageOperationError (object sender, EventArgs e)
		{
			RefreshAllChildNodes ();
		}

		void RefreshAllChildNodes ()
		{
			DispatchService.GuiDispatch (() => {
				foreach (IDotNetProject project in PackageManagementServices.Solution.GetDotNetProjects ()) {
					RefreshChildNodes (project.DotNetProject);
				}
			});
		}

		void RefreshChildNodes (IPackageManagementProject project)
		{
			DispatchService.GuiDispatch (() => RefreshChildNodes (project.DotNetProject));
		}

		void RefreshChildNodes (DotNetProject project)
		{
			ITreeBuilder builder = Context.GetTreeBuilder (project);
			if (builder != null) {
				builder.UpdateChildren ();
			}
		}

		public override void Dispose ()
		{
			FileService.FileChanged -= FileChanged;

			packageManagementEvents.PackagesRestored -= PackagesRestored;
			packageManagementEvents.PackageOperationsStarting -= PackageOperationsStarting;
			packageManagementEvents.PackageOperationError -= PackageOperationError;
		}

		public override bool CanBuildNode (Type dataType)
		{
			return typeof(DotNetProject).IsAssignableFrom (dataType);
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return ProjectHasPackages (dataObject);
		}

		bool ProjectHasPackages (object dataObject)
		{
			var project = (DotNetProject) dataObject;
			return project.HasPackages () || ProjectHasPendingPackages (project);
		}

		bool ProjectHasPendingPackages (DotNetProject project)
		{
			return PackageManagementServices
				.BackgroundPackageActionRunner
				.PendingInstallActionsForProject (project)
				.Any ();
		}

		public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
			var project = (DotNetProject)dataObject;
			if (ProjectHasPackages (project)) {
				treeBuilder.AddChild (new ProjectPackagesFolderNode (project));
			}
		}

		void FileChanged (object sender, FileEventArgs e)
		{
			if (IsPackagesConfigFileChanged (e)) {
				RefreshAllChildNodes ();
			}
		}

		bool IsPackagesConfigFileChanged (FileEventArgs fileEventArgs)
		{
			return fileEventArgs.Any (file => IsPackagesConfigFileName (file.FileName));
		}

		bool IsPackagesConfigFileName (FilePath filePath)
		{
			if (filePath == null) {
				return false;
			}

			return Constants.PackageReferenceFile.Equals (filePath.FileName, StringComparison.OrdinalIgnoreCase);
		}
	}
}

