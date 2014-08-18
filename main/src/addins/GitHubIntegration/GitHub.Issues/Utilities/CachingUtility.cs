using System;
using System.Collections;
using System.Collections.Generic;

namespace GitHub.Issues
{
	/// <summary>
	/// A caching utility to promote image loading time etc.
	/// </summary>
	public static class CachingUtility
	{
		/// <summary>
		/// Approximately 300 items will be held at max during the runtime of the program - if more then we rehash automatically
		/// Load factor is 0.6
		/// </summary>
		private static Hashtable cacheTable = new Hashtable (300, 0.6f);

		/// <summary>
		/// The default life time - 6 hour in milliseconds
		/// Should only be used for items which are unlikely to change like avatars etc.
		/// </summary>
		public static readonly uint DefaultLifeTime = 21600000;

		/// <summary>
		/// Caches the item for a "lifetime" number of milliseconds.
		/// </summary>
		/// <param name="key">Key.</param>
		/// <param name="item">Item.</param>
		/// <param name="lifetime">Lifetime - milliseconds.</param>
		public static void CacheItem (string key, object item, uint lifetime)
		{
			cacheTable.Add (key, new GitHub.Issues.CacheItem () { Item = item, Lifetime = lifetime });
		}

		/// <summary>
		/// Gets the cached item or returns null if item doesn't exist or has expired
		/// </summary>
		/// <returns>The cached item.</returns>
		/// <param name="key">Key.</param>
		public static object GetCachedItem (string key)
		{
			if (cacheTable.Contains (key)) {
				CacheItem cachedItem = (CacheItem)cacheTable [key];

				if (cachedItem.IsValid ()) {
					return cachedItem.Item;
				}
			}

			return null;
		}
	}
}

