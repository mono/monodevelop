//
// GlobalPackagesExtractor.cs
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

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.PackageManagement;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Packaging.PackageExtraction;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;

namespace MonoDevelop.PackageManagement
{
	internal static class GlobalPackagesExtractor
	{
		const int BufferSize = 8192;

		public static async Task Extract (
			Solution solution,
			PackageIdentity packageIdentity,
			DownloadResourceResult downloadResult,
			CancellationToken token)
		{
			string globalPackagesFolder = await GetPackagesDirectory (solution);

			var defaultPackagePathResolver = new VersionFolderPathResolver (globalPackagesFolder);

			string hashPath = defaultPackagePathResolver.GetHashPath (packageIdentity.Id, packageIdentity.Version);

			if (File.Exists (hashPath))
				return;

			var versionFolderPathContext = new VersionFolderPathContext (
				packageIdentity,
				globalPackagesFolder,
				NullLogger.Instance,
				PackageSaveMode.Defaultv3,
				PackageExtractionBehavior.XmlDocFileSaveMode);

			downloadResult.PackageStream.Position = 0;
			await PackageExtractor.InstallFromSourceAsync (
				stream => downloadResult.PackageStream.CopyToAsync (stream, BufferSize, token),
				versionFolderPathContext,
				token);
		}

		static Task<string> GetPackagesDirectory (Solution solution)
		{
			return Runtime.RunInMainThread (() => {
				return MSBuildNuGetImportGenerator.GetPackagesRootDirectory (solution);
			});
		}

		public static async Task Download (
			IMonoDevelopSolutionManager solutionManager,
			PackageIdentity packageIdentity,
			INuGetProjectContext context,
			CancellationToken token)
		{
			if (!IsMissing (solutionManager, packageIdentity))
				return;

			using (var sourceCacheContext = new SourceCacheContext ()) {
				var downloadContext = new PackageDownloadContext (sourceCacheContext);

				await PackageDownloader.GetDownloadResourceResultAsync (
					solutionManager.CreateSourceRepositoryProvider ().GetRepositories (),
					packageIdentity,
					downloadContext,
					SettingsUtility.GetGlobalPackagesFolder (solutionManager.Settings),
					new LoggerAdapter (context),
					token);
			}
		}

		public static bool IsMissing (
			IMonoDevelopSolutionManager solutionManager,
			PackageIdentity packageIdentity)
		{
			string globalPackagesFolder = SettingsUtility.GetGlobalPackagesFolder (solutionManager.Settings);
			var defaultPackagePathResolver = new VersionFolderPathResolver (globalPackagesFolder);

			string hashPath = defaultPackagePathResolver.GetHashPath (packageIdentity.Id, packageIdentity.Version);

			return !File.Exists (hashPath);
		}
	}
}
