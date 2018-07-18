//
// CreateBuildTemplatesTestBase.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
//		 Manish Sinha <manish.sinha@xamarin.com>
//
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using MonoDevelop.Components.AutoTest;
using MonoDevelop.UserInterfaceTesting.Controllers;
using NUnit.Framework;

namespace MonoDevelop.UserInterfaceTesting
{
	public abstract class CreateBuildTemplatesTestBase: UITestBase
	{
		public string GeneralKindRoot { get { return "General"; } }

		public string OtherCategoryRoot { get { return "Other"; } }

		public readonly static Action EmptyAction = Ide.EmptyAction;

		static Regex cleanSpecialChars = new Regex ("[^0-9a-zA-Z]+", RegexOptions.Compiled);

		public readonly static Action WaitForPackageUpdate = Ide.WaitForPackageUpdate;

		protected CreateBuildTemplatesTestBase (string mdBinPath = null) : base (mdBinPath)
		{
		}

		public static string GenerateProjectName (string templateName)
		{
			return cleanSpecialChars.Replace (templateName, string.Empty);
		}

		public void AssertExeHasOutput (string exe, string expectedOutput)
		{
			var sw = new StringWriter ();
			var p = ProcessUtils.StartProcess (new ProcessStartInfo (exe), sw, sw, CancellationToken.None);
			Assert.AreEqual (0, p.Result);
			string output = sw.ToString ();

			Assert.AreEqual (expectedOutput, output.Trim ());
		}

		public void CreateBuildProject (TemplateSelectionOptions templateOptions, Action beforeBuild,
			GitOptions gitOptions = null, object miscOptions = null)
		{
			var projectName = GenerateProjectName (templateOptions.TemplateKind);
			var projectDetails = new ProjectDetails {
				ProjectName = projectName,
				SolutionName = projectName,
				SolutionLocation = Util.CreateTmpDir (),
				ProjectInSolution = true
			};
			CreateBuildProject (templateOptions, projectDetails, beforeBuild, gitOptions, miscOptions);
		}

		public void CreateBuildProject (TemplateSelectionOptions templateOptions, ProjectDetails projectDetails,
			Action beforeBuild, GitOptions gitOptions = null, object miscOptions = null)
		{
			try {
				CreateProject (templateOptions, projectDetails, gitOptions, miscOptions);

				try {
					beforeBuild ();
					TakeScreenShot ("BeforeBuild");
				} catch (TimeoutException e) {
					TakeScreenShot ("BeforeBuildActionFailed");
					Assert.Fail (e.ToString ());
				}
				OnBuildTemplate ((int)projectDetails.BuildTimeout.TotalSeconds);
			} catch (Exception e) {
				TakeScreenShot ("TestFailedWithGenericException");
				Assert.Fail (e.ToString ());
			} finally {
				FoldersToClean.Add (projectDetails.SolutionLocation);
			}
		}

		public void CreateProject (TemplateSelectionOptions templateOptions,
			ProjectDetails projectDetails, GitOptions gitOptions = null, object miscOptions = null)
		{
			PrintToTestRunner (templateOptions, projectDetails, gitOptions, miscOptions);
			ReproStep ("Create a new project", templateOptions, projectDetails, gitOptions, miscOptions);
			var newProject = new NewProjectController ();

			if (projectDetails.AddProjectToExistingSolution)
				newProject.Open (projectDetails.SolutionName);
			else
				newProject.Open ();
			TakeScreenShot ("Open");

			OnSelectTemplate (newProject, templateOptions);

			OnEnterTemplateSpecificOptions (newProject, projectDetails.ProjectName, miscOptions);
			
			OnEnterProjectDetails (newProject, projectDetails, gitOptions, miscOptions);

			OnClickCreate (newProject, projectDetails);

			FoldersToClean.Add (projectDetails.SolutionLocation);
		}

		protected virtual void OnSelectTemplate (NewProjectController newProject, TemplateSelectionOptions templateOptions)
		{
			if (!newProject.SelectTemplateType (templateOptions.CategoryRoot, templateOptions.Category)) {
				throw new TemplateSelectionException (string.Format ("Failed to select Category '{0}' under '{1}'", 
					templateOptions.Category, templateOptions.CategoryRoot));
			}
			TakeScreenShot ("TemplateCategorySelected");

			if (!newProject.SelectTemplate (templateOptions.TemplateKindRoot, templateOptions.TemplateKind)) {
				throw new TemplateSelectionException (string.Format ("Failed to select Template '{0}' under '{1}'", 
					templateOptions.TemplateKind, templateOptions.TemplateKindRoot));
			}
			TakeScreenShot ("TemplateSelected");

			if (!newProject.Next ()) {
				throw new TemplateSelectionException ("Clicking Next failed after selecting template");
			}
			TakeScreenShot ("NextAfterTemplateSelected");
		}

		protected virtual void OnEnterTemplateSpecificOptions (NewProjectController newProject, string projectName, object miscOptions) {}

		protected virtual void OnEnterProjectDetails (NewProjectController newProject, ProjectDetails projectDetails,
			GitOptions gitOptions = null, object miscOptions = null)
		{
			if (!newProject.SetProjectName (projectDetails.ProjectName, projectDetails.AddProjectToExistingSolution)) {
				throw new CreateProjectException (string.Format ("Failed at entering ProjectName as '{0}'", projectDetails.ProjectName));
			}

			if (!string.IsNullOrEmpty (projectDetails.SolutionName)) {
				if (!newProject.SetSolutionName (projectDetails.SolutionName, projectDetails.AddProjectToExistingSolution)) {
					throw new CreateProjectException (string.Format ("Failed at entering SolutionName as '{0}'", projectDetails.SolutionName));
				}
			}

			if (!string.IsNullOrEmpty (projectDetails.SolutionLocation)) {
				if (!newProject.SetSolutionLocation (projectDetails.SolutionLocation)) {
					throw new CreateProjectException (string.Format ("Failed at entering SolutionLocation as '{0}'", projectDetails.SolutionLocation));
				}
			}

			if (!newProject.CreateProjectInSolutionDirectory (projectDetails.ProjectInSolution)) {
				throw new CreateProjectException (string.Format ("Failed at entering ProjectInSolution as '{0}'", projectDetails.ProjectInSolution));
			}

			if (gitOptions != null && !projectDetails.AddProjectToExistingSolution) {
				if (!newProject.UseGit (gitOptions)) {
					throw new CreateProjectException (string.Format ("Failed at setting Git as - '{0}'", gitOptions));
				}
			}

			TakeScreenShot ("AfterProjectDetailsFilled");
		}

		protected virtual void OnClickCreate (NewProjectController newProject, ProjectDetails projectDetails)
		{
			if (projectDetails.AddProjectToExistingSolution)
				newProject.Create ();
			else
				Session.RunAndWaitForTimer (() => newProject.Create (), "Ide.Shell.SolutionOpened");
		}

		protected virtual void OnBuildTemplate (int buildTimeoutInSecs = 180)
		{
			ReproStep ("Build solution");
			try {
				Assert.IsTrue (Ide.BuildSolution (timeoutInSecs : buildTimeoutInSecs), "Build Failed");
				TakeScreenShot ("AfterBuildFinishedSuccessfully");
			} catch (TimeoutException e) {
				Session.DebugObject.Debug ("Build Failed");
				ReproStep (string.Format ("Expected: Build should finish within '{0}' seconds\nActual: Build timed out", buildTimeoutInSecs));
				TakeScreenShot ("AfterBuildFailed");
				Assert.Fail (e.ToString ());
			}
		}

		protected void IsTemplateSelected (TemplateSelectionOptions templateOptions, string addToExistingSolution = null)
		{
//			var newProject = new NewProjectController ();
//			try {
//				newProject.WaitForOpen ();
//			} catch (TimeoutException) {
//				if (!string.IsNullOrEmpty (addToExistingSolution))
//					newProject.Open (addToExistingSolution);
//				else
//					newProject.Open ();
//			}
//			newProject.IsSelected (templateOptions);
		}

		protected void WaitForElement (Func<AppQuery, AppQuery> query, string expected, string actual, int timeoutInSecs = 5)
		{
			try {
				Session.WaitForElement (query, timeoutInSecs * 1000);
			} catch (TimeoutException) {
				ReproStep (expected, actual);
				throw;
			}
		}

		protected void WaitForElement (Action action, string expected, string actual)
		{
			try {
				action ();
			} catch (TimeoutException) {
				ReproStep (string.Format ("Expected: {0}\nActual:{1}", expected, actual));
				throw;
			}
		}

		void PrintToTestRunner (TemplateSelectionOptions templateOptions,
			ProjectDetails projectDetails, GitOptions gitOptions, object miscOptions)
		{
			templateOptions.PrintData ();
			projectDetails.PrintData ();
			gitOptions.PrintData ();
			miscOptions.PrintData ();
		}
	}
}
