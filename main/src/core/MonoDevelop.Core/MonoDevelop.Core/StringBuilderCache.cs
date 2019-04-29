//
// StringBuilderCache.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
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
using System.Text;
using Microsoft.Extensions.ObjectPool;

namespace MonoDevelop.Core
{
	/// <summary>
	/// This is a pool for storing StringBuilder objects.
	/// </summary>
	public static class StringBuilderCache
	{
		static readonly ObjectPool<StringBuilder> pool = new DefaultObjectPoolProvider ().Create (new StringBuilderClearingPooledObjectPolicy ());

		public static StringBuilder Allocate () => pool.Get ();
		public static void Free (StringBuilder sb) => pool.Return (sb);

		public static StringBuilder Allocate (string text)
		{
			return Allocate ().Append (text);
		}

		public static string ReturnAndFree (StringBuilder sb)
		{
			var result = sb.ToString ();
			Free (sb);
			return result;
		}

		class StringBuilderClearingPooledObjectPolicy : PooledObjectPolicy<StringBuilder>
		{
			// A lot of StringBuilders will usually just end up with a small number of appends, so keep initial capacity at 20.
			const int InitialCapacity = 20;

			// Prevent retaining too much memory via stringbuilders with big internal buffer capacity, by trimming them to 4k.
			const int MaximumRetainedCapacity = 4 * 1024;

			public override StringBuilder Create () => new StringBuilder (InitialCapacity);

			public override bool Return (StringBuilder obj)
			{
				if (obj.Capacity > MaximumRetainedCapacity) {
					// PERF: Benchmark showed that first trimming to the capacity, then setting the capacity, then clearing the stringbuilder
					// improves clearing times quite a bit.
					// Benchmark code: https://gist.github.com/Therzok/8ca14d02e14ef4c4bff613b2ecca7f7f
					obj.Length = Math.Min (obj.Length, MaximumRetainedCapacity);

					// Trim the capacity so we don't retain too much memory.
					obj.Capacity = MaximumRetainedCapacity;
				}
				obj.Clear ();

				return true;
			}
		}
	}
}