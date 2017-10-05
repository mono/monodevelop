//
// DotNetProjectExtensions.cs
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
using MonoDevelop.PackageManagement;
using MonoDevelop.Projects;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.Packaging
{
	static class DotNetProjectExtensions
	{
		public static bool HasNuGetMetadata (this DotNetProject project)
		{
			MSBuildPropertyGroup propertyGroup = project.MSBuildProject.GetNuGetMetadataPropertyGroup ();
			return propertyGroup.HasProperty ("PackageId");
		}

		public static void SetOutputAssemblyName (this DotNetProject project, string name)
		{
			foreach (var configuration in project.Configurations.OfType<DotNetProjectConfiguration> ()) {
				configuration.OutputAssembly = name;
			}
		}

		public static bool IsBuildPackagingNuGetPackageInstalled (this DotNetProject project)
		{
			return PackageManagementServices.ProjectOperations.GetInstalledPackages (project)
				.Any (package => string.Equals ("NuGet.Build.Packaging", package.Id, StringComparison.OrdinalIgnoreCase));
		}

		public static void InstallBuildPackagingNuGetPackage (this Project project)
		{
			InstallBuildPackagingNuGetPackage (new [] { project });
		}

		static string GetPackagesFolder ()
		{
			return Path.Combine (
				Path.GetDirectoryName (typeof (DotNetProjectExtensions).Assembly.Location),
				"packages");
		}

		public static void InstallBuildPackagingNuGetPackage (IEnumerable<Project> projects)
		{
			string packagesFolder = GetPackagesFolder ();
			var packageReference = new PackageManagementPackageReference ("NuGet.Build.Packaging", "0.2.0");

			var packageReferences = new [] { packageReference };

			var projectOperations = PackageManagementServices.ProjectOperations as PackageManagementProjectOperations;

			var actions = new List<INuGetPackageAction> ();
			foreach (Project project in projects) {
				actions.AddRange (projectOperations.CreateInstallActions (packagesFolder, project, packageReferences));
			}

			var message = ProgressMonitorStatusMessageFactory.CreateInstallingProjectTemplatePackagesMessage ();
			PackageManagementServices.BackgroundPackageActionRunner.Run (message, actions);
		}
	}
}

