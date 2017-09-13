//
// PackageManagementPathResolver.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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

using NuGet.Configuration;
using NuGet.PackageManagement;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.ProjectManagement.Projects;

namespace MonoDevelop.PackageManagement
{
	class PackageManagementPathResolver
	{
		FallbackPackagePathResolver pathResolver;
		VersionFolderPathResolver globalPackagesFolderResolver;
		FolderNuGetProject folderNuGetProject;

		public PackageManagementPathResolver (IMonoDevelopSolutionManager solutionManager)
		{
			var pathContext = NuGetPathContext.Create (solutionManager.Settings);
			pathResolver = new FallbackPackagePathResolver (pathContext);
			globalPackagesFolderResolver = new VersionFolderPathResolver (pathContext.UserPackageFolder);

			string packagesFolderPath = PackagesFolderPathUtility.GetPackagesFolderPath (solutionManager, solutionManager.Settings);
			folderNuGetProject = new FolderNuGetProject (packagesFolderPath);
		}

		/// <summary>
		/// No solution manager provided so only global packages cache will be considered.
		/// </summary>
		public PackageManagementPathResolver ()
		{
			var settings = SettingsLoader.LoadDefaultSettings ();
			var pathContext = NuGetPathContext.Create (settings);
			pathResolver = new FallbackPackagePathResolver (pathContext);
			globalPackagesFolderResolver = new VersionFolderPathResolver (pathContext.UserPackageFolder);

			folderNuGetProject = new FolderNuGetProject (pathContext.UserPackageFolder);
		}

		public string GetPackageInstallPath (NuGetProject nugetProject, PackageIdentity package)
		{
			if (nugetProject is INuGetIntegratedProject) {
				return pathResolver.GetPackageDirectory (package.Id, package.Version) ??
					globalPackagesFolderResolver.GetInstallPath (package.Id, package.Version);
			}

			return folderNuGetProject.GetInstalledPath (package);
		}
	}
}
