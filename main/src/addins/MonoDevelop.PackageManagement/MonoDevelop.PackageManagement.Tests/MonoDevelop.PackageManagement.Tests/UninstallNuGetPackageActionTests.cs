//
// UninstallNuGetPackageActionTests.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using NuGet.PackageManagement;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class UninstallNuGetPackageActionTests
	{
		TestableUninstallNuGetPackageAction action;
		FakeSolutionManager solutionManager;
		FakeDotNetProject project;
		FakeNuGetProject nugetProject;
		FakeNuGetPackageManager packageManager;

		void CreateAction (string packageId = "Test")
		{
			project = new FakeDotNetProject (@"d:\projects\MyProject\MyProject.csproj");
			solutionManager = new FakeSolutionManager ();
			nugetProject = new FakeNuGetProject (project);
			solutionManager.NuGetProjects[project] = nugetProject;

			action = new TestableUninstallNuGetPackageAction (
				solutionManager,
				project);

			packageManager = action.PackageManager;

			action.PackageId = packageId;
		}

		void AddUninstallPackageIntoProjectAction (string packageId, string version)
		{
			var projectAction = new FakeNuGetProjectAction (packageId, version, NuGetProjectActionType.Uninstall);
			packageManager.UninstallActions.Add (projectAction);
		}

		[Test]
		public void Execute_PackageIdIsSet_ActionsResolvedFromNuGetPackageManager ()
		{
			CreateAction ("Test");

			action.Execute ();

			Assert.AreEqual (nugetProject, packageManager.PreviewUninstallProject);
			Assert.AreEqual ("Test", packageManager.PreviewUninstallPackageId);
			Assert.AreEqual (action.ProjectContext, packageManager.PreviewUninstallProjectContext);
			Assert.IsFalse (packageManager.PreviewUninstallContext.ForceRemove);
			Assert.IsFalse (packageManager.PreviewUninstallContext.RemoveDependencies);
		}

		[Test]
		public void Execute_PackageIdAndVersionIsSet_ActionsAvailableForInstrumentation ()
		{
			CreateAction ();
			AddUninstallPackageIntoProjectAction ("Test", "1.2");

			action.Execute ();

			Assert.AreEqual (action.GetNuGetProjectActions(), packageManager.UninstallActions);
		}

		[Test]
		public void Execute_PackageIdAndVersionIsSet_InstallsPackageUsingResolvedActions ()
		{
			CreateAction ("Test");
			AddUninstallPackageIntoProjectAction ("Test", "1.2");

			action.Execute ();

			Assert.AreEqual (packageManager.UninstallActions, packageManager.ExecutedActions);
			Assert.AreEqual (nugetProject, packageManager.ExecutedNuGetProject);
			Assert.AreEqual (action.ProjectContext, packageManager.ExecutedProjectContext);
		}

		[Test]
		public void Execute_ForceRemoveIsTrue_PackageIsForcefullyRemoved ()
		{
			CreateAction ("Test");
			action.ForceRemove = true;

			action.Execute ();

			Assert.IsTrue (packageManager.PreviewUninstallContext.ForceRemove);
		}

		[Test]
		public void ForceRemove_NewInstance_IsFalseByDefault ()
		{
			CreateAction ("Test");

			Assert.IsFalse (action.ForceRemove);
		}

		[Test]
		public void Execute_NuGetProjectIsBuildIntegratedProject_OnAfterExecuteActionsIsCalled ()
		{
			CreateAction ("Test");
			AddUninstallPackageIntoProjectAction ("Test", "1.2");

			action.Execute ();

			Assert.AreEqual (packageManager.UninstallActions, nugetProject.ActionsPassedToOnAfterExecuteActions);
		}

		[Test]
		public void Execute_NuGetProjectIsBuildIntegratedProject_PostProcessingIsRun ()
		{
			CreateAction ("Test");

			action.Execute ();

			Assert.AreEqual (action.ProjectContext, nugetProject.PostProcessProjectContext);
		}

		[Test]
		public void Execute_NuGetProjectIsBuildIntegratedProject_OnBeforeUninstallIsCalled ()
		{
			CreateAction ("Test");
			AddUninstallPackageIntoProjectAction ("Test", "1.2");

			action.Execute ();

			Assert.AreEqual (packageManager.UninstallActions, nugetProject.ActionsPassedToOnBeforeUninstall);
		}
	}
}

