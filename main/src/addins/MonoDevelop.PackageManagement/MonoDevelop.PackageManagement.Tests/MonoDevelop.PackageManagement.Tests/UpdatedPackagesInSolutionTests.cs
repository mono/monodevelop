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
using System.Linq;
using ICSharpCode.PackageManagement;
using NUnit.Framework;
using MonoDevelop.PackageManagement.Tests.Helpers;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class UpdatedPackagesInSolutionTests
	{
		TestableUpdatedPackagesInSolution updatedPackagesInSolution;
		FakePackageManagementSolution solution;
		FakeRegisteredPackageRepositories registeredPackageRepositories;
		PackageManagementEvents packageManagementEvents;

		void CreateUpdatedPackagesInSolution ()
		{
			solution = new FakePackageManagementSolution ();
			registeredPackageRepositories = new FakeRegisteredPackageRepositories ();
			packageManagementEvents = new PackageManagementEvents ();
			updatedPackagesInSolution = new TestableUpdatedPackagesInSolution (
				solution,
				registeredPackageRepositories,
				packageManagementEvents);
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

		[Test]
		public void CheckForUpdates_OnePackageUpdated_OneUpdatedPackageFoundForProject ()
		{
			CreateUpdatedPackagesInSolution ();
			FakePackageManagementProject project = AddProjectToSolution ();
			project.AddFakePackage ("MyPackage", "1.0");
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
			project.AddFakePackage ("MyPackage", "1.0");
			FakePackage updatedPackage = AddUpdatedPackageToAggregateSourceRepository ("MyPackage", "1.1");

			updatedPackagesInSolution.CheckForUpdates ();
			UpdatedPackagesInProject updatedPackages = updatedPackagesInSolution.GetUpdatedPackages (project.Project);

			Assert.AreEqual (registeredPackageRepositories.FakeAggregateRepository, solution.SourceRepositoryPassedToGetProjects);
		}

		[Test]
		public void CheckForUpdates_NoPackagesUpdated_DoesNotReturnNullUpdatedPackagesForProject ()
		{
			CreateUpdatedPackagesInSolution ();
			FakePackageManagementProject project = AddProjectToSolution ();
			project.AddFakePackage ("MyPackage", "1.0");

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
			project.AddFakePackage ("MyPackage", "1.0");
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
			project.AddFakePackage ("MyPackage", "1.0");
			FakePackage updatedPackage = AddUpdatedPackageToAggregateSourceRepository ("MyPackage", "1.1");
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
			project.AddFakePackage ("MyPackage", "1.0");
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
			project.AddFakePackage ("MyPackage", "1.0");
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
			project.AddFakePackage ("MyPackage", "1.0");
			FakePackage updatedPackage = AddUpdatedPackageToAggregateSourceRepository ("MyPackage", "1.1");
			updatedPackagesInSolution.CheckForUpdates ();
			packageManagementEvents.OnParentPackageInstalled (updatedPackage, project);

			UpdatedPackagesInProject updatedPackages = updatedPackagesInSolution.GetUpdatedPackages (project.Project);

			Assert.AreEqual (0, updatedPackages.GetPackages ().Count ());
		}
	}
}

