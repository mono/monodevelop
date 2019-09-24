//
// FakePackageRestoreManager.cs
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
using NuGet.Frameworks;
using NuGet.PackageManagement;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.ProjectManagement;
using NuGet.Versioning;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	class FakePackageRestoreManager : IPackageRestoreManager
	{
		public bool IsCurrentSolutionEnabledForRestore {
			get {
				throw new NotImplementedException ();
			}
		}

		#pragma warning disable 67
		public event EventHandler<NuGet.PackageManagement.PackageRestoredEventArgs> PackageRestoredEvent;
		public event EventHandler<PackageRestoreFailedEventArgs> PackageRestoreFailedEvent;
		public event EventHandler<PackagesMissingStatusEventArgs> PackagesMissingStatusChanged;
		#pragma warning restore 67

		public void RaisePackageRestoreFailedEvent (Exception exception, string projectName)
		{
			var packageReference = new PackageReference (
				new PackageIdentity ("Test", new NuGetVersion ("1.2")),
				new NuGetFramework ("any")
			);
			var eventArgs = new PackageRestoreFailedEventArgs (
				packageReference,
				exception,
				new [] { projectName }
			);
			PackageRestoreFailedEvent?.Invoke (this, eventArgs);
		}

		public void EnableCurrentSolutionForRestore (bool fromActivation)
		{
			throw new NotImplementedException ();
		}

		public Dictionary<string, List<PackageRestoreData>> PackagesInSolution =
			new Dictionary<string, List<PackageRestoreData>> ();

		public void AddUnrestoredPackageForProject (string projectName, string solutionDirectory)
		{
			AddPackageForProject (projectName, solutionDirectory, isMissing: true);
		}

		public void AddRestoredPackageForProject (string projectName, string solutionDirectory)
		{
			AddPackageForProject (projectName, solutionDirectory, isMissing: false);
		}

		public void AddPackageForProject (string projectName, string solutionDirectory, bool isMissing)
		{
			var packageReference = new PackageReference (
				new PackageIdentity ("Test", new NuGetVersion ("1.0")),
				new NuGetFramework ("any"));

			var restoreData = new PackageRestoreData (
				packageReference,
				new [] { projectName },
				isMissing);

			var restoreDataList = new List<PackageRestoreData> ();
			restoreDataList.Add (restoreData);
			PackagesInSolution [solutionDirectory] = restoreDataList;
		}

		public Task<IEnumerable<PackageRestoreData>> GetPackagesInSolutionAsync (
			string solutionDirectory,
			CancellationToken token)
		{
			List<PackageRestoreData> restoreData = null;
			if (!PackagesInSolution.TryGetValue (solutionDirectory, out restoreData))
				restoreData = new List<PackageRestoreData> ();

			return Task.FromResult (restoreData.AsEnumerable ());
		}

		public Task RaisePackagesMissingEventForSolutionAsync (string solutionDirectory, CancellationToken token)
		{
			throw new NotImplementedException ();
		}

		public Task<PackageRestoreResult> RestoreMissingPackagesAsync (
			string solutionDirectory,
			IEnumerable<PackageRestoreData> packages,
			INuGetProjectContext nuGetProjectContext,
			PackageDownloadContext downloadContext,
			CancellationToken token)
		{
			RestoreMissingPackagesSolutionDirectory = solutionDirectory;
			PackagesToBeRestored = packages.ToList ();
			RestoreMissingPackagesProjectContext = nuGetProjectContext;
			RestoreMissingPackagesCancellationToken = token;

			BeforeRestoreMissingPackagesAsync ();

			return Task.FromResult (RestoreResult);
		}

		public string RestoreMissingPackagesSolutionDirectory;
		public INuGetProjectContext RestoreMissingPackagesProjectContext;
		public CancellationToken RestoreMissingPackagesCancellationToken;
		public List<PackageRestoreData> PackagesToBeRestored;

		public PackageRestoreResult RestoreResult = new PackageRestoreResult (true, new PackageIdentity[0]);

		public Action BeforeRestoreMissingPackagesAsync = () => { };

		public Task<PackageRestoreResult> RestoreMissingPackagesInSolutionAsync (string solutionDirectory, INuGetProjectContext nuGetProjectContext, CancellationToken token)
		{
			throw new NotImplementedException ();
		}

		public IEnumerable<PackageRestoreData> GetPackagesRestoreData (
			string solutionDirectory,
			Dictionary<PackageReference, List<string>> packageReferencesDict)
		{
			throw new NotImplementedException ();
		}

		public Task<PackageRestoreResult> RestoreMissingPackagesInSolutionAsync (string solutionDirectory, INuGetProjectContext nuGetProjectContext, ILogger logger, CancellationToken token)
		{
			throw new NotImplementedException ();
		}

		public Task<PackageRestoreResult> RestoreMissingPackagesAsync (string solutionDirectory, IEnumerable<PackageRestoreData> packages, INuGetProjectContext nuGetProjectContext, PackageDownloadContext downloadContext, ILogger logger, CancellationToken token)
		{
			throw new NotImplementedException ();
		}
	}
}

