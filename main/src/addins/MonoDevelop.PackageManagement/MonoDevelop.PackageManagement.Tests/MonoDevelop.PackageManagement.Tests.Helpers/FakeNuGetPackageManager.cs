//
// FakeNuGetPackageManager.cs
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
using NuGet.Common;
using NuGet.PackageManagement;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	class FakeNuGetPackageManager : INuGetPackageManager
	{
		public INuGetProjectContext ClearDirectInstallProjectContext;

		public void ClearDirectInstall (INuGetProjectContext nuGetProjectContext)
		{
			ClearDirectInstallProjectContext = nuGetProjectContext;
		}

		public PackageIdentity SetDirectInstallPackageIdentity;
		public INuGetProjectContext SetDirectInstallProjectContext;

		public void SetDirectInstall (PackageIdentity directInstall, INuGetProjectContext nuGetProjectContext)
		{
			SetDirectInstallPackageIdentity = directInstall;
			SetDirectInstallProjectContext = nuGetProjectContext;
		}

		public NuGetProject ExecutedNuGetProject;
		public List<NuGetProjectAction> ExecutedActions;
		public INuGetProjectContext ExecutedProjectContext;
		public CancellationToken ExecutedCancellationToken;

		public Action BeforeExecuteAction = () => { };

		public Task ExecuteNuGetProjectActionsAsync (
			NuGetProject nuGetProject,
			IEnumerable<NuGetProjectAction> nuGetProjectActions,
			INuGetProjectContext nuGetProjectContext,
			CancellationToken token)
		{
			ExecutedNuGetProject = nuGetProject;
			ExecutedActions = nuGetProjectActions.ToList ();
			ExecutedProjectContext = nuGetProjectContext;
			ExecutedCancellationToken = token;

			BeforeExecuteAction ();

			return Task.FromResult (0);
		}

		public NuGetVersion LatestVersion = new NuGetVersion ("1.2.3");
		public string GetLatestVersionPackageId;
		public NuGetProject GetLatestVersionProject;
		public ResolutionContext GetLatestVersionResolutionContext;
		public List<SourceRepository> GetLatestVersionSources;
		public ILogger GetLatestVersionLogger;
		public CancellationToken GetLatestVersionCancellationToken;

		public Task<ResolvedPackage> GetLatestVersionAsync (
			string packageId,
			NuGetProject project,
			ResolutionContext resolutionContext,
			IEnumerable<SourceRepository> sources,
			ILogger log,
			CancellationToken token)
		{
			GetLatestVersionPackageId = packageId;
			GetLatestVersionProject = project;
			GetLatestVersionResolutionContext = resolutionContext;
			GetLatestVersionSources = sources.ToList ();
			GetLatestVersionLogger = log;
			GetLatestVersionCancellationToken = token;

			token.ThrowIfCancellationRequested ();

			var resolvedPackage = new ResolvedPackage (LatestVersion, true);
			return Task.FromResult (resolvedPackage);
		}

		public List<FakeNuGetProjectAction> InstallActions = new List<FakeNuGetProjectAction> ();

		public NuGetProject PreviewInstallProject;
		public PackageIdentity PreviewInstallPackageIdentity;
		public ResolutionContext PreviewInstallResolutionContext;
		public List<SourceRepository> PreviewInstallPrimarySources;
		public IEnumerable<SourceRepository> PreviewInstallSecondarySources;
		public CancellationToken PreviewInstallCancellationToken;

		public Action BeforePreviewInstallPackageAsyncAction = () => { };

		public Task<IEnumerable<NuGetProjectAction>> PreviewInstallPackageAsync (
			NuGetProject nuGetProject,
			PackageIdentity packageIdentity,
			ResolutionContext resolutionContext,
			INuGetProjectContext nuGetProjectContext,
			IEnumerable<SourceRepository> primarySources,
			IEnumerable<SourceRepository> secondarySources,
			CancellationToken token)
		{
			PreviewInstallProject = nuGetProject;
			PreviewInstallPackageIdentity = packageIdentity;
			PreviewInstallResolutionContext = resolutionContext;
			PreviewInstallPrimarySources = primarySources.ToList ();
			PreviewInstallSecondarySources = secondarySources;
			PreviewInstallCancellationToken = token;

			BeforePreviewInstallPackageAsyncAction ();

			token.ThrowIfCancellationRequested ();

			IEnumerable<NuGetProjectAction> actions = InstallActions.ToArray ();
			return Task.FromResult (actions);
		}

		public List<PackageIdentity> PackagesInPackagesFolder = new List<PackageIdentity> ();

		public bool PackageExistsInPackagesFolder (PackageIdentity packageIdentity)
		{
			return PackagesInPackagesFolder.Contains (packageIdentity);
		}

		public List<FakeNuGetProjectAction> UpdateActions = new List<FakeNuGetProjectAction> ();

		public NuGetProject PreviewUpdateProject;
		public string PreviewUpdatePackageId;
		public ResolutionContext PreviewUpdateResolutionContext;
		public List<SourceRepository> PreviewUpdatePrimarySources;
		public IEnumerable<SourceRepository> PreviewUpdateSecondarySources;
		public CancellationToken PreviewUpdateCancellationToken;

		public Task<IEnumerable<NuGetProjectAction>> PreviewUpdatePackagesAsync (
			string packageId,
			NuGetProject nuGetProject,
			ResolutionContext resolutionContext,
			INuGetProjectContext nuGetProjectContext,
			IEnumerable<SourceRepository> primarySources,
			IEnumerable<SourceRepository> secondarySources,
			CancellationToken token)
		{
			PreviewUpdateProject = nuGetProject;
			PreviewUpdatePackageId = packageId;
			PreviewUpdateResolutionContext = resolutionContext;
			PreviewUpdatePrimarySources = primarySources.ToList ();
			PreviewUpdateSecondarySources = secondarySources;
			PreviewUpdateCancellationToken = token;

			IEnumerable<NuGetProjectAction> actions = UpdateActions.ToArray ();
			return Task.FromResult (actions);
		}

		public Task<IEnumerable<NuGetProjectAction>> PreviewUpdatePackagesAsync (
			NuGetProject nuGetProject,
			ResolutionContext resolutionContext,
			INuGetProjectContext nuGetProjectContext,
			IEnumerable<SourceRepository> primarySources,
			IEnumerable<SourceRepository> secondarySources,
			CancellationToken token)
		{
			return PreviewUpdatePackagesAsync (
				null,
				nuGetProject,
				resolutionContext,
				nuGetProjectContext,
				primarySources,
				secondarySources,
				token
			);
		}

		public void AddPackageToPackagesFolder (string packageId, string version)
		{
			var package = new PackageIdentity (packageId, new NuGetVersion (version));
			PackagesInPackagesFolder.Add (package);
		}

		public List<FakeNuGetProjectAction> UninstallActions = new List<FakeNuGetProjectAction> ();

		public NuGetProject PreviewUninstallProject;
		public string PreviewUninstallPackageId;
		public UninstallationContext PreviewUninstallContext;
		public INuGetProjectContext PreviewUninstallProjectContext;
		public CancellationToken PreviewUninstallCancellationToken;

		public Action BeforePreviewUninstallPackagesAsync = () => { };

		public Task<IEnumerable<NuGetProjectAction>> PreviewUninstallPackageAsync (
			NuGetProject nuGetProject,
			string packageId,
			UninstallationContext uninstallationContext,
			INuGetProjectContext nuGetProjectContext,
			CancellationToken token)
		{
			PreviewUninstallProject = nuGetProject;
			PreviewUninstallPackageId = packageId;
			PreviewUninstallContext = uninstallationContext;
			PreviewUninstallProjectContext = nuGetProjectContext;
			PreviewUninstallCancellationToken = token;

			BeforePreviewUninstallPackagesAsync ();

			IEnumerable<NuGetProjectAction> actions = UninstallActions.ToArray ();
			return Task.FromResult (actions);
		}

		public NuGetProject OpenReadmeFilesForProject;
		public List<PackageIdentity> OpenReadmeFilesForPackages;
		public INuGetProjectContext OpenReadmeFilesWithProjectContext;
		public CancellationToken OpenReadmeFilesWithCancellationToken;

		public Task OpenReadmeFiles (
			NuGetProject project,
			IEnumerable<PackageIdentity> packages,
			INuGetProjectContext nuGetProjectContext,
			CancellationToken token)
		{
			OpenReadmeFilesForProject = project;
			OpenReadmeFilesForPackages = packages.ToList ();
			OpenReadmeFilesWithProjectContext = nuGetProjectContext;
			OpenReadmeFilesWithCancellationToken = token;

			return Task.FromResult (0);
		}

		public IBuildIntegratedNuGetProject PreviewBuildIntegratedProject;
		public List<NuGetProjectAction> PreviewBuildIntegratedProjectActions;
		public INuGetProjectContext PreviewBuildIntegratedContext;
		public CancellationToken PreviewBuildIntegratedCancellationToken;
		public BuildIntegratedProjectAction BuildIntegratedProjectAction;

		public Task<BuildIntegratedProjectAction> PreviewBuildIntegratedProjectActionsAsync (
			IBuildIntegratedNuGetProject buildIntegratedProject,
			IEnumerable<NuGetProjectAction> nuGetProjectActions,
			INuGetProjectContext nuGetProjectContext,
			CancellationToken token)
		{
			PreviewBuildIntegratedProject = buildIntegratedProject;
			PreviewBuildIntegratedProjectActions = nuGetProjectActions.ToList ();
			PreviewBuildIntegratedContext = nuGetProjectContext;
			PreviewBuildIntegratedCancellationToken = token;

			BeforePreviewUninstallPackagesAsync ();

			return Task.FromResult (BuildIntegratedProjectAction);
		}
	}
}

