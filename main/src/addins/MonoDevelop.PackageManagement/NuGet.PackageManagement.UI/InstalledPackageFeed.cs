// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.PackageManagement;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;

namespace NuGet.PackageManagement.UI
{
	/// <summary>
	/// Represents a package feed enumerating installed packages.
	/// </summary>
	internal class InstalledPackageFeed : PlainPackageFeedBase
	{
		IEnumerable<PackageIdentity> _installedPackages;
		readonly IPackageMetadataProvider _metadataProvider;
		ManagePackagesLoadContext _context;

		public InstalledPackageFeed (
			ManagePackagesLoadContext context,
			IPackageMetadataProvider metadataProvider,
			Common.ILogger logger)
			: this (new PackageIdentity[0], metadataProvider, logger)
		{
			this._context = context;
		}

		public InstalledPackageFeed (
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

			var packages = _installedPackages
				.GetLatest ()
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
				(p, t) => GetPackageMetadataAsync (p, searchToken.SearchFilter.IncludePrerelease, t),
				cancellationToken);

			//  The packages were originally sorted which is important because we Skip and Take based on that sort
			//  however the asynchronous execution has randomly reordered the set. So we need to resort. 
			var result = SearchResult.FromItems (items.OrderBy (p => p.Identity.Id).ToArray ());

			var loadingStatus = hasMoreItems
				? LoadingStatus.Ready
				: packages.Length == 0
				? LoadingStatus.NoItemsFound
				: LoadingStatus.NoMoreItems;
			result.SourceSearchStatus = new Dictionary<string, LoadingStatus> {
				{ "Installed", loadingStatus }
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

		async Task<IPackageSearchMetadata> GetPackageMetadataAsync (PackageIdentity identity, bool includePrerelease, CancellationToken cancellationToken)
		{
			// first we try and load the metadata from a local package
			var packageMetadata = await _metadataProvider.GetLocalPackageMetadataAsync (identity, includePrerelease, cancellationToken);
			if (packageMetadata == null) {
				// and failing that we go to the network
				packageMetadata = await _metadataProvider.GetPackageMetadataAsync (identity, includePrerelease, cancellationToken);
			}
			return packageMetadata;
		}
	}
}