// 
// TestFormattingVisitor.cs
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
	public class TestSpacingVisitor : UnitTests.TestBase
	{
		[Test()]
		public void TestFieldSpacesBeforeComma1 ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	int a           ,                   b,          c;
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.ClassBraceStyle =  BraceStyle.EndOfLine;
			policy.BeforeFieldDeclarationComma = false;
			policy.AfterFieldDeclarationComma = false;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
	int a,b,c;
}", data.Document.Text);
		}
		
		[Test()]
		public void TestFieldSpacesBeforeComma2 ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	int a           ,                   b,          c;
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.ClassBraceStyle =  BraceStyle.EndOfLine;
			policy.BeforeFieldDeclarationComma = true;
			policy.AfterFieldDeclarationComma = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
	int a , b , c;
}", data.Document.Text);
		}
		
		[Test()]
		public void TestFixedFieldSpacesBeforeComma ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	fixed int a[10]           ,                   b[10],          c[10];
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.ClassBraceStyle =  BraceStyle.EndOfLine;
			policy.AfterFieldDeclarationComma = true;
			policy.BeforeFieldDeclarationComma = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
	fixed int a[10] , b[10] , c[10];
}", data.Document.Text);
		}

		[Test()]
		public void TestConstFieldSpacesBeforeComma ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	const int a = 1           ,                   b = 2,          c = 3;
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.ClassBraceStyle =  BraceStyle.EndOfLine;
			policy.AfterFieldDeclarationComma = false;
			policy.BeforeFieldDeclarationComma = false;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
	const int a = 1,b = 2,c = 3;
}", data.Document.Text);
		}
		
		[Test()]
		public void TestBeforeMethodDeclarationParentheses ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"public abstract class Test
{
	public abstract Test TestMethod();
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.BeforeMethodDeclarationParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			Console.WriteLine (data.Document.Text);
			Assert.AreEqual (@"public abstract class Test
{
	public abstract Test TestMethod ();
}", data.Document.Text);
		}
		
		
		
		
		[Test()]
		public void TestBeforeConstructorDeclarationParenthesesDestructorCase ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	~Test()
	{
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.BeforeConstructorDeclarationParentheses = true;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	~Test ()
	{
	}
}", data.Document.Text);
		}
		
		
		
		static void TestBinaryOperator (CSharpFormattingPolicy policy, string op)
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = "class Test { void TestMe () { result = left" +op+"right; } }";
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.IndexOf ("left");
			int i2 = data.Document.Text.IndexOf ("right") + "right".Length;
			if (i1 < 0 || i2 < 0)
				Assert.Fail ("text invalid:" + data.Document.Text);
			Assert.AreEqual ("left " + op + " right", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestSpacesAroundMultiplicativeOperator ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.AroundMultiplicativeOperatorParentheses = true;
			
			TestBinaryOperator (policy, "*");
			TestBinaryOperator (policy, "/");
		}
		
		[Test()]
		public void TestSpacesAroundShiftOperator ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.AroundShiftOperatorParentheses = true;
			TestBinaryOperator (policy, "<<");
			TestBinaryOperator (policy, ">>");
		}
		
		[Test()]
		public void TestSpacesAroundAdditiveOperator ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.AroundAdditiveOperatorParentheses = true;
			
			TestBinaryOperator (policy, "+");
			TestBinaryOperator (policy, "-");
		}
		
		[Test()]
		public void TestSpacesAroundBitwiseOperator ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.AroundBitwiseOperatorParentheses = true;
			
			TestBinaryOperator (policy, "&");
			TestBinaryOperator (policy, "|");
			TestBinaryOperator (policy, "^");
		}
		
		[Test()]
		public void TestSpacesAroundRelationalOperator ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.AroundRelationalOperatorParentheses = true;
			
			TestBinaryOperator (policy, "<");
			TestBinaryOperator (policy, "<=");
			TestBinaryOperator (policy, ">");
			TestBinaryOperator (policy, ">=");
		}
		
		[Test()]
		public void TestSpacesAroundEqualityOperator ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.AroundEqualityOperatorParentheses = true;
			
			TestBinaryOperator (policy, "==");
			TestBinaryOperator (policy, "!=");
		}
		
		[Test()]
		public void TestSpacesAroundLogicalOperator ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.AroundLogicalOperatorParentheses = true;
			
			TestBinaryOperator (policy, "&&");
			TestBinaryOperator (policy, "||");
		}
		
		[Test()]
		public void TestConditionalOperator ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		result = condition?trueexpr:falseexpr;
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.ConditionalOperatorAfterConditionSpace = true;
			policy.ConditionalOperatorAfterSeparatorSpace = true;
			policy.ConditionalOperatorBeforeConditionSpace = true;
			policy.ConditionalOperatorBeforeSeparatorSpace = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			
			int i1 = data.Document.Text.IndexOf ("condition");
			int i2 = data.Document.Text.IndexOf ("falseexpr") + "falseexpr".Length;
			Assert.AreEqual (@"condition ? trueexpr : falseexpr", data.Document.GetTextBetween (i1, i2));
			
			
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		result = true ? trueexpr : falseexpr;
	}
}";
			policy.ConditionalOperatorAfterConditionSpace = false;
			policy.ConditionalOperatorAfterSeparatorSpace = false;
			policy.ConditionalOperatorBeforeConditionSpace = false;
			policy.ConditionalOperatorBeforeSeparatorSpace = false;
			
			compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			i1 = data.Document.Text.IndexOf ("true");
			i2 = data.Document.Text.IndexOf ("falseexpr") + "falseexpr".Length;
			Assert.AreEqual (@"true?trueexpr:falseexpr", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestBeforeMethodCallParenthesesSpace ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		MethodCall();
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.BeforeMethodCallParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.IndexOf ("MethodCall");
			int i2 = data.Document.Text.IndexOf (";") + ";".Length;
			Assert.AreEqual (@"MethodCall ();", data.Document.GetTextBetween (i1, i2));
			
			
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		MethodCall                         				();
	}
}";
			policy.BeforeMethodCallParentheses = false;
			
			compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			i1 = data.Document.Text.IndexOf ("MethodCall");
			i2 = data.Document.Text.IndexOf (";") + ";".Length;
			Assert.AreEqual (@"MethodCall();", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestWithinMethodCallParenthesesSpace ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		MethodCall(true);
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.WithinMethodCallParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( true )", data.Document.GetTextBetween (i1, i2));
			
			
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		MethodCall( true );
	}
}";
			policy.WithinMethodCallParentheses = false;
			
			compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			i1 = data.Document.Text.LastIndexOf ("(");
			i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(true)", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestBeforeIfParenthesesSpace ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		if(true);
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.IfParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.IndexOf ("if");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"if (true)", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestWithinIfParenthesesSpace ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		if (true);
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.WithinIfParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( true )", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestBeforeWhileParenthesesSpace ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		while(true);
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.WhileParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.IndexOf ("while");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"while (true)", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestWithinWhileParenthesesSpace ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		while (true);
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.WithinWhileParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( true )", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestBeforeForParenthesesSpace ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		for(;;);
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.ForParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.IndexOf ("for");
			int i2 = data.Document.Text.LastIndexOf ("(") + "(".Length;
			Assert.AreEqual (@"for (", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestWithinForParenthesesSpace ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		for(;;);
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.WithinForParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( ;; )", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestBeforeForeachParenthesesSpace ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		foreach(var o in list);
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.ForeachParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.IndexOf ("foreach");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"foreach (var o in list)", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestWithinForeachParenthesesSpace ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		foreach(var o in list);
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.WithinForEachParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( var o in list )", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestBeforeCatchParenthesesSpace ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
  try {} catch(Exception) {}
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.CatchParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.IndexOf ("catch");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"catch (Exception)", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestWithinCatchParenthesesSpace ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		try {} catch(Exception) {}
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.WithinCatchParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( Exception )", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestBeforeLockParenthesesSpace ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		lock(this) {}
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.LockParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.IndexOf ("lock");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"lock (this)", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestWithinLockParenthesesSpace ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		lock(this) {}
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.WithinLockParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( this )", data.Document.GetTextBetween (i1, i2));
		}
		
		
		[Test()]
		public void TestSpacesAfterForSemicolon ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		for (int i;true;i++) ;
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpacesAfterForSemicolon = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("for");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			
			Assert.AreEqual (@"for (int i; true; i++)", data.Document.GetTextBetween (i1, i2));
		}
		[Test()]
		public void TestSpacesBeforeForSemicolon ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		for (int i;true;i++) ;
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpacesBeforeForSemicolon = true;
			policy.SpacesAfterForSemicolon = false;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("for");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			
			Assert.AreEqual (@"for (int i ;true ;i++)", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestSpacesAfterTypecast ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	Test TestMe ()
	{
return (Test)null;
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpacesAfterTypecast = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("return");
			int i2 = data.Document.Text.LastIndexOf ("null") + "null".Length;
			
			Assert.AreEqual (@"return (Test) null", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestBeforeUsingParenthesesSpace ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		using(a) {}
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.UsingParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.IndexOf ("using");
			int i2 = data.Document.Text.LastIndexOf ("(") + "(".Length;
			Assert.AreEqual (@"using (", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestWithinUsingParenthesesSpace ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		using(a) {}
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.WithinUsingParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( a )", data.Document.GetTextBetween (i1, i2));
		}
		
		static void TestAssignmentOperator (CSharpFormattingPolicy policy, string op)
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = "class Test { void TestMe () { left" +op+"right; } }";
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.IndexOf ("left");
			int i2 = data.Document.Text.IndexOf ("right") + "right".Length;
			if (i1 < 0 || i2 < 0)
				Assert.Fail ("text invalid:" + data.Document.Text);
			Assert.AreEqual ("left " + op + " right", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestAroundAssignmentSpace ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.AroundAssignmentParentheses = true;
			
			TestAssignmentOperator (policy, "=");
			TestAssignmentOperator (policy, "*=");
			TestAssignmentOperator (policy, "/=");
			TestAssignmentOperator (policy, "+=");
			TestAssignmentOperator (policy, "%=");
			TestAssignmentOperator (policy, "-=");
			TestAssignmentOperator (policy, "<<=");
			TestAssignmentOperator (policy, ">>=");
			TestAssignmentOperator (policy, "&=");
			TestAssignmentOperator (policy, "|=");
			TestAssignmentOperator (policy, "^=");
		}
		
		[Test()]
		public void TestAroundAssignmentSpaceInDeclarations ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		int left=right;
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.AroundAssignmentParentheses = true;
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("left");
			int i2 = data.Document.Text.LastIndexOf ("right") + "right".Length;
			Assert.AreEqual (@"left = right", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestBeforeSwitchParenthesesSpace ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		switch (test) { default: break; }
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SwitchParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.IndexOf ("switch");
			int i2 = data.Document.Text.LastIndexOf ("(") + "(".Length;
			Assert.AreEqual (@"switch (", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestWithinSwitchParenthesesSpace ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		switch (test) { default: break; }
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.WithinSwitchParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( test )", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestWithinParenthesesSpace ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		c = (test);
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.WithinParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( test )", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestWithinMethodDeclarationParenthesesSpace ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe (int a)
	{
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.WithinMethodDeclarationParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( int a )", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestWithinCastParenthesesSpace ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		a = (int)b;
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.WithinCastParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( int )", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestWithinSizeOfParenthesesSpace ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		a = sizeof(int);
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.WithinSizeOfParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( int )", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestBeforeSizeOfParentheses ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		a = sizeof(int);
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.BeforeSizeOfParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("sizeof");
			int i2 = data.Document.Text.LastIndexOf ("(") + "(".Length;
			Assert.AreEqual (@"sizeof (", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestWithinTypeOfParenthesesSpace ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		a = typeof(int);
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.WithinTypeOfParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( int )", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestBeforeTypeOfParentheses ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		a = typeof(int);
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.BeforeTypeOfParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("typeof");
			int i2 = data.Document.Text.LastIndexOf ("(") + "(".Length;
			Assert.AreEqual (@"typeof (", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestWithinCheckedExpressionParanthesesSpace ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		a = checked(a + b);
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.WithinCheckedExpressionParantheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( a + b )", data.Document.GetTextBetween (i1, i2));
			
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		a = unchecked(a + b);
	}
}";
			
			compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			i1 = data.Document.Text.LastIndexOf ("(");
			i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( a + b )", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestSpaceBeforeNewParentheses ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		new Test();
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.NewParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("new");
			int i2 = data.Document.Text.LastIndexOf (";") + ";".Length;
			Assert.AreEqual (@"new Test ();", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestWithinNewParentheses ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		new Test (1);
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.WithinNewParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("new");
			int i2 = data.Document.Text.LastIndexOf (";") + ";".Length;
			Assert.AreEqual (@"new Test ( 1 );", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestBetweenEmptyNewParentheses ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		new Test ();
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.BetweenEmptyNewParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("new");
			int i2 = data.Document.Text.LastIndexOf (";") + ";".Length;
			Assert.AreEqual (@"new Test ( );", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestBeforeNewParameterComma ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		new Test (1,2);
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.BeforeNewParameterComma = true;
			policy.AfterNewParameterComma = false;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("new");
			int i2 = data.Document.Text.LastIndexOf (";") + ";".Length;
			Assert.AreEqual (@"new Test (1 ,2);", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestAfterNewParameterComma ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		new Test (1,2);
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.AfterNewParameterComma = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("new");
			int i2 = data.Document.Text.LastIndexOf (";") + ";".Length;
			Assert.AreEqual (@"new Test (1, 2);", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestFieldDeclarationComma ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	int a,b,c;
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.BeforeFieldDeclarationComma = false;
			policy.AfterFieldDeclarationComma = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("int");
			int i2 = data.Document.Text.LastIndexOf (";") + ";".Length;
			Assert.AreEqual (@"int a, b, c;", data.Document.GetTextBetween (i1, i2));
			policy.BeforeFieldDeclarationComma = true;
			
			compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			i1 = data.Document.Text.LastIndexOf ("int");
			i2 = data.Document.Text.LastIndexOf (";") + ";".Length;
			Assert.AreEqual (@"int a , b , c;", data.Document.GetTextBetween (i1, i2));
			
			policy.BeforeFieldDeclarationComma = false;
			policy.AfterFieldDeclarationComma = false;
			compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			i1 = data.Document.Text.LastIndexOf ("int");
			i2 = data.Document.Text.LastIndexOf (";") + ";".Length;
			Assert.AreEqual (@"int a,b,c;", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestBeforeMethodDeclarationParameterComma ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	public void Foo (int a,int b,int c) {}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.BeforeMethodDeclarationParameterComma = true;
			policy.AfterMethodDeclarationParameterComma = false;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a ,int b ,int c)", data.Document.GetTextBetween (i1, i2));
			compilationUnit = new CSharpParser ().Parse (data);
			
			policy.BeforeMethodDeclarationParameterComma = false;
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			i1 = data.Document.Text.LastIndexOf ("(");
			i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a,int b,int c)", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestAfterMethodDeclarationParameterComma ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	public void Foo (int a,int b,int c) {}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.BeforeMethodDeclarationParameterComma = false;
			policy.AfterMethodDeclarationParameterComma = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a, int b, int c)", data.Document.GetTextBetween (i1, i2));
			compilationUnit = new CSharpParser ().Parse (data);
			
			policy.AfterMethodDeclarationParameterComma = false;
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			i1 = data.Document.Text.LastIndexOf ("(");
			i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a,int b,int c)", data.Document.GetTextBetween (i1, i2));
		}
		
		
		[Test()]
		public void TestSpacesInLambdaExpression ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		var v = x=>x!=null;
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.WithinWhileParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.IndexOf ("x");
			int i2 = data.Document.Text.LastIndexOf ("null") + "null".Length;
			Assert.AreEqual (@"x => x != null", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestBeforeLocalVariableDeclarationComma ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		int a,b,c;
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.BeforeLocalVariableDeclarationComma = true;
			policy.AfterLocalVariableDeclarationComma = false;

			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.IndexOf ("int");
			int i2 = data.Document.Text.IndexOf (";") + ";".Length;
			Assert.AreEqual (@"int a ,b ,c;", data.Document.GetTextBetween (i1, i2));

			compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);

			policy.BeforeLocalVariableDeclarationComma = false;

			compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			i1 = data.Document.Text.IndexOf ("int");
			i2 = data.Document.Text.IndexOf (";") + ";".Length;
			Assert.AreEqual (@"int a,b,c;", data.Document.GetTextBetween (i1, i2));
		}

		[Test()]
		public void TestLocalVariableDeclarationComma ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		int a = 5,b = 6,c;
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.BeforeLocalVariableDeclarationComma = true;
			policy.AfterLocalVariableDeclarationComma = true;

			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.IndexOf ("int");
			int i2 = data.Document.Text.IndexOf (";") + ";".Length;
			Assert.AreEqual (@"int a = 5 , b = 6 , c;", data.Document.GetTextBetween (i1, i2));

			compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);

			policy.BeforeLocalVariableDeclarationComma = false;
			policy.AfterLocalVariableDeclarationComma = false;

			compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			i1 = data.Document.Text.IndexOf ("int");
			i2 = data.Document.Text.IndexOf (";") + ";".Length;
			Assert.AreEqual (@"int a = 5,b = 6,c;", data.Document.GetTextBetween (i1, i2));
		}

		#region Constructors
		
		[Test()]
		public void TestBeforeConstructorDeclarationParentheses ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test()
	{
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.BeforeConstructorDeclarationParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	Test ()
	{
	}
}", data.Document.Text);
		}
				
		[Test()]
		public void TestBeforeConstructorDeclarationParameterComma ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	public Test (int a,int b,int c) {}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.BeforeConstructorDeclarationParameterComma = true;
			policy.AfterConstructorDeclarationParameterComma = false;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a ,int b ,int c)", data.Document.GetTextBetween (i1, i2));
			compilationUnit = new CSharpParser ().Parse (data);
			
			policy.BeforeConstructorDeclarationParameterComma = false;
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			i1 = data.Document.Text.LastIndexOf ("(");
			i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a,int b,int c)", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestAfterConstructorDeclarationParameterComma ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	public Test (int a,int b,int c) {}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.BeforeConstructorDeclarationParameterComma = false;
			policy.AfterConstructorDeclarationParameterComma = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a, int b, int c)", data.Document.GetTextBetween (i1, i2));
			compilationUnit = new CSharpParser ().Parse (data);
			
			policy.AfterConstructorDeclarationParameterComma = false;
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			i1 = data.Document.Text.LastIndexOf ("(");
			i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a,int b,int c)", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestWithinConstructorDeclarationParentheses ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	Test (int a)
	{
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.WithinConstructorDeclarationParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( int a )", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestBetweenEmptyConstructorDeclarationParentheses ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	Test ()
	{
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.BetweenEmptyConstructorDeclarationParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( )", data.Document.GetTextBetween (i1, i2));
		}
		
		#endregion
		
		#region Delegates
		[Test()]
		public void TestBeforeDelegateDeclarationParentheses ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"delegate void Test();";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.BeforeDelegateDeclarationParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			Assert.AreEqual (@"delegate void Test ();", data.Document.Text);
		}
		
		[Test()]
		public void TestBeforeDelegateDeclarationParenthesesComplex ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = "delegate void TestDelegate\t\t\t();";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.BeforeDelegateDeclarationParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			Assert.AreEqual (@"delegate void TestDelegate ();", data.Document.Text);
		}
		
		[Test()]
		public void TestBeforeDelegateDeclarationParameterComma ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"delegate void Test (int a,int b,int c);";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.BeforeDelegateDeclarationParameterComma = true;
			policy.AfterDelegateDeclarationParameterComma = false;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a ,int b ,int c)", data.Document.GetTextBetween (i1, i2));
			compilationUnit = new CSharpParser ().Parse (data);
			
			policy.BeforeDelegateDeclarationParameterComma = false;
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			i1 = data.Document.Text.LastIndexOf ("(");
			i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a,int b,int c)", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestAfterDelegateDeclarationParameterComma ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"delegate void Test (int a,int b,int c);";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.BeforeDelegateDeclarationParameterComma = false;
			policy.AfterDelegateDeclarationParameterComma = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a, int b, int c)", data.Document.GetTextBetween (i1, i2));
			compilationUnit = new CSharpParser ().Parse (data);
			
			policy.AfterDelegateDeclarationParameterComma = false;
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			i1 = data.Document.Text.LastIndexOf ("(");
			i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a,int b,int c)", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestWithinDelegateDeclarationParentheses ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"delegate void Test (int a);";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.WithinDelegateDeclarationParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( int a )", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestBetweenEmptyDelegateDeclarationParentheses ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"delegate void Test();";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.BetweenEmptyDelegateDeclarationParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( )", data.Document.GetTextBetween (i1, i2));
		}
		
		#endregion
		
		#region Method invocations
		[Test()]
		public void TestBeforeMethodCallParentheses ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class FooBar
{
	public void Foo ()
	{
		Test();
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.BeforeMethodCallParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			Assert.AreEqual (@"class FooBar
{
	public void Foo ()
	{
		Test ();
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestBeforeMethodCallParameterComma ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class FooBar
{
	public void Foo ()
	{
		Test(a,b,c);
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.BeforeMethodCallParameterComma = true;
			policy.AfterMethodCallParameterComma = false;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(a ,b ,c)", data.Document.GetTextBetween (i1, i2));
			compilationUnit = new CSharpParser ().Parse (data);
			
			policy.BeforeMethodCallParameterComma = false;
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			i1 = data.Document.Text.LastIndexOf ("(");
			i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(a,b,c)", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestAfterMethodCallParameterComma ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class FooBar
{
	public void Foo ()
	{
		Test(a,b,c);
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.BeforeMethodCallParameterComma = false;
			policy.AfterMethodCallParameterComma = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(a, b, c)", data.Document.GetTextBetween (i1, i2));
			compilationUnit = new CSharpParser ().Parse (data);
			
			policy.AfterMethodCallParameterComma = false;
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			i1 = data.Document.Text.LastIndexOf ("(");
			i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(a,b,c)", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestWithinMethodCallParentheses ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class FooBar
{
	public void Foo ()
	{
		Test(a);
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.WithinMethodCallParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( a )", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestBetweenEmptyMethodCallParentheses ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class FooBar
{
	public void Foo ()
	{
		Test();
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.BetweenEmptyMethodCallParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( )", data.Document.GetTextBetween (i1, i2));
		}
		
		#endregion
		
		#region Indexer declarations
		[Test()]
		public void TestBeforeIndexerDeclarationBracket ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class FooBar
{
	public int this[int a, int b] {
		get {
			return a + b;	
		}
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.BeforeIndexerDeclarationBracket = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			Assert.AreEqual (@"class FooBar
{
	public int this [int a, int b] {
		get {
			return a + b;	
		}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestBeforeIndexerDeclarationParameterComma ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class FooBar
{
	public int this[int a,int b] {
		get {
			return a + b;	
		}
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.BeforeIndexerDeclarationParameterComma = true;
			policy.AfterIndexerDeclarationParameterComma = false;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("[");
			int i2 = data.Document.Text.LastIndexOf ("]") + "]".Length;
			Assert.AreEqual (@"[int a ,int b]", data.Document.GetTextBetween (i1, i2));

		}
		
		[Test()]
		public void TestAfterIndexerDeclarationParameterComma ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class FooBar
{
	public int this[int a,int b] {
		get {
			return a + b;	
		}
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.AfterIndexerDeclarationParameterComma = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("[");
			int i2 = data.Document.Text.LastIndexOf ("]") + "]".Length;
			Assert.AreEqual (@"[int a, int b]", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestWithinIndexerDeclarationBracket ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class FooBar
{
	public int this[int a, int b] {
		get {
			return a + b;	
		}
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.WithinIndexerDeclarationBracket = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("[");
			int i2 = data.Document.Text.LastIndexOf ("]") + "]".Length;
			Assert.AreEqual (@"[ int a, int b ]", data.Document.GetTextBetween (i1, i2));
		}
		
		
		#endregion

		#region Brackets
		
		[Test()]
		public void TestSpacesWithinBrackets ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		this[0] = 5;
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpacesWithinBrackets = true;
			policy.SpacesBeforeBrackets = false;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			
			Assert.AreEqual (@"class Test {
	void TestMe ()
	{
		this[ 0 ] = 5;
	}
}", data.Document.Text);
			
			
		}
		[Test()]
		public void TestSpacesBeforeBrackets ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		this[0] = 5;
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpacesBeforeBrackets = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			
			Assert.AreEqual (@"class Test {
	void TestMe ()
	{
		this [0] = 5;
	}
}", data.Document.Text);
			
			
		}
		
		[Test()]
		public void TestBeforeBracketComma ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		this[1,2,3] = 5;
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.BeforeBracketComma = true;
			policy.AfterBracketComma = false;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			
			int i1 = data.Document.Text.LastIndexOf ("[");
			int i2 = data.Document.Text.LastIndexOf ("]") + "]".Length;
			Assert.AreEqual (@"[1 ,2 ,3]", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestAfterBracketComma ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		this[1,2,3] = 5;
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.AfterBracketComma = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			
			int i1 = data.Document.Text.LastIndexOf ("[");
			int i2 = data.Document.Text.LastIndexOf ("]") + "]".Length;
			Assert.AreEqual (@"[1, 2, 3]", data.Document.GetTextBetween (i1, i2));
		}
		
		#endregion
		
		[Test()]
		public void TestSpacesBeforeArrayDeclarationBrackets ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	int[] a;
	int[][] b;
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpacesBeforeArrayDeclarationBrackets = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			
			Assert.AreEqual (@"class Test {
	int [] a;
	int [][] b;
}", data.Document.Text);
			
			
		}
		
		[Test()]
		public void TestRemoveWhitespacesBeforeSemicolon ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe ()
	{
		Foo ()        ;
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.ForParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.IndexOf ("Foo");
			int i2 = data.Document.Text.LastIndexOf (";") + ";".Length;
			Assert.AreEqual (@"Foo ();", data.Document.GetTextBetween (i1, i2));
		}
		
	}
}
