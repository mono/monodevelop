//
// PackagingNuGetProject.cs
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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.PackageManagement;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.ProjectManagement.Projects;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace MonoDevelop.Packaging
{
	class PackagingNuGetProject : NuGetProject, INuGetIntegratedProject, IHasDotNetProject
	{
		PackagingProject project;

		public PackagingNuGetProject (PackagingProject project)
		{
			this.project = project;

			InternalMetadata.Add (NuGetProjectMetadataKeys.TargetFramework, NuGetFramework.Parse ("any"));
			InternalMetadata.Add (NuGetProjectMetadataKeys.Name, project.Name);
			InternalMetadata.Add (NuGetProjectMetadataKeys.UniqueName, project.Name);
		}

		public override Task<IEnumerable<NuGet.Packaging.PackageReference>> GetInstalledPackagesAsync (CancellationToken token)
		{
			return Task.FromResult (GetPackageReferences ());
		}

		IEnumerable<NuGet.Packaging.PackageReference> GetPackageReferences ()
		{
			return project
				.PackageReferences
				.Select (packageReference => packageReference.ToNuGetPackageReference ())
				.ToList ();
		}

		public override async Task<bool> InstallPackageAsync (
			PackageIdentity packageIdentity,
			DownloadResourceResult downloadResourceResult,
			INuGetProjectContext nuGetProjectContext,
			CancellationToken token)
		{
			return await Runtime.RunInMainThread (async () => {

				// Check if this NuGet package is already installed and should be removed.
				ProjectPackageReference existingPackageReference = project.FindPackageReference (packageIdentity);
				if (existingPackageReference != null) {
					if (ShouldRemoveExistingPackageReference (existingPackageReference, packageIdentity)) {
						project.PackageReferences.Remove (existingPackageReference);
					} else {
						nuGetProjectContext.Log (
							MessageLevel.Info,
							GettextCatalog.GetString ("Package '{0}' already installed.", packageIdentity)); 
						return true;
					}
				}

				bool developmentDependency = false;
				if (IsNuGetBuildPackagingPackage (packageIdentity)) {
					await GlobalPackagesExtractor.Extract (project.ParentSolution, packageIdentity, downloadResourceResult, token);

					developmentDependency = true;
					GenerateNuGetBuildPackagingTargets (packageIdentity);
				}

				var packageReference = ProjectPackageReference.Create (packageIdentity);
				if (developmentDependency)
					packageReference.Metadata.SetValue ("PrivateAssets", "All");
				project.PackageReferences.Add (packageReference);
				await SaveProject ();
				return true;
			});
		}

		public override async Task<bool> UninstallPackageAsync (
			PackageIdentity packageIdentity,
			INuGetProjectContext nuGetProjectContext,
			CancellationToken token)
		{
			return await Runtime.RunInMainThread (() => {
				ProjectPackageReference packageReference = project.FindPackageReference (packageIdentity);
				if (packageReference != null) {
					project.PackageReferences.Remove (packageReference);
					SaveProject ();
				}
				return true;
			});
		}

		/// <summary>
		/// If the package version is already installed then there is no need to install the 
		/// NuGet package.
		/// </summary>
		bool ShouldRemoveExistingPackageReference (ProjectPackageReference packageReference, PackageIdentity packageIdentity)
		{
			var existingPackageReference = packageReference.ToNuGetPackageReference ();
			return !VersionComparer.Default.Equals (existingPackageReference.PackageIdentity.Version, packageIdentity.Version);
		}

		bool IsNuGetBuildPackagingPackage (PackageIdentity packageIdentity)
		{
			return StringComparer.OrdinalIgnoreCase.Equals (packageIdentity.Id, "NuGet.Build.Packaging");
		}

		void GenerateNuGetBuildPackagingTargets (PackageIdentity packageIdentity)
		{
			GenerateNuGetBuildPackagingTargets (packageIdentity, project);
		}

		public static void GenerateNuGetBuildPackagingTargets (PackageIdentity packageIdentity, PackagingProject project)
		{
			var packagePathResolver = new VersionFolderPathResolver (string.Empty);
			string packageDirectory = packagePathResolver.GetPackageDirectory (packageIdentity.Id, packageIdentity.Version);
			string buildDirectory = Path.Combine (packageDirectory, "build");
			string prop = Path.Combine (buildDirectory, "NuGet.Build.Packaging.props");
			string target = Path.Combine (buildDirectory, "NuGet.Build.Packaging.targets");

			MSBuildNuGetImportGenerator.CreateImports (project, prop, target);
		}

		public Task SaveProject ()
		{
			return project.SaveAsync (new ProgressMonitor ());
		}

		public IDotNetProject Project {
			get { return new DotNetProjectProxy (project); }
		}
	}
}

