           //
// GeneralTests.cs
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
	class GeneralTests
	{
		[Test]
		public void UsingDeclarationTests()
		{
			var indent = Helper.CreateEngine("using NUnit.Framework;\n$");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("", indent.NextLineIndent);
		}

		[Test]
		public void NestedUsingDeclarationTest()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	namespace Bar {
		using NUnit.Framework;$");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestMixedLineEndingPosition()
		{
			var indent = Helper.CreateEngine("\n\r\n$");
			Assert.AreEqual(3, indent.Offset);
		}
	}
}

