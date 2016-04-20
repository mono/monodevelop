// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol.Core.v3;
using NuGet.Versioning;

namespace NuGet.Protocol.VisualStudio
{
	internal class UISearchResourceV3 : UISearchResource
	{
		private readonly RawSearchResourceV3 _searchResource;
		private readonly UIMetadataResource _metadataResource;

		public UISearchResourceV3(RawSearchResourceV3 searchResource, UIMetadataResource metadataResource)
			: base()
		{
			_searchResource = searchResource;
			_metadataResource = metadataResource;
		}

		public override async Task<IEnumerable<UISearchMetadata>> Search(string searchTerm,
		                                                                 SearchFilter filters,
		                                                                 int skip,
		                                                                 int take,
		                                                                 CancellationToken cancellationToken)
		{
			var searchResults = new List<UISearchMetadata>();

			var searchResultJsonObjects = await _searchResource.Search(searchTerm, filters, skip, take, cancellationToken);

			foreach (var searchResultJson in searchResultJsonObjects)
			{
				searchResults.Add(await GetSearchResult(searchResultJson, filters.IncludePrerelease, cancellationToken));
			}

			return searchResults;
		}

		private async Task<UISearchMetadata> GetSearchResult(JObject jObject, bool includePrerelease, CancellationToken token)
		{
			var id = jObject.GetString(Properties.PackageId);
			var version = NuGetVersion.Parse(jObject.GetString(Properties.Version));

			var topPackage = new PackageIdentity(id, version);
			var iconUrl = jObject.GetUri(Properties.IconUrl);
			var summary = jObject.GetString(Properties.Summary);

			if (string.IsNullOrWhiteSpace(summary))
			{
				// summary is empty. Use its description instead.
				summary = jObject.GetString(Properties.Description);
			}

			var title = jObject.GetString(Properties.Title);
			if (string.IsNullOrEmpty(title))
			{
				// Use the id instead of the title when no title exists.
				title = id;
			}

			// get other versions
			var versionList = GetLazyVersionList(jObject, includePrerelease, version);

			// retrieve metadata for the top package
			UIPackageMetadata metadata = null;

			var v3MetadataResult = _metadataResource as UIMetadataResourceV3;

			// for v3 just parse the data from the search results
			if (v3MetadataResult != null)
			{
				metadata = v3MetadataResult.ParseMetadata(jObject);
			}

			// if we do not have a v3 metadata resource, request it using whatever is available
			if (metadata == null)
			{
				metadata = await _metadataResource.GetMetadata(topPackage, token);
			}

			var searchResult = new UISearchMetadata(
				topPackage,
				title,
				summary,
				string.Join(", ", metadata.Authors),
				metadata.DownloadCount,
				iconUrl,
				versionList,
				metadata);
			return searchResult;
		}

		private static Lazy<Task<IEnumerable<VersionInfo>>> GetLazyVersionList(JObject package,
		                                                                       bool includePrerelease,
		                                                                       NuGetVersion version)
		{
			return new Lazy<Task<IEnumerable<VersionInfo>>>(() =>
			{
				var versionList = GetVersionList(package, includePrerelease, version);

				return Task.FromResult(versionList);
			});
		}

		private static IEnumerable<VersionInfo> GetVersionList(JObject package, bool includePrerelease, NuGetVersion version)
		{
			var versionList = new List<VersionInfo>();
			var versions = package.GetJArray(Properties.Versions);

			if (versions != null)
			{
				foreach (var v in versions)
				{
					var nugetVersion = NuGetVersion.Parse(v.Value<string>("version"));
					var count = v.Value<int?>("downloads");
					versionList.Add(new VersionInfo(nugetVersion, count));
				}
			}

			// TODO: in v2, we only have download count for all versions, not per version.
			// To be consistent, in v3, we also use total download count for now.
			var totalDownloadCount = versionList.Select(v => v.DownloadCount).Sum();
			versionList = versionList.Select(v => new VersionInfo(v.Version, totalDownloadCount))
			                         .ToList();

			if (!includePrerelease)
			{
				// remove prerelease version if includePrelease is false
				versionList.RemoveAll(v => v.Version.IsPrerelease);
			}

			if (!versionList.Select(v => v.Version).Contains(version))
			{
				versionList.Add(new VersionInfo(version, 0));
			}

			return versionList;
		}
	}
}
