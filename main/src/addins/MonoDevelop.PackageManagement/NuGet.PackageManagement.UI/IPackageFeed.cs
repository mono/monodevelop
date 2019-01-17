// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using NuGet.Protocol.Core.Types;

namespace NuGet.PackageManagement.UI
{
	/// <summary>
	/// Package feed abstraction providing services of package enumeration with search criteria.
	/// Supports pagination and background processing.
	/// </summary>
	internal interface IPackageFeed
	{
		bool IsMultiSource { get; }

		/// <summary>
		/// Starts new search.
		/// </summary>
		/// <param name="searchText">Optional text to search</param>
		/// <param name="filter">Combined search filter</param>
		/// <param name="cancellationToken">A cancellation token</param>
		/// <returns>Search result. Possible outcome</returns>
		Task<SearchResult<IPackageSearchMetadata>> SearchAsync(
			string searchText, SearchFilter filter, CancellationToken cancellationToken);

		/// <summary>
		/// Proceeds with loading of next page using the same search criteria.
		/// </summary>
		/// <param name="continuationToken">Search state as returned with previous search result</param>
		/// <param name="cancellationToken">A cancellation token</param>
		/// <returns>Search result</returns>
		Task<SearchResult<IPackageSearchMetadata>> ContinueSearchAsync(
			ContinuationToken continuationToken, CancellationToken cancellationToken);

		/// <summary>
		/// Retrieves a search result of a background search operation.
		/// </summary>
		/// <param name="refreshToken">Search state as returned with previous search result</param>
		/// <param name="cancellationToken">A cancellation token</param>
		/// <returns>Refreshed search result</returns>
		Task<SearchResult<IPackageSearchMetadata>> RefreshSearchAsync(
			RefreshToken refreshToken, CancellationToken cancellationToken);
	}
}
