//
// ProjectPackagesFolderNode.cs
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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using NuGet.Configuration;
using NuGet.PackageManagement;
using NuGet.Packaging;
using NuGet.ProjectManagement;
using NuGet.ProjectManagement.Projects;

namespace MonoDevelop.PackageManagement.NodeBuilders
{
	internal class ProjectPackagesFolderNode
	{
		IDotNetProject project;
		NuGetProject nugetProject;
		FolderNuGetProject folder;
		VersionFolderPathResolver packagePathResolver; 
		IUpdatedNuGetPackagesInWorkspace updatedPackagesInWorkspace;
		List<PackageReference> packageReferences = new List<PackageReference> ();
		bool packageReferencesRefreshed;

		CancellationTokenSource cancellationTokenSource;

		public static readonly string NodeName = "Packages";

		public ProjectPackagesFolderNode (DotNetProject project)
			: this (new DotNetProjectProxy (project), PackageManagementServices.UpdatedPackagesInWorkspace)
		{
		}

		public ProjectPackagesFolderNode (
			IDotNetProject project,
			IUpdatedNuGetPackagesInWorkspace updatedPackagesInWorkspace)
			: this (project, updatedPackagesInWorkspace, true)
		{
		}

		protected ProjectPackagesFolderNode (
			IDotNetProject project,
			IUpdatedNuGetPackagesInWorkspace updatedPackagesInWorkspace,
			bool createNuGetProject)
		{
			this.project = project;
			this.updatedPackagesInWorkspace = updatedPackagesInWorkspace;

			if (createNuGetProject)
				CreateInitNuGetProject ();
		}

		protected void CreateInitNuGetProject ()
		{
			var solutionManager = PackageManagementServices.Workspace.GetSolutionManager (project.ParentSolution);
			nugetProject = solutionManager.GetNuGetProject (project);

			if (nugetProject is INuGetIntegratedProject) {
				PackagesFolderPath = SettingsUtility.GetGlobalPackagesFolder (solutionManager.Settings); 
				packagePathResolver = new VersionFolderPathResolver ( 
					PackagesFolderPath, 
					normalizePackageId: false);
			} else {
				PackagesFolderPath = nugetProject.GetPackagesFolderPath (solutionManager);
				folder = new FolderNuGetProject (PackagesFolderPath);
			}
		}

		public FilePath PackagesFolderPath { get; private set; }

		public DotNetProject DotNetProject {
			get { return project.DotNetProject; }
		}

		internal IDotNetProject Project {
			get { return project; }
		}

		public IconId Icon {
			get { return Stock.OpenReferenceFolder; }
		}

		public IconId ClosedIcon {
			get { return Stock.ClosedReferenceFolder; }
		}

		public string GetLabel ()
		{
			return GettextCatalog.GetString ("Packages") + GetUpdatedPackagesCountLabel ();
		}

		string GetUpdatedPackagesCountLabel ()
		{
			int count = GetUpdatedPackagesCount ();
			if (count == 0) {
				return String.Empty;
			}

			return " <span color='grey'>" + GetUpdatedPackagesCountLabel (count) + "</span>";
		}

		string GetUpdatedPackagesCountLabel (int count)
		{
			return string.Format ("({0} {1})", count, GetUpdateText (count));
		}

		string GetUpdateText (int count)
		{
			if (count > 1) {
				return GettextCatalog.GetString ("updates");
			}
			return GettextCatalog.GetString ("update");
		}

		int GetUpdatedPackagesCount ()
		{
			if (!packageReferencesRefreshed) {
				return 0;
			}

			UpdatedNuGetPackagesInProject updatedPackages = updatedPackagesInWorkspace.GetUpdatedPackages (project);
			updatedPackages.RemoveUpdatedPackages (GetPackageReferences ());

			return updatedPackages.GetPackages ().Count ();
		}

		public bool AnyPackageReferences ()
		{
			return packageReferences.Any ();
		}

		public IEnumerable<PackageReferenceNode> GetPackageReferencesNodes ()
		{
			UpdatedNuGetPackagesInProject updatedPackages = updatedPackagesInWorkspace.GetUpdatedPackages (project);
			return GetPackageReferences ().Select (reference => CreatePackageReferenceNode (reference, updatedPackages));
		}

		protected virtual IEnumerable<PackageReference> GetPackageReferences ()
		{
			return packageReferences;
		}

		PackageReferenceNode CreatePackageReferenceNode (PackageReference reference, UpdatedNuGetPackagesInProject updatedPackages)
		{
			return new PackageReferenceNode (
				this,
				reference,
				IsPackageInstalled (reference),
				false,
				updatedPackages.GetUpdatedPackage (reference.PackageIdentity.Id));
		}

		protected virtual bool IsPackageInstalled (PackageReference reference)
		{
			if (nugetProject is INuGetIntegratedProject) {
				string path = packagePathResolver.GetHashPath (reference.PackageIdentity.Id, reference.PackageIdentity.Version);
				return File.Exists (path);
			}

			return folder.PackageExists (reference.PackageIdentity);
		}

		public event EventHandler PackageReferencesChanged;

		void OnPackageReferencesChanged ()
		{
			var handler = PackageReferencesChanged;
			if (handler != null) {
				handler (this, new EventArgs ());
			}
		}

		public void RefreshPackages ()
		{
			try {
				CancelCurrentRefresh ();
				GetInstalledPackages ();
			} catch (Exception ex) {
				LoggingService.LogError ("Refresh packages folder error.", ex);
			}
		}

		void CancelCurrentRefresh ()
		{
			if (cancellationTokenSource != null) {
				cancellationTokenSource.Cancel ();
				cancellationTokenSource.Dispose ();
				cancellationTokenSource = null;
			}
		}

		void GetInstalledPackages ()
		{
			var tokenSource = new CancellationTokenSource ();
			cancellationTokenSource = tokenSource;
			GetInstalledPackagesAsync (tokenSource)
				.ContinueWith (task => OnInstalledPackagesRead (task, tokenSource), TaskScheduler.FromCurrentSynchronizationContext ());
		}

		protected virtual Task<IEnumerable<PackageReference>> GetInstalledPackagesAsync (CancellationTokenSource tokenSource)
		{
			return nugetProject.GetInstalledPackagesAsync (tokenSource.Token);
		}

		protected virtual void OnInstalledPackagesRead (Task<IEnumerable<PackageReference>> task, CancellationTokenSource tokenSource)
		{
			try {
				if (task.IsFaulted) {
					LoggingService.LogError ("OnInstalledPackagesRead error.", task.Exception);
				} else if (!tokenSource.IsCancellationRequested) {
					packageReferencesRefreshed = true;
					packageReferences = task.Result.ToList ();
					OnPackageReferencesChanged ();
				}
			} catch (Exception ex) {
				LoggingService.LogError ("OnInstalledPackagesRead error.", ex);
			}
		}

		public bool AnyPackageReferencesRequiringReinstallation ()
		{
			return GetPackageReferences ().Any (packageReference => packageReference.RequireReinstallation);
		}
	}
}

