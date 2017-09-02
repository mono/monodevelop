// MSBuildTests.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.IO;
using System.Xml;
using NUnit.Framework;
using UnitTests;
using MonoDevelop.Core;
using System.Linq;
using MonoDevelop.Projects.MSBuild;
using System.Threading.Tasks;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects.Extensions;

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class MSBuildTests : TestBase
	{
		[TestFixtureSetUp]
		public void SetUp ()
		{
			string dir = Path.GetDirectoryName (typeof (Project).Assembly.Location);
			Environment.SetEnvironmentVariable ("HHH", "EnvTest");
			Environment.SetEnvironmentVariable ("SOME_PLACE", dir);
		}

		[Test ()]
		public async Task LoadSaveBuildConsoleProject ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");

			Solution item = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			Assert.IsTrue (item is Solution);

			Solution sol = (Solution)item;
			TestProjectsChecks.CheckBasicVsConsoleProject (sol);
			string projectFile = ((Project)sol.Items [0]).FileName;

			BuildResult cr = await item.Build (Util.GetMonitor (), "Debug");
			Assert.IsNotNull (cr);
			Assert.AreEqual (0, cr.ErrorCount);
			Assert.AreEqual (0, cr.WarningCount);

			string solXml = File.ReadAllText (solFile);
			string projectXml = File.ReadAllText (projectFile);

			await sol.SaveAsync (Util.GetMonitor ());

			Assert.AreEqual (solXml, File.ReadAllText (solFile));
			Assert.AreEqual (projectXml, File.ReadAllText (projectFile));

			sol.Dispose ();
		}

		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public async Task EvaluateUnknownPropertyDuringBuild (bool requiresMSBuild)
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");

			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

			var project = ((Project)sol.Items [0]);
			project.RequiresMicrosoftBuild = requiresMSBuild;

			var context = new TargetEvaluationContext ();
			context.PropertiesToEvaluate.Add ("TestUnknownPropertyToEvaluate");

			var res = await project.RunTarget (Util.GetMonitor (), "Build", project.Configurations [0].Selector, context);
			Assert.IsNotNull (res);
			Assert.IsNotNull (res.BuildResult);
			Assert.AreEqual (0, res.BuildResult.ErrorCount);
			Assert.AreEqual (0, res.BuildResult.WarningCount);
			Assert.IsNull (res.Properties.GetValue ("TestUnknownPropertyToEvaluate"));

			sol.Dispose ();
		}

		[Test]
		public async Task BuildConsoleProject ()
		{
			Solution sol = TestProjectsChecks.CreateConsoleSolution ("console-project-msbuild");
			await sol.SaveAsync (Util.GetMonitor ());

			// Ensure the project is buildable
			var result = await sol.Build (Util.GetMonitor (), "Debug");
			Assert.AreEqual (0, result.ErrorCount, "#1");

			sol.Dispose ();
		}

		[Test]
		public async Task BuildConsoleProjectAfterRename ()
		{
			Solution sol = TestProjectsChecks.CreateConsoleSolution ("console-project-msbuild");
			await sol.SaveAsync (Util.GetMonitor ());

			// Ensure the project is still buildable with xbuild after a rename
			var project = sol.GetAllProjects().First();
			FilePath newFile = project.FileName.ParentDirectory.Combine("Test" + project.FileName.Extension);
			FileService.RenameFile (project.FileName, newFile.FileName);
			project.Name = "Test";

			var result = await sol.Build (Util.GetMonitor (), "Release");
			Assert.AreEqual (0, result.ErrorCount, "#2");

			sol.Dispose ();
		}

		[Test]
		public async Task CreateConsoleProject ()
		{
			Solution sol = TestProjectsChecks.CreateConsoleSolution ("console-project-msbuild");
			sol.ConvertToFormat (MSBuildFileFormat.VS2010);
			await sol.SaveAsync (Util.GetMonitor ());

			// msbuild format

			string solXml = File.ReadAllText (sol.FileName);
			string projectXml = File.ReadAllText (((SolutionItem)sol.Items [0]).FileName);

			// Make sure we compare using the same guid
			Project p = sol.Items [0] as Project;
			string guid = p.ItemId;
			solXml = solXml.Replace (guid, "{969F05E2-0E79-4C5B-982C-8F3DD4D46311}");
			projectXml = projectXml.Replace (guid, "{969F05E2-0E79-4C5B-982C-8F3DD4D46311}");

			string solFile = Util.GetSampleProjectPath ("generated-console-project", "TestSolution.sln");
			string projectFile = Util.GetSampleProjectPath ("generated-console-project", "TestProject.csproj");

			Assert.AreEqual (Util.ToWindowsEndings (File.ReadAllText (solFile)), solXml);
			Assert.AreEqual (Util.ToWindowsEndings (File.ReadAllText (projectFile)), projectXml);

			sol.Dispose ();
		}

		[Test]
		public async Task SetCustomPropertiesInNewProject ()
		{
			Solution sol = TestProjectsChecks.CreateConsoleSolution ("console-project-msbuild");
			sol.ConvertToFormat (MSBuildFileFormat.VS2010);
			Project p = sol.Items [0] as Project;
			await p.WriteProjectAsync (Util.GetMonitor ());
			p.ProjectProperties.SetValue ("TestProperty", "TestValue");

			await sol.SaveAsync (Util.GetMonitor ());

			string projectXml = File.ReadAllText (((SolutionItem)sol.Items [0]).FileName);

			// Make sure we compare using the same guid
			projectXml = projectXml.Replace (p.ItemId, "{969F05E2-0E79-4C5B-982C-8F3DD4D46311}");

			string projectFile = Util.GetSampleProjectPath ("generated-console-project", "TestProject2.csproj");

			Assert.AreEqual (Util.ToWindowsEndings (File.ReadAllText (projectFile)), projectXml);

			sol.Dispose ();
		}

		[Test]
		public async Task TestCreateLoadSaveConsoleProject ()
		{
			await TestProjectsChecks.TestCreateLoadSaveConsoleProject (MSBuildFileFormat.VS2005);
		}

		[Test]
		public async Task GenericProject ()
		{
			await TestProjectsChecks.CheckGenericItemProject (MSBuildFileFormat.VS2005);
		}

		[Test]
		public async Task TestLoadSaveSolutionFolders ()
		{
			await TestProjectsChecks.TestLoadSaveSolutionFolders (MSBuildFileFormat.VS2005);
		}

		[Test]
		public async Task TestLoadSaveResources ()
		{
			await TestProjectsChecks.TestLoadSaveResources (MSBuildFileFormat.VS2005);
		}

		[Test]
		public async Task TestConfigurationWithAnyCpu ()
		{
			string projectFile = Util.GetSampleProject ("test-multi-configuration", "project.csproj");
			DotNetProject p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile) as DotNetProject;
			Assert.IsNotNull (p);

			var refXml = File.ReadAllText (p.FileName);
			await p.SaveAsync (Util.GetMonitor ());

			Assert.AreEqual (refXml, File.ReadAllText (p.FileName));

			p.Dispose ();
		}

		[Test]
		public async Task ProjectReferenceWithSpace ()
		{
			string solFile = Util.GetSampleProject ("project-ref-with-spaces", "project-ref-with-spaces.sln");
			Solution sol = await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile) as Solution;
			Assert.IsNotNull (sol);
			Assert.AreEqual (2, sol.Items.Count);

			DotNetProject p = sol.FindProjectByName ("project-ref-with-spaces") as DotNetProject;
			Assert.IsNotNull (p);

			Assert.AreEqual (1, p.References.Count);
			Assert.AreEqual ("some - library", p.References [0].Reference);

			sol.Dispose ();
		}

		[Test]
		public async Task RoundtripPropertyWithXmlCharacters ()
		{
			Solution sol = TestProjectsChecks.CreateConsoleSolution ("roundtrip-property-with-xml");
			sol.ConvertToFormat (MSBuildFileFormat.VS2005);

			var value = "Hello<foo>&.exe";

			var p = (DotNetProject)sol.GetAllProjects ().First ();
			var conf = ((DotNetProjectConfiguration)p.Configurations [0]);
			conf.OutputAssembly = value;
			await sol.SaveAsync (Util.GetMonitor ());

			sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), sol.FileName);
			p = (DotNetProject)sol.GetAllProjects ().First ();
			conf = ((DotNetProjectConfiguration)p.Configurations [0]);

			Assert.AreEqual (value, conf.OutputAssembly);

			sol.Dispose ();
		}



		[Test]
		public async Task RoundtripPropertyWithWhitespaceCharacters ()
		{
			var projectFile = Util.GetSampleProject ("test-whitespace-roundtrip", "project.csproj");
			var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile) as DotNetProject;
			Assert.IsNotNull (p);
			var configuration = p.Configurations [0];
			configuration.CopyFrom (p.Configurations [0]);
			p.Configurations.Remove (p.Configurations [0]);
			p.Configurations.Insert (0, configuration);
			await p.SaveAsync (Util.GetMonitor ());
			Assert.AreEqual (Util.ToSystemEndings (File.ReadAllText (p.FileName + ".saved")), File.ReadAllText (p.FileName));

			p.Dispose ();
		}

		[Test]
		//[Ignore ("xbuild bug. It is not returning correct values for evaluated-items-without-condition list")]
		public async Task SaveItemsWithProperties ()
		{
			string solFile = Util.GetSampleProject ("property-evaluation-test", "property-evaluation-test.sln");
			Solution sol = await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile) as Solution;
			var p = (DotNetProject)sol.GetAllProjects ().First ();

			string projectXml1 = File.ReadAllText (p.FileName);

			await p.SaveAsync (Util.GetMonitor ());

			string projectXml2 = File.ReadAllText (p.FileName);

			Assert.AreEqual (projectXml1, projectXml2);

			sol.Dispose ();
		}

		[Test]
		public async Task SaveItemsWithProperties2 ()
		{
			string projFile = Util.GetSampleProject ("property-save-test", "property-save-test.csproj");
			Project p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile) as Project;

			string projectXml1 = File.ReadAllText (p.FileName);

			await p.SaveAsync (Util.GetMonitor ());

			string projectXml2 = File.ReadAllText (p.FileName);

			Assert.AreEqual (projectXml1, projectXml2);

			p.Dispose ();
		}

		[Test]
		public async Task EvaluateProperties ()
		{
			string dir = Path.GetDirectoryName (typeof (Project).Assembly.Location);

			string solFile = Util.GetSampleProject ("property-evaluation-test", "property-evaluation-test.sln");
			Solution sol = await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile) as Solution;
			var p = (DotNetProject)sol.GetAllProjects ().First ();
			Assert.AreEqual ("Program1_test1.cs", p.Files [0].FilePath.FileName, "Basic replacement");
			Assert.AreEqual ("Program2_test1_test2.cs", p.Files [1].FilePath.FileName, "Property referencing same property");
			Assert.AreEqual ("Program3_full.cs", p.Files [2].FilePath.FileName, "Property inside group with non-evaluable condition");
			Assert.AreEqual ("Program4_yes_value.cs", p.Files [3].FilePath.FileName, "Evaluation of group condition");
			Assert.AreEqual ("Program5_yes_value.cs", p.Files [4].FilePath.FileName, "Evaluation of property condition");
			Assert.AreEqual ("Program6_unknown.cs", p.Files [5].FilePath.FileName, "Evaluation of property with non-evaluable condition");
			Assert.AreEqual ("Program7_test1.cs", p.Files [6].FilePath.FileName, "Item conditions are ignored");

			var testRef = Path.Combine (dir, "MonoDevelop.Core.dll");
			var asms = (await p.GetReferencedAssemblies (sol.Configurations [0].Selector)).Select (ar => ar.FilePath).ToArray ();
			Assert.IsTrue (asms.Contains (testRef));

			sol.Dispose ();
		}

		[Test]
		public async Task EvaluateImportedProperty ()
		{
			// Even when a property is defined in an imported targets file, the project properties should include the value
			string solFile = Util.GetSampleProject ("property-evaluation-test", "property-evaluation-test.sln");
			Solution sol = await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile) as Solution;
			var p = (DotNetProject)sol.GetAllProjects ().First ();

			Assert.AreEqual ("yes", p.ProjectProperties.GetValue ("Imported"));

			sol.Dispose ();
		}

		//[Ignore ("xbuild bug. It is not returning correct values for evaluated-items-without-condition list")]
		[Test]
		public async Task EvaluatePropertiesWithConditionalGroup ()
		{
			string solFile = Util.GetSampleProject ("property-evaluation-test", "property-evaluation-test.sln");
			Solution sol = await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile) as Solution;
			var p = (DotNetProject)sol.GetAllProjects ().First ();
			Assert.AreEqual ("Program8_test1.cs", p.Files [7].FilePath.FileName, "Item group conditions are not ignored");
			Assert.AreEqual ("Program9_yes.cs", p.Files [8].FilePath.FileName, "Non-evaluable property group clears properties");
			Assert.AreEqual ("Program10_$(AAA", p.Files [9].FilePath.FileName, "Invalid property reference");
			Assert.AreEqual ("Program11_EnvTest.cs", p.Files [10].FilePath.FileName, "Environment variable");

			sol.Dispose ();
		}

		async Task LoadBuildVSConsoleProject (string vsVersion, string toolsVersion)
		{
			string solFile = Util.GetSampleProject ("ConsoleApp-VS" + vsVersion, "ConsoleApplication.sln");
			var monitor = new ProgressMonitor ();
			var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (monitor, solFile);
			Assert.IsTrue (monitor.Errors.Length == 0);
			Assert.IsTrue (monitor.Warnings.Length == 0);
			var p = (DotNetProject)sol.GetAllProjects ().First ();
			Assert.AreEqual (toolsVersion, p.ToolsVersion);
			var r = await sol.Build (monitor, "Debug");
			Assert.IsTrue (monitor.Errors.Length == 0);
			Assert.IsTrue (monitor.Warnings.Length == 0);
			Assert.IsFalse (r.Failed);
			Assert.IsTrue (r.ErrorCount == 0);

			//there may be a single warning about not being able to find Client profile
			var f = r.Errors.FirstOrDefault ();
			var clientProfileError =
				"Unable to find framework corresponding to the target framework moniker " +
				"'.NETFramework,Version=v4.0,Profile=Client'";

			if (f != null)
				Assert.IsTrue (f.ErrorText.Contains (clientProfileError), "Build failed with: " + f.ErrorText);

			string projectFile = ((Project)sol.Items [0]).FileName;
			string projectXml = Util.ReadAllWithWindowsEndings (projectFile);

			await sol.SaveAsync (monitor);
			Assert.IsTrue (monitor.Errors.Length == 0);
			Assert.IsTrue (monitor.Warnings.Length == 0);

			Assert.AreEqual (projectXml, Util.ReadAllWithWindowsEndings (projectFile));

			sol.Dispose ();
		}

		[Test]
		public async Task LoadBuildVS2010ConsoleProject ()
		{
			await LoadBuildVSConsoleProject ("2010", "4.0");
		}

		[Test]
		public async Task LoadBuildVS2012ConsoleProject ()
		{
			await LoadBuildVSConsoleProject ("2012", "4.0");
		}

		[Test]
		public async Task LoadBuildVS2013ConsoleProject ()
		{
			await LoadBuildVSConsoleProject ("2013", "12.0");
		}

		[Test]
		public async Task SaveReferenceWithCondition ()
		{
			string solFile = Util.GetSampleProject ("console-project-conditional-reference", "ConsoleProject.sln");
			Solution sol = await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile) as Solution;

			string proj = sol.GetAllProjects ().First ().FileName;

			string projectXml1 = File.ReadAllText (proj);
			await sol.SaveAsync (new ProgressMonitor ());

			string projectXml2 = File.ReadAllText (proj);
			Assert.AreEqual (projectXml1, projectXml2);

			sol.Dispose ();
		}

		[Test]
		public async Task AddNewImportWithoutConditionToProject ()
		{
			Solution sol = TestProjectsChecks.CreateConsoleSolution ("console-project-msbuild");
			var project = sol.GetAllProjects ().First () as DotNetProject;
			project.AddImportIfMissing (@"packages\Xamarin.Forms\build\Xamarin.Forms.targets", null);
			await sol.SaveAsync (Util.GetMonitor ());

			var doc = new XmlDocument ();
			doc.Load (project.FileName);
			var manager = new XmlNamespaceManager (doc.NameTable);
			manager.AddNamespace ("ms", "http://schemas.microsoft.com/developer/msbuild/2003");
			XmlElement import = (XmlElement)doc.SelectSingleNode (@"//ms:Import[@Project='packages\Xamarin.Forms\build\Xamarin.Forms.targets']", manager);

			Assert.IsNotNull (import);
			Assert.IsFalse (import.HasAttribute ("Condition"));

			sol.Dispose ();
		}

		[Test]
		public async Task AddNewImportWithConditionToProject ()
		{
			Solution sol = TestProjectsChecks.CreateConsoleSolution ("console-project-msbuild");
			var project = sol.GetAllProjects ().First () as DotNetProject;
			string condition = @"Exists('packages\Xamarin.Forms\build\Xamarin.Forms.targets')";
			project.AddImportIfMissing (@"packages\Xamarin.Forms\build\Xamarin.Forms.targets", condition);
			await sol.SaveAsync (Util.GetMonitor ());

			var doc = new XmlDocument ();
			doc.Load (project.FileName);
			var manager = new XmlNamespaceManager (doc.NameTable);
			manager.AddNamespace ("ms", "http://schemas.microsoft.com/developer/msbuild/2003");
			XmlElement import = (XmlElement)doc.SelectSingleNode (@"//ms:Import[@Project='packages\Xamarin.Forms\build\Xamarin.Forms.targets']", manager);

			Assert.AreEqual (condition, import.GetAttribute ("Condition"));

			sol.Dispose ();
		}

		[Test]
		public async Task ProjectWithCustomConfigPropertyGroupBug20554 ()
		{
			string solFile = Util.GetSampleProject ("console-project-custom-configs", "ConsoleProject.sln");
			Solution sol = await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile) as Solution;

			string proj = sol.GetAllProjects ().First ().FileName;

			string projectXml1 = File.ReadAllText (proj);
			await sol.SaveAsync (new ProgressMonitor ());

			string projectXml2 = File.ReadAllText (proj);
			Assert.AreEqual (projectXml1, projectXml2);

			sol.Dispose ();
		}

		[Test]
		public async Task WriteExtendedProperties ()
		{
			var tn = new MyProjectTypeNode ();
			MSBuildProjectService.RegisterCustomItemType (tn);
			try {
				var p = Services.ProjectService.CreateProject (tn.Guid);
				Assert.IsInstanceOf<MyProject> (p);
				var mp = (MyProject)p;
				mp.ItemId = "{74FADC4E-C9A8-456E-9A2C-DB933220E073}";
				string dir = Util.CreateTmpDir ("WriteExtendedProperties");
				mp.FileName = Path.Combine (dir, "test.sln");
				mp.Data = new MyProjectData { Foo = "bar" };
				mp.DataProperty = new MyProjectData { Foo = "rep" };
				mp.SimpleData = "Test";
				await p.SaveAsync (Util.GetMonitor ());

				string referenceFile = Util.GetSampleProject ("extended-project-properties", "test-data.myproj");

				string projectXml1 = File.ReadAllText (referenceFile);
				string projectXml2 = File.ReadAllText (mp.FileName);
				Assert.AreEqual (Util.ToWindowsEndings (projectXml1), projectXml2);

				p.Dispose ();
			} finally {
				MSBuildProjectService.UnregisterCustomItemType (tn);
			}
		}

		[Test]
		public async Task LoadExtendedProperties ()
		{
			string projFile = Util.GetSampleProject ("extended-project-properties", "test-data.myproj");

			var tn = new MyProjectTypeNode ();
			MSBuildProjectService.RegisterCustomItemType (tn);
			try {
				var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				Assert.IsInstanceOf<MyProject> (p);
				var mp = (MyProject)p;

				Assert.NotNull (mp.Data);
				Assert.AreEqual (mp.Data.Foo, "bar");
				Assert.AreEqual (mp.SimpleData, "Test");

				p.Dispose ();
			} finally {
				MSBuildProjectService.UnregisterCustomItemType (tn);
			}
		}

		[Test]
		public async Task LoadSaveExtendedPropertiesWithUnknownProperty ()
		{
			// Unknown data should be kept in the file

			string projFile = Util.GetSampleProject ("extended-project-properties", "test-unknown-data.myproj");
			string projectXml1 = File.ReadAllText (projFile);

			var tn = new MyProjectTypeNode ();
			MSBuildProjectService.RegisterCustomItemType (tn);
			try {
				var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				Assert.IsInstanceOf<MyProject> (p);
				var mp = (MyProject)p;

				Assert.NotNull (mp.Data);
				Assert.AreEqual (mp.Data.Foo, "bar");
				Assert.AreEqual (mp.SimpleData, "Test");

				await mp.SaveAsync (Util.GetMonitor ());

				string projectXml2 = File.ReadAllText (projFile);
				Assert.AreEqual (projectXml1, projectXml2);

				p.Dispose ();

			} finally {
				MSBuildProjectService.UnregisterCustomItemType (tn);
			}
		}

		[Test]
		public async Task RemoveExtendedProperties ()
		{
			// Whole ProjectExtensions section should be removed

			string projFile = Util.GetSampleProject ("extended-project-properties", "test-data.myproj");

			var tn = new MyProjectTypeNode ();
			MSBuildProjectService.RegisterCustomItemType (tn);
			try {
				var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				Assert.IsInstanceOf<MyProject> (p);
				var mp = (MyProject)p;

				Assert.NotNull (mp.Data);
				Assert.AreEqual (mp.Data.Foo, "bar");
				Assert.AreEqual (mp.SimpleData, "Test");

				mp.Data = null;

				await mp.SaveAsync (Util.GetMonitor ());

				string projectXml1 = File.ReadAllText (Util.GetSampleProject ("extended-project-properties", "test-empty.myproj"));

				string projectXml2 = File.ReadAllText (projFile);
				Assert.AreEqual (projectXml1, projectXml2);

				p.Dispose ();

			} finally {
				MSBuildProjectService.UnregisterCustomItemType (tn);
			}
		}

		[Test]
		public async Task RemoveExtendedPropertiesWithUnknownProperty ()
		{
			// Unknown data should be kept in the file

			string projFile = Util.GetSampleProject ("extended-project-properties", "test-unknown-data.myproj");

			var tn = new MyProjectTypeNode ();
			MSBuildProjectService.RegisterCustomItemType (tn);
			try {
				var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				Assert.IsInstanceOf<MyProject> (p);
				var mp = (MyProject)p;

				Assert.NotNull (mp.Data);
				Assert.AreEqual (mp.Data.Foo, "bar");
				Assert.AreEqual (mp.SimpleData, "Test");

				mp.Data = null;

				await mp.SaveAsync (Util.GetMonitor ());

				string projectXml1 = File.ReadAllText (Util.GetSampleProject ("extended-project-properties", "test-extra-data.myproj"));

				string projectXml2 = File.ReadAllText (projFile);
				Assert.AreEqual (projectXml1, projectXml2);
				p.Dispose ();

			} finally {
				MSBuildProjectService.UnregisterCustomItemType (tn);
			}
		}

		[Test]
		public async Task FlavorLoadExtendedProperties ()
		{
			string projFile = Util.GetSampleProject ("extended-project-properties", "test-data.myproj");

			var tn = new MyEmptyProjectTypeNode ();
			var fn = new CustomItemNode<FlavorWithData> ();
			MSBuildProjectService.RegisterCustomItemType (tn);
			WorkspaceObject.RegisterCustomExtension (fn);
			try {
				var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				Assert.IsInstanceOf<MyEmptyProject> (p);
				var mp = (MyEmptyProject)p;

				var f = mp.GetFlavor<FlavorWithData> ();
				Assert.NotNull (f.Data);
				Assert.AreEqual (f.Data.Foo, "bar");
				Assert.AreEqual (f.SimpleData, "Test");
				p.Dispose ();
			} finally {
				MSBuildProjectService.UnregisterCustomItemType (tn);
				WorkspaceObject.UnregisterCustomExtension (fn);
			}
		}

		[Test]
		public async Task FlavorLoadExtendedProperties_InitialEmptyGroup ()
		{
			// Check that data load works when it is not defined in the main group
			// Test for BXC 41774.
			string projFile = Util.GetSampleProject ("extended-project-properties", "test-data-empty-group.myproj");

			var tn = new MyEmptyProjectTypeNode ();
			var fn = new CustomItemNode<FlavorWithData> ();
			MSBuildProjectService.RegisterCustomItemType (tn);
			WorkspaceObject.RegisterCustomExtension (fn);
			try {
				var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				Assert.IsInstanceOf<MyEmptyProject> (p);
				var mp = (MyEmptyProject)p;

				var f = mp.GetFlavor<FlavorWithData> ();
				Assert.NotNull (f.Data);
				Assert.AreEqual (f.Data.Foo, "bar");
				Assert.AreEqual (f.SimpleData, "Test");
				p.Dispose ();
			} finally {
				MSBuildProjectService.UnregisterCustomItemType (tn);
				WorkspaceObject.UnregisterCustomExtension (fn);
			}
		}

		[Test]
		public async Task FlavorLoadSaveExtendedPropertiesWithUnknownProperty ()
		{
			// Unknown data should be kept in the file

			string projFile = Util.GetSampleProject ("extended-project-properties", "test-unknown-data.myproj");
			string projectXml1 = File.ReadAllText (projFile);

			var tn = new MyEmptyProjectTypeNode ();
			var fn = new CustomItemNode<FlavorWithData> ();
			MSBuildProjectService.RegisterCustomItemType (tn);
			WorkspaceObject.RegisterCustomExtension (fn);
			try {
				var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				Assert.IsInstanceOf<MyEmptyProject> (p);
				var mp = (MyEmptyProject)p;

				var f = mp.GetFlavor<FlavorWithData> ();
				Assert.NotNull (f.Data);
				Assert.AreEqual (f.Data.Foo, "bar");
				Assert.AreEqual (f.SimpleData, "Test");

				await mp.SaveAsync (Util.GetMonitor ());

				string projectXml2 = File.ReadAllText (projFile);
				Assert.AreEqual (projectXml1, projectXml2);

				p.Dispose ();

			} finally {
				MSBuildProjectService.UnregisterCustomItemType (tn);
				WorkspaceObject.UnregisterCustomExtension (fn);
			}
		}

		[Test]
		public async Task FlavorRemoveExtendedProperties ()
		{
			// Whole ProjectExtensions section should be removed

			string projFile = Util.GetSampleProject ("extended-project-properties", "test-data.myproj");

			var tn = new MyEmptyProjectTypeNode ();
			var fn = new CustomItemNode<FlavorWithData> ();
			MSBuildProjectService.RegisterCustomItemType (tn);
			WorkspaceObject.RegisterCustomExtension (fn);
			try {
				var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				Assert.IsInstanceOf<MyEmptyProject> (p);
				var mp = (MyEmptyProject)p;

				var f = mp.GetFlavor<FlavorWithData> ();
				Assert.NotNull (f.Data);
				Assert.AreEqual (f.Data.Foo, "bar");
				Assert.AreEqual (f.SimpleData, "Test");

				f.Data = null;

				await mp.SaveAsync (Util.GetMonitor ());

				string projectXml1 = File.ReadAllText (Util.GetSampleProject ("extended-project-properties", "test-empty.myproj"));

				string projectXml2 = File.ReadAllText (projFile);
				Assert.AreEqual (projectXml1, projectXml2);

				p.Dispose ();

			} finally {
				MSBuildProjectService.UnregisterCustomItemType (tn);
				WorkspaceObject.UnregisterCustomExtension (fn);
			}
		}

		[Test]
		public async Task FlavorRemoveExtendedPropertiesWithUnknownProperty ()
		{
			// Unknown data should be kept in the file

			string projFile = Util.GetSampleProject ("extended-project-properties", "test-unknown-data.myproj");

			var tn = new MyEmptyProjectTypeNode ();
			var fn = new CustomItemNode<FlavorWithData> ();
			MSBuildProjectService.RegisterCustomItemType (tn);
			WorkspaceObject.RegisterCustomExtension (fn);
			try {
				var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				Assert.IsInstanceOf<MyEmptyProject> (p);
				var mp = (MyEmptyProject)p;

				var f = mp.GetFlavor<FlavorWithData> ();
				Assert.NotNull (f.Data);
				Assert.AreEqual (f.Data.Foo, "bar");
				Assert.AreEqual (f.SimpleData, "Test");

				f.Data = null;

				await mp.SaveAsync (Util.GetMonitor ());

				string projectXml1 = File.ReadAllText (Util.GetSampleProject ("extended-project-properties", "test-extra-data.myproj"));

				string projectXml2 = File.ReadAllText (projFile);
				Assert.AreEqual (projectXml1, projectXml2);

				p.Dispose ();

			} finally {
				MSBuildProjectService.UnregisterCustomItemType (tn);
				WorkspaceObject.UnregisterCustomExtension (fn);
			}
		}

		[Test]
		public async Task LoadAvailableItemName ()
		{
			string projFile = Util.GetSampleProject ("console-project-with-item-types", "ConsoleProject.csproj");

			var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			Assert.IsInstanceOf<Project> (p);
			var mp = (Project)p;

			var actions = mp.GetBuildActions ();

			// The main actions should always be the same and in the same position

			Assert.AreEqual (0, Array.IndexOf (actions, "None"), "'None' not found or in wrong position");
			Assert.AreEqual (1, Array.IndexOf (actions, "Compile"), "'Compile' not found or in wrong position");
			Assert.AreEqual (2, Array.IndexOf (actions, "EmbeddedResource"), "'EmbeddedResource' not found or in wrong position");
			Assert.AreEqual (3, Array.IndexOf (actions, "--"), "'--' not found or in wrong position");

			// The remaining actions may vary depending on the platform, but some of them must be there

			Assert.IsTrue (actions.Contains ("Content"), "'Content' not found");
			Assert.IsTrue (actions.Contains ("ItemOne"), "'ItemOne' not found");
			Assert.IsTrue (actions.Contains ("ItemTwo"), "'ItemTwo' not found");

			p.Dispose ();
		}

		[Test]
		public async Task LoadProjectWithWildcards ()
		{
			string projFile = Util.GetSampleProject ("console-project-with-wildcards", "ConsoleProject.csproj");

			var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			Assert.IsInstanceOf<Project> (p);
			var mp = (Project)p;
			var files = mp.Files.Select (f => f.FilePath.FileName).OrderBy (f => f).ToArray ();
			Assert.AreEqual (new string [] {
				"Data1.cs",
				"Data2.cs",
				"Data3.cs",
				"Program.cs",
				"text1-1.txt",
				"text1-2.txt",
				"text2-1.txt",
				"text2-2.txt",
				"text3-1.txt",
				"text3-2.txt",
			}, files);

			p.Dispose ();
		}

		[Test]
		public async Task LoadProjectWithWildcardsAndExcludes ()
		{
			string projFile = Util.GetSampleProject ("console-project-with-wildcards", "ConsoleProject-with-excludes.csproj");

			var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			Assert.IsInstanceOf<Project> (p);
			var mp = (Project)p;
			var files = mp.Files.Select (f => f.FilePath.FileName).OrderBy (f => f).ToArray ();
			Assert.AreEqual (new string [] {
				"Data2.cs",
				"p1.txt",
				"p4.txt",
				"p5.txt",
				"text3-1.txt",
			}, files);

			p.Dispose ();
		}

		/// <summary>
		/// As above but tests that files with a dotted filename (e.g. 'foo.bar.txt') are
		/// correctly excluded and included.
		/// </summary>
		[Test]
		public async Task LoadProjectWithWildcardsAndExcludes2 ()
		{
			FilePath projFile = Util.GetSampleProject ("console-project-with-wildcards", "ConsoleProject-with-excludes.csproj");

			File.WriteAllText (projFile.ParentDirectory.Combine ("file1.include.txt"), string.Empty);
			File.WriteAllText (projFile.ParentDirectory.Combine ("file2.exclude2.txt"), string.Empty);
			File.WriteAllText (projFile.ParentDirectory.Combine ("Extra", "No", "file3.include.txt"), string.Empty);

			var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			Assert.IsInstanceOf<Project> (p);
			var mp = (Project)p;
			var files = mp.Files.Select (f => f.FilePath.FileName).OrderBy (f => f).ToArray ();
			Assert.AreEqual (new string [] {
				"Data2.cs",
				"file1.include.txt",
				"p1.txt",
				"p4.txt",
				"p5.txt",
				"text3-1.txt",
			}, files);

			p.Dispose ();
		}

		[Test]
		public async Task LoadProjectWithWildcardsAndExcludesUsingForwardSlashInsteadOfBackslash ()
		{
			string projFile = Util.GetSampleProject ("console-project-with-wildcards", "ConsoleProject-with-forward-slash-excludes.csproj");

			var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			Assert.IsInstanceOf<Project> (p);
			var mp = (Project)p;
			var files = mp.Files.Select (f => f.FilePath.FileName).OrderBy (f => f).ToArray ();
			Assert.AreEqual (new string [] {
				"Data2.cs",
				"p1.txt",
				"p4.txt",
				"p5.txt",
				"text3-1.txt",
			}, files);

			p.Dispose ();
		}

		[Test]
		public async Task LoadProjectWithWildcardsAndExcludesUsingPropertyPathThatHasTrailingBackslash ()
		{
			string projFile = Util.GetSampleProject ("console-project-with-wildcards", "ConsoleProject-with-property-trailing-slash-excludes.csproj");

			var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			Assert.IsInstanceOf<Project> (p);
			var mp = (Project)p;
			var files = mp.Files.Select (f => f.FilePath.FileName).OrderBy (f => f).ToArray ();
			Assert.AreEqual (new string [] {
				"Data2.cs",
				"p1.txt",
				"p4.txt",
				"p5.txt",
				"text3-1.txt",
			}, files);

			p.Dispose ();
		}

		/// <summary>
		/// EnableDefaultItems set to false in project. C# file directly in
		/// project. The Sdk imports define C# files if EnableDefaultItems is true.
		/// This test ensures that duplicate files are not added to the project. This
		/// happens because Project.EvaluatedItemsIgnoringCondition is used when adding
		/// files to the project.
		/// </summary>
		[Test]
		public async Task LoadDotNetCoreProjectWithDefaultItemsDisabled ()
		{
			FilePath solFile = Util.GetSampleProject ("dotnetcore-console", "dotnetcore-disable-default-items.sln");
			FilePath sdksPath = solFile.ParentDirectory.Combine ("Sdks");
			MSBuildProjectService.RegisterProjectImportSearchPath ("MSBuildSDKsPath", sdksPath);

			try {
				var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
				var p = (Project)sol.Items [0];
				Assert.IsInstanceOf<Project> (p);
				var mp = (Project)p;
				var files = mp.Files.Select (f => f.FilePath.FileName).ToArray ();
				Assert.AreEqual (new string [] {
					"Program.cs"
				}, files);

				sol.Dispose ();
			} finally {
				MSBuildProjectService.UnregisterProjectImportSearchPath ("MSBuildSDKsPath", sdksPath);
			}
		}

		[Test]
		public async Task AddFileToDotNetCoreProjectWithDefaultItemsDisabled ()
		{
			FilePath solFile = Util.GetSampleProject ("dotnetcore-console", "dotnetcore-disable-default-items.sln");
			FilePath sdksPath = solFile.ParentDirectory.Combine ("Sdks");
			MSBuildProjectService.RegisterProjectImportSearchPath ("MSBuildSDKsPath", sdksPath);

			try {
				var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
				var p = (Project)sol.Items [0];
				Assert.IsInstanceOf<Project> (p);
				var mp = (Project)p;
				var files = mp.Files.Select (f => f.FilePath.FileName).ToArray ();
				Assert.AreEqual (new string [] {
					"Program.cs"
				}, files);

				string newFile = mp.Files[0].FilePath.ChangeName ("NewFile");
				File.WriteAllText (newFile, string.Empty);
				mp.AddFile (newFile);
				await mp.SaveAsync (Util.GetMonitor ());

				var itemGroup = mp.MSBuildProject.ItemGroups.LastOrDefault ();
				Assert.IsTrue (itemGroup.Items.Any (item => item.Include == "NewFile.cs"));
				Assert.AreEqual (2, itemGroup.Items.Count ());
				sol.Dispose ();
			} finally {
				MSBuildProjectService.UnregisterProjectImportSearchPath ("MSBuildSDKsPath", sdksPath);
			}
		}

		[Test]
		public async Task GeneratedNuGetMSBuildFilesAreImportedWithDotNetCoreProject ()
		{
			FilePath solFile = Util.GetSampleProject ("dotnetcore-console", "dotnetcore-disable-default-items.sln");
			FilePath sdksPath = solFile.ParentDirectory.Combine ("Sdks");
			MSBuildProjectService.RegisterProjectImportSearchPath ("MSBuildSDKsPath", sdksPath);
			FilePath baseIntermediateOutputPath = solFile.ParentDirectory.Combine ("dotnetcore-console", "obj");
			string projectFileName = "dotnetcore-disable-default-items.csproj";

			try {
				string nugetProps =
					"<Project xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">\r\n" +
					"  <PropertyGroup>\r\n" +
					"    <NuGetPropsImported>True</NuGetPropsImported>\r\n" +
					"  </PropertyGroup>\r\n" +
					"</Project>";

				string nugetTargets =
					"<Project xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">\r\n" +
					"  <PropertyGroup>\r\n" +
					"    <NuGetTargetsImported>True</NuGetTargetsImported>\r\n" +
					"  </PropertyGroup>\r\n" +
					"</Project>";

				Directory.CreateDirectory (baseIntermediateOutputPath);
				File.WriteAllText (baseIntermediateOutputPath.Combine (projectFileName + ".nuget.g.props"), nugetProps);
				File.WriteAllText (baseIntermediateOutputPath.Combine (projectFileName + ".nuget.g.targets"), nugetTargets);

				var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
				var p = (Project)sol.Items [0];
				Assert.IsInstanceOf<Project> (p);
				var mp = (Project)p;

				Assert.AreEqual ("True", p.MSBuildProject.EvaluatedProperties.GetValue ("NuGetPropsImported"));
				Assert.AreEqual ("True", p.MSBuildProject.EvaluatedProperties.GetValue ("NuGetTargetsImported"));

				sol.Dispose ();
			} finally {
				MSBuildProjectService.UnregisterProjectImportSearchPath ("MSBuildSDKsPath", sdksPath);
			}
		}

		[Test]
		public async Task SaveProjectWithWildcards ()
		{
			string projFile = Util.GetSampleProject ("console-project-with-wildcards", "ConsoleProject.csproj");

			var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			Assert.IsInstanceOf<Project> (p);
			var mp = (Project)p;
			mp.AddFile (Path.Combine (p.BaseDirectory, "Test.cs"));

			await p.SaveAsync (Util.GetMonitor ());

			Assert.AreEqual (Util.ToSystemEndings (File.ReadAllText (p.FileName + ".saved1")), File.ReadAllText (p.FileName));

			p.Dispose ();
		}

		[Test]
		public async Task SaveProjectWithWildcardsRemovingFile ()
		{
			string projFile = Util.GetSampleProject ("console-project-with-wildcards", "ConsoleProject.csproj");

			var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			Assert.IsInstanceOf<Project> (p);
			var mp = (Project)p;

			var f = mp.Files.FirstOrDefault (pf => pf.FilePath.FileName == "Data1.cs");
			mp.Files.Remove (f);

			f = mp.Files.FirstOrDefault (pf => pf.FilePath.FileName == "text1-1.txt");
			f.CopyToOutputDirectory = FileCopyMode.PreserveNewest;

			await p.SaveAsync (Util.GetMonitor ());

			Assert.AreEqual (Util.ToSystemEndings (File.ReadAllText (p.FileName + ".saved2")), File.ReadAllText (p.FileName));

			p.Dispose ();
		}

		[Test]
		public async Task SaveProjectWithWildcardsAfterBuildActionChanged ()
		{
			string projFile = Util.GetSampleProject ("console-project-with-wildcards", "ConsoleProject.csproj");

			var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			Assert.IsInstanceOf<Project> (p);
			var mp = (Project)p;
			mp.UseAdvancedGlobSupport = true;

			// Changing the text1-1.txt. file to EmbeddedResource should result in the following
			// being added:
			//
			// <Content Remove="Content\text1-1.txt" />
			// <EmbeddedResource Include="Content\text1-1.txt" />
			var f = mp.Files.FirstOrDefault (pf => pf.FilePath.FileName == "text1-1.txt");
			f.BuildAction = BuildAction.EmbeddedResource;

			await p.SaveAsync (Util.GetMonitor ());

			Assert.AreEqual (Util.ReadAllWithWindowsEndings (p.FileName + ".saved3"), Util.ReadAllWithWindowsEndings (p.FileName));

			p.Dispose ();
		}

		[Test]
		public async Task SaveProjectWithWildcardsBuildActionChangedThenCopyToOutputChanged ()
		{
			string projFile = Util.GetSampleProject ("console-project-with-wildcards", "ConsoleProject.csproj");

			var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			Assert.IsInstanceOf<Project> (p);
			var mp = (Project)p;
			mp.UseAdvancedGlobSupport = true;

			var f = mp.Files.FirstOrDefault (pf => pf.FilePath.FileName == "text1-1.txt");
			f.BuildAction = BuildAction.EmbeddedResource;
			await p.SaveAsync (Util.GetMonitor ());

			f.CopyToOutputDirectory = FileCopyMode.PreserveNewest;
			await p.SaveAsync (Util.GetMonitor ());

			Assert.AreEqual (Util.ReadAllWithWindowsEndings (p.FileName + ".saved4"), Util.ReadAllWithWindowsEndings (p.FileName));

			p.Dispose ();
		}

		[Test]
		public async Task SaveProjectWithImportedWildcardsBuildActionChangedThenCopyToOutputChanged ()
		{
			var fn = new CustomItemNode<SupportImportedProjectFilesDotNetProjectExtension> ();
			WorkspaceObject.RegisterCustomExtension (fn);

			try {
				string projFile = Util.GetSampleProject ("console-project-with-wildcards", "ConsoleProject-import.csproj");

				var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				Assert.IsInstanceOf<Project> (p);
				var mp = (Project)p;
				mp.UseAdvancedGlobSupport = true;

				var f = mp.Files.FirstOrDefault (pf => pf.FilePath.FileName == "text1-1.txt");
				f.BuildAction = BuildAction.EmbeddedResource;
				await p.SaveAsync (Util.GetMonitor ());

				f.CopyToOutputDirectory = FileCopyMode.PreserveNewest;
				await p.SaveAsync (Util.GetMonitor ());

				Assert.AreEqual (Util.ReadAllWithWindowsEndings (p.FileName + ".saved1"), Util.ReadAllWithWindowsEndings (p.FileName));

				p.Dispose ();
			} finally {
				WorkspaceObject.UnregisterCustomExtension (fn);
			}
		}

		[Test]
		public async Task SaveProjectWithWildcardsBuildActionChangedProjectReloadThenCopyToOutputChanged ()
		{
			string projFile = Util.GetSampleProject ("console-project-with-wildcards", "ConsoleProject.csproj");

			var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			Assert.IsInstanceOf<Project> (p);
			var mp = (Project)p;
			mp.UseAdvancedGlobSupport = true;

			var f = mp.Files.FirstOrDefault (pf => pf.FilePath.FileName == "text1-1.txt");
			f.BuildAction = BuildAction.EmbeddedResource;
			await p.SaveAsync (Util.GetMonitor ());

			p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			mp = (Project)p;
			mp.UseAdvancedGlobSupport = true;
			f = mp.Files.FirstOrDefault (pf => pf.FilePath.FileName == "text1-1.txt");
			f.CopyToOutputDirectory = FileCopyMode.PreserveNewest;
			await p.SaveAsync (Util.GetMonitor ());

			Assert.AreEqual (Util.ReadAllWithWindowsEndings (p.FileName + ".saved4"), Util.ReadAllWithWindowsEndings (p.FileName));

			p.Dispose ();
		}

		[Test]
		public async Task SaveProjectWithWildcardsBuildActionChangedThenCopyToOutputChangedRemoved ()
		{
			string projFile = Util.GetSampleProject ("console-project-with-wildcards", "ConsoleProject.csproj");

			var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			Assert.IsInstanceOf<Project> (p);
			var mp = (Project)p;
			mp.UseAdvancedGlobSupport = true;

			var f = mp.Files.FirstOrDefault (pf => pf.FilePath.FileName == "text1-1.txt");
			f.BuildAction = BuildAction.EmbeddedResource;
			f.CopyToOutputDirectory = FileCopyMode.PreserveNewest;
			await p.SaveAsync (Util.GetMonitor ());

			f.CopyToOutputDirectory = FileCopyMode.None;
			await p.SaveAsync (Util.GetMonitor ());

			Assert.AreEqual (Util.ReadAllWithWindowsEndings (p.FileName + ".saved3"), Util.ReadAllWithWindowsEndings (p.FileName));

			p.Dispose ();
		}

		/// <summary>
		/// If an MSBuild item has a property on loading then if all the properties are removed the 
		/// project file when saved will still have an end element. So this test uses a different
		/// .saved5 file compared with the previous test and includes the extra end tag for the
		/// EmbeddedResource.
		/// </summary>
		[Test]
		public async Task SaveProjectWithWildcardsBuildActionChangedThenCopyToOutputChangedRemovedAfterReload ()
		{
			string projFile = Util.GetSampleProject ("console-project-with-wildcards", "ConsoleProject.csproj");

			var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			Assert.IsInstanceOf<Project> (p);
			var mp = (Project)p;
			mp.UseAdvancedGlobSupport = true;

			var f = mp.Files.FirstOrDefault (pf => pf.FilePath.FileName == "text1-1.txt");
			f.BuildAction = BuildAction.EmbeddedResource;
			f.CopyToOutputDirectory = FileCopyMode.PreserveNewest;
			await p.SaveAsync (Util.GetMonitor ());

			p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			mp = (Project)p;
			mp.UseAdvancedGlobSupport = true;
			f = mp.Files.FirstOrDefault (pf => pf.FilePath.FileName == "text1-1.txt");
			f.CopyToOutputDirectory = FileCopyMode.None;
			await p.SaveAsync (Util.GetMonitor ());

			Assert.AreEqual (Util.ReadAllWithWindowsEndings (p.FileName + ".saved5"), Util.ReadAllWithWindowsEndings (p.FileName));

			p.Dispose ();
		}

		[Test]
		public async Task SaveProjectWithWildcardsBuildActionChangedBackAgain ()
		{
			string projFile = Util.GetSampleProject ("console-project-with-wildcards", "ConsoleProject.csproj");
			string originalProjectFileText = File.ReadAllText (projFile);

			var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			Assert.IsInstanceOf<Project> (p);
			var mp = (Project)p;
			mp.UseAdvancedGlobSupport = true;

			var f = mp.Files.FirstOrDefault (pf => pf.FilePath.FileName == "text1-1.txt");
			var originalBuildAction = f.BuildAction;
			f.BuildAction = BuildAction.EmbeddedResource;
			await p.SaveAsync (Util.GetMonitor ());

			f.BuildAction = originalBuildAction;
			await p.SaveAsync (Util.GetMonitor ());

			Assert.AreEqual (Util.ToSystemEndings (originalProjectFileText), File.ReadAllText (p.FileName));

			p.Dispose ();
		}

		[Test]
		public async Task SaveProjectWithWildcardsBuildActionChangedBackAgainAfterReload ()
		{
			string projFile = Util.GetSampleProject ("console-project-with-wildcards", "ConsoleProject.csproj");
			string originalProjectFileText = File.ReadAllText (projFile);

			var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			Assert.IsInstanceOf<Project> (p);
			var mp = (Project)p;
			mp.UseAdvancedGlobSupport = true;

			var f = mp.Files.FirstOrDefault (pf => pf.FilePath.FileName == "text1-1.txt");
			var originalBuildAction = f.BuildAction;
			f.BuildAction = BuildAction.EmbeddedResource;
			await p.SaveAsync (Util.GetMonitor ());

			p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			mp = (Project)p;
			mp.UseAdvancedGlobSupport = true;
			f = mp.Files.FirstOrDefault (pf => pf.FilePath.FileName == "text1-1.txt");
			f.BuildAction = originalBuildAction;
			await p.SaveAsync (Util.GetMonitor ());

			Assert.AreEqual (Util.ToSystemEndings (originalProjectFileText), File.ReadAllText (p.FileName));

			p.Dispose ();
		}

		/// <summary>
		/// Changed BuildAction include has an CopyToOutputDirectory property. After reverting
		/// the BuildAction the Remove and Include item should be removed but an Update
		/// item should be added with the CopyToOutputDirectory property.
		/// </summary>
		[Test]
		public async Task SaveProjectWithWildcardsBuildActionChangedBackAgain2 ()
		{
			string projFile = Util.GetSampleProject ("console-project-with-wildcards", "ConsoleProject.csproj");

			var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			Assert.IsInstanceOf<Project> (p);
			var mp = (Project)p;
			mp.UseAdvancedGlobSupport = true;

			var f = mp.Files.FirstOrDefault (pf => pf.FilePath.FileName == "text1-1.txt");
			var originalBuildAction = f.BuildAction;
			f.BuildAction = BuildAction.EmbeddedResource;
			f.CopyToOutputDirectory = FileCopyMode.PreserveNewest;
			await p.SaveAsync (Util.GetMonitor ());

			f.BuildAction = originalBuildAction;
			await p.SaveAsync (Util.GetMonitor ());

			Assert.AreEqual (Util.ReadAllWithWindowsEndings (p.FileName + ".saved6"), Util.ReadAllWithWindowsEndings (p.FileName));

			p.Dispose ();
		}

		[Test]
		public async Task SaveProjectWithWildcardsBuildActionChangedBackAgainAfterReload2 ()
		{
			string projFile = Util.GetSampleProject ("console-project-with-wildcards", "ConsoleProject.csproj");

			var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			Assert.IsInstanceOf<Project> (p);
			var mp = (Project)p;
			mp.UseAdvancedGlobSupport = true;

			var f = mp.Files.FirstOrDefault (pf => pf.FilePath.FileName == "text1-1.txt");
			var originalBuildAction = f.BuildAction;
			f.BuildAction = BuildAction.EmbeddedResource;
			f.CopyToOutputDirectory = FileCopyMode.PreserveNewest;
			await p.SaveAsync (Util.GetMonitor ());

			p.Dispose ();

			p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			mp = (Project)p;
			mp.UseAdvancedGlobSupport = true;
			f = mp.Files.FirstOrDefault (pf => pf.FilePath.FileName == "text1-1.txt");
			f.BuildAction = originalBuildAction;
			await p.SaveAsync (Util.GetMonitor ());

			Assert.AreEqual (Util.ReadAllWithWindowsEndings (p.FileName + ".saved6"), Util.ReadAllWithWindowsEndings (p.FileName));

			// Save again to make sure another Update item is not added.
			await p.SaveAsync (Util.GetMonitor ());

			Assert.AreEqual (Util.ReadAllWithWindowsEndings (p.FileName + ".saved6"), Util.ReadAllWithWindowsEndings (p.FileName));

			p.Dispose ();
		}

		/// <summary>
		/// The globs are defined in a file that is imported into the project.
		/// </summary>
		[Test]
		public async Task SaveProjectWithImportedWildcardsBuildActionChangedBackAgain ()
		{
			var fn = new CustomItemNode<SupportImportedProjectFilesDotNetProjectExtension> ();
			WorkspaceObject.RegisterCustomExtension (fn);

			try {
				string projFile = Util.GetSampleProject ("console-project-with-wildcards", "ConsoleProject-import.csproj");
				string originalProjectFileText = File.ReadAllText (projFile);

				var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				Assert.IsInstanceOf<Project> (p);
				var mp = (Project)p;
				mp.UseAdvancedGlobSupport = true;

				var f = mp.Files.FirstOrDefault (pf => pf.FilePath.FileName == "text1-1.txt");
				var originalBuildAction = f.BuildAction;
				f.BuildAction = BuildAction.EmbeddedResource;
				await p.SaveAsync (Util.GetMonitor ());

				f.BuildAction = originalBuildAction;
				await p.SaveAsync (Util.GetMonitor ());

				Assert.AreEqual (Util.ToSystemEndings (originalProjectFileText), File.ReadAllText (p.FileName));

				p.Dispose ();
			} finally {
				WorkspaceObject.UnregisterCustomExtension (fn);
			}
		}

		/// <summary>
		/// Tests that the C# file build action can be changed to None with globs:
		///
		/// None Include="**/*"
		/// None Remove="**/*.cs"
		/// Compile Include="**/*.cs"
		/// </summary>
		[Test]
		public async Task CSharpFileBuildActionChangedToNone ()
		{
			var fn = new CustomItemNode<SupportImportedProjectFilesDotNetProjectExtension> ();
			WorkspaceObject.RegisterCustomExtension (fn);

			try {
				string projFile = Util.GetSampleProject ("console-project-with-wildcards", "ConsoleProject-imported-none-wildcard.csproj");

				var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				Assert.IsInstanceOf<Project> (p);
				var mp = (Project)p;
				mp.UseAdvancedGlobSupport = true;

				// Changing the Program.cs file to None should result in the following
				// being added:
				//
				// <Compile Remove="Program.cs" />
				// <None Include="Program.cs" />
				var f = mp.Files.FirstOrDefault (pf => pf.FilePath.FileName == "Program.cs");
				f.BuildAction = BuildAction.None;

				await p.SaveAsync (Util.GetMonitor ());

				Assert.AreEqual (Util.ReadAllWithWindowsEndings (p.FileName + ".saved1"), Util.ReadAllWithWindowsEndings (p.FileName));

				p.Dispose ();
			} finally {
				WorkspaceObject.UnregisterCustomExtension (fn);
			}
		}

		/// <summary>
		/// As above but the build action is changed to None, then back to Compile,
		/// then back to None again. The project is saved on each change.
		/// </summary>
		[Test]
		public async Task CSharpFileBuildActionChangedToNoneBackToCompileBackToNoneAgain ()
		{
			var fn = new CustomItemNode<SupportImportedProjectFilesDotNetProjectExtension> ();
			WorkspaceObject.RegisterCustomExtension (fn);

			try {
				string projFile = Util.GetSampleProject ("console-project-with-wildcards", "ConsoleProject-imported-none-wildcard.csproj");

				var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				Assert.IsInstanceOf<Project> (p);
				var mp = (Project)p;
				mp.UseAdvancedGlobSupport = true;

				var f = mp.Files.FirstOrDefault (pf => pf.FilePath.FileName == "Program.cs");
				f.BuildAction = BuildAction.None;
				await p.SaveAsync (Util.GetMonitor ());

				f = mp.Files.FirstOrDefault (pf => pf.FilePath.FileName == "Program.cs");
				f.BuildAction = BuildAction.Compile;
				await p.SaveAsync (Util.GetMonitor ());

				f = mp.Files.FirstOrDefault (pf => pf.FilePath.FileName == "Program.cs");
				f.BuildAction = BuildAction.None;
				await p.SaveAsync (Util.GetMonitor ());

				Assert.AreEqual (Util.ReadAllWithWindowsEndings (p.FileName + ".saved1"), Util.ReadAllWithWindowsEndings (p.FileName));

				p.Dispose ();
			} finally {
				WorkspaceObject.UnregisterCustomExtension (fn);
			}
		}

		[Test]
		//[Ignore ("xbuild bug: RecursiveDir metadata returns the wrong value")]
		public async Task LoadProjectWithWildcardLinks ()
		{
			string solFile = Util.GetSampleProject ("project-with-wildcard-links", "PortableTest.sln");

			var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

			var mp = (Project)sol.Items [0];
			Assert.AreEqual (7, mp.Files.Count);

			var f1 = mp.Files.FirstOrDefault (pf => pf.FilePath.FileName == "Xamagon_1.png");
			var f2 = mp.Files.FirstOrDefault (pf => pf.FilePath.FileName == "Xamagon_2.png");

			Assert.AreEqual (Path.GetFullPath (Path.Combine (mp.BaseDirectory, "..", "test", "Xamagon_1.png")), Path.GetFullPath (f1.FilePath));
			Assert.AreEqual (Path.GetFullPath (Path.Combine (mp.BaseDirectory, "..", "test", "Subdir", "Xamagon_2.png")), Path.GetFullPath (f2.FilePath));

			Assert.AreEqual ("Xamagon_1.png", f1.Link.ToString ());
			Assert.AreEqual (Path.Combine ("Subdir", "Xamagon_2.png"), f2.Link.ToString ());

			sol.Dispose ();
		}

		[Test]
		public async Task LoadProjectWithWildcardLinks2 ()
		{
			// Merge with LoadProjectWithWildcardLinks test when the xbuild issue is fixed

			string solFile = Util.GetSampleProject ("project-with-wildcard-links", "PortableTest.sln");

			var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

			var mp = (Project)sol.Items [0];

			var f1 = mp.Files.FirstOrDefault (pf => pf.FilePath.FileName == "t1.txt");
			Assert.IsNotNull (f1);

			var f2 = mp.Files.FirstOrDefault (pf => pf.FilePath.FileName == "t2.txt");
			Assert.IsNotNull (f2);

			Assert.AreEqual (Path.GetFullPath (Path.Combine (mp.BaseDirectory, "..", "test", "t1.txt")), Path.GetFullPath (f1.FilePath));
			Assert.AreEqual (Path.GetFullPath (Path.Combine (mp.BaseDirectory, "..", "test", "t2.txt")), Path.GetFullPath (f2.FilePath));

			Assert.AreEqual (Path.Combine ("Data", "t1.txt"), f1.Link.ToString ());
			Assert.AreEqual (Path.Combine ("Data", "t2.txt"), f2.Link.ToString ());

			sol.Dispose ();
		}

		[Test]
		public async Task LoadProjectWithWildcardLinks3 ()
		{
			// %(RecursiveDir) is empty when used in a non-recursive include

			string solFile = Util.GetSampleProject ("project-with-wildcard-links", "PortableTest.sln");

			var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

			var mp = (Project)sol.Items [0];

			var f1 = mp.Files.FirstOrDefault (pf => pf.FilePath.FileName == "t1.dat");
			Assert.IsNotNull (f1);

			var f2 = mp.Files.FirstOrDefault (pf => pf.FilePath.FileName == "t2.dat");
			Assert.IsNotNull (f2);

			Assert.AreEqual (Path.GetFullPath (Path.Combine (mp.BaseDirectory, "..", "test", "t1.dat")), Path.GetFullPath (f1.FilePath));
			Assert.AreEqual (Path.GetFullPath (Path.Combine (mp.BaseDirectory, "..", "test", "t2.dat")), Path.GetFullPath (f2.FilePath));

			Assert.AreEqual ("t1.dat", f1.Link.ToString ());
			Assert.AreEqual ("t2.dat", f2.Link.ToString ());

			sol.Dispose ();
		}

		[Test]
		public async Task LoadProjectWithWildcardLinks4 ()
		{
			// %(RecursiveDir) is empty when used in a non-recursive include with a single file

			string solFile = Util.GetSampleProject ("project-with-wildcard-links", "PortableTest.sln");

			var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

			var mp = (Project)sol.Items [0];

			var f = mp.Files.FirstOrDefault (pf => pf.FilePath.FileName == "other.rst");

			Assert.IsNotNull(f);
			Assert.AreEqual("other.rst", f.Link.ToString());

			sol.Dispose();
		}

		/// <summary>
		/// Tests that an include such as "Properties\**" does not throw an ArgumentException
		/// and resolves all files in all subdirectories.
		/// </summary>
		[Test]
		public async Task LoadProjectWithPathWildcard ()
		{
			string projFile = Util.GetSampleProject ("console-project-with-wildcards", "ConsoleProject-with-path-wildcard.csproj");

			var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			Assert.IsInstanceOf<Project> (p);
			var mp = (Project)p;
			var files = mp.Files.Select (f => f.FilePath.FileName).OrderBy (f => f).ToArray ();
			Assert.AreEqual (new string [] {
				"Data1.cs",
				"Data2.cs",
				"Data3.cs",
				"Program.cs",
				"text1-1.txt",
				"text1-2.txt",
				"text2-1.txt",
				"text2-2.txt",
			}, files);

			p.Dispose ();
		}

		/// <summary>
		/// Tests that an update such as "Properties\**" does not throw an ArgumentException.
		/// </summary>
		[Test]
		public async Task LoadProjectWithPathWildcardUpdate ()
		{
			string projFile = Util.GetSampleProject ("console-project-with-wildcards", "ConsoleProject-with-path-wildcard-update.csproj");

			var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			Assert.IsInstanceOf<Project> (p);
			var mp = (Project)p;
			var files = mp.Files.Select (f => f.FilePath.FileName).OrderBy (f => f).ToArray ();
			Assert.AreEqual (new string [] {
				"Program.cs",
				"text1-1.txt",
				"text1-2.txt",
				"text2-1.txt",
				"text2-2.txt",
			}, files);

			var filesToCopyToOutputDirectory = mp.Files
				.Where (f => f.CopyToOutputDirectory == FileCopyMode.PreserveNewest)
				.Select (f => f.FilePath.FileName)
				.OrderBy (f => f).ToArray ();

			Assert.AreEqual (new string [] {
				"text1-1.txt",
				"text1-2.txt",
				"text2-1.txt",
				"text2-2.txt",
			}, filesToCopyToOutputDirectory);

			p.Dispose ();
		}

		/// <summary>
		/// Tests that an include such as "Properties/**/*.txt" does not throw an ArgumentException
		/// and resolves all files in all subdirectories.
		/// </summary>
		[Test]
		public async Task LoadProjectWithForwardSlashWildcard ()
		{
			string projFile = Util.GetSampleProject ("console-project-with-wildcards", "ConsoleProject-with-forward-slash-wildcard-update.csproj");

			var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			Assert.IsInstanceOf<Project> (p);
			var mp = (Project)p;
			var files = mp.Files
				.Where (f => f.CopyToOutputDirectory == FileCopyMode.PreserveNewest)
				.Select (f => f.FilePath.FileName)
				.OrderBy (f => f).ToArray ();
			Assert.AreEqual (new string [] {
				"text1-1.txt",
				"text1-2.txt",
				"text2-1.txt",
				"text2-2.txt",
			}, files);

			p.Dispose ();
		}

		/// <summary>
		/// Tests that an include such as "Properties/**/*.txt" does not throw an ArgumentException
		/// when the project is saved and UseAdvancedGlobSupport is enabled.
		/// </summary>
		[Test]
		public async Task SaveAdvancedGlobSupportProjectWithForwardSlashWildcard ()
		{
			string projFile = Util.GetSampleProject ("console-project-with-wildcards", "ConsoleProject-with-forward-slash-wildcard-update.csproj");

			var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			Assert.IsInstanceOf<Project> (p);
			var mp = (Project)p;
			mp.UseAdvancedGlobSupport = true;

			string newFile = Path.Combine (p.BaseDirectory, "Content", "newfile.txt");
			File.WriteAllText (newFile, "text");
			mp.AddFile (newFile, "Content");
			await mp.SaveAsync (Util.GetMonitor ());

			mp = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile) as Project;

			var files = mp.Files
				.Where (f => f.CopyToOutputDirectory == FileCopyMode.PreserveNewest)
				.Select (f => f.FilePath.FileName)
				.OrderBy (f => f).ToArray ();
			Assert.AreEqual (new string [] {
				"newfile.txt",
				"text1-1.txt",
				"text1-2.txt",
				"text2-1.txt",
				"text2-2.txt",
			}, files);

			var itemGroup = mp.MSBuildProject.ItemGroups.LastOrDefault ();
			Assert.AreEqual (2, mp.MSBuildProject.ItemGroups.Count ());
			Assert.IsFalse (itemGroup.Items.Any (item => item.Include == @"Content\newfile.txt"));
			Assert.AreEqual (3, itemGroup.Items.Count ());

			p.Dispose ();
		}

		/// <summary>
		/// Wildcard files added by imports should be added using the project's
		/// base directory and not the directory of the import itself.
		/// </summary>
		[Test]
		public async Task LoadProjectWithImportedWildcard ()
		{
			string projFile = Util.GetSampleProject ("console-project-with-wildcards", "ConsoleProject-imported-wildcard.csproj");

			var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			Assert.IsInstanceOf<Project> (p);
			var mp = (Project)p;
			var files = mp.MSBuildProject.EvaluatedItems.Where (item => item.Name == "Compile")
				.Select (item => item.Include).OrderBy (f => f).ToArray ();
			Assert.AreEqual (new string [] {
				@"Content\Data\Data1.cs",
				@"Content\Data\Data2.cs",
				@"Content\Data3.cs",
				"Program.cs"
			}, files);

			p.Dispose ();
		}

		/// <summary>
		/// Checks the imported wildcard prevents the new .cs file from being
		/// added to the project file when UseAdvancedGlobSupport is enabled.
		/// </summary>
		[Test]
		public async Task AddCSharpFileToProjectWithImportedCSharpFilesWildcard ()
		{
			string projFile = Util.GetSampleProject ("console-project-with-wildcards", "ConsoleProject-imported-wildcard.csproj");

			var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			Assert.IsInstanceOf<Project> (p);
			var mp = (Project)p;
			mp.UseAdvancedGlobSupport = true;

			string newFile = Path.Combine (p.BaseDirectory, "Test.cs");
			File.WriteAllText (newFile, "class Test { }");
			mp.AddFile (newFile);
			await mp.SaveAsync (Util.GetMonitor ());

			mp = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile) as Project;
			var itemGroup = mp.MSBuildProject.ItemGroups.FirstOrDefault ();
			Assert.AreEqual (1, mp.MSBuildProject.ItemGroups.Count ());
			Assert.IsFalse (itemGroup.Items.Any (item => item.Name != "Reference"));

			p.Dispose ();
		}

		[Test]
		public async Task DeleteFileAndThenAddNewFileToProjectWithSingleFileAndImportedCSharpFilesWildcard ()
		{
			var fn = new CustomItemNode<SupportImportedProjectFilesDotNetProjectExtension> ();
			WorkspaceObject.RegisterCustomExtension (fn);

			try {
				string projFile = Util.GetSampleProject ("console-project-imported-wildcard", "ConsoleProject-imported-wildcard.csproj");
				string originalProjectFileText = File.ReadAllText (projFile);

				var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				Assert.IsInstanceOf<Project> (p);
				var mp = (Project)p;
				mp.UseAdvancedGlobSupport = true;

				var f = mp.Files.Single ();
				Assert.AreEqual ("Program.cs", f.FilePath.FileName);
				string fileToDelete = f.FilePath;
				File.Delete (fileToDelete);
				mp.Files.Remove (f);
				await mp.SaveAsync (Util.GetMonitor ());

				string newFile = Path.Combine (p.BaseDirectory, "Test.cs");
				File.WriteAllText (newFile, "class Test { }");
				mp.AddFile (newFile);
				await mp.SaveAsync (Util.GetMonitor ());

				// Second save was triggering a null reference.
				await mp.SaveAsync (Util.GetMonitor ());

				var savedProjFileText = File.ReadAllText (projFile);
				Assert.AreEqual (originalProjectFileText, savedProjFileText);

				p.Dispose ();
			} finally {
				WorkspaceObject.UnregisterCustomExtension (fn);
			}
		}

		[Test]
		public async Task DeleteAllFilesIncludingWildcardItems ()
		{
			string projFile = Util.GetSampleProject ("console-project-with-wildcards", "ConsoleProject.csproj");

			var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			Assert.IsInstanceOf<Project> (p);
			var mp = (Project)p;
			mp.UseAdvancedGlobSupport = true;

			mp.Files.Clear ();
			await p.SaveAsync (Util.GetMonitor ());

			Assert.AreEqual (Util.ToSystemEndings (File.ReadAllText (p.FileName + ".saved7")), File.ReadAllText (p.FileName));

			p.Dispose ();
		}

		/// <summary>
		/// Checks that the remove applies to items using the root project as the
		/// starting point.
		/// </summary>
		[Test]
		public async Task LoadProjectWithImportedWildcardAndItemRemove ()
		{
			string projFile = Util.GetSampleProject ("console-project-with-wildcards", "ConsoleProject-imported-wildcard-remove.csproj");

			var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			Assert.IsInstanceOf<Project> (p);
			var mp = (Project)p;
			var files = mp.MSBuildProject.EvaluatedItems.Where (item => item.Name == "None")
				.Select (item => item.Include).OrderBy (f => f).ToArray ();
			Assert.AreEqual (new string [] {
				@"Content\Data\text2-1.txt",
				@"Content\Data\text2-2.txt",
				@"Content\text1-1.txt",
				@"Content\text1-2.txt"
			}, files);

			p.Dispose ();
		}

		/// <summary>
		/// Checks that a remove item defined in another import will affect
		/// items added by another import.
		/// </summary>
		[Test]
		public async Task LoadProjectWithImportedWildcardAndSeparateItemRemove ()
		{
			string projFile = Util.GetSampleProject ("console-project-with-wildcards", "ConsoleProject-imported-wildcard-separate-remove.csproj");

			var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			Assert.IsInstanceOf<Project> (p);
			var mp = (Project)p;
			var files = mp.MSBuildProject.EvaluatedItems.Where (item => item.Name == "Compile")
				.Select (item => item.Include).OrderBy (f => f).ToArray ();
			Assert.AreEqual (new string [] {
				@"Content\Data3.cs",
				"Program.cs"
			}, files);

			p.Dispose ();
		}

		/// <summary>
		/// Checks that the remove applies to items using the root project as the
		/// starting point.
		/// </summary>
		[Test]
		public async Task LoadProjectWithImportedWildcardAndItemUpdate ()
		{
			string projFile = Util.GetSampleProject ("console-project-with-wildcards", "ConsoleProject-imported-wildcard-update.csproj");

			var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			Assert.IsInstanceOf<Project> (p);
			var mp = (Project)p;
			var files = mp.MSBuildProject.EvaluatedItems.Where (item => item.Name == "None")
				.Select (item => item.Include).OrderBy (f => f).ToArray ();
			Assert.AreEqual (new string [] {
				@"Content\Data\text2-1.txt",
				@"Content\Data\text2-2.txt",
				@"Content\text1-1.txt",
				@"Content\text1-2.txt"
			}, files);

			var copyToOutputDirectory = mp.MSBuildProject.EvaluatedItems.Where (item => item.Name == "None")
				.Select (item => item.Metadata.GetValue ("CopyToOutputDirectory")).ToArray ();
			Assert.IsTrue (copyToOutputDirectory.All (propertyValue => propertyValue == "PreserveNewest"));

			p.Dispose ();
		}

		/// <summary>
		/// Checks that an update item from a separate import affects items that
		/// have already been included.
		/// </summary>
		[Test]
		public async Task LoadProjectWithImportedWildcardAndSeparateItemUpdate ()
		{
			string projFile = Util.GetSampleProject ("console-project-with-wildcards", "ConsoleProject-imported-wildcard-separate-update.csproj");

			var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			Assert.IsInstanceOf<Project> (p);
			var mp = (Project)p;
			var textFileItems =  mp.MSBuildProject.EvaluatedItems.Where (item => item.Name == "None").ToArray ();
			var preserveNewestFiles = textFileItems
				.Where (item => item.Metadata.GetValue ("CopyToOutputDirectory") == "PreserveNewest")
				.Select (item => item.Include).OrderBy (f => f).ToArray ();
			var nonUpdatedTextFiles = textFileItems
				.Where (item => item.Metadata.GetValue ("CopyToOutputDirectory") != "PreserveNewest")
				.Select (item => item.Include).OrderBy (f => f).ToArray ();

			Assert.AreEqual (new string [] {
				@"Content\Data\text2-1.txt",
				@"Content\Data\text2-2.txt",
				@"Content\text1-1.txt",
				@"Content\text1-2.txt"
			}, preserveNewestFiles);

			Assert.AreEqual (new string [] {
				@"Extra\No\More\p3.txt",
				@"Extra\No\p2.txt",
				@"Extra\p1.txt",
				@"Extra\Yes\More\p5.txt",
				@"Extra\Yes\More\p6.txt",
				@"Extra\Yes\p4.txt",
				"text3-1.txt",
				"text3-2.txt"
			}, nonUpdatedTextFiles);

			p.Dispose ();
		}

		[Test]
		public async Task VSFormatCompatibility ()
		{
			// Specific format compatibility issues tested here:
			// * Preserve the case of guids in project references
			// * Preserve the line endings used in the sln files
			// * Preserve initial blank lines in sln files
			// * Preserve the product description in the sln file, even if it doesn't match MD's file format
			// * If an assembly reference has SpecificVersion==false but the actual reference in the csproj
			//   does have version information, keep it when saving.
			// * Don't remove ProductVersion and SchemaVersion from csproj even when it is not necessary

			string solFile = Util.GetSampleProject ("project-from-vs", "console-with-libs.sln");

			var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p1 = sol.Items [0];
			var p2 = sol.Items [1];
			var p3 = sol.Items [2];

			var solContent = File.ReadAllText (solFile);
			var refXml1 = File.ReadAllText (p1.FileName);
			var refXml2 = File.ReadAllText (p2.FileName);
			var refXml3 = File.ReadAllText (p3.FileName);

			await sol.SaveAsync (Util.GetMonitor ());

			var savedSol = File.ReadAllText (solFile);
			var savedXml1 = File.ReadAllText (p1.FileName);
			var savedXml2 = File.ReadAllText (p2.FileName);
			var savedXml3 = File.ReadAllText (p3.FileName);

			Assert.AreEqual (solContent, savedSol);
			Assert.AreEqual (refXml1, savedXml1);
			Assert.AreEqual (refXml2, savedXml2);
			Assert.AreEqual (refXml3, savedXml3);

			sol.Dispose ();
		}

		[Test]
		public async Task VSFormatCompatibilityFolderOrdering ()
		{
			// Test for bug #28668 - Changing a sln from VS in XS re-orders solution folder lines

			string solFile = Util.GetSampleProject ("vs-compat-sln-ordering", "ConsoleApplication.sln");

			var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p1 = sol.Items [0];

			var solContent = File.ReadAllText (solFile);
			var refXml1 = File.ReadAllText (p1.FileName);

			await sol.SaveAsync (Util.GetMonitor ());

			var savedSol = File.ReadAllText (solFile);
			var savedXml1 = File.ReadAllText (p1.FileName);

			Assert.AreEqual (solContent, savedSol);
			Assert.AreEqual (refXml1, savedXml1);

			sol.Dispose ();
		}

		[Test]
		public async Task UnsupportedProjectSerializationRoundtrip ()
		{
			// Load and save a Windows Phone project.

			string solFile = Util.GetSampleProject ("unsupported-project-roundtrip", "TestApp.WinPhone.sln");

			var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = sol.Items [0];

			var refSol = File.ReadAllText (solFile);
			var refProj = File.ReadAllText (p.FileName);

			await sol.SaveAsync (Util.GetMonitor ());

			var savedSol = File.ReadAllText (solFile);
			var savedProj = File.ReadAllText (p.FileName);

			Assert.AreEqual (refSol, savedSol);
			Assert.AreEqual (refProj, savedProj);

			sol.Dispose ();
		}

		[Test ()]
		public async Task ProjectWithCustomGroup ()
		{
			string solFile = Util.GetSampleProject ("project-with-custom-group", "ConsoleProject.sln");
			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = sol.Items [0];

			var refXml = File.ReadAllText (p.FileName);
			await sol.SaveAsync (Util.GetMonitor ());
			var savedXml = File.ReadAllText (p.FileName);

			Assert.AreEqual (refXml, savedXml);

			sol.Dispose ();
		}

		[Test ()]
		public async Task ProjectWithEnvVars ()
		{
			string solFile = Util.GetSampleProject ("project-with-env-vars", "ConsoleProject.sln");
			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = sol.Items [0];

			var refXml = File.ReadAllText (p.FileName);
			await sol.SaveAsync (Util.GetMonitor ());
			var savedXml = File.ReadAllText (p.FileName);

			Assert.AreEqual (refXml, savedXml);

			sol.Dispose ();
		}

		[Test ()]
		public async Task DefaultProjectConfiguration ()
		{
			string projFile = Util.GetSampleProject ("default-project-config", "ConsoleProject.csproj");
			Project p = (Project)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);

			var refXml = File.ReadAllText (projFile);
			await p.SaveAsync (Util.GetMonitor ());
			var savedXml = File.ReadAllText (projFile);

			Assert.AreEqual (refXml, savedXml);

			var c = p.Configurations.FirstOrDefault<SolutionItemConfiguration> (co => co.Id == "Test");
			p.Configurations.Remove (c);

			await p.SaveAsync (Util.GetMonitor ());

			refXml = Util.ToSystemEndings (File.ReadAllText (projFile + ".saved2"));
			savedXml = File.ReadAllText (projFile);
			Assert.AreEqual (refXml, savedXml);

			c = p.Configurations.FirstOrDefault<SolutionItemConfiguration> (co => co.Id == "Test|x86");
			p.Configurations.Remove (c);

			await p.SaveAsync (Util.GetMonitor ());

			refXml = Util.ToSystemEndings (File.ReadAllText (projFile + ".saved3"));
			savedXml = File.ReadAllText (projFile);
			Assert.AreEqual (refXml, savedXml);

			p.Configurations.RemoveRange (p.Configurations.Where<SolutionItemConfiguration> (co => co.Name == "Debug"));

			await p.SaveAsync (Util.GetMonitor ());

			refXml = Util.ToSystemEndings (File.ReadAllText (projFile + ".saved4"));
			savedXml = File.ReadAllText (projFile);
			Assert.AreEqual (refXml, savedXml);

			p.Dispose ();
		}

		[Test]
		public async Task ProjectSerializationRoundtrip (
			[Values (
				"broken-condition.csproj",
				"empty-element.csproj",
				"comment.csproj",
				"text-spacing.csproj",
				"inconsistent-line-endings.csproj",
				"attribute-order.csproj",
				"custom-namespace.csproj",
				"multiple-prop-definition.csproj",
				"env-vars-prop.csproj"
				//"ICSharpCode.NRefactory.Cecil.csproj"
			)]
			string project)
		{
			string solFile = Util.GetSampleProject ("roundtrip-test-projects", project);
			var p = (SolutionItem)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), solFile);

			var refXml = File.ReadAllText (p.FileName);
			await p.SaveAsync (Util.GetMonitor ());
			var savedXml = File.ReadAllText (p.FileName);

			Assert.AreEqual (refXml, savedXml);

			p.Dispose ();
		}

		[Test]
		public async Task AddProjectConfigurationWithProperties ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");

			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (Project)sol.Items [0];

			var conf = p.CreateConfiguration ("Test");
			conf.Properties.SetValue ("TestProperty", "TestValue");
			conf.Properties.SetValue ("TestPath", p.BaseDirectory.Combine ("Subdir", "SomeFile.txt"));
			p.Configurations.Add (conf);

			await p.SaveAsync (Util.GetMonitor ());

			var refXml = Util.ToSystemEndings (File.ReadAllText (p.FileName + ".config-props-added"));
			var savedXml = File.ReadAllText (p.FileName);
			Assert.AreEqual (refXml, savedXml);
			sol.Dispose ();

			sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			p = (Project)sol.Items [0];

			conf = p.Configurations.OfType<ProjectConfiguration> ().FirstOrDefault (c => c.Name == "Test");
			Assert.AreEqual ("TestValue", conf.Properties.GetValue ("TestProperty"));
			Assert.AreEqual (p.BaseDirectory.Combine ("Subdir", "SomeFile.txt"), conf.Properties.GetPathValue ("TestPath"));

			sol.Dispose ();
		}

		[Test]
		public async Task RenameProjectConfiguration ()
		{
			// Change the name of the Debug configuration.
			// - The configuration condition in the msbuild file has to change.
			// - The default configuration has to change

			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");

			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (Project)sol.Items [0];

			var conf = p.Configurations.OfType<ProjectConfiguration> ().FirstOrDefault (c => c.Name == "Debug");
			var newConf = p.CreateConfiguration ("Test");
			newConf.CopyFrom (conf);
			p.Configurations [p.Configurations.IndexOf (conf)] = newConf;
			newConf.IntermediateOutputDirectory = p.BaseDirectory.Combine ("obj", "Test");

			await p.SaveAsync (Util.GetMonitor ());

			var refXml = Util.ToSystemEndings (File.ReadAllText (p.FileName + ".config-renamed"));
			var savedXml = File.ReadAllText (p.FileName);
			Assert.AreEqual (refXml, savedXml);

			sol.Dispose ();
		}

		[Test]
		public async Task CustomProjectItemWithMetadata ()
		{
			// Save a custom item with metadata

			try {
				MSBuildProjectService.RegisterCustomProjectItemType ("CustomItem", typeof (CustomItem));

				string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");

				Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
				var p = (Project)sol.Items [0];

				CustomItem it = new CustomItem {
					SomeMetadata = "FooTest"
				};
				p.Items.Add (it);

				await p.SaveAsync (Util.GetMonitor ());

				var refXml = Util.ToSystemEndings (File.ReadAllText (p.FileName + ".custom-item"));
				var savedXml = File.ReadAllText (p.FileName);
				Assert.AreEqual (refXml, savedXml);

				sol.Dispose ();

				sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
				p = (Project)sol.Items [0];

				it = p.Items.OfType<CustomItem> ().FirstOrDefault ();
				Assert.IsNotNull (it);
				Assert.AreEqual ("TestInclude", it.Include);
				Assert.AreEqual ("FooTest", it.SomeMetadata);

				sol.Dispose ();
			} finally {
				MSBuildProjectService.UnregisterCustomProjectItemType ("CustomItem");
			}
		}

		[Test]
		public async Task RunTarget ()
		{
			string projFile = Util.GetSampleProject ("msbuild-tests", "project-with-custom-target.csproj");
			var p = (Project)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);

			var ctx = new TargetEvaluationContext ();
			ctx.GlobalProperties.SetValue ("TestProp", "has");
			ctx.PropertiesToEvaluate.Add ("GenProp");
			ctx.PropertiesToEvaluate.Add ("AssemblyName");
			ctx.ItemsToEvaluate.Add ("GenItem");
			var res = await p.RunTarget (Util.GetMonitor (), "Test", p.Configurations [0].Selector, ctx);

			Assert.AreEqual (1, res.BuildResult.Errors.Count);
			Assert.AreEqual ("Something failed: has foo bar", res.BuildResult.Errors [0].ErrorText);

			// Verify that properties are returned

			Assert.AreEqual ("ConsoleProject", res.Properties.GetValue ("AssemblyName"));
			Assert.AreEqual ("foo", res.Properties.GetValue ("GenProp"));

			// Verify that items are returned

			var items = res.Items.ToArray ();
			Assert.AreEqual (1, items.Length);
			Assert.AreEqual ("bar", items [0].Include);
			Assert.AreEqual ("Hello", items [0].Metadata.GetValue ("MyMetadata"));

			p.Dispose ();
		}

		[Test]
		public async Task TargetEvaluationResultTryGetPathValueForNullPropertyValue ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (Project)sol.Items [0];

			var ctx = new TargetEvaluationContext ();
			ctx.PropertiesToEvaluate.Add ("MissingProperty");
			var res = await p.RunTarget (Util.GetMonitor (), "Build", p.Configurations [0].Selector, ctx);

			Assert.IsNull (res.Properties.GetValue ("MissingProperty"));

			FilePath path = null;
			bool foundProperty = res.Properties.TryGetPathValue ("MissingProperty", out path);
			Assert.IsFalse (foundProperty);

			p.Dispose ();
		}

		[Test]
		public async Task BuildWithCustomProps ()
		{
			string projFile = Util.GetSampleProject ("msbuild-tests", "project-with-custom-build-target.csproj");
			var p = (Project)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);

			var ctx = new ProjectOperationContext ();
			ctx.GlobalProperties.SetValue ("TestProp", "foo");
			var res = await p.Build (Util.GetMonitor (), p.Configurations [0].Selector, ctx);

			Assert.AreEqual (1, res.Errors.Count);
			Assert.AreEqual ("Something failed: foo", res.Errors [0].ErrorText);

			await p.Clean (Util.GetMonitor (), p.Configurations [0].Selector);
			res = await p.Build (Util.GetMonitor (), p.Configurations [0].Selector, true);

			// Check that the global property is reset
			Assert.AreEqual (1, res.Errors.Count);
			Assert.AreEqual ("Something failed: show", res.Errors [0].ErrorText);

			p.Dispose ();
		}

		/// <summary>
		/// As above but the property is used to import different .targets files
		/// and MSBuild is used
		/// </summary>
		[Test]
		[Platform (Exclude = "Win")]
		public async Task BuildWithCustomProps2 ()
		{
			string projFile = Util.GetSampleProject ("msbuild-tests", "project-with-custom-build-target2.csproj");
			var p = (Project)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			p.RequiresMicrosoftBuild = true;

			var ctx = new ProjectOperationContext ();
			ctx.GlobalProperties.SetValue ("TestProp", "foo");
			var res = await p.Build (Util.GetMonitor (), p.Configurations [0].Selector, ctx);

			Assert.AreEqual (1, res.Errors.Count);
			Assert.AreEqual ("Something failed (foo.targets): foo", res.Errors [0].ErrorText);

			await p.Clean (Util.GetMonitor (), p.Configurations [0].Selector);
			res = await p.Build (Util.GetMonitor (), p.Configurations [0].Selector, true);

			// Check that the global property is reset
			Assert.AreEqual (1, res.Errors.Count);
			Assert.AreEqual ("Something failed (show.targets): show", res.Errors [0].ErrorText);

			p.Dispose ();
		}

		/// <summary>
		/// As above but the property has the same as a default global property defined
		/// by the IDE. This test makes sures the existing global properties are
		/// restored.
		/// </summary>
		[Test]
		[Platform (Exclude = "Win")]
		public async Task BuildWithCustomProps3 ()
		{
			string projFile = Util.GetSampleProject ("msbuild-tests", "project-with-custom-build-target3.csproj");
			var p = (Project)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			p.RequiresMicrosoftBuild = true;

			var ctx = new ProjectOperationContext ();
			ctx.GlobalProperties.SetValue ("BuildingInsideVisualStudio", "false");
			var res = await p.Build (Util.GetMonitor (), p.Configurations [0].Selector, ctx);

			Assert.AreEqual (1, res.Errors.Count);
			Assert.AreEqual ("Something failed (false.targets): false", res.Errors [0].ErrorText);

			await p.Clean (Util.GetMonitor (), p.Configurations [0].Selector);
			res = await p.Build (Util.GetMonitor (), p.Configurations [0].Selector, true);

			// Check that the global property is reset
			Assert.AreEqual (1, res.Errors.Count);
			Assert.AreEqual ("Something failed (true.targets): true", res.Errors [0].ErrorText);

			p.Dispose ();
		}

		/// <summary>
		/// Tests that the MSBuildSDKsPath property is set when building a project.
		/// This is used by Microsoft.NET.Sdk.Web .NET Core projects when importing
		/// other MSBuild .targets and .props.
		/// </summary>
		[Test]
		[Platform (Exclude = "Win")]
		[Ignore]
		public async Task BuildDotNetCoreProjectWithImportUsingMSBuildSDKsPathProperty ()
		{
			// This test is being ignored for now because relying on MSBuildSDKsPath is not entirely correct,
			// the correct approach is to use the Sdk attribute in the import.
			// In any case this currently works for web projects because MSBuildSDKsPath ends being resolved
			// to the Mono's msbuild dir, which has the web targets.

			FilePath solFile = Util.GetSampleProject ("dotnetcore-console", "dotnetcore-msbuildsdkspath-import.sln");

			FilePath sdksPath = solFile.ParentDirectory.Combine ("Sdks");
			MSBuildProjectService.RegisterProjectImportSearchPath ("MSBuildSDKsPath", sdksPath);

			try {
				var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
				var p = (Project)sol.Items [0];
				p.RequiresMicrosoftBuild = true;

				p.DefaultConfiguration = new DotNetProjectConfiguration ("Debug") {
					OutputAssembly = p.BaseDirectory.Combine ("bin", "test.dll")
				};
				var res = await p.RunTarget (Util.GetMonitor (), "Build", ConfigurationSelector.Default);
				var buildResult = res.BuildResult;

				Assert.AreEqual (1, buildResult.Errors.Count);
				string expectedMessage = string.Format ("Something failed (test-import.targets): {0}", sdksPath);
				Assert.AreEqual (expectedMessage, buildResult.Errors [0].ErrorText);

				sol.Dispose ();

			} finally {
				MSBuildProjectService.UnregisterProjectImportSearchPath ("MSBuildSDKsPath", sdksPath);
			}
		}

		/// <summary>
		/// The MSBuildSDKsPath property needs to be set when a non .NET Core project references
		/// a .NET Core project. Otherwise the build will fail with an error similar to:
		/// Error MSB4019: The imported project "~/Library/Caches/VisualStudio/7.0/MSBuild/8209_1/Sdks/Microsoft.NET.Sdk/Sdk/Sdk.props"
		/// was not found.
		/// </summary>
		[Test]
		[Platform (Exclude = "Win")]
		public async Task BuildProjectReferencingDotNetCoreProject ()
		{
			FilePath solFile = Util.GetSampleProject ("DotNetCoreProjectReference", "DotNetCoreProjectReference.sln");

			var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (Project)sol.Items [0];
			Assert.AreEqual ("DotNetFrameworkProject", p.Name);
			p.RequiresMicrosoftBuild = true;

			p.DefaultConfiguration = new DotNetProjectConfiguration ("Debug") {
				OutputAssembly = p.BaseDirectory.Combine ("bin", "test.dll")
			};
			var res = await p.RunTarget (Util.GetMonitor (false), "Build", ConfigurationSelector.Default);
			var buildResult = res.BuildResult;

			Assert.AreEqual (0, buildResult.Errors.Count);

			sol.Dispose ();
		}

		/// <summary>
		/// Ensure project builder is updated when SdksPath changes after referencing a .NET Core project
		/// </summary>
		[Test]
		[Platform (Exclude = "Win")]
		public async Task BuildProjectAfterReferencingDotNetCoreProject ()
		{
			FilePath solFile = Util.GetSampleProject ("DotNetCoreProjectReference", "NoProjectReference.sln");

			var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (DotNetProject)sol.Items [0];
			Assert.AreEqual ("DotNetFrameworkProject", p.Name);
			p.RequiresMicrosoftBuild = true;

			FilePath projectFile = sol.BaseDirectory.Combine ("DotNetCoreNetStandardProject", "DotNetCoreNetStandardProject.csproj");
			var dotNetCoreProject = (Project)await sol.RootFolder.AddItem (Util.GetMonitor (), projectFile);
			//var dotNetCoreProject = (DotNetProject)sol.Items [0];
			dotNetCoreProject.RequiresMicrosoftBuild = true;
			await sol.SaveAsync (Util.GetMonitor ());

			p.DefaultConfiguration = new DotNetProjectConfiguration ("Debug") {
				OutputAssembly = p.BaseDirectory.Combine ("bin", "test.dll")
			};
			var res = await p.RunTarget (Util.GetMonitor (false), "Clean", ConfigurationSelector.Default);

			var pr = ProjectReference.CreateProjectReference ((DotNetProject)dotNetCoreProject);
			pr.ReferenceOutputAssembly = false;
			pr.LocalCopy = false;
			p.References.Add (pr);
			await p.SaveAsync (Util.GetMonitor ());

			res = await p.RunTarget (Util.GetMonitor (false), "Build", ConfigurationSelector.Default);
			var buildResult = res.BuildResult;

			Assert.AreEqual (0, buildResult.Errors.Count);

			sol.Dispose ();
		}

		/// <summary>
		/// Tests that the first target framework is used to evaluate the project.
		/// </summary>
		[Test]
		public async Task LoadDotNetCoreProjectWithMultipleTargetFrameworks ()
		{
			FilePath solFile = Util.GetSampleProject ("DotNetCoreMultiTargetFramework", "DotNetCoreMultiTargetFramework.sln");

			var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (Project)sol.Items [0];
			var capabilities = p.GetProjectCapabilities ().ToList ();

			Assert.That (capabilities, Contains.Item ("TestCapabilityNetStandard"));
			Assert.That (capabilities, Has.None.EqualTo ("TestCapabilityNetCoreApp"));

			await p.ReevaluateProject (Util.GetMonitor ());

			capabilities = p.GetProjectCapabilities ().ToList ();

			Assert.That (capabilities, Contains.Item ("TestCapabilityNetStandard"));
			Assert.That (capabilities, Has.None.EqualTo ("TestCapabilityNetCoreApp"));

			sol.Dispose ();
		}

		[Test]
		public async Task CopyConfiguration ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");

			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			Project p = (Project)sol.Items [0];

			var conf = p.Configurations.OfType<ProjectConfiguration> ().FirstOrDefault (c => c.Name == "Debug");
			conf.Properties.SetValue ("Foo", "Bar");

			var newConf = p.CreateConfiguration ("Test");
			newConf.CopyFrom (conf);
			p.Configurations.Add (newConf);

			await p.SaveAsync (Util.GetMonitor ());

			var refXml = Util.ToWindowsEndings (File.ReadAllText (p.FileName + ".config-copied"));
			var savedXml = Util.ToWindowsEndings (File.ReadAllText (p.FileName));
			Assert.AreEqual (refXml, savedXml);

			sol.Dispose ();
		}

		[Test]
		public void DefaultMSBuildSupport ()
		{
			var project = Services.ProjectService.CreateDotNetProject ("C#");
			bool byDefault, require;
			MSBuildProjectService.CheckHandlerUsesMSBuildEngine (project, out byDefault, out require);
			Assert.IsTrue (byDefault);
			Assert.IsFalse (require);

			project.Dispose ();
		}

		[Test]
		public async Task RenameFile ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			Project p = (Project)sol.Items [0];

			var f = p.GetProjectFile (p.ItemDirectory.Combine ("Program.cs"));
			f.Name = p.ItemDirectory.Combine ("test.cs");

			Assert.AreEqual ("test.cs", f.FilePath.FileName);
			await sol.SaveAsync (Util.GetMonitor ());

			var mp = await MSBuildProject.LoadAsync (p.FileName);
			mp.Evaluate ();
			Assert.IsTrue (mp.EvaluatedItems.FirstOrDefault (i => i.Name == "Compile" && i.Include == "test.cs") != null);
			Assert.IsTrue (mp.EvaluatedItems.FirstOrDefault (i => i.Name == "Compile" && i.Include == "Program.cs") == null);

			sol.Dispose ();
		}

		[Test]
		public void FrameworkAssemblyVersionNotStored ()
		{
			// We don't store the version number for framework assemblies
			var p = Services.ProjectService.CreateDotNetProject ("C#");
			var pr = ProjectReference.CreateAssemblyReference ("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
			p.References.Add (pr);

			Assert.AreEqual ("System", pr.Include);

			p.Dispose ();
		}

		[Test]
		public async Task ProjectDefinesCommonPropertiesInExternalFile ()
		{
			string solFile = Util.GetSampleProject ("project-includes-props", "ConsoleProject.sln");
			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			Project p = (Project)sol.Items [0];

			var refXml = Util.ToSystemEndings (File.ReadAllText (p.FileName));

			await p.SaveAsync (Util.GetMonitor ());

			var savedXml = File.ReadAllText (p.FileName);
			Assert.AreEqual (refXml, savedXml);

			sol.Dispose ();
		}

		[Test]
		public async Task ProjectWithMultiIncludeItem ()
		{
			string solFile = Util.GetSampleProject ("project-multi-include-item", "ConsoleProject.sln");
			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			Project p = (Project)sol.Items [0];

			var f = p.Files.FirstOrDefault (pf => pf.FilePath.FileName == "Program1.cs");
			Assert.NotNull (f);
			Assert.IsFalse (f.Visible);

			f = p.Files.FirstOrDefault (pf => pf.FilePath.FileName == "Program2.cs");
			Assert.NotNull (f);
			Assert.IsFalse (f.Visible);

			f = p.Files.FirstOrDefault (pf => pf.FilePath.FileName == "Program3.cs");
			Assert.NotNull (f);
			Assert.IsFalse (f.Visible);

			var refXml = Util.ToSystemEndings (File.ReadAllText (p.FileName));

			await p.SaveAsync (Util.GetMonitor ());
			await p.SaveAsync (Util.GetMonitor ());

			var savedXml = File.ReadAllText (p.FileName);
			Assert.AreEqual (refXml, savedXml);

			refXml = Util.ToSystemEndings (Util.ToWindowsEndings (File.ReadAllText (p.FileName + ".2")));
			f.Visible = true;
			await p.SaveAsync (Util.GetMonitor ());
			await p.SaveAsync (Util.GetMonitor ());

			savedXml = File.ReadAllText (p.FileName);
			Assert.AreEqual (refXml, savedXml);

			sol.Dispose ();
		}

		[Test ()]
		public async Task SolutionDirIsSet ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");

			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

			var p = (Project)sol.Items [0];
			Assert.AreEqual (sol.ItemDirectory.ToString () + Path.DirectorySeparatorChar, p.MSBuildProject.EvaluatedProperties.GetValue ("SolutionDir"));

			sol.Dispose ();
		}

		[Test]
		public async Task RenameConfiguration ()
		{
			// When renaming a configuration, paths that use the configuration name should also be renamed

			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

			var p = (DotNetProject)sol.Items [0];
			var c = p.GetConfiguration (new ItemConfigurationSelector ("Release"));
			var renamed = p.CreateConfiguration ("Test");
			renamed.CopyFrom (c, true);
			p.Configurations.Remove (c);
			p.Configurations.Add (renamed);
			await p.SaveAsync (Util.GetMonitor ());

			var savedXml = File.ReadAllText (p.FileName);
			var compXml = Util.ToSystemEndings (File.ReadAllText (p.FileName.ChangeName ("ConsoleProject-conf-renamed")));
			Assert.AreEqual (compXml, savedXml);

			sol.Dispose ();
		}

		[Test]
		public async Task ProjectWithDuplicateConfigGroup ()
		{
			// The project has two property groups with Debug|AnyCPU. This has to result in a single
			// Debug configuration. If a change is done in the configuration, it has to be applied
			// to the last group.

			string projFile = Util.GetSampleProject ("msbuild-tests", "project-with-duplicated-conf.csproj");
			var p = (Project)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);

			Assert.AreEqual (2, p.Configurations.Count);

			var c = p.GetConfiguration (new ItemConfigurationSelector ("Debug")) as DotNetProjectConfiguration;
			Assert.IsNotNull (c);
			Assert.IsTrue (c.DebugSymbols);

			c.Properties.SetValue ("Test", "foo");
			await p.SaveAsync (Util.GetMonitor ());

			var savedXml = File.ReadAllText (p.FileName);
			var refXml = File.ReadAllText (p.FileName.ChangeName ("project-with-duplicated-conf-saved"));
			Assert.AreEqual (refXml, savedXml);

			p.Dispose ();
		}

		[Test]
		public async Task ConditionedHintPath ()
		{
			// A reference with several hint paths with conditions. Only the hint path with the true condition
			// will be used

			string projFile = Util.GetSampleProject ("msbuild-tests", "conditioned-hintpath.csproj");
			var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);

			Assert.AreEqual (2, p.References.Count);

			Assert.AreEqual (p.ItemDirectory.Combine ("a.dll").ToString (), p.References [0].HintPath.ToString ());
			Assert.AreEqual (p.ItemDirectory.Combine ("b.dll").ToString (), p.References [1].HintPath.ToString ());

			var refXml = File.ReadAllText (p.FileName);
			await p.SaveAsync (Util.GetMonitor ());

			var savedXml = File.ReadAllText (p.FileName);
			Assert.AreEqual (refXml, savedXml);

			p.Dispose ();
		}

		[Test]
		public async Task MSBuildPropertiesSetWhenSaving ()
		{
			Solution sol = TestProjectsChecks.CreateConsoleSolution ("console-project-msbuild");
			sol.ConvertToFormat (MSBuildFileFormat.VS2010);

			var p = sol.GetAllProjects ().First ();
			var c = (ProjectConfiguration)p.Configurations [0];
			Assert.IsFalse (p.ProjectProperties.HasProperty ("TargetName"));
			Assert.IsFalse (p.MSBuildProject.EvaluatedProperties.HasProperty ("TargetName"));
			Assert.IsFalse (c.Properties.HasProperty ("TargetName"));

			await sol.SaveAsync (Util.GetMonitor ());

			// MSBuild properties defined in imported targets are loaded after saving a project for the first time

			Assert.IsTrue (p.ProjectProperties.HasProperty ("TargetName"));
			Assert.IsTrue (p.MSBuildProject.EvaluatedProperties.HasProperty ("TargetName"));
			Assert.IsTrue (c.Properties.HasProperty ("TargetName"));

			sol.Dispose ();
		}

		[Test ()]
		public async Task LoadSaveConsoleProjectWithEmptyGroup ()
		{
			var fn = new CustomFlavorNode ();
			WorkspaceObject.RegisterCustomExtension (fn);

			try {
				string solFile = Util.GetSampleProject ("console-project-empty-group", "ConsoleProject.sln");

				Solution item = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
				Assert.IsTrue (item is Solution);

				Solution sol = (Solution)item;
				TestProjectsChecks.CheckBasicVsConsoleProject (sol);

				var p = sol.GetAllProjects ().FirstOrDefault ();
				Assert.NotNull (p);
				Assert.NotNull (p.GetFlavor<CustomFlavor> ());

				string projectFile = ((Project)sol.Items [0]).FileName;

				string solXml = File.ReadAllText (solFile);
				string projectXml = File.ReadAllText (projectFile);

				await sol.SaveAsync (Util.GetMonitor ());

				Assert.AreEqual (solXml, File.ReadAllText (solFile));
				Assert.AreEqual (projectXml, File.ReadAllText (projectFile));

				sol.Dispose ();
			} finally {
				WorkspaceObject.UnregisterCustomExtension (fn);
			}
		}

		[Test]
		public async Task RemoveAndAddProperty ()
		{
			string solFile = Util.GetSampleProject ("msbuild-project-test", "test.csproj");

			Project p = (Project) await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), solFile);

			string projectXml = File.ReadAllText (p.FileName);

			p.ProjectProperties.RemoveProperty ("TestRewrite");
			await p.SaveAsync (Util.GetMonitor ());
			p.ProjectProperties.SetValue ("TestRewrite", "Val");
			await p.SaveAsync (Util.GetMonitor ());

			Assert.AreEqual (projectXml, File.ReadAllText (p.FileName));

			p.Dispose ();
		}

		[Test]
		public async Task GetReferencedAssemblies ()
		{
			string projFile = Util.GetSampleProject ("msbuild-tests", "aliased-references.csproj");
			var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);

			var asms = (await p.GetReferencedAssemblies (p.Configurations [0].Selector)).ToArray ();

			var ar = asms.FirstOrDefault (a => a.FilePath.FileName == "System.Xml.dll");
			Assert.IsNotNull (ar);
			Assert.AreEqual ("", ar.Aliases);

			ar = asms.FirstOrDefault (a => a.FilePath.FileName == "System.Data.dll");
			Assert.IsNotNull (ar);
			Assert.AreEqual ("Foo", ar.Aliases);
		
			Assert.AreEqual (4, asms.Length);

			p.Dispose ();
		}

		[Test]
		public async Task GetReferences ()
		{
			string projFile = Util.GetSampleProject ("msbuild-tests", "aliased-references.csproj");
			var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);

			var asms = (await p.GetReferences (p.Configurations [0].Selector)).ToArray ();

			var ar = asms.FirstOrDefault (a => a.FilePath.FileName == "System.Xml.dll");
			Assert.IsNotNull (ar);
			Assert.AreEqual ("", ar.Aliases);

			ar = asms.FirstOrDefault (a => a.FilePath.FileName == "System.Data.dll");
			Assert.IsNotNull (ar);
			Assert.AreEqual ("Foo", ar.Aliases);

			Assert.AreEqual (4, asms.Length);

			p.Dispose ();
		}

		/// <summary>
		/// Tests that the default tools version is used for the new project.
		/// </summary>
		[Test]
		public async Task ToolsVersion_CreateSolutionWithProjectReloadAndAddNewProject ()
		{
			string directory = Util.CreateTmpDir ("ToolsVersionTest");
			string solutionFileName = Path.Combine (directory, "ToolsVersionTest.sln");
			var solution = new Solution ();
			solution.FileName = solutionFileName;
			var project = Services.ProjectService.CreateDotNetProject ("C#");
			project.FileName = Path.Combine (directory, "ToolsVersionTest.csproj");
			solution.RootFolder.AddItem (project);

			await solution.SaveAsync (Util.GetMonitor ());

			solution = await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName) as Solution;

			var project2 = Services.ProjectService.CreateDotNetProject ("C#");
			project2.FileName = Path.Combine (directory, "ToolsVersionTest2.csproj");
			solution.RootFolder.AddItem (project2);

			Assert.AreEqual (project.ToolsVersion, project2.ToolsVersion);
		}
	}

	class MyProjectTypeNode: ProjectTypeNode
	{
		public MyProjectTypeNode ()
		{
			Guid = "{52136883-B1F9-4238-BAAA-0FB243663676}";
			Extension = "myproj";
		}

		public override Type ItemType {
			get {
				return typeof(MyProject);
			}
		}
	}

	class MyEmptyProjectTypeNode: ProjectTypeNode
	{
		public MyEmptyProjectTypeNode ()
		{
			Guid = "{52136883-B1F9-4238-BAAA-0FB243663676}";
			Extension = "myproj";
		}

		public override Type ItemType {
			get {
				return typeof(MyEmptyProject);
			}
		}
	}

	class MyProject: Project
	{
		[ItemProperty]
		public string SimpleData { get; set; }

		[ItemProperty (IsExternal = true)]
		public MyProjectData Data;

		[ItemProperty (WrapObject = false)]
		public MyProjectData DataProperty;
	}

	class MyProjectData
	{
		[ItemProperty]
		public string Foo { get; set; }
	}

	class MyEmptyProject: Project
	{
	}

	class FlavorWithData: ProjectExtension
	{
		[ItemProperty]
		public string SimpleData { get; set; }

		[ItemProperty (IsExternal = true)]
		public MyProjectData Data;
	}

	class CustomItem: ProjectItem
	{
		public override string Include {
			get {
				return "TestInclude";
			}
			protected set {
			}
		}

		[ItemProperty]
		public string SomeMetadata { get; set; }
	}

	class CustomFlavorNode: SolutionItemExtensionNode
	{
		public CustomFlavorNode ()
		{
			Guid = "{57EDDE80-A1D8-43D5-8478-C17416DFC16F}";
		}

		public override object CreateInstance ()
		{
			return new CustomFlavor ();
		}
	}

	class CustomFlavor: ProjectExtension
	{
	}

	class SupportImportedProjectFilesDotNetProjectExtension : DotNetProjectExtension
	{
		internal protected override bool OnGetSupportsImportedItem (IMSBuildItemEvaluated buildItem)
		{
			return BuildAction.DotNetActions.Contains (buildItem.Name);
		}
	}
}
