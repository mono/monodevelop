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

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class MSBuildTests: TestBase
	{
		[Test()]
		public void LoadSaveBuildConsoleProject()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			
			WorkspaceItem item = Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			Assert.IsTrue (item is Solution);
			
			Solution sol = (Solution) item;
			TestProjectsChecks.CheckBasicVsConsoleProject (sol);
			string projectFile = ((Project)sol.Items [0]).FileName;
			
			BuildResult cr = item.Build (Util.GetMonitor (), "Debug");
			Assert.IsNotNull (cr);
			Assert.AreEqual (0, cr.ErrorCount);
			Assert.AreEqual (0, cr.WarningCount);
			
			string solXml = File.ReadAllText (solFile);
			string projectXml = Util.GetXmlFileInfoset (projectFile);
			
			sol.Save (Util.GetMonitor ());
			
			Assert.AreEqual (solXml, File.ReadAllText (solFile));
			Assert.AreEqual (projectXml, Util.GetXmlFileInfoset (projectFile));
		}

		[Test]
		public void BuildConsoleProject ()
		{
			Solution sol = TestProjectsChecks.CreateConsoleSolution ("console-project-msbuild");
			sol.Save (Util.GetMonitor ());

			// Ensure the project is buildable
			var result = sol.Build (Util.GetMonitor (), "Debug");
			Assert.AreEqual (0, result.ErrorCount, "#1");

			// Ensure the project is still buildable with xbuild after a rename
			ProjectOptionsDialog.RenameItem (sol.GetAllProjects () [0], "Test");
			result = sol.Build (Util.GetMonitor (), "Release");
			Assert.AreEqual (0, result.ErrorCount, "#2");
		}

		[Test]
		public void CreateConsoleProject ()
		{
			Solution sol = TestProjectsChecks.CreateConsoleSolution ("console-project-msbuild");
			sol.ConvertToFormat (Util.FileFormatMSBuild10, true);
			sol.Save (Util.GetMonitor ());
			
			// msbuild format

			string solXml = File.ReadAllText (sol.FileName);
			string projectXml = Util.GetXmlFileInfoset (((SolutionEntityItem)sol.Items [0]).FileName);
			
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
		public void TestCreateLoadSaveConsoleProject ()
		{
			TestProjectsChecks.TestCreateLoadSaveConsoleProject ("MSBuild05");
		}
		
		[Test]
		public void GenericProject ()
		{
			TestProjectsChecks.CheckGenericItemProject ("MSBuild05");
		}
		
		[Test]
		public void TestLoadSaveSolutionFolders ()
		{
			TestProjectsChecks.TestLoadSaveSolutionFolders ("MSBuild05");
		}
		
		[Test]
		public void TestLoadSaveResources ()
		{
			TestProjectsChecks.TestLoadSaveResources ("MSBuild05");
		}
		
		[Test]
		public void TestConfigurationMerging ()
		{
			string solFile = Util.GetSampleProject ("test-configuration-merging", "TestConfigurationMerging.sln");
			Solution sol = Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile) as Solution;
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

			sol.Save (Util.GetMonitor ());
			Assert.AreEqual (1, pars.WarningLevel);

			string savedFile = Path.Combine (p.BaseDirectory, "TestConfigurationMergingSaved.csproj");
			Assert.AreEqual (Util.GetXmlFileInfoset (savedFile), Util.GetXmlFileInfoset (p.FileName));
		}
		
		[Test]
		public void TestConfigurationMergingConfigPlatformCombinations ()
		{
			string projectFile = Util.GetSampleProject ("test-configuration-merging", "TestConfigurationMerging2.csproj");
			DotNetProject p = Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile) as DotNetProject;
			Assert.IsNotNull (p);

			Assert.IsNotNull (p.Configurations ["Debug|x86"]);
			Assert.IsNotNull (p.Configurations ["Debug|x86-64"]);
			Assert.IsNotNull (p.Configurations ["Debug|Other"]);

			Assert.IsNotNull (p.Configurations ["Release|x86"]);
			Assert.IsNotNull (p.Configurations ["Release|x86-64"]);
			Assert.IsNotNull (p.Configurations ["Release|Other"]);
			
			string originalContent = Util.GetXmlFileInfoset (p.FileName);
			
			p.Save (Util.GetMonitor ());

			Assert.AreEqual (originalContent, Util.GetXmlFileInfoset (p.FileName));
		}
		
		[Test]
		public void TestConfigurationMergingDefaultValues ()
		{
			string projectFile = Util.GetSampleProject ("test-configuration-merging", "TestConfigurationMerging3.csproj");
			DotNetProject p = Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile) as DotNetProject;
			Assert.IsNotNull (p);

			DotNetProjectConfiguration conf = p.Configurations ["Release|x86"] as DotNetProjectConfiguration;
			Assert.IsNotNull (conf);
			Assert.IsTrue (conf.DebugMode);
			conf.DebugMode = false;
			CSharpCompilerParameters cparams = (CSharpCompilerParameters) conf.CompilationParameters;
			Assert.IsTrue (cparams.UnsafeCode);
			cparams.UnsafeCode = false;
			
			p.Save (Util.GetMonitor ());

			Assert.AreEqual (Util.GetXmlFileInfoset (p.FileName + ".saved"), Util.GetXmlFileInfoset (p.FileName));
		}
		
		[Test]
		public void TestConfigurationMergingKeepOldConfig ()
		{
			string projectFile = Util.GetSampleProject ("test-configuration-merging", "TestConfigurationMerging4.csproj");
			DotNetProject p = Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile) as DotNetProject;
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
			
			p.Save (Util.GetMonitor ());

			Assert.AreEqual (Util.GetXmlFileInfoset (p.FileName + ".saved"), Util.GetXmlFileInfoset (p.FileName));
		}
		
		[Test]
		public void TestConfigurationMergingChangeNoMergeToParent ()
		{
			string projectFile = Util.GetSampleProject ("test-configuration-merging", "TestConfigurationMerging5.csproj");
			DotNetProject p = Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile) as DotNetProject;
			Assert.IsNotNull (p);

			DotNetProjectConfiguration conf = p.Configurations ["Debug|x86"] as DotNetProjectConfiguration;
			Assert.IsNotNull (conf);
			Assert.IsTrue (conf.SignAssembly);
			
			conf = p.Configurations ["Release|x86"] as DotNetProjectConfiguration;
			Assert.IsNotNull (conf);
			Assert.IsTrue (conf.SignAssembly);
			conf.SignAssembly = false;
			
			p.Save (Util.GetMonitor ());

			Assert.AreEqual (Util.GetXmlFileInfoset (p.FileName + ".saved"), Util.GetXmlFileInfoset (p.FileName));
		}
		
		[Test]
		public void TestConfigurationMergingChangeMergeToParent ()
		{
			string projectFile = Util.GetSampleProject ("test-configuration-merging", "TestConfigurationMerging6.csproj");
			DotNetProject p = Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile) as DotNetProject;
			Assert.IsNotNull (p);

			DotNetProjectConfiguration conf = p.Configurations ["Debug|x86"] as DotNetProjectConfiguration;
			Assert.IsNotNull (conf);
			Assert.IsTrue (conf.SignAssembly);
			conf.SignAssembly = false;
			
			conf = p.Configurations ["Release|x86"] as DotNetProjectConfiguration;
			Assert.IsNotNull (conf);
			Assert.IsTrue (conf.SignAssembly);
			conf.SignAssembly = false;
			
			p.Save (Util.GetMonitor ());

			Assert.AreEqual (Util.GetXmlFileInfoset (p.FileName + ".saved"), Util.GetXmlFileInfoset (p.FileName));
		}
		
		[Test]
		public void TestConfigurationMergingChangeMergeToParent2 ()
		{
			string projectFile = Util.GetSampleProject ("test-configuration-merging", "TestConfigurationMerging7.csproj");
			DotNetProject p = Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile) as DotNetProject;
			Assert.IsNotNull (p);

			DotNetProjectConfiguration conf = p.Configurations ["Debug|x86"] as DotNetProjectConfiguration;
			Assert.IsNotNull (conf);
			Assert.IsTrue (conf.SignAssembly);
			conf.SignAssembly = true;
			
			conf = p.Configurations ["Release|x86"] as DotNetProjectConfiguration;
			Assert.IsNotNull (conf);
			Assert.IsFalse (conf.SignAssembly);
			conf.SignAssembly = true;
			
			p.Save (Util.GetMonitor ());

			Assert.AreEqual (Util.GetXmlFileInfoset (p.FileName + ".saved"), Util.GetXmlFileInfoset (p.FileName));
		}
		
		[Test]
		public void ProjectReferenceWithSpace ()
		{
			string solFile = Util.GetSampleProject ("project-ref-with-spaces", "project-ref-with-spaces.sln");
			Solution sol = Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile) as Solution;
			Assert.IsNotNull (sol);
			Assert.AreEqual (2, sol.Items.Count);

			DotNetProject p = sol.FindProjectByName ("project-ref-with-spaces") as DotNetProject;
			Assert.IsNotNull (p);
			
			Assert.AreEqual (1, p.References.Count);
			Assert.AreEqual ("some - library", p.References[0].Reference);
		}

		[Test]
		public void RoundtripPropertyWithXmlCharacters ()
		{
			Solution sol = TestProjectsChecks.CreateConsoleSolution ("roundtrip-property-with-xml");
			sol.ConvertToFormat (Util.FileFormatMSBuild05, true);

			var value = "Hello<foo>&.exe";

			var p = (DotNetProject) sol.GetAllProjects ().First ();
			var conf = ((DotNetProjectConfiguration)p.Configurations [0]);
			conf.OutputAssembly = value;
			sol.Save (Util.GetMonitor ());

			sol = (Solution) Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), sol.FileName);
			p = (DotNetProject) sol.GetAllProjects ().First ();
			conf = ((DotNetProjectConfiguration)p.Configurations [0]);

			Assert.AreEqual (value, conf.OutputAssembly);
		}

		[Test]
		public void EvaluateProperties ()
		{
			string dir = Path.GetDirectoryName (typeof(Project).Assembly.Location);
			Environment.SetEnvironmentVariable ("HHH", "EnvTest");
			Environment.SetEnvironmentVariable ("SOME_PLACE", dir);

			string solFile = Util.GetSampleProject ("property-evaluation-test", "property-evaluation-test.sln");
			Solution sol = Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile) as Solution;
			var p = (DotNetProject) sol.GetAllProjects ().First ();
			Assert.AreEqual ("Program1_test1.cs", p.Files[0].FilePath.FileName, "Basic replacement");
			Assert.AreEqual ("Program2_test1_test2.cs", p.Files[1].FilePath.FileName, "Property referencing same property");
			Assert.AreEqual ("Program3_$(DebugType).cs", p.Files[2].FilePath.FileName, "Property inside group with non-evaluable condition");
			Assert.AreEqual ("Program4_yes_value.cs", p.Files[3].FilePath.FileName, "Evaluation of group condition");
			Assert.AreEqual ("Program5_yes_value.cs", p.Files[4].FilePath.FileName, "Evaluation of property condition");
			Assert.AreEqual ("Program6_$(FFF).cs", p.Files[5].FilePath.FileName, "Evaluation of property with non-evaluable condition");
			Assert.AreEqual ("Program7_test1.cs", p.Files[6].FilePath.FileName, "Item conditions are ignored");
			Assert.AreEqual ("Program8_test1.cs", p.Files[7].FilePath.FileName, "Item group conditions are ignored");
			Assert.AreEqual ("Program9_$(GGG).cs", p.Files[8].FilePath.FileName, "Non-evaluable property group clears properties");
			Assert.AreEqual ("Program10_$(AAA", p.Files[9].FilePath.FileName, "Invalid property reference");
			Assert.AreEqual ("Program11_EnvTest.cs", p.Files[10].FilePath.FileName, "Environment variable");

			var testRef = Path.Combine (dir, "MonoDevelop.Core.dll");
			var asms = p.GetReferencedAssemblies (sol.Configurations [0].Selector).ToArray ();
			Assert.IsTrue (asms.Contains (testRef));
		}

		void LoadBuildVSConsoleProject (string vsVersion, string toolsVersion)
		{
			string solFile = Util.GetSampleProject ("ConsoleApp-VS" + vsVersion, "ConsoleApplication.sln");
			var monitor = new NullProgressMonitor ();
			var sol = (Solution)Services.ProjectService.ReadWorkspaceItem (monitor, solFile);
			Assert.IsTrue (monitor.Errors.Length == 0);
			Assert.IsTrue (monitor.Warnings.Length == 0);
			var p = (DotNetProject) sol.GetAllProjects ().First ();
			Assert.AreEqual (toolsVersion, MSBuildProjectService.GetHandler (p).ToolsVersion);
			var r = sol.Build (monitor, "Debug");
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

			sol.Save (monitor);
			Assert.IsTrue (monitor.Errors.Length == 0);
			Assert.IsTrue (monitor.Warnings.Length == 0);

			Assert.AreEqual (projectXml, Util.ReadAllWithWindowsEndings (projectFile));
		}

		[Test]
		public void LoadBuildVS2010ConsoleProject ()
		{
			LoadBuildVSConsoleProject ("2010", "4.0");
		}

		[Test]
		public void LoadBuildVS2012ConsoleProject ()
		{
			LoadBuildVSConsoleProject ("2012", "4.0");
		}

		[Ignore ("ToolsVersion 12.0 does not yet work w/ xbuild")]
		[Test]
		public void LoadBuildVS2013ConsoleProject ()
		{
			LoadBuildVSConsoleProject ("2013", "12.0");
		}
	}
}
