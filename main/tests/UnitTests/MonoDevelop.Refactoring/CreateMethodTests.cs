// 
// CreateMethodTests.cs
//  
// Author:
//       mkrueger <>
// 
// Copyright (c) 2010 mkrueger
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
using MonoDevelop.CSharp.Refactoring.CreateMethod;
using NUnit.Framework;
using System.Collections.Generic;
using Mono.TextEditor;
using MonoDevelop.Projects.CodeGeneration;
using System.Linq;
using MonoDevelop.Ide;

namespace MonoDevelop.Refactoring.Tests
{
	[TestFixture()]
	public class CreateMethodTests : UnitTests.TestBase
	{
		class CSharpCodeGeneratorNode : MimeTypeExtensionNode
		{
			public CSharpCodeGeneratorNode ()
			{
				MimeType = "text/x-csharp";
			}

			public override object CreateInstance ()
			{
				return new MonoDevelop.CSharp.Refactoring.CSharpCodeGenerator ();
			}
		}
		
		void TestCreateMethod (string input, string outputString)
		{
			TestCreateMethod (input, outputString, false);
		}

		void TestCreateMethod (string input, string outputString, bool returnWholeFile)
		{
			var generator = new CSharpCodeGeneratorNode ();
			MonoDevelop.Projects.CodeGeneration.CodeGenerator.AddGenerator (generator);
			var refactoring = new CreateMethodCodeGenerator ();
			RefactoringOptions options = ExtractMethodTests.CreateRefactoringOptions (input);
			Assert.IsTrue (refactoring.IsValid (options));
			if (returnWholeFile) {
				refactoring.SetInsertionPoint (CodeGenerationService.GetInsertionPoints (options.Document, refactoring.DeclaringType).First ());
			} else {
				DocumentLocation loc = new DocumentLocation (1, 1);
				refactoring.SetInsertionPoint (new InsertionPoint (loc, NewLineInsertion.Eol, NewLineInsertion.Eol));
			}
			List<Change> changes = refactoring.PerformChanges (options, null);
//			changes.ForEach (c => Console.WriteLine (c));
			// get just the generated method.
			string output = ExtractMethodTests.GetOutput (options, changes);
			if (returnWholeFile) {
				Assert.IsTrue (ExtractMethodTests.CompareSource (output, outputString), "Expected:" + Environment.NewLine + outputString + Environment.NewLine + "was:" + Environment.NewLine + output);
				return;
			}
			output = output.Substring (0, output.IndexOf ('}') + 1).Trim ();
			// crop 1 level of indent
			Document doc = new Document (output);
			foreach (LineSegment line in doc.Lines) {
				if (doc.GetCharAt (line.Offset) == '\t')
					((IBuffer)doc).Remove (line.Offset, 1);
			}
			output = doc.Text;
			
			Assert.IsTrue (ExtractMethodTests.CompareSource (output, outputString), "Expected:" + Environment.NewLine + outputString + Environment.NewLine + "was:" + Environment.NewLine + output);
			MonoDevelop.Projects.CodeGeneration.CodeGenerator.RemoveGenerator (generator);
		}

		[Test()]
		public void TestPrivateSimpleCreateMethod ()
		{
			TestCreateMethod (@"class TestClass
{
	int member = 5;
	string Test { get; set; }

	void TestMethod ()
	{
		$NonExistantMethod (member, Test, 5);
	}
}
", @"void NonExistantMethod (int member, string test, int par1)
{
	throw new System.NotImplementedException ();
}");
		}

		[Test()]
		public void TestPublicSimpleCreateMethod ()
		{
			TestCreateMethod (@"class TestClass
{
	public void TestMethod ()
	{
		int testLocalVar;
		$NonExistantMethod (testLocalVar);
	}
}
", @"public void NonExistantMethod (int testLocalVar)
{
	throw new System.NotImplementedException ();
}");
		}

		[Test()]
		public void TestGuessAssignmentReturnType ()
		{
			TestCreateMethod (@"class TestClass
{
	static void TestMethod ()
	{
		int testLocalVar = $NonExistantMethod ();
	}
}", @"static int NonExistantMethod ()
{
	throw new System.NotImplementedException ();
}");
		}

		[Test()]
		public void TestGuessAssignmentReturnTypeCase2 ()
		{
			TestCreateMethod (@"class TestClass
{
	static void TestMethod ()
	{
		int testLocalVar;
		testLocalVar = $NonExistantMethod ();
	}
}", @"static int NonExistantMethod ()
{
	throw new System.NotImplementedException ();
}");
		}

		[Test()]
		public void TestGuessAssignmentReturnTypeCase3 ()
		{
			TestCreateMethod (@"class TestClass
{
	static void TestMethod ()
	{
		var testLocalVar = (string)$NonExistantMethod ();
	}
}", @"static string NonExistantMethod ()
{
	throw new System.NotImplementedException ();
}");
		}

		[Test()]
		public void TestGuessParameterType ()
		{
			TestCreateMethod (@"class TestClass
{
	void TestMethod ()
	{
		Test ($NonExistantMethod ());
	}
	void Test (int a) {}

}", @"int NonExistantMethod ()
{
	throw new System.NotImplementedException ();
}");
		}
		
		[Test()]
		public void TestCreateDelegateDeclaration ()
		{
			TestCreateMethod (@"
class TestClass
{
	public event MyDelegate MyEvent;

	void TestMethod ()
	{
		MyEvent += $NonExistantMethod;
	}
}

public delegate string MyDelegate (int a, object b);
", @"string NonExistantMethod (int a, object b)
{
	throw new System.NotImplementedException ();
}");
		}
		
		[Test()]
		public void TestExternMethod ()
		{
			TestCreateMethod (
@"
class FooBar
{
}

class TestClass
{
	void TestMethod ()
	{
		var fb = new FooBar ();
		fb.$NonExistantMethod ();
	}
}
", @"
class FooBar
{
	public void NonExistantMethod ()
	{
		throw new System.NotImplementedException ();
	}	
}

class TestClass
{
	void TestMethod ()
	{
		var fb = new FooBar ();
		fb.NonExistantMethod ();
	}
}
", true);
		}
		
		[Test()]
		public void TestCreateInterfaceMethod ()
		{
			TestCreateMethod (
@"
interface FooBar
{
}

class TestClass
{
	void TestMethod ()
	{
		FooBar fb;
		fb.$NonExistantMethod ();
	}
}
", @"
interface FooBar
{
	void NonExistantMethod ();
}

class TestClass
{
	void TestMethod ()
	{
		FooBar fb;
		fb.NonExistantMethod ();
	}
}
", true);
		}
		
		[Test()]
		public void TestCreateInStaticClassMethod ()
		{
			TestCreateMethod (
@"
static class FooBar
{
}

class TestClass
{
	void TestMethod ()
	{
		FooBar.$NonExistantMethod ();
	}
}
", @"public static void NonExistantMethod ()
{
	throw new System.NotImplementedException ();
}");
		}
		
		[Test()]
		public void TestRefOutCreateMethod ()
		{
			TestCreateMethod (@"class TestClass
{
	void TestMethod ()
	{
		int a, b;
		$NonExistantMethod (ref a, out b);
	}
}
", @"void NonExistantMethod (ref int a, out int b)
{
	throw new System.NotImplementedException ();
}");
		}
		
		/// <summary>
		/// Bug 677522 - "Create Method" creates at wrong indent level
		/// </summary>
		[Test()]
		public void TestBug677522 ()
		{
			TestCreateMethod (
@"namespace Test {
	class TestClass
	{
		void TestMethod ()
		{
			$NonExistantMethod ();
		}
	}
}
", @"namespace Test {
	class TestClass
	{
		void NonExistantMethod ()
		{
			throw new System.NotImplementedException ();
		}	
		
		void TestMethod ()
		{
			NonExistantMethod ();
		}
	}
}
", true);
		}
		
		/// <summary>
		/// Bug 677527 - "Create Method" uses fully qualified namespace when "using" statement exists
		/// </summary>
		[Test()]
		public void TestBug677527 ()
		{
			TestCreateMethod (
@"using System.Text;

namespace Test {
	class TestClass
	{
		void TestMethod ()
		{
			StringBuilder sb = new StringBuilder ();
			$NonExistantMethod (sb);
		}
	}
}
", @"using System.Text;

namespace Test {
	class TestClass
	{
		void NonExistantMethod (StringBuilder sb)
		{
			throw new System.NotImplementedException ();
		}	
		
		void TestMethod ()
		{
			StringBuilder sb = new StringBuilder ();
			NonExistantMethod (sb);
		}
	}
}
", true);
		}
		
		
		/// <summary>
		/// Bug 693949 - Create method uses the wrong type for param
		/// </summary>
		[Test()]
		public void TestBug693949 ()
		{
			// the c# code isn't 100% correct since test isn't accessible in Main (can't call non static method from static member)
			TestCreateMethod (
@"using System.Text;

namespace Test {
	class TestClass
	{
		string test(string a)
		{
		}
	
		public static void Main(string[] args)
		{
			Type a = $M(test(""a""));
		}
	}
}
", @"using System.Text;

namespace Test {
	class TestClass
	{
		public static Type M (string par1)
		{
			throw new System.NotImplementedException ();
		}

		string test(string a)
		{
		}
	
		public static void Main(string[] args)
		{
			Type a = M(test(""a""));
		}
	}
}
", true);
		}
		
	}
	
}

