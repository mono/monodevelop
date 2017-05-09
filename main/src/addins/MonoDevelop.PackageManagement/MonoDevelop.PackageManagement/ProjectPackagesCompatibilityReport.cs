//
// ProjectPackagesCompatibilityReport.cs
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

using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Linq;
using System.IO;
using MonoDevelop.Core;
using NuGet.Packaging.Core;

namespace MonoDevelop.PackageManagement
{
	internal class ProjectPackagesCompatibilityReport
	{
		FrameworkName projectTargetFramework;
		List<PackageCompatibility> packageCompatibilities = new List<PackageCompatibility> ();
		TextWriter writer;

		public ProjectPackagesCompatibilityReport (FrameworkName projectTargetFramework)
		{
			this.projectTargetFramework = projectTargetFramework;
		}

		public void Add (PackageCompatibility packageCompatibility)
		{
			packageCompatibilities.Add (packageCompatibility);
		}

		public void GenerateReport (TextWriter writer)
		{
			this.writer = writer;

			ReportIncompatiblePackages ();
			ReportPackagesNeedingReinstall ();
		}

		public bool AnyIncompatiblePackages ()
		{
			return GetPackagesIncompatibleWithNewProjectTargetFramework ().Any ();
		}

		void ReportIncompatiblePackages ()
		{
			List<PackageIdentity> incompatiblePackages = GetPackagesIncompatibleWithNewProjectTargetFramework ().ToList ();
			if (incompatiblePackages.Any ()) {
				writer.WriteLine (GetIncompatiblePackagesWarningMessage ());
				writer.WriteLine ();

				foreach (PackageIdentity package in incompatiblePackages) {
					writer.WriteLine (package.Id);
				}

				writer.WriteLine ();
			}
		}

		void ReportPackagesNeedingReinstall ()
		{
			List<PackageIdentity> packagesToReinstall = GetCompatiblePackagesNeedingReinstall ().ToList ();
			if (packagesToReinstall.Any ()) {
				writer.WriteLine (GetPackageReinstallationWarningMessage ());
				writer.WriteLine ();

				foreach (PackageIdentity package in packagesToReinstall) {
					writer.WriteLine (package.Id);
				}
			}
		}

		IEnumerable<PackageIdentity> GetPackagesIncompatibleWithNewProjectTargetFramework ()
		{
			return packageCompatibilities
				.Where (packageCompatibility => !packageCompatibility.IsCompatibleWithNewProjectTargetFramework)
				.Select (packageCompatibility => packageCompatibility.Package);
		}

		IEnumerable<PackageIdentity> GetCompatiblePackagesNeedingReinstall ()
		{
			return packageCompatibilities
				.Where(packageCompatibility => packageCompatibility.ShouldReinstallPackage)
				.Where (packageCompatibility => packageCompatibility.IsCompatibleWithNewProjectTargetFramework)
				.Select (packageCompatibility => packageCompatibility.Package);
		}

		string GetIncompatiblePackagesWarningMessage ()
		{
			return GettextCatalog.GetString ("The following packages are incompatible with the current project target framework '{0}'. The packages do not contain any assembly references or content files that are compatible with the current project target framework and may no longer work. Retargeting these packages will fail and cause them to be removed from the project.",
				projectTargetFramework);
		}

		string GetPackageReinstallationWarningMessage()
		{
			return GettextCatalog.GetString ("The following packages should be retargeted. They were installed with a target framework that is different from the current project target framework '{0}'. The packages contain assembly references or content files for the current project target framework which are not currently installed.",
				projectTargetFramework);
		}
	}
}

