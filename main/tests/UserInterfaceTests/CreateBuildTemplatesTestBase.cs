//
// SimpleTest.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
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

using MonoDevelop.Core;
using NUnit.Framework;

namespace UserInterfaceTests
{
	public abstract class CreateBuildTemplatesTestBase: UITestBase
	{
		public string GeneralKindRoot { get { return "General"; } }

		public string OtherCategoryRoot { get { return "Other"; } }

		public readonly static Action EmptyAction = delegate { };

		static Regex cleanSpecialChars = new Regex ("[^0-9a-zA-Z]+", RegexOptions.Compiled);

		public readonly static Action WaitForPackageUpdate = Ide.WaitForPackageUpdate;

		protected CreateBuildTemplatesTestBase (string mdBinPath = null) : base (mdBinPath)
		{
		}

		public string GenerateProjectName (string templateName)
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
			var templateName = templateOptions.TemplateKind;
			var projectName = !string.IsNullOrEmpty (templateOptions.ProjectName) ? templateOptions.ProjectName: GenerateProjectName (templateName);

			var solutionParentDirectory = Util.CreateTmpDir (projectName);
			try {
				var newProject = new NewProjectController ();
				newProject.Open ();
				TakeScreenShot ("Open");

				OnSelectTemplate (newProject, templateOptions);

				OnEnterTemplateSpecificOptions (newProject, projectName, miscOptions);

				OnEnterProjectDetails (newProject, projectName, projectName, solutionParentDirectory, true, gitOptions);

				OnClickCreate (newProject);

				try {
					beforeBuild ();
					TakeScreenShot ("BeforeBuild");
				} catch (TimeoutException e) {
					TakeScreenShot ("BeforeBuildActionFailed");
					Assert.Fail (e.ToString ());
				}

				OnBuildTemplate ();
			} catch (Exception e) {
				TakeScreenShot ("TestFailedWithGenericException");
				Assert.Fail (e.ToString ());
			}
		}

		protected virtual void OnSelectTemplate (NewProjectController newProject, TemplateSelectionOptions templateOptions)
		{
			Assert.IsTrue (newProject.SelectTemplateType (templateOptions.CategoryRoot, templateOptions.Category));
			TakeScreenShot ("TemplateCategorySelected");
			Assert.IsTrue (newProject.SelectTemplate (templateOptions.TemplateKindRoot, templateOptions.TemplateKind));
			TakeScreenShot ("TemplateSelected");
			Assert.IsTrue (newProject.Next ());
			TakeScreenShot ("NextAfterTemplateSelected");
		}

		protected virtual void OnEnterTemplateSpecificOptions (NewProjectController newProject, string projectName, object miscOptions) {}

		protected virtual void OnEnterProjectDetails (NewProjectController newProject, string projectName,
			string solutionName, string solutionLocation, bool createProjectInSolution = true, GitOptions gitOptions = null)
		{
			Assert.IsTrue (newProject.SetProjectName (projectName));

			if (!string.IsNullOrEmpty (solutionName)) {
				Assert.IsTrue (newProject.SetSolutionName (solutionName));
			}

			if (!string.IsNullOrEmpty (solutionLocation)) {
				Assert.IsTrue (newProject.SetSolutionLocation (solutionLocation));
			}

			Assert.IsTrue (newProject.CreateProjectInSolutionDirectory (createProjectInSolution));

			if (gitOptions != null)
				Assert.IsTrue (newProject.UseGit (gitOptions));

			TakeScreenShot ("AfterProjectDetailsFilled");
		}

		protected virtual void OnClickCreate (NewProjectController newProject)
		{
			Session.RunAndWaitForTimer (() => newProject.Next(), "Ide.Shell.SolutionOpened");
		}

		protected virtual void OnBuildTemplate (int buildTimeoutInSecs = 180)
		{
			try {
				Assert.IsTrue (Ide.BuildSolution (timeoutInSecs : buildTimeoutInSecs));
				TakeScreenShot ("AfterBuildFinishedSuccessfully");
			} catch (TimeoutException e) {
				TakeScreenShot ("AfterBuildFailed");
				Assert.Fail (e.ToString ());
			}
		}
	}
}
