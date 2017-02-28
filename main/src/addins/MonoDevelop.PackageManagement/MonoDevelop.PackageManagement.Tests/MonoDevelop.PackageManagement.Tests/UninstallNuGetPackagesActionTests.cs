//
// UninstallNuGetPackagesActionTests.cs
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

using System.Linq;
using MonoDevelop.PackageManagement.Tests.Helpers;
using NuGet.PackageManagement;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class UninstallNuGetPackagesActionTests
	{
		TestableUninstallNuGetPackagesAction action;
		FakeSolutionManager solutionManager;
		FakeDotNetProject project;
		FakeNuGetProject nugetProject;
		FakeNuGetPackageManager packageManager;

		void CreateAction (params string[] packageIds)
		{
			project = new FakeDotNetProject (@"d:\projects\MyProject\MyProject.csproj");
			project.Name = "MyProject";
			solutionManager = new FakeSolutionManager ();
			nugetProject = new FakeNuGetProject (project);
			solutionManager.NuGetProjects[project] = nugetProject;

			action = new TestableUninstallNuGetPackagesAction (
				solutionManager,
				project);

			packageManager = action.PackageManager;

			action.AddPackageIds (packageIds);
		}

		[Test]
		public void Execute_TwoPackageIds_ActionsResolvedFromNuGetPackageManager ()
		{
			CreateAction ("Test1", "Test2");

			action.Execute ();

			Assert.AreEqual (nugetProject, packageManager.PreviewBuildIntegratedProject);
			Assert.AreEqual ("Test1", packageManager.PreviewBuildIntegratedProjectActions[0].PackageIdentity.Id);
			Assert.AreEqual ("Test2", packageManager.PreviewBuildIntegratedProjectActions[1].PackageIdentity.Id);
			Assert.AreEqual (NuGetProjectActionType.Uninstall, packageManager.PreviewBuildIntegratedProjectActions[0].NuGetProjectActionType);
			Assert.AreEqual (NuGetProjectActionType.Uninstall, packageManager.PreviewBuildIntegratedProjectActions[1].NuGetProjectActionType);
			Assert.AreEqual (action.ProjectContext, packageManager.PreviewBuildIntegratedContext);
		}

		[Test]
		public void Execute_TwoPackageIds_ActionsAvailableForInstrumentation ()
		{
			CreateAction ("Test1", "Test2");

			action.Execute ();

			Assert.AreEqual (action.GetNuGetProjectActions (), packageManager.PreviewBuildIntegratedProjectActions);
		}

		[Test]
		public void NewInstance_NotExecutedAndGetNuGetProjectActions_NullReferenceExceptionNotThrown ()
		{
			CreateAction ("Test1", "Test2");

			Assert.AreEqual (0, action.GetNuGetProjectActions ().Count ());
		}

		[Test]
		public void Execute_TwoPackageIds_ExecutesBuilldIntegratedActionReturnedFromNuGetPackageManager ()
		{
			CreateAction ("Test1", "Test2");

			action.Execute ();

			Assert.AreEqual (packageManager.BuildIntegratedProjectAction, packageManager.ExecutedActions[0]);
			Assert.AreEqual (1, packageManager.ExecutedActions.Count);
			Assert.AreEqual (nugetProject, packageManager.ExecutedNuGetProject);
			Assert.AreEqual (action.ProjectContext, packageManager.ExecutedProjectContext);
		}

		[Test]
		public void Execute_TwoPackageIds_OnAfterExecuteActionsIsCalled ()
		{
			CreateAction ("Test1", "Test2");

			action.Execute ();

			Assert.AreEqual (packageManager.PreviewBuildIntegratedProjectActions, nugetProject.ActionsPassedToOnAfterExecuteActions);
		}

		[Test]
		public void Execute_TwoPackageIds_PostProcessingIsRun ()
		{
			CreateAction ("Test1", "Test2");

			action.Execute ();

			Assert.AreEqual (action.ProjectContext, nugetProject.PostProcessProjectContext);
		}

		[Test]
		public void Execute_TwoPackageIds_OnBeforeUninstallIsCalled ()
		{
			CreateAction ("Test1", "Test2");

			action.Execute ();

			Assert.AreEqual (packageManager.PreviewBuildIntegratedProjectActions, nugetProject.ActionsPassedToOnBeforeUninstall);
		}
	}
}
