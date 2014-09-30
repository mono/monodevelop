//
// FinalProjectConfigurationPage.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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

using MonoDevelop.Ide.Templates;
using SolutionFolder = MonoDevelop.Projects.SolutionFolder;

namespace MonoDevelop.Ide.Projects
{
	public class FinalProjectConfigurationPage
	{
		ProjectConfiguration config;

		public FinalProjectConfigurationPage (ProjectConfiguration config)
		{
			this.config = config;
		}

		public SolutionFolder ParentFolder { get; set; }

		public string Location {
			get { return config.Location; }
			set { config.Location = value; }
		}

		public string ProjectName {
			get { return config.ProjectName; }
			set { config.ProjectName = value; }
		}

		public string SolutionName {
			get {
				if (ParentFolder != null) {
					return ParentFolder.Name;
				}
				return config.SolutionName;
			}
			set { config.SolutionName = value; }
		}

		public string ProjectFileName {
			get { return config.ProjectFileName; }
		}

		public string SolutionFileName {
			get { return config.SolutionFileName; }
		}

		public string GetValidProjectName ()
		{
			return config.GetValidProjectName ();
		}

		public string GetValidSolutionName ()
		{
			if (ParentFolder != null) {
				return ParentFolder.Name;
			}
			return config.GetValidSolutionName ();
		}

		public bool CreateGitIgnoreFile {
			get { return config.CreateGitIgnoreFile; }
			set { config.CreateGitIgnoreFile = value; }
		}

		public bool CreateProjectDirectoryInsideSolutionDirectory {
			get { return config.CreateProjectDirectoryInsideSolutionDirectory; }
			set { config.CreateProjectDirectoryInsideSolutionDirectory = value; }
		}

		public bool IsProjectNameEnabled {
			get { return true; }
		}

		public bool IsSolutionNameEnabled {
			get { return config.CreateSolution; }
		}

		public bool IsGitIgnoreEnabled {
			get { return config.CreateSolution; }
		}

		public bool IsUseGitEnabled {
			get { return config.CreateSolution; }
		}
	}
}

