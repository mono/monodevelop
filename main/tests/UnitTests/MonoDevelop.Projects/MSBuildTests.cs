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
using CSharpBinding;

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
		public void CreateConsoleProject ()
		{
			Solution sol = TestProjectsChecks.CreateConsoleSolution ("console-project-msbuild");
			sol.Save (Util.GetMonitor ());
			
			// msbuild format

			string solXml = File.ReadAllText (sol.FileName);
			string projectXml = Util.GetXmlFileInfoset (((SolutionEntityItem)sol.Items [0]).FileName);
			
			// Make sure we compare using the same guid
			Project p = sol.Items [0] as Project;
			string guid = p.ItemId;
			solXml = solXml.Replace (guid, "{DC577202-654B-4FDB-95C7-8CC5DDF6D32D}");
			projectXml = projectXml.Replace (guid, "{DC577202-654B-4FDB-95C7-8CC5DDF6D32D}");
			
			string solFile = Util.GetSampleProjectPath ("generated-console-project", "TestSolution.sln");
			string projectFile = Util.GetSampleProjectPath ("generated-console-project", "TestProject.csproj");
			
			Assert.AreEqual (File.ReadAllText (solFile), solXml);
			Assert.AreEqual (Util.GetXmlFileInfoset (projectFile), projectXml);

			// MD1 format
			
			sol.ConvertToFormat (Util.FileFormatMD1, true);
			sol.Save (Util.GetMonitor ());

			solXml = Util.GetXmlFileInfoset (sol.FileName);
			projectXml = Util.GetXmlFileInfoset (((SolutionEntityItem)sol.Items [0]).FileName);

			solFile = Util.GetSampleProjectPath ("generated-console-project", "TestSolution.mds");
			projectFile = Util.GetSampleProjectPath ("generated-console-project", "TestProject.mdp");
			
			Assert.AreEqual (Util.GetXmlFileInfoset (solFile), solXml, "solXml: " + sol.FileName);
			Assert.AreEqual (Util.GetXmlFileInfoset (projectFile), projectXml, "projectXml: " + ((SolutionEntityItem)sol.Items [0]).FileName);
			
//			sol.FileFormat = Services.ProjectService.FileFormats.GetFileFormat ("MSBuild08");
//			sol.Save (Util.GetMonitor ());
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
			
			DotNetProjectConfiguration conf = p.GetConfiguration ("Debug") as DotNetProjectConfiguration;
			Assert.IsNotNull (conf);
			Assert.AreEqual ("Debug", conf.Name);
			Assert.AreEqual (string.Empty, conf.Platform);

			CSharpCompilerParameters pars = conf.CompilationParameters as CSharpCompilerParameters;
			Assert.IsNotNull (pars);
			Assert.AreEqual (2, pars.WarningLevel);

			pars.WarningLevel = 4;

			// Release config
			
			conf = p.GetConfiguration ("Release") as DotNetProjectConfiguration;
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
	}
}
