//
// CodeCompletionAccessibleTests.cs
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
	public class CodeCompletionAccessibleTests
	{
		static string testClass = @"
using System;

public class TestClass
{
	public int PubField;
	public int PubProperty { get; set; }
	public void PubMethod () { }

	protected int ProtField;
	protected int ProtProperty { get; set; }
	protected void ProtMethod () { }
	
	private int PrivField;
	private int PrivProperty { get; set; }
	private void PrivMethod () { }

	public static int PubStaticField;
	public static int PubStaticProperty { get; set; }
	public static void PubStaticMethod () { }

	protected static int ProtStaticField;
	protected static int ProtStaticProperty { get; set; }
	protected static void ProtStaticMethod () { }
	
	private static int PrivStaticField;
	private static int PrivStaticProperty { get; set; }
	private static void PrivStaticMethod () { }
";
		[Test()]
		public void TestNonStaticClassAccess ()
		{
			CompletionDataList provider = CodeCompletionTests.CreateProvider (testClass +
@"
	void TestMethod () 
	{
		this.$
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.AreEqual (10, provider.Count); // TestMethod is the 10.
			Assert.IsNotNull (provider.Find ("PubField"));
			Assert.IsNotNull (provider.Find ("PubProperty"));
			Assert.IsNotNull (provider.Find ("PubMethod"));
			
			Assert.IsNotNull (provider.Find ("ProtField"));
			Assert.IsNotNull (provider.Find ("ProtProperty"));
			Assert.IsNotNull (provider.Find ("ProtMethod"));
			
			Assert.IsNotNull (provider.Find ("PrivField"));
			Assert.IsNotNull (provider.Find ("PrivProperty"));
			Assert.IsNotNull (provider.Find ("PrivMethod"));
		}
		
		[Test()]
		public void TestStaticClassAccess ()
		{
			CompletionDataList provider = CodeCompletionTests.CreateProvider (testClass +
@"
	void TestMethod () 
	{
		TestClass.$
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.AreEqual (9, provider.Count);
			Assert.IsNotNull (provider.Find ("PubStaticField"));
			Assert.IsNotNull (provider.Find ("PubStaticProperty"));
			Assert.IsNotNull (provider.Find ("PubStaticMethod"));
			
			Assert.IsNotNull (provider.Find ("ProtStaticField"));
			Assert.IsNotNull (provider.Find ("ProtStaticProperty"));
			Assert.IsNotNull (provider.Find ("ProtStaticMethod"));
			
			Assert.IsNotNull (provider.Find ("PrivStaticField"));
			Assert.IsNotNull (provider.Find ("PrivStaticProperty"));
			Assert.IsNotNull (provider.Find ("PrivStaticMethod"));
		}
		
		[Test()]
		public void TestExternalNonStaticClassAccess ()
		{
			CompletionDataList provider = CodeCompletionTests.CreateProvider (testClass +
@"}
class AClass {
	void TestMethod () 
	{
		TestClass c;
		c.$ 
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.AreEqual (3, provider.Count);
			Assert.IsNotNull (provider.Find ("PubField"));
			Assert.IsNotNull (provider.Find ("PubProperty"));
			Assert.IsNotNull (provider.Find ("PubMethod"));
		}
		
		[Test()]
		public void TestExternalStaticClassAccess ()
		{
			CompletionDataList provider = CodeCompletionTests.CreateProvider (testClass +
@"}
class AClass {
	void TestMethod () 
	{
		TestClass.$ 
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.AreEqual (3, provider.Count);
			Assert.IsNotNull (provider.Find ("PubStaticField"));
			Assert.IsNotNull (provider.Find ("PubStaticProperty"));
			Assert.IsNotNull (provider.Find ("PubStaticMethod"));
		}
		
		[Test()]
		public void TestExternalNonStaticSubclassAccess ()
		{
			CompletionDataList provider = CodeCompletionTests.CreateProvider (testClass +
@"}
class AClass : TestClass {
	void TestMethod () 
	{
		this.$ 
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.AreEqual (7, provider.Count); // TestMethod is 7.
			Assert.IsNotNull (provider.Find ("PubField"));
			Assert.IsNotNull (provider.Find ("PubProperty"));
			Assert.IsNotNull (provider.Find ("PubMethod"));
			Assert.IsNotNull (provider.Find ("ProtField"));
			Assert.IsNotNull (provider.Find ("ProtProperty"));
			Assert.IsNotNull (provider.Find ("ProtMethod"));
		}
	}
}
