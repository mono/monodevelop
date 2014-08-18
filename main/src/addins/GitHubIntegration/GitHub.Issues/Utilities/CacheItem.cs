using System;

namespace GitHub.Issues
{
	/// <summary>
	/// An item which can be cached by the caching utility
	/// </summary>
	public class CacheItem
	{
		/// <summary>
		/// Item to be cached
		/// </summary>
		/// <value>The item.</value>
		public object Item { get; set; }

		/// <summary>
		/// Lifetime in milliseconds of the cached item
		/// </summary>
		/// <value>The lifetime.</value>
		public uint Lifetime { get; set; }

		/// <summary>
		/// Gets or sets the cached at date and time
		/// </summary>
		/// <value>The cached at.</value>
		public DateTime CachedAt { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="GitHub.Issues.CacheItem"/> class.
		/// </summary>
		public CacheItem ()
		{
		}

		/// <summary>
		/// Verifies if the item is still valid or not based on when it was cached and what it's lifetime is
		/// </summary>
		/// <returns><c>true</c> if this instance is valid; otherwise, <c>false</c>.</returns>
		public bool IsValid ()
		{
			int cachedFor = DateTime.Today.Millisecond - CachedAt.Millisecond;

			if (cachedFor <= Lifetime) {
				return true;
			}

			return false;
		}
	}
}

