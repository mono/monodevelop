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

using System.IO;
using System.Linq;
using MonoDevelop.Ide.Projects;
using NUnit.Framework;

namespace MonoDevelop.Ide.Templates
{
	[TestFixture]
	public class FinalProjectConfigurationTests
	{
		NewProjectConfiguration config;

		void CreateProjectConfig (string location)
		{
			config = new NewProjectConfiguration {
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

		[Test]
		public void NewSolutionWithoutAnyProjects ()
		{
			CreateProjectConfig (@"d:\projects");
			config.SolutionName = "MySolution";
			config.CreateProjectDirectoryInsideSolutionDirectory = false;
			config.CreateSolution = true;
			config.IsNewSolutionWithoutProjects = true;

			AssertPathsAreEqual (@"d:\projects\MySolution", config.SolutionLocation);
			AssertPathsAreEqual (@"d:\projects\MySolution", config.ProjectLocation);
		}

		[Test]
		public void NewSolutionWithoutAnyProjectsAndCreateProjectDirectoryInsideSolutionDirectory ()
		{
			CreateProjectConfig (@"d:\projects");
			config.SolutionName = "MySolution";
			config.CreateProjectDirectoryInsideSolutionDirectory = true;
			config.CreateSolution = true;
			config.IsNewSolutionWithoutProjects = true;

			AssertPathsAreEqual (@"d:\projects\MySolution", config.SolutionLocation);
			AssertPathsAreEqual (@"d:\projects\MySolution", config.ProjectLocation);
		}

		[Test]
		public void NewSolutionWithoutAnyProjectsWithProjectNameSpecifiedAndCreateProjectDirectoryInsideSolutionDirectory ()
		{
			CreateProjectConfig (@"d:\projects");
			config.SolutionName = "MySolution";
			config.CreateProjectDirectoryInsideSolutionDirectory = true;
			config.CreateSolution = true;
			config.ProjectName = "MyProject";
			config.IsNewSolutionWithoutProjects = true;

			AssertPathsAreEqual (@"d:\projects\MySolution", config.SolutionLocation);
			AssertPathsAreEqual (@"d:\projects\MySolution", config.ProjectLocation);
		}

		[Test]
		public void EmptyProjectNameAndLocationIsNotValid ()
		{
			CreateProjectConfig (@"d:\projects");
			config.SolutionName = "a";
			config.ProjectName = string.Empty;
			config.Location = string.Empty;

			bool result = config.IsValid ();

			Assert.IsFalse (result);
		}

		[Test]
		public void ProjectNameAndSolutionNameAndLocationAreNotEmptyIsValid ()
		{
			CreateProjectConfig (@"d:\projects");
			config.SolutionName = "a";
			config.ProjectName = "b";

			bool result = config.IsValid ();

			Assert.IsTrue (result);
		}

		[Test]
		public void EmptyProjectNameIsNotValid ()
		{
			CreateProjectConfig (@"d:\projects");
			config.SolutionName = "a";
			config.ProjectName = string.Empty;

			bool result = config.IsValid ();

			Assert.IsFalse (result);
		}

		[Test]
		public void ProjectNameWithSpacesIsNotValid ()
		{
			CreateProjectConfig (@"d:\projects");
			config.SolutionName = "a";
			config.ProjectName = "a b";

			bool result = config.IsValid ();

			Assert.IsFalse (result);
		}

		[Test]
		public void EmptyProjectNameWhenCreatingOnlySolutionIsValid ()
		{
			CreateProjectConfig (@"d:\projects");
			config.SolutionName = "a";
			config.ProjectName = string.Empty;
			config.IsNewSolutionWithoutProjects = true;

			bool result = config.IsValid ();

			Assert.IsTrue (result);
		}

		[Test]
		public void SolutionNameWithInvalidCharactersWhenCreatingSolutionOnlyIsNotValid ()
		{
			CreateProjectConfig (@"d:\projects");
			config.SolutionName = "a" + Path.GetInvalidPathChars ().First ();
			config.ProjectName = string.Empty;
			config.CreateSolution = true;
			config.IsNewSolutionWithoutProjects = true;

			bool result = config.IsValid ();

			Assert.IsFalse (result);
		}

		[Test]
		public void SolutionLocationWithInvalidCharactersWhenCreatingSolutionOnlyIsNotValid ()
		{
			CreateProjectConfig (@"d:\projects");
			config.SolutionName = "a";
			config.Location = config.Location + Path.GetInvalidPathChars ().First ();
			config.IsNewSolutionWithoutProjects = true;

			bool result = config.IsValid ();

			Assert.IsFalse (result);
		}

		[TestCase ("a", true)]
		[TestCase ("a.b", true)]
		[TestCase ("a_b", true)]
		[TestCase ("a&b", false)]
		[TestCase ("a<b", false)]
		[TestCase ("a*b", false)]
		[TestCase ("a;b", false)]
		[TestCase ("a?b", false)]
		[TestCase ("a>b", false)]
		[TestCase ("a%b", false)]
		[TestCase ("a:b", false)]
		[TestCase ("a#b", false)]
		[TestCase ("a|b", false)]
		public void CreateSolutionDirectoryWhenInvalidSolutionNameCharactersCauseConfigToBeInvalid (string solutionName, bool valid)
		{
			CreateProjectConfig (@"d:\projects");
			config.CreateSolution = true;
			config.ProjectName = "a";
			config.SolutionName = solutionName;

			bool result = config.IsValid ();

			Assert.AreEqual (valid, result);
		}

		[TestCase ("a", true)]
		[TestCase ("a.b", true)]
		[TestCase ("a_b", true)]
		[TestCase ("a&b", false)]
		[TestCase ("a<b", false)]
		[TestCase ("a*b", false)]
		[TestCase ("a;b", false)]
		[TestCase ("a?b", false)]
		[TestCase ("a>b", false)]
		[TestCase ("a%b", false)]
		[TestCase ("a:b", false)]
		[TestCase ("a#b", false)]
		[TestCase ("a|b", false)]
		public void CreateSolutionWithoutSeparateSolutionDirectoryWhenInvalidSolutionNameCharactersCauseConfigToBeInvalid (string solutionName, bool valid)
		{
			CreateProjectConfig (@"d:\projects");
			config.CreateSolution = true;
			config.CreateProjectDirectoryInsideSolutionDirectory = false;
			config.ProjectName = "a";
			config.SolutionName = solutionName;

			bool result = config.IsValid ();

			Assert.AreEqual (valid, result);
		}

		[TestCase ("a", true)]
		[TestCase ("a.b", true)]
		[TestCase ("a_b", true)]
		[TestCase ("a-b", true)]
		[TestCase ("a&b", false)]
		[TestCase ("a<b", false)]
		[TestCase ("a*b", false)]
		[TestCase ("a;b", false)]
		[TestCase ("a?b", false)]
		[TestCase ("a>b", false)]
		[TestCase ("a%b", false)]
		[TestCase ("a:b", false)]
		[TestCase ("a#b", false)]
		[TestCase ("a|b", false)]
		public void InvalidProjectNameCharactersCauseConfigToBeInvalid (string projectName, bool valid)
		{
			CreateProjectConfig (@"d:\projects");
			config.SolutionName = "a";
			config.ProjectName = projectName;

			bool result = config.IsValid ();

			Assert.AreEqual (valid, result);
		}

		[Test]
		public void EmojiProjectNameCharactersCauseConfigToBeInvalid ()
		{
			CreateProjectConfig (@"d:\projects");
			config.SolutionName = "a";

			// Mahjong tile
			config.ProjectName = "\U0001F004"; 
			Assert.IsFalse (config.IsValid ());

			// Smiley face
			config.ProjectName = "\U0001F600"; 
			Assert.IsFalse (config.IsValid ());

			// Zimbabwe flag.
			config.ProjectName = "\U0001F1FF\U0001F1FC"; 
			Assert.IsFalse (config.IsValid ());

			// Double exclamation mark.
			config.ProjectName = "\U0000203C"; 
			Assert.IsFalse (config.IsValid ());
		}

		[Test]
		public void NewProjectOnlyAndCreateProjectDirectory ()
		{
			CreateProjectConfig (@"d:\projects\MySolution");
			config.ProjectName = "MyProject";
			config.SolutionName = "MySolution";
			config.CreateProjectDirectoryInsideSolutionDirectory = true;
			config.CreateSolution = false;

			AssertPathsAreEqual (@"d:\projects\MySolution\MyProject", config.ProjectLocation);
			AssertPathsAreEqual (@"d:\projects\MySolution", config.SolutionLocation);
		}
	}
}

