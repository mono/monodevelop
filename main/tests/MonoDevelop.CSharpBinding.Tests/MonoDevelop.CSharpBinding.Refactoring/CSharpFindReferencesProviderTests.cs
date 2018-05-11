//
// CSharpFindReferencesProviderTests.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2017 Microsoft Corporation
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
#pragma warning disable RECS0165 // Asynchronous methods should return a Task instead of void

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.NRefactory6;
using Microsoft.CodeAnalysis;
using MonoDevelop.Core;
using MonoDevelop.CSharp.Formatting;
using MonoDevelop.CSharpBinding;
using MonoDevelop.CSharpBinding.Tests;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.FindInFiles;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Policies;
using NUnit.Framework;
using System.Collections.Concurrent;

namespace MonoDevelop.CSharp.Refactoring
{
	[TestFixture]
	class CSharpFindReferencesProviderTests : TestBase
	{

		static async Task<List<SearchResult>> GatherReferences (string input, Func<MonoDevelop.Projects.Project, Task<IEnumerable<SearchResult>>> findRefsCallback)
		{
			TestWorkbenchWindow tww = new TestWorkbenchWindow ();
			TestViewContent content = new TestViewContent ();
			tww.ViewContent = content;
			content.ContentName = "/a.cs";
			content.Data.MimeType = "text/x-csharp";

			var doc = new MonoDevelop.Ide.Gui.Document (tww);

			var text = input;
			content.Text = text;

			var project = Services.ProjectService.CreateProject ("C#");
			project.Name = "test";
			project.FileName = "test.csproj";
			project.Files.Add (new ProjectFile (content.ContentName, BuildAction.Compile));
			project.Policies.Set (PolicyService.InvariantPolicies.Get<CSharpFormattingPolicy> (), CSharpFormatter.MimeType);
			var solution = new MonoDevelop.Projects.Solution ();
			solution.AddConfiguration ("", true);
			solution.DefaultSolutionFolder.AddItem (project);
			using (var monitor = new ProgressMonitor ())
				await TypeSystemService.Load (solution, monitor);
			content.Project = project;
			doc.SetProject (project);

			await doc.UpdateParseDocument ();
			try {
				return (await findRefsCallback (project)).ToList ();
			} finally {
				TypeSystemService.Unload (solution);
			}
		}

		class MockSearchProgressMonitor : SearchProgressMonitor
		{
			internal ConcurrentBag<SearchResult> Results = new ConcurrentBag<SearchResult> ();
			protected override void OnReportResults (IEnumerable<SearchResult> results)
			{
				foreach (var result in results)
					ReportResult (result);
			}

			protected override void OnReportResult (SearchResult result)
			{
				Results.Add (result);
			}
		}

		[Test]
		public async Task TestFindReferences ()
		{
			var refs = await GatherReferences (@"class FooBar {}", async project => {
				var provider = new CSharpFindReferencesProvider ();
				var monitor = new MockSearchProgressMonitor ();
				await provider.FindReferences ("T:FooBar", project, monitor);
				return monitor.Results;
			});
			Assert.AreEqual (1, refs.Count);
		}

		[Test]
		public async Task TestFindAllReferences ()
		{
			var refs = await GatherReferences (@"class FooBar {  
public void Foo() {}
public void Foo(int i) {}
public void Foo(string s) {}
public void Foo(int i, int j) {}
 }", async project => {
				var provider = new CSharpFindReferencesProvider ();
				var monitor = new MockSearchProgressMonitor ();
				await provider.FindAllReferences ("M:FooBar.Foo", project, monitor);
				return monitor.Results;
			});
			Assert.AreEqual (4, refs.Count);
		}

		/// <summary>
		/// Bug 58060 - [VSFeedback Ticket] #454760 - "Find References" fails leaving an error in the log
		/// </summary>
		[Test]
		public async Task TestBug58060 ()
		{
			var refs = await GatherReferences (@"class FooBar { FooBar fb; }", async project => {
				var provider = new CSharpFindReferencesProvider ();
				var monitor = new MockSearchProgressMonitor ();
				await provider.FindAllReferences ("T:FooBar", project, monitor);
				return monitor.Results;
			});
			Assert.AreEqual (2, refs.Count);
		}

		/// <summary>
		/// Bug 591385: [Feedback] Visual Studio Mac Community, find reference stops working in some classes.
		/// </summary>
		[Test]
		public async Task TestBug591385 ()
		{
			var refs = await GatherReferences (@"
public class RefBug {
    public int xxx;
    public void Meth() { xxx++; }
}

public class RefBug2
{
    public int xxx;
    public void Meth() { xxx++; }
}

", async project => {
				var provider = new CSharpFindReferencesProvider ();
				var monitor = new MockSearchProgressMonitor ();
				await provider.FindAllReferences ("F:RefBug2.xxx", project, monitor);
				return monitor.Results;
			});
			Assert.AreEqual (2, refs.Count);
		}
	}
}
