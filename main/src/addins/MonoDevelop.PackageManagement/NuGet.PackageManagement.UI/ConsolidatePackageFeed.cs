// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;

namespace NuGet.PackageManagement.UI
{
	/// <summary>
	/// Package feed facilitating iteration over installed packages needing version consolidation
	/// </summary>
	internal class ConsolidatePackageFeed : PlainPackageFeedBase
	{
		IEnumerable<PackageIdentity> _installedPackages;
		readonly IPackageMetadataProvider _metadataProvider;
		PackageLoadContext _context;

		public ConsolidatePackageFeed (
			PackageLoadContext context,
			IPackageMetadataProvider metadataProvider,
			Common.ILogger logger)
			: this (new PackageIdentity[0], metadataProvider, logger)
		{
			_context = context;
		}

		public ConsolidatePackageFeed (
			IEnumerable<PackageIdentity> installedPackages,
			IPackageMetadataProvider metadataProvider,
			Common.ILogger logger)
		{
			if (installedPackages == null) {
				throw new ArgumentNullException (nameof (installedPackages));
			}
			_installedPackages = installedPackages;

			if (metadataProvider == null) {
				throw new ArgumentNullException (nameof (metadataProvider));
			}
			_metadataProvider = metadataProvider;

			if (logger == null) {
				throw new ArgumentNullException (nameof (logger));
			}

			PageSize = 25;
		}

		public override async Task<SearchResult<IPackageSearchMetadata>> ContinueSearchAsync (ContinuationToken continuationToken, CancellationToken cancellationToken)
		{
			var searchToken = continuationToken as FeedSearchContinuationToken;
			if (searchToken == null) {
				throw new InvalidOperationException ("Invalid token");
			}

			if (_context != null) {
				_installedPackages = await _context.GetInstalledPackagesAsync ();
				_context = null;
			}

			var packagesNeedingConsolidation = _installedPackages
				.GroupById ()
				.Where (g => g.Count () > 1)
				.Select (g => new PackageIdentity (g.Key, g.Max ()))
				.ToArray ();

			var packages = packagesNeedingConsolidation
				.Where (p => p.Id.IndexOf (searchToken.SearchString, StringComparison.OrdinalIgnoreCase) != -1)
				.OrderBy (p => p.Id)
				.Skip (searchToken.StartIndex)
				.Take (PageSize + 1)
				.ToArray ();

			var hasMoreItems = packages.Length > PageSize;
			if (hasMoreItems) {
				packages = packages.Take (packages.Length - 1).ToArray ();
			}

			var items = await TaskCombinators.ThrottledAsync (
				packages,
				(p, t) => _metadataProvider.GetPackageMetadataAsync (p, searchToken.SearchFilter.IncludePrerelease, t),
				cancellationToken);

			var result = SearchResult.FromItems (items.ToArray ());

			var loadingStatus = hasMoreItems
				? LoadingStatus.Ready
				: packages.Length == 0
				? LoadingStatus.NoItemsFound
				: LoadingStatus.NoMoreItems;
			result.SourceSearchStatus = new Dictionary<string, LoadingStatus> {
				{ "Consolidate", loadingStatus }
			};

			if (hasMoreItems) {
				result.NextToken = new FeedSearchContinuationToken {
					SearchString = searchToken.SearchString,
					SearchFilter = searchToken.SearchFilter,
					StartIndex = searchToken.StartIndex + packages.Length
				};
			}

			return result;
		}
	}
}