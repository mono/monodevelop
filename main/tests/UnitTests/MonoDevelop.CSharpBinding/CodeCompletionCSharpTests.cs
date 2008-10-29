//
// CodeCompletionCSharpTests.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using NUnit.Framework;
using MonoDevelop.CSharpBinding.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.CSharpBinding.Tests
{
	[TestFixture()]
	public class CodeCompletionCSharpTests
	{
		[Test()]
		public void TestUsingDeclaration ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"namespace Test {
	class Class
	{
	}
}

using $
");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("Test"), "namespace 'Test' not found.");
		}
		
		[Test()]
		public void TestLocalVariableDeclaration ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"class Test
{
	void Test ()
	{
		Test t;
		t.$
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("Test"), "method 'Test' not found.");
		}
		
		[Test()]
		public void TestObjectCreationExpression ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"class Test
{
	void Test ()
	{
		new Test ().$
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("Test"), "method 'Test' not found.");
		}
		
		[Test()]
		public void TestCastExpression ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"class Test
{
	void Test ()
	{
		object o;
		((Test)o).$
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("Test"), "method 'Test' not found.");
		}
		
		[Test()]
		public void TestThisReferenceExpression ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"class Test
{
	void Test ()
	{
		this.$
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("Test"), "method 'Test' not found.");
		}
		
		[Test()]
		public void TestBaseReferenceExpression ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
class Test
{
	public void Test ()
	{
	}
}

class Test2 : Test
{
	void Test2 ()
	{
		base.$
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("Test"), "method 'Test' not found.");
			Assert.IsNull (provider.Find ("Test2"), "method 'Test2' found but shouldn't.");
		}
		
		[Test()]
		public void TestConditionalExpression ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
class Test
{
	void Test ()
	{
		Test a, b;
		(1 == 1 ? a : b).$
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("Test"), "method 'Test' not found.");
		}

		[Test()]
		public void TestIndexerExpression ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
class Test
{
	void Test ()
	{
		Test[] a;
		a[0].$
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("Test"), "method 'Test' not found.");
		}
		
		[Test()]
		public void TestInvocationExpression ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
class Test
{
	static Test GetTest () {}
	
	void Test ()
	{
		GetTest ().$
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("Test"), "method 'Test' not found.");
		}
		
		[Test()]
		public void TestParenthesizedExpression ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
class Test
{
	void Test ()
	{
		(this).$
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("Test"), "method 'Test' not found.");
		}
		
		[Test()]
		public void TestForeachLoopVariable ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
class Test
{
	void Test ()
	{
		foreach (Test t in notExist)
			t.$
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("Test"), "method 'Test' not found.");
		}
		

	}
}
