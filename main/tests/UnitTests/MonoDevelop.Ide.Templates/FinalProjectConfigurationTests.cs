//
// FinalProjectConfigurationTests.cs
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

using NUnit.Framework;
using System.IO;

namespace MonoDevelop.Ide.Templates
{
	[TestFixture]
	public class FinalProjectConfigurationTests
	{
		ProjectConfiguration config;

		void CreateProjectConfig (string location)
		{
			config = new ProjectConfiguration {
				Location = ToNativePath (location)
			};
		}

		/// <summary>
		/// Converts a Windows path to a native path by converting any
		/// directory separators.
		/// </summary>
		static string ToNativePath (string filePath)
		{
			if (Path.DirectorySeparatorChar == '\\')
				return filePath;

			if (filePath.Contains (":")) {
				filePath = filePath.Replace (":", "_drive");
				filePath = "/" + filePath;
			}

			return filePath.Replace ('\\', Path.DirectorySeparatorChar);
		}

		void AssertPathsAreEqual (string expected, string actual)
		{
			Assert.AreEqual (ToNativePath (expected), actual);
		}

		[Test]
		public void NewSolutionAndCreateProjectDirectory ()
		{
			CreateProjectConfig (@"d:\projects");
			config.ProjectName = "MyProject";
			config.SolutionName = "MySolution";
			config.CreateProjectDirectoryInsideSolutionDirectory = true;
			config.CreateSolution = true;

			AssertPathsAreEqual (@"d:\projects\MySolution\MyProject", config.ProjectLocation);
			AssertPathsAreEqual (@"d:\projects\MySolution", config.SolutionLocation);
		}

		[Test]
		public void NewSolutionAndDoNotCreateProjectDirectory ()
		{
			CreateProjectConfig (@"d:\projects");
			config.ProjectName = "MyProject";
			config.SolutionName = "MySolution";
			config.CreateProjectDirectoryInsideSolutionDirectory = false;
			config.CreateSolution = true;

			AssertPathsAreEqual (@"d:\projects\MyProject", config.ProjectLocation);
			AssertPathsAreEqual (@"d:\projects\MyProject", config.SolutionLocation);
		}

		[Test]
		public void AddProjectToExistingSolutionAndCreateProjectDirectory ()
		{
			CreateProjectConfig (@"d:\projects");
			config.ProjectName = "MyProject";
			config.CreateProjectDirectoryInsideSolutionDirectory = true;
			config.CreateSolution = false;

			AssertPathsAreEqual (@"d:\projects\MyProject", config.ProjectLocation);
		}

		[Test]
		public void AddProjectToExistingSolutionAndDoNotCreateProjectDirectory ()
		{
			CreateProjectConfig (@"d:\projects");
			config.ProjectName = "MyProject";
			config.CreateProjectDirectoryInsideSolutionDirectory = false;
			config.CreateSolution = false;

			AssertPathsAreEqual (@"d:\projects", config.ProjectLocation);
		}
	}
}

