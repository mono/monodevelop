//
// PackageCompatibilityChecker.cs
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
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.Frameworks;

namespace MonoDevelop.PackageManagement
{
	internal class PackageCompatibilityChecker
	{
		List<PackageIdentity> packagesRequiringReinstallation = new List<PackageIdentity> ();
		List<PackageReference> packageReferences;
		ProjectPackagesCompatibilityReport compatibilityReport;
		string packageReferenceFileName;

		public string PackageReferenceFileName {
			get { return packageReferenceFileName; }
		}

		public async Task CheckProjectPackages (IDotNetProject project)
		{
			packageReferenceFileName = project.GetPackagesConfigFilePath ();

			var targetFramework = new ProjectTargetFramework (project);
			compatibilityReport = new ProjectPackagesCompatibilityReport (targetFramework.TargetFrameworkName);

			IPackageCompatibilityNuGetProject nugetProject = await GetNuGetProject (project);

			var installedPackages = await nugetProject.GetInstalledPackagesAsync (CancellationToken.None);
			packageReferences = installedPackages.ToList ();

			foreach (var packageReference in packageReferences) {
				if (PackageNeedsReinstall (nugetProject, packageReference)) {
					packagesRequiringReinstallation.Add (packageReference.PackageIdentity);
				}
			}
		}

		protected virtual Task<IPackageCompatibilityNuGetProject> GetNuGetProject (IDotNetProject project)
		{
			return Runtime.RunInMainThread (() => {
				var solutionManager = PackageManagementServices.Workspace.GetSolutionManager (project.ParentSolution);
				var nugetProject = solutionManager.GetNuGetProject (project) as MSBuildNuGetProject;
				return new PackageCompatibilityNuGetProject (nugetProject) as IPackageCompatibilityNuGetProject;
			});
		}

		bool PackageNeedsReinstall (IPackageCompatibilityNuGetProject nugetProject, PackageReference packageReference)
		{
			var targetFramework = nugetProject.TargetFramework;
			string packageFileName = nugetProject.GetInstalledPackageFilePath (packageReference.PackageIdentity);
			if (!FileExists (packageFileName))
				return false;

			var compatibility = CreatePackageCompatibility (targetFramework, packageReference, packageFileName);
			compatibility.CheckCompatibility ();
			compatibilityReport.Add (compatibility);

			return compatibility.ShouldReinstallPackage;
		}

		protected virtual bool FileExists (string fileName)
		{
			return File.Exists (fileName);
		}

		protected virtual PackageCompatibility CreatePackageCompatibility (
			NuGetFramework targetFramework,
			PackageReference packageReference,
			string packageFileName)
		{
			return new PackageCompatibility (targetFramework, packageReference, packageFileName);
		}

		public bool AnyIncompatiblePackages ()
		{
			return compatibilityReport.AnyIncompatiblePackages ();
		}

		public bool AnyPackagesRequireReinstallation ()
		{
			return packagesRequiringReinstallation.Any ();
		}

		public void MarkPackagesForReinstallation ()
		{
			GuiDispatch (() => {
				var packageReferencesToUpdate = new Dictionary<PackageReference, PackageReference> ();

				foreach (PackageReference packageReference in packageReferences) {
					bool reinstall = packagesRequiringReinstallation.Any (package => package.Id == packageReference.PackageIdentity.Id);

					if (reinstall != packageReference.RequireReinstallation) {
						var updatedPackageReference = new PackageReference (
							packageReference.PackageIdentity,
							packageReference.TargetFramework,
							packageReference.IsUserInstalled,
							packageReference.IsDevelopmentDependency,
							reinstall);
						packageReferencesToUpdate.Add (packageReference, updatedPackageReference);
					}
				}

				if (packageReferencesToUpdate.Any ()) {
					UpdatePackageReferences (packageReferenceFileName, packageReferencesToUpdate);
				}
			});
		}

		protected virtual void UpdatePackageReferences (
			string packageReferenceFileName, 
			Dictionary<PackageReference, PackageReference> packageReferencesToUpdate)
		{
			using (var writer = new PackagesConfigWriter (packageReferenceFileName, createNew: false)) {
				foreach (var entry in packageReferencesToUpdate) {
					writer.UpdatePackageEntry (entry.Key, entry.Value);
				}
			}
		}

		public bool PackagesMarkedForReinstallationInPackageReferenceFile ()
		{
			return packageReferences.Any (packageReference => packageReference.RequireReinstallation);
		}

		protected virtual void GuiDispatch (Action handler)
		{
			Runtime.RunInMainThread (handler);
		}

		public void GenerateReport (TextWriter writer)
		{
			compatibilityReport.GenerateReport (writer);
		}
	}
}

