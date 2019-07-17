// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using NuGet.Protocol.Core.Types;

namespace NuGet.PackageManagement.UI
{
	/// <summary>
	/// Shared base implementation of plain package feeds.
	/// </summary>
	internal abstract class PlainPackageFeedBase : IPackageFeed
	{
		public int PageSize { get; protected set; } = 100;

		// No, it's not.
		public bool IsMultiSource => false;

		public Task<SearchResult<IPackageSearchMetadata>> SearchAsync (string searchText, SearchFilter searchFilter, CancellationToken cancellationToken)
		{
			var searchToken = new FeedSearchContinuationToken {
				SearchString = searchText,
				SearchFilter = searchFilter,
				StartIndex = 0
			};

			return ContinueSearchAsync (searchToken, cancellationToken);
		}

		public abstract Task<SearchResult<IPackageSearchMetadata>> ContinueSearchAsync (ContinuationToken continuationToken, CancellationToken cancellationToken);

		public Task<SearchResult<IPackageSearchMetadata>> RefreshSearchAsync (RefreshToken refreshToken, CancellationToken cancellationToken)
			=> Task.FromResult (SearchResult.Empty<IPackageSearchMetadata> ());
	}
}