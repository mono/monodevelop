//
// BuildIntegratedProjectSystem.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// based on NuGet.Clients
// src/NuGet.Clients/PackageManagement.VisualStudio/ProjectSystems/BuildIntegratedProjectSystem.cs
//
// Copyright (c) 2016 Xamarin Inc.
// Copyright (c) .NET Foundation. All rights reserved.
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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.PackageManagement;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.ProjectManagement.Projects;
using NuGet.ProjectModel;

namespace MonoDevelop.PackageManagement
{
	internal class ProjectJsonBuildIntegratedProjectSystem
		: ProjectJsonBuildIntegratedNuGetProject, IBuildIntegratedNuGetProject, IHasDotNetProject
	{
		DotNetProjectProxy dotNetProject;
		PackageManagementEvents packageManagementEvents;
		VersionFolderPathResolver packagePathResolver;

		public ProjectJsonBuildIntegratedProjectSystem (
			string jsonConfigPath,
			string msbuildProjectFilePath,
			DotNetProject dotNetProject,
			IMSBuildNuGetProjectSystem msbuildProjectSystem,
			string uniqueName,
			ISettings settings)
			: base (jsonConfigPath, msbuildProjectFilePath, msbuildProjectSystem)
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

		public override Task<bool> ExecuteInitScriptAsync (PackageIdentity identity, string packageInstallPath, INuGetProjectContext projectContext, bool throwOnFailure)
		{
			// Not supported. This gets called for every NuGet package
			// even if they do not have an init.ps1 so do not report this.
			return Task.FromResult (false);
		}

		public override Task PostProcessAsync (INuGetProjectContext nuGetProjectContext, System.Threading.CancellationToken token)
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

		public override Task<IReadOnlyList<ProjectRestoreReference>> GetDirectProjectReferencesAsync (DependencyGraphCacheContext context)
		{
			return Runtime.RunInMainThread (() => {
				return GetDirectProjectReferences (dotNetProject.DotNetProject, context.Logger);
			});
		}

		/// <summary>
		/// Returns the closure of all project to project references below this project.
		/// The code is a port of src/NuGet.Clients/PackageManagement.VisualStudio/IDE/VSProjectReferenceUtility.cs
		/// </summary>
		static IReadOnlyList<ProjectRestoreReference> GetDirectProjectReferences (
			DotNetProject project,
			ILogger log)
		{
			var results = new List<ProjectRestoreReference>();

			// Verify ReferenceOutputAssembly
			var excludedProjects = GetExcludedReferences (project);
			bool hasMissingReferences = false;

			// find all references in the project
			foreach (var childReference in project.References) {
				try {
					if (!childReference.IsValid) {
						// Skip missing references and show a warning
						hasMissingReferences = true;
						continue;
					}

					// Skip missing references
					DotNetProject sourceProject = null;
					if (childReference.ReferenceType == ReferenceType.Project) {
						sourceProject = childReference.ResolveProject (project.ParentSolution) as DotNetProject;
					}

					// Skip missing references
					if (sourceProject != null) {
						if (sourceProject.IsShared) {
							// Skip this shared project
							continue;
						}

						string childProjectPath = sourceProject.FileName;

						// Skip projects which have ReferenceOutputAssembly=false
						if (!string.IsNullOrEmpty (childProjectPath) &&
						    !excludedProjects.Contains (childProjectPath, StringComparer.OrdinalIgnoreCase)) {
							var restoreReference = new ProjectRestoreReference () {
								ProjectPath = childProjectPath,
								ProjectUniqueName = childProjectPath
							};

							results.Add(restoreReference);
						}
					}
				} catch (Exception ex) {
					// Exceptions are expected in some scenarios for native projects,
					// ignore them and show a warning
					hasMissingReferences = true;

					log.LogDebug (ex.ToString ());

					LoggingService.LogError ("Unable to find project dependencies.", ex);
				}
			}

			if (hasMissingReferences) {
				// Log a warning message once per project
				// This warning contains only the names of the root project and the project with the
				// broken reference. Attempting to display more details on the actual reference
				// that has the problem may lead to another exception being thrown.
				var warning = string.Format (
					"Failed to resolve all project references. The package restore result for '{0}' or a dependant project may be incomplete.",
					project.Name);

				log.LogWarning (warning);
			}

			return results;
		}

		/// <summary>
		/// Get the unique names of all references which have ReferenceOutputAssembly set to false.
		/// </summary>
		static List<string> GetExcludedReferences (DotNetProject project)
		{
			var excludedReferences = new List<string> ();

			foreach (var reference in project.References) {
				// 1. Verify that this is a project reference
				// 2. Check that it is valid and resolved
				// 3. Follow the reference to the DotNetProject and get the unique name
				if (!reference.ReferenceOutputAssembly &&
				    reference.IsValid &&
				    reference.ReferenceType == ReferenceType.Project) {

					var sourceProject = reference.ResolveProject (project.ParentSolution);
					if (sourceProject != null) {
						string childPath = sourceProject.FileName;
						excludedReferences.Add (childPath);
					}
				}
			}

			return excludedReferences;
		}

		public void NotifyProjectReferencesChanged (bool includeTransitiveProjectReferences)
		{
			Runtime.AssertMainThread ();

			dotNetProject.RefreshProjectBuilder ();
			dotNetProject.DotNetProject.NotifyModified ("References");
		}
	}
}
