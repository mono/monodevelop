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

using System;
using System.Linq;
using MonoDevelop.Components.AutoTest;
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
		[Benchmark (Tolerance = 2)]
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

		[Test]
		[Benchmark (Tolerance = 5)]
		public void TestNewXamarinFormsProject ()
		{
			OpenApplicationAndWait ();

			var templateOptions = new TemplateSelectionOptions {
				CategoryRoot = "Multiplatform",
				Category = "App",
				TemplateKindRoot = "Xamarin.Forms",
				TemplateKind = "Blank Forms App"
			};

			CreateNewXamarinFormsSolutionAndWait (templateOptions);

			Session.RunAndWaitForTimer (() => {
				// Do nothing. Cannot call this with CreateNewSolutionAndWait since the timer is not available
				// at that point but is available after it has been called. Using RunAndWaitForTimer here
				// ensures we wait for the timer to change. Otherwise we would get the timer with no duration
				// since the project is still being created.
			}, "Ide.NewProjectDialog.ProjectCreated", 60000);

			var t = Session.GetTimerDuration ("Ide.NewProjectDialog.ProjectCreated");

			Benchmark.SetTime (t.TotalSeconds);
		}

		void CreateNewSolutionAndWait (TemplateSelectionOptions template, Action<NewProjectController> wizardPageAction = null)
		{
			var newProject = new NewProjectController ();
			newProject.Open ();

			AppQuery categoryUIQuery (AppQuery c) => templateCategoriesQuery (c)
				.Contains (template.CategoryRoot)
				.Children ()
				.Text (template.Category);

			Session.SelectElement (templateCategoriesWidgetQuery);
			Session.SelectElement (categoryUIQuery);
			bool result = Session.WaitForElement (c => categoryUIQuery (c).Selected ()).Any ();

			Assert.IsTrue (result, "Project template category not selected {0}/{1}", template.CategoryRoot, template.Category);

			AppQuery templateUIQuery (AppQuery c) => templatesQuery (c)
				.Contains (template.TemplateKindRoot)
				.Children ()
				.Text (template.TemplateKind);

			Session.SelectElement (templatesWidgetQuery);
			Session.SelectElement (templateUIQuery);
			result = Session.WaitForElement (c => templateUIQuery (c).Selected ()).Any ();

			Assert.IsTrue (result, "Project template not selected {0}/{1}", template.TemplateKindRoot, template.TemplateKind);

			// Does not work.
			//result = newProject.SelectTemplateType (template.CategoryRoot, template.Category);

			// Does not work.
			//result = newProject.SelectTemplate (template.TemplateKindRoot, template.TemplateKind);

			newProject.Next ();

			wizardPageAction?.Invoke (newProject);

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

		void CreateNewXamarinFormsSolutionAndWait (TemplateSelectionOptions templateOptions)
		{
			CreateNewSolutionAndWait (templateOptions, FillInXamarinFormsTemplateWizardPage);
		}

		void FillInXamarinFormsTemplateWizardPage (NewProjectController newProject)
		{
			Session.EnterText (c => c.Marked ("appNameTextBox"), "formstest");
			Session.EnterText (c => c.Marked ("organizationTextBox"), "com.test");

			Session.ToggleElement (c => c.CheckButton ().Text ("Android"), true);
			Session.WaitForElement (c => c.CheckButton ().Text ("iOS").Property ("Active", true));

			Session.ToggleElement (c => c.CheckButton ().Text ("Use .NET Standard").Visibility (true), true);
			Session.WaitForElement (c => c.CheckButton ().Text ("Use .NET Standard").Visibility (true).Property ("Active", true));

			newProject.Next	();
		}

		readonly Func<AppQuery, AppQuery> templateCategoriesWidgetQuery = c => c.TreeView ().Marked ("templateCategoriesTreeView");
		readonly Func<AppQuery, AppQuery> templatesWidgetQuery = c => c.TreeView ().Marked ("templatesTreeView");
		readonly Func<AppQuery, AppQuery> templateCategoriesQuery = c => c.TreeView ().Marked ("templateCategoriesTreeView").Model ("templateCategoriesListStore__Name");
		readonly Func<AppQuery, AppQuery> templatesQuery = c => c.TreeView ().Marked ("templatesTreeView").Model ("templateListStore__Name");
	}
}
