//
// ProjectTargetFrameworkMonitorTests.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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

using System;
using System.Collections.Generic;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.PackageManagement.Tests.Helpers;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class ProjectTargetFrameworkMonitorTests
	{
		ProjectTargetFrameworkMonitor monitor;
		FakePackageManagementProjectService projectService;
		List<ProjectTargetFrameworkChangedEventArgs> eventArgs;
		FakeSolution solution;
		int solutionCount;
		const string targetFrameworkPropertyName = "TargetFramework";

		void CreateProjectTargetFrameworkMonitor ()
		{
			eventArgs = new List<ProjectTargetFrameworkChangedEventArgs> ();
			projectService = new FakePackageManagementProjectService ();
			monitor = new ProjectTargetFrameworkMonitor (projectService);
		}

		FakeDotNetProject LoadSolutionWithOneProject ()
		{
			solutionCount++;
			string fileName = String.Format (@"d:\projects\MySolution\MySolution{0}.sln", solutionCount);
			solution = new FakeSolution (fileName);
			projectService.OpenSolution = solution;
			var project = new FakeDotNetProject ();
			projectService.OpenProjects.Add (project);
			solution.Projects.Add (project);
			projectService.RaiseSolutionLoadedEvent (solution);

			return project;
		}

		void CaptureProjectTargetFrameworkChangedEvents ()
		{
			monitor.ProjectTargetFrameworkChanged += (sender, e) => {
				eventArgs.Add (e);
			};
		}

		void UnloadSolution ()
		{
			UnloadSolution (solution);
		}

		void UnloadSolution (FakeSolution solutionToUnload)
		{
			projectService.RaiseSolutionUnloadedEvent (solutionToUnload);
		}

		FakeDotNetProject AddNewProjectToSolution ()
		{
			var project = new FakeDotNetProject ();
			solution.Projects.Add (project);
			solution.RaiseProjectAddedEvent (project);

			return project;
		}

		FakeDotNetProject CreateProjectWithTargetFramework (string targetFramework)
		{
			var project = new FakeDotNetProject ();
			project.TargetFrameworkMoniker = TargetFrameworkMoniker.Parse (targetFramework);
			return project;
		}

		void RaiseProjectReloadedEvent (FakeDotNetProject oldProject, FakeDotNetProject newProject)
		{
			projectService.RaiseProjectReloadedEvent (oldProject, newProject);
		}

		[Test]
		public void ProjectTargetFrameworkChanged_ProjectTargetFrameworkChanged_EventFiresAfterProjectIsSaved ()
		{
			CreateProjectTargetFrameworkMonitor ();
			FakeDotNetProject project = LoadSolutionWithOneProject ();
			CaptureProjectTargetFrameworkChangedEvents ();

			project.RaiseModifiedEvent (project, targetFrameworkPropertyName);
			int eventArgsCountBeforeSave = eventArgs.Count;
			project.RaiseSavedEvent ();

			Assert.AreEqual (1, eventArgs.Count);
			Assert.AreEqual (project, eventArgs [0].Project);
			Assert.IsFalse (eventArgs [0].IsReload);
			Assert.AreEqual (0, eventArgsCountBeforeSave);
		}

		[Test]
		public void ProjectTargetFrameworkChanged_ProjectTargetFrameworkChangedButNobodyListeningForEvents_NullReferenceIsNotThrown ()
		{
			CreateProjectTargetFrameworkMonitor ();
			FakeDotNetProject project = LoadSolutionWithOneProject ();

			Assert.DoesNotThrow (() => {
				project.RaiseModifiedEvent (project, targetFrameworkPropertyName);
			});
		}

		[Test]
		public void ProjectTargetFrameworkChanged_OtherProjectPropertyChangedNotProjectTargetFrameworkProperty_EventDoesNotFire ()
		{
			CreateProjectTargetFrameworkMonitor ();
			FakeDotNetProject project = LoadSolutionWithOneProject ();
			CaptureProjectTargetFrameworkChangedEvents ();

			project.RaiseModifiedEvent (project, "SomeOtherProperty");
			project.RaiseSavedEvent ();

			Assert.AreEqual (0, eventArgs.Count);
		}

		[Test]
		public void ProjectTargetFrameworkChanged_ProjectTargetFrameworkChangedButPropertyNameIsInDifferentCase_EventFires ()
		{
			CreateProjectTargetFrameworkMonitor ();
			FakeDotNetProject project = LoadSolutionWithOneProject ();
			CaptureProjectTargetFrameworkChangedEvents ();

			project.RaiseModifiedEvent (project, targetFrameworkPropertyName.ToUpperInvariant ());
			project.RaiseSavedEvent ();

			Assert.AreEqual (1, eventArgs.Count);
			Assert.AreEqual (project, eventArgs [0].Project);
			Assert.IsFalse (eventArgs [0].IsReload);
		}

		[Test]
		public void ProjectTargetFrameworkChanged_ProjectTargetFrameworkChangedAfterSolutionUnloaded_EventDoesNotFire ()
		{
			CreateProjectTargetFrameworkMonitor ();
			FakeDotNetProject project = LoadSolutionWithOneProject ();
			CaptureProjectTargetFrameworkChangedEvents ();
			UnloadSolution ();

			project.RaiseModifiedEvent (project, targetFrameworkPropertyName);
			project.RaiseSavedEvent ();

			Assert.AreEqual (0, eventArgs.Count);
		}

		[Test]
		public void ProjectTargetFrameworkChanged_NewProjectAddedToSolutionAndProjectTargetFrameworkChanged_EventFires ()
		{
			CreateProjectTargetFrameworkMonitor ();
			LoadSolutionWithOneProject ();
			CaptureProjectTargetFrameworkChangedEvents ();
			FakeDotNetProject project = AddNewProjectToSolution ();

			project.RaiseModifiedEvent (project, targetFrameworkPropertyName);
			project.RaiseSavedEvent ();

			Assert.AreEqual (1, eventArgs.Count);
			Assert.AreEqual (project, eventArgs [0].Project);
			Assert.IsFalse (eventArgs [0].IsReload);
		}

		[Test]
		public void ProjectTargetFrameworkChanged_ProjectTargetFrameworkChangedForNewlyAddedProjectAfterSolutionUnloaded_EventDoesNotFire ()
		{
			CreateProjectTargetFrameworkMonitor ();
			LoadSolutionWithOneProject ();
			CaptureProjectTargetFrameworkChangedEvents ();
			FakeDotNetProject project = AddNewProjectToSolution ();
			UnloadSolution ();

			project.RaiseModifiedEvent (project, targetFrameworkPropertyName);
			project.RaiseSavedEvent ();

			Assert.AreEqual (0, eventArgs.Count);
		}

		[Test]
		public void ProjectTargetFrameworkChanged_NewProjectAddedToSolutionAndProjectTargetFrameworkChangedAfterSolutionUnloaded_EventDoesNotFire ()
		{
			CreateProjectTargetFrameworkMonitor ();
			LoadSolutionWithOneProject ();
			CaptureProjectTargetFrameworkChangedEvents ();
			UnloadSolution ();
			FakeDotNetProject project = AddNewProjectToSolution ();

			project.RaiseModifiedEvent (project, targetFrameworkPropertyName);
			project.RaiseSavedEvent ();

			Assert.AreEqual (0, eventArgs.Count);
		}

		[Test]
		public void ProjectReloaded_TargetFrameworkChanged_EventFires ()
		{
			CreateProjectTargetFrameworkMonitor ();
			FakeDotNetProject project = LoadSolutionWithOneProject ();
			FakeDotNetProject reloadedProject = CreateProjectWithTargetFramework (".NETFramework,Version=v2.0");
			CaptureProjectTargetFrameworkChangedEvents ();

			RaiseProjectReloadedEvent (project, reloadedProject);

			Assert.AreEqual (1, eventArgs.Count);
			Assert.AreEqual (reloadedProject, eventArgs [0].Project);
			Assert.IsTrue (eventArgs [0].IsReload);
		}

		[Test]
		public void ProjectReloaded_TargetFrameworkNotChanged_EventDoesNotFire ()
		{
			CreateProjectTargetFrameworkMonitor ();
			FakeDotNetProject project = LoadSolutionWithOneProject ();
			var reloadedProject = new FakeDotNetProject ();
			reloadedProject.TargetFrameworkMoniker = project.TargetFrameworkMoniker;
			CaptureProjectTargetFrameworkChangedEvents ();

			RaiseProjectReloadedEvent (project, reloadedProject);

			Assert.AreEqual (0, eventArgs.Count);
		}

		[Test]
		public void ProjectReloaded_TargetFrameworkNullInReloadedProject_EventDoesNotFire ()
		{
			CreateProjectTargetFrameworkMonitor ();
			FakeDotNetProject project = LoadSolutionWithOneProject ();
			var reloadedProject = new FakeDotNetProject ();
			reloadedProject.TargetFrameworkMoniker = null;
			CaptureProjectTargetFrameworkChangedEvents ();

			RaiseProjectReloadedEvent (project, reloadedProject);

			Assert.AreEqual (0, eventArgs.Count);
		}

		[Test]
		public void SolutionUnloaded_TwoSolutionsLoadedInWorkspaceAndBothSolutionsUnloaded_NullReferenceExceptionIsNotThrown ()
		{
			CreateProjectTargetFrameworkMonitor ();
			LoadSolutionWithOneProject ();
			projectService.OpenProjects.Clear ();
			LoadSolutionWithOneProject ();
			UnloadSolution ();

			Assert.DoesNotThrow (UnloadSolution);
		}

		[Test]
		public void ProjectModified_TwoSolutionsLoadedProjectTargetFrameworkChangedInFirstAndSecondSolution_EventFiresForBothProjects ()
		{
			CreateProjectTargetFrameworkMonitor ();
			FakeDotNetProject firstProject = LoadSolutionWithOneProject ();
			projectService.OpenProjects.Clear ();
			FakeDotNetProject secondProject = LoadSolutionWithOneProject ();
			CaptureProjectTargetFrameworkChangedEvents ();

			firstProject.RaiseModifiedEvent (firstProject, targetFrameworkPropertyName);
			secondProject.RaiseModifiedEvent (secondProject, targetFrameworkPropertyName);
			firstProject.RaiseSavedEvent ();
			secondProject.RaiseSavedEvent ();

			Assert.AreEqual (2, eventArgs.Count);
			Assert.AreEqual (firstProject, eventArgs [0].Project);
			Assert.AreEqual (secondProject, eventArgs [1].Project);
			Assert.IsFalse (eventArgs [0].IsReload);
			Assert.IsFalse (eventArgs [1].IsReload);
		}

		[Test]
		public void ProjectModified_TwoSolutionsLoadedSecondSolutionUnloadedProjectTargetFrameworkChangedInFirstAndSecondSolution_EventFiresForProjectInOpenSolutionOnly ()
		{
			CreateProjectTargetFrameworkMonitor ();
			FakeDotNetProject firstProject = LoadSolutionWithOneProject ();
			projectService.OpenProjects.Clear ();
			FakeDotNetProject secondProject = LoadSolutionWithOneProject ();
			CaptureProjectTargetFrameworkChangedEvents ();
			UnloadSolution ();

			firstProject.RaiseModifiedEvent (firstProject, targetFrameworkPropertyName);
			secondProject.RaiseModifiedEvent (secondProject, targetFrameworkPropertyName);
			firstProject.RaiseSavedEvent ();
			secondProject.RaiseSavedEvent ();

			Assert.AreEqual (1, eventArgs.Count);
			Assert.AreEqual (firstProject, eventArgs [0].Project);
			Assert.IsFalse (eventArgs [0].IsReload);
		}

		[Test]
		public void ProjectModified_TwoSolutionsLoadedFirstSolutionUnloadedProjectTargetFrameworkChangedInFirstAndSecondSolution_EventFiresForProjectInOpenSolutionOnly ()
		{
			CreateProjectTargetFrameworkMonitor ();
			FakeDotNetProject firstProject = LoadSolutionWithOneProject ();
			FakeSolution firstSolution = new FakeSolution (solution.FileName);
			projectService.OpenProjects.Clear ();
			FakeDotNetProject secondProject = LoadSolutionWithOneProject ();
			CaptureProjectTargetFrameworkChangedEvents ();
			UnloadSolution (firstSolution);

			firstProject.RaiseModifiedEvent (firstProject, targetFrameworkPropertyName);
			secondProject.RaiseModifiedEvent (secondProject, targetFrameworkPropertyName);
			firstProject.RaiseSavedEvent ();
			secondProject.RaiseSavedEvent ();

			Assert.AreEqual (1, eventArgs.Count);
			Assert.AreEqual (secondProject, eventArgs [0].Project);
			Assert.IsFalse (eventArgs [0].IsReload);
		}

		[Test]
		public void ProjectTargetFrameworkChanged_ProjectRemovedFromSolutionAndProjectTargetFrameworkChanged_EventDoesNotFire ()
		{
			CreateProjectTargetFrameworkMonitor ();
			FakeDotNetProject originalProject = LoadSolutionWithOneProject ();
			CaptureProjectTargetFrameworkChangedEvents ();
			// Ensure IDotNetProject.Equals method is used since a new DotNetProjectProxy is
			// created for the event so the object instances will be different and just removing
			// the instance from the list matching the instance is incorrect. 
			var project = new FakeDotNetProject ();
			originalProject.EqualsAction = p => p == project;
			solution.RaiseProjectRemovedEvent (project);

			originalProject.RaiseModifiedEvent (originalProject, targetFrameworkPropertyName);
			originalProject.RaiseSavedEvent ();

			Assert.AreEqual (0, eventArgs.Count);
		}

		[Test]
		public void ProjectTargetFrameworkChanged_ProjectNotSaved_EventDoesNotFire ()
		{
			CreateProjectTargetFrameworkMonitor ();
			FakeDotNetProject project = LoadSolutionWithOneProject ();
			CaptureProjectTargetFrameworkChangedEvents ();

			project.RaiseModifiedEvent (project, targetFrameworkPropertyName);

			Assert.AreEqual (0, eventArgs.Count);
		}

		[Test]
		public void ProjectTargetFrameworkChanged_ProjectTargetFrameworkChangedAndProjectSavedTwiceAfterwards_EventFiresOnce ()
		{
			CreateProjectTargetFrameworkMonitor ();
			FakeDotNetProject project = LoadSolutionWithOneProject ();
			CaptureProjectTargetFrameworkChangedEvents ();

			project.RaiseModifiedEvent (project, targetFrameworkPropertyName);
			project.RaiseSavedEvent ();
			project.RaiseSavedEvent ();

			Assert.AreEqual (1, eventArgs.Count);
			Assert.AreEqual (project, eventArgs [0].Project);
			Assert.IsFalse (eventArgs [0].IsReload);
		}

		/// <summary>
		/// Ensures the ProjectSaved event handler is removed when the solution is unloaded.
		/// </summary>
		/// <returns>The target framework changed project target framework changed solution unloaded then project saved event does not fire.</returns>
		[Test]
		public void ProjectTargetFrameworkChanged_ProjectTargetFrameworkChangedSolutionUnloadedThenProjectSaved_EventDoesNotFire ()
		{
			CreateProjectTargetFrameworkMonitor ();
			FakeDotNetProject project = LoadSolutionWithOneProject ();
			CaptureProjectTargetFrameworkChangedEvents ();
			project.RaiseModifiedEvent (project, targetFrameworkPropertyName);
			UnloadSolution ();

			project.RaiseSavedEvent ();

			Assert.AreEqual (0, eventArgs.Count);
		}
	}
}

