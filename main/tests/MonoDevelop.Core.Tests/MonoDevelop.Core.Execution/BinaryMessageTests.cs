//
// BinaryMessage.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2017 
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
using NUnit.Framework;

namespace MonoDevelop.Core.Execution
{
	[TestFixture]
	public class BinaryMessageTests
	{
		public class Data
		{
			public Data (Array arr, int expectedEnumerations, int[][] expectedValues)
			{
				ExpectedEnumerations = expectedEnumerations;
				Array = arr;
			}

			public int ExpectedEnumerations { get; }
			public Array Array { get; }
		}

		public Data [] TestCase = {
			new Data (new int[1, 2, 3], 6, new int[][] {
				new int[] {0, 0, 0},
				new int[] {0, 0, 1},
				new int[] {0, 0, 2},
				new int[] {0, 1, 0},
				new int[] {0, 1, 1},
				new int[] {0, 1, 2},
			}),
			new Data (new int[0, 0, 0], 0, new int[][] {}),
			new Data (new int[0, 0, 1], 1, new int[][] { new int[] { 0, 0, 0 } }),
			new Data (new int[3, 3, 3], 27, new int[][] {
				new int[] {0, 0, 0},
				new int[] {0, 0, 1},
				new int[] {0, 0, 2},

				new int[] {0, 1, 0},
				new int[] {0, 1, 1},
				new int[] {0, 1, 2},

				new int[] {0, 2, 0},
				new int[] {0, 2, 1},
				new int[] {0, 2, 2},

				new int[] {1, 0, 0},
				new int[] {1, 0, 1},
				new int[] {1, 0, 2},

				new int[] {1, 1, 0},
				new int[] {1, 1, 1},
				new int[] {1, 1, 2},

				new int[] {1, 2, 0},
				new int[] {1, 2, 1},
				new int[] {1, 2, 2},

				new int[] {2, 0, 0},
				new int[] {2, 0, 1},
				new int[] {2, 0, 2},

				new int[] {2, 1, 0},
				new int[] {2, 1, 1},
				new int[] {2, 1, 2},

				new int[] {2, 2, 0},
				new int[] {2, 2, 1},
				new int[] {2, 2, 2},
			}),
		};

		[TestCaseSource ("TestCase")]
		public void TestIteration (Data data)
		{
			var iter = new BinaryMessage.MultiDimensionalIterator (data.Array);
			int count = 0;
			(bool, int []) res;
			while ((res = iter.TryMoveNext ()).Item1) {
				count++;
			}

			Assert.AreEqual (data.ExpectedEnumerations, count);
		}

		[Test]
		public void TestFill ()
		{
			int count = 0;
			var toFill = new int [3, 3, 3];

			var iter = new BinaryMessage.MultiDimensionalIterator (toFill);
			iter.Fill (() => count++);

			int expected = 0;
			foreach (var val in toFill) {
				Assert.AreEqual (expected, val);
				expected++;
			}
		}
	}
}
