// 
// TestStatementIndentation.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.CSharp.Parser;
using MonoDevelop.CSharp.Resolver;
using MonoDevelop.CSharp.Completion;
using Mono.TextEditor;
using MonoDevelop.CSharp.Formatting;

namespace MonoDevelop.CSharpBinding.FormattingTests
{
	[TestFixture()]
	public class TestStatementIndentation : UnitTests.TestBase
	{
		[Test()]
		public void TestInvocationIndentation ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	Test TestMethod ()
	{
this.TestMethod ();
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
	Test TestMethod ()
	{
		this.TestMethod ();
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestIndentBlocks ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	Test TestMethod ()
	{
{
{}
}
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.IndentBlocks = true;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	Test TestMethod ()
	{
		{
			{}
		}
	}
}", data.Document.Text);
			policy.IndentBlocks = false;
			compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			
			Assert.AreEqual (@"class Test
{
	Test TestMethod ()
	{
		{
		{}
		}
	}
}", data.Document.Text);
			policy.IndentBlocks = false;
		}
		
		[Test()]
		public void TestBreakIndentation ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	Test TestMethod ()
	{
                              break;
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
	Test TestMethod ()
	{
		break;
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestCheckedIndentation ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	Test TestMethod ()
	{
checked {
}
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
	Test TestMethod ()
	{
		checked {
		}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestBaseIndentation ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	Test TestMethod ()
	{
                              base.FooBar();
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
	Test TestMethod ()
	{
		base.FooBar();
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestUncheckedIndentation ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	Test TestMethod ()
	{
unchecked {
}
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
	Test TestMethod ()
	{
		unchecked {
		}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestContinueIndentation ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	Test TestMethod ()
	{
continue;
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
	Test TestMethod ()
	{
		continue;
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestEmptyStatementIndentation ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	Test TestMethod ()
	{
;
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
	Test TestMethod ()
	{
		;
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestFixedStatementIndentation ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	Test TestMethod ()
	{
fixed (object* obj = &obj)
;
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
	Test TestMethod ()
	{
		fixed (object* obj = &obj)
			;
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestFixedForcementAdd ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test TestMethod ()
	{
		fixed (object* obj = &obj) {
		}
		fixed (object* obj = &obj) ;
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLine;
			policy.FixedBraceForcement = BraceForcement.AddBraces;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			
			Assert.AreEqual (@"class Test
{
	Test TestMethod ()
	{
		fixed (object* obj = &obj) {
		}
		fixed (object* obj = &obj) {
			;
		}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestForeachIndentation ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test TestMethod ()
	{
foreach (var obj in col) {
}
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLine;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	Test TestMethod ()
	{
		foreach (var obj in col) {
		}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestForIndentation ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	Test TestMethod ()
	{
for (;;) {
}
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
	Test TestMethod ()
	{
		for (;;) {
		}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestGotoIndentation ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	Test TestMethod ()
	{
goto label;
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
	Test TestMethod ()
	{
		goto label;
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestReturnIndentation ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	Test TestMethod ()
	{
return;
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
	Test TestMethod ()
	{
		return;
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestLockIndentation ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	Test TestMethod ()
	{
lock (this) {
}
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
	Test TestMethod ()
	{
		lock (this) {
		}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestThrowIndentation ()
		{
			
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	Test TestMethod ()
	{
throw new NotSupportedException ();
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
	Test TestMethod ()
	{
		throw new NotSupportedException ();
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestUnsafeIndentation ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	Test TestMethod ()
	{
unsafe {
}
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
	Test TestMethod ()
	{
		unsafe {
		}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestUsingIndentation ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	Test TestMethod ()
	{
using (var o = new MyObj()) {
}
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
	Test TestMethod ()
	{
		using (var o = new MyObj()) {
		}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestUsingForcementAdd ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test TestMethod ()
	{
		using (var o = new MyObj()) {
		}
		using (var o = new MyObj()) ;
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLine;
			policy.UsingBraceForcement = BraceForcement.AddBraces;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			
			Assert.AreEqual (@"class Test
{
	Test TestMethod ()
	{
		using (var o = new MyObj()) {
		}
		using (var o = new MyObj()) {
			;
		}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestUsingForcementDoNotChange ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test TestMethod ()
	{
		using (var o = new MyObj()) {
		}
		using (var o = new MyObj()) ;
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLine;
			policy.UsingBraceForcement = BraceForcement.DoNotChange;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			
			Assert.AreEqual (@"class Test
{
	Test TestMethod ()
	{
		using (var o = new MyObj()) {
		}
		using (var o = new MyObj())
			;
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestUsingAlignment ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	Test TestMethod ()
	{
using (var p = new MyObj())
using (var o = new MyObj()) {
}
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.AlignEmbeddedUsingStatements = true;
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
	Test TestMethod ()
	{
		using (var p = new MyObj())
		using (var o = new MyObj()) {
		}
	}
}", data.Document.Text);
			policy.AlignEmbeddedUsingStatements = false;
			compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
	Test TestMethod ()
	{
		using (var p = new MyObj())
			using (var o = new MyObj()) {
			}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestVariableDeclarationIndentation ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	Test TestMethod ()
	{
Test a;
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			Console.WriteLine (data.Document.Text);
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
	Test TestMethod ()
	{
		Test a;
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestConstantVariableDeclarationIndentation ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	Test TestMethod ()
	{
const int a=5;
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			Console.WriteLine (data.Document.Text);
			Assert.AreEqual (@"class Test {
	Test TestMethod ()
	{
		const int a = 5;
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestYieldIndentation ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	Test TestMethod ()
	{
yield return null;
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
	Test TestMethod ()
	{
		yield return null;
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestWhileIndentation ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	Test TestMethod ()
	{
while (true)
;
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
	Test TestMethod ()
	{
		while (true)
			;
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestDoWhileIndentation ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	Test TestMethod ()
	{
do {
} while (true);
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
	Test TestMethod ()
	{
		do {
		} while (true);
	}
}", data.Document.Text);
		}
		
		
		[Test()]
		public void TestForeachBracketPlacement ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test TestMethod ()
	{
		foreach (var obj in col) {}
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLine;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	Test TestMethod ()
	{
		foreach (var obj in col) {
		}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestForeachBracketPlacement2 ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test TestMethod ()
	{
		foreach (var obj in col) {;}
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.NextLineShifted2;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	Test TestMethod ()
	{
		foreach (var obj in col)
			{
				;
			}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestForEachBraceForcementAdd ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test TestMethod ()
	{
		foreach (var obj in col)
		{
		}
		foreach (var obj in col) ;
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.NextLine;
			policy.ForEachBraceForcement = BraceForcement.AddBraces;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			
			Console.WriteLine (data.Document.Text);
			
			Assert.AreEqual (@"class Test
{
	Test TestMethod ()
	{
		foreach (var obj in col)
		{
		}
		foreach (var obj in col)
		{
			;
		}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestForBraceForcementAdd ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test TestMethod ()
	{
		for (;;)
		{
		}
		for (;;) ;
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.NextLine;
			policy.ForBraceForcement = BraceForcement.AddBraces;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			
			Assert.AreEqual (@"class Test
{
	Test TestMethod ()
	{
		for (;;)
		{
		}
		for (;;)
		{
			;
		}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestForEachBraceForcementRemove ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test TestMethod ()
	{
		foreach (var obj in col)
		{
			;
			;
		}
		foreach (var obj in col)
		{
			;
		}
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.NextLine;
			policy.ForEachBraceForcement = BraceForcement.RemoveBraces;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			
			Console.WriteLine (data.Document.Text);
			
			Assert.AreEqual (@"class Test
{
	Test TestMethod ()
	{
		foreach (var obj in col)
		{
			;
			;
		}
		foreach (var obj in col)
			;
	}
}", data.Document.Text);
		}
		
		
		[Test()]
		public void TestIfBracketPlacement ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test TestMethod ()
	{
		if (true) {}
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLine;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	Test TestMethod ()
	{
		if (true) {
		}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestAllowIfBlockInline ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.StatementBraceStyle = BraceStyle.EndOfLine;
			policy.AllowIfBlockInline = true;
			
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test TestMethod ()
	{
		if (true) {}
	}
}";
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	Test TestMethod ()
	{
		if (true) {}
	}
}", data.Document.Text);
			
			
			data.Document.Text = @"class Test
{
	Test TestMethod ()
	{
		if (true) { Foo (); }
	}
}";
			
			compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	Test TestMethod ()
	{
		if (true) { Foo (); }
	}
}", data.Document.Text);
			
			
			data.Document.Text = @"class Test
{
	Test TestMethod ()
	{
		if (true) Foo ();
	}
}";
			
			compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	Test TestMethod ()
	{
		if (true) Foo ();
	}
}", data.Document.Text);
			
			
			data.Document.Text = @"class Test
{
	Test TestMethod ()
	{
		if (true)
			Foo ();
	}
}";
			
			compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	Test TestMethod ()
	{
		if (true)
			Foo ();
	}
}", data.Document.Text);
			
			
		}
		
		[Test()]
		public void TestIfElseBracketPlacement ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test TestMethod ()
	{
		if (true) {} else {}
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLine;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	Test TestMethod ()
	{
		if (true) {
		} else {
		}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestIfForcementRemove ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test TestMethod ()
	{
		if (true)
		{
			;
			;
		}
		if (true)
		{
			;
		}
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.NextLine;
			policy.IfElseBraceForcement = BraceForcement.RemoveBraces;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			
			Console.WriteLine (data.Document.Text);
			
			Assert.AreEqual (@"class Test
{
	Test TestMethod ()
	{
		if (true)
		{
			;
			;
		}
		if (true)
			;
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestIfAlignment ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	Test TestMethod ()
	{
if (a)
if (b) {
}
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.AlignEmbeddedIfStatements = true;
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
	Test TestMethod ()
	{
		if (a)
		if (b) {
		}
	}
}", data.Document.Text);
			policy.AlignEmbeddedIfStatements = false;
			compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
	Test TestMethod ()
	{
		if (a)
			if (b) {
			}
	}
}", data.Document.Text);
		}
		
		
		[Test()]
		public void TestIfForcementAdd ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	void TestMethod ()
	{
		if (true)
			Call ();
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLine;
			policy.IfElseBraceForcement = BraceForcement.AddBraces;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			System.Console.WriteLine (data.Document.Text);
			Assert.AreEqual (@"class Test
{
	void TestMethod ()
	{
		if (true) {
			Call ();
		}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestIfForcementWithComment ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	void TestMethod ()
	{
		if (true) // TestComment
			Call ();
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLine;
			policy.IfElseBraceForcement = BraceForcement.AddBraces;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			System.Console.WriteLine (data.Document.Text);
			Assert.AreEqual (@"class Test
{
	void TestMethod ()
	{
		if (true) {
			// TestComment
			Call ();
		}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestIfElseForcementAdd ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	void TestMethod ()
	{
		if (true)
			Call ();
		else
			Call2 ();
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLine;
			policy.IfElseBraceForcement = BraceForcement.AddBraces;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			System.Console.WriteLine (data.Document.Text);
			Assert.AreEqual (@"class Test
{
	void TestMethod ()
	{
		if (true) {
			Call ();
		} else {
			Call2 ();
		}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestIfElseIFForcementAdd ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	void TestMethod ()
	{
		if (true)
			Call ();
		else if (false)
			Call2 ();
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLine;
			policy.IfElseBraceForcement = BraceForcement.AddBraces;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			System.Console.WriteLine (data.Document.Text);
			Assert.AreEqual (@"class Test
{
	void TestMethod ()
	{
		if (true) {
			Call ();
		} else if (false) {
			Call2 ();
		}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestElseOnNewLine ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test TestMethod ()
	{
		if (true) {
			;
		} else if (false) {
			;
		}
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.PlaceElseOnNewLine = true;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			
			Console.WriteLine (data.Document.Text);
			
			Assert.AreEqual (@"class Test
{
	Test TestMethod ()
	{
		if (true) {
			;
		}
		else if (false) {
			;
		}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestElseIfOnNewLine ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test TestMethod ()
	{
		if (true) {
			;
		} else if (false) {
			;
		}
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.PlaceElseIfOnNewLine = true;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			
			Assert.AreEqual (@"class Test
{
	Test TestMethod ()
	{
		if (true) {
			;
		} else
		if (false) {
			;
		}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestElseOnNewLineOff ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test TestMethod ()
	{
		if (true) {
			;
		}
		else if (false) {
			;
		}
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.PlaceElseOnNewLine = false;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			
			Console.WriteLine (data.Document.Text);
			
			Assert.AreEqual (@"class Test
{
	Test TestMethod ()
	{
		if (true) {
			;
		} else if (false) {
			;
		}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestSimpleIfElseComment ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	void TestMethod ()
	{
		if (true) Call (); else Call ();
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLine;
			policy.PlaceElseIfOnNewLine = false; // for simple statements it must be new line.
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	void TestMethod ()
	{
		if (true)
			Call ();
		else
			Call ();
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestWhileForcementRemove ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test TestMethod ()
	{
		while (true)
		{
			;
			;
		}
		while (true)
		{
			;
		}
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.NextLine;
			policy.WhileBraceForcement = BraceForcement.RemoveBraces;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			
			Console.WriteLine (data.Document.Text);
			
			Assert.AreEqual (@"class Test
{
	Test TestMethod ()
	{
		while (true)
		{
			;
			;
		}
		while (true)
			;
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestFixedBracketPlacement ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test TestMethod ()
	{
		fixed (object* obj = &obj)

;
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.NextLineShifted;
			policy.FixedBraceForcement = BraceForcement.AddBraces;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Console.WriteLine (data.Document.Text);
			Assert.AreEqual (@"class Test
{
	Test TestMethod ()
	{
		fixed (object* obj = &obj)
			{
			;
			}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestForBracketPlacement ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test TestMethod ()
	{
		for (;;) {;}
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLineWithoutSpace;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	Test TestMethod ()
	{
		for (;;){
			;
		}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestCheckedBracketPlacement ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test TestMethod ()
	{
		checked {;}
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLineWithoutSpace;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	Test TestMethod ()
	{
		checked{
			;
		}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestUncheckedBracketPlacement ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test TestMethod ()
	{
		unchecked {;}
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLineWithoutSpace;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	Test TestMethod ()
	{
		unchecked{
			;
		}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestLockBracketPlacement ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test TestMethod ()
	{
		lock (this)
		{
			;
		}
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLine;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	Test TestMethod ()
	{
		lock (this) {
			;
		}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestUnsafeBracketPlacement ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test TestMethod ()
	{
		unsafe
		{
			;
		}
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLine;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	Test TestMethod ()
	{
		unsafe {
			;
		}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestUsingBracketPlacement ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test TestMethod ()
	{
		using (var e = new E())
		{
			;
		}
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLine;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	Test TestMethod ()
	{
		using (var e = new E()) {
			;
		}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestWhileBracketPlacement ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test TestMethod ()
	{
		while (true)
		{
			;
		}
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLine;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	Test TestMethod ()
	{
		while (true) {
			;
		}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestDoWhileBracketPlacement ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test TestMethod ()
	{
		do
		{
			;
		} while (true);
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLine;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	Test TestMethod ()
	{
		do {
			;
		} while (true);
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestSwitchFormatting1 ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test TestMethod ()
	{
		switch (a) { case 1: case 2: DoSomething(); break; default: Foo (); break;}
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.IndentSwitchBody = true;
			policy.IndentCaseBody = true;
			policy.IndentBreakStatements = true;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	Test TestMethod ()
	{
		switch (a) {
			case 1:
			case 2:
				DoSomething();
				break;
			default:
				Foo ();
				break;
		}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestSwitchFormatting2 ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test TestMethod ()
	{
		switch (a) { case 1: case 2: DoSomething(); break; default: Foo (); break;}
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.IndentSwitchBody = false;
			policy.IndentCaseBody = false;
			policy.IndentBreakStatements = false;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	Test TestMethod ()
	{
		switch (a) {
		case 1:
		case 2:
		DoSomething();
		break;
		default:
		Foo ();
		break;
		}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestTryCatchBracketPlacement ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test TestMethod ()
	{
		try { ; } catch (Exception e) { } finally { }
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLine;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			
			Console.WriteLine (data.Document.Text);
			Assert.AreEqual (@"class Test
{
	Test TestMethod ()
	{
		try {
			;
		} catch (Exception e) {
		} finally {
		}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestPlaceCatchOnNewLine ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test TestMethod ()
	{
		try {
			;
		} catch (Exception e) {
		} finally {
		}
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.PlaceCatchOnNewLine = true;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			
			Console.WriteLine (data.Document.Text);
			Assert.AreEqual (@"class Test
{
	Test TestMethod ()
	{
		try {
			;
		}
		catch (Exception e) {
		} finally {
		}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestPlaceFinallyOnNewLine ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test TestMethod ()
	{
		try {
			;
		} catch (Exception e) {
		} finally {
		}
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.PlaceFinallyOnNewLine = true;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			
			Console.WriteLine (data.Document.Text);
			Assert.AreEqual (@"class Test
{
	Test TestMethod ()
	{
		try {
			;
		} catch (Exception e) {
		}
		finally {
		}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestPlaceWhileOnNewLine ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test TestMethod ()
	{
		do {
			;
		} while (true);
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.PlaceWhileOnNewLine = true;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstIndentationVisitor (policy, data), null);
			
			Console.WriteLine (data.Document.Text);
			Assert.AreEqual (@"class Test
{
	Test TestMethod ()
	{
		do {
			;
		}
		while (true);
	}
}", data.Document.Text);
		}
		
	}
}
