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
using MonoDevelop.Projects.Extensions;
using MonoDevelop.CSharp.Project;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Projects;
using System.Linq;
using Mono.CSharp;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Projects.Formats.MSBuild;
using System.Threading.Tasks;

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class MSBuildTests: TestBase
	{
		[Test()]
		public async Task LoadSaveBuildConsoleProject()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			
			Solution item = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			Assert.IsTrue (item is Solution);
			
			Solution sol = (Solution) item;
			TestProjectsChecks.CheckBasicVsConsoleProject (sol);
			string projectFile = ((Project)sol.Items [0]).FileName;
			
			BuildResult cr = await item.Build (Util.GetMonitor (), "Debug");
			Assert.IsNotNull (cr);
			Assert.AreEqual (0, cr.ErrorCount);
			Assert.AreEqual (0, cr.WarningCount);
			
			string solXml = File.ReadAllText (solFile);
			string projectXml = Util.GetXmlFileInfoset (projectFile);
			
			await sol.SaveAsync (Util.GetMonitor ());
			
			Assert.AreEqual (solXml, File.ReadAllText (solFile));
			Assert.AreEqual (projectXml, Util.GetXmlFileInfoset (projectFile));
		}

		[Test]
		public async Task BuildConsoleProject ()
		{
			Solution sol = TestProjectsChecks.CreateConsoleSolution ("console-project-msbuild");
			await sol.SaveAsync (Util.GetMonitor ());

			// Ensure the project is buildable
			var result = await sol.Build (Util.GetMonitor (), "Debug");
			Assert.AreEqual (0, result.ErrorCount, "#1");

			// Ensure the project is still buildable with xbuild after a rename
			ProjectOptionsDialog.RenameItem (sol.GetAllProjects ().First (), "Test");
			result = await sol.Build (Util.GetMonitor (), "Release");
			Assert.AreEqual (0, result.ErrorCount, "#2");
		}

		[Test]
		public async Task CreateConsoleProject ()
		{
			Solution sol = TestProjectsChecks.CreateConsoleSolution ("console-project-msbuild");
			await sol.ConvertToFormat (Util.FileFormatMSBuild10, true);
			await sol.SaveAsync (Util.GetMonitor ());
			
			// msbuild format

			string solXml = File.ReadAllText (sol.FileName);
			string projectXml = Util.GetXmlFileInfoset (((SolutionItem)sol.Items [0]).FileName);
			
			// Make sure we compare using the same guid
			Project p = sol.Items [0] as Project;
			string guid = p.ItemId;
			solXml = solXml.Replace (guid, "{969F05E2-0E79-4C5B-982C-8F3DD4D46311}");
			projectXml = projectXml.Replace (guid, "{969F05E2-0E79-4C5B-982C-8F3DD4D46311}");
			
			string solFile = Util.GetSampleProjectPath ("generated-console-project", "TestSolution.sln");
			string projectFile = Util.GetSampleProjectPath ("generated-console-project", "TestProject.csproj");
			
			Assert.AreEqual (Util.ToWindowsEndings (File.ReadAllText (solFile)), solXml);
			Assert.AreEqual (Util.ToWindowsEndings (Util.GetXmlFileInfoset (projectFile)), projectXml);
		}
		
		[Test]
		public async Task TestCreateLoadSaveConsoleProject ()
		{
			await TestProjectsChecks.TestCreateLoadSaveConsoleProject ("MSBuild05");
		}
		
		[Test]
		public async Task GenericProject ()
		{
			await TestProjectsChecks.CheckGenericItemProject ("MSBuild05");
		}
		
		[Test]
		public async Task TestLoadSaveSolutionFolders ()
		{
			await TestProjectsChecks.TestLoadSaveSolutionFolders ("MSBuild05");
		}
		
		[Test]
		public async Task TestLoadSaveResources ()
		{
			await TestProjectsChecks.TestLoadSaveResources ("MSBuild05");
		}
		
		[Test]
		public async Task TestConfigurationMerging ()
		{
			string solFile = Util.GetSampleProject ("test-configuration-merging", "TestConfigurationMerging.sln");
			Solution sol = await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile) as Solution;
			Assert.IsNotNull (sol);
			Assert.AreEqual (1, sol.Items.Count);

			DotNetProject p = sol.Items [0] as DotNetProject;
			Assert.IsNotNull (p);

			// Debug config
			
			DotNetProjectConfiguration conf = p.Configurations ["Debug"] as DotNetProjectConfiguration;
			Assert.IsNotNull (conf);
			Assert.AreEqual ("Debug", conf.Name);
			Assert.AreEqual (string.Empty, conf.Platform);

			CSharpCompilerParameters pars = conf.CompilationParameters as CSharpCompilerParameters;
			Assert.IsNotNull (pars);
			Assert.AreEqual (2, pars.WarningLevel);

			pars.WarningLevel = 4;

			// Release config
			
			conf = p.Configurations ["Release"] as DotNetProjectConfiguration;
			Assert.IsNotNull (conf);
			Assert.AreEqual ("Release", conf.Name);
			Assert.AreEqual (string.Empty, conf.Platform);

			pars = conf.CompilationParameters as CSharpCompilerParameters;
			Assert.IsNotNull (pars);
			Assert.AreEqual ("ReleaseMod", Path.GetFileName (conf.OutputDirectory));
			Assert.AreEqual (3, pars.WarningLevel);
			
			pars.WarningLevel = 1;
			Assert.AreEqual (1, pars.WarningLevel);
			conf.DebugMode = true;

			await sol.SaveAsync (Util.GetMonitor ());
			Assert.AreEqual (1, pars.WarningLevel);

			string savedFile = Path.Combine (p.BaseDirectory, "TestConfigurationMergingSaved.csproj");
			Assert.AreEqual (Util.GetXmlFileInfoset (savedFile), Util.GetXmlFileInfoset (p.FileName));
		}
		
		[Test]
		public async Task TestConfigurationMergingConfigPlatformCombinations ()
		{
			string projectFile = Util.GetSampleProject ("test-configuration-merging", "TestConfigurationMerging2.csproj");
			DotNetProject p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile) as DotNetProject;
			Assert.IsNotNull (p);

			Assert.IsNotNull (p.Configurations ["Debug|x86"]);
			Assert.IsNotNull (p.Configurations ["Debug|x86-64"]);
			Assert.IsNotNull (p.Configurations ["Debug|Other"]);

			Assert.IsNotNull (p.Configurations ["Release|x86"]);
			Assert.IsNotNull (p.Configurations ["Release|x86-64"]);
			Assert.IsNotNull (p.Configurations ["Release|Other"]);
			
			string originalContent = Util.GetXmlFileInfoset (p.FileName);
			
			await p.SaveAsync (Util.GetMonitor ());

			Assert.AreEqual (originalContent, Util.GetXmlFileInfoset (p.FileName));
		}
		
		[Test]
		public async Task TestConfigurationMergingDefaultValues ()
		{
			string projectFile = Util.GetSampleProject ("test-configuration-merging", "TestConfigurationMerging3.csproj");
			DotNetProject p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile) as DotNetProject;
			Assert.IsNotNull (p);

			DotNetProjectConfiguration conf = p.Configurations ["Release|x86"] as DotNetProjectConfiguration;
			Assert.IsNotNull (conf);
			Assert.IsTrue (conf.DebugMode);
			conf.DebugMode = false;
			CSharpCompilerParameters cparams = (CSharpCompilerParameters) conf.CompilationParameters;
			Assert.IsTrue (cparams.UnsafeCode);
			cparams.UnsafeCode = false;
			
			await p.SaveAsync (Util.GetMonitor ());

			Assert.AreEqual (Util.GetXmlFileInfoset (p.FileName + ".saved"), Util.GetXmlFileInfoset (p.FileName));
		}
		
		[Test]
		public async Task TestConfigurationMergingKeepOldConfig ()
		{
			string projectFile = Util.GetSampleProject ("test-configuration-merging", "TestConfigurationMerging4.csproj");
			DotNetProject p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile) as DotNetProject;
			Assert.IsNotNull (p);

			DotNetProjectConfiguration conf = p.Configurations ["Debug|x86"] as DotNetProjectConfiguration;
			Assert.IsNotNull (conf);
			Assert.IsTrue (conf.DebugMode);
			CSharpCompilerParameters cparams = (CSharpCompilerParameters) conf.CompilationParameters;
			Assert.IsTrue (cparams.UnsafeCode);
			
			conf = p.Configurations ["Release|x86"] as DotNetProjectConfiguration;
			Assert.IsNotNull (conf);
			Assert.IsFalse (conf.DebugMode);
			conf.DebugMode = true;
			cparams = (CSharpCompilerParameters) conf.CompilationParameters;
			Assert.IsFalse (cparams.UnsafeCode);
			cparams.UnsafeCode = true;
			
			await p.SaveAsync (Util.GetMonitor ());

			Assert.AreEqual (Util.GetXmlFileInfoset (p.FileName + ".saved"), Util.GetXmlFileInfoset (p.FileName));
		}
		
		[Test]
		public async Task TestConfigurationMergingChangeNoMergeToParent ()
		{
			string projectFile = Util.GetSampleProject ("test-configuration-merging", "TestConfigurationMerging5.csproj");
			DotNetProject p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile) as DotNetProject;
			Assert.IsNotNull (p);

			DotNetProjectConfiguration conf = p.Configurations ["Debug|x86"] as DotNetProjectConfiguration;
			Assert.IsNotNull (conf);
			Assert.IsTrue (conf.SignAssembly);
			
			conf = p.Configurations ["Release|x86"] as DotNetProjectConfiguration;
			Assert.IsNotNull (conf);
			Assert.IsTrue (conf.SignAssembly);
			conf.SignAssembly = false;
			
			await p.SaveAsync (Util.GetMonitor ());

			Assert.AreEqual (Util.GetXmlFileInfoset (p.FileName + ".saved"), Util.GetXmlFileInfoset (p.FileName));
		}
		
		[Test]
		public async Task TestConfigurationMergingChangeMergeToParent ()
		{
			string projectFile = Util.GetSampleProject ("test-configuration-merging", "TestConfigurationMerging6.csproj");
			DotNetProject p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile) as DotNetProject;
			Assert.IsNotNull (p);

			DotNetProjectConfiguration conf = p.Configurations ["Debug|x86"] as DotNetProjectConfiguration;
			Assert.IsNotNull (conf);
			Assert.IsTrue (conf.SignAssembly);
			conf.SignAssembly = false;
			
			conf = p.Configurations ["Release|x86"] as DotNetProjectConfiguration;
			Assert.IsNotNull (conf);
			Assert.IsTrue (conf.SignAssembly);
			conf.SignAssembly = false;
			
			await p.SaveAsync (Util.GetMonitor ());

			Assert.AreEqual (Util.GetXmlFileInfoset (p.FileName + ".saved"), Util.GetXmlFileInfoset (p.FileName));
		}
		
		[Test]
		public async Task TestConfigurationMergingChangeMergeToParent2 ()
		{
			string projectFile = Util.GetSampleProject ("test-configuration-merging", "TestConfigurationMerging7.csproj");
			DotNetProject p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile) as DotNetProject;
			Assert.IsNotNull (p);

			DotNetProjectConfiguration conf = p.Configurations ["Debug|x86"] as DotNetProjectConfiguration;
			Assert.IsNotNull (conf);
			Assert.IsTrue (conf.SignAssembly);
			conf.SignAssembly = true;
			
			conf = p.Configurations ["Release|x86"] as DotNetProjectConfiguration;
			Assert.IsNotNull (conf);
			Assert.IsFalse (conf.SignAssembly);
			conf.SignAssembly = true;
			
			await p.SaveAsync (Util.GetMonitor ());

			Assert.AreEqual (Util.GetXmlFileInfoset (p.FileName + ".saved"), Util.GetXmlFileInfoset (p.FileName));
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
			Assert.AreEqual ("some - library", p.References[0].Reference);
		}

		[Test]
		public async Task RoundtripPropertyWithXmlCharacters ()
		{
			Solution sol = TestProjectsChecks.CreateConsoleSolution ("roundtrip-property-with-xml");
			await sol.ConvertToFormat (Util.FileFormatMSBuild05, true);

			var value = "Hello<foo>&.exe";

			var p = (DotNetProject) sol.GetAllProjects ().First ();
			var conf = ((DotNetProjectConfiguration)p.Configurations [0]);
			conf.OutputAssembly = value;
			await sol.SaveAsync (Util.GetMonitor ());

			sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), sol.FileName);
			p = (DotNetProject) sol.GetAllProjects ().First ();
			conf = ((DotNetProjectConfiguration)p.Configurations [0]);

			Assert.AreEqual (value, conf.OutputAssembly);
		}

		[Test]
		public async Task EvaluateProperties ()
		{
			string dir = Path.GetDirectoryName (typeof(Project).Assembly.Location);
			Environment.SetEnvironmentVariable ("HHH", "EnvTest");
			Environment.SetEnvironmentVariable ("SOME_PLACE", dir);

			string solFile = Util.GetSampleProject ("property-evaluation-test", "property-evaluation-test.sln");
			Solution sol = await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile) as Solution;
			var p = (DotNetProject) sol.GetAllProjects ().First ();
			Assert.AreEqual ("Program1_test1.cs", p.Files[0].FilePath.FileName, "Basic replacement");
			Assert.AreEqual ("Program2_test1_test2.cs", p.Files[1].FilePath.FileName, "Property referencing same property");
			Assert.AreEqual ("Program3_full.cs", p.Files[2].FilePath.FileName, "Property inside group with non-evaluable condition");
			Assert.AreEqual ("Program4_yes_value.cs", p.Files[3].FilePath.FileName, "Evaluation of group condition");
			Assert.AreEqual ("Program5_yes_value.cs", p.Files[4].FilePath.FileName, "Evaluation of property condition");
			Assert.AreEqual ("Program6_unknown.cs", p.Files[5].FilePath.FileName, "Evaluation of property with non-evaluable condition");
			Assert.AreEqual ("Program7_test1.cs", p.Files[6].FilePath.FileName, "Item conditions are ignored");

			var testRef = Path.Combine (dir, "MonoDevelop.Core.dll");
			var asms = p.GetReferencedAssemblies (sol.Configurations [0].Selector).ToArray ();
			Assert.IsTrue (asms.Contains (testRef));
		}

		[Ignore ("xbuild bug. It is not returning correct values for evaluated items without condition list")]
		[Test]
		public async Task EvaluatePropertiesWithConditionalGroup ()
		{
			string dir = Path.GetDirectoryName (typeof(Project).Assembly.Location);
			Environment.SetEnvironmentVariable ("HHH", "EnvTest");
			Environment.SetEnvironmentVariable ("SOME_PLACE", dir);

			string solFile = Util.GetSampleProject ("property-evaluation-test", "property-evaluation-test.sln");
			Solution sol = await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile) as Solution;
			var p = (DotNetProject) sol.GetAllProjects ().First ();
			Assert.AreEqual ("Program8_test1.cs", p.Files[7].FilePath.FileName, "Item group conditions are not ignored");
			Assert.AreEqual ("Program9_$(GGG).cs", p.Files[8].FilePath.FileName, "Non-evaluable property group clears properties");
			Assert.AreEqual ("Program10_$(AAA", p.Files[9].FilePath.FileName, "Invalid property reference");
			Assert.AreEqual ("Program11_EnvTest.cs", p.Files[10].FilePath.FileName, "Environment variable");
		}

		async Task LoadBuildVSConsoleProject (string vsVersion, string toolsVersion)
		{
			string solFile = Util.GetSampleProject ("ConsoleApp-VS" + vsVersion, "ConsoleApplication.sln");
			var monitor = new ProgressMonitor ();
			var sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (monitor, solFile);
			Assert.IsTrue (monitor.Errors.Length == 0);
			Assert.IsTrue (monitor.Warnings.Length == 0);
			var p = (DotNetProject) sol.GetAllProjects ().First ();
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
			string solXml = Util.ReadAllWithWindowsEndings (solFile);
			string projectXml = Util.ReadAllWithWindowsEndings (projectFile);

			await sol.SaveAsync (monitor);
			Assert.IsTrue (monitor.Errors.Length == 0);
			Assert.IsTrue (monitor.Warnings.Length == 0);

			Assert.AreEqual (projectXml, Util.ReadAllWithWindowsEndings (projectFile));
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

		[Ignore ("ToolsVersion 12.0 does not yet work w/ xbuild")]
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

			string projectXml1 = Util.GetXmlFileInfoset (proj);
			await sol.SaveAsync (new ProgressMonitor ());

			string projectXml2 = Util.GetXmlFileInfoset (proj);
			Assert.AreEqual (projectXml1, projectXml2);
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
		}

		[Test]
		public async Task ProjectWithCustomConfigPropertyGroupBug20554 ()
		{
			string solFile = Util.GetSampleProject ("console-project-custom-configs", "ConsoleProject.sln");
			Solution sol = await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile) as Solution;

			string proj = sol.GetAllProjects ().First ().FileName;

			string projectXml1 = Util.GetXmlFileInfoset (proj);
			await sol.SaveAsync (new ProgressMonitor ());

			string projectXml2 = Util.GetXmlFileInfoset (proj);
			Assert.AreEqual (projectXml1, projectXml2);
		}
	}
}
