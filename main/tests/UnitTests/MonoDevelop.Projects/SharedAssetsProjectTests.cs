﻿//
// SharedAssetsProjectTests.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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
using NUnit.Framework;
using System.Linq;
using UnitTests;
using MonoDevelop.Projects.SharedAssetsProjects;
using System.IO;

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class SharedAssetsProjectTests: TestBase
	{
		[Test]
		public void LoadSharedProject ()
		{
			string solFile = Util.GetSampleProject ("SharedProjectTest", "SharedProjectTest.sln");
			Solution sol = (Solution) Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

			Assert.AreEqual (4, sol.GetAllProjects ().Count);

			var pc1 = sol.FindProjectByName ("Console1");
			Assert.IsNotNull (pc1);

			var pc2 = sol.FindProjectByName ("Console2");
			Assert.IsNotNull (pc2);

			var pc3 = sol.FindProjectByName ("Console3");
			Assert.IsNotNull (pc3);

			var pcs = (SharedAssetsProject) sol.FindProjectByName ("Shared");
			Assert.IsNotNull (pcs);

			var sharedFile = pcs.ItemDirectory.Combine ("MyClass.cs");

			Assert.IsTrue (pc1.Files.GetFile (sharedFile) != null);
			Assert.IsTrue (pc2.Files.GetFile (sharedFile) != null);
			Assert.IsFalse (pc3.Files.GetFile (sharedFile) != null);
			Assert.IsTrue (pcs.Files.GetFile (sharedFile) != null);

			Assert.AreEqual ("SharedNamespace", pcs.DefaultNamespace);
		}

		[Test]
		public void PropagateFileChanges ()
		{
			string solFile = Util.GetSampleProject ("SharedProjectTest", "SharedProjectTest.sln");
			Solution sol = (Solution) Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var pc1 = sol.FindProjectByName ("Console1");
			var pc2 = sol.FindProjectByName ("Console2");
			var pc3 = sol.FindProjectByName ("Console3");
			var ps = sol.FindProjectByName ("Shared");

			var sharedFile = ps.ItemDirectory.Combine ("MyFutureClass.cs");
			ps.Files.Add (new ProjectFile (sharedFile));
			var pc1f = pc1.Files.GetFile (sharedFile);
			var pc2f = pc2.Files.GetFile (sharedFile);
			var pc3f = pc3.Files.GetFile (sharedFile);
			var psf = ps.Files.GetFile (sharedFile);

			Assert.AreEqual ("Compile", pc1f.BuildAction);
			Assert.AreEqual ("Compile", pc2f.BuildAction);
			Assert.AreEqual ("Compile", psf.BuildAction);

			Assert.IsTrue (pc1f != null);
			Assert.IsTrue (pc2f != null);
			Assert.IsTrue (pc3f == null);
			Assert.IsTrue (psf != null);

			psf.BuildAction = "TestAction";
			pc1f = pc1.Files.GetFile (sharedFile);
			pc2f = pc2.Files.GetFile (sharedFile);
			Assert.AreEqual ("TestAction", pc1f.BuildAction);
			Assert.AreEqual ("TestAction", pc2f.BuildAction);

			psf.CopyToOutputDirectory = FileCopyMode.PreserveNewest;
			pc1f = pc1.Files.GetFile (sharedFile);
			pc2f = pc2.Files.GetFile (sharedFile);
			Assert.AreEqual (FileCopyMode.PreserveNewest, pc1f.CopyToOutputDirectory);
			Assert.AreEqual (FileCopyMode.PreserveNewest, pc2f.CopyToOutputDirectory);

			ps.Files.Remove (psf);
			pc1f = pc1.Files.GetFile (sharedFile);
			pc2f = pc2.Files.GetFile (sharedFile);
			Assert.IsTrue (pc1f == null);
			Assert.IsTrue (pc2f == null);
		}

		[Test]
		public void AddReference ()
		{
			string solFile = Util.GetSampleProject ("SharedProjectTest", "SharedProjectTest.sln");
			Solution sol = (Solution) Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

			var pcs = sol.FindProjectByName ("Shared");
			var pc3 = (DotNetProject) sol.FindProjectByName ("Console3");

			var sharedFile = pcs.ItemDirectory.Combine ("MyClass.cs");
			Assert.IsFalse (pc3.Files.GetFile (sharedFile) != null);

			var r = pc3.References.FirstOrDefault (re => re.Reference == "Shared");
			Assert.IsNull (r);

			pc3.References.Add (new ProjectReference (pcs));

			r = pc3.References.FirstOrDefault (re => re.Reference == "Shared");
			Assert.IsNotNull (r);

			Assert.IsTrue (pc3.Files.GetFile (sharedFile) != null);
		}

		[Test]
		public void RemoveReference ()
		{
			string solFile = Util.GetSampleProject ("SharedProjectTest", "SharedProjectTest.sln");
			Solution sol = (Solution) Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

			var pc1 = (DotNetProject) sol.FindProjectByName ("Console1");
			var pc2 = (DotNetProject) sol.FindProjectByName ("Console2");
			var pcs = sol.FindProjectByName ("Shared");

			var sharedFile = pcs.ItemDirectory.Combine ("MyClass.cs");

			Assert.IsTrue (pc1.Files.GetFile (sharedFile) != null);
			Assert.IsTrue (pc2.Files.GetFile (sharedFile) != null);
			Assert.IsTrue (pcs.Files.GetFile (sharedFile) != null);

			var r = pc1.References.FirstOrDefault (re => re.Reference == "Shared");
			pc1.References.Remove (r);

			Assert.IsFalse (pc1.Files.GetFile (sharedFile) != null);
			Assert.IsTrue (pc2.Files.GetFile (sharedFile) != null);
			Assert.IsTrue (pcs.Files.GetFile (sharedFile) != null);
		}

		[Test]
		public void SaveSharedProject ()
		{
			Solution sol = TestProjectsChecks.CreateConsoleSolution ("shared-project");
			sol.ConvertToFormat (Util.FileFormatMSBuild12, true);
			sol.Save (Util.GetMonitor ());

			var pc = (DotNetProject) sol.Items [0];

			// Add shared project

			var sp = new SharedAssetsProject () {
				LanguageName = "C#",
				DefaultNamespace = "TestNamespace"
			};

			sp.AddFile (sol.ItemDirectory.Combine ("Test.cs"));
			sp.Save (sol.ItemDirectory.Combine ("Shared"), Util.GetMonitor ());

			sol.RootFolder.AddItem (sp);
			sol.Save (Util.GetMonitor ());

			// Make sure we compare using the same guid

			string solXml = File.ReadAllText (sol.FileName).Replace (pc.ItemId, "{7DE4B613-BAB6-49DE-83FA-707D4E120306}").Replace (sp.ItemId, "{8DD793BE-42C3-4D66-8359-460CEE75980D}");
			string projectXml = Util.GetXmlFileInfoset (pc.FileName).Replace (pc.ItemId, "{7DE4B613-BAB6-49DE-83FA-707D4E120306}");
			string sharedProjectXml = Util.GetXmlFileInfoset (sp.FileName).Replace (sp.ItemId, "{8DD793BE-42C3-4D66-8359-460CEE75980D}");
			string sharedProjectItemsXml = Util.GetXmlFileInfoset (sp.FileName.ChangeExtension (".projitems")).Replace (sp.ItemId, "{8DD793BE-42C3-4D66-8359-460CEE75980D}");

			string refSolXml = Util.ToWindowsEndings (File.ReadAllText (Util.GetSampleProjectPath ("generated-shared-project", "TestSolution.sln")));
			string refProjectXml = Util.ToWindowsEndings (Util.GetXmlFileInfoset (Util.GetSampleProjectPath ("generated-shared-project", "TestProject.csproj")));
			string refSharedProjectXml = Util.ToWindowsEndings (Util.GetXmlFileInfoset (Util.GetSampleProjectPath ("generated-shared-project", "Shared.shproj")));
			string refSharedProjectItemsXml = Util.ToWindowsEndings (Util.GetXmlFileInfoset (Util.GetSampleProjectPath ("generated-shared-project", "Shared.projitems")));

			Assert.AreEqual (refSolXml, solXml);
			Assert.AreEqual (refProjectXml, projectXml);
			Assert.AreEqual (refSharedProjectXml, sharedProjectXml);
			Assert.AreEqual (refSharedProjectItemsXml, sharedProjectItemsXml);

			// Add a reference

			var r = new ProjectReference (sp);
			pc.References.Add (r);
			sol.Save (Util.GetMonitor ());

			solXml = File.ReadAllText (sol.FileName).Replace (pc.ItemId, "{7DE4B613-BAB6-49DE-83FA-707D4E120306}").Replace (sp.ItemId, "{8DD793BE-42C3-4D66-8359-460CEE75980D}");
			projectXml = Util.GetXmlFileInfoset (pc.FileName).Replace (pc.ItemId, "{7DE4B613-BAB6-49DE-83FA-707D4E120306}");
			sharedProjectXml = Util.GetXmlFileInfoset (sp.FileName).Replace (sp.ItemId, "{8DD793BE-42C3-4D66-8359-460CEE75980D}");
			sharedProjectItemsXml = Util.GetXmlFileInfoset (sp.FileName.ChangeExtension (".projitems")).Replace (sp.ItemId, "{8DD793BE-42C3-4D66-8359-460CEE75980D}");

			refProjectXml = Util.ToWindowsEndings (Util.GetXmlFileInfoset (Util.GetSampleProjectPath ("generated-shared-project", "TestProject.csproj.saved1")));

			Assert.AreEqual (refSolXml, solXml);
			Assert.AreEqual (refProjectXml, projectXml);
			Assert.AreEqual (refSharedProjectXml, sharedProjectXml);
			Assert.AreEqual (refSharedProjectItemsXml, sharedProjectItemsXml);

			// Add a file and change the default namespace

			sp.DefaultNamespace = "TestNamespace2";
			var file = sp.AddFile (sol.ItemDirectory.Combine ("Test2.cs"));
			sol.Save (Util.GetMonitor ());

			solXml = File.ReadAllText (sol.FileName).Replace (pc.ItemId, "{7DE4B613-BAB6-49DE-83FA-707D4E120306}").Replace (sp.ItemId, "{8DD793BE-42C3-4D66-8359-460CEE75980D}");
			projectXml = Util.GetXmlFileInfoset (pc.FileName).Replace (pc.ItemId, "{7DE4B613-BAB6-49DE-83FA-707D4E120306}");
			sharedProjectXml = Util.GetXmlFileInfoset (sp.FileName).Replace (sp.ItemId, "{8DD793BE-42C3-4D66-8359-460CEE75980D}");
			sharedProjectItemsXml = Util.GetXmlFileInfoset (sp.FileName.ChangeExtension (".projitems")).Replace (sp.ItemId, "{8DD793BE-42C3-4D66-8359-460CEE75980D}");

			refSharedProjectItemsXml = Util.ToWindowsEndings (Util.GetXmlFileInfoset (Util.GetSampleProjectPath ("generated-shared-project", "Shared.projitems.saved1")));

			Assert.AreEqual (refSolXml, solXml);
			Assert.AreEqual (refProjectXml, projectXml);
			Assert.AreEqual (refSharedProjectXml, sharedProjectXml);
			Assert.AreEqual (refSharedProjectItemsXml, sharedProjectItemsXml);

			// Remove a file and restore the namespace

			sp.DefaultNamespace = "TestNamespace";
			sp.Files.Remove (file);
			sol.Save (Util.GetMonitor ());

			solXml = File.ReadAllText (sol.FileName).Replace (pc.ItemId, "{7DE4B613-BAB6-49DE-83FA-707D4E120306}").Replace (sp.ItemId, "{8DD793BE-42C3-4D66-8359-460CEE75980D}");
			projectXml = Util.GetXmlFileInfoset (pc.FileName).Replace (pc.ItemId, "{7DE4B613-BAB6-49DE-83FA-707D4E120306}");
			sharedProjectXml = Util.GetXmlFileInfoset (sp.FileName).Replace (sp.ItemId, "{8DD793BE-42C3-4D66-8359-460CEE75980D}");
			sharedProjectItemsXml = Util.GetXmlFileInfoset (sp.FileName.ChangeExtension (".projitems")).Replace (sp.ItemId, "{8DD793BE-42C3-4D66-8359-460CEE75980D}");

			refSharedProjectItemsXml = Util.ToWindowsEndings (Util.GetXmlFileInfoset (Util.GetSampleProjectPath ("generated-shared-project", "Shared.projitems")));

			Assert.AreEqual (refSolXml, solXml);
			Assert.AreEqual (refProjectXml, projectXml);
			Assert.AreEqual (refSharedProjectXml, sharedProjectXml);
			Assert.AreEqual (refSharedProjectItemsXml, sharedProjectItemsXml);

			// Remove reference

			pc.References.Remove (r);
			sol.Save (Util.GetMonitor ());

			solXml = File.ReadAllText (sol.FileName).Replace (pc.ItemId, "{7DE4B613-BAB6-49DE-83FA-707D4E120306}").Replace (sp.ItemId, "{8DD793BE-42C3-4D66-8359-460CEE75980D}");
			projectXml = Util.GetXmlFileInfoset (pc.FileName).Replace (pc.ItemId, "{7DE4B613-BAB6-49DE-83FA-707D4E120306}");
			sharedProjectXml = Util.GetXmlFileInfoset (sp.FileName).Replace (sp.ItemId, "{8DD793BE-42C3-4D66-8359-460CEE75980D}");
			sharedProjectItemsXml = Util.GetXmlFileInfoset (sp.FileName.ChangeExtension (".projitems")).Replace (sp.ItemId, "{8DD793BE-42C3-4D66-8359-460CEE75980D}");

			refProjectXml = Util.ToWindowsEndings (Util.GetXmlFileInfoset (Util.GetSampleProjectPath ("generated-shared-project", "TestProject.csproj")));

			Assert.AreEqual (refSolXml, solXml);
			Assert.AreEqual (refProjectXml, projectXml);
			Assert.AreEqual (refSharedProjectXml, sharedProjectXml);
			Assert.AreEqual (refSharedProjectItemsXml, sharedProjectItemsXml);
		}

		[Test]
		public void SharedProjectCantBeStartup ()
		{
			var sol = new Solution ();
			var shared = new SharedAssetsProject ();

			// Shared projects are not executable
			Assert.IsFalse (shared.SupportsExecute ());

			sol.RootFolder.AddItem (shared);

			// The shared project is not executable, so it shouldn't be set as startup by default
			Assert.IsNull (sol.StartupItem);

			// An executable project is set as startup by default when there is no startup project
			DotNetAssemblyProject project = new DotNetAssemblyProject ("C#");
			sol.RootFolder.AddItem (project);
			Assert.IsTrue (sol.StartupItem == project);
		}

		[Test]
		public void IncludingProjectAddedAfterShared ()
		{
			var sol = new Solution ();
			var shared = new SharedAssetsProject ("C#");
			shared.AddFile ("Foo.cs");

			sol.RootFolder.AddItem (shared);

			// Reference to shared is added before adding project to solution
			var main = new DotNetAssemblyProject ("C#");
			main.References.Add (new ProjectReference (shared));
			sol.RootFolder.AddItem (main);

			Assert.IsNotNull (main.Files.GetFile ("Foo.cs"));
		}

		[Test]
		public void SharedProjectAddedAfterIncluder ()
		{
			var sol = new Solution ();
			var shared = new SharedAssetsProject ("C#");
			shared.AddFile ("Foo.cs");

			// Reference to shared is added before adding project to solution
			var main = new DotNetAssemblyProject ("C#");
			main.References.Add (new ProjectReference (shared));
			sol.RootFolder.AddItem (main);

			sol.RootFolder.AddItem (shared);

			Assert.IsNotNull (main.Files.GetFile ("Foo.cs"));
		}

		[Test]
		public void RemoveSharedProjectFromSolution ()
		{
			var sol = new Solution ();

			var shared = new SharedAssetsProject ("C#");
			shared.AddFile ("Foo.cs");

			var main = new DotNetAssemblyProject ("C#");
			var pref = new ProjectReference (shared);
			main.References.Add (pref);

			sol.RootFolder.AddItem (main);
			sol.RootFolder.AddItem (shared);

			Assert.IsNotNull (main.Files.GetFile ("Foo.cs"));
			Assert.IsTrue (main.References.Contains (pref));

			sol.RootFolder.Items.Remove (shared);

			// The shared file and the reference must be gone.

			Assert.IsNull (main.Files.GetFile ("Foo.cs"));
			Assert.IsFalse (main.References.Contains (pref));
		}
	}
}

