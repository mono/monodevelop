//
// CoreExtensions.Memoize.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2019 Microsoft Inc.
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

namespace System
{
	public static partial class CoreExtensions
	{
		public static Func<R> Memoize<R> (this Func<R> f)
		{
			R value = default;
			bool hasValue = false;

			return () => {
				if (!hasValue) {
					hasValue = true;
					value = f ();
				}
				return value;
			};
		}

		public static Func<T1, R> Memoize<T1, R> (this Func<T1, R> f)
		{
			var map = new Dictionary<T1, R> ();
			return a => {
				if (!map.TryGetValue (a, out R value)) {
					map [a] = value = f (a);
				}
				return value;
			};
		}

		public static Func<T1, R> MemoizeWithLock<T1, R> (this Func<T1, R> f)
		{
			var map = new Dictionary<T1, R> ();
			return a => {
				lock (map) {
					if (!map.TryGetValue (a, out R value)) {
						map[a] = value = f (a);
					}
					return value;
				}
			};
		}

		public static Func<T1, T2, R> Memoize<T1, T2, R> (this Func<T1, T2, R> f)
		{
			var map = new Dictionary<(T1, T2), R> ();
			return (a, b) => {
				var key = (a, b);
				if (!map.TryGetValue (key, out R value)) {
					map [key] = value = f (a, b);
				}
				return value;
			};
		}

		public static Func<T1, T2, R> MemoizeWithLock<T1, T2, R> (this Func<T1, T2, R> f)
		{
			var map = new Dictionary<(T1, T2), R> ();
			return (a, b) => {
				var key = (a, b);
				lock (map) {
					if (!map.TryGetValue (key, out R value)) {
						map [key] = value = f (a, b);
					}
					return value;
				}
			};
		}

		static class MemoizeUtil<Value>
		{
			public static Dictionary<Key, Value> Create<Key> (Key justForFancyTypeInference)
				=> new Dictionary<Key, Value> ();
		}
	}
}
