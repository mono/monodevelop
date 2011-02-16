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
			policy.SpaceBeforeFieldDeclarationComma = false;
			policy.SpaceAfterFieldDeclarationComma = false;
			
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
			policy.SpaceBeforeFieldDeclarationComma = true;
			policy.SpaceAfterFieldDeclarationComma = true;
			
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
			policy.SpaceAfterFieldDeclarationComma = true;
			policy.SpaceBeforeFieldDeclarationComma = true;
			
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
			policy.SpaceAfterFieldDeclarationComma = false;
			policy.SpaceBeforeFieldDeclarationComma = false;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
	const int a = 1,b = 2,c = 3;
}", data.Document.Text);
		}
		
		[Test()]
		public void TestSpaceBeforeMethodDeclarationParentheses ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"public abstract class Test
{
	public abstract Test TestMethod();
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceBeforeMethodDeclarationParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			Console.WriteLine (data.Document.Text);
			Assert.AreEqual (@"public abstract class Test
{
	public abstract Test TestMethod ();
}", data.Document.Text);
		}
		
		
		
		
		[Test()]
		public void TestSpaceBeforeConstructorDeclarationParenthesesDestructorCase ()
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
			policy.SpaceBeforeConstructorDeclarationParentheses = true;
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
			policy.SpaceAroundMultiplicativeOperator = true;
			
			TestBinaryOperator (policy, "*");
			TestBinaryOperator (policy, "/");
		}
		
		[Test()]
		public void TestSpacesAroundShiftOperator ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceAroundShiftOperator = true;
			TestBinaryOperator (policy, "<<");
			TestBinaryOperator (policy, ">>");
		}
		
		[Test()]
		public void TestSpacesAroundAdditiveOperator ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceAroundAdditiveOperator = true;
			
			TestBinaryOperator (policy, "+");
			TestBinaryOperator (policy, "-");
		}
		
		[Test()]
		public void TestSpacesAroundBitwiseOperator ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceAroundBitwiseOperator = true;
			
			TestBinaryOperator (policy, "&");
			TestBinaryOperator (policy, "|");
			TestBinaryOperator (policy, "^");
		}
		
		[Test()]
		public void TestSpacesAroundRelationalOperator ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceAroundRelationalOperator = true;
			
			TestBinaryOperator (policy, "<");
			TestBinaryOperator (policy, "<=");
			TestBinaryOperator (policy, ">");
			TestBinaryOperator (policy, ">=");
		}
		
		[Test()]
		public void TestSpacesAroundEqualityOperator ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceAroundEqualityOperator = true;
			
			TestBinaryOperator (policy, "==");
			TestBinaryOperator (policy, "!=");
		}
		
		[Test()]
		public void TestSpacesAroundLogicalOperator ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceAroundLogicalOperator = true;
			
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
			policy.SpaceAfterConditionalOperatorCondition = true;
			policy.SpaceAfterConditionalOperatorSeparator = true;
			policy.SpaceBeforeConditionalOperatorCondition = true;
			policy.SpaceBeforeConditionalOperatorSeparator = true;
			
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
			policy.SpaceAfterConditionalOperatorCondition = false;
			policy.SpaceAfterConditionalOperatorSeparator = false;
			policy.SpaceBeforeConditionalOperatorCondition = false;
			policy.SpaceBeforeConditionalOperatorSeparator = false;
			
			compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			i1 = data.Document.Text.IndexOf ("true");
			i2 = data.Document.Text.IndexOf ("falseexpr") + "falseexpr".Length;
			Assert.AreEqual (@"true?trueexpr:falseexpr", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestSpaceBeforeMethodCallParenthesesSpace ()
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
			policy.SpaceBeforeMethodCallParentheses = true;
			
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
			policy.SpaceBeforeMethodCallParentheses = false;
			
			compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			i1 = data.Document.Text.IndexOf ("MethodCall");
			i2 = data.Document.Text.IndexOf (";") + ";".Length;
			Assert.AreEqual (@"MethodCall();", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestSpaceWithinMethodCallParenthesesSpace ()
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
			policy.SpacesWithinMethodCallParentheses = true;
			
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
			policy.SpacesWithinMethodCallParentheses = false;
			
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
			policy.SpaceBeforeIfParentheses = true;
			
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
			policy.SpacesWithinIfParentheses = true;
			
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
			policy.SpaceBeforeWhileParentheses = true;
			
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
			policy.SpacesWithinWhileParentheses = true;
			
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
			policy.SpaceBeforeForParentheses = true;
			
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
			policy.SpacesWithinForParentheses = true;
			
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
			policy.SpaceBeforeForeachParentheses = true;
			
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
			policy.SpacesWithinForEachParentheses = true;
			
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
			policy.SpaceBeforeCatchParentheses = true;
			
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
			policy.SpacesWithinCatchParentheses = true;
			
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
			policy.SpaceBeforeLockParentheses = true;
			
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
			policy.SpacesWithinLockParentheses = true;
			
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
			policy.SpaceAfterForSemicolon = true;
			
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
			policy.SpaceBeforeForSemicolon = true;
			policy.SpaceAfterForSemicolon = false;
			
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
			policy.SpaceAfterTypecast = true;
			
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
			policy.SpaceBeforeUsingParentheses = true;
			
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
			policy.SpacesWithinUsingParentheses = true;
			
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
			policy.SpaceAroundAssignment = true;
			
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
			policy.SpaceAroundAssignment = true;
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
			policy.SpaceBeforeSwitchParentheses = true;
			
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
			policy.SpacesWithinSwitchParentheses = true;
			
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
			policy.SpacesWithinParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( test )", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestSpaceWithinMethodDeclarationParenthesesSpace ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	void TestMe (int a)
	{
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpacesWithinMethodDeclarationParentheses = true;
			
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
			policy.SpacesWithinCastParentheses = true;
			
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
			policy.SpacesWithinSizeOfParentheses = true;
			
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
			policy.SpaceBeforeSizeOfParentheses = true;
			
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
			policy.SpacesWithinTypeOfParentheses = true;
			
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
			policy.SpaceBeforeTypeOfParentheses = true;
			
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
			policy.SpacesWithinCheckedExpressionParantheses = true;
			
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
			policy.SpaceBeforeNewParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("new");
			int i2 = data.Document.Text.LastIndexOf (";") + ";".Length;
			Assert.AreEqual (@"new Test ();", data.Document.GetTextBetween (i1, i2));
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
			policy.SpaceBeforeFieldDeclarationComma = false;
			policy.SpaceAfterFieldDeclarationComma = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("int");
			int i2 = data.Document.Text.LastIndexOf (";") + ";".Length;
			Assert.AreEqual (@"int a, b, c;", data.Document.GetTextBetween (i1, i2));
			policy.SpaceBeforeFieldDeclarationComma = true;
			
			compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			i1 = data.Document.Text.LastIndexOf ("int");
			i2 = data.Document.Text.LastIndexOf (";") + ";".Length;
			Assert.AreEqual (@"int a , b , c;", data.Document.GetTextBetween (i1, i2));
			
			policy.SpaceBeforeFieldDeclarationComma = false;
			policy.SpaceAfterFieldDeclarationComma = false;
			compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			i1 = data.Document.Text.LastIndexOf ("int");
			i2 = data.Document.Text.LastIndexOf (";") + ";".Length;
			Assert.AreEqual (@"int a,b,c;", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestSpaceBeforeMethodDeclarationParameterComma ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	public void Foo (int a,int b,int c) {}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceBeforeMethodDeclarationParameterComma = true;
			policy.SpaceAfterMethodDeclarationParameterComma = false;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a ,int b ,int c)", data.Document.GetTextBetween (i1, i2));
			compilationUnit = new CSharpParser ().Parse (data);
			
			policy.SpaceBeforeMethodDeclarationParameterComma = false;
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			i1 = data.Document.Text.LastIndexOf ("(");
			i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a,int b,int c)", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestSpaceAfterMethodDeclarationParameterComma ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	public void Foo (int a,int b,int c) {}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceBeforeMethodDeclarationParameterComma = false;
			policy.SpaceAfterMethodDeclarationParameterComma = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a, int b, int c)", data.Document.GetTextBetween (i1, i2));
			compilationUnit = new CSharpParser ().Parse (data);
			
			policy.SpaceAfterMethodDeclarationParameterComma = false;
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
			policy.SpacesWithinWhileParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.IndexOf ("x");
			int i2 = data.Document.Text.LastIndexOf ("null") + "null".Length;
			Assert.AreEqual (@"x => x != null", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestSpaceBeforeLocalVariableDeclarationComma ()
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
			policy.SpaceBeforeLocalVariableDeclarationComma = true;
			policy.SpaceAfterLocalVariableDeclarationComma = false;

			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.IndexOf ("int");
			int i2 = data.Document.Text.IndexOf (";") + ";".Length;
			Assert.AreEqual (@"int a ,b ,c;", data.Document.GetTextBetween (i1, i2));

			compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);

			policy.SpaceBeforeLocalVariableDeclarationComma = false;

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
			policy.SpaceBeforeLocalVariableDeclarationComma = true;
			policy.SpaceAfterLocalVariableDeclarationComma = true;

			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.IndexOf ("int");
			int i2 = data.Document.Text.IndexOf (";") + ";".Length;
			Assert.AreEqual (@"int a = 5 , b = 6 , c;", data.Document.GetTextBetween (i1, i2));

			compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);

			policy.SpaceBeforeLocalVariableDeclarationComma = false;
			policy.SpaceAfterLocalVariableDeclarationComma = false;

			compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			i1 = data.Document.Text.IndexOf ("int");
			i2 = data.Document.Text.IndexOf (";") + ";".Length;
			Assert.AreEqual (@"int a = 5,b = 6,c;", data.Document.GetTextBetween (i1, i2));
		}

		#region Constructors
		
		[Test()]
		public void TestSpaceBeforeConstructorDeclarationParentheses ()
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
			policy.SpaceBeforeConstructorDeclarationParentheses = true;
			
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
		public void TestSpaceBeforeConstructorDeclarationParameterComma ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	public Test (int a,int b,int c) {}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceBeforeConstructorDeclarationParameterComma = true;
			policy.SpaceAfterConstructorDeclarationParameterComma = false;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a ,int b ,int c)", data.Document.GetTextBetween (i1, i2));
			compilationUnit = new CSharpParser ().Parse (data);
			
			policy.SpaceBeforeConstructorDeclarationParameterComma = false;
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			i1 = data.Document.Text.LastIndexOf ("(");
			i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a,int b,int c)", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestSpaceAfterConstructorDeclarationParameterComma ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	public Test (int a,int b,int c) {}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceBeforeConstructorDeclarationParameterComma = false;
			policy.SpaceAfterConstructorDeclarationParameterComma = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a, int b, int c)", data.Document.GetTextBetween (i1, i2));
			compilationUnit = new CSharpParser ().Parse (data);
			
			policy.SpaceAfterConstructorDeclarationParameterComma = false;
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			i1 = data.Document.Text.LastIndexOf ("(");
			i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a,int b,int c)", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestSpaceWithinConstructorDeclarationParentheses ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	Test (int a)
	{
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpacesWithinConstructorDeclarationParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( int a )", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestSpaceBetweenEmptyConstructorDeclarationParentheses ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {
	Test ()
	{
	}
}";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceBetweenEmptyConstructorDeclarationParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( )", data.Document.GetTextBetween (i1, i2));
		}
		
		#endregion
		
		#region Delegates
		[Test()]
		public void TestSpaceBeforeDelegateDeclarationParentheses ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"delegate void Test();";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceBeforeDelegateDeclarationParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			Assert.AreEqual (@"delegate void Test ();", data.Document.Text);
		}
		
		[Test()]
		public void TestSpaceBeforeDelegateDeclarationParenthesesComplex ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = "delegate void TestDelegate\t\t\t();";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceBeforeDelegateDeclarationParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			Assert.AreEqual (@"delegate void TestDelegate ();", data.Document.Text);
		}
		
		[Test()]
		public void TestSpaceBeforeDelegateDeclarationParameterComma ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"delegate void Test (int a,int b,int c);";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceBeforeDelegateDeclarationParameterComma = true;
			policy.SpaceAfterDelegateDeclarationParameterComma = false;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a ,int b ,int c)", data.Document.GetTextBetween (i1, i2));
			compilationUnit = new CSharpParser ().Parse (data);
			
			policy.SpaceBeforeDelegateDeclarationParameterComma = false;
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			i1 = data.Document.Text.LastIndexOf ("(");
			i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a,int b,int c)", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestSpaceAfterDelegateDeclarationParameterComma ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"delegate void Test (int a,int b,int c);";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceBeforeDelegateDeclarationParameterComma = false;
			policy.SpaceAfterDelegateDeclarationParameterComma = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a, int b, int c)", data.Document.GetTextBetween (i1, i2));
			compilationUnit = new CSharpParser ().Parse (data);
			
			policy.SpaceAfterDelegateDeclarationParameterComma = false;
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			i1 = data.Document.Text.LastIndexOf ("(");
			i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a,int b,int c)", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestSpaceWithinDelegateDeclarationParentheses ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"delegate void Test (int a);";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpacesWithinDelegateDeclarationParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( int a )", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestSpaceBetweenEmptyDelegateDeclarationParentheses ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"delegate void Test();";
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceBetweenEmptyDelegateDeclarationParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( )", data.Document.GetTextBetween (i1, i2));
		}
		
		#endregion
		
		#region Method invocations
		[Test()]
		public void TestSpaceBeforeMethodCallParentheses ()
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
			policy.SpaceBeforeMethodCallParentheses = true;
			
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
		public void TestSpaceBeforeMethodCallParameterComma ()
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
			policy.SpaceBeforeMethodCallParameterComma = true;
			policy.SpaceAfterMethodCallParameterComma = false;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(a ,b ,c)", data.Document.GetTextBetween (i1, i2));
			compilationUnit = new CSharpParser ().Parse (data);
			
			policy.SpaceBeforeMethodCallParameterComma = false;
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			i1 = data.Document.Text.LastIndexOf ("(");
			i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(a,b,c)", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestSpaceAfterMethodCallParameterComma ()
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
			policy.SpaceBeforeMethodCallParameterComma = false;
			policy.SpaceAfterMethodCallParameterComma = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(a, b, c)", data.Document.GetTextBetween (i1, i2));
			compilationUnit = new CSharpParser ().Parse (data);
			
			policy.SpaceAfterMethodCallParameterComma = false;
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			i1 = data.Document.Text.LastIndexOf ("(");
			i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(a,b,c)", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestSpaceWithinMethodCallParentheses ()
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
			policy.SpacesWithinMethodCallParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( a )", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestSpaceBetweenEmptyMethodCallParentheses ()
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
			policy.SpaceBetweenEmptyMethodCallParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("(");
			int i2 = data.Document.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( )", data.Document.GetTextBetween (i1, i2));
		}
		
		#endregion
		
		#region Indexer declarations
		[Test()]
		public void TestSpaceBeforeIndexerDeclarationBracket ()
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
			policy.SpaceBeforeIndexerDeclarationBracket = true;
			
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
		public void TestSpaceBeforeIndexerDeclarationParameterComma ()
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
			policy.SpaceBeforeIndexerDeclarationParameterComma = true;
			policy.SpaceAfterIndexerDeclarationParameterComma = false;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("[");
			int i2 = data.Document.Text.LastIndexOf ("]") + "]".Length;
			Assert.AreEqual (@"[int a ,int b]", data.Document.GetTextBetween (i1, i2));

		}
		
		[Test()]
		public void TestSpaceAfterIndexerDeclarationParameterComma ()
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
			policy.SpaceAfterIndexerDeclarationParameterComma = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.LastIndexOf ("[");
			int i2 = data.Document.Text.LastIndexOf ("]") + "]".Length;
			Assert.AreEqual (@"[int a, int b]", data.Document.GetTextBetween (i1, i2));
		}
		
		[Test()]
		public void TestSpaceWithinIndexerDeclarationBracket ()
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
			policy.SpacesWithinIndexerDeclarationBracket = true;
			
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
			policy.SpaceBeforeBrackets = false;
			
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
			policy.SpaceBeforeBrackets = true;
			
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
			policy.SpaceBeforeBracketComma = true;
			policy.SpaceAfterBracketComma = false;
			
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
			policy.SpaceAfterBracketComma = true;
			
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
			policy.SpaceBeforeArrayDeclarationBrackets = true;
			
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
			policy.SpaceBeforeForParentheses = true;
			
			var compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new AstSpacingVisitor (policy, data), null);
			int i1 = data.Document.Text.IndexOf ("Foo");
			int i2 = data.Document.Text.LastIndexOf (";") + ";".Length;
			Assert.AreEqual (@"Foo ();", data.Document.GetTextBetween (i1, i2));
		}
		
	}
}
