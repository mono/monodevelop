//
// CodeCompletionCSharp3Tests.cs
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
	public class CodeCompletionCSharp3Tests : UnitTests.TestBase
	{
		[Test()]
		public void TestExtensionMethods ()
		{
			CodeCompletionDataProvider provider = CodeCompletionTests.CreateProvider (
@"using System;

public static class EMClass
{
	public static int ToInt32Ext (this Program s)
	{
		return Int32.Parse (s);
	}
}

class Program
{
	static void Main (string[] args)
	{
		Program s;
		int i = s.$
	}
}
");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.SearchData ("ToInt32Ext"), "extension method 'ToInt32Ext' not found.");
		}
		
		[Test()]
		public void TestVarLocalVariables ()
		{
			CodeCompletionDataProvider provider = CodeCompletionTests.CreateProvider (
@"using System;

class Test
{
	public void TestMethod ()
	{
	}
}

class Program
{
	static void Main (string[] args)
	{
		var t = new Test ();
		t.$
	}
}
");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.SearchData ("TestMethod"), "method 'TestMethod' not found.");
		}
		
		[Test()]
		public void TestVarLoopVariable ()
		{
			CodeCompletionDataProvider provider = CodeCompletionTests.CreateProvider (
@"using System;

class Test
{
	public void TestMethod ()
	{
	}
}

class Program
{
	static void Main (string[] args)
	{
		var t = new Test[] {};
		foreach (var loopVar in t) {
			loopVar.$
		}
	}
}
");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.SearchData ("TestMethod"), "method 'TestMethod' not found.");
		}

		[Test()]
		public void TestAnonymousType ()
		{
			CodeCompletionDataProvider provider = CodeCompletionTests.CreateProvider (
@"
class Program
{
	static void Main (string[] args)
	{
		var t = new { TestInt = 6, TestChar='e', TestString =""Test""};
		t.$
	}
}
");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.SearchData ("TestInt"), "property 'TestInt' not found.");
			Assert.IsNotNull (provider.SearchData ("TestChar"), "property 'TestChar' not found.");
			Assert.IsNotNull (provider.SearchData ("TestString"), "property 'TestString' not found.");
		}
		

	}
}
