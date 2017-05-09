//
// ProjectTemplateTests.cs
//
// Author:
//       Alan McGovern <alan@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc.
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
using System.Collections.Generic;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Projects;
using MonoDevelop.Projects.SharedAssetsProjects;
using NUnit.Framework;
using UnitTests;
using System.Linq;
using System.Threading.Tasks;

namespace MonoDevelop.Ide
{
	[TestFixture]
	public class ProjectTemplateTests : TestBase
	{
		public ProjectTemplateTests ()
		{
			Simulate ();
		}

		IEnumerable<string> Templates {
			get {
				return ProjectTemplate.ProjectTemplates.Select (t => t.Id);
			}
		}

		[Test]
		[TestCaseSource ("Templates")]
		public void CreateEveryProjectTemplate (string tt)
		{
			var template = ProjectTemplate.ProjectTemplates.FirstOrDefault (t => t.Id == tt);
			if (template.Name.Contains ("Gtk#"))
				return;
			var dir = Util.CreateTmpDir (template.Id);
			var cinfo = new ProjectCreateInformation {
				ProjectBasePath = dir,
				ProjectName = "ProjectName",
				SolutionName = "SolutionName",
				SolutionPath = dir
			};
			cinfo.Parameters ["CreateSharedAssetsProject"] = "False";
			cinfo.Parameters ["UseUniversal"] = "True";
			cinfo.Parameters ["UseIPad"] = "False";
			cinfo.Parameters ["UseIPhone"] = "False";
			cinfo.Parameters ["CreateiOSUITest"] = "False";
			cinfo.Parameters ["CreateAndroidUITest"] = "False";

			template.CreateWorkspaceItem (cinfo);
		}

		[Test]
		public async Task NewSharedProjectAddedToExistingSolutionUsesCorrectBuildAction ()
		{
			Solution sol = TestProjectsChecks.CreateConsoleSolution ("shared-project");
			await sol.SaveAsync (Util.GetMonitor ());

			var template = ProjectTemplate.ProjectTemplates.FirstOrDefault (t => t.Id == "MonoDevelop.CSharp.SharedProject");
			var dir = Util.CreateTmpDir (template.Id);
			var cinfo = new ProjectCreateInformation {
				ProjectBasePath = dir,
				ProjectName = "ProjectName",
				SolutionName = "SolutionName",
				SolutionPath = dir
			};

			var sharedAssetsProject = template.CreateProjects (sol.RootFolder, cinfo)
				.OfType<SharedAssetsProject> ().Single ();
			var myclassFile = sharedAssetsProject.Files.First (f => f.FilePath.FileName == "MyClass.cs");
			Assert.AreEqual ("Compile", myclassFile.BuildAction);
		}
	}
}

