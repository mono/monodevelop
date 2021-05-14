// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace NuGet.PackageManagement.UI
{
	public class DetailedPackageMetadata
	{
		public DetailedPackageMetadata()
		{
		}

		public DetailedPackageMetadata(IPackageSearchMetadata serverData, long? downloadCount)
		{
			Id = serverData.Identity.Id;
			Version = serverData.Identity.Version;
			Summary = serverData.Summary;
			Description = serverData.Description;
			Authors = serverData.Authors;
			Owners = serverData.Owners;
			IconUrl = serverData.IconUrl;
			LicenseUrl = serverData.LicenseUrl;
			ProjectUrl = serverData.ProjectUrl;
			ReportAbuseUrl = serverData.ReportAbuseUrl;
			Tags = serverData.Tags;
			DownloadCount = downloadCount;
			Published = serverData.Published;
			DependencySets = serverData.DependencySets?
				.Select(e => new PackageDependencySetMetadata(e))
				?? new PackageDependencySetMetadata[] { };
			HasDependencies = DependencySets.Any(
				dependencySet => dependencySet.Dependencies != null && dependencySet.Dependencies.Count > 0);
			LicenseMetadata = serverData.LicenseMetadata;
			localMetadata = serverData as LocalPackageSearchMetadata;
		}

		readonly LocalPackageSearchMetadata localMetadata;

		public string Id { get; set; }

		public NuGetVersion Version { get; set; }

		public string Summary { get; set; }

		public string Description { get; set; }

		public string Authors { get; set; }

		public string Owners { get; set; }

		public Uri IconUrl { get; set; }

		public Uri LicenseUrl { get; set; }

		public Uri ProjectUrl { get; set; }

		public Uri ReportAbuseUrl { get; set; }

		public string Tags { get; set; }

		public long? DownloadCount { get; set; }

		public DateTimeOffset? Published { get; set; }

		public IEnumerable<PackageDependencySetMetadata> DependencySets { get; set; }

		// This property is used by data binding to display text "No dependencies"
		public bool HasDependencies { get; set; }

		public LicenseMetadata LicenseMetadata { get; set; }

		public IReadOnlyList<IText> LicenseLinks => PackageLicenseUtilities.GenerateLicenseLinks (this);

		public string LoadFileAsText (string path)
		{
			if (localMetadata != null) {
				return localMetadata.LoadFileAsText (path);
			}
			return null;
		}
	}
}