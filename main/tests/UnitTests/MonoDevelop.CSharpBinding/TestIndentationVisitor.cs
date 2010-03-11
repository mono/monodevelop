// 
// TestIndentationVisitor.cs
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
/*
using System;
using NUnit.Framework;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Projects.Gui.Completion;
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
	public class TestIndentationVisitor : UnitTests.TestBase
	{
		[Test()]
		public void TestClassIndentation ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = 
@"			class Test {}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.ClassBraceStyle =  BraceStyle.EndOfLine;
			
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {}", data.Document.Text);
		}
		
		
		[Test()]
		public void TestNamespaceIndentation ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = 
@"			namespace Test {
class FooBar {}
		}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.ClassBraceStyle =  BraceStyle.EndOfLine;
			
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"namespace Test {
class FooBar {}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestMethodIndentation ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = 
@"class Test {
MyType TestMethod () {}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.ClassBraceStyle =  BraceStyle.EndOfLine;
			
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
	MyType TestMethod () {}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestPropertyIndentation ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = 
@"class Test {
				public int Prop { get; set; }
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.ClassBraceStyle =  BraceStyle.EndOfLine;
			
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
	public int Prop { get; set; }
}", data.Document.Text);
		}
		
		[Test()]
		public void TestPropertyIndentationCase2 ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = 
@"class Test {
				public int Prop {
 get;
set;
}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.ClassBraceStyle =  BraceStyle.EndOfLine;
			
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
	public int Prop {
		get;
		set;
	}
}", data.Document.Text);
		}
		
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
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
	Test TestMethod ()
	{
		this.TestMethod ();
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestBlockIndentation ()
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
			
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
	Test TestMethod ()
	{
		{
			{}
		}
	}
}", data.Document.Text);
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
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
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
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
	Test TestMethod ()
	{
		checked {
		}
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
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
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
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
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
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
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
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
	Test TestMethod ()
	{
		fixed (object* obj = &obj)
			;
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestForeachIndentation ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	Test TestMethod ()
	{
foreach (var obj in col) {
}
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
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
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
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
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
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
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
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
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
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
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
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
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
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
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
	Test TestMethod ()
	{
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
			
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
	Test TestMethod ()
	{
		Test a;
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
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
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
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
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
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
	Test TestMethod ()
	{
		do {
		} while (true);
	}
}", data.Document.Text);
		}
		
	}
}*/
