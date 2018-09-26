//
// BucketTimings.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
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
using System.Collections.Immutable;

namespace MonoDevelop.Core.Instrumentation
{
	public class BucketTimings
	{
		readonly int [] buckets;
		readonly ImmutableArray<int> bucketUpperLimit;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:MonoDevelop.Core.Instrumentation.BucketTimings"/> class,
		/// taking into account the upper limit bucket counts given.
		/// </summary>
		/// <param name="bucketUpperLimit">A sorted array of values to use when trying to bucket.</param>
		public BucketTimings (ImmutableArray<int> bucketUpperLimit)
		{
			this.bucketUpperLimit = bucketUpperLimit;

			// One more than bucketUpperLimit because the last bucket is everything else.
			// This number is the max time a keystroke can take to be placed into this bucket
			buckets = new int [bucketUpperLimit.Length + 1];
		}

		int CalculateBucket (TimeSpan duration)
		{
			long ms = (long)duration.TotalMilliseconds;
			for (var bucket = 0; bucket < bucketUpperLimit.Length; bucket++) {
				if (ms <= bucketUpperLimit [bucket]) {
					return bucket;
				}
			}

			return buckets.Length - 1;
		}

		public void Add (TimeSpan duration)
		{
			var bucketNumber = CalculateBucket (duration);
			buckets [bucketNumber]++;
		}

		public void AddTo (CounterMetadata metadata)
		{
			for (var bucket = 0; bucket < buckets.Length; bucket++) {
				metadata.Properties [$"Bucket{bucket}"] = buckets [bucket];
			}
		}
	}
}
