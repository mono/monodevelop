//
// LookupTable.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2018 Microsoft
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace MonoDevelop.Core.Collections
{
	/// <summary>
	/// A dictionary that allows multiple items for a single key.
	/// </summary>
	class LookupTable<TKey, TElement>
	{
		Dictionary<TKey, (TElement Item, ImmutableList<TElement>.Builder List)> lookupItems = new Dictionary<TKey, (TElement Item, ImmutableList<TElement>.Builder List)> ();

		public void Add (TKey key, TElement item)
		{
			(TElement Item, ImmutableList<TElement>.Builder List) existingItem;

			if (lookupItems.TryGetValue (key, out existingItem)) {
				if (existingItem.List == null) {
					existingItem.List = ImmutableList.CreateBuilder<TElement> ();
					existingItem.List.Add (existingItem.Item);
					// Need to add the updated tuple back to the dictionary
					// otherwise the list is not added.
					lookupItems [key] = existingItem;
				}
				existingItem.List.Add (item);
			} else {
				lookupItems.Add (key, (item, null));
			}
		}

		public IEnumerable<TElement> GetItems (TKey key)
		{
			(TElement Item, ImmutableList<TElement>.Builder List) existingItem;

			if (lookupItems.TryGetValue (key, out existingItem)) {
				if (existingItem.List != null) {
					return existingItem.List;
				} else {
					return GetSingleItemAsEnumerable (existingItem.Item);
				}
			}

			return Enumerable.Empty<TElement> ();
		}

		IEnumerable<TElement> GetSingleItemAsEnumerable (TElement item)
		{
			yield return item;
		}

		public bool Remove (TKey key, TElement item)
		{
			(TElement Item, ImmutableList<TElement>.Builder List) existingItem;

			if (lookupItems.TryGetValue (key, out existingItem)) {
				if (existingItem.List != null) {
					existingItem.List.Remove (item);
					if (!existingItem.List.Any ()) {
						lookupItems.Remove (key);
					}
				} else {
					lookupItems.Remove (key);
				}
				return true;
			}

			return false;
		}
	}
}
