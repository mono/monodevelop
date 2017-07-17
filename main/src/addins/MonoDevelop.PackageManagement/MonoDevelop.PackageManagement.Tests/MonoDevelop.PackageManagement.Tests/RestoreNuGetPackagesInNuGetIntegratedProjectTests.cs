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
		DotNetProject dotNetProject;
		FakeDotNetProject fakeDotNetProject;
		TestableDotNetCoreNuGetProject nugetProject;
		FakeMonoDevelopBuildIntegratedRestorer buildIntegratedRestorer;

		void CreateAction ()
		{
			var solutionManager = new FakeSolutionManager ();
			CreateNuGetProject ();
			fakeDotNetProject = new FakeDotNetProject (dotNetProject.FileName);
			fakeDotNetProject.DotNetProject = dotNetProject;

			action = new RestoreNuGetPackagesInNuGetIntegratedProject (
				fakeDotNetProject,
				nugetProject,
				solutionManager,
				buildIntegratedRestorer);
		}

		void CreateNuGetProject (string projectName = "MyProject", string fileName = @"d:\projects\MyProject\MyProject.csproj")
		{
			var context = new FakeNuGetProjectContext ();
			dotNetProject = CreateDotNetCoreProject (projectName, fileName);
			var solution = new Solution ();
			solution.RootFolder.AddItem (dotNetProject);
			nugetProject = new TestableDotNetCoreNuGetProject (dotNetProject);
			buildIntegratedRestorer = nugetProject.BuildIntegratedRestorer;
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
			CreateAction ();

			action.Execute ();

			Assert.AreEqual (nugetProject, buildIntegratedRestorer.ProjectRestored);
		}

		[Test]
		public void Execute_Events_PackagesRestoredEventFired ()
		{
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
			CreateAction ();

			action.Execute ();

			Assert.IsTrue (fakeDotNetProject.IsReferenceStatusRefreshed);
		}
	}
}
