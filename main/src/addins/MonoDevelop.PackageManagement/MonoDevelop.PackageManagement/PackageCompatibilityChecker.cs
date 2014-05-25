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
using System.Linq;
using System.Runtime.Versioning;
using ICSharpCode.PackageManagement;
using NuGet;

namespace MonoDevelop.PackageManagement
{
	public class PackageCompatibilityChecker
	{
		IPackageManagementSolution solution;
		List<IPackage> packagesRequiringReinstallation = new List<IPackage> ();
		PackageReferenceFile packageReferenceFile;
		List<PackageReference> packageReferences;

		public PackageCompatibilityChecker ()
			: this (PackageManagementServices.Solution)
		{
		}

		public PackageCompatibilityChecker (IPackageManagementSolution solution)
		{
			this.solution = solution;
		}

		public string PackageReferenceFileName {
			get { return packageReferenceFile.FullPath; }
		}

		public void CheckProjectPackages (IDotNetProject project)
		{
			IPackageManagementProject packageManagementProject = solution.GetProject (PackageManagementServices.RegisteredPackageRepositories.ActiveRepository, project);

			packageReferenceFile = new PackageReferenceFile (project.GetPackagesConfigFilePath ());
			packageReferences = packageReferenceFile.GetPackageReferences ().ToList ();

			foreach (PackageReference packageReference in packageReferences) {
				IPackage package = packageManagementProject.FindPackage (packageReference.Id);
				if (PackageNeedsReinstall (project, package, packageReference.TargetFramework)) {
					packagesRequiringReinstallation.Add (package);
				}
			}
		}

		bool PackageNeedsReinstall (IDotNetProject project, IPackage package, FrameworkName packageTargetFramework)
		{
			if (package == null)
				return false;

			var projectTargetFramework = new ProjectTargetFramework (project);
			return ProjectRetargetingUtility.ShouldPackageBeReinstalled (
					projectTargetFramework.TargetFrameworkName,
					packageTargetFramework,
					package);
		}

		public bool AnyPackagesRequireReinstallation ()
		{
			return packagesRequiringReinstallation.Any ();
		}

		public IEnumerable<string> GetPackagesRequiringReinstallation ()
		{
			return packagesRequiringReinstallation
				.Select (package => package.Id);
		}

		public void MarkPackagesForReinstallation ()
		{
			foreach (PackageReference packageReference in packageReferences) {
				bool reinstall = packagesRequiringReinstallation.Any (package => package.Id == packageReference.Id);
				packageReferenceFile.MarkEntryForReinstallation (
					packageReference.Id,
					packageReference.Version,
					packageReference.TargetFramework,
					reinstall);
			}
		}
	}
}

