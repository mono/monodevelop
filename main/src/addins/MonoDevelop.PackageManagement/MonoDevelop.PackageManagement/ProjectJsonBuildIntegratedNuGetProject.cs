//
// ProjectJsonBuildIntegratedNuGetProject.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using NuGet.Configuration;
using NuGet.PackageManagement;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.ProjectManagement.Projects;

namespace MonoDevelop.PackageManagement
{
	internal class ProjectJsonBuildIntegratedNuGetProject
		: ProjectJsonNuGetProject, IBuildIntegratedNuGetProject, IHasDotNetProject
	{
		DotNetProjectProxy dotNetProject;
		PackageManagementEvents packageManagementEvents;
		VersionFolderPathResolver packagePathResolver;

		public ProjectJsonBuildIntegratedNuGetProject (
			string jsonConfigPath,
			string msbuildProjectFilePath,
			DotNetProject dotNetProject,
			ISettings settings)
			: base (jsonConfigPath, msbuildProjectFilePath)
		{
			this.dotNetProject = new DotNetProjectProxy (dotNetProject);
			packageManagementEvents = (PackageManagementEvents)PackageManagementServices.PackageManagementEvents;

			string path = SettingsUtility.GetGlobalPackagesFolder (settings);
			packagePathResolver = new VersionFolderPathResolver (path);
		}

		public Task SaveProject ()
		{
			return dotNetProject.SaveAsync ();
		}

		public DotNetProjectProxy Project {
			get { return dotNetProject; }
		}

		public override Task PostProcessAsync (INuGetProjectContext nuGetProjectContext, CancellationToken token)
		{
			Runtime.RunInMainThread (() => {
				dotNetProject.DotNetProject.NotifyModified ("References");
			});

			packageManagementEvents.OnFileChanged (JsonConfigPath);

			return base.PostProcessAsync (nuGetProjectContext, token);
		}

		public void OnAfterExecuteActions (IEnumerable<NuGetProjectAction> actions)
		{
			ProcessActions (actions, OnPackageInstalled, OnPackageUninstalled);
		}

		public void OnBeforeUninstall (IEnumerable<NuGetProjectAction> actions)
		{
			ProcessActions (actions, identity => {}, OnPackageUninstalling);
		}

		void ProcessActions (
			IEnumerable<NuGetProjectAction> actions,
			Action<PackageIdentity> installAction,
			Action<PackageIdentity> uninstallAction)
		{
			foreach (var action in actions) {
				if (action.NuGetProjectActionType == NuGetProjectActionType.Install) {
					installAction (action.PackageIdentity);
				} else if (action.NuGetProjectActionType == NuGetProjectActionType.Uninstall) {
					uninstallAction (action.PackageIdentity);
				}
			}
		}

		void OnPackageInstalled (PackageIdentity identity)
		{
			var eventArgs = CreatePackageEventArgs (identity);
			packageManagementEvents.OnPackageInstalled (dotNetProject, eventArgs);
		}

		PackageEventArgs CreatePackageEventArgs (PackageIdentity identity)
		{
			string installPath = packagePathResolver.GetInstallPath (identity.Id, identity.Version);
			return new PackageEventArgs (this, identity, installPath);
		}

		void OnPackageUninstalling (PackageIdentity identity)
		{
			var eventArgs = CreatePackageEventArgs (identity);
			packageManagementEvents.OnPackageUninstalling (dotNetProject, eventArgs);
		}

		void OnPackageUninstalled (PackageIdentity identity)
		{
			var eventArgs = CreatePackageEventArgs (identity);
			packageManagementEvents.OnPackageUninstalled (dotNetProject, eventArgs);
		}

		public void NotifyProjectReferencesChanged (bool includeTransitiveProjectReferences)
		{
			Runtime.AssertMainThread ();

			dotNetProject.RefreshProjectBuilder ();
			dotNetProject.DotNetProject.NotifyModified ("References");
		}
	}
}
