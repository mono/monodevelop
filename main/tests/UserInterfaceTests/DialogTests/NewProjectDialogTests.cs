//
// NewProjectDialogTests.cs
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
using System;
using NUnit.Framework;

namespace UserInterfaceTests
{
	[TestFixture]
	[Category ("Dialog")]
	[Category ("ProjectDialog")]
	public class NewProjectDialogTests : CreateBuildTemplatesTestBase
	{
		readonly TemplateSelectionOptions templateOptions = new TemplateSelectionOptions {
			CategoryRoot = "Other",
			Category = ".NET",
			TemplateKindRoot = "General",
			TemplateKind = "Console Project"
		};

		readonly string projectName = "ConsoleProject";
		readonly string solutionName = "ConsoleSolution";

		readonly NewProjectController ctrl = new NewProjectController ();

		readonly string solutionLocation = Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments);

		[Test]
		[TestCase (true, true, true, TestName = "WithGit--WithGitIgnore--WithProjectWithinSolution")]
		[TestCase (true, true, false, TestName = "WithGit--WithGitIgnore--WithoutProjectWithinSolution")]
		[TestCase (true, true, true, TestName = "WithGit--WithGitIgnore--WithProjectWithinSolution")]
		[TestCase (false, false, false, TestName = "WithoutGit--WithoutGitIgnore--WithoutProjectWithinSolution")]
		[TestCase (true, false, true, TestName = "WithGit--WithoutGitIgnore--WithProjectWithinSolution")]
		[TestCase (false, true, false, TestName = "WithoutGit--WithGitIgnore--WithoutProjectWithinSolution")]
		[TestCase (false, true, true, TestName = "WithoutGit--WithGitIgnore--WithProjectWithinSolution")]
		[TestCase (true, false, false, TestName = "WithGit--WithoutGitIgnore--WithoutProjectWithinSolution")]
		public void TestFolderPreview (bool useGit, bool useGitIgnore, bool projectWithinSolution)
		{
			TestFolderPreview (new GitOptions {
				UseGit = useGit,
				UseGitIgnore = useGitIgnore
			}, projectWithinSolution);
		}

		void TestFolderPreview (GitOptions gitOptions, bool projectWithinSolution)
		{
			var projectDetails = new ProjectDetails {
				ProjectName = projectName,
				SolutionName = solutionName,
				SolutionLocation = solutionLocation,
				ProjectInSolution = projectWithinSolution
			};

			ctrl.Open ();
			OnSelectTemplate (ctrl, templateOptions);
			OnEnterProjectDetails (ctrl, projectDetails, gitOptions);
			ctrl.ValidatePreviewTree (projectDetails, gitOptions);
			ctrl.Close ();
		}
	}
}
