//
// StringTests.cs
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

using MonoDevelop.CSharp.Formatting;
using NUnit.Framework;

namespace ICSharpCode.NRefactory6.IndentationTests
{
	[TestFixture]
	class StringTests
	{
		[Test]
		public void TestString_Simple()
		{
			var indent = Helper.CreateEngine(@"""some string""$");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("", indent.NextLineIndent);
		}

		[Test]
		public void TestString_Escaped()
		{
			var indent = Helper.CreateEngine(@"""some escaped \"" string "" { $");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("\t", indent.NextLineIndent);
		}

		[Test]
		public void TestString_NotEscaped()
		{
			var indent = Helper.CreateEngine(@"""some not escaped "" string "" { $");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("", indent.NextLineIndent);
		}

		[Test]
		public void TestString_NotEnded()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	""some string {
#if true $");
			Assert.AreEqual("\t", indent.ThisLineIndent);
			Assert.AreEqual("\t", indent.NextLineIndent);
		}

		[Test]
		public void TestChar_Simple()
		{
			var indent = Helper.CreateEngine(@"'X'$");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("", indent.NextLineIndent);
		}

		[Test]
		public void TestChar_Escaped()
		{
			var indent = Helper.CreateEngine(@"'\'' { $");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("\t", indent.NextLineIndent);
		}

		[Test]
		public void TestChar_NotEscaped()
		{
			var indent = Helper.CreateEngine(@"''' { $");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("", indent.NextLineIndent);
		}

		[Test]
		public void TestChar_NotEnded()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	' { 
#if true $");
			Assert.AreEqual("\t", indent.ThisLineIndent);
			Assert.AreEqual("\t", indent.NextLineIndent);
		}

		[Test]
		public void TestVerbatim_Simple()
		{
			var indent = Helper.CreateEngine(@"@"" verbatim string "" $");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("", indent.NextLineIndent);
		}

		[Test]
		public void TestVerbatim_MultiLine()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo 
	{
		@"" verbatim string $");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("", indent.NextLineIndent);
		}

		[Test]
		public void TestVerbatim_MultiLine2()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo 
	{
		@"" verbatim string 
{ $");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("", indent.NextLineIndent);
		}

		[Test]
		public void TestVerbatim_Escaped()
		{
			var indent = Helper.CreateEngine(@"@"" some """"string { """" in a verbatim string "" $");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("", indent.NextLineIndent);
		}

		[Test]
		public void TestVerbatim_NotEscaped()
		{
			var indent = Helper.CreateEngine(@"@"" some ""string { "" in a verbatim string "" $");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("\t", indent.NextLineIndent);
		}

		[Test]
		public void TestVerbatim_EscapedMultiLine()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	@"" some verbatim string """" { $");
			Assert.AreEqual("\t", indent.ThisLineIndent);
			Assert.AreEqual("", indent.NextLineIndent);
		}

		[Test]
		public void TestVerbatim_EscapedMultiLine2()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	@"" some verbatim string """""" { $");
			Assert.AreEqual("\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestStringLiteralPasteStrategyUnicodeDecode()
		{
			var s = CSharpTextPasteHandler.TextPasteUtils.StringLiteralPasteStrategy.Instance.Decode(@"\u0066");
			Assert.AreEqual("\u0066", s);

			s = CSharpTextPasteHandler.TextPasteUtils.StringLiteralPasteStrategy.Instance.Decode(@"\U00000066");
			Assert.AreEqual("\U00000066", s);

			s = CSharpTextPasteHandler.TextPasteUtils.StringLiteralPasteStrategy.Instance.Decode(@"\xAFFE");
			Assert.AreEqual("\xAFFE", s);

		}
	}
}
