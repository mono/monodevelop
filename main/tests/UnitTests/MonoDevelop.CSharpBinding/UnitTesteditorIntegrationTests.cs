//
// UnitTesteditorIntegrationTests.cs
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

using System;
using System.Collections;

using MonoDevelop.Ide.Gui;
using System.Collections.Generic;
using Mono.Addins;
using NUnit.Framework;
using MonoDevelop.CSharp;

namespace MonoDevelop.CSharpBinding.Tests
{
	[TestFixture]
	public class UnitTesteditorIntegrationTests : UnitTests.TestBase
	{
		static UnitTestTextEditorExtension Setup (string input, out TestViewContent content)
		{
			TestWorkbenchWindow tww = new TestWorkbenchWindow ();
			content = new TestViewContent ();
			tww.ViewContent = content;
			content.ContentName = "a.cs";
			content.GetTextEditorData ().Document.MimeType = "text/x-csharp";

			Document doc = new Document (tww);

			var text = @"namespace NUnit.Framework {
	using System;
	class TestFixtureAttribute : Attribute {} 
	class TestAttribute : Attribute {} 
} namespace Test { " + input +"}";
			int endPos = text.IndexOf ('$');
			if (endPos >= 0)
				text = text.Substring (0, endPos) + text.Substring (endPos + 1);

			content.Text = text;
			content.CursorPosition = System.Math.Max (0, endPos);


			var compExt = new UnitTestTextEditorExtension ();
			compExt.Initialize (doc);
			content.Contents.Add (compExt);

			doc.UpdateParseDocument ();
			return compExt;
		}

		[Test]
		public void TestSimple ()
		{
			TestViewContent content;
			var ext = Setup (@"using NUnit.Framework;
[TestFixture]
class Test
{
	[Test]
	public void MyTest () {}
}
", out content);
			var tests = ext.GatherUnitTests ();
			Assert.IsNotNull (tests);
			Assert.AreEqual (2, tests.Count);
		}

		[Test]
		public void TestNoTests ()
		{
			TestViewContent content;
			var ext = Setup (@"using NUnit.Framework;
class Test
{
	public void MyTest () {}
}
", out content);
			var tests = ext.GatherUnitTests ();
			Assert.IsNotNull (tests);
			Assert.AreEqual (0, tests.Count);
		}



		/// <summary>
		/// Bug 14522 - Unit test editor integration does not work for derived classes 
		/// </summary>
		[Test]
		public void TestBug14522 ()
		{
			TestViewContent content;
			var ext = Setup (@"using NUnit.Framework;
[TestFixture]
abstract class MyBase
{
}

public class Derived : MyBase
{
	[Test]
	public void MyTest () {}
}
", out content);
			var tests = ext.GatherUnitTests ();
			Assert.IsNotNull (tests);
			Assert.AreEqual (2, tests.Count);

			Assert.AreEqual ("Test.Derived", tests [0].UnitTestIdentifier);
			Assert.AreEqual ("Test.Derived.MyTest", tests [1].UnitTestIdentifier);
		}
	}
}

