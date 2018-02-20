// ProjectTests.cs
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
using MonoDevelop.Core;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Projects.Policies;
using System.Xml;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.Projects
{
	[TestFixture()]
	public class ProjectTests: TestBase
	{
		[Test()]
		public void ProjectFilePaths ()
		{
			DotNetProject project = Services.ProjectService.CreateDotNetProject ("C#");
			string dir = Environment.CurrentDirectory;
			
			ProjectFile file1 = project.AddFile (Util.Combine (dir, "test1.cs"), BuildAction.Compile);
			Assert.AreEqual (Util.Combine (dir, "test1.cs"), file1.Name);
			
			ProjectFile file2 = project.AddFile (Util.Combine (dir, "aaa", "..", "bbb", "test2.cs"), BuildAction.Compile);
			Assert.AreEqual (Util.Combine (dir, "bbb", "test2.cs"), file2.Name);
			
			ProjectFile file = project.Files.GetFile (Util.Combine (dir, "test1.cs"));
			Assert.AreEqual (file1, file);
			
			file = project.Files.GetFile (Util.Combine (dir, "aaa", "..", "test1.cs"));
			Assert.AreEqual (file1, file);
			
			file = project.Files.GetFile (Util.Combine (dir, "bbb", "test2.cs"));
			Assert.AreEqual (file2, file);
			
			file = project.Files.GetFile (Util.Combine (dir, "aaa", "..", "bbb", "test2.cs"));
			Assert.AreEqual (file2, file);

			project.Dispose ();
		}
		
		[Test()]
		[Platform (Exclude = "Win")]
		public async Task Resources ()
		{
			string solFile = Util.GetSampleProject ("resources-tester", "ResourcesTester.sln");
			Solution sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			CheckResourcesSolution (sol);

			BuildResult res = await sol.Build (Util.GetMonitor (), "Debug");
			Assert.AreEqual (0, res.ErrorCount);
			Assert.AreEqual (0, res.WarningCount);
			Assert.AreEqual (1, res.BuildCount);

			string spath = Util.Combine (sol.BaseDirectory, "ResourcesTester", "bin", "Debug", "ca", "ResourcesTesterApp.resources.dll");
			Assert.IsTrue (File.Exists (spath), "Satellite assembly not generated");

			await sol.Clean (Util.GetMonitor (), "Debug");
			Assert.IsFalse (File.Exists (spath), "Satellite assembly not removed");

			// msbuild doesn't delete this directory
			// Assert.IsFalse (Directory.Exists (Path.GetDirectoryName (spath)), "Satellite assembly directory not removed");

			sol.Dispose ();
		}
		
		public static void CheckResourcesSolution (Solution sol)
		{
			DotNetProject p = (DotNetProject) sol.Items [0];
			Assert.AreEqual ("ResourcesTesterNamespace", p.DefaultNamespace);
			
			string f = Path.Combine (p.BaseDirectory, "Bitmap1.bmp");
			ProjectFile pf = p.Files.GetFile (f);
			Assert.IsNotNull (pf, "Bitmap1.bmp not found");
			Assert.AreEqual (BuildAction.EmbeddedResource, pf.BuildAction);
			Assert.AreEqual ("ResourcesTesterNamespace.Bitmap1.bmp", pf.ResourceId);
			
			f = Path.Combine (p.BaseDirectory, "BitmapCultured.ca.bmp");
			pf = p.Files.GetFile (f);
			Assert.IsNotNull (pf, "BitmapCultured.ca.bmp not found");
			Assert.AreEqual (BuildAction.EmbeddedResource, pf.BuildAction);
			Assert.AreEqual ("ResourcesTesterNamespace.BitmapCultured.bmp", pf.ResourceId);
			
			f = Path.Combine (p.BaseDirectory, "Cultured.ca.resx");
			pf = p.Files.GetFile (f);
			Assert.IsNotNull (pf, "Cultured.ca.resx not found");
			Assert.AreEqual (BuildAction.EmbeddedResource, pf.BuildAction);
			Assert.AreEqual ("ResourcesTesterNamespace.Cultured.ca.resources", pf.ResourceId);
			
			f = Path.Combine (p.BaseDirectory, "FormFile.ca.resx");
			pf = p.Files.GetFile (f);
			Assert.IsNotNull (pf, "FormFile.ca.resx not found");
			Assert.AreEqual (BuildAction.EmbeddedResource, pf.BuildAction);
			Assert.AreEqual ("ResourcesTester.Form1.ca.resources", pf.ResourceId);
			
			f = Path.Combine (p.BaseDirectory, "FormFile.resx");
			pf = p.Files.GetFile (f);
			Assert.IsNotNull (pf, "FormFile.resx not found");
			Assert.AreEqual (BuildAction.EmbeddedResource, pf.BuildAction);
			Assert.AreEqual ("ResourcesTester.Form1.resources", pf.ResourceId);
			
			f = Path.Combine (p.BaseDirectory, "Normal.resx");
			pf = p.Files.GetFile (f);
			Assert.IsNotNull (pf, "Normal.resx not found");
			Assert.AreEqual (BuildAction.EmbeddedResource, pf.BuildAction);
			Assert.AreEqual ("ResourcesTesterNamespace.Normal.resources", pf.ResourceId);
			
			string subdir = Path.Combine (p.BaseDirectory, "Subfolder");
			
			f = Path.Combine (subdir, "Bitmap2.bmp");
			pf = p.Files.GetFile (f);
			Assert.IsNotNull (pf, "Subfolder/Bitmap2.bmp not found");
			Assert.AreEqual (BuildAction.EmbeddedResource, pf.BuildAction);
			Assert.AreEqual ("ResourcesTesterNamespace.Subfolder.Bitmap2.bmp", pf.ResourceId);
			
			f = Path.Combine (subdir, "BitmapCultured2.ca.bmp");
			pf = p.Files.GetFile (f);
			Assert.IsNotNull (pf, "Subfolder/BitmapCultured2.ca.bmp not found");
			Assert.AreEqual (BuildAction.EmbeddedResource, pf.BuildAction);
			Assert.AreEqual ("ResourcesTesterNamespace.Subfolder.BitmapCultured2.bmp", pf.ResourceId);
			
			f = Path.Combine (subdir, "Cultured2.ca.resx");
			pf = p.Files.GetFile (f);
			Assert.IsNotNull (pf, "Subfolder/Cultured2.ca.resx not found");
			Assert.AreEqual (BuildAction.EmbeddedResource, pf.BuildAction);
			Assert.AreEqual ("ResourcesTesterNamespace.Subfolder.Cultured2.ca.resources", pf.ResourceId);
			
			f = Path.Combine (subdir, "FormFile2.ca.resx");
			pf = p.Files.GetFile (f);
			Assert.IsNotNull (pf, "Subfolder/FormFile2.ca.resx not found");
			Assert.AreEqual (BuildAction.EmbeddedResource, pf.BuildAction);
			Assert.AreEqual ("ResourcesTester.Form2.ca.resources", pf.ResourceId);
			
			f = Path.Combine (subdir, "FormFile2.resx");
			pf = p.Files.GetFile (f);
			Assert.IsNotNull (pf, "Subfolder/FormFile2.resx not found");
			Assert.AreEqual (BuildAction.EmbeddedResource, pf.BuildAction);
			Assert.AreEqual ("ResourcesTester.Form2.resources", pf.ResourceId);
			
			f = Path.Combine (subdir, "Normal2.resx");
			pf = p.Files.GetFile (f);
			Assert.IsNotNull (pf, "Subfolder/Normal2.resx not found");
			Assert.AreEqual (BuildAction.EmbeddedResource, pf.BuildAction);
			Assert.AreEqual ("ResourcesTesterNamespace.Subfolder.Normal2.resources", pf.ResourceId);
		}
		
		[Test()]
		public void ProjectNameWithDots ()
		{
			// Test case for bug #437392

			ProjectCreateInformation info = new ProjectCreateInformation ();
			info.ProjectName = "Some.Test";
			info.ProjectBasePath = "/tmp/test";
			var doc = new XmlDocument ();
			var projectOptions = doc.CreateElement ("Options");
			projectOptions.SetAttribute ("language", "C#");
			DotNetProject p = (DotNetProject) Services.ProjectService.CreateProject ("C#", info, projectOptions);

			Assert.AreEqual (2, p.Configurations.Count);
			Assert.AreEqual ("Debug", p.Configurations [0].Name);
			Assert.AreEqual ("Release", p.Configurations [1].Name);
			
			Assert.AreEqual ("Some.Test", ((DotNetProjectConfiguration) p.Configurations [0]).OutputAssembly);
			Assert.AreEqual ("Some.Test", ((DotNetProjectConfiguration) p.Configurations [1]).OutputAssembly);

			p.Dispose ();
		}
		
		[Test()]
		public void NewConfigurationsHaveAnAssemblyName ()
		{
			DotNetProject p = Services.ProjectService.CreateDotNetProject ("C#");
			p.Name = "HiThere";
			DotNetProjectConfiguration c = (DotNetProjectConfiguration) p.CreateConfiguration ("First");
			Assert.AreEqual ("HiThere", c.OutputAssembly);
			p.Dispose ();
		}
		
		[Test()]
		public void CustomCommands ()
		{
			DotNetProject p = Services.ProjectService.CreateDotNetProject ("C#");
			p.Name = "SomeProject";
			DotNetProjectConfiguration c = (DotNetProjectConfiguration) p.CreateConfiguration ("First");
			
			CustomCommand cmd = new CustomCommand ();
			cmd.Command = "aa bb cc";
			Assert.AreEqual ("aa", cmd.GetCommandFile (p, c.Selector));
			Assert.AreEqual ("bb cc", cmd.GetCommandArgs (p, c.Selector));
			
			cmd.Command = "\"aa bb\" cc dd";
			Assert.AreEqual ("aa bb", cmd.GetCommandFile (p, c.Selector));
			Assert.AreEqual ("cc dd", cmd.GetCommandArgs (p, c.Selector));
			
			cmd.Command = "\"aa ${ProjectName}\" cc ${ProjectName}";
			Assert.AreEqual ("aa SomeProject", cmd.GetCommandFile (p, c.Selector));
			Assert.AreEqual ("cc SomeProject", cmd.GetCommandArgs (p, c.Selector));
			
			cmd.WorkingDir = NormalizePath ("/some/${ProjectName}/place");
			Assert.AreEqual (Path.GetFullPath (NormalizePath ("/some/SomeProject/place")), (string)cmd.GetCommandWorkingDir (p, c.Selector));
			p.Dispose ();
		}
		
		[Test()]
		public async Task FileDependencies ()
		{
			string solFile = Util.GetSampleProject ("file-dependencies", "ConsoleProject.sln");
			Solution sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

			Project p = (Project) sol.Items [0];
			var dir = p.BaseDirectory;

			var file1 = p.Files.GetFile (dir.Combine ("file1.xml"));
			var file2 = p.Files.GetFile (dir.Combine ("file2.xml"));
			var file3 = p.Files.GetFile (dir.Combine ("file3.xml"));
			var file4 = p.Files.GetFile (dir.Combine ("file4.xml"));
			var file5 = p.Files.GetFile (dir.Combine ("file5.xml"));

			Assert.AreEqual (file3, file1.DependsOnFile);
			Assert.AreEqual (file3, file2.DependsOnFile);

			Assert.AreEqual (2, file3.DependentChildren.Count);
			Assert.IsTrue (file3.DependentChildren.Contains (file1));
			Assert.IsTrue (file3.DependentChildren.Contains (file2));

			Assert.AreEqual (file5, file4.DependsOnFile);
			Assert.AreEqual (1, file5.DependentChildren.Count);
			Assert.IsTrue (file5.DependentChildren.Contains (file4));

			// Change a dependency

			file1.DependsOn = "";
			Assert.IsNull (file1.DependsOnFile);
			Assert.AreEqual (file3, file2.DependsOnFile);

			Assert.AreEqual (1, file3.DependentChildren.Count);
			Assert.IsTrue (file3.DependentChildren.Contains (file2));

			// Unresolved dependency

			file1.DependsOn = "foo.xml";
			Assert.IsNull (file1.DependsOnFile);

			var foo = p.AddFile (dir.Combine ("foo.xml"));
			Assert.AreEqual (foo, file1.DependsOnFile);

			// Resolved dependency

			file2.DependsOn = "foo.xml";
			Assert.AreEqual (foo, file2.DependsOnFile);
			Assert.AreEqual (0, file3.DependentChildren.Count);

			// Remove a file

			p.Files.Remove (file5);
			Assert.IsNull (file4.DependsOnFile);

			// Add a file

			file5 = p.AddFile (dir.Combine ("file5.xml"));
			Assert.AreEqual (file5, file4.DependsOnFile);
			Assert.AreEqual (1, file5.DependentChildren.Count);
			Assert.IsTrue (file5.DependentChildren.Contains (file4));
			sol.Dispose ();
		}

		public static string NormalizePath (string path)
		{
			return path.Replace ('/', Path.DirectorySeparatorChar);
		}

		[Test]
		public async Task RefreshReferences ()
		{
			string solFile = Util.GetSampleProject ("reference-refresh", "ConsoleProject.sln");

			Solution sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			DotNetProject project = sol.GetAllItems<DotNetProject> ().FirstOrDefault ();

			Assert.AreEqual (4, project.References.Count);

			ProjectReference r;

			r = project.References.FirstOrDefault (re => re.Reference.StartsWith ("System,"));
			Assert.IsNotNull (r);
			Assert.AreEqual (r.ReferenceType, ReferenceType.Package);
			Assert.IsTrue (r.IsValid);

			r = project.References.FirstOrDefault (re => re.Reference.StartsWith ("System.Xml,"));
			Assert.IsNotNull (r);
			Assert.AreEqual (r.ReferenceType, ReferenceType.Package);
			Assert.IsTrue (r.IsValid);

			r = project.References.FirstOrDefault (re => re.Reference == "test");
			Assert.IsNotNull (r);
			Assert.AreEqual (r.ReferenceType, ReferenceType.Assembly);
			Assert.AreEqual (r.GetReferencedFileNames(project.DefaultConfiguration.Selector).Single (), project.BaseDirectory.Combine ("test.dll").FullPath.ToString ());
			Assert.IsTrue (r.IsValid);

			r = project.References.FirstOrDefault (re => re.Reference == "gtk-sharp");
			Assert.IsNotNull (r);
			Assert.AreEqual (r.ReferenceType, ReferenceType.Assembly);
			Assert.AreEqual (r.GetReferencedFileNames(project.DefaultConfiguration.Selector).Single (), project.BaseDirectory.Combine ("gtk-sharp.dll").FullPath.ToString ());
			Assert.IsTrue (r.IsValid);

			// Refresh without any change

			project.RefreshReferenceStatus ();

			Assert.AreEqual (4, project.References.Count);

			r = project.References.FirstOrDefault (re => re.Reference.StartsWith ("System,"));
			Assert.IsNotNull (r);
			Assert.AreEqual (r.ReferenceType, ReferenceType.Package);
			Assert.IsTrue (r.IsValid);

			r = project.References.FirstOrDefault (re => re.Reference.StartsWith ("System.Xml,"));
			Assert.IsNotNull (r);
			Assert.AreEqual (r.ReferenceType, ReferenceType.Package);
			Assert.IsTrue (r.IsValid);

			r = project.References.FirstOrDefault (re => re.Reference == "test");
			Assert.IsNotNull (r);
			Assert.AreEqual (r.ReferenceType, ReferenceType.Assembly);
			Assert.AreEqual (r.GetReferencedFileNames(project.DefaultConfiguration.Selector).Single (), project.BaseDirectory.Combine ("test.dll").FullPath.ToString ());
			Assert.IsTrue (r.IsValid);

			r = project.References.FirstOrDefault (re => re.Reference == "gtk-sharp");
			Assert.IsNotNull (r);
			Assert.AreEqual (r.ReferenceType, ReferenceType.Assembly);
			Assert.AreEqual (r.GetReferencedFileNames(project.DefaultConfiguration.Selector).Single (), project.BaseDirectory.Combine ("gtk-sharp.dll").FullPath.ToString ());
			Assert.IsTrue (r.IsValid);

			// Refresh after deleting test.dll

			File.Move (project.BaseDirectory.Combine ("test.dll"), project.BaseDirectory.Combine ("test.dll.tmp"));
			project.RefreshReferenceStatus ();

			Assert.AreEqual (4, project.References.Count);

			r = project.References.FirstOrDefault (re => re.Reference.StartsWith ("System,"));
			Assert.IsNotNull (r);
			Assert.AreEqual (r.ReferenceType, ReferenceType.Package);
			Assert.IsTrue (r.IsValid);

			r = project.References.FirstOrDefault (re => re.Reference.StartsWith ("System.Xml,"));
			Assert.IsNotNull (r);
			Assert.AreEqual (r.ReferenceType, ReferenceType.Package);
			Assert.IsTrue (r.IsValid);

			r = project.References.FirstOrDefault (re => re.Reference == "test");
			Assert.IsNotNull (r);
			Assert.AreEqual (r.ReferenceType, ReferenceType.Package);
			Assert.IsFalse (r.IsValid);

			r = project.References.FirstOrDefault (re => re.Reference == "gtk-sharp");
			Assert.IsNotNull (r);
			Assert.AreEqual (r.ReferenceType, ReferenceType.Assembly);
			Assert.AreEqual (r.GetReferencedFileNames(project.DefaultConfiguration.Selector).Single (), project.BaseDirectory.Combine ("gtk-sharp.dll").FullPath.ToString ());
			Assert.IsTrue (r.IsValid);

			// Refresh after deleting gtk-sharp.dll

			File.Move (project.BaseDirectory.Combine ("gtk-sharp.dll"), project.BaseDirectory.Combine ("gtk-sharp.dll.tmp"));
			project.RefreshReferenceStatus ();

			Assert.AreEqual (4, project.References.Count);

			r = project.References.FirstOrDefault (re => re.Reference.StartsWith ("System,"));
			Assert.IsNotNull (r);
			Assert.AreEqual (r.ReferenceType, ReferenceType.Package);
			Assert.IsTrue (r.IsValid);

			r = project.References.FirstOrDefault (re => re.Reference.StartsWith ("System.Xml,"));
			Assert.IsNotNull (r);
			Assert.AreEqual (r.ReferenceType, ReferenceType.Package);
			Assert.IsTrue (r.IsValid);

			r = project.References.FirstOrDefault (re => re.Reference == "test");
			Assert.IsNotNull (r);
			Assert.AreEqual (r.ReferenceType, ReferenceType.Package);
			Assert.IsFalse (r.IsValid);

			r = project.References.FirstOrDefault (re => re.Reference.StartsWith ("gtk-sharp,"));
			Assert.IsNotNull (r);
			Assert.AreEqual (r.ReferenceType, ReferenceType.Package);
			Assert.AreEqual ("gtk-sharp.dll", Path.GetFileName (r.GetReferencedFileNames(project.DefaultConfiguration.Selector).Single ()));
			Assert.IsTrue (r.IsValid);		

			// Refresh after restoring gtk-sharp.dll and test.dll

			File.Move (project.BaseDirectory.Combine ("test.dll.tmp"), project.BaseDirectory.Combine ("test.dll"));
			File.Move (project.BaseDirectory.Combine ("gtk-sharp.dll.tmp"), project.BaseDirectory.Combine ("gtk-sharp.dll"));
			project.RefreshReferenceStatus ();

			Assert.AreEqual (4, project.References.Count);

			r = project.References.FirstOrDefault (re => re.Reference.StartsWith ("System,"));
			Assert.IsNotNull (r);
			Assert.AreEqual (r.ReferenceType, ReferenceType.Package);
			Assert.IsTrue (r.IsValid);

			r = project.References.FirstOrDefault (re => re.Reference.StartsWith ("System.Xml,"));
			Assert.IsNotNull (r);
			Assert.AreEqual (r.ReferenceType, ReferenceType.Package);
			Assert.IsTrue (r.IsValid);

			r = project.References.FirstOrDefault (re => re.Reference == "test");
			Assert.IsNotNull (r);
			Assert.AreEqual (r.ReferenceType, ReferenceType.Assembly);
			Assert.AreEqual (r.GetReferencedFileNames(project.DefaultConfiguration.Selector).Single (), project.BaseDirectory.Combine ("test.dll").FullPath.ToString ());
			Assert.IsTrue (r.IsValid);

			r = project.References.FirstOrDefault (re => re.Reference.StartsWith ("gtk-sharp,"));
			Assert.IsNotNull (r);
			Assert.AreEqual (r.ReferenceType, ReferenceType.Assembly);
			Assert.AreEqual (r.GetReferencedFileNames(project.DefaultConfiguration.Selector).Single (), project.BaseDirectory.Combine ("gtk-sharp.dll").FullPath.ToString ());
			Assert.IsTrue (r.IsValid);

			sol.Dispose ();
		}

		[Test]
		public async Task RemoveRefreshedReferenceSaveProjectAndAddReferenceBackAgain ()
		{
			string solFile = Util.GetSampleProject ("reference-refresh", "ConsoleProject.sln");

			Solution sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			DotNetProject project = sol.GetAllItems<DotNetProject> ().FirstOrDefault ();

			File.Move (project.BaseDirectory.Combine ("test.dll"), project.BaseDirectory.Combine ("test.dll.tmp"));

			sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			project = sol.GetAllItems<DotNetProject> ().FirstOrDefault ();

			ProjectReference r = project.References.FirstOrDefault (re => re.Reference == "test");
			Assert.IsNotNull (r);
			Assert.AreEqual (r.ReferenceType, ReferenceType.Package);
			Assert.IsFalse (r.IsValid);

			File.Move (project.BaseDirectory.Combine ("test.dll.tmp"), project.BaseDirectory.Combine ("test.dll"));

			ProjectReference refreshedReference = r.GetRefreshedReference ();
			Assert.IsNotNull (refreshedReference);

			project.References.Remove (r);
			await project.SaveAsync (Util.GetMonitor ());

			project.References.Add (refreshedReference);
			await project.SaveAsync (Util.GetMonitor ());

			// Reload project.
			sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			project = sol.GetAllItems<DotNetProject> ().FirstOrDefault ();

			r = project.References.FirstOrDefault (re => re.Reference == "test");
			Assert.IsNotNull (r);
			Assert.AreEqual (r.ReferenceType, ReferenceType.Assembly);
			Assert.AreEqual (r.GetReferencedFileNames(project.DefaultConfiguration.Selector).Single (), project.BaseDirectory.Combine ("test.dll").FullPath.ToString ());
			Assert.IsTrue (r.IsValid);

			sol.Dispose ();
		}

		[Test]
		public void AssemblyReferenceHintPath ()
		{
			var file = (FilePath) GetType ().Assembly.Location;
			var asmName = Path.GetFileNameWithoutExtension (file);

			var r = ProjectReference.CreateAssemblyFileReference (file);
			Assert.AreEqual (asmName, r.Reference);
			Assert.AreEqual (file, r.HintPath);

			r = ProjectReference.CreateCustomReference (ReferenceType.Assembly, "Foo", file);
			Assert.AreEqual ("Foo", r.Reference);
			Assert.AreEqual (file, r.HintPath);

		}

		[Test]
		public async Task RefreshInMemoryProjectFirstTime ()
		{
			// Check that the in-memory project data is used when the builder is loaded for the first time.

			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			Solution sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

			var p = (DotNetProject) sol.Items [0];
			p.References.Add (ProjectReference.CreateAssemblyReference ("System.Xml.Linq"));

			var refs = (await p.GetReferencedAssemblies (ConfigurationSelector.Default)).ToArray ();

			Assert.IsTrue (refs.Any (r => r.FilePath.FileName == "System.Xml.Linq.dll"));
			sol.Dispose ();
		}

		[Test]
		public async Task RefreshInMemoryProject ()
		{
			// Check that the builder is refreshed when the file has been modified in memory.

			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			Solution sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

			var p = (DotNetProject) sol.Items [0];

			// This will force the loading of the builder
			(await p.GetReferencedAssemblies (ConfigurationSelector.Default)).ToArray ();

			p.References.Add (ProjectReference.CreateAssemblyReference ("System.Xml.Linq"));

			var refs = (await p.GetReferencedAssemblies (ConfigurationSelector.Default)).ToArray ();

			// Check that the in-memory project data is used when the builder is loaded for the first time.
			Assert.IsTrue (refs.Any (r => r.FilePath.FileName == "System.Xml.Linq.dll"));

			sol.Dispose ();
		}

		[Test]
		public async Task GetReferencesIncludesProjectReferences ()
		{
			string solFile = Util.GetSampleProject ("console-with-libs", "console-with-libs.sln");
			Solution sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

			var p = (DotNetProject) sol.FindProjectByName ("console-with-libs");
			var lib1Project = (DotNetProject) sol.FindProjectByName ("library1");
			var lib2Project = (DotNetProject) sol.FindProjectByName ("library2");

			// This will force the loading of the builder
			var refs = await p.GetReferences (ConfigurationSelector.Default);

			var projectRefs = refs.Where (r => r.IsProjectReference).ToList ();
			var lib1ProjectRef = projectRefs.FirstOrDefault (r => r.FilePath.FileName == "library1.dll");
			var lib2ProjectRef = projectRefs.FirstOrDefault (r => r.FilePath.FileName == "library2.dll");
			var systemRef = refs.FirstOrDefault (r => r.FilePath.FileName == "System.dll");

			Assert.AreEqual (lib1Project, lib1ProjectRef.GetReferencedItem (sol));
			Assert.AreEqual (lib2Project, lib2ProjectRef.GetReferencedItem (sol));
			Assert.IsTrue (lib1ProjectRef.ReferenceOutputAssembly);
			Assert.IsTrue (lib2ProjectRef.ReferenceOutputAssembly);
			Assert.IsTrue (lib1ProjectRef.IsCopyLocal);
			Assert.IsTrue (lib2ProjectRef.IsCopyLocal);
			Assert.IsTrue (systemRef.IsFrameworkFile);
			Assert.IsFalse (systemRef.IsCopyLocal);

			sol.Dispose ();
		}

		[Test]
		public async Task DefaultMSBuildSupport ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			Solution sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (DotNetProject) sol.Items [0];
			Assert.AreEqual (MSBuildSupport.Supported, p.MSBuildEngineSupport);
			sol.Dispose ();
		}

		[Test]
		public void DefaultMSBuildSupportForNewProject ()
		{
			var project = Services.ProjectService.CreateDotNetProject ("C#");
			bool byDefault, require;
			MSBuildProjectService.CheckHandlerUsesMSBuildEngine (project, out byDefault, out require);
			Assert.IsTrue (byDefault);
			Assert.IsFalse (require);

			project.Dispose ();
		}

		[Test]
		public async Task MSBuildResourceNamingPolicy ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			Solution sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (DotNetProject) sol.Items [0];

			var pol = new DotNetNamingPolicy (DirectoryNamespaceAssociation.Flat, ResourceNamePolicy.MSBuild);
			p.Policies.Set (pol);

			var f = p.AddFile (p.BaseDirectory.Combine ("foo/SomeFile.txt"), BuildAction.EmbeddedResource);
			Assert.AreEqual ("ConsoleProject.foo.SomeFile.txt", f.ResourceId);

			await p.SaveAsync (Util.GetMonitor ());

			var p2 = (DotNetProject) await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), p.FileName);
			f = p2.GetProjectFile (f.FilePath);
			Assert.AreEqual ("ConsoleProject.foo.SomeFile.txt", f.ResourceId);

			sol.Dispose ();
		}

		[Test]
		public async Task MDResourceNamingPolicy ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			Solution sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (DotNetProject) sol.Items [0];

			var pol = new DotNetNamingPolicy (DirectoryNamespaceAssociation.Flat, ResourceNamePolicy.FileName);
			p.Policies.Set (pol);

			var f = p.AddFile (p.BaseDirectory.Combine ("foo/SomeFile.txt"), BuildAction.EmbeddedResource);
			Assert.AreEqual ("SomeFile.txt", f.ResourceId);

			await p.SaveAsync (Util.GetMonitor ());

			var p2 = (DotNetProject) await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), p.FileName);
			f = p2.GetProjectFile (f.FilePath);
			Assert.AreEqual ("SomeFile.txt", f.ResourceId);

			sol.Dispose ();
		}

		[Test]
		public async Task LoadResourceWithCorrectId ()
		{
			string projFile = Util.GetSampleProject ("test-resource-id", "ConsoleProject.csproj");
			var p = (DotNetProject) await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);

			var f = p.Files.FirstOrDefault (pf => pf.FilePath.FileName == "SomeFile.txt");
			Assert.AreEqual ("ConsoleProject.foo.SomeFile.txt", f.ResourceId);

			var pol = p.Policies.Get<DotNetNamingPolicy> ();
			Assert.AreEqual (ResourceNamePolicy.FileName, pol.ResourceNamePolicy);

			p.Dispose ();
		}

		[Test]
		public async Task AddReference ()
		{
			// Check that the in-memory project data is used when the builder is loaded for the first time.

			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			Solution sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

			var p = (DotNetProject) sol.Items [0];
			p.References.Add (ProjectReference.CreateAssemblyReference ("System.Xml.Linq"));

			var asm = p.AssemblyContext.GetAssemblies ().FirstOrDefault (a => a.Name == "System.Net");
			p.References.Add (ProjectReference.CreateAssemblyReference (asm));

			await p.SaveAsync (Util.GetMonitor ());

			var refXml = Util.ToSystemEndings (File.ReadAllText (p.FileName + ".reference-added"));
			var savedXml = File.ReadAllText (p.FileName);

			Assert.AreEqual (refXml, savedXml);

			sol.Dispose ();
		}

		[Test]
		public async Task ChangeBuildAction ()
		{
			// Check that the in-memory project data is used when the builder is loaded for the first time.

			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			Solution sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

			var p = (DotNetProject) sol.Items [0];
			var f = p.Files.FirstOrDefault (fi => fi.FilePath.FileName == "Program.cs");
			f.BuildAction = BuildAction.EmbeddedResource;

			await p.SaveAsync (Util.GetMonitor ());

			var refXml = Util.ToSystemEndings (File.ReadAllText (p.FileName + ".build-action-change1"));
			var savedXml = File.ReadAllText (p.FileName);

			Assert.AreEqual (refXml, savedXml);

			f.BuildAction = BuildAction.Compile;

			await p.SaveAsync (Util.GetMonitor ());

			refXml = Util.ToSystemEndings (File.ReadAllText (p.FileName + ".build-action-change2"));
			savedXml = File.ReadAllText (p.FileName);

			Assert.AreEqual (refXml, savedXml);

			sol.Dispose ();
		}

		[Test]
		public async Task AddRemoveReferenceEvents ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (DotNetProject)sol.Items [0];
			int added = 0, removed = 0, modifiedRefs = 0, modifiedItems = 0, refsChanged = 0;

			// We should get two ReferenceAdded events (one for each reference), but only one global modified and assemblies changed event.

			var refs = new [] { ProjectReference.CreateAssemblyReference ("Foo"), ProjectReference.CreateAssemblyReference ("Bar") };

			p.ReferenceAddedToProject += delegate {
				added++;
				Assert.IsTrue (refs.All (r => r.OwnerProject != null));
			};
			p.ReferenceRemovedFromProject += delegate {
				removed++;
				Assert.IsTrue (refs.All (r => r.OwnerProject == null));
			};

			EventHandler refsChangedHandler = delegate {
				refsChanged++;
				Assert.IsTrue (refs.All (r => r.OwnerProject != null));
			};
			p.ReferencedAssembliesChanged += refsChangedHandler;

			SolutionItemModifiedEventHandler modifiedHandler = delegate (object sender, SolutionItemModifiedEventArgs e) {
				foreach (var ev in e) {
					if (ev.Hint == "References")
						modifiedRefs++;
					if (ev.Hint == "Items")
						modifiedItems++;
				}
				Assert.IsTrue (refs.All (r => r.OwnerProject != null));
			};
			p.Modified += modifiedHandler;

			p.References.AddRange (refs);

			Assert.AreEqual (2, added);
			Assert.AreEqual (1, modifiedRefs);
			Assert.AreEqual (1, modifiedItems);
			Assert.AreEqual (1, refsChanged);

			modifiedRefs = modifiedItems = refsChanged = 0;
			p.ReferencedAssembliesChanged -= refsChangedHandler;
			p.Modified -= modifiedHandler;

			refsChangedHandler = delegate {
				refsChanged++;
				Assert.IsTrue (refs.All (r => r.OwnerProject == null));
			};
			p.ReferencedAssembliesChanged += refsChangedHandler;

			modifiedHandler = delegate (object sender, SolutionItemModifiedEventArgs e) {
				foreach (var ev in e) {
					if (ev.Hint == "References")
						modifiedRefs++;
					if (ev.Hint == "Items")
						modifiedItems++;
				}
				Assert.IsTrue (refs.All (r => r.OwnerProject == null));
			};
			p.Modified += modifiedHandler;

			p.References.RemoveRange (refs);

			Assert.AreEqual (2, removed);
			Assert.AreEqual (1, modifiedRefs);
			Assert.AreEqual (1, modifiedItems);
			Assert.AreEqual (1, refsChanged);

			sol.Dispose ();
		}

		[Test]
		public void GetDefaultNamespaceWhenProjectRootNamespaceContainsHyphen ()
		{
			var project = Services.ProjectService.CreateDotNetProject ("C#");
			project.DefaultNamespace = "abc-test";
			string defaultNamespace = project.GetDefaultNamespace (null);

			Assert.AreEqual ("abctest", defaultNamespace);
		}

		[TestCase ("ProjectName", null, "ProjectName")]
		[TestCase ("ProjectName", "", "ProjectName")]
		[TestCase ("ProjectName", "MyProject", "MyProject")]
		[TestCase ("1", "", "Application")]
		[TestCase ("ProjectName", "1", "Application")]
		public void GetDefaultNamespace_NullFileName (
			string projectName,
			string projectDefaultNamespace,
			string expectedDefaultNamespace)
		{
			var project = Services.ProjectService.CreateDotNetProject ("C#");
			project.Name = projectName;
			project.DefaultNamespace = projectDefaultNamespace;

			string result = project.GetDefaultNamespace (null);

			Assert.AreEqual (expectedDefaultNamespace, result);
		}
	}
}
