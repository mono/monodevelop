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
using System.Collections.Generic;
using NUnit.Framework;
using UnitTests;
using MonoDevelop.Core;
using MonoDevelop.Projects.Formats.MSBuild;

namespace MonoDevelop.Projects
{
	[TestFixture()]
	public class SolutionTests: TestBase
	{
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
			
			DotNetAssemblyProject project = new DotNetAssemblyProject ("C#");
			project.Name = "project1";
			sol.RootFolder.Items.Add (project);
			
			Assert.AreEqual (2, countSolutionItemAdded);
			Assert.AreEqual (1, sol.Items.Count);
			
			DotNetAssemblyProject project2 = new DotNetAssemblyProject ("C#");
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
			
			ProjectReference pr1 = new ProjectReference (ReferenceType.Package, "SomeTest");
			project.References.Add (pr1);
			Assert.AreEqual (1, countReferenceAddedToProject);
			
			ProjectReference pr2 = new ProjectReference (project);
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
			
			sol.ConvertToFormat (Util.FileFormatMSBuild10, true);
			Assert.AreEqual ("test4", sol.Name);
			Assert.AreEqual (Path.Combine (tmp, "test4.sln"), (string) sol.FileName);
			Assert.AreEqual (4, nameChanges);
		}
		
		[Test()]
		public void ProjectName ()
		{
			int nameChanges = 0;
			
			DotNetAssemblyProject prj = new DotNetAssemblyProject ("C#");
			prj.FileFormat = Util.FileFormatMSBuild05;
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
			
			prj.FileFormat = Util.FileFormatMSBuild12;
			Assert.AreEqual ("test4", prj.Name);
			Assert.AreEqual (Path.Combine (Path.GetTempPath (), "test4.csproj"), (string) prj.FileName);
			Assert.AreEqual (4, nameChanges);
			Assert.AreEqual ("MSBuild12", prj.FileFormat.Id);
			
			// Projects inherit the file format from the parent solution
			Solution sol = new Solution ();
			sol.ConvertToFormat (Util.FileFormatMSBuild05, true);
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
		public void Reloading ()
		{
			Solution sol = TestProjectsChecks.CreateConsoleSolution ("reloading");
			sol.Save (Util.GetMonitor ());
			Assert.IsFalse (sol.NeedsReload);
			
			Project p = sol.Items [0] as Project;
			Assert.IsFalse (p.NeedsReload);
			
			// Changing format must reset the reload flag (it's like we just created a new solution in memory)
			sol.ConvertToFormat (Util.FileFormatMSBuild10, true);
			Assert.IsFalse (sol.NeedsReload);
			Assert.IsFalse (p.NeedsReload);
			sol.ConvertToFormat (Util.FileFormatMSBuild12, true);
			Assert.IsFalse (sol.NeedsReload);
			Assert.IsFalse (p.NeedsReload);
			
			sol.RootFolder.Items.Remove (p);
			Assert.IsFalse (p.NeedsReload);
			p.FileFormat = Util.FileFormatMSBuild12;
			Assert.IsFalse (p.NeedsReload);
			sol.RootFolder.Items.Add (p);
			Assert.IsFalse (p.NeedsReload);
			sol.RootFolder.Items.Remove (p);
			Assert.IsFalse (p.NeedsReload);
			p.FileFormat = Util.FileFormatMSBuild05;
			Assert.IsFalse (p.NeedsReload);
			sol.RootFolder.Items.Add (p);
			Assert.IsFalse (p.NeedsReload);

			string solFile2 = Util.GetSampleProject ("csharp-console", "csharp-console.sln");
			Solution sol2 = (Solution) Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile2);
			Project p2 = sol2.Items [0] as Project;
			Assert.IsFalse (sol2.NeedsReload);
			Assert.IsFalse (p2.NeedsReload);
			
			// Check reloading flag in another solution
			
			string solFile = sol.FileName;
			Solution sol3 = (Solution) Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			Assert.IsFalse (sol3.NeedsReload);
			
			Project p3 = sol3.Items [0] as Project;
			Assert.IsFalse (p3.NeedsReload);
			
			System.Threading.Thread.Sleep (1000);
			sol.Save (Util.GetMonitor ());
			
			Assert.IsTrue (sol3.NeedsReload);
		}
		
		[Test()]
		public void ReloadingReferencedProject ()
		{
			string solFile = Util.GetSampleProject ("console-with-libs", "console-with-libs.sln");
			
			Solution sol = (Solution) Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			DotNetProject p = (DotNetProject) sol.FindProjectByName ("console-with-libs");
			DotNetProject lib2 = (DotNetProject) sol.FindProjectByName ("library2");
			
			Assert.AreEqual (3, p.References.Count);
			
			lib2.ParentFolder.ReloadItem (Util.GetMonitor (), lib2);
			
			Assert.AreEqual (3, p.References.Count);
		}
		
		[Test()]
		public void ReloadingKeepsBuildConfigurationAndStartupProject ()
		{
			string solFile = Util.GetSampleProject ("console-with-libs", "console-with-libs.sln");

			Solution sol = (Solution) Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			DotNetProject p = (DotNetProject) sol.FindProjectByName ("console-with-libs");
			DotNetProject lib2 = (DotNetProject) sol.FindProjectByName ("library2");

			Assert.AreSame (sol.StartupItem, p);

			var be = sol.Configurations ["Debug"].GetEntryForItem (lib2);
			be.Build = false;
			be.ItemConfiguration = "FooConfig";
			sol.Save (Util.GetMonitor ());

			// Test that build configuration info is not lost when reloading a project

			lib2 = (DotNetProject) lib2.ParentFolder.ReloadItem (Util.GetMonitor (), lib2);

			be = sol.Configurations ["Debug"].GetEntryForItem (lib2);
			Assert.IsFalse (be.Build);
			Assert.AreEqual ("FooConfig", be.ItemConfiguration);

			// Test that startup project is the reloaded project

			p = (DotNetProject) p.ParentFolder.ReloadItem (Util.GetMonitor (), p);
			Assert.AreSame (sol.StartupItem, p);
		}

		[Test()]
		public void GetItemFiles ()
		{
			Solution sol = TestProjectsChecks.CreateConsoleSolution ("item-files");
			
			List<FilePath> files = sol.GetItemFiles (false);
			Assert.AreEqual (1, files.Count);
			Assert.AreEqual (sol.FileName, files [0]);
			
			DotNetProject p = (DotNetProject) sol.Items [0];
			files = p.GetItemFiles (false);
			Assert.AreEqual (1, files.Count);
			Assert.AreEqual (p.FileName, files [0]);
			
			files = p.GetItemFiles (true);
			Assert.AreEqual (6, files.Count);
			Assert.IsTrue (files.Contains (p.FileName));
			foreach (ProjectFile pf in p.Files)
				Assert.IsTrue (files.Contains (pf.FilePath), "Contains " + pf.FilePath);
			
			files = sol.GetItemFiles (true);
			Assert.AreEqual (7, files.Count);
			Assert.IsTrue (files.Contains (sol.FileName));
			Assert.IsTrue (files.Contains (p.FileName));
			foreach (ProjectFile pf in p.Files)
				Assert.IsTrue (files.Contains (pf.FilePath), "Contains " + pf.FilePath);
		}
		
		[Test()]
		public void NeedsBuilding ()
		{
			string solFile = Util.GetSampleProject ("console-with-libs", "console-with-libs.sln");
			
			Solution sol = (Solution) Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			DotNetProject p = (DotNetProject) sol.FindProjectByName ("console-with-libs");
			Assert.IsNotNull (p);
			DotNetProject lib1 = (DotNetProject) sol.FindProjectByName ("library1");
			Assert.IsNotNull (lib1);
			DotNetProject lib2 = (DotNetProject) sol.FindProjectByName ("library2");
			Assert.IsNotNull (lib2);
			
			SolutionConfigurationSelector config = (SolutionConfigurationSelector) "Debug";
			
			Assert.IsTrue (p.NeedsBuilding (config));
			Assert.IsTrue (lib1.NeedsBuilding (config));
			Assert.IsTrue (lib2.NeedsBuilding (config));
			
			// Build the project and the references
			
			BuildResult res = p.Build (Util.GetMonitor (), config, true);
			foreach (BuildError er in res.Errors)
				Console.WriteLine (er);
			Assert.AreEqual (0, res.ErrorCount);
			Assert.AreEqual (0, res.WarningCount);
			Assert.AreEqual (3, res.BuildCount);

			Assert.IsTrue (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", "console-with-libs.exe")));
			Assert.IsTrue (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", GetMdb ("console-with-libs.exe"))));
			Assert.IsTrue (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", "library1.dll")));
			Assert.IsTrue (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", GetMdb ("library1.dll"))));
			Assert.IsTrue (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", "library2.dll")));
			Assert.IsTrue (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", GetMdb ("library2.dll"))));
			
			// Build the project, but not the references
			
			res = p.Build (Util.GetMonitor (), config, false);
			Assert.AreEqual (0, res.ErrorCount);
			Assert.AreEqual (0, res.WarningCount);
			Assert.AreEqual (1, res.BuildCount);
		}
		
		[Test()]
		public void BuildingAndCleaning ()
		{
			string solFile = Util.GetSampleProject ("console-with-libs", "console-with-libs.sln");
			
			Solution sol = (Solution) Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
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
			ws.Save (Util.GetMonitor ());
			
			// Build the project and the references
			
			BuildResult res = ws.Build (Util.GetMonitor (), "Debug");
			Assert.AreEqual (0, res.ErrorCount);
			Assert.AreEqual (0, res.WarningCount);
			Assert.AreEqual (3, res.BuildCount);
			
			Assert.IsTrue (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", "console-with-libs.exe")));
			Assert.IsTrue (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", GetMdb ("console-with-libs.exe"))));
			Assert.IsTrue (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", "library1.dll")));
			Assert.IsTrue (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", GetMdb ("library1.dll"))));
			Assert.IsTrue (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", "library2.dll")));
			Assert.IsTrue (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", GetMdb ("library2.dll"))));
			
			// Clean the workspace
			
			ws.Clean (Util.GetMonitor (), "Debug");
			Assert.IsFalse (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", "console-with-libs.exe")));
			Assert.IsFalse (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", GetMdb ("console-with-libs.exe"))));
			Assert.IsFalse (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", "library1.dll")));
			Assert.IsFalse (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", GetMdb ("library1.dll"))));
			Assert.IsFalse (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", "library2.dll")));
			Assert.IsFalse (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", GetMdb ("library2.dll"))));
			
			// Build the solution
			
			res = ws.Build (Util.GetMonitor (), "Debug");
			Assert.AreEqual (0, res.ErrorCount);
			Assert.AreEqual (0, res.WarningCount);
			Assert.AreEqual (3, res.BuildCount);
			
			Assert.IsTrue (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", "console-with-libs.exe")));
			Assert.IsTrue (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", GetMdb ("console-with-libs.exe"))));
			Assert.IsTrue (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", "library1.dll")));
			Assert.IsTrue (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", GetMdb ("library1.dll"))));
			Assert.IsTrue (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", "library2.dll")));
			Assert.IsTrue (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", GetMdb ("library2.dll"))));
			
			// Clean the solution
			
			sol.Clean (Util.GetMonitor (), "Debug");
			Assert.IsFalse (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", "console-with-libs.exe")));
			Assert.IsFalse (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", GetMdb ("console-with-libs.exe"))));
			Assert.IsFalse (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", "library1.dll")));
			Assert.IsFalse (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", GetMdb ("library1.dll"))));
			Assert.IsFalse (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", "library2.dll")));
			Assert.IsFalse (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", GetMdb ("library2.dll"))));
			
			// Build the solution folder
			
			res = folder.Build (Util.GetMonitor (), (SolutionConfigurationSelector) "Debug");
			Assert.AreEqual (0, res.ErrorCount);
			Assert.AreEqual (0, res.WarningCount);
			Assert.AreEqual (1, res.BuildCount);
			
			Assert.IsFalse (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", "console-with-libs.exe")));
			Assert.IsFalse (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", GetMdb ("console-with-libs.exe"))));
			Assert.IsFalse (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", "library1.dll")));
			Assert.IsFalse (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", GetMdb ("library1.dll"))));
			Assert.IsTrue (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", "library2.dll")));
			Assert.IsTrue (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", GetMdb ("library2.dll"))));
			
			// Clean the solution folder
			
			folder.Clean (Util.GetMonitor (), (SolutionConfigurationSelector) "Debug");
			Assert.IsFalse (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", "library2.dll")));
			Assert.IsFalse (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", GetMdb ("library2.dll"))));
		}
		
		[Test()]
		public void FormatConversions ()
		{
			Solution sol = TestProjectsChecks.CreateConsoleSolution ("reloading");
			Project p = (Project) sol.Items [0];
			
			Assert.AreEqual (Services.ProjectService.DefaultFileFormat.Id, sol.FileFormat.Id);
			Assert.AreEqual (Services.ProjectService.DefaultFileFormat.Id, p.FileFormat.Id);
			Assert.AreEqual ("4.0", MSBuildProjectService.GetHandler (p).ToolsVersion);
			
			// Change solution format of unsaved solution
			
			sol.ConvertToFormat (Util.FileFormatMSBuild08, true);
			
			Assert.AreEqual ("MSBuild08", sol.FileFormat.Id);
			Assert.AreEqual ("MSBuild08", p.FileFormat.Id);
			Assert.AreEqual ("3.5", MSBuildProjectService.GetHandler (p).ToolsVersion);

			sol.ConvertToFormat (Util.FileFormatMSBuild10, true);
			
			Assert.AreEqual ("MSBuild10", sol.FileFormat.Id);
			Assert.AreEqual ("MSBuild10", p.FileFormat.Id);
			Assert.AreEqual ("4.0", MSBuildProjectService.GetHandler (p).ToolsVersion);

			// Change solution format of saved solution
			
			sol.Save (Util.GetMonitor ());

			sol.ConvertToFormat (Util.FileFormatMSBuild05, false);
			
			Assert.AreEqual ("MSBuild05", sol.FileFormat.Id);
			Assert.AreEqual ("MSBuild05", p.FileFormat.Id);
			Assert.AreEqual ("2.0", MSBuildProjectService.GetHandler (p).ToolsVersion);

			// Add new project
			
			Project newp = new DotNetAssemblyProject ("C#");
			Assert.AreEqual ("MSBuild12", newp.FileFormat.Id);
			Assert.AreEqual ("4.0", MSBuildProjectService.GetHandler (newp).ToolsVersion);

			sol.RootFolder.Items.Add (newp);
			Assert.AreEqual ("MSBuild05", newp.FileFormat.Id);
			Assert.AreEqual ("2.0", MSBuildProjectService.GetHandler (newp).ToolsVersion);

			// Add saved project
			
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			Solution msol = (Solution) Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			
			Project mp = (Project) msol.Items [0];
			Assert.AreEqual ("MSBuild05", mp.FileFormat.Id);
			
			sol.RootFolder.Items.Add (newp);
			Assert.AreEqual ("MSBuild05", mp.FileFormat.Id);
		}
		
		[Test()]
		public void BuildConfigurationMappings ()
		{
			string solFile = Util.GetSampleProject ("test-build-configs", "test-build-configs.sln");
			
			Solution sol = (Solution) Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
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
			
			CheckSolutionBuildClean (sol, "Debug");
			CheckSolutionBuildClean (sol, "Release");
			
			// Check that building a project builds the correct referenced project configurations
			
			CheckProjectReferencesBuildClean (sol, "Debug");
			CheckProjectReferencesBuildClean (sol, "Release");
			
			// Single project build and clean
			
			CheckProjectBuildClean (lib2, "Debug");
			CheckProjectBuildClean (lib2, "Release");
			CheckProjectBuildClean (lib3, "Debug");
			CheckProjectBuildClean (lib3, "Release");
			CheckProjectBuildClean (lib4, "Debug");
			CheckProjectBuildClean (lib4, "Release");
		}
		
		void CheckSolutionBuildClean (Solution sol, string configuration)
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
			
			BuildResult res = sol.Build (Util.GetMonitor (), config);
			Assert.AreEqual (0, res.WarningCount, tag);
			Assert.AreEqual (0, res.ErrorCount, tag);
			
			Assert.IsTrue (File.Exists (lib1.GetOutputFileName (config)), tag);
			Assert.IsTrue (File.Exists (lib2.GetOutputFileName (config)), tag);
			Assert.IsTrue (File.Exists (lib3.GetOutputFileName (config)), tag);
			Assert.IsTrue (File.Exists (lib4.GetOutputFileName (config)), tag);
			
			sol.Clean (Util.GetMonitor (), config);
			
			Assert.IsFalse (File.Exists (lib1.GetOutputFileName (config)), tag);
			Assert.IsFalse (File.Exists (lib2.GetOutputFileName (config)), tag);
			Assert.IsFalse (File.Exists (lib3.GetOutputFileName (config)), tag);
			Assert.IsFalse (File.Exists (lib4.GetOutputFileName (config)), tag);
		}
		
		void CheckProjectReferencesBuildClean (Solution sol, string configuration)
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
			
			BuildResult res = lib1.Build (Util.GetMonitor (), config, true);
			Assert.AreEqual (0, res.WarningCount, tag);
			Assert.AreEqual (0, res.ErrorCount, tag + " " + res.CompilerOutput);
			
			Assert.IsTrue (File.Exists (lib1.GetOutputFileName (config)), tag);
			Assert.IsTrue (File.Exists (lib2.GetOutputFileName (config)), tag);
			Assert.IsTrue (File.Exists (lib3.GetOutputFileName (config)), tag);
			Assert.IsTrue (File.Exists (lib4.GetOutputFileName (config)), tag);
			
			sol.Clean (Util.GetMonitor (), config);
		}
		
		void CheckProjectBuildClean (DotNetProject lib, string configuration)
		{
			SolutionConfigurationSelector config = (SolutionConfigurationSelector) configuration;
			string tag = "CheckProjectBuildClean lib=" + lib.Name + " config=" + configuration;
			
			Assert.IsFalse (File.Exists (lib.GetOutputFileName (config)), tag);
			
			BuildResult res = lib.Build (Util.GetMonitor (), config, false);
			Assert.AreEqual (0, res.WarningCount, tag);
			Assert.AreEqual (0, res.ErrorCount, tag);
			
			Assert.IsTrue (File.Exists (lib.GetOutputFileName (config)), tag);
			
			lib.Clean (Util.GetMonitor (), config);
			Assert.IsFalse (File.Exists (lib.GetOutputFileName (config)), tag);
		}
		
		string GetConfigFolderName (DotNetProject lib, string conf)
		{
			return Path.GetFileName (Path.GetDirectoryName (lib.GetOutputFileName ((SolutionConfigurationSelector)conf)));
		}
	}
}
