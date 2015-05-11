//
// RevisionTests.cs
//
// Author:
//       Marius Ungureanu <marius.ungureanu@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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

namespace MonoDevelop.VersionControl.Git.Tests
{
	class MockRevision : Revision
	{
		public MockRevision (string message) : base (null, DateTime.Now, "me", message)
		{
		}

		public override Revision GetPrevious ()
		{
			return this;
		}
	}

	[TestFixture]
	public class RevisionTests
	{
		const string shorterThan80 = @"I'm a string";
		const string longerThan80 = @"Hey, I'm a string that's longer than 80 characters.

Lorem ipsum, something-something test string is long now.";

		[Test]
		public void ShortMessageFormatting ()
		{
			var rev = new MockRevision (shorterThan80);
			Assert.AreEqual (shorterThan80, rev.Message);
			Assert.AreEqual (shorterThan80, rev.ShortMessage);

			rev = new MockRevision (longerThan80);

			Assert.AreEqual (longerThan80, rev.Message);
			Assert.AreEqual (longerThan80.Substring (0, 80), rev.ShortMessage);

			rev.ShortMessage = shorterThan80;
			Assert.AreEqual (longerThan80, rev.Message);
			Assert.AreEqual (shorterThan80, rev.ShortMessage);
		}
	}
}

