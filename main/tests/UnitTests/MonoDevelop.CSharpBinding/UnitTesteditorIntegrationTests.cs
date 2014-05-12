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
using System.Threading;
using MonoDevelop.Projects;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide;

namespace MonoDevelop.CSharpBinding.Tests
{
	[TestFixture]
	public class UnitTesteditorIntegrationTests : UnitTests.TestBase
	{
		static UnitTestTextEditorExtension Setup (string input, out TestViewContent content)
		{
			var tww = new TestWorkbenchWindow ();
			content = new TestViewContent ();
			tww.ViewContent = content;
			content.ContentName = "/a.cs";
			content.GetTextEditorData ().Document.MimeType = "text/x-csharp";

			var doc = new Document (tww);

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

			var project = new DotNetAssemblyProject (Microsoft.CodeAnalysis.LanguageNames.CSharp);
			project.Name = "test";
			project.References.Add (new ProjectReference (ReferenceType.Package, "System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"));
			project.References.Add (new ProjectReference (ReferenceType.Package, "System.Core"));

			project.FileName = "test.csproj";
			project.Files.Add (new ProjectFile ("/a.cs", BuildAction.Compile)); 

			var solution = new MonoDevelop.Projects.Solution ();
			var config = solution.AddConfiguration ("", true); 
			solution.DefaultSolutionFolder.AddItem (project);
			RoslynTypeSystemService.Load (solution);
			content.Project = project;
			doc.SetProject (project);

			var compExt = new UnitTestTextEditorExtension ();
			compExt.Initialize (doc);
			content.Contents.Add (compExt);
			doc.UpdateParseDocument ();
			return compExt;
		}

		protected override void InternalSetup (string rootDir)
		{
			base.InternalSetup (rootDir);
			IdeApp.Initialize (new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor ()); 
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
			var tests = ext.GatherUnitTests (default(CancellationToken)).Result;
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
			var tests = ext.GatherUnitTests (default(CancellationToken)).Result;
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
			var tests = ext.GatherUnitTests (default(CancellationToken)).Result;
			Assert.IsNotNull (tests);
			Assert.AreEqual (2, tests.Count);

			Assert.AreEqual ("Test.Derived", tests [0].UnitTestIdentifier);
			Assert.AreEqual ("Test.Derived.MyTest", tests [1].UnitTestIdentifier);
		}


		/// <summary>
		/// Bug 19651 - Should not require [TestFixture] for Unit Test Integration
		/// </summary>
		[Test]
		public void TestBug19651 ()
		{
			TestViewContent content;
			var ext = Setup (@"using NUnit.Framework;
class Test
{
	[Test]
	public void MyTest () {}
}
", out content);
			var tests = ext.GatherUnitTests (default(CancellationToken)).Result;
			Assert.IsNotNull (tests);
			Assert.AreEqual (2, tests.Count);
		}

	}
}

