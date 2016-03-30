// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol.Core.v2;

namespace NuGet.Protocol.VisualStudio
{
	internal class UISearchResourceV2 : UISearchResource
	{
		private IPackageRepository V2Client { get; }

		public UISearchResourceV2(V2Resource resource)
		{
			V2Client = resource.V2Client;
		}

		public UISearchResourceV2(IPackageRepository repo)
		{
			V2Client = repo;
		}

		public override async Task<IEnumerable<UISearchMetadata>> Search(string searchTerm,
		                                                                 SearchFilter filters,
		                                                                 int skip,
		                                                                 int take,
		                                                                 CancellationToken cancellationToken)
		{
			return await GetSearchResultsForVisualStudioUIAsync(searchTerm, filters, skip, take, cancellationToken);
		}

		private async Task<IEnumerable<UISearchMetadata>> GetSearchResultsForVisualStudioUIAsync(string searchTerm,
		                                                                                         SearchFilter filters,
		                                                                                         int skip,
		                                                                                         int take,
		                                                                                         CancellationToken cancellationToken)
		{
			return await Task.Run(() =>
			{
				// Check if source is available.
				if (!IsHttpSource(V2Client.Source) && !IsLocalOrUNC(V2Client.Source))
				{
					throw new InvalidOperationException(
						String.Format ("The path '{0}' for the selected source could not be resolved.", V2Client.Source));
				}

				var query = V2Client.Search(
					searchTerm,
					filters.SupportedFrameworks,
					filters.IncludePrerelease);

				// V2 sometimes requires that we also use an OData filter for
				// latest /latest prerelease version
				if (filters.IncludePrerelease)
				{
					query = query.Where(p => p.IsAbsoluteLatestVersion);
				}
				else
				{
					query = query.Where(p => p.IsLatestVersion);
				}
				query = query.OrderByDescending(p => p.DownloadCount)
				             .ThenBy(p => p.Id);

				// Some V2 sources, e.g. NuGet.Server, local repository, the result contains all
				// versions of each package. So we need to group the result by Id.
				var collapsedQuery = query.AsEnumerable().AsCollapsed();

				// execute the query
				var allPackages = collapsedQuery
					.Skip(skip)
					.Take(take)
					.ToList();

				var results = new List<UISearchMetadata>();

				foreach (var package in allPackages)
				{
					results.Add(CreatePackageSearchResult(package, filters, cancellationToken));
				}

				return results;
			});
		}

		private UISearchMetadata CreatePackageSearchResult(IPackage package,
		                                                   SearchFilter filters,
		                                                   CancellationToken cancellationToken)
		{
			var id = package.Id;
			var version = V2Utilities.SafeToNuGetVer(package.Version);
			var title = package.Title;
			var summary = package.Summary;

			if (string.IsNullOrWhiteSpace(summary))
			{
				summary = package.Description;
			}

			if (string.IsNullOrEmpty(title))
			{
				title = id;
			}

			var iconUrl = package.IconUrl;
			var identity = new PackageIdentity(id, version);

			var versions = new Lazy<Task<IEnumerable<VersionInfo>>>(() =>
			                                                        GetVersionInfoAsync(package, filters, CancellationToken.None));

			var searchMetaData = new UISearchMetadata(
				identity,
				title,
				summary,
				string.Join(", ", package.Authors),
				package.DownloadCount,
				iconUrl,
				versions,
				UIMetadataResourceV2.GetVisualStudioUIPackageMetadata(package));

			return searchMetaData;
		}

		public Task<IEnumerable<VersionInfo>> GetVersionInfoAsync(IPackage package,
		                                                          SearchFilter filters,
		                                                          CancellationToken cancellationToken)
		{
			return Task.Run(() =>
			{
				cancellationToken.ThrowIfCancellationRequested();

				// apply the filters to the version list returned
				var versions = V2Client.FindPackagesById(package.Id)
				                       .Where(p => filters.IncludeDelisted || !p.Published.HasValue || p.Published.Value.Year > 1901)
				                       .Where(v => filters.IncludePrerelease || string.IsNullOrEmpty(v.Version.SpecialVersion)).ToArray();

				if (!versions.Any())
				{
					versions = new[] { package };
				}

				var nuGetVersions = versions.Select(p =>
				                                    new VersionInfo(V2Utilities.SafeToNuGetVer(p.Version), p.DownloadCount));

				return nuGetVersions;
			});
		}

		private static bool IsHttpSource(string source)
		{
			if (string.IsNullOrEmpty(source))
			{
				return false;
			}

			Uri uri;
			if (Uri.TryCreate(source, UriKind.Absolute, out uri))
			{
				return (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
			}
			else
			{
				return false;
			}
		}

		private static bool IsLocalOrUNC(string currentSource)
		{
			Uri currentURI;
			if (Uri.TryCreate(currentSource, UriKind.RelativeOrAbsolute, out currentURI))
			{
				if (currentURI.IsFile || currentURI.IsUnc)
				{
					if (Directory.Exists(currentSource))
					{
						return true;
					}
				}
			}

			return false;
		}
	}
}
