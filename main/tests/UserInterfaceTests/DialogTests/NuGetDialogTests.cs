//
// NuGetDialogTests.cs
//
// Author:
//       Manish Sinha <manish.sinha@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc.
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

using NUnit.Framework;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Core;
using System.IO;
using System.Xml;
using System;
using System.Collections.Generic;

namespace UserInterfaceTests
{
	[TestFixture, Timeout(60000)]
	[Category ("Dialog")]
	[Category ("NuGet")]
	[Category ("PackagesDialog")]
	public class NuGetDialogTests : CreateBuildTemplatesTestBase
	{
		[Test, Category("Smoke")]
		[Description ("Add a single NuGet Package")]
		public void AddPackagesTest ()
		{
			CreateProject ();
			NuGetController.AddPackage (new NuGetPackageOptions {
				PackageName = "CommandLineParser",
				Version = "2.0.257-beta",
				IsPreRelease = true
			}, this);
		}

		[Test, Timeout(300000)]
		[Description ("When a solution is opened and package updates are available, don't show in status bar")]
		public void DontShowPackageUpdatesAvailable ()
		{
			var templateOptions = new TemplateSelectionOptions {
				CategoryRoot = OtherCategoryRoot,
				Category = ".NET",
				TemplateKindRoot = GeneralKindRoot,
				TemplateKind = "Console Project"
			};
			var projectDetails = new ProjectDetails (templateOptions);
			CreateProject (templateOptions, projectDetails);
			NuGetController.AddPackage (new NuGetPackageOptions {
				PackageName = "CommandLineParser",
				Version = "1.9.3.34",
				IsPreRelease = false
			}, this);

			string solutionFolder = GetSolutionDirectory ();
			string solutionPath = Path.Combine (solutionFolder, projectDetails.SolutionName+".sln");
			var projectPath = Path.Combine (solutionFolder, projectDetails.ProjectName, projectDetails.ProjectName + ".csproj");
			Assert.IsTrue (File.Exists (projectPath));

			Workbench.CloseWorkspace (this);

			Workbench.OpenWorkspace (solutionPath, this);
			try {
				const string expected = "When a solution is opened and package updates are available, it don't show in status bar";
				ReproStep (expected);
				Ide.WaitForPackageUpdate ();
				var failureMessage = string.Format ("Expected: {0}\nActual: {1}",
					expected, "When a solution is opened and package updates are available, it shows in status bar");
				ReproStep (failureMessage);
				Assert.Fail (failureMessage);
			} catch (TimeoutException) {
				Session.DebugObject.Debug ("WaitForPackageUpdate throws TimeoutException as expected");
			}
			Ide.WaitForSolutionLoaded ();
			TakeScreenShot ("Solution-Ready");
		}

		[Test, Category("Smoke")]
		[Description ("Add a single NuGet Package and check if it's readme.txt opens")]
		public void TestReadmeTxtOpens ()
		{
			CreateProject ();
			NuGetController.AddPackage (new NuGetPackageOptions {
				PackageName = "RestSharp",
				Version = "105.2.3",
				IsPreRelease = true
			}, this);
			WaitForNuGetReadmeOpened ();
		}

		[Test]
		[Description ("Add a single NuGet Package. Check if readme.txt opens even when updating")]
		public void TestReadmeTxtUpgradeOpens ()
		{
			CreateProject ();
			NuGetController.AddPackage (new NuGetPackageOptions {
				PackageName = "RestSharp",
				Version = "105.2.2",
				IsPreRelease = true
			}, this);
			WaitForNuGetReadmeOpened ();

			Session.ExecuteCommand (FileCommands.CloseFile);
			Session.WaitForElement (IdeQuery.TextArea);
			TakeScreenShot ("About-To-Update-Package");
			NuGetController.UpdatePackage (new NuGetPackageOptions {
				PackageName = "RestSharp",
				Version = "105.2.3",
				IsPreRelease = true
			}, this);
			WaitForNuGetReadmeOpened ();
		}

		[Test, Category("Smoke")]
		[Timeout (90000)]
		[Description ("When readme.txt from a package has already been opened, adding same package to another project should not open readme.txt")]
		public void TestDontOpenReadmeOpenedInOther ()
		{
			var packageInfo = new NuGetPackageOptions {
				PackageName = "RestSharp",
				Version = "105.2.3",
				IsPreRelease = true
			};

			var projectDetails = CreateProject ();
			NuGetController.AddPackage (packageInfo, this);
			WaitForNuGetReadmeOpened ();
			Session.ExecuteCommand (FileCommands.CloseFile);

			var pclTemplateOptions = new TemplateSelectionOptions {
				CategoryRoot = "Other",
				Category = ".NET",
				TemplateKindRoot = "General",
				TemplateKind = "Library"
			};
			var pclProjectDetails = ProjectDetails.ToExistingSolution (projectDetails.SolutionName,
				GenerateProjectName (pclTemplateOptions.TemplateKind));
			CreateProject (pclTemplateOptions, pclProjectDetails);
			Ide.WaitForIdeIdle (30);

			SolutionExplorerController.SelectProject (projectDetails.SolutionName, pclProjectDetails.ProjectName);
			NuGetController.AddPackage (packageInfo, this);
			try {
				WaitForNuGetReadmeOpened (false);
				var failureMessage = string.Format ("Expected: {0}\nActual:{1}",
					"readme.txt tab should not open", "readme.txt tab opens");
				ReproStep (failureMessage);
				Assert.Fail (failureMessage);
			} catch (TimeoutException) {
				Session.DebugObject.Debug ("readme.txt tab failed to open as expected");
			}
		}

		[Test]
		[Description ("Add a package with powershell scripts and assert that Xamarin Studio doesn't report warnings "+
			"when trying to add powershell scripts to Xamarin Studio")]
		public void TestDontShowWarningWithPowerShellScripts ()
		{
			CreateProject ();
			NuGetController.AddPackage (new NuGetPackageOptions {
				PackageName = "Newtonsoft.Json",
			}, this);
			WaitForNuGet.Success ("Newtonsoft.Json", NuGetOperations.Add, false, this);
			TakeScreenShot ("NewtonSoftJson-Package-Added-Without-Warning");
		}

		[Test, Timeout (300000)]
		[Description ("When a NuGet package is updated, the 'Local Copy' value should be preserved")]
		public void TestLocalCopyPreservedUpdate ()
		{
			var templateOptions = new TemplateSelectionOptions {
				CategoryRoot = OtherCategoryRoot,
				Category = ".NET",
				TemplateKindRoot = GeneralKindRoot,
				TemplateKind = "Console Project"
			};
			var projectDetails = new ProjectDetails (templateOptions);
			CreateProject (templateOptions, projectDetails);
			NuGetController.AddPackage (new NuGetPackageOptions {
				PackageName = "CommandLineParser",
				Version = "1.9.71",
				IsPreRelease = false
			}, this);

			string solutionFolder = GetSolutionDirectory ();
			string solutionPath = Path.Combine (solutionFolder, projectDetails.SolutionName+".sln");
			var projectPath = Path.Combine (solutionFolder, projectDetails.ProjectName, projectDetails.ProjectName + ".csproj");
			Assert.IsTrue (File.Exists (projectPath));

			ReproStep ("Check 'Local Copy' on CommandLine package under References");

			Workbench.CloseWorkspace (this);

			AddOrCheckLocalCopy (projectPath, true);

			Workbench.OpenWorkspace (solutionPath, this);

			NuGetController.UpdateAllNuGetPackages (this);

			ReproStep ("Check if CommandLine package under References has 'Local Copy' checked");
			AddOrCheckLocalCopy (projectPath, false);
		}

		void AddOrCheckLocalCopy (string projectPath, bool addLocalCopy)
		{
			using (var stream = new FileStream (projectPath, FileMode.Open, FileAccess.ReadWrite)) {
				var xmlDoc = new XmlDocument();
				xmlDoc.Load(stream);
				var ns = "http://schemas.microsoft.com/developer/msbuild/2003";
				XmlNamespaceManager xnManager = new XmlNamespaceManager(xmlDoc.NameTable);
				xnManager.AddNamespace("ui", ns);
				XmlNode root = xmlDoc.DocumentElement; 
				var uitest = root.SelectSingleNode ("//ui:Reference[@Include=\"CommandLine\"]", xnManager);
				Assert.IsNotNull (uitest, "Cannot find CommandLine package reference in file: "+projectPath);
				var privateUITestNode = uitest.SelectSingleNode ("./ui:Private", xnManager);

				if (addLocalCopy) {
					Assert.IsNull (privateUITestNode, uitest.InnerXml, "CommandLine package is already set to 'No Local Copy'");
					var privateNode = xmlDoc.CreateElement ("Private", ns);
					privateNode.InnerText = "False";
					uitest.AppendChild (privateNode);
					stream.SetLength (0);
					xmlDoc.Save (stream);
					stream.Flush ();
				} else {
					Assert.IsNotNull (privateUITestNode, "Cannot find CommandLine package with 'No Local Copy' set");
					Assert.AreEqual (privateUITestNode.InnerText, "False");
				}
				stream.Close ();
			}
		}


		ProjectDetails CreateProject (TemplateSelectionOptions templateOptions = null, ProjectDetails projectDetails = null)
		{
			templateOptions = templateOptions ?? new TemplateSelectionOptions {
				CategoryRoot = OtherCategoryRoot,
				Category = ".NET",
				TemplateKindRoot = GeneralKindRoot,
				TemplateKind = "Console Project"
			};
			projectDetails = projectDetails ?? new ProjectDetails (templateOptions);
			CreateProject (templateOptions,
					projectDetails,
				new GitOptions { UseGit = true, UseGitIgnore = true});
			Session.WaitForElement (IdeQuery.TextArea);
			FoldersToClean.Add (projectDetails.SolutionLocation);
			return projectDetails;
		}

		void WaitForNuGetReadmeOpened (bool expectSuccess = true)
		{
			ReproStep ("Wait for tab with readme.txt to be opened");
			try {
				Session.WaitForElement (c => c.Window ().Marked ("MonoDevelop.Ide.Gui.DefaultWorkbench").Property ("TabControl.CurrentTab.Text", "readme.txt"));
			} catch (TimeoutException) {
				if (expectSuccess) {
					ReproStep (string.Format ("Expected: {0}\nActual: {1}",
						"readme.txt tab should open", "readm.txt tab did not open"));
				}
				throw;
			}
		}
	}
}

