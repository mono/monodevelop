//
// MonoDevelopPackageRestoreManager.cs
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
using NuGet.PackageManagement;
using NuGet.Packaging;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;

namespace MonoDevelop.PackageManagement
{
	internal class MonoDevelopPackageRestoreManager : IPackageRestoreManager
	{
		PackageRestoreManager restoreManager;

		public MonoDevelopPackageRestoreManager (IMonoDevelopSolutionManager solutionManager)
		{
			restoreManager = new PackageRestoreManager (
				solutionManager.CreateSourceRepositoryProvider (),
				solutionManager.Settings,
				solutionManager);
		}

		public bool IsCurrentSolutionEnabledForRestore {
			get {
				return restoreManager.IsCurrentSolutionEnabledForRestore;
			}
		}

		public event EventHandler<NuGet.PackageManagement.PackageRestoredEventArgs> PackageRestoredEvent {
			add { restoreManager.PackageRestoredEvent += value; }
			remove { restoreManager.PackageRestoredEvent -= value; }
		}

		public event EventHandler<PackageRestoreFailedEventArgs> PackageRestoreFailedEvent{
			add { restoreManager.PackageRestoreFailedEvent += value; }
			remove { restoreManager.PackageRestoreFailedEvent -= value; }
		}
		public event EventHandler<PackagesMissingStatusEventArgs> PackagesMissingStatusChanged{
			add { restoreManager.PackagesMissingStatusChanged += value; }
			remove { restoreManager.PackagesMissingStatusChanged -= value; }
		}

		public void EnableCurrentSolutionForRestore (bool fromActivation)
		{
			restoreManager.EnableCurrentSolutionForRestore (fromActivation);
		}

		public Task<IEnumerable<PackageRestoreData>> GetPackagesInSolutionAsync (
			string solutionDirectory,
			CancellationToken token)
		{
			return restoreManager.GetPackagesInSolutionAsync (solutionDirectory, token);
		}

		public Task RaisePackagesMissingEventForSolutionAsync (string solutionDirectory, CancellationToken token)
		{
			return restoreManager.RaisePackagesMissingEventForSolutionAsync (solutionDirectory, token);
		}

		public Task<PackageRestoreResult> RestoreMissingPackagesAsync (
			string solutionDirectory,
			IEnumerable<PackageRestoreData> packages,
			INuGetProjectContext nuGetProjectContext,
			PackageDownloadContext downloadContext,
			CancellationToken token)
		{
			return restoreManager.RestoreMissingPackagesAsync (
				solutionDirectory,
				packages,
				nuGetProjectContext,
				downloadContext,
				token
			);
		}

		public Task<PackageRestoreResult> RestoreMissingPackagesInSolutionAsync (
			string solutionDirectory,
			INuGetProjectContext nuGetProjectContext,
			CancellationToken token)
		{
			return restoreManager.RestoreMissingPackagesInSolutionAsync (
				solutionDirectory,
				nuGetProjectContext,
				token
			);
		}

		public IEnumerable<PackageRestoreData> GetPackagesRestoreData (
			string solutionDirectory,
			Dictionary<PackageReference, List<string>> packageReferencesDict)
		{
			return restoreManager.GetPackagesRestoreData (solutionDirectory, packageReferencesDict);
		}
	}

	internal static class PackageRestoreManagerExtensions
	{
		public static async Task<PackageRestoreResult> RestoreMissingPackagesAsync (
			this IPackageRestoreManager restoreManager,
			string solutionDirectory,
			NuGetProject nuGetProject,
			INuGetProjectContext nuGetProjectContext,
			PackageDownloadContext downloadContext,
			CancellationToken token)
		{
			var installedPackages = await nuGetProject.GetInstalledPackagesAsync (token);
			var nuGetProjectName = NuGetProject.GetUniqueNameOrName (nuGetProject);
			var projectNames = new[] { nuGetProjectName };

			var packages = installedPackages.Select (package => new PackageRestoreData (package, projectNames, isMissing: true));

			return await restoreManager.RestoreMissingPackagesAsync (
				solutionDirectory,
				packages,
				nuGetProjectContext,
				downloadContext,
				token);
		}
	}
}

