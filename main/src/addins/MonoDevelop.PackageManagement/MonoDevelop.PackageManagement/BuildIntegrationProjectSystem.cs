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
	internal class BuildIntegratedProjectSystem : BuildIntegratedNuGetProject, IBuildIntegratedNuGetProject
	{
		DotNetProjectProxy dotNetProject;
		PackageManagementEvents packageManagementEvents;
		VersionFolderPathResolver packagePathResolver;

		public BuildIntegratedProjectSystem (
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

		public override Task<IReadOnlyList<ExternalProjectReference>> GetProjectReferenceClosureAsync (ExternalProjectReferenceContext context)
		{
			return Runtime.RunInMainThread (() => {
				return GetExternalProjectReferenceClosureAsync (context);
			});
		}

		/// <summary>
		/// Returns the closure of all project to project references below this project.
		/// The code is a port of src/NuGet.Clients/PackageManagement.VisualStudio/ProjectSystems/BuildIntegratedProjectSystem.cs
		/// </summary>
		Task<IReadOnlyList<ExternalProjectReference>> GetExternalProjectReferenceClosureAsync (ExternalProjectReferenceContext context)
		{
			var logger = context.Logger;
			var cache = context.Cache;

			var results = new HashSet<ExternalProjectReference> ();

			// projects to walk - DFS
			var toProcess = new Stack<DotNetProjectReference> ();

			// keep track of found projects to avoid duplicates
			string rootProjectPath = dotNetProject.FileName;

			// start with the current project
			toProcess.Push (new DotNetProjectReference (dotNetProject.DotNetProject, rootProjectPath));

			var uniqueNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			// continue walking all project references until we run out
			while (toProcess.Count > 0) {
				var reference = toProcess.Pop ();

				// Find the path of the current project
				string projectFileFullPath = reference.Path;
				var project = reference.Project;

				if (string.IsNullOrEmpty(projectFileFullPath) || !uniqueNames.Add (projectFileFullPath)) {
					// This has already been processed or does not exist
					continue;
				}

				IReadOnlyList<ExternalProjectReference> cacheReferences;
				if (cache.TryGetValue (projectFileFullPath, out cacheReferences)) {
					// The cached value contains the entire closure, add it to the results and skip
					// all child references.
					results.UnionWith (cacheReferences);
				} else {
					// Get direct references
					var projectResult = GetDirectProjectReferences (
						reference.Project,
						projectFileFullPath,
						context,
						rootProjectPath);

					// Add results to the closure
					results.UnionWith (projectResult.Processed);

					// Continue processing
					foreach (var item in projectResult.ToProcess) {
						toProcess.Push (item);
					}
				}
			}

			// Cache the results for this project and every child project which has not been cached
			foreach (var project in results) {
				if (!context.Cache.ContainsKey (project.UniqueName)) {
					var closure = BuildIntegratedRestoreUtility.GetExternalClosure (project.UniqueName, results);

					context.Cache.Add(project.UniqueName, closure.ToList ());
				}
			}

			var result = cache[rootProjectPath];
			return Task.FromResult (result);
		}

		/// <summary>
		/// Get only the direct dependencies from a project
		/// </summary>
		private DirectReferences GetDirectProjectReferences(
			DotNetProject project,
			string projectFileFullPath,
			ExternalProjectReferenceContext context,
			string rootProjectPath)
		{
			var logger = context.Logger;
			var cache = context.Cache;

			var result = new DirectReferences ();

			// Find a project.json in the project
			// This checks for files on disk to match how BuildIntegratedProjectSystem checks at creation time.
			// NuGet.exe also uses disk paths and not the project file.
			var projectName = Path.GetFileNameWithoutExtension (projectFileFullPath);

			var projectDirectory = Path.GetDirectoryName (projectFileFullPath);

			// Check for projectName.project.json and project.json
			string jsonConfigItem =
				ProjectJsonPathUtilities.GetProjectConfigPath (
					directoryPath: projectDirectory,
					projectName: projectName);

			var hasProjectJson = true;

			// Verify the file exists, otherwise clear it
			if (!File.Exists(jsonConfigItem)) {
				jsonConfigItem = null;
				hasProjectJson = false;
			}

			// Verify ReferenceOutputAssembly
			var excludedProjects = GetExcludedReferences (project);

			var childReferences = new HashSet<string> (StringComparer.Ordinal);
			var hasMissingReferences = false;

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

					if (sourceProject != null) {

						string childName = sourceProject.FileName;

						// Skip projects which have ReferenceOutputAssembly=false
						if (!string.IsNullOrEmpty (childName)
						    && !excludedProjects.Contains (childName, StringComparer.OrdinalIgnoreCase)) {
							childReferences.Add (childName);

							result.ToProcess.Add (new DotNetProjectReference (sourceProject, childName));
						}
					} else if (hasProjectJson) {
						// SDK references do not have a SourceProject or child references, 
						// but they can contain project.json files, and should be part of the closure
						// SDKs are not projects, only the project.json name is checked here
						//var possibleSdkPath = childReference.Path;

						//if (!string.IsNullOrEmpty (possibleSdkPath)
						//    && !possibleSdkPath.EndsWith (".dll", StringComparison.OrdinalIgnoreCase)
						//    && Directory.Exists(possibleSdkPath)) {
						//	var possibleProjectJson = Path.Combine (
						//		possibleSdkPath,
						//		ProjectJsonPathUtilities.ProjectConfigFileName);

						//	if (File.Exists (possibleProjectJson)) {
						//		childReferences.Add (possibleProjectJson);

						//		// add the sdk to the results here
						//		result.Processed.Add (new ExternalProjectReference (
						//			possibleProjectJson,
						//			childReference.Name,
						//			possibleProjectJson,
						//			msbuildProjectPath: null,
						//			projectReferences: Enumerable.Empty<string> ()));
						//	}
						//}
					}
				} catch (Exception ex) {
					// Exceptions are expected in some scenarios for native projects,
					// ignore them and show a warning
					hasMissingReferences = true;

					logger.LogDebug (ex.Message);

					LoggingService.LogError ("Unable to find project closure.", ex);
				}
			}

			if (hasMissingReferences) {
				// Log a warning message once per project
				// This warning contains only the names of the root project and the project with the
				// broken reference. Attempting to display more details on the actual reference 
				// that has the problem may lead to another exception being thrown.
				var warning = GettextCatalog.GetString ("Failed to resolve all project references for '{0}'. The package restore result for '{1}' may be incomplete.",
					projectName,
					rootProjectPath);

				logger.LogWarning (warning);
			}

			// Only set a package spec project name if a package spec exists
			var packageSpecProjectName = jsonConfigItem == null ? null : projectName;

			// Add the parent project to the results
			result.Processed.Add (new ExternalProjectReference (
				projectFileFullPath,
				packageSpecProjectName,
				jsonConfigItem,
				projectFileFullPath,
				childReferences));

			return result;
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


		/// <summary>
		/// Top level references
		/// </summary>
		class DirectReferences
		{
			public HashSet<DotNetProjectReference> ToProcess { get; } = new HashSet<DotNetProjectReference> ();

			public HashSet<ExternalProjectReference> Processed { get; } = new HashSet<ExternalProjectReference> ();
		}

		/// <summary>
		/// Holds the full path to a project and the project.
		/// </summary>
		private class DotNetProjectReference : IEquatable<DotNetProjectReference>, IComparable<DotNetProjectReference>
		{
			public DotNetProjectReference (DotNetProject project, string path)
			{
				Project = project;
				Path = path;
			}

			public DotNetProject Project { get; }

			public string Path { get; }

			public bool Equals (DotNetProjectReference other)
			{
				return StringComparer.Ordinal.Equals (Path, other.Path);
			}

			public int CompareTo (DotNetProjectReference other)
			{
				return StringComparer.Ordinal.Compare (Path, other.Path);
			}

			public override int GetHashCode ()
			{
				return StringComparer.Ordinal.GetHashCode (Path);
			}

			public override bool Equals (object obj)
			{
				return Equals (obj as DotNetProjectReference);
			}
		}
	}
}
