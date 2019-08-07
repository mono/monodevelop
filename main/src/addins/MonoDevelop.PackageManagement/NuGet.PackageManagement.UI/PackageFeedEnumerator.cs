// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Protocol.Core.Types;

namespace NuGet.PackageManagement.UI
{
	internal class PackageFeedEnumerator : IEnumerator<IPackageSearchMetadata>, IDisposable, IEnumerator
	{
		private readonly IPackageFeed _packageFeed;
		private readonly Task<SearchResult<IPackageSearchMetadata>> _startFromTask;
		private readonly CancellationToken _cancellationToken;

		private Task<SearchResult<IPackageSearchMetadata>> _searchTask;
		private IEnumerator<IPackageSearchMetadata> _current;

		private PackageFeedEnumerator (
			IPackageFeed packageFeed,
			Task<SearchResult<IPackageSearchMetadata>> searchTask,
			CancellationToken cancellationToken)
		{
			if (packageFeed == null) {
				throw new ArgumentNullException (nameof (packageFeed));
			}
			_packageFeed = packageFeed;

			if (searchTask == null) {
				throw new ArgumentNullException (nameof (searchTask));
			}
			_startFromTask = searchTask;

			_cancellationToken = cancellationToken;

			Reset ();
		}

		private PackageFeedEnumerator (PackageFeedEnumerator other)
		{
			if (other == null) {
				throw new ArgumentNullException (nameof (other));
			}
			_packageFeed = other._packageFeed;
			_startFromTask = other._startFromTask;
			_cancellationToken = other._cancellationToken;

			Reset ();
		}

		public IPackageSearchMetadata Current => _current.Current;

		object IEnumerator.Current => _current.Current;

		public void Dispose ()
		{
		}

		public bool MoveNext ()
		{
			if (_current.MoveNext ()) {
				return true;
			}

			LoadNextAsync ().Wait ();
			return _current.MoveNext ();
		}

		private async Task LoadNextAsync ()
		{
			var searchResult = await _searchTask;

			while (searchResult.RefreshToken != null) {
				searchResult = await _packageFeed.RefreshSearchAsync (searchResult.RefreshToken, _cancellationToken);
			}

			_current = searchResult.GetEnumerator ();

			if (searchResult.NextToken != null) {
				_searchTask = _packageFeed.ContinueSearchAsync (searchResult.NextToken, _cancellationToken);
			} else {
				_searchTask = Task.FromResult (SearchResult.Empty<IPackageSearchMetadata> ());
			}
		}

		public void Reset ()
		{
			_searchTask = _startFromTask;
			_current = Enumerable.Empty<IPackageSearchMetadata> ().GetEnumerator ();
		}

		public static IEnumerable<IPackageSearchMetadata> Enumerate (
			IPackageFeed packageFeed,
			Task<SearchResult<IPackageSearchMetadata>> searchTask,
			CancellationToken cancellationToken)
		{
			var enumerator = new PackageFeedEnumerator (packageFeed, searchTask, cancellationToken);
			return new PackageFeedEnumerable (enumerator);
		}

		private sealed class PackageFeedEnumerable : IEnumerable<IPackageSearchMetadata>
		{
			private readonly PackageFeedEnumerator _enumerator;

			public PackageFeedEnumerable (PackageFeedEnumerator enumerator)
			{
				_enumerator = enumerator;
			}

			public IEnumerator<IPackageSearchMetadata> GetEnumerator () => new PackageFeedEnumerator (_enumerator);

			IEnumerator IEnumerable.GetEnumerator () => this.GetEnumerator ();
		}
	}
}
