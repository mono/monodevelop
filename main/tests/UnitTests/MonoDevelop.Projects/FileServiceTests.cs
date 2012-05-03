// 
// FileServiceTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Core;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class FileServiceTests : TestBase
	{
		[Test]
		public void TestGetRelativePath ()
		{
			Assert.AreEqual (@"blub", FileService.AbsoluteToRelativePath (@"/a", @"/a/blub"));
		}
		[Test]
		public void TestGetRelativePathCase2 ()
		{
			Assert.AreEqual (@"../a/blub", FileService.AbsoluteToRelativePath (@"/hello/", @"/a/blub"));
		}
		[Test]
		public void TestGetRelativePathCase3 ()
		{
			Assert.AreEqual (@"../a/blub", FileService.AbsoluteToRelativePath (@"/hello", @"/a/blub"));
		}

		[Test]
		public void TestGetRelativePathCase4 ()
		{
			Assert.AreEqual (@".", FileService.AbsoluteToRelativePath (@"/aa/bb/cc", @"/aa/bb/cc"));
		}

		[Test]
		public void TestGetRelativeGoUpCaseAtEnd ()
		{
			Assert.AreEqual (@"..", FileService.AbsoluteToRelativePath (@"/aa/bb/cc", @"/aa/bb"));
		}

		[Test]
		public void TestGetRelativeGoSeveralUpCaseAtEnd ()
		{
			Assert.AreEqual (@"../..", FileService.AbsoluteToRelativePath (@"/aa/bb/cc/dd", @"/aa/bb"));
		}
	}
}

