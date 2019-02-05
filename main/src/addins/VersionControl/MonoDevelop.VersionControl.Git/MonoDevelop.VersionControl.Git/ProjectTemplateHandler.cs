//
// ProjectTemplateHandler.cs
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

using System.IO;
using LibGit2Sharp;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Core;

namespace MonoDevelop.VersionControl.Git
{
	public class ProjectTemplateHandler : IVersionControlProjectTemplateHandler
	{
		public void Run (NewProjectConfiguration config)
		{
			if (config.UseGit) {
				if (config.CreateGitIgnoreFile) {
					CreateGitIgnoreFile (config.SolutionLocation);
				}

				CreateGitRepository (config.SolutionLocation);
			}
		}

		void CreateGitIgnoreFile (FilePath solutionPath)
		{
			FilePath gitIgnoreFilePath = solutionPath.Combine (".gitignore");
			if (!File.Exists (gitIgnoreFilePath)) {
				FilePath sourceGitIgnoreFilePath = GetSourceGitIgnoreFilePath ();
				File.Copy (sourceGitIgnoreFilePath, gitIgnoreFilePath);
			}
		}

		FilePath GetSourceGitIgnoreFilePath ()
		{
			string directory = Path.GetDirectoryName (typeof(ProjectTemplateHandler).Assembly.Location);
			return FilePath.Build (directory, "GitIgnore.txt");
		}

		void CreateGitRepository (FilePath solutionPath)
		{
			using (var repo = GitUtil.Init (solutionPath, null))
				LibGit2Sharp.Commands.Stage (repo ,"*");
		}
	}
}

