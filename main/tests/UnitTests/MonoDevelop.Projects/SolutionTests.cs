// SolutionTests.cs
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
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using UnitTests;
using MonoDevelop.Core;
using MonoDevelop.Projects.MSBuild;
using MonoDevelop.Core.ProgressMonitoring;
using System.Threading.Tasks;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects.Extensions;

namespace MonoDevelop.Projects
{
	[TestFixture()]
	public class SolutionTests: TestBase
	{
		public static string GetMdb (Project p, string file)
		{
			return ((DotNetProject)p).GetAssemblyDebugInfoFile (p.Configurations [0].Selector, file);
		}

		[Test()]
		public void SolutionItemsEvents()
		{
			int countFileAddedToProject = 0;
			int countFileRemovedFromProject = 0;
			int countFileRenamedInProject = 0;
			int countReferenceAddedToProject = 0;
			int countReferenceRemovedFromProject = 0;
			int countSolutionItemAdded = 0;
			int countSolutionItemRemoved = 0;
			
			Solution sol = new Solution ();
			sol.FileAddedToProject += delegate {
				countFileAddedToProject++;
			};
			sol.FileRemovedFromProject += delegate {
				countFileRemovedFromProject++;
			};
			sol.FileRenamedInProject += delegate {
				countFileRenamedInProject++;
			};
			sol.ReferenceAddedToProject += delegate {
				countReferenceAddedToProject++;
			};
			sol.ReferenceRemovedFromProject += delegate {
				countReferenceRemovedFromProject++;
			};
			sol.SolutionItemAdded += delegate {
				countSolutionItemAdded++;
			};
			sol.SolutionItemRemoved += delegate {
				countSolutionItemRemoved++;
			};
			
			Assert.AreEqual (0, countFileAddedToProject);
			Assert.AreEqual (0, countFileRemovedFromProject);
			Assert.AreEqual (0, countFileRenamedInProject);
			Assert.AreEqual (0, countReferenceAddedToProject);
			Assert.AreEqual (0, countReferenceRemovedFromProject);
			Assert.AreEqual (0, countSolutionItemAdded);
			Assert.AreEqual (0, countSolutionItemRemoved);
			
			SolutionFolder folder = new SolutionFolder ();
			folder.Name = "Folder1";
			sol.RootFolder.Items.Add (folder);
			
			Assert.AreEqual (1, countSolutionItemAdded);
			Assert.AreEqual (0, sol.Items.Count);
			
			var project = Services.ProjectService.CreateDotNetProject ("C#");
			project.Name = "project1";
			sol.RootFolder.Items.Add (project);
			
			Assert.AreEqual (2, countSolutionItemAdded);
			Assert.AreEqual (1, sol.Items.Count);
			
			var project2 = Services.ProjectService.CreateDotNetProject ("C#");
			project2.Name = "project2";
			folder.Items.Add (project2);
			
			Assert.AreEqual (3, countSolutionItemAdded);
			Assert.AreEqual (2, sol.Items.Count);
			
			ProjectFile p1 = new ProjectFile ("test1.cs");
			project2.Files.Add (p1);
			Assert.AreEqual (1, countFileAddedToProject);
			
			ProjectFile p2 = new ProjectFile ("test1.cs");
			project.Files.Add (p2);
			Assert.AreEqual (2, countFileAddedToProject);
			
			p1.Name = "test2.cs";
			Assert.AreEqual (1, countFileRenamedInProject);
			
			p2.Name = "test2.cs";
			Assert.AreEqual (2, countFileRenamedInProject);
			
			project2.Files.Remove (p1);
			Assert.AreEqual (1, countFileRemovedFromProject);
			
			project.Files.Remove ("test2.cs");
			Assert.AreEqual (2, countFileRemovedFromProject);
			
			ProjectReference pr1 = ProjectReference.CreateAssemblyReference ("SomeTest");
			project.References.Add (pr1);
			Assert.AreEqual (1, countReferenceAddedToProject);
			
			ProjectReference pr2 = ProjectReference.CreateProjectReference (project);
			project2.References.Add (pr2);
			Assert.AreEqual (2, countReferenceAddedToProject);
			
			project.References.Remove (pr1);
			Assert.AreEqual (1, countReferenceRemovedFromProject);
			
			sol.RootFolder.Items.Remove (project);
			Assert.AreEqual (2, countReferenceRemovedFromProject, "Removing a project must remove all references to it");
			Assert.AreEqual (1, countSolutionItemRemoved);
			Assert.AreEqual (1, sol.Items.Count);
			
			folder.Items.Remove (project2);
			Assert.AreEqual (2, countSolutionItemRemoved);
			Assert.AreEqual (0, sol.Items.Count);
			
			sol.RootFolder.Items.Remove (folder);
			
			Assert.AreEqual (2, countFileAddedToProject);
			Assert.AreEqual (2, countFileRemovedFromProject);
			Assert.AreEqual (2, countFileRenamedInProject);
			Assert.AreEqual (2, countReferenceAddedToProject);
			Assert.AreEqual (2, countReferenceRemovedFromProject);
			Assert.AreEqual (3, countSolutionItemAdded);
			Assert.AreEqual (3, countSolutionItemRemoved);
		}
		
		[Test()]
		public void SolutionName ()
		{
			int nameChanges = 0;
			
			Solution sol = new Solution ();
			sol.NameChanged += delegate {
				nameChanges++;
			};
			
			string tmp = Path.GetTempPath ();

			sol.Name = "test1";
			Assert.AreEqual ("test1", sol.Name);
			Assert.AreEqual ("test1.sln", (string) sol.FileName);
			Assert.AreEqual (1, nameChanges);
			
			sol.Name = "test2";
			Assert.AreEqual ("test2", sol.Name);
			Assert.AreEqual ("test2.sln", (string) sol.FileName);
			Assert.AreEqual (2, nameChanges);
			
			sol.FileName = Path.Combine (tmp, "test3.sln");
			Assert.AreEqual ("test3", sol.Name);
			Assert.AreEqual (Path.Combine (tmp, "test3.sln"), (string) sol.FileName);
			Assert.AreEqual (3, nameChanges);
			
			sol.Name = "test4";
			Assert.AreEqual ("test4", sol.Name);
			Assert.AreEqual (Path.Combine (tmp, "test4.sln"), (string) sol.FileName);
			Assert.AreEqual (4, nameChanges);
			
			sol.ConvertToFormat (MSBuildFileFormat.VS2010);
			Assert.AreEqual ("test4", sol.Name);
			Assert.AreEqual (Path.Combine (tmp, "test4.sln"), (string) sol.FileName);
			Assert.AreEqual (4, nameChanges);
		}
		
		[Test()]
		public void ProjectName ()
		{
			int nameChanges = 0;
			
			var prj = Services.ProjectService.CreateDotNetProject ("C#");
			prj.FileFormat = MSBuildFileFormat.VS2005;
			prj.NameChanged += delegate {
				nameChanges++;
			};
			
			prj.Name = "test1";
			Assert.AreEqual ("test1", prj.Name);
			Assert.AreEqual ("test1.csproj", (string) prj.FileName);
			Assert.AreEqual (1, nameChanges);
			
			prj.Name = "test2";
			Assert.AreEqual ("test2", prj.Name);
			Assert.AreEqual ("test2.csproj", (string) prj.FileName);
			Assert.AreEqual (2, nameChanges);
			
			string fname = Path.Combine (Path.GetTempPath (), "test3.csproj");
			prj.FileName = fname;
			Assert.AreEqual ("test3", prj.Name);
			Assert.AreEqual (fname, (string) prj.FileName);
			Assert.AreEqual (3, nameChanges);
			
			prj.Name = "test4";
			Assert.AreEqual ("test4", prj.Name);
			Assert.AreEqual (Path.Combine (Path.GetTempPath (), "test4.csproj"), (string) prj.FileName);
			Assert.AreEqual (4, nameChanges);
			
			prj.FileFormat = MSBuildFileFormat.VS2012;
			Assert.AreEqual ("test4", prj.Name);
			Assert.AreEqual (Path.Combine (Path.GetTempPath (), "test4.csproj"), (string) prj.FileName);
			Assert.AreEqual (4, nameChanges);
			Assert.AreEqual ("MSBuild12", prj.FileFormat.Id);
			
			// Projects inherit the file format from the parent solution
			Solution sol = new Solution ();
			sol.ConvertToFormat (MSBuildFileFormat.VS2005);
			sol.RootFolder.Items.Add (prj);
			Assert.AreEqual ("test4", prj.Name);
			Assert.AreEqual (Path.Combine (Path.GetTempPath (), "test4.csproj"), (string) prj.FileName);
			Assert.AreEqual (4, nameChanges);
			Assert.AreEqual ("MSBuild05", prj.FileFormat.Id);

			// Removing the project from the solution should not restore the old format
			sol.RootFolder.Items.Remove (prj);
			Assert.AreEqual ("MSBuild05", prj.FileFormat.Id);
			Assert.AreEqual ("test4", prj.Name);
			Assert.AreEqual (Path.Combine (Path.GetTempPath (), "test4.csproj"), (string) prj.FileName);
			Assert.AreEqual (4, nameChanges);
		}
		
		[Test()]
		public async Task Reloading ()
		{
			Solution sol = TestProjectsChecks.CreateConsoleSolution ("reloading");
			await sol.SaveAsync (Util.GetMonitor ());
			Assert.IsFalse (sol.NeedsReload);
			
			Project p = sol.Items [0] as Project;
			Assert.IsFalse (p.NeedsReload);
			
			// Changing format must reset the reload flag (it's like we just created a new solution in memory)
			sol.ConvertToFormat (MSBuildFileFormat.VS2010);
			Assert.IsFalse (sol.NeedsReload);
			Assert.IsFalse (p.NeedsReload);
			sol.ConvertToFormat (MSBuildFileFormat.VS2012);
			Assert.IsFalse (sol.NeedsReload);
			Assert.IsFalse (p.NeedsReload);
			
			sol.RootFolder.Items.Remove (p);
			Assert.IsFalse (p.NeedsReload);
			p.FileFormat = MSBuildFileFormat.VS2012;
			Assert.IsFalse (p.NeedsReload);
			sol.RootFolder.Items.Add (p);
			Assert.IsFalse (p.NeedsReload);
			sol.RootFolder.Items.Remove (p);
			Assert.IsFalse (p.NeedsReload);
			p.FileFormat = MSBuildFileFormat.VS2005;
			Assert.IsFalse (p.NeedsReload);
			sol.RootFolder.Items.Add (p);
			Assert.IsFalse (p.NeedsReload);

			string solFile2 = Util.GetSampleProject ("csharp-console", "csharp-console.sln");
			Solution sol2 = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile2);
			Project p2 = sol2.Items [0] as Project;
			Assert.IsFalse (sol2.NeedsReload);
			Assert.IsFalse (p2.NeedsReload);
			
			// Check reloading flag in another solution
			
			string solFile = sol.FileName;
			Solution sol3 = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			Assert.IsFalse (sol3.NeedsReload);
			
			Project p3 = sol3.Items [0] as Project;
			Assert.IsFalse (p3.NeedsReload);
			
			System.Threading.Thread.Sleep (1000);
			sol.Description = "Foo"; // Small change to force the solution file save
			await sol.SaveAsync (Util.GetMonitor ());
			
			Assert.IsTrue (sol3.NeedsReload);
		}
		
		[Test()]
		public async Task ReloadingReferencedProject ()
		{
			string solFile = Util.GetSampleProject ("console-with-libs", "console-with-libs.sln");
			
			Solution sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			DotNetProject p = (DotNetProject) sol.FindProjectByName ("console-with-libs");
			DotNetProject lib2 = (DotNetProject) sol.FindProjectByName ("library2");
			
			Assert.AreEqual (3, p.References.Count);
			
			await lib2.ParentFolder.ReloadItem (Util.GetMonitor (), lib2);
			
			Assert.AreEqual (3, p.References.Count);
		}
		
		[Test()]
		public async Task ReloadingKeepsBuildConfigurationAndStartupProject ()
		{
			string solFile = Util.GetSampleProject ("console-with-libs", "console-with-libs.sln");

			Solution sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			DotNetProject p = (DotNetProject) sol.FindProjectByName ("console-with-libs");
			DotNetProject lib2 = (DotNetProject) sol.FindProjectByName ("library2");

			Assert.AreSame (sol.StartupItem, p);

			var be = sol.Configurations ["Debug"].GetEntryForItem (lib2);
			be.Build = false;
			be.ItemConfiguration = "FooConfig";
			await sol.SaveAsync (Util.GetMonitor ());

			// Test that build configuration info is not lost when reloading a project

			lib2 = (DotNetProject) await lib2.ParentFolder.ReloadItem (Util.GetMonitor (), lib2);

			be = sol.Configurations ["Debug"].GetEntryForItem (lib2);
			Assert.IsFalse (be.Build);
			Assert.AreEqual ("FooConfig", be.ItemConfiguration);

			// Test that startup project is the reloaded project

			p = (DotNetProject) await p.ParentFolder.ReloadItem (Util.GetMonitor (), p);
			Assert.AreSame (sol.StartupItem, p);
		}

		[Test()]
		public void GetItemFiles ()
		{
			Solution sol = TestProjectsChecks.CreateConsoleSolution ("item-files");
			
			List<FilePath> files = sol.GetItemFiles (false).ToList ();
			Assert.AreEqual (1, files.Count);
			Assert.AreEqual (sol.FileName, files [0]);
			
			DotNetProject p = (DotNetProject) sol.Items [0];
			files = p.GetItemFiles (false).ToList ();
			Assert.AreEqual (1, files.Count);
			Assert.AreEqual (p.FileName, files [0]);
			
			files = p.GetItemFiles (true).ToList ();
			Assert.AreEqual (6, files.Count);
			Assert.IsTrue (files.Contains (p.FileName));
			foreach (ProjectFile pf in p.Files)
				Assert.IsTrue (files.Contains (pf.FilePath), "Contains " + pf.FilePath);
			
			files = sol.GetItemFiles (true).ToList ();
			Assert.AreEqual (7, files.Count);
			Assert.IsTrue (files.Contains (sol.FileName));
			Assert.IsTrue (files.Contains (p.FileName));
			foreach (ProjectFile pf in p.Files)
				Assert.IsTrue (files.Contains (pf.FilePath), "Contains " + pf.FilePath);
		}
		
		[Test()]
		public async Task NeedsBuilding ()
		{
			string solFile = Util.GetSampleProject ("console-with-libs", "console-with-libs.sln");
			
			Solution sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			DotNetProject p = (DotNetProject) sol.FindProjectByName ("console-with-libs");
			Assert.IsNotNull (p);
			DotNetProject lib1 = (DotNetProject) sol.FindProjectByName ("library1");
			Assert.IsNotNull (lib1);
			DotNetProject lib2 = (DotNetProject) sol.FindProjectByName ("library2");
			Assert.IsNotNull (lib2);
			
			SolutionConfigurationSelector config = (SolutionConfigurationSelector) "Debug";
			
			Assert.IsTrue (p. NeedsBuilding (config));
			Assert.IsTrue (lib1.NeedsBuilding (config));
			Assert.IsTrue (lib2.NeedsBuilding (config));
			
			// Build the project and the references
			
			BuildResult res = await p.Build (Util.GetMonitor (), config, true);
			foreach (BuildError er in res.Errors)
				Console.WriteLine (er);
			Assert.AreEqual (0, res.ErrorCount);
			Assert.AreEqual (0, res.WarningCount);
			Assert.AreEqual (3, res.BuildCount);

			Assert.IsTrue (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", "console-with-libs.exe")));
			Assert.IsTrue (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", GetMdb (p, "console-with-libs.exe"))));
			Assert.IsTrue (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", "library1.dll")));
			Assert.IsTrue (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", GetMdb (lib1, "library1.dll"))));
			Assert.IsTrue (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", "library2.dll")));
			Assert.IsTrue (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", GetMdb (lib2, "library2.dll"))));
			
			// Build the project, but not the references
			
			res = await p.Build (Util.GetMonitor (), config, false);
			Assert.AreEqual (0, res.ErrorCount);
			Assert.AreEqual (0, res.WarningCount);
			Assert.AreEqual (1, res.BuildCount);
		}
		
		[Test()]
		public async Task BuildingAndCleaning ()
		{
			string solFile = Util.GetSampleProject ("console-with-libs", "console-with-libs.sln");
			
			Solution sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			DotNetProject p = (DotNetProject) sol.FindProjectByName ("console-with-libs");
			DotNetProject lib1 = (DotNetProject) sol.FindProjectByName ("library1");
			DotNetProject lib2 = (DotNetProject) sol.FindProjectByName ("library2");
			
			SolutionFolder folder = new SolutionFolder ();
			folder.Name = "subfolder";
			sol.RootFolder.Items.Add (folder);
			sol.RootFolder.Items.Remove (lib2);
			folder.Items.Add (lib2);
			
			Workspace ws = new Workspace ();
			ws.FileName = Path.Combine (sol.BaseDirectory, "workspace");
			ws.Items.Add (sol);
			await ws.SaveAsync (Util.GetMonitor ());
			
			// Build the project and the references
			
			BuildResult res = await ws.Build (Util.GetMonitor (), ConfigurationSelector.Default);
			Assert.AreEqual (0, res.ErrorCount);
			Assert.AreEqual (0, res.WarningCount);
			Assert.AreEqual (3, res.BuildCount);
			
			Assert.IsTrue (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", "console-with-libs.exe")));
			Assert.IsTrue (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", GetMdb (p, "console-with-libs.exe"))));
			Assert.IsTrue (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", "library1.dll")));
			Assert.IsTrue (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", GetMdb (lib1, "library1.dll"))));
			Assert.IsTrue (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", "library2.dll")));
			Assert.IsTrue (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", GetMdb (lib2, "library2.dll"))));
			
			// Clean the workspace
			
			await ws.Clean (Util.GetMonitor (), ConfigurationSelector.Default);
			Assert.IsFalse (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", "console-with-libs.exe")));
			Assert.IsFalse (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", GetMdb (p, "console-with-libs.exe"))));
			Assert.IsFalse (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", "library1.dll")));
			Assert.IsFalse (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", GetMdb (lib1, "library1.dll"))));
			Assert.IsFalse (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", "library2.dll")));
			Assert.IsFalse (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", GetMdb (lib2, "library2.dll"))));
			
			// Build the solution
			
			res = await ws.Build (Util.GetMonitor (), ConfigurationSelector.Default);
			Assert.AreEqual (0, res.ErrorCount);
			Assert.AreEqual (0, res.WarningCount);
			Assert.AreEqual (3, res.BuildCount);
			
			Assert.IsTrue (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", "console-with-libs.exe")));
			Assert.IsTrue (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", GetMdb (p, "console-with-libs.exe"))));
			Assert.IsTrue (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", "library1.dll")));
			Assert.IsTrue (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", GetMdb (lib1, "library1.dll"))));
			Assert.IsTrue (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", "library2.dll")));
			Assert.IsTrue (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", GetMdb (lib2, "library2.dll"))));
			
			// Clean the solution
			
			await sol.Clean (Util.GetMonitor (), "Debug");
			Assert.IsFalse (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", "console-with-libs.exe")));
			Assert.IsFalse (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", GetMdb (p, "console-with-libs.exe"))));
			Assert.IsFalse (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", "library1.dll")));
			Assert.IsFalse (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", GetMdb (lib1, "library1.dll"))));
			Assert.IsFalse (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", "library2.dll")));
			Assert.IsFalse (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", GetMdb (lib2, "library2.dll"))));
			
			// Build the solution folder
			
			res = await folder.Build (Util.GetMonitor (), (SolutionConfigurationSelector) "Debug");
			Assert.AreEqual (0, res.ErrorCount);
			Assert.AreEqual (0, res.WarningCount);
			Assert.AreEqual (1, res.BuildCount);
			
			Assert.IsFalse (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", "console-with-libs.exe")));
			Assert.IsFalse (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", GetMdb (p, "console-with-libs.exe"))));
			Assert.IsFalse (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", "library1.dll")));
			Assert.IsFalse (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", GetMdb (lib1, "library1.dll"))));
			Assert.IsTrue (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", "library2.dll")));
			Assert.IsTrue (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", GetMdb (lib2, "library2.dll"))));
			
			// Clean the solution folder
			
			await folder.Clean (Util.GetMonitor (), (SolutionConfigurationSelector) "Debug");
			Assert.IsFalse (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", "library2.dll")));
			Assert.IsFalse (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", GetMdb (lib2, "library2.dll"))));
			sol.Dispose ();
		}
		
		[Test()]
		public async Task FormatConversions ()
		{
			Solution sol = TestProjectsChecks.CreateConsoleSolution ("reloading");
			Project p = (Project) sol.Items [0];
			
			Assert.AreEqual (MSBuildFileFormat.DefaultFormat.Id, sol.FileFormat.Id);
			Assert.AreEqual (MSBuildFileFormat.DefaultFormat.Id, p.FileFormat.Id);
			Assert.AreEqual ("4.0", p.ToolsVersion);
			
			// Change solution format of unsaved solution
			
			sol.ConvertToFormat (MSBuildFileFormat.VS2008);
			
			Assert.AreEqual ("MSBuild08", sol.FileFormat.Id);
			Assert.AreEqual ("MSBuild08", p.FileFormat.Id);
			Assert.AreEqual ("3.5", p.ToolsVersion);

			sol.ConvertToFormat (MSBuildFileFormat.VS2010);
			
			Assert.AreEqual ("MSBuild10", sol.FileFormat.Id);
			Assert.AreEqual ("MSBuild10", p.FileFormat.Id);
			Assert.AreEqual ("4.0", p.ToolsVersion);

			// Change solution format of saved solution
			
			await sol.SaveAsync (Util.GetMonitor ());

			sol.ConvertToFormat (MSBuildFileFormat.VS2005);

			Assert.AreEqual ("MSBuild05", sol.FileFormat.Id);
			Assert.AreEqual ("MSBuild05", p.FileFormat.Id);
			Assert.AreEqual ("2.0", p.ToolsVersion);

			// Add new project
			
			Project newp = Services.ProjectService.CreateDotNetProject ("C#");
			Assert.AreEqual ("MSBuild12", newp.FileFormat.Id);
			Assert.AreEqual ("4.0", newp.ToolsVersion);

			sol.RootFolder.Items.Add (newp);
			Assert.AreEqual ("MSBuild05", newp.FileFormat.Id);
			Assert.AreEqual ("2.0", newp.ToolsVersion);

			// Add saved project
			
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			Solution msol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			
			Project mp = (Project) msol.Items [0];
			Assert.AreEqual ("MSBuild05", mp.FileFormat.Id);
			
			sol.RootFolder.Items.Add (newp);
			Assert.AreEqual ("MSBuild05", mp.FileFormat.Id);
		}
		
		[Test()]
		public async Task BuildConfigurationMappings ()
		{
			string solFile = Util.GetSampleProject ("test-build-configs", "test-build-configs.sln");
			
			Solution sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			DotNetProject lib1 = (DotNetProject) sol.FindProjectByName ("Lib1");
			DotNetProject lib2 = (DotNetProject) sol.FindProjectByName ("Lib2");
			DotNetProject lib3 = (DotNetProject) sol.FindProjectByName ("Lib3");
			DotNetProject lib4 = (DotNetProject) sol.FindProjectByName ("Lib4");
			
			// Check that the output file names are correct
			
			Assert.AreEqual (GetConfigFolderName (lib1, "Debug"), "Debug");
			Assert.AreEqual (GetConfigFolderName (lib1, "Release"), "Release");
			Assert.AreEqual (GetConfigFolderName (lib2, "Debug"), "Release");
			Assert.AreEqual (GetConfigFolderName (lib2, "Release"), "Debug");
			Assert.AreEqual (GetConfigFolderName (lib3, "Debug"), "DebugExtra");
			Assert.AreEqual (GetConfigFolderName (lib3, "Release"), "ReleaseExtra");

			// Check that building the solution builds the correct project configurations
			
			await CheckSolutionBuildClean (sol, "Debug");
			await CheckSolutionBuildClean (sol, "Release");
			
			// Check that building a project builds the correct referenced project configurations
			
			await CheckProjectReferencesBuildClean (sol, "Debug");
			await CheckProjectReferencesBuildClean (sol, "Release");
			
			// Single project build and clean
			
			await CheckProjectBuildClean (lib2, "Debug");
			await CheckProjectBuildClean (lib2, "Release");
			await CheckProjectBuildClean (lib3, "Debug");
			await CheckProjectBuildClean (lib3, "Release");
			await CheckProjectBuildClean (lib4, "Debug");
			await CheckProjectBuildClean (lib4, "Release");
		}
		
		async Task CheckSolutionBuildClean (Solution sol, string configuration)
		{
			SolutionConfigurationSelector config = (SolutionConfigurationSelector) configuration;
			string tag = "CheckSolutionBuildClean config=" + configuration;
			DotNetProject lib1 = (DotNetProject) sol.FindProjectByName ("Lib1");
			DotNetProject lib2 = (DotNetProject) sol.FindProjectByName ("Lib2");
			DotNetProject lib3 = (DotNetProject) sol.FindProjectByName ("Lib3");
			DotNetProject lib4 = (DotNetProject) sol.FindProjectByName ("Lib4");
			
			Assert.IsFalse (File.Exists (lib1.GetOutputFileName (config)), tag);
			Assert.IsFalse (File.Exists (lib2.GetOutputFileName (config)), tag);
			Assert.IsFalse (File.Exists (lib3.GetOutputFileName (config)), tag);
			Assert.IsFalse (File.Exists (lib4.GetOutputFileName (config)), tag);
			
			BuildResult res = await sol.Build (Util.GetMonitor (), config);
			Assert.AreEqual (0, res.WarningCount, tag);
			Assert.AreEqual (0, res.ErrorCount, tag);
			
			Assert.IsTrue (File.Exists (lib1.GetOutputFileName (config)), tag);
			Assert.IsTrue (File.Exists (lib2.GetOutputFileName (config)), tag);
			Assert.IsTrue (File.Exists (lib3.GetOutputFileName (config)), tag);
			Assert.IsTrue (File.Exists (lib4.GetOutputFileName (config)), tag);
			
			await sol.Clean (Util.GetMonitor (), config);
			
			Assert.IsFalse (File.Exists (lib1.GetOutputFileName (config)), tag);
			Assert.IsFalse (File.Exists (lib2.GetOutputFileName (config)), tag);
			Assert.IsFalse (File.Exists (lib3.GetOutputFileName (config)), tag);
			Assert.IsFalse (File.Exists (lib4.GetOutputFileName (config)), tag);
		}
		
		async Task CheckProjectReferencesBuildClean (Solution sol, string configuration)
		{
			SolutionConfigurationSelector config = (SolutionConfigurationSelector) configuration;
			string tag = "CheckProjectReferencesBuildClean config=" + configuration;
			DotNetProject lib1 = (DotNetProject) sol.FindProjectByName ("Lib1");
			DotNetProject lib2 = (DotNetProject) sol.FindProjectByName ("Lib2");
			DotNetProject lib3 = (DotNetProject) sol.FindProjectByName ("Lib3");
			DotNetProject lib4 = (DotNetProject) sol.FindProjectByName ("Lib4");
			
			Assert.IsFalse (File.Exists (lib1.GetOutputFileName (config)), tag);
			Assert.IsFalse (File.Exists (lib2.GetOutputFileName (config)), tag);
			Assert.IsFalse (File.Exists (lib3.GetOutputFileName (config)), tag);
			Assert.IsFalse (File.Exists (lib4.GetOutputFileName (config)), tag);
			
			BuildResult res = await lib1.Build (Util.GetMonitor (), config, true);
			Assert.AreEqual (0, res.WarningCount, tag);
			Assert.AreEqual (0, res.ErrorCount, tag + " " + res.CompilerOutput);
			
			Assert.IsTrue (File.Exists (lib1.GetOutputFileName (config)), tag);
			Assert.IsTrue (File.Exists (lib2.GetOutputFileName (config)), tag);
			Assert.IsTrue (File.Exists (lib3.GetOutputFileName (config)), tag);
			Assert.IsTrue (File.Exists (lib4.GetOutputFileName (config)), tag);
			
			await sol.Clean (Util.GetMonitor (), config);
		}
		
		async Task CheckProjectBuildClean (DotNetProject lib, string configuration)
		{
			SolutionConfigurationSelector config = (SolutionConfigurationSelector) configuration;
			string tag = "CheckProjectBuildClean lib=" + lib.Name + " config=" + configuration;
			
			Assert.IsFalse (File.Exists (lib.GetOutputFileName (config)), tag);
			
			BuildResult res = await lib.Build (Util.GetMonitor (), config, false);
			Assert.AreEqual (0, res.WarningCount, tag);
			Assert.AreEqual (0, res.ErrorCount, tag);
			
			Assert.IsTrue (File.Exists (lib.GetOutputFileName (config)), tag);
			
			await lib.Clean (Util.GetMonitor (), config);
			Assert.IsFalse (File.Exists (lib.GetOutputFileName (config)), tag);
		}
		
		string GetConfigFolderName (DotNetProject lib, string conf)
		{
			return Path.GetFileName (Path.GetDirectoryName (lib.GetOutputFileName ((SolutionConfigurationSelector)conf)));
		}

		[Test]
		public async Task LoadKnownUnsupportedProjects ()
		{
			string solFile = Util.GetSampleProject ("unsupported-project", "console-with-libs.sln");

			Solution sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var app = sol.GetAllItems<SolutionItem> ().FirstOrDefault (it => it.FileName.FileName == "console-with-libs.csproj");
			var lib1 = sol.GetAllItems<SolutionItem> ().FirstOrDefault (it => it.FileName.FileName == "library1.csproj");
			var lib2 = sol.GetAllItems<SolutionItem> ().FirstOrDefault (it => it.FileName.FileName == "library2.csproj");

			Assert.IsInstanceOf<DotNetProject> (app);
			Assert.IsTrue (lib1.IsUnsupportedProject);
			Assert.IsTrue (lib2.IsUnsupportedProject);

			Assert.IsInstanceOf<Project> (lib2);

			var p = (Project)lib2;

			Assert.AreEqual (2, p.Files.Count);

			p.AddFile (p.BaseDirectory.Combine ("Test.cs"), BuildAction.Compile);

			var solText = File.ReadAllLines (solFile);

			await sol.SaveAsync (new ProgressMonitor ());

			Assert.AreEqual (Util.GetXmlFileInfoset (p.FileName + ".saved"), Util.GetXmlFileInfoset (p.FileName));
			Assert.AreEqual (solText, File.ReadAllLines (solFile));
		}

		[Test]
		public async Task BuildSolutionWithUnsupportedProjects ()
		{
			string solFile = Util.GetSampleProject ("unsupported-project", "console-with-libs.sln");

			Solution sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var res = await sol.Build (Util.GetMonitor (), "Debug");

			// The solution has a console app that references an unsupported library. The build of the solution should fail.
			Assert.AreEqual (1, res.ErrorCount);

			var app = sol.GetAllItems<DotNetProject> ().FirstOrDefault (it => it.FileName.FileName == "console-with-libs.csproj");

			// The console app references an unsupported library. The build of the project should fail.
			res = await app.Build (Util.GetMonitor (), ConfigurationSelector.Default, true);
			Assert.IsTrue (res.ErrorCount == 1);

			// A solution build should succeed if it has unbuildable projects but those projects are not referenced by buildable projects
			app.References.Clear ();
			await sol.SaveAsync (Util.GetMonitor ());
			res = await sol.Build (Util.GetMonitor (), "Debug");
			Assert.IsTrue (res.ErrorCount == 0);

			// Regular project not referencing anything else. Should build.
			res = await app.Build (Util.GetMonitor (), ConfigurationSelector.Default, true);
			Assert.IsTrue (res.ErrorCount == 0);
		}

		[Test]
		public async Task UnloadProject ()
		{
			string solFile = Util.GetSampleProject ("console-with-libs", "console-with-libs.sln");

			Solution sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			SolutionItem p = sol.FindProjectByName ("console-with-libs");
			SolutionItem lib1 = sol.FindProjectByName ("library1");
			SolutionItem lib2 = sol.FindProjectByName ("library2");

			Assert.IsTrue (p.Enabled);
			Assert.IsTrue (lib1.Enabled);
			Assert.IsTrue (lib2.Enabled);
			Assert.IsTrue (sol.Configurations [0].BuildEnabledForItem (p));

			p.Enabled = false;
			await p.ParentFolder.ReloadItem (Util.GetMonitor (), p);

			p = sol.GetAllItems<SolutionItem> ().FirstOrDefault (it => it.Name == "console-with-libs");
			Assert.IsNotNull (p);
			Assert.IsFalse (p.Enabled);
			Assert.IsTrue (lib1.Enabled);
			Assert.IsTrue (lib2.Enabled);

			p.Enabled = true;
			await p.ParentFolder.ReloadItem (Util.GetMonitor (), p);

			p = sol.FindProjectByName ("console-with-libs");
			Assert.IsNotNull (p);
			Assert.IsTrue (sol.Configurations [0].BuildEnabledForItem (p));

			// Reload the solution

			Assert.IsTrue (sol.Configurations [0].BuildEnabledForItem (lib1));
			lib1.Enabled = false;
			await sol.SaveAsync (Util.GetMonitor ());
			sol.Dispose ();

			sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			lib1 = sol.GetAllItems<SolutionItem> ().FirstOrDefault (it => it.Name == "library1");
			Assert.IsNotNull (lib1);
			lib1.Enabled = true;
			await lib1.ParentFolder.ReloadItem (Util.GetMonitor (), lib1);

			lib1 = sol.FindProjectByName ("library1");
			Assert.IsNotNull (lib1);
			Assert.IsTrue (sol.Configurations [0].BuildEnabledForItem (lib1));
		}

		[Test]
		public void SolutionBoundUnbound ()
		{
			Solution sol = new Solution ();

			var e = new SomeItem ();
			Assert.AreEqual (0, e.BoundEvents);
			Assert.AreEqual (0, e.UnboundEvents);

			sol.RootFolder.AddItem (e);
			Assert.AreEqual (1, e.BoundEvents);
			Assert.AreEqual (0, e.UnboundEvents);
			Assert.AreEqual (1, e.InternalItem.BoundEvents);
			Assert.AreEqual (0, e.InternalItem.UnboundEvents);

			e.Reset ();
			sol.RootFolder.Items.Remove (e);
			Assert.AreEqual (0, e.BoundEvents);
			Assert.AreEqual (1, e.UnboundEvents);
			Assert.AreEqual (0, e.InternalItem.BoundEvents);
			Assert.AreEqual (1, e.InternalItem.UnboundEvents);

			e.Reset ();
			sol.RootFolder.AddItem (e);
			Assert.AreEqual (1, e.BoundEvents);
			Assert.AreEqual (0, e.UnboundEvents);
			Assert.AreEqual (1, e.InternalItem.BoundEvents);
			Assert.AreEqual (0, e.InternalItem.UnboundEvents);

			e.Reset ();
			sol.RootFolder.Items.Remove (e);
			Assert.AreEqual (0, e.BoundEvents);
			Assert.AreEqual (1, e.UnboundEvents);
			Assert.AreEqual (0, e.InternalItem.BoundEvents);
			Assert.AreEqual (1, e.InternalItem.UnboundEvents);

			e.Reset ();
			var f = new SolutionFolder ();
			f.AddItem (e);
			Assert.AreEqual (0, e.BoundEvents);
			Assert.AreEqual (0, e.UnboundEvents);
			Assert.AreEqual (0, e.InternalItem.BoundEvents);
			Assert.AreEqual (0, e.InternalItem.UnboundEvents);

			sol.RootFolder.AddItem (f);
			Assert.AreEqual (1, e.BoundEvents);
			Assert.AreEqual (0, e.UnboundEvents);
			Assert.AreEqual (1, e.InternalItem.BoundEvents);
			Assert.AreEqual (0, e.InternalItem.UnboundEvents);

			e.Reset ();
			sol.RootFolder.Items.Remove (f);
			Assert.AreEqual (0, e.BoundEvents);
			Assert.AreEqual (1, e.UnboundEvents);
			Assert.AreEqual (0, e.InternalItem.BoundEvents);
			Assert.AreEqual (1, e.InternalItem.UnboundEvents);

			f.Dispose ();
			sol.Dispose ();
		}

		[Test]
		public async Task SolutionUnboundWhenUnloadingProject ()
		{
			var sol = new Solution ();

			var item = new SomeItem ();
			item.Name = "SomeItem";
			Assert.AreEqual (0, item.BoundEvents);
			Assert.AreEqual (0, item.UnboundEvents);

			sol.RootFolder.AddItem (item);
			Assert.AreEqual (1, item.BoundEvents);
			Assert.AreEqual (0, item.UnboundEvents);
			Assert.AreEqual (1, item.InternalItem.BoundEvents);
			Assert.AreEqual (0, item.InternalItem.UnboundEvents);

			Assert.IsTrue (item.Enabled);

			item.Reset ();

			item.Enabled = false;
			await item.ParentFolder.ReloadItem (Util.GetMonitor (), item);

			SolutionItem reloadedItem = sol.GetAllItems<SolutionItem> ().FirstOrDefault (it => it.Name == "SomeItem");
			Assert.IsNotNull (reloadedItem);
			Assert.IsFalse (reloadedItem.Enabled);
			Assert.IsInstanceOf<UnloadedSolutionItem> (reloadedItem);
			Assert.AreEqual (0, item.BoundEvents);
			Assert.AreEqual (1, item.UnboundEvents);
			Assert.AreEqual (0, item.InternalItem.BoundEvents);
			Assert.AreEqual (1, item.InternalItem.UnboundEvents);
		}

		[Test]
		public async Task SolutionBuildOrder ()
		{
			string solFile = Util.GetSampleProject ("solution-build-order", "ConsoleApplication3.sln");

			Solution sol = await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile) as Solution;
			var p = sol.FindProjectByName ("ConsoleApplication3");
			var lib1 = sol.FindProjectByName ("ClassLibrary1");
			var lib2 = sol.FindProjectByName ("ClassLibrary2");

			Assert.IsTrue (p.ItemDependencies.Contains (lib1));
			Assert.IsTrue (p.ItemDependencies.Contains (lib2));
			Assert.AreEqual (2, p.ItemDependencies.Count);

			Assert.IsTrue (lib2.ItemDependencies.Contains (lib1));
			Assert.AreEqual (1, lib2.ItemDependencies.Count);
			Assert.AreEqual (0, lib1.ItemDependencies.Count);

			// Check that dependencies are saved

			var solContent1 = File.ReadAllLines (solFile);

			await sol.SaveAsync (new ProgressMonitor ());

			var solContent2 = File.ReadAllLines (solFile);
			Assert.AreEqual (solContent1, solContent2);

			// Check that when an item is removed, it is removed from the dependencies list

			lib1.ParentFolder.Items.Remove (lib1);
			lib1.Dispose ();

			Assert.IsTrue (p.ItemDependencies.Contains (lib2));
			Assert.AreEqual (1, p.ItemDependencies.Count);
			Assert.AreEqual (0, lib2.ItemDependencies.Count);

			// Check that when an item is reloaded, it is kept from the dependencies list

			var lib2Reloaded = await lib2.ParentFolder.ReloadItem (Util.GetMonitor (), lib2);

			Assert.AreNotEqual (lib2, lib2Reloaded);
			Assert.IsTrue (p.ItemDependencies.Contains (lib2Reloaded));
			Assert.AreEqual (1, p.ItemDependencies.Count);
		}

		[Test]
		public async Task WriteCustomData ()
		{
			var en = new CustomSolutionItemNode<TestSolutionExtension> ();
			WorkspaceObject.RegisterCustomExtension (en);
			try {
				string solFile = Util.GetSampleProject ("solution-custom-data", "custom-data.sln");

				var sol = new Solution ();
				var ext = sol.GetService<TestSolutionExtension> ();
				Assert.NotNull (ext);
				ext.Prop1 = "one";
				ext.Prop2 = "two";
				ext.Extra = new ComplexSolutionData {
					Prop3 = "three",
					Prop4 = "four"
				};
				var savedFile = solFile + ".saved.sln";
				await sol.SaveAsync (savedFile, Util.GetMonitor ());
				Assert.AreEqual (File.ReadAllText (solFile), File.ReadAllText (savedFile));
			} finally {
				WorkspaceObject.UnregisterCustomExtension (en);
			}
		}

		[Test]
		public async Task ReadCustomData ()
		{
			var en = new CustomSolutionItemNode<TestSolutionExtension> ();
			WorkspaceObject.RegisterCustomExtension (en);
			try {
				string solFile = Util.GetSampleProject ("solution-custom-data", "custom-data.sln");
				var sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

				var ext = sol.GetService<TestSolutionExtension> ();
				Assert.NotNull (ext);
				Assert.AreEqual ("one", ext.Prop1);
				Assert.AreEqual ("two", ext.Prop2);
				Assert.NotNull (ext.Extra);
				Assert.AreEqual ("three", ext.Extra.Prop3);
				Assert.AreEqual ("four", ext.Extra.Prop4);
			} finally {
				WorkspaceObject.UnregisterCustomExtension (en);
			}
		}

		[Test]
		public async Task KeepUnknownCustomData ()
		{
			var en = new CustomSolutionItemNode<TestSolutionExtension> ();
			WorkspaceObject.RegisterCustomExtension (en);
			try {
				FilePath solFile = Util.GetSampleProject ("solution-custom-data", "custom-data-keep-unknown.sln");
				var sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

				var ext = sol.GetService<TestSolutionExtension> ();
				ext.Prop1 = "one-mod";
				ext.Prop2 = "";
				ext.Extra.Prop3 = "three-mod";
				ext.Extra.Prop4 = "";

				var refFile = solFile.ParentDirectory.Combine ("custom-data-keep-unknown.sln.saved");

				await sol.SaveAsync (Util.GetMonitor ());

				Assert.AreEqual (File.ReadAllText (refFile), File.ReadAllText (sol.FileName));

			} finally {
				WorkspaceObject.UnregisterCustomExtension (en);
			}
		}

		[Test]
		public async Task RemoveCustomData ()
		{
			var en = new CustomSolutionItemNode<TestSolutionExtension> ();
			WorkspaceObject.RegisterCustomExtension (en);
			try {
				FilePath solFile = Util.GetSampleProject ("solution-custom-data", "custom-data.sln");
				var sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

				var ext = sol.GetService<TestSolutionExtension> ();
				ext.Prop1 = "xx";
				ext.Prop2 = "";
				ext.Extra = null;

				var refFile = solFile.ParentDirectory.Combine ("no-custom-data.sln");

				await sol.SaveAsync (Util.GetMonitor ());

				Assert.AreEqual (File.ReadAllText (refFile), File.ReadAllText (sol.FileName));

			} finally {
				WorkspaceObject.UnregisterCustomExtension (en);
			}
		}
	}

	class SomeItem: SolutionItem
	{
		public int BoundEvents;
		public int UnboundEvents;

		public SomeItem InternalItem;

		public SomeItem (bool createInternal = true)
		{
			Initialize (this);
			if (createInternal) {
				InternalItem = new SomeItem (false);
				RegisterInternalChild (InternalItem);
			}
		}

		public void Reset ()
		{
			BoundEvents = UnboundEvents = 0;
			if (InternalItem != null)
				InternalItem.Reset ();
		}

		protected override void OnBoundToSolution ()
		{
			base.OnBoundToSolution ();
			BoundEvents++;
		}

		protected override void OnUnboundFromSolution ()
		{
			base.OnUnboundFromSolution ();
			UnboundEvents++;
		}
	}

	class CustomSolutionItemNode<T>: ProjectModelExtensionNode where T:new()
	{
		public override object CreateInstance ()
		{
			return new T ();
		}
	}

	[SolutionDataSection ("TestData")]
	class TestSolutionExtension: SolutionExtension
	{
		[ItemProperty ("prop1", DefaultValue = "xx")]
		public string Prop1 { get; set; }

		[ItemProperty ("prop2", DefaultValue = "")]
		public string Prop2 { get; set; }

		[ItemProperty ("extra")]
		public ComplexSolutionData Extra { get; set; }
	}

	class ComplexSolutionData
	{
		[ItemProperty ("prop3")]
		public string Prop3 { get; set; }

		[ItemProperty ("prop4", DefaultValue = "")]
		public string Prop4 { get; set; }
	}
}
