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
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Core;
using MonoDevelop.Ide.CodeFormatting;
using MonoDevelop.Core.Text;
using System;

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

		async Task FormatFile (Project p, FilePath file)
		{
			string mime = DesktopService.GetMimeTypeForUri (file);
			if (mime == null)
				return;

			var formatter = CodeFormatterService.GetFormatter (mime);
			if (formatter != null) {
				try {
					var content = await TextFileUtility.ReadAllTextAsync (file);
					var formatted = formatter.FormatText (p.Policies, content.Text);
					if (formatted != null)
						TextFileUtility.WriteText (file, formatted, content.Encoding);
				} catch (Exception ex) {
					LoggingService.LogError ("File formatting failed", ex);
				}
			}
		}

		[Test ()]
		public async Task Bug57840 ()
		{
			var templatingService = new TemplatingService ();
			var mutliplatformLibraryCategory = templatingService.GetProjectTemplateCategories ().Single (c => c.Id == "multiplat")
																	   .Categories.Single (c => c.Id == "library")
																	   .Categories.Single (c => c.Id == "general");
			var pclTemplate = mutliplatformLibraryCategory.Templates.Single (t => t.Id == "MonoDevelop.CSharp.PortableLibrary");

			var standardTemplate = mutliplatformLibraryCategory.Templates.Single (t => t.Id == "Microsoft.Common.Library.CSharp");

			var tempDirectory = Util.CreateTmpDir ("Bug57840Test");
			var result = await templatingService.ProcessTemplate (pclTemplate, new Ide.Projects.NewProjectConfiguration () {
				CreateSolution = true,
				Location = tempDirectory,
				SolutionName = "Bug57840Test",
				ProjectName = "Bug57840PclTestProject",
				CreateProjectDirectoryInsideSolutionDirectory = false
			}, null);

			var solution = result.WorkspaceItems.OfType<Solution> ().Single ();

			await solution.SaveAsync (Util.GetMonitor ());
			var project = solution.GetAllProjects ().Single ();
			project.Policies.Set<TextStylePolicy> (new TextStylePolicy (1, 1, 1, true, true, true, EolMarker.Mac), "text/x-csharp");

			var file = project.Files.Single (f => f.FilePath.FileName == "MyClass.cs").FilePath;
			var fileContentBeforeFormat = await TextFileUtility.ReadAllTextAsync (file);
			await FormatFile (project, file);
			var fileContentAfterFormat = await TextFileUtility.ReadAllTextAsync (file);

			Assert.AreNotEqual (fileContentBeforeFormat.Text, fileContentAfterFormat.Text);//Make sure our weird formatting applied

			var result2 = await templatingService.ProcessTemplate (standardTemplate, new Ide.Projects.NewProjectConfiguration () {
				CreateSolution = false,
				Location = solution.BaseDirectory,
				ProjectName = "Bug57840StandardTestProject",
				CreateProjectDirectoryInsideSolutionDirectory = false
			}, solution.RootFolder);
			await solution.SaveAsync (Util.GetMonitor ());
			var fileContentAfterSecondProject = await TextFileUtility.ReadAllTextAsync (file);
			Assert.AreEqual (fileContentAfterSecondProject.Text, fileContentAfterFormat.Text);//Mkae sure our weird formatting is preserved
		}
	}
}

