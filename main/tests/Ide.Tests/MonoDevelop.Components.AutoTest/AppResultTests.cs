//
// AppResultTests.cs
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
using NUnit.Framework;

namespace MonoDevelop.Components.AutoTest
{
	[TestFixture]
	public class AppResultTests
	{
		[Test]
		public void TestDisposeCount()
		{
			var a = new MockResult ("a");
			var b = (MockResult)(a.FirstChild = new MockResult("b"));
			var c = (MockResult)(a.FirstChild.FirstChild = new MockResult ("b"));
			var d = (MockResult)(a.FirstChild.NextSibling = new MockResult ("c"));

			var aProperties = a.Properties ();
			var dProperties = d.Properties ();

			var prop = aProperties ["ToString"];
			Assert.AreEqual ("a", prop.ToString ());

			a.Dispose ();

			Assert.AreEqual (1, a.DisposeCount);
			Assert.AreEqual (1, b.DisposeCount);
			Assert.AreEqual (1, c.DisposeCount);
			Assert.AreEqual (1, d.DisposeCount);
			Assert.AreEqual ("null", prop.ToString ());

			Assert.Throws<NullReferenceException> (() => _ = aProperties ["ToString"]);
			Assert.Throws<NullReferenceException> (() => _ = dProperties ["ToString"]);
		}

		class MockResult : Results.ObjectResult
		{
			public int DisposeCount;

			public MockResult (object value) : base (value)
			{
			}

			protected override void Dispose (bool disposing)
			{
				DisposeCount++;
				base.Dispose (disposing);
			}
		}
	}
}
