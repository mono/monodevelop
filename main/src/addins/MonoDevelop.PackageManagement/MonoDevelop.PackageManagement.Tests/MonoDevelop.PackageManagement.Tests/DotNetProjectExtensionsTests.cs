//
// DotNetProjectExtensionsTests.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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

using System.Collections.Generic;
using System.Linq;
using MonoDevelop.PackageManagement.Tests.Helpers;
using MonoDevelop.Projects;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class DotNetProjectExtensionsTests
	{
		List<string> existingFiles;
		FakeDotNetProject project;

		[SetUp]
		public void Init ()
		{
			existingFiles = new List<string> ();
			DotNetProjectExtensions.FileExists = existingFiles.Contains;
		}

		void CreateProject (string fileName, string projectName)
		{
			project = new FakeDotNetProject (fileName.ToNativePath ()) {
				Name = projectName
			};
		}

		void AddExistingFile (string fileName)
		{
			existingFiles.Add (fileName.ToNativePath ());
		}

		static DummyDotNetProject CreateDotNetCoreProject (string projectName = "MyProject", string fileName = @"d:\projects\MyProject\MyProject.csproj")
		{
			var project = new DummyDotNetProject ();
			project.Name = projectName;
			project.FileName = fileName.ToNativePath ();
			return project;
		}

		void AddParentSolution (DotNetProject dotNetProject)
		{
			var solution = new Solution ();
			solution.RootFolder.AddItem (dotNetProject);
		}

		[Test]
		public void GetPackagesConfigFilePath_ProjectPackagesConfigFileDoesNotExist_ReturnsDefaultPackagesConfigFile ()
		{
			CreateProject (@"d:\projects\MyProject.csproj", "MyProject");

			string fileName = project.GetPackagesConfigFilePath ();

			Assert.AreEqual (@"d:\projects\packages.config".ToNativePath (), fileName);
		}

		[Test]
		public void GetPackagesConfigFilePath_ProjectPackagesConfigFileExists_ReturnsPackagesConfigFileNamedAfterProject ()
		{
			CreateProject (@"d:\projects\MyProject.csproj", "MyProject");
			AddExistingFile (@"d:\projects\packages.MyProject.config");

			string fileName = project.GetPackagesConfigFilePath ();

			Assert.AreEqual (@"d:\projects\packages.MyProject.config".ToNativePath (), fileName);
		}

		[Test]
		public void GetPackagesConfigFilePath_ProjectNameHasSpaceProjectPackagesConfigFileExists_ReturnsPackagesConfigFileNamedAfterProject ()
		{
			CreateProject (@"d:\projects\MyProject.csproj", "My Project");
			AddExistingFile (@"d:\projects\packages.My_Project.config");

			string fileName = project.GetPackagesConfigFilePath ();

			Assert.AreEqual (@"d:\projects\packages.My_Project.config".ToNativePath (), fileName);
		}

		[Test]
		public void HasPackages_PackagesConfigFileDoesNotExist_ReturnsFalse ()
		{
			CreateProject (@"d:\projects\MyProject.csproj", "MyProject");

			bool result = project.HasPackages ();

			Assert.IsFalse (result);
		}

		[Test]
		public void HasPackages_PackagesConfigFileExistsInProjectDirectory_ReturnsTrue ()
		{
			CreateProject (@"d:\projects\MyProject.csproj", "MyProject");
			AddExistingFile (@"d:\projects\packages.config");

			bool result = project.HasPackages ();

			Assert.IsTrue (result);
		}

		[Test]
		public void HasPackages_ProjectPackagesConfigFileExistsInProjectDirectory_ReturnsTrue ()
		{
			CreateProject (@"d:\projects\MyProject.csproj", "MyProject");
			AddExistingFile (@"d:\projects\packages.MyProject.config");

			bool result = project.HasPackages ();

			Assert.IsTrue (result);
		}

		[Test]
		public void HasPackages_ProjectNameHasSpaceAndProjectPackagesConfigFileExistsInProjectDirectory_ReturnsTrue ()
		{
			CreateProject (@"d:\projects\MyProject.csproj", "My Project");
			AddExistingFile (@"d:\projects\packages.My_Project.config");

			bool result = project.HasPackages ();

			Assert.IsTrue (result);
		}

		[Test]
		public void HasPackagesConfig_PackagesConfigFileDoesNotExist_ReturnsFalse ()
		{
			CreateProject (@"d:\projects\MyProject.csproj", "MyProject");

			bool result = project.HasPackagesConfig ();

			Assert.IsFalse (result);
		}

		[Test]
		public void HasPackagesConfig_PackagesConfigFileExistsInProjectDirectory_ReturnsTrue ()
		{
			CreateProject (@"d:\projects\MyProject.csproj", "MyProject");
			AddExistingFile (@"d:\projects\packages.config");

			bool result = project.HasPackagesConfig ();

			Assert.IsTrue (result);
		}

		[Test]
		public void HasPackages_PackagesJsonFileExistsInProjectDirectory_ReturnsTrue ()
		{
			CreateProject (@"d:\projects\MyProject.csproj", "MyProject");
			AddExistingFile (@"d:\projects\project.json");

			bool result = project.HasPackages ();

			Assert.IsTrue (result);
		}

		[Test]
		public void DotNetCoreNotifyReferencesChanged_NoProjectReferencesAllProjects_NotifyReferencesChangedForProject ()
		{
			var dotNetProject = CreateDotNetCoreProject ();
			AddParentSolution (dotNetProject);
			string modifiedHint = null;
			dotNetProject.Modified += (sender, e) => {
				modifiedHint = e.First ().Hint;
			};

			dotNetProject.DotNetCoreNotifyReferencesChanged (transitiveOnly: false);

			Assert.AreEqual ("References", modifiedHint);
		}

		[Test]
		public void DotNetCoreNotifyReferencesChanged_NoProjectReferencesTransitiveProjectReferencesOnly_NotifyReferencesChangedNotFiredForProject ()
		{
			var dotNetProject = CreateDotNetCoreProject ();
			AddParentSolution (dotNetProject);
			string modifiedHint = null;
			dotNetProject.Modified += (sender, e) => {
				modifiedHint = e.First ().Hint;
			};

			dotNetProject.DotNetCoreNotifyReferencesChanged (transitiveOnly: true);

			Assert.IsNull (modifiedHint);
		}

		[Test]
		public void DotNetCoreNotifyReferencesChanged_OneProjectReferencesProject_NotifyReferencesChangedForAllProjects ()
		{
			var dotNetProject = CreateDotNetCoreProject ();
			AddParentSolution (dotNetProject);
			var referencingProject = CreateDotNetCoreProject ();
			dotNetProject.ParentSolution.RootFolder.AddItem (referencingProject);
			referencingProject.References.Add (ProjectReference.CreateProjectReference (dotNetProject));
			string modifiedHint = null;
			dotNetProject.Modified += (sender, e) => {
				modifiedHint = e.First ().Hint;
			};
			string modifiedHintForReferencingProject = null;
			referencingProject.Modified += (sender, e) => {
				modifiedHintForReferencingProject = e.First ().Hint;
			};

			dotNetProject.DotNetCoreNotifyReferencesChanged ();

			Assert.AreEqual ("References", modifiedHint);
			Assert.AreEqual ("References", modifiedHintForReferencingProject);
		}

		[Test]
		public void DotNetCoreNotifyReferencesChanged_OneProjectReferencesProjectWithReferencedOutputAssemblyFalse_NotifyReferencesChangedNotFiredForReferencingProject ()
		{
			var dotNetProject = CreateDotNetCoreProject ();
			AddParentSolution (dotNetProject);
			var referencingProject = CreateDotNetCoreProject ();
			dotNetProject.ParentSolution.RootFolder.AddItem (referencingProject);
			var projectReference = ProjectReference.CreateProjectReference (dotNetProject);
			projectReference.ReferenceOutputAssembly = false;
			referencingProject.References.Add (projectReference);
			string modifiedHintForReferencingProject = null;
			referencingProject.Modified += (sender, e) => {
				modifiedHintForReferencingProject = e.First ().Hint;
			};

			dotNetProject.DotNetCoreNotifyReferencesChanged (true);

			Assert.IsNull (modifiedHintForReferencingProject);
		}

		[Test]
		public void DotNetCoreNotifyReferencesChanged_TwoOneProjectReferencesModifiedProject_NotifyReferencesChangedForAllProjects ()
		{
			var dotNetProject = CreateDotNetCoreProject ();
			AddParentSolution (dotNetProject);
			var referencingProject1 = CreateDotNetCoreProject ();
			dotNetProject.ParentSolution.RootFolder.AddItem (referencingProject1);
			referencingProject1.References.Add (ProjectReference.CreateProjectReference (dotNetProject));
			var referencingProject2 = CreateDotNetCoreProject ();
			dotNetProject.ParentSolution.RootFolder.AddItem (referencingProject2);
			referencingProject2.References.Add (ProjectReference.CreateProjectReference (dotNetProject));
			string modifiedHint = null;
			dotNetProject.Modified += (sender, e) => {
				modifiedHint = e.First ().Hint;
			};
			string modifiedHintForReferencingProject1 = null;
			referencingProject1.Modified += (sender, e) => {
				modifiedHintForReferencingProject1 = e.First ().Hint;
			};
			string modifiedHintForReferencingProject2 = null;
			referencingProject2.Modified += (sender, e) => {
				modifiedHintForReferencingProject2 = e.First ().Hint;
			};

			dotNetProject.DotNetCoreNotifyReferencesChanged ();

			Assert.AreEqual ("References", modifiedHint);
			Assert.AreEqual ("References", modifiedHintForReferencingProject1);
			Assert.AreEqual ("References", modifiedHintForReferencingProject2);
		}

		[Test]
		public void DotNetCoreNotifyReferencesChanged_TwoProjectReferencesChainToModifiedProject_NotifyReferencesChangedForAllProjects ()
		{
			var dotNetProject = CreateDotNetCoreProject ();
			AddParentSolution (dotNetProject);
			var referencingProject1 = CreateDotNetCoreProject ();
			dotNetProject.ParentSolution.RootFolder.AddItem (referencingProject1);
			referencingProject1.References.Add (ProjectReference.CreateProjectReference (dotNetProject));
			var referencingProject2 = CreateDotNetCoreProject ();
			dotNetProject.ParentSolution.RootFolder.AddItem (referencingProject2);
			referencingProject2.References.Add (ProjectReference.CreateProjectReference (referencingProject1));
			string modifiedHint = null;
			dotNetProject.Modified += (sender, e) => {
				modifiedHint = e.First ().Hint;
			};
			string modifiedHintForReferencingProject1 = null;
			referencingProject1.Modified += (sender, e) => {
				modifiedHintForReferencingProject1 = e.First ().Hint;
			};
			string modifiedHintForReferencingProject2 = null;
			referencingProject2.Modified += (sender, e) => {
				modifiedHintForReferencingProject2 = e.First ().Hint;
			};

			dotNetProject.DotNetCoreNotifyReferencesChanged ();

			Assert.AreEqual ("References", modifiedHint);
			Assert.AreEqual ("References", modifiedHintForReferencingProject1);
			Assert.AreEqual ("References", modifiedHintForReferencingProject2);
		}

		/// <summary>
		/// Same as above but the projects are added to the solution in a different order.
		/// </summary>
		[Test]
		public void DotNetCoreNotifyReferencesChanged_TwoProjectReferencesChainToModifiedProject_NotifyReferencesChangedForAllProjects2 ()
		{
			var dotNetProject = CreateDotNetCoreProject ();
			AddParentSolution (dotNetProject);
			var referencingProject1 = CreateDotNetCoreProject ();
			var referencingProject2 = CreateDotNetCoreProject ();
			dotNetProject.ParentSolution.RootFolder.AddItem (referencingProject2);
			dotNetProject.ParentSolution.RootFolder.AddItem (referencingProject1);
			referencingProject1.References.Add (ProjectReference.CreateProjectReference (dotNetProject));
			referencingProject2.References.Add (ProjectReference.CreateProjectReference (referencingProject1));
			string modifiedHint = null;
			dotNetProject.Modified += (sender, e) => {
				modifiedHint = e.First ().Hint;
			};
			string modifiedHintForReferencingProject1 = null;
			referencingProject1.Modified += (sender, e) => {
				modifiedHintForReferencingProject1 = e.First ().Hint;
			};
			string modifiedHintForReferencingProject2 = null;
			referencingProject2.Modified += (sender, e) => {
				modifiedHintForReferencingProject2 = e.First ().Hint;
			};

			dotNetProject.DotNetCoreNotifyReferencesChanged ();

			Assert.AreEqual ("References", modifiedHint);
			Assert.AreEqual ("References", modifiedHintForReferencingProject1);
			Assert.AreEqual ("References", modifiedHintForReferencingProject2);
		}

		[Test]
		public void GetReferencingProjects_ThreeProjectsOneProjectReferencesModifiedProject_OneProjectReturned ()
		{
			var dotNetProject = CreateDotNetCoreProject ();
			AddParentSolution (dotNetProject);
			var referencedProject = CreateDotNetCoreProject ();
			dotNetProject.ParentSolution.RootFolder.AddItem (referencedProject);
			referencedProject.References.Add (ProjectReference.CreateProjectReference (dotNetProject));
			var otherProject = CreateDotNetCoreProject ();
			dotNetProject.ParentSolution.RootFolder.AddItem (otherProject);

			var projects = dotNetProject.GetReferencingProjects ().ToList ();

			Assert.AreEqual (1, projects.Count);
			Assert.AreEqual (projects[0], referencedProject);
		}

		[Test]
		public void GetReferencingProjects_TwoProjectReferencesChainToModifiedProject_NotifyReferencesChangedForAllProjects ()
		{
			var dotNetProject = CreateDotNetCoreProject ();
			AddParentSolution (dotNetProject);
			var referencingProject1 = CreateDotNetCoreProject ();
			dotNetProject.ParentSolution.RootFolder.AddItem (referencingProject1);
			referencingProject1.References.Add (ProjectReference.CreateProjectReference (dotNetProject));
			var referencingProject2 = CreateDotNetCoreProject ();
			dotNetProject.ParentSolution.RootFolder.AddItem (referencingProject2);
			referencingProject2.References.Add (ProjectReference.CreateProjectReference (referencingProject1));
			var otherProject = CreateDotNetCoreProject ();
			dotNetProject.ParentSolution.RootFolder.AddItem (otherProject);

			var projects = dotNetProject.GetReferencingProjects ().ToList ();

			Assert.AreEqual (2, projects.Count);
			Assert.That (projects, Contains.Item (referencingProject1));
			Assert.That (projects, Contains.Item (referencingProject2));
		}
	}
}

