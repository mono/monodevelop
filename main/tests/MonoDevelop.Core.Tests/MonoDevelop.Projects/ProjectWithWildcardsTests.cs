//
// ProjectWithWildcardsTests.cs
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

using System.IO;
using NUnit.Framework;
using UnitTests;
using MonoDevelop.Core;
using System.Linq;
using MonoDevelop.Projects.MSBuild;
using System.Threading.Tasks;

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class ProjectWithWildcardsTests: TestBase
	{
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

			Assert.IsNotNull (f);
			Assert.AreEqual ("other.rst", f.Link.ToString ());

			sol.Dispose ();
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

			foreach (var file in mp.Files) {
				File.Delete (file.FilePath);
			}

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
			var textFileItems = mp.MSBuildProject.EvaluatedItems.Where (item => item.Name == "None").ToArray ();
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
	}

	class SupportImportedProjectFilesDotNetProjectExtension : DotNetProjectExtension
	{
		internal protected override bool OnGetSupportsImportedItem (IMSBuildItemEvaluated buildItem)
		{
			return BuildAction.DotNetActions.Contains (buildItem.Name);
		}
	}
}
