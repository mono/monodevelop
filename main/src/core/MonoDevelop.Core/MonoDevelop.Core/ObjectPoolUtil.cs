//
// CollectionCache.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corporation. All rights reserved.
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
using Microsoft.Extensions.ObjectPool;

namespace MonoDevelop.Core
{
	public static class ObjectPoolUtil
	{
		const int maximumCollectionSize = 1024;

		public static ObjectPool<List<T>> CreateListPool<T> () => ObjectPool.Create (new PooledListPolicy<T> ());

		public static ObjectPool<HashSet<T>> CreateHashSetPool<T> () => ObjectPool.Create (new PooledHashSetolicy<T> ());

		sealed class PooledHashSetolicy<T> : PooledObjectPolicy<HashSet<T>>
		{
			public override HashSet<T> Create () => new HashSet<T> ();

			public override bool Return (HashSet<T> obj)
			{
				if (obj.Count > maximumCollectionSize)
					return false;
				obj.Clear ();
				return true;
			}
		}

		public static ObjectPool<Dictionary<T, U>> CreateDictionaryPool<T, U> () => ObjectPool.Create (new PooledDictionaryPolicy<T, U> ());

		sealed class PooledDictionaryPolicy<T, U> : PooledObjectPolicy<Dictionary<T, U>>
		{
			public override Dictionary<T, U> Create () => new Dictionary<T, U> ();

			public override bool Return (Dictionary<T, U> obj)
			{
				if (obj.Count > maximumCollectionSize)
					return false;
				obj.Clear ();
				return true;
			}
		}

		public static ObjectPool<Stack<T>> CreateStackPool<T> () => ObjectPool.Create (new PooledStackPolicy<T> ());

		sealed class PooledStackPolicy<T> : PooledObjectPolicy<Stack<T>>
		{
			public override Stack<T> Create () => new Stack<T> ();

			public override bool Return (Stack<T> obj)
			{
				if (obj.Count > maximumCollectionSize)
					return false;
				obj.Clear ();
				return true;
			}
		}

		public static ObjectPool<Queue<T>> CreateQueuePool<T> () => ObjectPool.Create (new PooledQueuePolicy<T> ());

		sealed class PooledQueuePolicy<T> : PooledObjectPolicy<Queue<T>>
		{
			public override Queue<T> Create () => new Queue<T> ();

			public override bool Return (Queue<T> obj)
			{
				if (obj.Count > maximumCollectionSize)
					return false;
				obj.Clear ();
				return true;
			}
		}
	}
}
