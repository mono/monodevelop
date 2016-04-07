//
// CommentTests.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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

using NUnit.Framework;

namespace ICSharpCode.NRefactory6.IndentationTests
{
	[TestFixture]
	class CommentTests
	{
		[Test]
		public void TestLineComment_Simple()
		{
			var indent = Helper.CreateEngine("// comment $");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("", indent.NextLineIndent);
		}

		[Test]
		public void TestLineComment_PreProcessor()
		{
			var indent = Helper.CreateEngine(@"
#if NOTTHERE
	// comment $");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("", indent.NextLineIndent);
		}

		[Test]
		public void TestLineComment_Class()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	// comment $");
			Assert.AreEqual("\t", indent.ThisLineIndent);
			Assert.AreEqual("\t", indent.NextLineIndent);
		}

		[Test]
		public void TestLineComment_For()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	for (;;)
		// comment 
		$");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestLineComment_For2()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	for (;;)
		// comment 
		Test();
	$");
			Assert.AreEqual("\t", indent.ThisLineIndent);
			Assert.AreEqual("\t", indent.NextLineIndent);
		}

		[Test]
		public void TestLineComment_For3()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	for (;;) ;
	// comment $");
			Assert.AreEqual("\t", indent.ThisLineIndent);
			Assert.AreEqual("\t", indent.NextLineIndent);
		}

		[Test]
		public void TestMultiLineComment_Simple()
		{
			var indent = Helper.CreateEngine(@"/* comment */$");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("", indent.NextLineIndent);
		}

		[Test]
		public void TestMultiLineComment_ExtraSpaces()
		{
			var indent = Helper.CreateEngine(@"/* comment $");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("", indent.NextLineIndent);
		}

		[Test]
		public void TestMultiLineComment_MultiLines()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	/* line 1 
	line 2
	*/$");
			Assert.AreEqual("\t", indent.ThisLineIndent);
			Assert.AreEqual("\t", indent.NextLineIndent);
		}

		[Ignore ("Should not be respected")]
		[Test]
		public void TestCommentBug()
		{
			var indent = Helper.CreateEngine(@"
namespace FooBar
{
//
// $");
			Assert.AreEqual("", indent.ThisLineIndent);
		}
	}
}
