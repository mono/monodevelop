//
// BucketTimingsTests.cs
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
using NUnit.Framework;

namespace MonoDevelop.Core.Instrumentation
{
	[TestFixture]
	public class BucketTimingsTests
	{
		[Test]
		public void BucketIndexing ()
		{
			var bucketLimits = ImmutableArray.Create (0, 1, 2, 3, 4);
			var bucket = new BucketTimings (bucketLimits);

			for (int i = 0; i < bucketLimits.Length + 1; ++i)
				bucket.Add (TimeSpan.FromMilliseconds (i));

			var metadata = new CounterMetadata ();
			bucket.AddTo (metadata);

			for (int i = 0; i < bucketLimits.Length + 1; ++i)
				Assert.AreEqual (1, metadata.Properties [$"Bucket{i}"]);

			Assert.That (metadata.Properties, Is.Not.Contains ($"Bucket{bucketLimits.Length + 2}"));
		}
	}
}
