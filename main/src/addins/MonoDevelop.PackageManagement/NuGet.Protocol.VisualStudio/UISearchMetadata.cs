// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NuGet.Packaging.Core;

namespace NuGet.Protocol.VisualStudio
{
	/// <summary>
	/// Model for Search results displayed by Visual Studio Package Manager dialog UI.
	/// </summary>
	internal class UISearchMetadata
	{
		public UISearchMetadata(PackageIdentity identity,
		                        string title,
		                        string summary,
		                        string author,
		                        long? downloadCount,
		                        Uri iconUrl,
		                        Lazy<Task<IEnumerable<VersionInfo>>> versions,
		                        UIPackageMetadata latestPackageMetadata)
		{
			Identity = identity;
			Title = title;
			Summary = summary;
			IconUrl = iconUrl;
			Versions = versions;
			Author = author;
			DownloadCount = downloadCount;
			LatestPackageMetadata = latestPackageMetadata;
		}

		public PackageIdentity Identity { get; }

		public string Summary { get; }

		public Uri IconUrl { get; }

		public Lazy<Task<IEnumerable<VersionInfo>>> Versions { get; }

		public UIPackageMetadata LatestPackageMetadata { get; }

		public string Title { get; }

		public string Author { get; }

		public long? DownloadCount { get; }
	}
}
