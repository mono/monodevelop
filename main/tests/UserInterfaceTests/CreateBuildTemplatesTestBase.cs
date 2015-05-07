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

		public string ScreenshotsPath { get; private set; }

		public readonly static Action EmptyAction = () => { };

		public readonly static Action WaitForPackageUpdate = delegate {
			Ide.WaitUntil (() => Ide.GetStatusMessage () == "Package updates are available.",
				pollStep: 1000, timeout: 120000);
		};

		static Regex cleanSpecialChars = new Regex ("[^0-9a-zA-Z]+", RegexOptions.Compiled);

		string projectScreenshotFolder;

		protected CreateBuildTemplatesTestBase (string folderName)
		{
			Init (folderName);
		}

		protected CreateBuildTemplatesTestBase (string mdBinPath, string folderName) : base (mdBinPath)
		{
			Init (folderName);
		}

		void Init (string folderName)
		{
			var pictureFolderName = Environment.GetFolderPath (Environment.SpecialFolder.MyPictures);
			folderName = folderName ?? DateTime.Now.ToLongDateString ().Replace (",", "").Replace (' ', '-');
			ScreenshotsPath = Path.Combine (pictureFolderName, "XamarinStudioUITests", folderName, GetType ().Name);
			if (Directory.Exists (ScreenshotsPath))
				Directory.Delete (ScreenshotsPath, true);
			Directory.CreateDirectory (ScreenshotsPath);
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

			SetUpTemplateScreenshot (projectName);
			var solutionParentDirectory = Util.CreateTmpDir (projectName);
			try {
				var newProject = new NewProjectController ();
				newProject.Open ();
				TakeScreenShot ("Open");

				OnSelectTemplate (newProject, templateOptions);

				OnEnterTemplateSpecificOptions (newProject, projectName, miscOptions);

				OnEnterProjectDetails (newProject, projectName, projectName, solutionParentDirectory, gitOptions);

				try {
					beforeBuild ();
					TakeScreenShot ("BeforeBuild");
				} catch (TimeoutException e) {
					TakeScreenShot ("BeforeBuildActionFailed");
					Assert.Fail (e.ToString ());
				}

				OnBuildTemplate ();
			} catch (Exception e) {
				Assert.Fail (e.StackTrace);
			} finally {
				var actualSolutionDirectory = GetSolutionDirectory ();
				Ide.CloseAll ();
				try {
					if (Directory.Exists (actualSolutionDirectory))
						Directory.Delete (actualSolutionDirectory, true);
				} catch (IOException) { }
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
			string solutionName, string solutionLocation, GitOptions gitOptions = null)
		{
			Assert.IsTrue (newProject.SetProjectName (projectName));

			if (!string.IsNullOrEmpty (solutionName)) {
				Assert.IsTrue (newProject.SetSolutionName (solutionName));
			}

			if (!string.IsNullOrEmpty (solutionLocation)) {
				Assert.IsTrue (newProject.SetSolutionLocation (solutionLocation));
			}

			Assert.IsTrue (newProject.CreateProjectInSolutionDirectory (true));

			if (gitOptions != null)
				Assert.IsTrue (newProject.UseGit (gitOptions));

			TakeScreenShot ("AfterProjectDetailsFilled");

			Session.RunAndWaitForTimer (() => newProject.Next(), "Ide.Shell.SolutionOpened");
		}

		protected virtual void OnBuildTemplate ()
		{
			try {
				Assert.IsTrue (Ide.BuildSolution ());
				TakeScreenShot ("AfterBuildFinishedSuccessfully");
			} catch (TimeoutException e) {
				TakeScreenShot ("AfterBuildFailed");
				Assert.Fail (e.ToString ());
			}
		}

		protected string GetSolutionDirectory ()
		{
			return Session.GetGlobalValue ("MonoDevelop.Ide.IdeApp.ProjectOperations.CurrentSelectedSolution.RootFolder.BaseDirectory").ToString ();
		}

		void SetUpTemplateScreenshot (string projectName)
		{
			projectScreenshotFolder = Path.Combine (ScreenshotsPath, projectName);
			if (Directory.Exists (projectScreenshotFolder))
				Directory.Delete (projectScreenshotFolder, true);
			Directory.CreateDirectory (projectScreenshotFolder);
		}

		protected void TakeScreenShot (string stepName)
		{
			var screenshotPath = Path.Combine (projectScreenshotFolder, stepName) + ".png";
			Session.TakeScreenshot (screenshotPath);
		}
	}
}
