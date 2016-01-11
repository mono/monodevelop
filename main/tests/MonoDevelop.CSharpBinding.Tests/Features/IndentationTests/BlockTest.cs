//
// BlockTest.cs
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
using ICSharpCode.NRefactory6.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;

namespace ICSharpCode.NRefactory6.IndentationTests
{
	[TestFixture]
	public class BracketsTest
	{
		[Test]
		public void TestBrackets_Simple()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo {
		$");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_PreProcessor_If()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo {
#if NOTTHERE
	{
#endif
		$");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_PreProcessor_If2()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo {
#if NOTTHERE || true
		{
#endif
		$");
			Assert.AreEqual("\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_If_AllmanOpenBrace()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		if (true)
		{$", FormattingOptionsFactory.CreateAllman());
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_If_MonoOpenBrace()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		if (true) {$", FormattingOptionsFactory.CreateMono());
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}



		[Test]
		public void TestBrackets_If()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		if (true)$");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_While()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		while (true)$");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_For()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		for (;;)$");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_Foreach()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		foreach (var v in V)$");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_Do()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		do
			$");
			Assert.AreEqual("\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_Do2()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		do
			;
$");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_NestedDo()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		do do
			$");
			Assert.AreEqual("\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_NestedDo2()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		do do
				;
$");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_NestedDoContinuationSetBack()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		do do do
			foo();
$");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_NestedDoContinuationSetBack2()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		do 
			do
				do
					foo();
$");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_NestedDoContinuation_ExpressionEnded()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		do do do foo(); $");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_NestedDoContinuation_ExpressionNotEnded()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		do do do foo() $");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_NestedDoContinuation_ExpressionNotEnded2()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		do do do 
			foo() $");
			Assert.AreEqual("\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_ThisLineIndentAfterCurlyBrace()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
	}$");
			Assert.AreEqual("\t", indent.ThisLineIndent);
			Assert.AreEqual("\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_ThisLineIndentAfterCurlyBrace2()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{ }$");
			Assert.AreEqual("\t", indent.ThisLineIndent);
			Assert.AreEqual("\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_Parameters()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		Foo(true,$");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t    ", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_Parameters2()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		Foo($");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_Parenthesis()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		Foooo(a, b, c, // ) 
				$");
			Assert.AreEqual("\t\t      ", indent.ThisLineIndent);
			Assert.AreEqual("\t\t      ", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_Parenthesis2()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		Foooo(a, b, c, // ) 
				d) $");
			Assert.AreEqual("\t\t      ", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_SquareBrackets()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		var v = [a, b, c, // ] 
					$");
			Assert.AreEqual("\t\t         ", indent.ThisLineIndent);
			Assert.AreEqual("\t\t         ", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_SquareBrackets2()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		var v = [a, b, c, // ]
					d]; $");
			Assert.AreEqual("\t\t         ", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_AngleBrackets()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		Func<a, b, c, // > 
				$");
			Assert.Inconclusive("Not implemented.");
			Assert.AreEqual("\t\t     ", indent.ThisLineIndent);
			Assert.AreEqual("\t\t     ", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_AngleBrackets2()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		Func<a, b, c, // >
				d> $");
			Assert.Inconclusive("Not implemented.");
			Assert.AreEqual("\t\t     ", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_Nested()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		Foo(a, b, bar(c, d[T,  // T
		                   G], // G
		              e), $    // e
			f);");
			Assert.AreEqual("\t\t              ", indent.ThisLineIndent);
			Assert.AreEqual("\t\t    ", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_NotLineStart()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo {
		void Test(int i,
		          double d) { $");
			Assert.AreEqual("\t\t          ", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_RightHandExpression()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		var v = from i in I
				where i == ';'
				select i; $");
			Assert.AreEqual("\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Ignore("Not supported anymore")]
		[Test]
		public void TestBrackets_DotExpression()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		var v = I.Where(i => i == ';')
			.Select(i => i); $");
			Assert.AreEqual("\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Ignore("Not supported anymore")]
		[Test]
		public void TestBrackets_LambdaExpression()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		var v = () => { $
		};");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Ignore("Not supported anymore")]
		[Test]
		public void TestBrackets_LambdaExpression2()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		var v = () => {
		}; $");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_EqualContinuation()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		var v = 
			0; $");
			Assert.AreEqual("\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_EqualExtraSpaces()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		var v = 1 + $");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Ignore("Option no longer supported")]
		[Test]
		public void TestBrackets_NamespaceIndentingOff()
		{
			var fmt = FormattingOptionsFactory.CreateMono();
//			fmt.IndentNamespaceBody = false;
			var indent = Helper.CreateEngine(@"
namespace Bar {
class Foo {
void Test ()
{
	$", fmt);
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
		}

		[Ignore("Not supported anymore")]
		[Test]
		public void ThisLineIndentInCollectionInitializer()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{ 
		public static CSharpFormattingOptions CreateMono()
		{
			return new CSharpFormattingOptions {
				IndentNamespaceBody = true,
				IndentClassBody = true,
				$
");
			Assert.AreEqual("\t\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_AnonymousMethodAsParameter()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{ 
		Foo (
			a,
			delegate {
				evlel();
				$
");
			Assert.AreEqual("\t\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t\t", indent.NextLineIndent);
		}

		[Ignore("Not supported anymore")]
		[Test]
		public void TestBrackets_AnonymousMethodAsParameterCase2()
		{
			var opt = FormattingOptionsFactory.CreateMono();
			//opt.AlignToFirstMethodCallArgument = false;
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{ 
		Foo (a,
			b,
			delegate {
				evlel();
				$
", opt);
			Assert.AreEqual("\t\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t\t", indent.NextLineIndent);
		}

		[Ignore("Not supported anymore")]
		[Test]
		public void TestBrackets_StackedIfElse_AlignElseToCorrectIf()
		{
			var fmt = FormattingOptionsFactory.CreateMono();
			//fmt.AlignEmbeddedStatements = false;
			//fmt.AlignElseInIfStatements = false;
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{ 
		if (true)
			if (true)
			{ }
			else $ ", fmt);
			Assert.AreEqual("\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_StackedIfElse_AlignElseToCorrectIf2()
		{
			var fmt = FormattingOptionsFactory.CreateMono();
			//fmt.AlignEmbeddedStatements = false;
			//fmt.AlignElseInIfStatements = false;
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{ 
		if (true)
			lock (this)
				if (true)
					if (false)
					{ }
					else
						;
				else $ ", fmt);
			Assert.AreEqual("\t\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_StackedIfElse_BreakNestedStatementsOnSemicolon()
		{
			var fmt = FormattingOptionsFactory.CreateMono();
			// fmt.AlignEmbeddedStatements = false;
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{ 
		if (true)
			lock (this)
				if (true)
					if (false)
						;
		; // this should break the nested statements
		else $ ", fmt);
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_StackedIfElse_ElseIf()
		{
			var fmt = FormattingOptionsFactory.CreateMono();
			// fmt.AlignEmbeddedStatements = false;
			// fmt.AlignElseInIfStatements = false;
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{ 
		if (true)
			;
		else if (false)
			lock (this)
				if (true)
					;
				else if (false)
				{ }
				else $ ", fmt);
			Assert.AreEqual("\t\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_StackedIfElse_BreakNestedStatementsOnIf()
		{
			var fmt = FormattingOptionsFactory.CreateMono();
			// fmt.AlignEmbeddedStatements = true;
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{ 
		if (true)
		if (true)
			lock (this)
				if (true)
					lock(this)
						if (true)
							;
						else if (false)
						{ }
						else 
							;
		if (true) // this if should break the nested statements
			;
		else 
			;
		else $ ", fmt);
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_StackedIfElse_BreakNestedStatementsOnAnyStatement()
		{
			var fmt = FormattingOptionsFactory.CreateMono();
			// fmt.AlignEmbeddedStatements = true;
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{ 
		if (true)
		if (true)
			lock (this)
				if (true)
					lock(this)
						if (true)
							;
						else if (false)
						{ }
						else 
							;
		lock (this) // any statement should break the nested statements
			;
		else $ ", fmt);
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_StackedIfElse_BreakNestedStatementsOnAnonymousBlock()
		{
			var fmt = FormattingOptionsFactory.CreateMono();
			// fmt.AlignEmbeddedStatements = false;
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{ 
		if (true)
			lock (this)
				if (true)
					if (false)
						;
		{ } // this should break the nested statements
		else $ ", fmt);
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Ignore("Not supported anymore")]
		[Test]
		public void TestBrackets_StackedIfElseIf_IfInNewLine()
		{
			var fmt = FormattingOptionsFactory.CreateMono();
			// fmt.AlignEmbeddedStatements = false;
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{ 
		if (true)
			FooBar ();
		else
			if (true) {
				$
", fmt);
			Assert.AreEqual("\t\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t\t", indent.NextLineIndent);
		}

		[Ignore("Not supported anymore")]
		[Test]
		public void TestBrackets_RemoveStatementContinuationWhenNoSemicolon()
		{
			var fmt = FormattingOptionsFactory.CreateMono();
			// fmt.AlignEmbeddedStatements = false;
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{ 
		if (true)
			using (this)
				if (true)
				{
					// ...
				} $ ", fmt);
			Assert.AreEqual("\t\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Ignore]
		[Test]
		public void TestBrackets_CustomIndent()
		{
			var fmt = FormattingOptionsFactory.CreateMono();
			// fmt.AlignEmbeddedStatements = false;
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		// ...
			if (true)
			{
				$ 
			}", fmt);
			Assert.AreEqual("\t\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t\t", indent.NextLineIndent);
		}

		[Ignore]
		[Test]
		public void TestBrackets_CustomIndent2()
		{
			var fmt = FormattingOptionsFactory.CreateMono();
			// fmt.AlignEmbeddedStatements = false;
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
				if (true)
				{
					// ...
				} $ ", fmt);
			Assert.AreEqual("\t\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Ignore]
		[Test]
		public void TestBrackets_CustomIndent3()
		{
			var fmt = FormattingOptionsFactory.CreateMono();
			// fmt.AlignEmbeddedStatements = false;
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		using (this)
					if (true)
					{ } $ ", fmt);
			Assert.AreEqual("\t\t\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Ignore]
		[Test]
		public void TestBrackets_CustomIndent4()
		{
			var fmt = FormattingOptionsFactory.CreateMono();
			// fmt.AlignEmbeddedStatements = false;
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
			using (this)
			{
					if (true)
					{ } $ 
			}", fmt);
			Assert.AreEqual("\t\t\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_CustomIndent5()
		{
			var fmt = FormattingOptionsFactory.CreateMono();
			// fmt.AlignEmbeddedStatements = false;
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		using (this)
if (true)
{ } $ ", fmt);
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Ignore("Not supported anymore")]
		[Test]
		public void TestBrackets_CustomIndent6()
		{
			var fmt = FormattingOptionsFactory.CreateMono();
			// fmt.AlignEmbeddedStatements = false;
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		using (this)
			if (true)
	{
				$	
	}", fmt);
			Assert.AreEqual("\t\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t\t", indent.NextLineIndent);
		}

		[Ignore("Not supported anymore")]
		[Test]
		public void TestBrackets_AnonymousMethodAsFirstParameterWithoutAlignment()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			// policy.AlignToFirstMethodCallArgument = policy.AlignToFirstIndexerArgument = false;

			var indent = Helper.CreateEngine(@"
class Foo 
{
	void Test ()
	{ 
		Foo (delegate {
			$
", policy);
			Assert.AreEqual("\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Ignore("Not supported anymore")]
		[Test]
		public void TestBrackets_AnonymousMethodOpenBracketAlignment()
		{
			var policy = FormattingOptionsFactory.CreateAllman();
			// policy.IndentBlocksInsideExpressions = false;
			var indent = Helper.CreateEngine(@"
class Foo 
{
	void Test ()
	{ 
		Foo (delegate
		{$
", policy);
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_AnonymousMethodCloseingBracketAlignment()
		{
			var policy = FormattingOptionsFactory.CreateAllman();
			// policy.IndentBlocksInsideExpressions = false;
			var indent = Helper.CreateEngine(@"
class Foo 
{
	void Test ()
	{ 
		Foo (delegate
		{
		}$
", policy);
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
		}

		[Ignore("Not supported anymore")]
		[Test]
		public void TestBrackets_ArrayCreationAsFirstParameterWithoutAlignment()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			// policy.AlignToFirstMethodCallArgument = policy.AlignToFirstIndexerArgument = false;

			var indent = Helper.CreateEngine(@"
class Foo 
{
	void Test ()
	{ 
		Foo (new int[] {
			$
", policy);
			Assert.AreEqual("\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Ignore("Not supported anymore")]
		[Test]
		public void TestBrackets_ObjectCreationAsFirstParameterWithoutAlignment()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			// policy.AlignToFirstMethodCallArgument = policy.AlignToFirstIndexerArgument = false;

			var indent = Helper.CreateEngine(@"
class Foo 
{
	void Test ()
	{ 
		Foo (new MyOBject {
			$
", policy);
			Assert.AreEqual("\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Ignore("Not supported anymore")]
		[Test]
		public void TestBrackets_ArrayCreationAsFirstIndexerParameterWithoutAlignment()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			// policy.AlignToFirstMethodCallArgument = policy.AlignToFirstIndexerArgument = false;

			var indent = Helper.CreateEngine(@"
class Foo 
{
	void Test ()
	{ 
		Foo [new int[] {
			$
", policy);
			Assert.AreEqual("\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}


		/// <summary>
		/// Bug 16231 - smart indent broken in 4.2.0
		/// </summary>
		[Ignore("Not supported anymore")]
			[Test]
		public void TestBug16231()
		{
			var policy = FormattingOptionsFactory.CreateMono();

			var indent = Helper.CreateEngine(@"
class Foo 
{
	void Test ()
	{ 
		switch (foo) {
		}
		if (true) {
			$
", policy);
			Assert.AreEqual("\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestComplexIfElseElsePlacement_AlignmentOff()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			// policy.AlignElseInIfStatements = false;
			var indent = Helper.CreateEngine(@"
class Foo 
{
	void Test ()
	{ 
		if (1 > 0)
			a = 1;
		else
			if (2 < 10)
				a = 2;
			else$
", policy);
			Assert.AreEqual("\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t\t", indent.NextLineIndent);
		}

		[Ignore("Not supported anymore")]
		[Test]
		public void TestComplexIfElseElsePlacement_AlignmentOn()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			// policy.AlignElseInIfStatements = true;
			var indent = Helper.CreateEngine(@"
class Foo 
{
	void Test ()
	{ 
		if (1 > 0)
			a = 1;
		else
			if (2 < 10)
				a = 2;
		else$
", policy);
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}


		[Ignore("Not supported anymore")]
		[Test]
		public void TestNextLineShifted_OpeningBrace()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			// policy.ClassBraceStyle = BraceStyle.NextLineShifted;
			var indent = Helper.CreateEngine(@"
class Foo 
{$
", policy);
			Assert.AreEqual("\t", indent.ThisLineIndent);
			Assert.AreEqual("\t", indent.NextLineIndent);
		}

		[Ignore("Not supported anymore")]
		[Test]
		public void TestNextLineShifted_ClosingBrace()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			// policy.ClassBraceStyle = BraceStyle.NextLineShifted;
			var indent = Helper.CreateEngine(@"
class Foo 
	{
	}$
", policy);
			Assert.AreEqual("\t", indent.ThisLineIndent);
			Assert.AreEqual("", indent.NextLineIndent);
		}

		[Ignore("Not supported anymore")]
		[Test]
		public void TestNextLineShifted2()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			// policy.ClassBraceStyle = BraceStyle.NextLineShifted2;
			var indent = Helper.CreateEngine(@"
class Foo 
{$
", policy);
			Assert.AreEqual("\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Ignore("Not supported anymore")]
		[Test]
		public void TestBanner_ClosingBrace()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			// policy.ClassBraceStyle = BraceStyle.BannerStyle;
			var indent = Helper.CreateEngine(@"
class Foo {
	}$
", policy);
			Assert.AreEqual("\t", indent.ThisLineIndent);
			Assert.AreEqual("", indent.NextLineIndent);
		}

		[Ignore("Not supported anymore")]
		[Test]
		public void TestNextLineShifted_IfStatement()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			// policy.StatementBraceStyle = BraceStyle.NextLineShifted;
			var indent = Helper.CreateEngine(@"
class Foo 
{
	public static void Main (string[] args)
	{
		if (true)
		{$
	}
", policy);
			Assert.AreEqual("\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestPreprocessorIndenting_Case1()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			// policy.StatementBraceStyle = BraceStyle.NextLineShifted;
			var indent = Helper.CreateEngine(@"
using System;

class X
{
	static void Foo (int arg)
	{
		#if !DEBUG
		if (arg > 0) {
		#else$
		if (arg < 0) {
		#endif
			
		}
	}

	public static void Main ()
	{
	}
}", policy);
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
		}

		[Test]
		public void TestPreprocessorIndenting_Case2()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			// policy.StatementBraceStyle = BraceStyle.NextLineShifted;
			var indent = Helper.CreateEngine(@"
using System;

class X
{
	static void Foo (int arg)
	{
		#if !DEBUG
		if (arg > 0) {
		#else
		if (arg < 0) {$
		#endif
			
		}
	}

	public static void Main ()
	{
	}
}", policy);
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Ignore("Not supported anymore")]
		[Test]
		public void TestPreprocessorIndenting_Case3()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			// policy.StatementBraceStyle = BraceStyle.NextLineShifted;
			var indent = Helper.CreateEngine(@"
using System;

class X
{
	static void Foo (int arg)
	{
		#if !DEBUG
		if (arg > 0) {
		#else
		if (arg < 0) {
		#endif
			$
		}
	}

	public static void Main ()
	{
	}
}", policy);
			Assert.AreEqual("\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Ignore("Not supported anymore")]
		[Test]
		public void TestBrackets_IndentBlocksInsideExpressionsOpenBrace()
		{
			var policy = FormattingOptionsFactory.CreateAllman();

			var indent = Helper.CreateEngine(@"
class Foo 
{
	void Test()
	{ 
		Foo (new MyOBject
			{$
", policy);
			Assert.AreEqual("\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t\t", indent.NextLineIndent);
		}

		[Ignore("Not supported anymore")]
		[Test]
		public void TestBrackets_IndentBlocksInsideExpressions()
		{
			var policy = FormattingOptionsFactory.CreateAllman();

			var indent = Helper.CreateEngine(@"
class Foo 
{
	void Test()
	{ 
		Foo (new MyOBject
			{
				$
", policy);
			Assert.AreEqual("\t\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t\t", indent.NextLineIndent);
		}

		/// <summary>
		/// Bug 18463 - Indentation does not work when typed statement does not require semicolon
		/// </summary>
		[Ignore("Not supported anymore")]
		[Test]
		public void TestBug18463()
		{
			var policy = FormattingOptionsFactory.CreateMono();

			var indent = Helper.CreateEngine(@"
namespace FooBar
{
	public class TestProject
	{
		public static int Main ()
		{
			switch (current_token) {
				case Token.CLOSE_PARENS:
				case Token.TRUE:
				case Token.FALSE:
				case Token.NULL:
				case Token.LITERAL:
					return Token.INTERR;
			}
			if (true) {
				$
", policy);
			Assert.AreEqual("\t\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t\t", indent.NextLineIndent);
		}

	}
}
