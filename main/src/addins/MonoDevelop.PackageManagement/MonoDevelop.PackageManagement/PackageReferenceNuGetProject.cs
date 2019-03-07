//
// PackageReferenceNuGetProject.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using NuGet.Commands;
using NuGet.Frameworks;
using NuGet.PackageManagement;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.ProjectManagement.Projects;
using NuGet.ProjectModel;
using NuGet.Versioning;

namespace MonoDevelop.PackageManagement
{
	/// <summary>
	/// Supports projects that use PackageReference MSBuild items but do not use a SDK style
	/// project used by .NET Core projects.
	/// </summary>
	class PackageReferenceNuGetProject : BuildIntegratedNuGetProject, IBuildIntegratedNuGetProject, IHasDotNetProject
	{
		DotNetProject project;
		ConfigurationSelector configuration;
		PackageManagementEvents packageManagementEvents;
		string msbuildProjectPath;
		string projectName;
		bool reevaluationRequired;

		public PackageReferenceNuGetProject (DotNetProject project, ConfigurationSelector configuration)
			: this (project, configuration, (PackageManagementEvents)PackageManagementServices.PackageManagementEvents)
		{
		}

		public PackageReferenceNuGetProject (
			DotNetProject project,
			ConfigurationSelector configuration,
			PackageManagementEvents packageManagementEvents)
		{
			this.project = project;
			this.configuration = configuration;
			this.packageManagementEvents = packageManagementEvents;

			var targetFramework = NuGetFramework.Parse (project.TargetFramework.Id.ToString ());

			InternalMetadata.Add (NuGetProjectMetadataKeys.TargetFramework, targetFramework);
			InternalMetadata.Add (NuGetProjectMetadataKeys.Name, project.Name);
			InternalMetadata.Add (NuGetProjectMetadataKeys.FullPath, project.BaseDirectory);

			msbuildProjectPath = project.FileName;
			projectName = project.Name;
		}

		public IDotNetProject Project {
			get { return new DotNetProjectProxy (project); }
		}

		internal DotNetProject DotNetProject {
			get { return project; }
		}

		public override string ProjectName {
			get { return projectName; }
		}

		public override string MSBuildProjectPath {
			get { return msbuildProjectPath; }
		}

		public static bool CanCreate (DotNetProject project)
		{
			return project.HasPackageReferences () || project.HasPackageReferenceRestoreProjectStyle ();
		}

		public static NuGetProject Create (DotNetProject project)
		{
			return Create (project, ConfigurationSelector.Default);
		}

		public static NuGetProject Create (DotNetProject project, ConfigurationSelector configuration)
		{
			if (CanCreate (project))
				return new PackageReferenceNuGetProject (project, configuration);

			return null;
		}

		public virtual Task SaveProject ()
		{
			return project.SaveAsync (new ProgressMonitor ());
		}

		public override Task<IEnumerable<PackageReference>> GetInstalledPackagesAsync (CancellationToken token)
		{
			return Task.FromResult (GetPackageReferences ());
		}

		IEnumerable<PackageReference> GetPackageReferences ()
		{
			return project.Items.OfType<ProjectPackageReference> ()
				.Select (projectItem => projectItem.CreatePackageReference ())
				.ToList ();
		}

		public async override Task<bool> InstallPackageAsync (
			string packageId,
			VersionRange range,
			INuGetProjectContext nuGetProjectContext,
			BuildIntegratedInstallationContext installationContext,
			CancellationToken token)
		{
			var packageIdentity = new PackageIdentity (packageId, range.MinVersion);

			bool added = await Runtime.RunInMainThread (() => {
				return AddPackageReference (packageIdentity, nuGetProjectContext);
			});

			if (added) {
				await SaveProject ();
			}

			return added;
		}

		bool AddPackageReference (PackageIdentity packageIdentity, INuGetProjectContext context)
		{
			ProjectPackageReference packageReference = project.GetPackageReference (packageIdentity, matchVersion: false);
			if (packageReference?.Equals (packageIdentity, matchVersion: true) == true) {
				context.Log (MessageLevel.Warning, GettextCatalog.GetString ("Package '{0}' already exists in project '{1}'", packageIdentity, project.Name));
				return false;
			}

			if (packageReference != null) {
				packageReference.Version = packageIdentity.Version.ToNormalizedString ();
			} else {
				packageReference = ProjectPackageReference.Create (packageIdentity);
				project.Items.Add (packageReference);
			}

			return true;
		}

		public override async Task<bool> UninstallPackageAsync (
			PackageIdentity packageIdentity,
			INuGetProjectContext nuGetProjectContext,
			CancellationToken token)
		{
			bool removed = await Runtime.RunInMainThread (() => {
				return RemovePackageReference (packageIdentity, nuGetProjectContext);
			});

			if (removed) {
				await SaveProject ();
			}

			return removed;
		}

		bool RemovePackageReference (PackageIdentity packageIdentity, INuGetProjectContext context)
		{
			ProjectPackageReference packageReference = project.GetPackageReference (packageIdentity, matchVersion: false);

			if (packageReference == null) {
				context.Log (MessageLevel.Warning, GettextCatalog.GetString ("Package '{0}' does not exist in project '{1}'", packageIdentity.Id, project.Name));
				return false;
			}

			project.Items.Remove (packageReference);

			return true;
		}

		public override Task<string> GetAssetsFilePathAsync ()
		{
			string assetsFilePath = project.GetNuGetAssetsFilePath ();
			return Task.FromResult (assetsFilePath);
		}

		public override Task<string> GetAssetsFilePathOrNullAsync ()
		{
			return GetAssetsFilePathAsync ();
		}

		public override Task<string> GetCacheFilePathAsync ()
		{
			string cacheFilePath = NoOpRestoreUtilities.GetProjectCacheFilePath (
				project.BaseIntermediateOutputPath,
				msbuildProjectPath);
			return Task.FromResult (cacheFilePath);
		}

		public override async Task<IReadOnlyList<PackageSpec>> GetPackageSpecsAsync (DependencyGraphCacheContext context)
		{
			PackageSpec existingPackageSpec = context.GetExistingProjectPackageSpec (MSBuildProjectPath);
			if (existingPackageSpec != null) {
				return new [] { existingPackageSpec };
			}

			PackageSpec packageSpec = await CreateProjectPackageSpec (context);
			return new [] { packageSpec };
		}

		async Task<PackageSpec> CreateProjectPackageSpec (DependencyGraphCacheContext context)
		{
			DependencyGraphSpec dependencySpec = await MSBuildPackageSpecCreator.GetDependencyGraphSpec (project, configuration, context?.Logger);

			context.AddToCache (dependencySpec);

			PackageSpec spec = dependencySpec.GetProjectSpec (project.FileName);
			if (spec != null)
				return spec;

			throw new InvalidOperationException (GettextCatalog.GetString ("Unable to create package spec for project. '{0}'", project.FileName));
		}

		public override async Task PostProcessAsync (INuGetProjectContext nuGetProjectContext, CancellationToken token)
		{
			await Runtime.RunInMainThread (async () => {
				if (reevaluationRequired) {
					await DotNetProject.ReevaluateProject (new ProgressMonitor ());
				}
				DotNetProject.NotifyModified ("References");
			});

			await base.PostProcessAsync (nuGetProjectContext, token);
		}

		public void OnBeforeUninstall (IEnumerable<NuGetProjectAction> actions)
		{
		}

		public void OnAfterExecuteActions (IEnumerable<NuGetProjectAction> actions)
		{
			reevaluationRequired = actions.Any (action => action.NuGetProjectActionType == NuGetProjectActionType.Install);

			foreach (var action in actions) {
				var eventArgs = new PackageEventArgs (this, action.PackageIdentity, null);
				if (action.NuGetProjectActionType == NuGetProjectActionType.Install) {
					packageManagementEvents.OnPackageInstalled (Project, eventArgs);
				} else if (action.NuGetProjectActionType == NuGetProjectActionType.Uninstall) {
					packageManagementEvents.OnPackageUninstalled (Project, eventArgs);
				}
			}
		}

		public void NotifyProjectReferencesChanged (bool includeTransitiveProjectReferences)
		{
			Runtime.AssertMainThread ();

			DotNetProject.RefreshProjectBuilder ();
			DotNetProject.NotifyModified ("References");
		}

		/// <summary>
		/// Always returns true so the project is re-evaluated after a restore.
		/// This ensures any imports in the generated .nuget.g.targets are
		/// re-evaluated. Without this custom MSBuild targets used by a NuGet package
		/// that was restored into the local NuGet package cache would not be available
		/// until the solution is closed and re-opened. Also handles the project file
		/// being edited by hand and a new package reference being added that has a
		/// custom MSBuild target.
		/// </summary>
		public bool ProjectRequiresReloadAfterRestore ()
		{
			return true;
		}

		public Task AddFileToProjectAsync (string filePath)
		{
			if (project.IsFileInProject (filePath))
				return Task.CompletedTask;

			return Runtime.RunInMainThread (async () => {
				var fullPath = GetFullPath (filePath);
				string buildAction = project.GetDefaultBuildAction (fullPath);
				var fileItem = new ProjectFile (fullPath) {
					BuildAction = buildAction
				};
				project.AddFile (fileItem);
				await SaveProject ();
			});
		}

		string GetFullPath (string relativePath)
		{
			return project.BaseDirectory.Combine (relativePath);
		}
	}
}
