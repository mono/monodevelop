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
using System.IO;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Ide.Projects;
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
	public class ProjectTemplateTests : IdeTestBase
	{
		Solution solution;

		public ProjectTemplateTests ()
		{
			Simulate ();
		}

		IEnumerable<string> Templates {
			get {
				return ProjectTemplate.ProjectTemplates.Select (t => t.Id);
			}
		}

		[TearDown]
		public override void TearDown ()
		{
			solution?.Dispose ();
			solution = null;

			base.TearDown ();
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

			solution = template.CreateWorkspaceItem (cinfo) as Solution;
		}

		[Test]
		public async Task NewSharedProjectAddedToExistingSolutionUsesCorrectBuildAction ()
		{
			solution = TestProjectsChecks.CreateConsoleSolution ("shared-project");
			await solution.SaveAsync (Util.GetMonitor ());

			var template = ProjectTemplate.ProjectTemplates.FirstOrDefault (t => t.Id == "MonoDevelop.CSharp.SharedProject");
			var dir = Util.CreateTmpDir (template.Id);
			var cinfo = new ProjectCreateInformation {
				ProjectBasePath = dir,
				ProjectName = "ProjectName",
				SolutionName = "SolutionName",
				SolutionPath = dir
			};

			var sharedAssetsProject = template.CreateProjects (solution.RootFolder, cinfo)
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
			var pclTemplate = mutliplatformLibraryCategory.Templates.Single (t => t.GroupId == "md-project-portable-library").GetTemplate ("C#");
			var standardTemplate = mutliplatformLibraryCategory.Templates.Single (t => t.GroupId == "Microsoft.Common.Library").GetTemplate ("C#");

			var tempDirectory = Util.CreateTmpDir ("Bug57840Test");
			var result = await templatingService.ProcessTemplate (pclTemplate, new Ide.Projects.NewProjectConfiguration () {
				CreateSolution = true,
				Location = tempDirectory,
				SolutionName = "Bug57840Test",
				ProjectName = "Bug57840PclTestProject",
				CreateProjectDirectoryInsideSolutionDirectory = false
			}, null);

			solution = result.WorkspaceItems.OfType<Solution> ().Single ();

			await solution.SaveAsync (Util.GetMonitor ());
			var project = solution.GetAllProjects ().Single ();
			project.Policies.Set<TextStylePolicy> (new TextStylePolicy (1, 1, 1, true, true, true, EolMarker.Mac), "text/x-csharp");

			var file = project.Files.Single (f => f.FilePath.FileName == "MyClass.cs").FilePath;
			var fileContentBeforeFormat = await TextFileUtility.ReadAllTextAsync (file);
			await FormatFile (project, file);
			var fileContentAfterFormat = await TextFileUtility.ReadAllTextAsync (file);

			Assert.AreNotEqual (fileContentBeforeFormat.Text, fileContentAfterFormat.Text);//Make sure our weird formatting applied

			solution.Policies.Set<TextStylePolicy> (new TextStylePolicy (3, 3, 3, true, true, true, EolMarker.Mac), "text/x-csharp");

			var result2 = await templatingService.ProcessTemplate (standardTemplate, new Ide.Projects.NewProjectConfiguration () {
				CreateSolution = false,
				Location = solution.BaseDirectory,
				ProjectName = "Bug57840StandardTestProject",
				CreateProjectDirectoryInsideSolutionDirectory = false
			}, solution.RootFolder);
			var standardProject = result2.WorkspaceItems.OfType<DotNetProject> ().Single ();
			solution.RootFolder.AddItem (standardProject);
			await solution.SaveAsync (Util.GetMonitor ());
			var fileContentAfterSecondProject = await TextFileUtility.ReadAllTextAsync (file);
			Assert.AreEqual (fileContentAfterSecondProject.Text, fileContentAfterFormat.Text);//Make sure our weird formatting is preserved
			var class1File = standardProject.Files.Single (f => f.FilePath.FileName == "Class1.cs").FilePath;
			var fileContentAfterCreation = await TextFileUtility.ReadAllTextAsync (class1File);
			standardProject.Policies.Set<TextStylePolicy> (new TextStylePolicy (3, 3, 3, true, true, true, EolMarker.Mac), "text/x-csharp");
			await FormatFile (standardProject, class1File);
			standardProject.Dispose();
			var fileContentAfterForceFormatting = await TextFileUtility.ReadAllTextAsync (class1File);
			Assert.AreEqual (fileContentAfterForceFormatting.Text, fileContentAfterCreation.Text,
			                "We expect them to be same because we placed same formatting policy on solution before creataion as after creation on project when we manually formatted.");
		}

		[Test]
		public async Task DotNetCoreProjectTemplateUsingBackslashesInPrimaryOutputPathsIsSupported ()
		{
			var templatingService = new TemplatingService ();

			string templateId = "MonoDevelop.Ide.Tests.TwoProjects.CSharp";
			string scanPath = Util.GetSampleProjectPath ("DotNetCoreTemplating");
			var template = MicrosoftTemplateEngineProjectTemplatingProvider.CreateTemplate (templateId, scanPath);

			string tempDirectory = Util.CreateTmpDir ("BackslashInPrimaryOutputTest");
			string projectDirectory = Path.Combine (tempDirectory, "BackslashInPrimaryOutputTestProject");
			Directory.CreateDirectory (projectDirectory);
			string library1FileToOpen = Path.Combine (projectDirectory, "Library1", "MyClass.cs");
			string library2FileToOpen = Path.Combine (projectDirectory, "Library2", "MyClass.cs");

			var result = await templatingService.ProcessTemplate (template, new NewProjectConfiguration () {
				CreateSolution = true,
				Location = tempDirectory,
				SolutionName = "BackslashInPrimaryOutputTest",
				ProjectName = "BackslashInPrimaryOutputTestProject",
				CreateProjectDirectoryInsideSolutionDirectory = false,
			}, null);

			solution = result.WorkspaceItems.OfType<Solution> ().Single ();

			await solution.SaveAsync (Util.GetMonitor ());
			Assert.AreEqual (2, solution.GetAllProjects ().Count ());
			Assert.IsNotNull (solution.FindProjectByName ("Library1"));
			Assert.IsNotNull (solution.FindProjectByName ("Library2"));
			Assert.AreEqual (2, result.Actions.Count ());
			Assert.That (result.Actions, Contains.Item (library1FileToOpen));
			Assert.That (result.Actions, Contains.Item (library2FileToOpen));
		}

		[TestCase ("*.xml")]
		[TestCase ("*.XML")]
		[TestCase ("*.txt|*.xml")]
		[TestCase ("test.xml")]
		[TestCase ("TEST.xml")]
		[TestCase (null)]
		public async Task DotNetCoreProjectTemplate_ExcludeFile (string exclude)
		{
			var templatingService = new TemplatingService ();

			string templateId = "MonoDevelop.Ide.Tests.FileFormatExclude.CSharp";
			string scanPath = Util.GetSampleProjectPath ("FileFormatExclude");

			var xmlFilePath = Path.Combine (scanPath, "test.xml");
			string expectedXml = "<root><child/></root>";
			File.WriteAllText (xmlFilePath, expectedXml);

			var template = MicrosoftTemplateEngineProjectTemplatingProvider.CreateTemplate (templateId, scanPath) as MicrosoftTemplateEngineSolutionTemplate;
			template.FileFormattingExclude = exclude;

			string tempDirectory = Util.CreateTmpDir ("FileFormatExcludeTest");

			string projectDirectory = Path.Combine (tempDirectory, "FileFormatExcludeTestProject");
			Directory.CreateDirectory (projectDirectory);

			var result = await templatingService.ProcessTemplate (template, new NewProjectConfiguration () {
				CreateSolution = true,
				Location = tempDirectory,
				SolutionName = "FileFormatExcludeTest",
				ProjectName = "FileFormatExcludeTestProject",
				CreateProjectDirectoryInsideSolutionDirectory = false,
			}, null);

			solution = result.WorkspaceItems.OfType<Solution> ().Single ();

			await solution.SaveAsync (Util.GetMonitor ());

			xmlFilePath = Path.Combine (projectDirectory, "test.xml");
			string xml = File.ReadAllText (xmlFilePath);

			if (string.IsNullOrEmpty (exclude)) {
				// Ensure formatting occurs if file is not excluded.
				Assert.AreNotEqual (expectedXml, xml);
			} else {
				Assert.AreEqual (expectedXml, xml);
			}
		}
	}
}

