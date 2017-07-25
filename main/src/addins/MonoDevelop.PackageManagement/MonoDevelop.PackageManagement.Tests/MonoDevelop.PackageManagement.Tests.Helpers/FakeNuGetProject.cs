//
// FakeNuGetProject.cs
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
using NuGet.Frameworks;
using NuGet.PackageManagement;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	class FakeNuGetProject : NuGetProject, IBuildIntegratedNuGetProject
	{
		public FakeNuGetProject (IDotNetProject project)
		{
			Project = project;
			if (project.Name != null) {
				InternalMetadata.Add (NuGetProjectMetadataKeys.Name, project.Name);
			}
		}

		public IDotNetProject Project { get; private set; }

		public List<PackageReference> InstalledPackages = new List<PackageReference> ();

		public override Task<IEnumerable<PackageReference>> GetInstalledPackagesAsync (CancellationToken token)
		{
			return Task.FromResult (InstalledPackages.AsEnumerable ());
		}

		public override Task<bool> InstallPackageAsync (PackageIdentity packageIdentity, DownloadResourceResult downloadResourceResult, INuGetProjectContext nuGetProjectContext, CancellationToken token)
		{
			throw new NotImplementedException ();
		}

		public override Task<bool> UninstallPackageAsync (PackageIdentity packageIdentity, INuGetProjectContext nuGetProjectContext, CancellationToken token)
		{
			throw new NotImplementedException ();
		}

		public void AddPackageReference (string id, string version, VersionRange versionRange = null)
		{
			var packageId = new PackageIdentity (id, new NuGetVersion (version));
			var packageReference = new PackageReference (
				packageId,
				new NuGetFramework ("net45"),
				true,
				false,
				false,
				versionRange
			);
			InstalledPackages.Add (packageReference);
		}

		public List<NuGetProjectAction> ActionsPassedToOnBeforeUninstall;

		public void OnBeforeUninstall (IEnumerable<NuGetProjectAction> actions)
		{
			ActionsPassedToOnBeforeUninstall = actions.ToList ();
		}

		public List<NuGetProjectAction> ActionsPassedToOnAfterExecuteActions;

		public void OnAfterExecuteActions (IEnumerable<NuGetProjectAction> actions)
		{
			ActionsPassedToOnAfterExecuteActions = actions.ToList ();
		}

		public INuGetProjectContext PostProcessProjectContext;
		public CancellationToken PostProcessCancellationToken;

		public override Task PostProcessAsync (INuGetProjectContext nuGetProjectContext, CancellationToken token)
		{
			PostProcessProjectContext = nuGetProjectContext;
			PostProcessCancellationToken = token;

			return Task.FromResult (0);
		}

		public void NotifyProjectReferencesChanged (bool includeTransitiveProjectReferences)
		{
		}
	}
}

