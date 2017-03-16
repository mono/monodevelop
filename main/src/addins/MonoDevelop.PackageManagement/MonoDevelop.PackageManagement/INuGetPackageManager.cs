//
// INuGetPackageManager.cs
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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.PackageManagement;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;

namespace MonoDevelop.PackageManagement
{
	internal interface INuGetPackageManager
	{
		Task<IEnumerable<NuGetProjectAction>> PreviewInstallPackageAsync (
			NuGetProject nuGetProject,
			PackageIdentity packageIdentity,
			ResolutionContext resolutionContext,
			INuGetProjectContext nuGetProjectContext,
			IEnumerable<SourceRepository> primarySources,
			IEnumerable<SourceRepository> secondarySources,
			CancellationToken token);

		Task<IEnumerable<NuGetProjectAction>> PreviewUpdatePackagesAsync (
			string packageId,
			NuGetProject nuGetProject,
			ResolutionContext resolutionContext,
			INuGetProjectContext nuGetProjectContext,
			IEnumerable<SourceRepository> primarySources,
			IEnumerable<SourceRepository> secondarySources,
			CancellationToken token);

		Task<IEnumerable<NuGetProjectAction>> PreviewUpdatePackagesAsync(
			NuGetProject nuGetProject,
			ResolutionContext resolutionContext,
			INuGetProjectContext nuGetProjectContext,
			IEnumerable<SourceRepository> primarySources,
			IEnumerable<SourceRepository> secondarySources,
			CancellationToken token);

		Task<IEnumerable<NuGetProjectAction>> PreviewUninstallPackageAsync(
			NuGetProject nuGetProject,
			string packageId,
			UninstallationContext uninstallationContext,
			INuGetProjectContext nuGetProjectContext,
			CancellationToken token);

		Task<BuildIntegratedProjectAction> PreviewBuildIntegratedProjectActionsAsync(
			IBuildIntegratedNuGetProject buildIntegratedProject,
			IEnumerable<NuGetProjectAction> nuGetProjectActions,
			INuGetProjectContext nuGetProjectContext,
			CancellationToken token);

		Task<ResolvedPackage> GetLatestVersionAsync (
			string packageId,
			NuGetProject project,
			ResolutionContext resolutionContext,
			IEnumerable<SourceRepository> sources,
			ILogger log,
			CancellationToken token);

		Task ExecuteNuGetProjectActionsAsync (
			NuGetProject nuGetProject,
			IEnumerable<NuGetProjectAction> nuGetProjectActions,
			INuGetProjectContext nuGetProjectContext,
			CancellationToken token);

		void SetDirectInstall (PackageIdentity directInstall, INuGetProjectContext nuGetProjectContext);
		void ClearDirectInstall (INuGetProjectContext nuGetProjectContext);

		bool PackageExistsInPackagesFolder (PackageIdentity packageIdentity);

		Task OpenReadmeFiles (
			NuGetProject project,
			IEnumerable<PackageIdentity> packages,
			INuGetProjectContext nuGetProjectContext,
			CancellationToken token);
	}
}

