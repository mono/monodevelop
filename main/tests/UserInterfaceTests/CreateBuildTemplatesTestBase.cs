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

		public readonly static Action EmptyAction = () => { };

		public readonly static Action WaitForPackageUpdate = delegate {
			Ide.WaitUntil (() => Ide.GetStatusMessage () == "Package updates are available.",
				pollStep: 1000, timeout: 30000);
		};

		static Regex cleanSpecialChars = new Regex ("[^0-9a-zA-Z]+", RegexOptions.Compiled);

		public CreateBuildTemplatesTestBase () {}

		public CreateBuildTemplatesTestBase (string mdBinPath) : base (mdBinPath) {}

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

		public void CreateBuildProject (string projectName, string categoryRoot, string category, string kindRoot, string kind, Action beforeBuild)
		{
			var solutionParentDirectory = Util.CreateTmpDir (projectName);
			string actualSolutionDirectory = string.Empty;
			try {
				var newProject = new NewProjectController ();
				newProject.Open ();

				SelectTemplate (newProject, categoryRoot, category, kindRoot, kind);

				Assert.IsTrue (newProject.Next ());

				EnterProjectDetails (newProject, projectName, projectName, solutionParentDirectory);

				Assert.IsTrue (newProject.CreateProjectInSolutionDirectory (false));
				Assert.IsTrue (newProject.UseGit (true, false));

				Session.RunAndWaitForTimer (() => newProject.Next(), "Ide.Shell.SolutionOpened");

				actualSolutionDirectory = GetSolutionDirectory ();

				beforeBuild ();

				Assert.IsTrue (Ide.BuildSolution ());
			} finally {
				Ide.CloseAll ();
				try {
					if (Directory.Exists (actualSolutionDirectory))
						Directory.Delete (actualSolutionDirectory, true);
				} catch (IOException) { }
			}
		}

		public void SelectTemplate (NewProjectController newProject, string categoryRoot, string category, string kindRoot, string kind)
		{
			Assert.IsTrue (newProject.SelectTemplateType (categoryRoot, category));
			Assert.IsTrue (newProject.SelectTemplate (kindRoot, kind));
		}

		public void EnterProjectDetails (NewProjectController newProject, string projectName, string solutionName, string solutionLocation)
		{
			Assert.IsTrue (newProject.SetProjectName (projectName));

			if (!string.IsNullOrEmpty (solutionName)) {
				Assert.IsTrue (newProject.SetSolutionName (solutionName));
			}

			if (!string.IsNullOrEmpty (solutionLocation)) {
				Assert.IsTrue (newProject.SetSolutionLocation (solutionLocation));
			}
		}

		public string GetSolutionDirectory ()
		{
			return Session.GetGlobalValue ("MonoDevelop.Ide.IdeApp.ProjectOperations.CurrentSelectedSolution.RootFolder.BaseDirectory").ToString ();
		}
	}
}