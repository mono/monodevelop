//
// TemplateTestOptions.cs
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

namespace MonoDevelop.UserInterfaceTesting
{
	[Flags]
	public enum Platforms
	{
		None = 0x0,
		IOS = 0x1,
		Android = 0x2
	}

	public enum BeforeBuildAction
	{
		None,
		WaitForPackageUpdate,
		WaitForSolutionCheckedOut
	}

	public enum CodeSharingType
	{
		PortableClassLibrary,
		SharedProject
	}

	public class TemplateSelectionOptions
	{
		public string CategoryRoot { get; set; }

		public string Category { get; set; }

		public string TemplateKindRoot { get; set; }

		public string TemplateKind { get; set; }

		public override string ToString ()
		{
			return string.Format ("CategoryRoot={0}, Category={1}, TemplateKindRoot={2}, TemplateKind={3}",
				CategoryRoot, Category, TemplateKindRoot, TemplateKind);
		}
	}

	public class GitOptions
	{
		public GitOptions ()
		{
			UseGit = true;
			UseGitIgnore = true;
		}

		public bool UseGit { get; set; }

		public bool UseGitIgnore { get; set; }

		public override string ToString ()
		{
			return string.Format ("UseGit={0}, UseGitIgnore={1}", UseGit, UseGitIgnore);
		}
	}

	public class ProjectDetails
	{
		public ProjectDetails ()
		{
			SolutionLocation = Util.CreateTmpDir ();
			ProjectInSolution = true;
			BuildTimeout = TimeSpan.FromSeconds (180);
		}

		public ProjectDetails (TemplateSelectionOptions templateData) : this ()
		{
			ProjectName = CreateBuildTemplatesTestBase.GenerateProjectName (templateData.TemplateKind);
			SolutionName = ProjectName;
		}

		public static ProjectDetails ToExistingSolution (string solutionName, string projectName)
		{
			return new ProjectDetails  {
				ProjectName = projectName,
				SolutionName = solutionName,
				AddProjectToExistingSolution = true,
				SolutionLocation = null
			};
		}

		public string ProjectName { get; set; }

		public string SolutionName { get; set; }

		public string SolutionLocation { get; set; }

		public bool ProjectInSolution { get; set; }

		public bool AddProjectToExistingSolution { get; set; }

		public TimeSpan BuildTimeout { get; set; }

		public override string ToString ()
		{
			return string.Format ("ProjectName={0}, SolutionName={1}, SolutionLocation={2}, ProjectInSolution={3}, AddProjectToExistingSolution={4}",
				ProjectName, SolutionName, SolutionLocation, ProjectInSolution, AddProjectToExistingSolution);
		}
	}

	public class NewFileOptions
	{
		public string FileName { get; set; }

		public string FileType { get; set; }

		public string FileTypeCategory { get; set; }

		public string FileTypeCategoryRoot { get; set; }

		public string AddToProjectName { get; set; }

		public override string ToString ()
		{
			return string.Format ("FileName={0}, FileType={1}, FileTypeCategory={2}, FileTypeCategoryRoot={3}, AddToProjectName={4}",
				FileName, FileType, FileTypeCategory, FileTypeCategoryRoot, AddToProjectName);
		}
	}
}

