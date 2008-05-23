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
			
			DotNetProject project = new DotNetProject ("C#");
			project.Name = "project1";
			sol.RootFolder.Items.Add (project);
			
			Assert.AreEqual (2, countSolutionItemAdded);
			Assert.AreEqual (1, sol.Items.Count);
			
			DotNetProject project2 = new DotNetProject ("C#");
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
			
			ProjectReference pr1 = new ProjectReference (ReferenceType.Gac, "SomeTest");
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
			
			sol.Name = "test1";
			Assert.AreEqual ("test1", sol.Name);
			Assert.AreEqual ("test1.sln", sol.FileName);
			Assert.AreEqual (1, nameChanges);
			
			sol.Name = "test2";
			Assert.AreEqual ("test2", sol.Name);
			Assert.AreEqual ("test2.sln", sol.FileName);
			Assert.AreEqual (2, nameChanges);
			
			sol.FileName = "/tmp/test3.sln";
			Assert.AreEqual ("test3", sol.Name);
			Assert.AreEqual ("/tmp/test3.sln", sol.FileName);
			Assert.AreEqual (3, nameChanges);
			
			sol.Name = "test4";
			Assert.AreEqual ("test4", sol.Name);
			Assert.AreEqual ("/tmp/test4.sln", sol.FileName);
			Assert.AreEqual (4, nameChanges);
			
			sol.FileFormat = Services.ProjectService.FileFormats.GetFileFormat ("MD1");
			Assert.AreEqual ("test4", sol.Name);
			Assert.AreEqual ("/tmp/test4.mds", sol.FileName);
			Assert.AreEqual (4, nameChanges);
		}
		
		[Test()]
		public void ProjectName ()
		{
			int nameChanges = 0;
			
			DotNetProject prj = new DotNetProject ("C#");
			prj.NameChanged += delegate {
				nameChanges++;
			};
			
			prj.Name = "test1";
			Assert.AreEqual ("test1", prj.Name);
			Assert.AreEqual ("test1.csproj", prj.FileName);
			Assert.AreEqual (1, nameChanges);
			
			prj.Name = "test2";
			Assert.AreEqual ("test2", prj.Name);
			Assert.AreEqual ("test2.csproj", prj.FileName);
			Assert.AreEqual (2, nameChanges);
			
			prj.FileName = "/tmp/test3.csproj";
			Assert.AreEqual ("test3", prj.Name);
			Assert.AreEqual ("/tmp/test3.csproj", prj.FileName);
			Assert.AreEqual (3, nameChanges);
			
			prj.Name = "test4";
			Assert.AreEqual ("test4", prj.Name);
			Assert.AreEqual ("/tmp/test4.csproj", prj.FileName);
			Assert.AreEqual (4, nameChanges);
			
			prj.FileFormat = Services.ProjectService.FileFormats.GetFileFormat ("MD1");
			Assert.AreEqual ("test4", prj.Name);
			Assert.AreEqual ("/tmp/test4.mdp", prj.FileName);
			Assert.AreEqual (4, nameChanges);
			Assert.AreEqual ("MD1", prj.FileFormat.Id);
			
			// Projects inherit the file format from the parent solution
			Solution sol = new Solution ();
			sol.RootFolder.Items.Add (prj);
			Assert.AreEqual ("test4", prj.Name);
			Assert.AreEqual ("/tmp/test4.csproj", prj.FileName);
			Assert.AreEqual (4, nameChanges);
			Assert.AreEqual ("MSBuild05", prj.FileFormat.Id);

			// Removing the project from the solution should not restore the old format
			sol.RootFolder.Items.Remove (prj);
			Assert.AreEqual ("MSBuild05", prj.FileFormat.Id);
			Assert.AreEqual ("test4", prj.Name);
			Assert.AreEqual ("/tmp/test4.csproj", prj.FileName);
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
			sol.FileFormat = Services.ProjectService.FileFormats.GetFileFormat ("MD1");
			Assert.IsFalse (sol.NeedsReload);
			Assert.IsFalse (p.NeedsReload);
			sol.FileFormat = Services.ProjectService.FileFormats.GetFileFormat ("MSBuild05");
			Assert.IsFalse (sol.NeedsReload);
			Assert.IsFalse (p.NeedsReload);
			
			sol.RootFolder.Items.Remove (p);
			Assert.IsFalse (p.NeedsReload);
			p.FileFormat = Services.ProjectService.FileFormats.GetFileFormat ("MD1");
			Assert.IsFalse (p.NeedsReload);
			sol.RootFolder.Items.Add (p);
			Assert.IsFalse (p.NeedsReload);
			sol.RootFolder.Items.Remove (p);
			Assert.IsFalse (p.NeedsReload);
			p.FileFormat = Services.ProjectService.FileFormats.GetFileFormat ("MSBuild05");
			Assert.IsFalse (p.NeedsReload);
			sol.RootFolder.Items.Add (p);
			Assert.IsFalse (p.NeedsReload);
			
			string mdsSolFile = Util.GetSampleProject ("csharp-console-mdp", "csharp-console-mdp.mds");
			Solution mdsSol = (Solution) Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), mdsSolFile);
			Project pmds = mdsSol.Items [0] as Project;
			Assert.IsFalse (mdsSol.NeedsReload);
			Assert.IsFalse (pmds.NeedsReload);
			
			// Check reloading flag in another solution
			
			string solFile = sol.FileName;
			Solution sol2 = (Solution) Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			Assert.IsFalse (sol2.NeedsReload);
			
			Project p2 = sol2.Items [0] as Project;
			Assert.IsFalse (p2.NeedsReload);
			
			System.Threading.Thread.Sleep (1000);
			sol.Save (Util.GetMonitor ());
			
			Assert.IsTrue (sol2.NeedsReload);
		}
		
		[Test()]
		public void ReloadingReferencedProject ()
		{
			string solFile = Util.GetSampleProject ("console-with-libs-mdp", "console-with-libs-mdp.mds");
			
			Solution sol = (Solution) Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			DotNetProject p = (DotNetProject) sol.FindProjectByName ("console-with-libs-mdp");
			DotNetProject lib2 = (DotNetProject) sol.FindProjectByName ("library2");
			
			Assert.AreEqual (3, p.References.Count);
			
			lib2.ParentFolder.ReloadItem (Util.GetMonitor (), lib2);
			
			Assert.AreEqual (3, p.References.Count);
		}
		
		[Test()]
		public void GetItemFiles ()
		{
			Solution sol = TestProjectsChecks.CreateConsoleSolution ("item-files");
			
			List<string> files = sol.GetItemFiles (false);
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
			string solFile = Util.GetSampleProject ("console-with-libs-mdp", "console-with-libs-mdp.mds");
			
			Solution sol = (Solution) Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			DotNetProject p = (DotNetProject) sol.FindProjectByName ("console-with-libs-mdp");
			Assert.IsNotNull (p);
			DotNetProject lib1 = (DotNetProject) sol.FindProjectByName ("library1");
			Assert.IsNotNull (lib1);
			DotNetProject lib2 = (DotNetProject) sol.FindProjectByName ("library2");
			Assert.IsNotNull (lib2);
			
			Assert.IsTrue (p.NeedsBuilding ("Debug"));
			Assert.IsTrue (lib1.NeedsBuilding ("Debug"));
			Assert.IsTrue (lib2.NeedsBuilding ("Debug"));
			
			// Build the project and the references
			
			BuildResult res = p.Build (Util.GetMonitor (), "Debug", true);
			Assert.AreEqual (0, res.ErrorCount);
			Assert.AreEqual (0, res.WarningCount);
			Assert.AreEqual (3, res.BuildCount);
			Assert.IsFalse (p.NeedsBuilding ("Debug"));
			Assert.IsFalse (lib1.NeedsBuilding ("Debug"));
			Assert.IsFalse (lib2.NeedsBuilding ("Debug"));
			
			Assert.IsTrue (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", "console-with-libs-mdp.exe")));
			Assert.IsTrue (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", "console-with-libs-mdp.exe.mdb")));
			Assert.IsTrue (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", "library1.dll")));
			Assert.IsTrue (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", "library1.dll.mdb")));
			Assert.IsTrue (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", "library2.dll")));
			Assert.IsTrue (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", "library2.dll.mdb")));
			
			// Build the project, but not the references
			
			p.SetNeedsBuilding (true);
			lib1.SetNeedsBuilding (true);
			lib2.SetNeedsBuilding (true);
			Assert.IsTrue (p.NeedsBuilding ("Debug"));
			Assert.IsTrue (lib1.NeedsBuilding ("Debug"));
			Assert.IsTrue (lib2.NeedsBuilding ("Debug"));
			
			res = p.Build (Util.GetMonitor (), "Debug", false);
			Assert.AreEqual (0, res.ErrorCount);
			Assert.AreEqual (0, res.WarningCount);
			Assert.AreEqual (1, res.BuildCount);
			Assert.IsTrue (p.NeedsBuilding ("Debug"));   // True because references require building
			Assert.IsTrue (lib1.NeedsBuilding ("Debug"));
			Assert.IsTrue (lib2.NeedsBuilding ("Debug"));
		}
		
		[Test()]
		public void BuildingAndCleaning ()
		{
			string solFile = Util.GetSampleProject ("console-with-libs-mdp", "console-with-libs-mdp.mds");
			
			Solution sol = (Solution) Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			DotNetProject p = (DotNetProject) sol.FindProjectByName ("console-with-libs-mdp");
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
			
			Assert.IsTrue (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", "console-with-libs-mdp.exe")));
			Assert.IsTrue (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", "console-with-libs-mdp.exe.mdb")));
			Assert.IsTrue (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", "library1.dll")));
			Assert.IsTrue (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", "library1.dll.mdb")));
			Assert.IsTrue (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", "library2.dll")));
			Assert.IsTrue (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", "library2.dll.mdb")));
			
			// Clean the workspace
			
			ws.Clean (Util.GetMonitor (), "Debug");
			Assert.IsFalse (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", "console-with-libs-mdp.exe")));
			Assert.IsFalse (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", "console-with-libs-mdp.exe.mdb")));
			Assert.IsFalse (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", "library1.dll")));
			Assert.IsFalse (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", "library1.dll.mdb")));
			Assert.IsFalse (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", "library2.dll")));
			Assert.IsFalse (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", "library2.dll.mdb")));
			
			// Build the solution
			
			res = ws.Build (Util.GetMonitor (), "Debug");
			Assert.AreEqual (0, res.ErrorCount);
			Assert.AreEqual (0, res.WarningCount);
			Assert.AreEqual (3, res.BuildCount);
			
			Assert.IsTrue (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", "console-with-libs-mdp.exe")));
			Assert.IsTrue (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", "console-with-libs-mdp.exe.mdb")));
			Assert.IsTrue (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", "library1.dll")));
			Assert.IsTrue (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", "library1.dll.mdb")));
			Assert.IsTrue (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", "library2.dll")));
			Assert.IsTrue (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", "library2.dll.mdb")));
			
			// Clean the solution
			
			sol.Clean (Util.GetMonitor (), "Debug");
			Assert.IsFalse (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", "console-with-libs-mdp.exe")));
			Assert.IsFalse (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", "console-with-libs-mdp.exe.mdb")));
			Assert.IsFalse (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", "library1.dll")));
			Assert.IsFalse (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", "library1.dll.mdb")));
			Assert.IsFalse (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", "library2.dll")));
			Assert.IsFalse (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", "library2.dll.mdb")));
			
			// Build the solution folder
			
			res = folder.Build (Util.GetMonitor (), "Debug");
			Assert.AreEqual (0, res.ErrorCount);
			Assert.AreEqual (0, res.WarningCount);
			Assert.AreEqual (1, res.BuildCount);
			
			Assert.IsFalse (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", "console-with-libs-mdp.exe")));
			Assert.IsFalse (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", "console-with-libs-mdp.exe.mdb")));
			Assert.IsFalse (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", "library1.dll")));
			Assert.IsFalse (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", "library1.dll.mdb")));
			Assert.IsTrue (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", "library2.dll")));
			Assert.IsTrue (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", "library2.dll.mdb")));
			
			// Clean the solution folder
			
			folder.Clean (Util.GetMonitor (), "Debug");
			Assert.IsFalse (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", "library2.dll")));
			Assert.IsFalse (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", "library2.dll.mdb")));
		}
		
		[Test()]
		public void FormatConversions ()
		{
			Solution sol = TestProjectsChecks.CreateConsoleSolution ("reloading");
			Project p = (Project) sol.Items [0];
			
			Assert.AreEqual ("MSBuild05", sol.FileFormat.Id);
			Assert.AreEqual ("MSBuild05", p.FileFormat.Id);
			
			// Change solution format of unsaved solution
			
			sol.FileFormat = Util.FileFormatMD1;
			
			Assert.AreEqual ("MD1", sol.FileFormat.Id);
			Assert.AreEqual ("MD1", p.FileFormat.Id);
			
			sol.FileFormat = Util.FileFormatMSBuild05;
			
			Assert.AreEqual ("MSBuild05", sol.FileFormat.Id);
			Assert.AreEqual ("MSBuild05", p.FileFormat.Id);
			
			// Change solution format of saved solution
			
			sol.Save (Util.GetMonitor ());

			sol.FileFormat = Util.FileFormatMD1;
			
			Assert.AreEqual ("MD1", sol.FileFormat.Id);
			Assert.AreEqual ("MD1", p.FileFormat.Id);
			
			// Add new project
			
			Project newp = new DotNetProject ("C#");
			Assert.AreEqual ("MSBuild05", newp.FileFormat.Id);
			
			sol.RootFolder.Items.Add (newp);
			Assert.AreEqual ("MD1", newp.FileFormat.Id);
			
			// Add saved project
			
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			Solution msol = (Solution) Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			
			Project mp = (Project) msol.Items [0];
			Assert.AreEqual ("MSBuild05", mp.FileFormat.Id);
			
			sol.RootFolder.Items.Add (newp);
			Assert.AreEqual ("MSBuild05", mp.FileFormat.Id);
		}
	}
}
