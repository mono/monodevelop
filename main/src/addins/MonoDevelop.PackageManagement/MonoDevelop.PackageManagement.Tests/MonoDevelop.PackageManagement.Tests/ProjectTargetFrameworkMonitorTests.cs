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
		const string targetFrameworkPropertyName = "TargetFramework";

		void CreateProjectTargetFrameworkMonitor ()
		{
			eventArgs = new List<ProjectTargetFrameworkChangedEventArgs> ();
			projectService = new FakePackageManagementProjectService ();
			monitor = new ProjectTargetFrameworkMonitor (projectService);
		}

		FakeDotNetProject LoadSolutionWithOneProject ()
		{
			solution = new FakeSolution ();
			projectService.OpenSolution = solution;
			var project = new FakeDotNetProject ();
			projectService.OpenProjects.Add (project);
			solution.Projects.Add (project);
			projectService.RaiseSolutionLoadedEvent ();

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
			projectService.RaiseSolutionUnloadedEvent ();
		}

		FakeDotNetProject AddNewProjectToSolution ()
		{
			var project = new FakeDotNetProject ();
			solution.Projects.Add (project);
			solution.RaiseProjectAddedEvent (project);

			return project;
		}

		[Test]
		public void ProjectTargetFrameworkChanged_ProjectTargetFrameworkChanged_EventFires ()
		{
			CreateProjectTargetFrameworkMonitor ();
			FakeDotNetProject project = LoadSolutionWithOneProject ();
			CaptureProjectTargetFrameworkChangedEvents ();

			project.RaiseModifiedEvent (project, targetFrameworkPropertyName);

			Assert.AreEqual (1, eventArgs.Count);
			Assert.AreEqual (project, eventArgs [0].Project);
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

			Assert.AreEqual (0, eventArgs.Count);
		}

		[Test]
		public void ProjectTargetFrameworkChanged_ProjectTargetFrameworkChangedButPropertyNameIsInDifferentCase_EventFires ()
		{
			CreateProjectTargetFrameworkMonitor ();
			FakeDotNetProject project = LoadSolutionWithOneProject ();
			CaptureProjectTargetFrameworkChangedEvents ();

			project.RaiseModifiedEvent (project, targetFrameworkPropertyName.ToUpperInvariant ());

			Assert.AreEqual (1, eventArgs.Count);
			Assert.AreEqual (project, eventArgs [0].Project);
		}

		[Test]
		public void ProjectTargetFrameworkChanged_ProjectTargetFrameworkChangedAfterSolutionUnloaded_EventDoesNotFire ()
		{
			CreateProjectTargetFrameworkMonitor ();
			FakeDotNetProject project = LoadSolutionWithOneProject ();
			CaptureProjectTargetFrameworkChangedEvents ();
			UnloadSolution ();

			project.RaiseModifiedEvent (project, targetFrameworkPropertyName);

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

			Assert.AreEqual (1, eventArgs.Count);
			Assert.AreEqual (project, eventArgs [0].Project);
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

			Assert.AreEqual (0, eventArgs.Count);
		}
	}
}

