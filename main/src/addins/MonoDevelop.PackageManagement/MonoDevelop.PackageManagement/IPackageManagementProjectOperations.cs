//
// IPackageManagementProjectOperations.cs
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
using MonoDevelop.Projects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonoDevelop.PackageManagement
{
	/// <summary>
	/// Provides a simplified interface for working with NuGet packages in a project.
	/// 
	/// Used by the ComponentReferencingProjectAdaptor in Xamarin.Ide so keep the NuGet 
	/// package management logic in the NuGet addin.
	/// </summary>
	public interface IPackageManagementProjectOperations
	{
		/// <summary>
		/// Installs NuGet packages into the selected project. If a NuGet package requires a license to be
		/// accepted then a dialog will be displayed.
		/// </summary>
		/// <param name="packageSourceUrl">Package source URL.</param>
		/// <param name="project">Project.</param>
		/// <param name="packages">Packages.</param>
		void InstallPackages (string packageSourceUrl, Project project, IEnumerable<PackageManagementPackageReference> packages);

		/// <summary>
		/// Installs NuGet packages into the selected project.
		/// </summary>
		/// <param name="packageSourceUrl">Package source URL.</param>
		/// <param name="project">Project.</param>
		/// <param name="packages">Packages.</param>
		/// <param name="licensesAccepted">True if NuGet package licenses have already been accepted. If false then the 
		/// license acceptance dialog will be displayed for any licences that require a license to be accepted.</param>
		void InstallPackages (string packageSourceUrl, Project project, IEnumerable<PackageManagementPackageReference> packages, bool licensesAccepted);

		/// <summary>
		/// Installs NuGet packages into the selected project using the enabled package sources.
		/// </summary>
		/// <param name="project">Project.</param>
		/// <param name="packages">Packages.</param>
		void InstallPackages (Project project, IEnumerable<PackageManagementPackageReference> packages);

		/// <summary>
		/// Installs NuGet packages into the selected project using the enabled package sources.
		/// </summary>
		/// <param name="project">Project.</param>
		/// <param name="packages">Packages.</param>
		Task InstallPackagesAsync (Project project, IEnumerable<PackageManagementPackageReference> packages);

		/// <summary>
		/// Installs NuGet packages into the selected project using the enabled package sources.
		/// </summary>
		/// <param name="project">Project.</param>
		/// <param name="packages">Packages.</param>
		/// <param name="licensesAccepted">True if NuGet package licenses have already been accepted. If false then the 
		/// license acceptance dialog will be displayed for any licences that require a license to be accepted.</param>
		Task InstallPackagesAsync (Project project, IEnumerable<PackageManagementPackageReference> packages, bool licensesAccepted);

		/// <summary>
		/// Installs NuGet packages into the selected project. If a NuGet package requires a license to be
		/// accepted then a dialog will be displayed.
		/// </summary>
		/// <returns>A task that can be used to determine when all the packages have been installed.</returns>
		/// <param name="packageSourceUrl">Package source URL.</param>
		/// <param name="project">Project.</param>
		/// <param name="packages">Packages.</param>
		Task InstallPackagesAsync (string packageSourceUrl, Project project, IEnumerable<PackageManagementPackageReference> packages);

		/// <summary>
		/// Removes the NuGet packages from the selected project and optionally removes any
		/// dependencies for the NuGet packages.
		/// </summary>
		/// <param name="project">Project.</param>
		/// <param name="packages">Packages.</param>
		/// <param name="removeDependencies">If set to <c>true</c> remove dependencies.</param>
		void UninstallPackages (Project project, IEnumerable<string> packages, bool removeDependencies = false);

		/// <summary>
		/// Removes the NuGet packages from the selected project and optionally removes any
		/// dependencies for the NuGet packages.
		/// </summary>
		/// <returns>A task that can be used to determine when all the packages have been uninstalled.</returns>
		/// <param name="project">Project.</param>
		/// <param name="packages">Packages.</param>
		/// <param name="removeDependencies">If set to <c>true</c> remove dependencies.</param>
		Task UninstallPackagesAsync (Project project, IEnumerable<string> packages, bool removeDependencies = false);

		IEnumerable<PackageManagementPackageReference> GetInstalledPackages (Project project);

		event EventHandler<PackageManagementPackageReferenceEventArgs> PackageReferenceAdded;
		event EventHandler<PackageManagementPackageReferenceEventArgs> PackageReferenceRemoved;
		event EventHandler PackagesRestored;
	}
}

