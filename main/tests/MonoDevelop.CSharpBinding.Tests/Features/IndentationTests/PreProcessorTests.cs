//
// PreProcessorTests.cs
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

using ICSharpCode.NRefactory6.CSharp;
using NUnit.Framework;

namespace ICSharpCode.NRefactory6.IndentationTests
{
	[TestFixture]
	public class PreProcessorTests
	{
		[Test]
		public void TestPreProcessor_Simple()
		{
			var indent = Helper.CreateEngine("#if MONO");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("", indent.NextLineIndent);
		}

		[Test]
		public void TestPreProcessor_If()
		{
			var indent = Helper.CreateEngine(@"
#if false
{ $");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("", indent.NextLineIndent);
		}

		[Test]
		public void TestPreProcessor_If2()
		{
			var indent = Helper.CreateEngine(@"
#if true
{ $");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("\t", indent.NextLineIndent);
		}

		[Test]
		public void TestPreProcessorComment_NestedBlocks()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo {
#if false
		{ $");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestPreProcessorStatement_NestedBlocks()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo {
#if true
		{ $");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestPreProcessor_Elif()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo {
#if true
		{
#elif false
		} 
#endif
			$");
			Assert.AreEqual("\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestPreProcessor_Elif2()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo {
#if false
		{
#elif true
	}
#endif
	$");
			Assert.AreEqual("\t", indent.ThisLineIndent);
			Assert.AreEqual("\t", indent.NextLineIndent);
		}

		[Test]
		public void TestPreProcessor_Else()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo {
#if false
		{
#else
	}
#endif
	$");
			Assert.AreEqual("\t", indent.ThisLineIndent);
			Assert.AreEqual("\t", indent.NextLineIndent);
		}

		[Test]
		public void TestPreProcessor_Else2()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo {
#if true
		{
#else
		} 
#endif
			$");
			Assert.AreEqual("\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		#region Single-line directives

		[Test]
		public void TestPreProcessor_Region()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo {
		#region Foo $");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestPreProcessor_Endegion()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo {
		#region
		void Test() { }
		#endregion $");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestPreProcessor_Pragma()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo {
#pragma Foo 42 $");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestPreProcessor_Warning()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo {
#warning Foo $");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestPreProcessor_Error()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo {
#error Foo $");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestPreProcessor_Line()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo {
#line 42 $");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestPreProcessor_Define()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo {
#define Foo 42 $");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestPreProcessor_Undef()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo {
#undef Foo $");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		#endregion

		[Test]
		public void TestBrackets_PreProcessor_If_DefineDirective()
		{
			var indent = Helper.CreateEngine(@"
#define NOTTHERE
namespace Foo {
	class Foo {
#if NOTTHERE
		{
#endif
		$");
			Assert.AreEqual("\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_PreProcessor_If_UndefDirective()
		{
			var indent = Helper.CreateEngine(@"
#define NOTTHERE
namespace Foo {
	class Foo {
#undef NOTTHERE
#if NOTTHERE
		{
#endif
		$");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestPreProcessor_IndentPreprocessor()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			//policy.IndentPreprocessorDirectives = true;

			var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo {
		#if true $ ", policy);

			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestStateAfterDoublePreprocessorIf()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			//policy.AlignToFirstMethodCallArgument = policy.AlignToFirstIndexerArgument = false;

			var indent = Helper.CreateEngine(@"
class Foo 
{
	void Test ()
	{ 
		#if true
		#if true
		if (true)
			return;$
", policy);
			Assert.AreEqual("\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}


		[Test]
		public void TestComplexIfElse()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			//policy.AlignToFirstMethodCallArgument = policy.AlignToFirstIndexerArgument = false;

			var indent = Helper.CreateEngine(@"
class Foo 
{
	void Test ()
	{ 
		#if false
		#elif true
		#if false
		#endif
		if (true)
			return;$
		#endif
", policy);
			Assert.AreEqual("\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}


		[Test]
		public void TestDefinedSymbol()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			//policy.AlignToFirstMethodCallArgument = policy.AlignToFirstIndexerArgument = false;

			var indent = Helper.CreateEngine(@"
class Foo 
{
	void Test ()
	{ 
		#if DEBUG
		if (true)
			return;$
", policy, new [] { "DEBUG"});
			Assert.AreEqual("\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}


		[Test]
		public void TestNestedPreprocessorCase()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	#if false
	class Foo
	{
	#if true
	}
	#endif
	#endif
	$
}");
			Assert.AreEqual("\t", indent.ThisLineIndent);
			Assert.AreEqual("\t", indent.NextLineIndent);
		}
	}
}
