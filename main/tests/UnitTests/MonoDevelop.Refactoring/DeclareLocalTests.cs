// 
// DeclareLocalTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Refactoring;
using System.Collections.Generic;
using MonoDevelop.CSharp.Refactoring.DeclareLocal;

namespace MonoDevelop.Refactoring.Tests
{
	[TestFixture()]
	public class DeclareLocalTests : UnitTests.TestBase
	{
		void TestDeclareLocal (string inputString, string outputString)
		{
			DeclareLocalCodeGenerator refactoring = new DeclareLocalCodeGenerator ();
			RefactoringOptions options = ExtractMethodTests.CreateRefactoringOptions (inputString);
			List<Change> changes = refactoring.PerformChanges (options, null);
			string output = ExtractMethodTests.GetOutput (options, changes);
			Assert.IsTrue (ExtractMethodTests.CompareSource (output, outputString), "Expected:" + Environment.NewLine + outputString + Environment.NewLine + "was:" + Environment.NewLine + output);
		}
		
		[Test()]
		[Ignore()]
		public void DeclareLocalStatementTest ()
		{
			TestDeclareLocal (@"class TestClass
{
	void Test ()
	{
		<-test()->;
	}
}
", @"class TestClass
{
	void Test ()
	{
		int i = test();
	}
}
");
		}
		
		/// <summary>
		/// Bug 693855 - Extracting variable from ELSE IF puts it in the wrong place
		/// </summary>
		[Test()]
		public void DeclareLocalexpressionTest ()
		{
			TestDeclareLocal (@"class TestClass
{
	void Test ()
	{
		Console.WriteLine (1 +<- 9 ->+ 5);
	}
}
", @"class TestClass
{
	void Test ()
	{
		int i = 9;
		Console.WriteLine (1 + i + 5);
	}
}
");
		}
		
		/// <summary>
		/// Bug 693855 - Extracting variable from ELSE IF puts it in the wrong place
		/// </summary>
		[Test()]
		public void TestBug693855 ()
		{
			TestDeclareLocal (@"class TestClass
{
	void Test ()
	{
		string str = ""test"";
		if (str == ""something"") {
			//do A
		} else if (<-str == ""other""->) {
			//do B
		} else {
			//do C
		}
	}
}", 
@"class TestClass
{
	void Test ()
	{
		string str = ""test"";
		bool b = str == ""other"";
		if (str == ""something"") {
			//do A
		} else if (b) {
			//do B
		} else {
			//do C
		}
	}
}");
		}
		
		
		/// <summary>
		/// Bug 693875 - Extract Local on just method name leaves brackets in wrong place
		/// </summary>
		[Test()]
		public void TestBug693875 ()
		{
			TestDeclareLocal (@"class TestClass
{
	void DoStuff() 
	{
		if (<-GetInt->() == 0) {
		}
	}
	
	int GetInt()
	{
		return 1;
	}
}", 
@"class TestClass
{
	void DoStuff() 
	{
		System.Func<int> func = GetInt;
		if (func() == 0) {
		}
	}
	
	int GetInt()
	{
		return 1;
	}
}");
		}
	}
}
