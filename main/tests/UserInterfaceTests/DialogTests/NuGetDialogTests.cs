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

namespace UserInterfaceTests
{
	[TestFixture]
	[Category ("Dialog")]
	[Category ("PackagesDialog")]
	public class NuGetDialogTests : CreateBuildTemplatesTestBase
	{
		[Test]
		public void AddPackagesTest ()
		{
			CreateProject ();
			NuGetController.AddPackage (new NuGetPackageOptions {
				PackageName = "CommandLineParser",
				Version = "2.0.85-alpha",
				IsPreRelease = true
			});
		}

		[Test]
		public void TestReadmeTxtOpens ()
		{
			CreateProject ();
			NuGetController.AddPackage (new NuGetPackageOptions {
				PackageName = "RestSharp",
				Version = "105.0.1",
				IsPreRelease = true
			});
			Session.WaitForElement (c => c.Window ().Marked ("MonoDevelop.Ide.Gui.DefaultWorkbench").Property ("TabControl.CurrentTab.Text", "readme.txt"));
		}

		[Test, Category ("NuGetUpgrade")]
		public void TestReadmeTxtUpgradeOpens ()
		{
			CreateProject ();
			NuGetController.AddPackage (new NuGetPackageOptions {
				PackageName = "RestSharp",
				Version = "105.0.1",
				IsPreRelease = true
			}, TakeScreenShot);
			Session.WaitForElement (c => c.Window ().Marked ("MonoDevelop.Ide.Gui.DefaultWorkbench").Property ("TabControl.CurrentTab.Text", "readme.txt"));
			Session.ExecuteCommand (MonoDevelop.Ide.Commands.FileCommands.CloseFile);
			Session.WaitForElement (IdeQuery.TextArea);
			NuGetController.UpdatePackage (new NuGetPackageOptions {
				PackageName = "RestSharp",
				Version = "105.1.0",
				IsPreRelease = true
			}, TakeScreenShot);
			Session.WaitForElement (c => c.Window ().Marked ("MonoDevelop.Ide.Gui.DefaultWorkbench").Property ("TabControl.CurrentTab.Text", "readme.txt"));
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
	}
}

