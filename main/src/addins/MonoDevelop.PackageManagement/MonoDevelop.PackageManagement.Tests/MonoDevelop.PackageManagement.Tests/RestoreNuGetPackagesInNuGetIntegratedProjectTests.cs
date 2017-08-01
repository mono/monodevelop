//
// RestoreNuGetPackagesInNuGetIntegratedProjectTests.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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

using MonoDevelop.PackageManagement.Tests.Helpers;
using MonoDevelop.Projects;
using NUnit.Framework;
namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	class RestoreNuGetPackagesInNuGetIntegratedProjectTests
	{
		RestoreNuGetPackagesInNuGetIntegratedProject action;
		FakeSolutionManager solutionManager;
		FakeSolution fakeSolution;
		DotNetProject dotNetProject;
		FakeDotNetProject fakeDotNetProject;
		TestableDotNetCoreNuGetProject nugetProject;
		FakeMonoDevelopBuildIntegratedRestorer buildIntegratedRestorer;

		void CreateProject ()
		{
			solutionManager = new FakeSolutionManager ();
			fakeSolution = new FakeSolution ();
			CreateNuGetProject ();
			fakeDotNetProject = new FakeDotNetProject (dotNetProject.FileName);
			fakeDotNetProject.ParentSolution = fakeSolution;
			fakeDotNetProject.DotNetProject = dotNetProject;
			fakeSolution.Projects.Add (fakeDotNetProject);
		}

		void CreateAction (bool restoreTransitiveProjectReferences = false)
		{
			action = new RestoreNuGetPackagesInNuGetIntegratedProject (
				fakeDotNetProject,
				nugetProject,
				solutionManager,
				buildIntegratedRestorer,
				restoreTransitiveProjectReferences);
		}

		void CreateNuGetProject (string projectName = "MyProject", string fileName = @"d:\projects\MyProject\MyProject.csproj")
		{
			dotNetProject = CreateDotNetCoreProject (projectName, fileName);
			var solution = new Solution ();
			solution.RootFolder.AddItem (dotNetProject);
			nugetProject = new TestableDotNetCoreNuGetProject (dotNetProject);
			buildIntegratedRestorer = nugetProject.BuildIntegratedRestorer;
		}

		TestableDotNetCoreNuGetProject CreateNuGetProject (DotNetProject project)
		{
			var dotNetProjectProxy = new FakeDotNetProject ();
			dotNetProjectProxy.DotNetProject = project;
			fakeSolution.Projects.Add (dotNetProjectProxy);

			var dotNetCoreNuGetProject = new TestableDotNetCoreNuGetProject (project);
			dotNetCoreNuGetProject.BuildIntegratedRestorer = null;

			solutionManager.NuGetProjectsUsingDotNetProjects.Add (project, dotNetCoreNuGetProject);

			return dotNetCoreNuGetProject;
		}

		static DummyDotNetProject CreateDotNetCoreProject (string projectName = "MyProject", string fileName = @"d:\projects\MyProject\MyProject.csproj")
		{
			var project = new DummyDotNetProject ();
			project.Name = projectName;
			project.FileName = fileName.ToNativePath ();
			return project;
		}

		[Test]
		public void Execute_BuildIntegratedRestorer_PackagesRestoredForProject ()
		{
			CreateProject ();
			CreateAction ();

			action.Execute ();

			Assert.AreEqual (nugetProject, buildIntegratedRestorer.ProjectRestored);
		}

		[Test]
		public void Execute_Events_PackagesRestoredEventFired ()
		{
			CreateProject ();
			CreateAction ();
			bool packagesRestoredEventFired = false;
			PackageManagementServices.PackageManagementEvents.PackagesRestored += (sender, e) => {
				packagesRestoredEventFired = true;
			};

			action.Execute ();

			Assert.IsTrue (packagesRestoredEventFired);
		}

		[Test]
		public void Execute_ReferenceStatus_IsRefreshed ()
		{
			CreateProject ();
			CreateAction ();

			action.Execute ();

			Assert.IsTrue (fakeDotNetProject.IsReferenceStatusRefreshed);
		}

		[Test]
		public void IncludeTransitiveProjectReferences_ThreeProjectsOneReferencedAnother_TwoProjectsRestored ()
		{
			CreateProject ();
			var referencingProject = CreateDotNetCoreProject ();
			dotNetProject.ParentSolution.RootFolder.AddItem (referencingProject);
			referencingProject.References.Add (ProjectReference.CreateProjectReference (dotNetProject));
			var otherProject = CreateDotNetCoreProject ();
			dotNetProject.ParentSolution.RootFolder.AddItem (otherProject);
			var referencingNuGetProject = CreateNuGetProject (referencingProject);
			CreateAction (true);

			action.Execute ();

			Assert.AreEqual (2, buildIntegratedRestorer.ProjectsRestored.Count);
			Assert.AreEqual (buildIntegratedRestorer.ProjectsRestored[0], nugetProject);
			Assert.AreEqual (buildIntegratedRestorer.ProjectsRestored[1], referencingNuGetProject);
		}
	}
}
