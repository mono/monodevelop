//
// UpdatedPackagesInSolutionTests.cs
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
using System.Linq;
using ICSharpCode.PackageManagement;
using MonoDevelop.Core;
using MonoDevelop.PackageManagement.Tests.Helpers;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class UpdatedPackagesInSolutionTests
	{
		TestableUpdatedPackagesInSolution updatedPackagesInSolution;
		FakePackageManagementSolution solution;
		FakeRegisteredPackageRepositories registeredPackageRepositories;
		PackageManagementEvents packageManagementEvents;
		FakeTaskFactory taskFactory;
		FakeProgressMonitorFactory progressMonitorFactory;
		TestableCheckForUpdatesTaskRunner checkForUpdatesTaskRunner;
		List<string> messagesLogged;

		void CreateUpdatedPackagesInSolution ()
		{
			solution = new FakePackageManagementSolution ();
			registeredPackageRepositories = new FakeRegisteredPackageRepositories ();
			packageManagementEvents = new PackageManagementEvents ();
			taskFactory = new FakeTaskFactory ();
			taskFactory.RunTasksSynchronously = true;
			progressMonitorFactory = new FakeProgressMonitorFactory ();
			checkForUpdatesTaskRunner = new TestableCheckForUpdatesTaskRunner (taskFactory);
			updatedPackagesInSolution = new TestableUpdatedPackagesInSolution (
				solution,
				registeredPackageRepositories,
				packageManagementEvents,
				checkForUpdatesTaskRunner);
		}

		FakePackageManagementProject AddProjectToSolution ()
		{
			var project = new FakePackageManagementProject ();
			project.FakeSourceRepository = registeredPackageRepositories.FakeAggregateRepository;
			solution.FakeProjects.Add (project);
			return project;
		}

		FakePackage AddUpdatedPackageToAggregateSourceRepository (string id, string version)
		{
			return registeredPackageRepositories.FakeAggregateRepository.AddFakePackageWithVersion (id, version);
		}

		void CaptureMessagesLogged ()
		{
			messagesLogged = new List<string> ();
			packageManagementEvents.PackageOperationMessageLogged += (sender, e) => {
				messagesLogged.Add (e.Message.ToString ());
			};
		}

		[Test]
		public void CheckForUpdates_OnePackageUpdated_OneUpdatedPackageFoundForProject ()
		{
			CreateUpdatedPackagesInSolution ();
			FakePackageManagementProject project = AddProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0");
			FakePackage updatedPackage = AddUpdatedPackageToAggregateSourceRepository ("MyPackage", "1.1");
			var expectedPackages = new FakePackage [] { updatedPackage };

			updatedPackagesInSolution.CheckForUpdates ();
			UpdatedPackagesInProject updatedPackages = updatedPackagesInSolution.GetUpdatedPackages (project.Project);

			Assert.AreEqual (project.Project, updatedPackages.Project);
			Assert.IsNotNull (updatedPackages.Project);
			CollectionAssert.AreEqual (expectedPackages, updatedPackages.GetPackages ());
		}

		[Test]
		public void CheckForUpdates_OnePackageUpdated_AggregateRepositoryUsedWhenCheckingForUpdates ()
		{
			CreateUpdatedPackagesInSolution ();
			FakePackageManagementProject project = AddProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0");
			AddUpdatedPackageToAggregateSourceRepository ("MyPackage", "1.1");

			updatedPackagesInSolution.CheckForUpdates ();
			updatedPackagesInSolution.GetUpdatedPackages (project.Project);

			Assert.AreEqual (registeredPackageRepositories.FakeAggregateRepository, solution.SourceRepositoryPassedToGetProjects);
		}

		[Test]
		public void CheckForUpdates_NoPackagesUpdated_DoesNotReturnNullUpdatedPackagesForProject ()
		{
			CreateUpdatedPackagesInSolution ();
			FakePackageManagementProject project = AddProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0");

			updatedPackagesInSolution.CheckForUpdates ();
			UpdatedPackagesInProject updatedPackages = updatedPackagesInSolution.GetUpdatedPackages (project.Project);

			Assert.AreEqual (project.Project, updatedPackages.Project);
			Assert.IsNotNull (updatedPackages.Project);
			Assert.AreEqual (0, updatedPackages.GetPackages ().Count ());
		}

		[Test]
		public void Clear_OnePackageUpdatedButEverythingCleared_NoUpdatedPackagesFoundForProject ()
		{
			CreateUpdatedPackagesInSolution ();
			FakePackageManagementProject project = AddProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0");
			AddUpdatedPackageToAggregateSourceRepository ("MyPackage", "1.1");
			updatedPackagesInSolution.CheckForUpdates ();

			updatedPackagesInSolution.Clear ();

			UpdatedPackagesInProject updatedPackages = updatedPackagesInSolution.GetUpdatedPackages (project.Project);

			Assert.AreEqual (project.Project, updatedPackages.Project);
			Assert.IsNotNull (updatedPackages.Project);
			Assert.AreEqual (0, updatedPackages.GetPackages ().Count ());
		}

		[Test]
		public void CheckForUpdates_OnePackageUpdated_UpdatedPackagesAvailableEventIsFired ()
		{
			CreateUpdatedPackagesInSolution ();
			FakePackageManagementProject project = AddProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0");
			AddUpdatedPackageToAggregateSourceRepository ("MyPackage", "1.1");
			bool fired = false;
			packageManagementEvents.UpdatedPackagesAvailable += (sender, e) => {
				fired = true;
			};

			updatedPackagesInSolution.CheckForUpdates ();

			Assert.IsTrue (fired);
		}

		[Test]
		public void CheckForUpdates_NoPackagesUpdated_UpdatedPackagesAvailableEventIsNotFired ()
		{
			CreateUpdatedPackagesInSolution ();
			FakePackageManagementProject project = AddProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0");
			updatedPackagesInSolution.CheckForUpdates ();
			bool fired = false;
			packageManagementEvents.UpdatedPackagesAvailable += (sender, e) => {
				fired = true;
			};

			updatedPackagesInSolution.CheckForUpdates ();

			Assert.IsFalse (fired);
		}

		[Test]
		public void GetUpdatedPackages_OnePackageUpdatedSameUnderlyingDotNetProjectButDifferentProxy_OneUpdatedPackageFoundForProject ()
		{
			CreateUpdatedPackagesInSolution ();
			FakePackageManagementProject project = AddProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0");
			FakePackage updatedPackage = AddUpdatedPackageToAggregateSourceRepository ("MyPackage", "1.1");
			var expectedPackages = new FakePackage [] { updatedPackage };
			var newProject = new FakeDotNetProject ();
			project.FakeDotNetProject.EqualsAction = p => {
				return p == newProject;
			};
			updatedPackagesInSolution.CheckForUpdates ();

			UpdatedPackagesInProject updatedPackages = updatedPackagesInSolution.GetUpdatedPackages (newProject);

			Assert.IsNotNull (updatedPackages.Project);
			CollectionAssert.AreEqual (expectedPackages, updatedPackages.GetPackages ());
			Assert.AreNotEqual (newProject, updatedPackages.Project);
		}

		[Test]
		public void GetUpdatedPackages_OnePackageUpdatedAndPackageUpdateIsInstalled_NoUpdatesAvailable ()
		{
			CreateUpdatedPackagesInSolution ();
			FakePackageManagementProject project = AddProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0");
			FakePackage updatedPackage = AddUpdatedPackageToAggregateSourceRepository ("MyPackage", "1.1");
			updatedPackagesInSolution.CheckForUpdates ();
			project.PackageReferences.Clear ();
			project.AddPackageReference  ("MyPackage", "1.1");
			packageManagementEvents.OnParentPackageInstalled (updatedPackage, project);

			UpdatedPackagesInProject updatedPackages = updatedPackagesInSolution.GetUpdatedPackages (project.Project);

			Assert.AreEqual (0, updatedPackages.GetPackages ().Count ());
		}

		[Test]
		public void CheckForUpdates_TwoProjectsAndNoPackagesUpdated_CheckingProjectMessageIsLogged ()
		{
			CreateUpdatedPackagesInSolution ();
			FakePackageManagementProject project1 = AddProjectToSolution ();
			project1.Name = "MyProject1";
			project1.AddFakePackage ("MyPackage", "1.0");
			FakePackageManagementProject project2 = AddProjectToSolution ();
			project2.Name = "MyProject2";
			updatedPackagesInSolution.CheckForUpdates ();
			CaptureMessagesLogged ();

			updatedPackagesInSolution.CheckForUpdates ();

			Assert.That (messagesLogged, Contains.Item ("Checking MyProject1 for updates..."));
			Assert.That (messagesLogged, Contains.Item ("Checking MyProject2 for updates..."));
			Assert.That (messagesLogged, Contains.Item ("0 updates found."));
		}

		[Test]
		public void CheckForUpdates_OnePackageUpdated_OneFoundMessageLogged ()
		{
			CreateUpdatedPackagesInSolution ();
			FakePackageManagementProject project = AddProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0");
			AddUpdatedPackageToAggregateSourceRepository ("MyPackage", "1.1");
			CaptureMessagesLogged ();

			updatedPackagesInSolution.CheckForUpdates ();

			Assert.That (messagesLogged, Contains.Item ("1 update found."));
		}

		[Test]
		public void CheckForUpdates_TwoPackagesUpdated_TwoUpdatesFoundMessageLogged ()
		{
			CreateUpdatedPackagesInSolution ();
			FakePackageManagementProject project = AddProjectToSolution ();
			project.AddPackageReference ("One", "1.0");
			project.AddPackageReference ("Two", "1.0");
			AddUpdatedPackageToAggregateSourceRepository ("One", "1.1");
			AddUpdatedPackageToAggregateSourceRepository ("Two", "1.4");
			CaptureMessagesLogged ();

			updatedPackagesInSolution.CheckForUpdates ();

			Assert.That (messagesLogged, Contains.Item ("2 updates found."));
		}

		[Test]
		public void CheckForUpdates_ProjectHasNoPackagesConfigFile_NoProjectsCheckedForUpdates ()
		{
			CreateUpdatedPackagesInSolution ();
			FakePackageManagementProject project = AddProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0");
			string expectedPackagesConfigFileName = @"d:\projects\MyProject\packages.config".ToNativePath ();
			string fileChecked = null;
			updatedPackagesInSolution.FileExistsAction = path => {
				fileChecked = path;
				return false;
			};
			project.FakeDotNetProject.BaseDirectory = @"d:\projects\MyProject".ToNativePath ();
			CaptureMessagesLogged ();

			updatedPackagesInSolution.CheckForUpdates ();

			Assert.AreEqual (0, messagesLogged.Count);
			Assert.AreEqual (expectedPackagesConfigFileName, fileChecked);
		}

		[Test]
		public void GetUpdatedPackages_OnePackageUpdatedAndPackageIsUninstalled_NoUpdatesAvailableForUninstalledPackage ()
		{
			CreateUpdatedPackagesInSolution ();
			FakePackageManagementProject project = AddProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0");
			var package = FakePackage.CreatePackageWithVersion ("MyPackage", "1.0");
			AddUpdatedPackageToAggregateSourceRepository ("MyPackage", "1.1");
			updatedPackagesInSolution.CheckForUpdates ();
			project.PackageReferences.Clear ();
			packageManagementEvents.OnParentPackageUninstalled (package, project);

			UpdatedPackagesInProject updatedPackages = updatedPackagesInSolution.GetUpdatedPackages (project.Project);

			Assert.AreEqual (0, updatedPackages.GetPackages ().Count ());
		}

		[Test]
		public void CheckForUpdates_NoPackagesUpdated_LoggerConfiguredForProject ()
		{
			CreateUpdatedPackagesInSolution ();
			FakePackageManagementProject project = AddProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0");

			updatedPackagesInSolution.CheckForUpdates ();
			UpdatedPackagesInProject updatedPackages = updatedPackagesInSolution.GetUpdatedPackages (project.Project);

			Assert.IsInstanceOf<PackageManagementLogger> (project.Logger);
		}

		[Test]
		public void GetUpdatedPackages_TwoPackagesInstalledOneUpdatedAndUpdatesAvailableForBoth_OneUpdateAvailable ()
		{
			CreateUpdatedPackagesInSolution ();
			FakePackageManagementProject project = AddProjectToSolution ();
			project.AddPackageReference ("One", "1.0");
			project.AddPackageReference ("Two", "1.0");
			FakePackage updatedPackage = AddUpdatedPackageToAggregateSourceRepository ("One", "1.1");
			AddUpdatedPackageToAggregateSourceRepository ("Two", "1.1");
			updatedPackagesInSolution.CheckForUpdates ();
			project.PackageReferences.Clear ();
			project.AddPackageReference ("One", "1.1");
			project.AddPackageReference ("Two", "1.0");
			packageManagementEvents.OnParentPackageInstalled (updatedPackage, project);

			UpdatedPackagesInProject updatedPackages = updatedPackagesInSolution.GetUpdatedPackages (project.Project);

			Assert.AreEqual (1, updatedPackages.GetPackages ().Count ());
			Assert.AreEqual ("Two", updatedPackages.GetPackages ().FirstOrDefault ().Id);
		}

		[Test]
		public void GetUpdatedPackages_TwoPackagesInstalledOneUpdatedWhichUpdatesItsDependency_NoUpdatesAvailable ()
		{
			CreateUpdatedPackagesInSolution ();
			FakePackageManagementProject project = AddProjectToSolution ();
			project.AddPackageReference ("One", "1.0");
			project.AddPackageReference ("Two", "1.0");
			FakePackage updatedPackage = AddUpdatedPackageToAggregateSourceRepository ("One", "1.1");
			AddUpdatedPackageToAggregateSourceRepository ("Two", "1.1");
			updatedPackagesInSolution.CheckForUpdates ();
			project.PackageReferences.Clear ();
			project.AddPackageReference ("One", "1.1");
			project.AddPackageReference ("Two", "1.1");
			packageManagementEvents.OnParentPackageInstalled (updatedPackage, project);

			UpdatedPackagesInProject updatedPackages = updatedPackagesInSolution.GetUpdatedPackages (project.Project);

			Assert.AreEqual (0, updatedPackages.GetPackages ().Count ());
		}

		[Test]
		public void CheckForUpdates_OnePackageUpdatedButSolutionClosedBeforeResultsReturned_UpdatedPackagesAvailableEventIsNotFiredAndNoPackageUpdatesAvailable ()
		{
			CreateUpdatedPackagesInSolution ();
			taskFactory.RunTasksSynchronously = false;
			FakePackageManagementProject project = AddProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0");
			AddUpdatedPackageToAggregateSourceRepository ("MyPackage", "1.1");
			bool fired = false;
			packageManagementEvents.UpdatedPackagesAvailable += (sender, e) => {
				fired = true;
			};
			updatedPackagesInSolution.CheckForUpdates ();
			var task = taskFactory.FakeTasksCreated [0] as FakeTask<CheckForUpdatesTask>;
			task.ExecuteTaskButNotContinueWith ();
			updatedPackagesInSolution.Clear ();

			task.ExecuteContinueWith ();

			Assert.IsFalse (fired);
			Assert.IsFalse (updatedPackagesInSolution.AnyUpdates ());
		}

		[Test]
		public void CheckForUpdates_NoPackagesUpdated_NoUpdates ()
		{
			CreateUpdatedPackagesInSolution ();
			FakePackageManagementProject project = AddProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0");

			updatedPackagesInSolution.CheckForUpdates ();
			UpdatedPackagesInProject updatedPackages = updatedPackagesInSolution.GetUpdatedPackages (project.Project);

			Assert.IsFalse (updatedPackagesInSolution.AnyUpdates ());
		}

		[Test]
		public void CheckForUpdates_ExceptionThrownWhilstCheckingForUpdates_ExceptionLogged ()
		{
			CreateUpdatedPackagesInSolution ();
			taskFactory.RunTasksSynchronously = false;
			FakePackageManagementProject project = AddProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0");
			updatedPackagesInSolution.CheckForUpdates ();

			var task = taskFactory.FakeTasksCreated [0] as FakeTask<CheckForUpdatesTask>;
			task.IsFaulted = true;
			var innerException = new ApplicationException ("Inner exception error message");
			task.Exception = new AggregateException ("Aggregate error message", innerException);
			task.ExecuteTaskButNotContinueWith ();
			task.Result = null;
			task.ExecuteContinueWith ();

			Assert.AreEqual ("Current check for updates task error.", checkForUpdatesTaskRunner.LoggedErrorMessages[0]);
			Assert.AreEqual (task.Exception, checkForUpdatesTaskRunner.LoggedExceptions[0]);
		}

		[Test]
		public void CheckForUpdates_ExceptionThrownWhilstCheckingForUpdatesButSolutionClosedBeforeCheckForUpdatesReturns_ErrorIsLogged ()
		{
			CreateUpdatedPackagesInSolution ();
			taskFactory.RunTasksSynchronously = false;
			FakePackageManagementProject project = AddProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0");
			updatedPackagesInSolution.CheckForUpdates ();

			var task = taskFactory.FakeTasksCreated [0] as FakeTask<CheckForUpdatesTask>;
			task.IsFaulted = true;
			task.Exception = new AggregateException ("Error message");
			task.ExecuteTaskButNotContinueWith ();
			updatedPackagesInSolution.Clear ();
			task.Result = null;
			task.ExecuteContinueWith ();

			Assert.AreEqual ("Check for updates task error.", checkForUpdatesTaskRunner.LoggedErrorMessages[0]);
			Assert.AreEqual (task.Exception, checkForUpdatesTaskRunner.LoggedExceptions[0]);
		}

		[Test]
		public void CheckForUpdates_ExceptionThrownWhilstCheckingForUpdatesButSolutionClosedBeforeCheckForUpdatesReturnsAndSecondCheckForUpdatesIsStarted_ErrorIsLogged ()
		{
			CreateUpdatedPackagesInSolution ();
			taskFactory.RunTasksSynchronously = false;
			FakePackageManagementProject project = AddProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0");
			updatedPackagesInSolution.CheckForUpdates ();

			var task = taskFactory.FakeTasksCreated [0] as FakeTask<CheckForUpdatesTask>;
			task.IsFaulted = true;
			task.Exception = new AggregateException ("Error message");
			task.ExecuteTaskButNotContinueWith ();
			updatedPackagesInSolution.CheckForUpdates ();
			task.Result = null;
			task.ExecuteContinueWith ();

			Assert.AreEqual ("Check for updates task error.", checkForUpdatesTaskRunner.LoggedErrorMessages[0]);
			Assert.AreEqual (task.Exception, checkForUpdatesTaskRunner.LoggedExceptions[0]);
		}

		[Test]
		public void CheckForUpdates_OnePackageUpdatedAndSolutionClosedBeforeResultsReturnedAndThenSolutionOpenedAgain_UpdatedPackagesAvailableEventIsFiredForSecondOpeningOfSolution ()
		{
			CreateUpdatedPackagesInSolution ();
			taskFactory.RunTasksSynchronously = false;
			FakePackageManagementProject project = AddProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0");
			AddUpdatedPackageToAggregateSourceRepository ("MyPackage", "1.1");
			bool fired = false;
			packageManagementEvents.UpdatedPackagesAvailable += (sender, e) => {
				fired = true;
			};
			updatedPackagesInSolution.CheckForUpdates ();
			var firstTask = taskFactory.FakeTasksCreated [0] as FakeTask<CheckForUpdatesTask>;
			firstTask.ExecuteTaskButNotContinueWith ();
			updatedPackagesInSolution.CheckForUpdates ();
			firstTask.ExecuteContinueWith ();
			var secondTask = taskFactory.FakeTasksCreated [1] as FakeTask<CheckForUpdatesTask>;
			secondTask.ExecuteTaskButNotContinueWith ();
			secondTask.ExecuteContinueWith ();

			Assert.IsTrue (updatedPackagesInSolution.AnyUpdates ());
			Assert.IsTrue (fired);
		}

		[Test]
		public void GetUpdatedPackages_OnePackageUpdatedAndPackageIsUninstalledWhilstCheckingForUpdates_NoUpdatesAvailableForUninstalledPackage ()
		{
			CreateUpdatedPackagesInSolution ();
			taskFactory.RunTasksSynchronously = false;
			FakePackageManagementProject project = AddProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0");
			var package = FakePackage.CreatePackageWithVersion ("MyPackage", "1.0");
			AddUpdatedPackageToAggregateSourceRepository ("MyPackage", "1.1");
			updatedPackagesInSolution.CheckForUpdates ();
			var task = taskFactory.FakeTasksCreated [0] as FakeTask<CheckForUpdatesTask>;
			task.ExecuteTaskButNotContinueWith ();
			project.PackageReferences.Clear ();
			packageManagementEvents.OnParentPackageUninstalled (package, project);
			task.ExecuteContinueWith ();

			UpdatedPackagesInProject updatedPackages = updatedPackagesInSolution.GetUpdatedPackages (project.Project);

			Assert.AreEqual (0, updatedPackages.GetPackages ().Count ());
		}

		[Test]
		public void CheckForUpdates_TaskCancelled_TaskResultIsNotReferenced ()
		{
			CreateUpdatedPackagesInSolution ();
			taskFactory.RunTasksSynchronously = false;
			FakePackageManagementProject project = AddProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0");
			updatedPackagesInSolution.CheckForUpdates ();

			var task = taskFactory.FakeTasksCreated [0] as FakeTask<CheckForUpdatesTask>;
			task.IsCancelled = true;
			task.ExecuteTaskButNotContinueWith ();
			task.Result = null;

			Assert.DoesNotThrow (() => {
				task.ExecuteContinueWith ();
			});
		}

		[Test]
		public void GetUpdatedPackages_OnePackageHasUpdatesAndNewerVersionButNotLatestIsInstalled_UpdatesStillShowAsAvailable ()
		{
			CreateUpdatedPackagesInSolution ();
			FakePackageManagementProject project = AddProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0");
			var newerPackage = new FakePackage ("MyPackage", "1.1");
			FakePackage updatedPackage = AddUpdatedPackageToAggregateSourceRepository ("MyPackage", "1.9");
			updatedPackagesInSolution.CheckForUpdates ();
			project.PackageReferences.Clear ();
			project.AddPackageReference ("MyPackage", "1.1");
			packageManagementEvents.OnParentPackageInstalled (newerPackage, project);

			UpdatedPackagesInProject updatedPackages = updatedPackagesInSolution.GetUpdatedPackages (project.Project);

			Assert.AreEqual (1, updatedPackages.GetPackages ().Count ());
			Assert.AreEqual ("MyPackage", updatedPackages.GetPackages ().First ().Id);
			Assert.AreEqual ("1.9", updatedPackages.GetPackages ().First ().Version.ToString ());
		}

		[Test]
		public void GetUpdatedPackages_OnePackageUpdatedAndPackageUpdatedWhilstCheckingForUpdates_UpdateIsNotAvailableForPackage ()
		{
			CreateUpdatedPackagesInSolution ();
			taskFactory.RunTasksSynchronously = false;
			FakePackageManagementProject project = AddProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0");
			FakePackage updatedPackage = AddUpdatedPackageToAggregateSourceRepository ("MyPackage", "1.1");
			updatedPackagesInSolution.CheckForUpdates ();
			var task = taskFactory.FakeTasksCreated [0] as FakeTask<CheckForUpdatesTask>;
			task.ExecuteTaskButNotContinueWith ();
			project.PackageReferences.Clear ();
			project.AddPackageReference ("MyPackage", "1.1");
			packageManagementEvents.OnParentPackageInstalled (updatedPackage, project);
			task.ExecuteContinueWith ();

			UpdatedPackagesInProject updatedPackages = updatedPackagesInSolution.GetUpdatedPackages (project.Project);

			Assert.AreEqual (0, updatedPackages.GetPackages ().Count ());
		}

		[Test]
		public void GetUpdatedPackages_OnePackageUpdatedAndNewerButNotLatestPackageIsInstalledWhilstCheckingForUpdates_UpdateIsAvailableForPackage ()
		{
			CreateUpdatedPackagesInSolution ();
			taskFactory.RunTasksSynchronously = false;
			FakePackageManagementProject project = AddProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0");
			var installedPackage = FakePackage.CreatePackageWithVersion ("MyPackage", "1.2");
			AddUpdatedPackageToAggregateSourceRepository ("MyPackage", "1.8");
			updatedPackagesInSolution.CheckForUpdates ();
			var task = taskFactory.FakeTasksCreated [0] as FakeTask<CheckForUpdatesTask>;
			task.ExecuteTaskButNotContinueWith ();
			project.PackageReferences.Clear ();
			project.AddPackageReference ("MyPackage", "1.2");
			packageManagementEvents.OnParentPackageInstalled (installedPackage, project);
			task.ExecuteContinueWith ();

			UpdatedPackagesInProject updatedPackages = updatedPackagesInSolution.GetUpdatedPackages (project.Project);

			Assert.AreEqual (1, updatedPackages.GetPackages ().Count ());
			Assert.AreEqual ("MyPackage", updatedPackages.GetPackages ().First ().Id);
			Assert.AreEqual ("1.8", updatedPackages.GetPackages ().First ().Version.ToString ());
		}
	}
}

