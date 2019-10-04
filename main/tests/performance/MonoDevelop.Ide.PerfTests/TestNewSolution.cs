//
// TestNewSolution.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corporation
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

using MonoDevelop.Core;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.PerformanceTesting;
using MonoDevelop.UserInterfaceTesting;
using MonoDevelop.UserInterfaceTesting.Controllers;
using NUnit.Framework;

namespace MonoDevelop.Ide.PerfTests
{
	[TestFixture]
	[BenchmarkCategory]
	public class TestNewSolution : UITestBase
	{
		public override void SetUp ()
		{
			InstrumentationService.Enabled = true;
			PreStart ();
		}

		[Test]
		[Benchmark (Tolerance = 0.50)]
		public void TestNewConsoleProject ()
		{
			OpenApplicationAndWait ();

			var templateOptions = new TemplateSelectionOptions {
				CategoryRoot = "Other",
				Category = ".NET",
				TemplateKindRoot = "General",
				TemplateKind = "Console Project"
			};

			CreateNewSolutionAndWait (templateOptions);

			Session.RunAndWaitForTimer (() => {
				// Do nothing. Cannot call this with CreateNewSolutionAndWait since the timer is not available
				// at that point but is available after it has been called. Using RunAndWaitForTimer here
				// ensures we wait for the timer to change. Otherwise we would get the timer with no duration
				// since the project is still being created.
			}, "Ide.NewProjectDialog.ProjectCreated");

			var t = Session.GetTimerDuration ("Ide.NewProjectDialog.ProjectCreated");

			Benchmark.SetTime (t.TotalSeconds);
		}

		void CreateNewSolutionAndWait (TemplateSelectionOptions templateOptions)
		{
			var newProject = new NewProjectController ();
			newProject.Open ();

			newProject.Select (templateOptions);
			newProject.Next ();

			newProject.SetProjectName ("NewProjectTest", false);

			FilePath solutionDirectory = Util.CreateTmpDir ()
				.Combine ("NewSolutionRoot");
			newProject.SetSolutionLocation (solutionDirectory);

			newProject.CreateProjectInSolutionDirectory (true);

			var gitOptions = new GitOptions {
				UseGit = true,
				UseGitIgnore = true
			};
			newProject.UseGit (gitOptions);

			newProject.Create ();
		}
	}
}
