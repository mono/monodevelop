//
// CoreExtensionsTests.cs
//
// Author:
//       Marius Ungureanu <marius.ungureanu@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc (http://www.xamarin.com)
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
using System.Linq;
using NUnit.Framework;

namespace MonoDevelop.Core
{
	[TestFixture]
	public class CoreExtensionsTests
	{
		[SetUp]
		public void SetUp ()
		{
			memoTest1CallCount = 0;
			memoTest2CallCount = 0;
		}

		[Test]
		public void ConcatWorks ()
		{
			string toAdd = "Test String";

			var initial = Enumerable.Repeat (toAdd, 4);
			var expected = Enumerable.Repeat (toAdd, 5);

			Assert.That (expected, Is.EquivalentTo (initial.Concat (toAdd)));
		}

		[TestCase ("item1", "item2", "item1", 0)]
		[TestCase ("item1", "item2", "item2", 1)]
		[TestCase ("item1", "item2", "item3", -1)]
		public void IndexOfWorks (string toAdd1, string toAdd2, string toFind, int expectedIndex)
		{
			var toSearch = Enumerable.Empty<string> ().Concat (toAdd1).Concat (toAdd2);

			Assert.AreEqual (expectedIndex, toSearch.IndexOf (toFind));
		}

		[TestCase ("item1", "item2", "item1", 0)]
		[TestCase ("item1", "item2", "item2", 1)]
		[TestCase ("item1", "item2", "item3", -1)]
		public void FindIndexWorks (string toAdd1, string toAdd2, string toFind, int expectedIndex)
		{
			var toSearch = Enumerable.Empty<string> ().Concat (toAdd1).Concat (toAdd2);

			Assert.AreEqual (expectedIndex, toSearch.FindIndex (i => i == toFind));
		}

		static int memoTest1CallCount;
		static object MemoTest1 ()
		{
			memoTest1CallCount++;
			return new object ();
		}

		[Test]
		public void TestMemoizeMethod1 ()
		{
			var f1 = CoreExtensions.Memoize (MemoTest1);
			var obj1 = f1 ();
			var obj2 = f1 ();
			Assert.AreSame (obj1, obj2);
			Assert.AreEqual (1, memoTest1CallCount);
		}

		[Test]
		public void TestMemoizeMethodsAreDifferent ()
		{
			var f1 = CoreExtensions.Memoize (MemoTest1);
			var f2 = CoreExtensions.Memoize (MemoTest1);

			Assert.AreNotSame (f1(), f2());
			Assert.AreEqual (2, memoTest1CallCount);
		}

		static int memoTest2CallCount;
		const int knownArg = 1;
		static object MemoTest2 (int a)
		{
			memoTest2CallCount++;
			if (a == knownArg)
				return new object ();
			return new object ();
		}

		[Test]
		public void TestMemoizeMethod2 ()
		{
			var f2 = CoreExtensions.Memoize<int, object> (MemoTest2);
			var obj11 = f2 (knownArg);
			var obj12 = f2 (knownArg);
			var obj21 = f2 (knownArg + 1);
			var obj22 = f2 (knownArg + 1);
			Assert.AreSame (obj11, obj12);
			Assert.AreSame (obj21, obj22);
			Assert.AreNotSame (obj11, obj22);
			Assert.AreEqual (2, memoTest2CallCount);
		}

		static int memoTest3CallCount;
		static object MemoTest3 (int a, int b)
		{
			memoTest3CallCount++;
			if (a == knownArg && b == knownArg)
				return new object ();
			
			if (a != knownArg) {
				if (b != knownArg)
					return new object ();
				return new object ();
			}
			return new object ();
		}

		[Test]
		public void TestMemoizeMethod3 ()
		{
			var f2 = CoreExtensions.Memoize<int, int, object> (MemoTest3);
			var obj1 = f2 (knownArg, knownArg);
			var obj2 = f2 (knownArg, knownArg + 1);
			var obj3 = f2 (knownArg + 1, knownArg);
			var obj4 = f2 (knownArg + 1, knownArg + 1);
			Assert.AreNotSame (obj1, obj2);
			Assert.AreNotSame (obj2, obj3);
			Assert.AreNotSame (obj3, obj4);

			// Force this, we already test the general algorithm in memo1 and memo2.
			obj1 = f2 (knownArg, knownArg);
			Assert.AreEqual (4, memoTest3CallCount);
		}

		class DateTimeWrapper
		{
			public DateTime DateTime;
		}

		readonly DateTimeWrapper[] dateTimeSource = {
			new DateTimeWrapper { DateTime = new DateTime (2016, 01, 11) },
			new DateTimeWrapper { DateTime = new DateTime (2016, 01, 10) },
			new DateTimeWrapper { DateTime = new DateTime (2016, 01, 10) },
			new DateTimeWrapper { DateTime = new DateTime (2016, 01, 16) },
			new DateTimeWrapper { DateTime = new DateTime (2016, 01, 14) },
		};

		readonly DateTimeWrapper [] defaultDateTimeSource = { };

		class DateTimeComparer : IComparer<DateTime>
		{
			public int Compare (DateTime x, DateTime y)
			{
				return x.CompareTo (y);
			}
		}

		[Test]
		public void TestMaxExtension ()
		{
			Assert.AreSame (dateTimeSource [3], dateTimeSource.MaxValue (dtw => dtw.DateTime));
			Assert.AreSame (dateTimeSource [3], dateTimeSource.MaxValue (dtw => dtw.DateTime, new DateTimeComparer ()));
			Assert.AreSame (dateTimeSource [3], dateTimeSource.MaxValueOrDefault (dtw => dtw.DateTime));
			Assert.AreSame (dateTimeSource [3], dateTimeSource.MaxValueOrDefault (dtw => dtw.DateTime, new DateTimeComparer ()));
		}

		[Test]
		public void TestMaxOrDefaultExtension ()
		{
			Assert.Throws<InvalidOperationException> (() => defaultDateTimeSource.MaxValue (dtw => dtw.DateTime));
			Assert.Throws<InvalidOperationException> (() => defaultDateTimeSource.MaxValue (dtw => dtw.DateTime, new DateTimeComparer ()));
			Assert.AreEqual (null, defaultDateTimeSource.MaxValueOrDefault (dtw => dtw.DateTime));
			Assert.AreEqual (null, defaultDateTimeSource.MaxValueOrDefault (dtw => dtw.DateTime, new DateTimeComparer ()));
		}

		[Test]
		public void TestMinExtension ()
		{
			Assert.AreSame (dateTimeSource [1], dateTimeSource.MinValue (dtw => dtw.DateTime));
			Assert.AreSame (dateTimeSource [1], dateTimeSource.MinValue (dtw => dtw.DateTime, new DateTimeComparer ()));
			Assert.AreSame (dateTimeSource [1], dateTimeSource.MinValueOrDefault (dtw => dtw.DateTime));
			Assert.AreSame (dateTimeSource [1], dateTimeSource.MinValueOrDefault (dtw => dtw.DateTime, new DateTimeComparer ()));
		}

		[Test]
		public void TestMinOrDefaultExtension ()
		{
			Assert.Throws<InvalidOperationException> (() => defaultDateTimeSource.MinValue (dtw => dtw.DateTime));
			Assert.Throws<InvalidOperationException> (() => defaultDateTimeSource.MinValue (dtw => dtw.DateTime, new DateTimeComparer ()));
			Assert.AreEqual (null, defaultDateTimeSource.MinValueOrDefault (dtw => dtw.DateTime));
			Assert.AreEqual (null, defaultDateTimeSource.MinValueOrDefault (dtw => dtw.DateTime, new DateTimeComparer ()));
		}
	}
}

