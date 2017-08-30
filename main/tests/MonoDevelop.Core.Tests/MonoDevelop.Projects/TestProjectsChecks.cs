// TestProjectsChecks.cs
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

using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnitTests;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects.MSBuild;
using System.Threading.Tasks;

namespace MonoDevelop.Projects
{
	public class TestProjectsChecks
	{
		public static void CheckBasicMdConsoleProject (Solution sol)
		{
			// Check projects
			
			Assert.AreEqual (1, sol.Items.Count);
			Assert.IsTrue (sol.Items [0] is DotNetProject, "Project is DotNetProject");
			DotNetProject project = (DotNetProject) sol.Items [0];
			Assert.AreEqual ("csharp-console", project.Name);
			
			// Check files
			
			Assert.AreEqual (2, project.Files.Count, "File count");
			
			ProjectFile file = project.GetProjectFile (Path.Combine (project.BaseDirectory, "Program.cs"));
			Assert.AreEqual ("Program.cs", Path.GetFileName (file.Name));
			Assert.AreEqual (BuildAction.Compile, file.BuildAction);
			
			file = project.GetProjectFile (Path.Combine (project.BaseDirectory, "Properties", "AssemblyInfo.cs"));
			Assert.AreEqual ("AssemblyInfo.cs", Path.GetFileName (file.Name));
			Assert.AreEqual (BuildAction.Compile, file.BuildAction);
			
			// References
			
			Assert.AreEqual (1, project.References.Count);
			ProjectReference pr = project.References [0];
			Assert.AreEqual (ReferenceType.Package, pr.ReferenceType);
			Assert.AreEqual ("System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", pr.Reference);
			
			// Configurations
			
			Assert.AreEqual (2, sol.Configurations.Count, "Configuration count");
			
			SolutionConfiguration sc = sol.Configurations [0];
			Assert.AreEqual ("Debug", sc.Id);
			SolutionConfigurationEntry sce = sc.GetEntryForItem (project);
			Assert.IsTrue (sce.Build);
			Assert.AreEqual (project, sce.Item);
			Assert.AreEqual ("Debug", sce.ItemConfiguration);
			
			sc = sol.Configurations [1];
			Assert.AreEqual ("Release", sc.Id);
			sce = sc.GetEntryForItem (project);
			Assert.IsTrue (sce.Build);
			Assert.AreEqual (project, sce.Item);
			Assert.AreEqual ("Release2", sce.ItemConfiguration);
		}

		public static void CheckBasicVsConsoleProject (Solution sol)
		{
			// Check projects
			
			Assert.AreEqual (1, sol.Items.Count);
			DotNetProject project = (DotNetProject) sol.Items [0];
			
			// Check files
			
			Assert.AreEqual (2, project.Files.Count);
			
			ProjectFile file = project.GetProjectFile (Path.Combine (project.BaseDirectory, "Program.cs"));
			Assert.AreEqual ("Program.cs", Path.GetFileName (file.Name));
			Assert.AreEqual (BuildAction.Compile, file.BuildAction);
			
			file = project.GetProjectFile (Path.Combine (project.BaseDirectory, Path.Combine ("Properties", "AssemblyInfo.cs")));
			Assert.AreEqual ("AssemblyInfo.cs", Path.GetFileName (file.Name));
			Assert.AreEqual (BuildAction.Compile, file.BuildAction);
			
			// References
			
			Assert.AreEqual (3, project.References.Count);
			
			ProjectReference pr = project.References [0];
			Assert.AreEqual (ReferenceType.Package, pr.ReferenceType);
			Assert.AreEqual ("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", pr.Reference);
			
			pr = project.References [1];
			Assert.AreEqual (ReferenceType.Package, pr.ReferenceType);
			Assert.AreEqual ("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", pr.Reference);
			
			pr = project.References [2];
			Assert.AreEqual (ReferenceType.Package, pr.ReferenceType);
			Assert.AreEqual ("System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", pr.Reference);
			
			// Configurations
			
			Assert.AreEqual (2, sol.Configurations.Count, "Configuration count");
			
			SolutionConfiguration sc = sol.Configurations [0];
			Assert.AreEqual ("Debug", sc.Name);
			SolutionConfigurationEntry sce = sc.GetEntryForItem (project);
			Assert.IsTrue (sce.Build);
			Assert.AreEqual (project, sce.Item);
			Assert.AreEqual ("Debug", sce.ItemConfiguration);
			
			sc = sol.Configurations [1];
			Assert.AreEqual ("Release", sc.Name);
			sce = sc.GetEntryForItem (project);
			Assert.IsTrue (sce.Build);
			Assert.AreEqual (project, sce.Item);
			Assert.AreEqual ("Release", sce.ItemConfiguration);
		}
		
		public static Solution CreateConsoleSolution (string hint)
		{
			string dir = Util.CreateTmpDir (hint);
			
			Solution sol = new Solution ();
			SolutionConfiguration scDebug = sol.AddConfiguration ("Debug", true);
			
			DotNetProject project = Services.ProjectService.CreateDotNetProject ("C#");
			sol.RootFolder.Items.Add (project);
			Assert.AreEqual (0, project.Configurations.Count);
			
			InitializeProject (dir, project, "TestProject");
			project.References.Add (ProjectReference.CreateAssemblyReference ("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"));
			project.References.Add (ProjectReference.CreateAssemblyReference ("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"));
			project.References.Add (ProjectReference.CreateAssemblyReference ("System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"));
			project.Files.Add (new ProjectFile (Path.Combine (dir, "Program.cs")));
			project.Files.Add (new ProjectFile (Path.Combine (dir, "Resource.xml"), BuildAction.EmbeddedResource));
			project.Files.Add (new ProjectFile (Path.Combine (dir, "Excluded.xml"), BuildAction.Content));
			ProjectFile pf = new ProjectFile (Path.Combine (dir, "Copy.xml"), BuildAction.Content);
			pf.CopyToOutputDirectory = FileCopyMode.Always;
			project.Files.Add (pf);
			project.Files.Add (new ProjectFile (Path.Combine (dir, "Nothing.xml"), BuildAction.None));
			
			Assert.IsFalse (scDebug.GetEntryForItem (project).Build);
			scDebug.GetEntryForItem (project).Build = true;
			
			SolutionConfiguration scRelease = sol.AddConfiguration ("Release", true);
			
			string file = Path.Combine (dir, "TestSolution.sln");
			sol.FileName = file;
			
			Assert.AreEqual (2, sol.Configurations.Count);
			Assert.AreEqual (1, scDebug.Configurations.Count);
			Assert.AreEqual (1, scRelease.Configurations.Count);
			Assert.AreEqual (2, project.Configurations.Count);
			foreach (var v in project.Files) {
				if (v.FilePath.FileName == "Program.cs") {
				File.WriteAllText (v.FilePath,
				                   @"
using System;

namespace Foo {
	public class MainClass {
		public static void Main (string [] args)
		{
		}
	}
}");
				} else {
					File.WriteAllText (v.FilePath, v.Name);
				}
			}
			return sol;
		}
		
		public static void CheckConsoleProject (Solution sol)
		{
			Assert.AreEqual (1, sol.Items.Count);
			Assert.AreEqual (2, sol.Configurations.Count);
			Assert.AreEqual ("Debug", sol.Configurations [0].Id);
			Assert.AreEqual ("Release", sol.Configurations [1].Id);
			
			DotNetProject p = (DotNetProject) sol.Items [0];
			Assert.AreEqual (5, p.Files.Count);
			Assert.IsTrue (p.Files.GetFile (Path.Combine (p.BaseDirectory, "Program.cs")) != null);
			Assert.IsTrue (p.Files.GetFile (Path.Combine (p.BaseDirectory, "Resource.xml")) != null);
			Assert.IsTrue (p.Files.GetFile (Path.Combine (p.BaseDirectory, "Excluded.xml")) != null);
			Assert.IsTrue (p.Files.GetFile (Path.Combine (p.BaseDirectory, "Copy.xml")) != null);
			Assert.IsTrue (p.Files.GetFile (Path.Combine (p.BaseDirectory, "Nothing.xml")) != null);
			
			Assert.AreEqual (3, p.References.Count);
		}
		
		public static DotNetProject CreateProject (string dir, string lang, string name)
		{
			DotNetProject project = Services.ProjectService.CreateDotNetProject (lang);
			InitializeProject (dir, project, name);
			return project;
		}
		
		public static void InitializeProject (string dir, DotNetProject project, string name)
		{
			project.DefaultNamespace = name;
			
			DotNetProjectConfiguration pcDebug = project.AddNewConfiguration ("Debug") as DotNetProjectConfiguration;
			pcDebug.DebugType = "full";
			pcDebug.OutputDirectory = Path.Combine (dir, "bin/Debug");
			pcDebug.OutputAssembly = name;
			pcDebug.DebugSymbols = true;
			dynamic csparamsDebug = pcDebug.CompilationParameters;
			csparamsDebug.DefineSymbols = "DEBUG;TRACE";
			csparamsDebug.Optimize = false;
			
			DotNetProjectConfiguration pcRelease = project.AddNewConfiguration ("Release") as DotNetProjectConfiguration;
			pcRelease.DebugType = "none";
			pcRelease.OutputDirectory = Path.Combine (dir, "bin/Release");
			pcRelease.OutputAssembly = name;
			dynamic csparamsRelease = pcRelease.CompilationParameters;
			csparamsRelease.DefineSymbols = "TRACE";
			csparamsRelease.Optimize = true;
			
			string pfile = Path.Combine (dir, name);
			project.FileName = pfile;
		}
		
		public static async Task CheckGenericItemProject (MSBuildFileFormat format)
		{
			Solution sol = new Solution ();
			sol.ConvertToFormat (format);
			string dir = Util.CreateTmpDir ("generic-item-" + format.Name);
			sol.FileName = Path.Combine (dir, "TestGenericItem");
			sol.Name = "TheItem";

			MonoDevelop.Projects.MSBuild.MSBuildProjectService.RegisterGenericProjectType ("GenericItem", typeof(GenericItem));
			
			GenericItem it = new GenericItem ();
			it.SomeValue = "hi";
			
			sol.RootFolder.Items.Add (it);
			it.FileName = Path.Combine (dir, "TheItem");
			it.Name = "TheItem";
			
			await sol.SaveAsync (Util.GetMonitor ());
			
			Solution sol2 = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), sol.FileName);
			Assert.AreEqual (1, sol2.Items.Count);
			Assert.IsInstanceOf<GenericItem> (sol2.Items [0]);
			
			it = (GenericItem) sol2.Items [0];
			Assert.AreEqual ("hi", it.SomeValue);

			sol.Dispose ();
		}
		
		public static async Task TestLoadSaveSolutionFolders (MSBuildFileFormat fileFormat)
		{
			List<string> ids = new List<string> ();
			
			Solution sol = new Solution ();
			sol.ConvertToFormat (fileFormat);
			string dir = Util.CreateTmpDir ("solution-folders-" + fileFormat.Name);
			sol.FileName = Path.Combine (dir, "TestSolutionFolders");
			sol.Name = "TheSolution";
			
			var p1 = Services.ProjectService.CreateDotNetProject ("C#");
			p1.FileName = Path.Combine (dir, "p1");
			sol.RootFolder.Items.Add (p1);
			string idp1 = p1.ItemId;
			Assert.IsFalse (string.IsNullOrEmpty (idp1));
			Assert.IsFalse (ids.Contains (idp1));
			ids.Add (idp1);

			SolutionFolder f1 = new SolutionFolder ();
			f1.Name = "f1";
			sol.RootFolder.Items.Add (f1);
			string idf1 = f1.ItemId;
			Assert.IsFalse (string.IsNullOrEmpty (idf1));
			Assert.IsFalse (ids.Contains (idf1));
			ids.Add (idf1);
			
			var p2 = Services.ProjectService.CreateDotNetProject ("C#");
			p2.FileName = Path.Combine (dir, "p2");
			f1.Items.Add (p2);
			string idp2 = p2.ItemId;
			Assert.IsFalse (string.IsNullOrEmpty (idp2));
			Assert.IsFalse (ids.Contains (idp2));
			ids.Add (idp2);

			SolutionFolder f2 = new SolutionFolder ();
			f2.Name = "f2";
			f1.Items.Add (f2);
			string idf2 = f2.ItemId;
			Assert.IsFalse (string.IsNullOrEmpty (idf2));
			Assert.IsFalse (ids.Contains (idf2));
			ids.Add (idf2);
			
			var p3 = Services.ProjectService.CreateDotNetProject ("C#");
			p3.FileName = Path.Combine (dir, "p3");
			f2.Items.Add (p3);
			string idp3 = p3.ItemId;
			Assert.IsFalse (string.IsNullOrEmpty (idp3));
			Assert.IsFalse (ids.Contains (idp3));
			ids.Add (idp3);
			
			var p4 = Services.ProjectService.CreateDotNetProject ("C#");
			p4.FileName = Path.Combine (dir, "p4");
			f2.Items.Add (p4);
			string idp4 = p4.ItemId;
			Assert.IsFalse (string.IsNullOrEmpty (idp4));
			Assert.IsFalse (ids.Contains (idp4));
			ids.Add (idp4);
			
			await sol.SaveAsync (Util.GetMonitor ());
			
			Solution sol2 = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), sol.FileName);
			Assert.AreEqual (4, sol2.Items.Count);
			Assert.AreEqual (2, sol2.RootFolder.Items.Count);
			Assert.AreEqual ("MonoDevelop.CSharp.Project.CSharpProject", sol2.RootFolder.Items[0].GetType().FullName);
			Assert.AreEqual (typeof(SolutionFolder), sol2.RootFolder.Items [1].GetType ());
			Assert.AreEqual ("p1", sol2.RootFolder.Items [0].Name);
			Assert.AreEqual ("f1", sol2.RootFolder.Items [1].Name);
			Assert.AreEqual (idp1, sol2.RootFolder.Items [0].ItemId, "idp1");
			Assert.AreEqual (idf1, sol2.RootFolder.Items [1].ItemId, "idf1");
			
			f1 = (SolutionFolder) sol2.RootFolder.Items [1];
			Assert.AreEqual (2, f1.Items.Count);
			Assert.AreEqual ("MonoDevelop.CSharp.Project.CSharpProject", f1.Items [0].GetType ().FullName);
			Assert.AreEqual (typeof(SolutionFolder), f1.Items [1].GetType ());
			Assert.AreEqual ("p2", f1.Items [0].Name);
			Assert.AreEqual ("f2", f1.Items [1].Name);
			Assert.AreEqual (idp2, f1.Items [0].ItemId, "idp2");
			Assert.AreEqual (idf2, f1.Items [1].ItemId, "idf2");
			
			f2 = (SolutionFolder) f1.Items [1];
			Assert.AreEqual (2, f2.Items.Count);
			Assert.AreEqual ("MonoDevelop.CSharp.Project.CSharpProject", f2.Items [0].GetType ().FullName);
			Assert.AreEqual ("MonoDevelop.CSharp.Project.CSharpProject", f2.Items [1].GetType ().FullName);
			Assert.AreEqual ("p3", f2.Items [0].Name);
			Assert.AreEqual ("p4", f2.Items [1].Name);
			Assert.AreEqual (idp3, f2.Items [0].ItemId, "idp4");
			Assert.AreEqual (idp4, f2.Items [1].ItemId, "idp4");

			sol.Dispose ();
		}
		
		public static async Task TestCreateLoadSaveConsoleProject (MSBuildFileFormat fileFormat)
		{
			Solution sol = CreateConsoleSolution ("TestCreateLoadSaveConsoleProject");
			sol.ConvertToFormat (fileFormat);
			
			await sol.SaveAsync (Util.GetMonitor ());
			string solFile = sol.FileName;
			
			sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			CheckConsoleProject (sol);

			// Save over existing file
			await sol.SaveAsync (Util.GetMonitor ());
			sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			CheckConsoleProject (sol);

			sol.Dispose ();
		}
		
		public static async Task TestLoadSaveResources (MSBuildFileFormat fileFormat)
		{
			string solFile = Util.GetSampleProject ("resources-tester", "ResourcesTester.sln");
			Solution sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			sol.ConvertToFormat (fileFormat);
			ProjectTests.CheckResourcesSolution (sol);
			
			await sol.SaveAsync (Util.GetMonitor ());
			solFile = sol.FileName;

			sol.Dispose ();

			sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			ProjectTests.CheckResourcesSolution (sol);
			
			DotNetProject p = (DotNetProject) sol.Items [0];
			string f = Path.Combine (p.BaseDirectory, "Bitmap1.bmp");
			ProjectFile pf = p.Files.GetFile (f);
			pf.ResourceId = "SomeBitmap.bmp";
			await sol.SaveAsync (Util.GetMonitor ());

			sol.Dispose ();

			sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			p = (DotNetProject) sol.Items [0];
			f = Path.Combine (p.BaseDirectory, "Bitmap1.bmp");
			pf = p.Files.GetFile (f);
			Assert.AreEqual ("SomeBitmap.bmp", pf.ResourceId);

			sol.Dispose ();
		}
	}
	
	public class GenericItem: Project
	{
		[ItemProperty]
		public string SomeValue;

		public GenericItem ()
		{
			Initialize (this);
		}
	}
}
