//
// NuGetPackageLicenseAuditor.cs
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
using MonoDevelop.Core;
using NuGet.PackageManagement;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol.VisualStudio;

namespace MonoDevelop.PackageManagement
{
	internal class NuGetPackageLicenseAuditor
	{
		List<SourceRepository> sources;
		ILicenseAcceptanceService licenseAcceptanceService;

		public NuGetPackageLicenseAuditor (IEnumerable<SourceRepository> sources)
			: this (
				sources,
				new LicenseAcceptanceService ())
		{
		}

		public NuGetPackageLicenseAuditor (
			IEnumerable<SourceRepository> sources,
			ILicenseAcceptanceService licenseAcceptanceService)
		{
			this.sources = sources.ToList ();
			this.licenseAcceptanceService = licenseAcceptanceService;
		}

		public static Task AcceptLicenses (
			IEnumerable<SourceRepository> sources,
			IEnumerable<NuGetProjectAction> actions,
			CancellationToken cancellationToken)
		{
			var auditor = new NuGetPackageLicenseAuditor (sources);
			return auditor.AcceptLicenses (actions, cancellationToken);
		}

		public async Task AcceptLicenses (
			IEnumerable<NuGetProjectAction> actions,
			CancellationToken cancellationToken)
		{
			var licenses = await GetPackagesWithLicences (actions, cancellationToken);
			if (licenses.Any ()) {
				if (!licenseAcceptanceService.AcceptLicenses (licenses)) {
					throw new ApplicationException (GettextCatalog.GetString ("Licenses not accepted."));
				}
			}
		}

		public async Task<IEnumerable<NuGetPackageLicense>> GetPackagesWithLicences (
			IEnumerable<NuGetProjectAction> actions,
			CancellationToken cancellationToken)
		{
			var licenses = new List<NuGetPackageLicense> ();

			foreach (PackageIdentity package in GetPackages (actions)) {
				NuGetPackageLicense license = await GetPackageLicense (package, cancellationToken);
				if (license != null) {
					licenses.Add (license);
				}
			}

			return licenses;
		}

		async Task<NuGetPackageLicense> GetPackageLicense (
			PackageIdentity package,
			CancellationToken cancellationToken)
		{
			foreach (SourceRepository source in sources) {
				var metadataResource = source.GetResource<UIMetadataResource> ();
				if (metadataResource != null) {
					var packagesMetadata = await metadataResource.GetMetadata (
						package.Id,
						includePrerelease: true,
						includeUnlisted: true,
						token: cancellationToken);

					var metadata = packagesMetadata.FirstOrDefault (p => p.Identity.Version == package.Version);
					if (metadata != null) {
						if (metadata.RequireLicenseAcceptance) {
							return new NuGetPackageLicense (metadata);
						}
						return null;
					}
				}
			}

			return null;
		}

		IEnumerable<PackageIdentity> GetPackages (IEnumerable<NuGetProjectAction> actions)
		{
			var installActions = actions
				.Where (action => action.NuGetProjectActionType == NuGetProjectActionType.Install);

			var packages = new HashSet<PackageIdentity> (PackageIdentity.Comparer);
			foreach (NuGetProjectAction action in installActions) {
				packages.Add (action.PackageIdentity);
			}

			return packages;
		}
	}
}

