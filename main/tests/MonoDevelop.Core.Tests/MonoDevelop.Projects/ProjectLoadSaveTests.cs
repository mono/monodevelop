//
// ProjectLoadSaveTests.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2017 Microsoft
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
	public class ProjectLoadSaveTests: TestBase
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

			Project p = (Project)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), solFile);

			string projectXml = File.ReadAllText (p.FileName);

			p.ProjectProperties.RemoveProperty ("TestRewrite");
			await p.SaveAsync (Util.GetMonitor ());
			p.ProjectProperties.SetValue ("TestRewrite", "Val");
			await p.SaveAsync (Util.GetMonitor ());

			Assert.AreEqual (projectXml, File.ReadAllText (p.FileName));

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

		[Test]
		public void SanitizeProjectNamespace ()
		{
			var info = new ProjectCreateInformation {
				ProjectBasePath = "/tmp/test",
				ProjectName = "abc.0"
			};

			var doc = new XmlDocument ();
			var projectOptions = doc.CreateElement ("Options");
			projectOptions.SetAttribute ("language", "C#");

			DotNetProject project = (DotNetProject)Services.ProjectService.CreateProject ("C#", info, projectOptions);
			Assert.AreEqual ("abc", project.DefaultNamespace);
			project.Dispose ();

			info.ProjectName = "a.";
			project = (DotNetProject)Services.ProjectService.CreateProject ("C#", info, projectOptions);
			Assert.AreEqual ("a", project.DefaultNamespace);
			project.Dispose ();
		}

		[Test]
		public async Task SerializedWrite ()
		{
			var node = new CustomItemNode<SerializedSaveTestExtension> ();
			WorkspaceObject.RegisterCustomExtension (node);

			try {
				string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
				Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
				var p = (DotNetProject)sol.Items [0];

				var op1 = p.SaveAsync (Util.GetMonitor ());
				var op2 = p.SaveAsync (Util.GetMonitor ());
				await op1;
				await op2;
				Assert.AreEqual (2, SerializedSaveTestExtension.SaveCount);
				sol.Dispose ();
			} finally {
				WorkspaceObject.UnregisterCustomExtension (node);
			}
		}

		[Test]
		public async Task AddImportThenRemoveImportAndThenAddImportAgain ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

			var p = (DotNetProject)sol.Items [0];
			p.AddImportIfMissing ("MyImport.targets", null);
			await p.SaveAsync (Util.GetMonitor ());

			p.RemoveImport ("MyImport.targets");
			await p.SaveAsync (Util.GetMonitor ());

			p.AddImportIfMissing ("MyImport.targets", null);
			await p.SaveAsync (Util.GetMonitor ());

			var savedXml = File.ReadAllText (p.FileName);

			Assert.That (savedXml, Contains.Substring ("<Import Project=\"MyImport.targets\""));

			sol.Dispose ();
		}

		[Test]
		public void LoadReferenceWithSpaces_bug43510 ()
		{
			var pref = ProjectReference.CreateAssemblyReference (" gtk-sharp, Version=2.12.0.0, Culture=neutral, PublicKeyToken=35e10195dab3c99f");
			var p = (DotNetProject)Services.ProjectService.CreateProject ("C#");
			p.References.Add (pref);
			Assert.IsTrue (pref.IsValid);
			p.Dispose ();
		}

		[Test]
		public async Task WriteAndReadEnvironmentVariableProperty ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

			var p = (DotNetProject)sol.Items [0];
			var config = (ProjectConfiguration)p.Configurations ["Debug"];
			config.EnvironmentVariables.Add ("Test1", "Test1Value");

			await p.SaveAsync (Util.GetMonitor ());

			sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			p = (DotNetProject)sol.Items [0];
			config = (ProjectConfiguration)p.Configurations ["Debug"];

			var doc = new XmlDocument ();
			doc.Load (p.FileName);
			var propertyGroup = doc.DocumentElement.ChildNodes [1];
			var environmentVariablesProperty = propertyGroup.ChildNodes.OfType<XmlElement> ().Last ();
			var environmentVariablesChildProperty = environmentVariablesProperty.ChildNodes.OfType<XmlElement> ().First ();
			var variableProperty = environmentVariablesChildProperty.ChildNodes.OfType<XmlElement> ().First ();

			Assert.AreEqual ("Test1Value", config.EnvironmentVariables ["Test1"]);
			Assert.AreEqual ("EnvironmentVariables", environmentVariablesProperty.Name);
			Assert.IsFalse (environmentVariablesProperty.HasAttribute ("xmlns"));
			Assert.IsFalse (environmentVariablesChildProperty.HasAttribute ("xmlns"));
			Assert.IsFalse (variableProperty.HasAttribute ("xmlns"));

			sol.Dispose ();
		}

		[Test]
		public async Task WriteAndReadEnvironmentVariablePropertyForSdkProject ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (DotNetProject)sol.Items [0];

			string xml =
				"<Project Sdk=\"Microsoft.NET.Sdk\" ToolsVersion=\"15.0\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"    <OutputType>Exe</OutputType>\r\n" +
				"    <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"  <PropertyGroup Condition=\" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' \">\r\n" +
				"    <Value1>true</Value1>\r\n" +
				"  </PropertyGroup>\r\n" +
				"</Project>";
			File.WriteAllText (p.FileName, xml);
			sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			p = (DotNetProject)sol.Items [0];
			var config = (ProjectConfiguration)p.Configurations ["Debug"];
			config.EnvironmentVariables.Add ("Test1", "Test1Value");

			await p.SaveAsync (Util.GetMonitor ());
			sol.Dispose ();

			sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			p = (DotNetProject)sol.Items [0];
			config = (ProjectConfiguration)p.Configurations ["Debug"];

			var doc = new XmlDocument ();
			doc.Load (p.FileName);
			var propertyGroup = doc.DocumentElement.ChildNodes [1];
			var environmentVariablesProperty = (XmlElement)propertyGroup.ChildNodes [1];
			var environmentVariablesChildProperty = environmentVariablesProperty.ChildNodes.OfType<XmlElement> ().First ();
			var variableProperty = environmentVariablesChildProperty.ChildNodes.OfType<XmlElement> ().First ();

			Assert.AreEqual ("Test1Value", config.EnvironmentVariables ["Test1"]);
			Assert.AreEqual ("EnvironmentVariables", environmentVariablesProperty.Name);
			Assert.IsFalse (environmentVariablesProperty.HasAttribute ("xmlns"));
			Assert.IsFalse (environmentVariablesChildProperty.HasAttribute ("xmlns"));
			Assert.IsFalse (variableProperty.HasAttribute ("xmlns"));

			sol.Dispose ();
		}
	}

	class CustomItem : ProjectItem
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

	class CustomFlavorNode : SolutionItemExtensionNode
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

	class CustomFlavor : ProjectExtension
	{
	}

	class SerializedSaveTestExtension: SolutionItemExtension
	{
		static bool Running = false;
		public static int SaveCount = 0;

		internal protected override async Task OnSave (ProgressMonitor monitor)
		{
			if (Running)
				Assert.Fail ("A save operation is already running");
			Running = true;
			await Task.Delay (500);
			Running = false;
			SaveCount++;
			await base.OnSave (monitor);
		}
	}
}
