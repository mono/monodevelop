//
// PackageViewModelTests.cs
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
using MonoDevelop.PackageManagement.Tests.Helpers;
using NuGet;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class PackageViewModelTests
	{
		TestablePackageViewModel viewModel;
		FakePackage fakePackage;
		FakePackageManagementSolution fakeSolution;
		PackageManagementEvents packageManagementEvents;
		FakeUninstallPackageAction fakeUninstallPackageAction;
		FakeLogger fakeLogger;
		List<PackageManagementSelectedProject> fakeSelectedProjects;
		AvailablePackagesViewModel viewModelParent;

		void CreateFakeSolution ()
		{
			fakeSolution = new FakePackageManagementSolution ();
			fakeSolution.FakeActiveDotNetProject = ProjectHelper.CreateTestProject ();
		}

		void CreateViewModel ()
		{
			CreateFakeSolution ();
			CreateViewModel (fakeSolution);
		}

		void CreateViewModel (FakePackageManagementSolution solution)
		{
			viewModelParent = CreateViewModelParent (solution);
			viewModel = new TestablePackageViewModel (viewModelParent, solution);
			fakePackage = viewModel.FakePackage;
			this.fakeSolution = solution;
			packageManagementEvents = viewModel.PackageManagementEvents;
			fakeLogger = viewModel.FakeLogger;
			fakeUninstallPackageAction = solution.FakeProjectToReturnFromGetProject.FakeUninstallPackageAction;
		}

		AvailablePackagesViewModel CreateViewModelParent (FakePackageManagementSolution solution)
		{
			var taskFactory = new FakeTaskFactory ();
			var registeredPackageRepositories = new FakeRegisteredPackageRepositories ();
			var packageViewModelFactory = new FakePackageViewModelFactory ();
			var recentPackageRepository = new FakeRecentPackageRepository ();

			return new AvailablePackagesViewModel (
				solution,
				registeredPackageRepositories,
				recentPackageRepository,
				packageViewModelFactory,
				taskFactory);
		}

		void AddProjectToSolution ()
		{
			FakeDotNetProject project = ProjectHelper.CreateTestProject ();
			fakeSolution.FakeDotNetProjects.Add (project);
		}

		void CreateViewModelWithTwoProjectsSelected (string projectName1, string projectName2)
		{
			CreateFakeSolution ();
			AddTwoProjectsSelected (projectName1, projectName2);
			CreateViewModel (fakeSolution);
		}

		void AddTwoProjectsSelected (string projectName1, string projectName2)
		{
			AddProjectToSolution ();
			AddProjectToSolution ();
			fakeSolution.FakeDotNetProjects [0].Name = projectName1;
			fakeSolution.FakeDotNetProjects [1].Name = projectName2;
			fakeSolution.NoProjectsSelected ();

			fakeSolution.AddFakeProjectToReturnFromGetProject (projectName1);
			fakeSolution.AddFakeProjectToReturnFromGetProject (projectName2);
		}

		void SetPackageIdAndVersion (string id, string version)
		{
			fakePackage.Id = id;
			fakePackage.Version = new SemanticVersion (version);
		}

		void UserCancelsProjectSelection ()
		{
			packageManagementEvents.SelectProjects += (sender, e) => {
				e.IsAccepted = false;
			};
		}

		void UserAcceptsProjectSelection ()
		{
			packageManagementEvents.SelectProjects += (sender, e) => {
				e.IsAccepted = true;
			};
		}

		List<PackageManagementSelectedProject> CreateTwoFakeSelectedProjects ()
		{
			fakeSelectedProjects = new List<PackageManagementSelectedProject> ();
			var projectA = new FakePackageManagementProject ("Project A");
			fakeSelectedProjects.Add (new PackageManagementSelectedProject (projectA));

			var projectB = new FakePackageManagementProject ("Project B");
			fakeSelectedProjects.Add (new PackageManagementSelectedProject (projectB));
			return fakeSelectedProjects;
		}

		FakePackageOperation AddFakeInstallPackageOperationWithPackageThatRequiresLicenseAcceptance (PackageManagementSelectedProject selectedProject)
		{
			return AddFakeInstallPackageOperationWithPackage (selectedProject, requireLicenseAcceptance: true);
		}

		FakePackageOperation AddFakeInstallPackageOperationWithPackageThatDoesNotRequireLicenseAcceptance (PackageManagementSelectedProject selectedProject)
		{
			return AddFakeInstallPackageOperationWithPackage (selectedProject, requireLicenseAcceptance: false);
		}

		FakePackageOperation AddFakeInstallPackageOperationWithPackage (PackageManagementSelectedProject selectedProject, bool requireLicenseAcceptance)
		{
			var project = selectedProject.Project as FakePackageManagementProject;
			FakePackageOperation operation = project.AddFakeInstallOperation ();
			operation.FakePackage.RequireLicenseAcceptance = requireLicenseAcceptance;
			return operation;
		}

		FakePackageOperation AddFakeUninstallPackageOperationWithPackageThatRequiresLicenseAcceptance (PackageManagementSelectedProject selectedProject)
		{
			var project = selectedProject.Project as FakePackageManagementProject;
			FakePackageOperation uninstallOperation = project.AddFakeUninstallOperation ();
			uninstallOperation.FakePackage.RequireLicenseAcceptance = true;
			return uninstallOperation;
		}

		PackageManagementSelectedProject FirstFakeSelectedProject {
			get { return fakeSelectedProjects [0]; }
		}

		PackageManagementSelectedProject SecondFakeSelectedProject {
			get { return fakeSelectedProjects [1]; }
		}

		void ParentAllowsPrereleasePackages ()
		{
			viewModelParent.IncludePrerelease = true;
		}

		[Test]
		public void AddPackageCommand_CommandExecuted_InstallsPackage ()
		{
			CreateViewModel ();
			viewModel.AddOneFakeInstallPackageOperationForViewModelPackage ();

			viewModel.AddPackageCommand.Execute (null);

			IPackage package = fakeSolution.FakeProjectToReturnFromGetProject.LastInstallPackageCreated.Package;
			Assert.AreEqual (fakePackage, package);
		}

		[Test]
		public void AddPackage_PackageAddedSuccessfully_SourcePackageRepositoryUsedToCreateProject ()
		{
			CreateViewModel ();
			viewModel.AddOneFakeInstallPackageOperationForViewModelPackage ();

			viewModel.AddPackage ();

			Assert.AreEqual (fakePackage.Repository, fakeSolution.RepositoryPassedToGetProject);
		}

		[Test]
		public void AddPackage_PackageAddedSuccessfully_PackageOperationsUsedWhenInstallingPackage ()
		{
			CreateViewModel ();
			viewModel.AddOneFakeInstallPackageOperationForViewModelPackage ();
			viewModel.AddPackage ();

			PackageOperation[] expectedOperations = new PackageOperation[] {
				new PackageOperation (fakePackage, PackageAction.Install)
			};

			FakeInstallPackageAction action = fakeSolution.FakeProjectToReturnFromGetProject.LastInstallPackageCreated;
			CollectionAssert.AreEqual (expectedOperations, action.Operations);
		}

		[Test]
		public void HasLicenseUrl_PackageHasLicenseUrl_ReturnsTrue ()
		{
			CreateViewModel ();
			fakePackage.LicenseUrl = new Uri ("http://sharpdevelop.com");

			Assert.IsTrue (viewModel.HasLicenseUrl);
		}

		[Test]
		public void HasLicenseUrl_PackageHasNoLicenseUrl_ReturnsFalse ()
		{
			CreateViewModel ();
			fakePackage.LicenseUrl = null;

			Assert.IsFalse (viewModel.HasLicenseUrl);
		}

		[Test]
		public void HasProjectUrl_PackageHasProjectUrl_ReturnsTrue ()
		{
			CreateViewModel ();
			fakePackage.ProjectUrl = new Uri ("http://sharpdevelop.com");

			Assert.IsTrue (viewModel.HasProjectUrl);
		}

		[Test]
		public void HasProjectUrl_PackageHasNoProjectUrl_ReturnsFalse ()
		{
			CreateViewModel ();
			fakePackage.ProjectUrl = null;

			Assert.IsFalse (viewModel.HasProjectUrl);
		}

		[Test]
		public void HasReportAbuseUrl_PackageHasReportAbuseUrl_ReturnsTrue ()
		{
			CreateViewModel ();
			fakePackage.ReportAbuseUrl = new Uri ("http://sharpdevelop.com");

			Assert.IsTrue (viewModel.HasReportAbuseUrl);
		}

		[Test]
		public void HasReportAbuseUrl_PackageHasNoReportAbuseUrl_ReturnsFalse ()
		{
			CreateViewModel ();
			fakePackage.ReportAbuseUrl = null;

			Assert.IsFalse (viewModel.HasReportAbuseUrl);
		}

		[Test]
		public void IsAdded_ProjectHasPackageAdded_ReturnsTrue ()
		{
			CreateViewModel ();
			fakeSolution.FakeProjectToReturnFromGetProject.FakePackages.Add (fakePackage);

			Assert.IsTrue (viewModel.IsAdded);
		}

		[Test]
		public void IsAdded_ProjectDoesNotHavePackageInstalled_ReturnsFalse ()
		{
			CreateViewModel ();
			fakeSolution.FakeProjectToReturnFromGetProject.FakePackages.Clear ();

			Assert.IsFalse (viewModel.IsAdded);
		}

		[Test]
		public void RemovePackageCommand_CommandExecuted_UninstallsPackage ()
		{
			CreateViewModel ();
			viewModel.RemovePackageCommand.Execute (null);

			Assert.AreEqual (fakePackage, fakeUninstallPackageAction.Package);
		}

		[Test]
		public void RemovePackage_PackageRemovedSuccessfully_RepositoryUsedToCreateProject ()
		{
			CreateViewModel ();
			viewModel.RemovePackage ();

			Assert.AreEqual (fakePackage.Repository, fakeSolution.RepositoryPassedToGetProject);
		}

		[Test]
		public void PackageChanged_PackageRemovedSuccessfully_PropertyNotifyChangedFiredForIsAddedProperty ()
		{
			CreateViewModel ();
			string propertyChangedName = null;
			viewModel.PropertyChanged += (sender, e) => propertyChangedName = e.PropertyName;
			viewModel.RemovePackage ();

			Assert.AreEqual ("IsAdded", propertyChangedName);
		}

		[Test]
		public void PackageChanged_PackageRemovedSuccessfully_PropertyNotifyChangedFiredAfterPackageUninstalled ()
		{
			CreateViewModel ();
			IPackage packagePassedToUninstallPackageWhenPropertyNameChanged = null;
			viewModel.PropertyChanged += (sender, e) => {
				packagePassedToUninstallPackageWhenPropertyNameChanged = fakeUninstallPackageAction.Package;
			};
			viewModel.RemovePackage ();

			Assert.AreEqual (fakePackage, packagePassedToUninstallPackageWhenPropertyNameChanged);
		}

		[Test]
		public void HasDependencies_PackageHasNoDependencies_ReturnsFalse ()
		{
			CreateViewModel ();
			fakePackage.HasDependencies = false;

			Assert.IsFalse (viewModel.HasDependencies);
		}

		[Test]
		public void HasDependencies_PackageHasDependency_ReturnsTrue ()
		{
			CreateViewModel ();
			fakePackage.AddDependency ("Dependency");

			Assert.IsTrue (viewModel.HasDependencies);
		}

		[Test]
		public void HasNoDependencies_PackageHasNoDependencies_ReturnsTrue ()
		{
			CreateViewModel ();

			Assert.IsTrue (viewModel.HasNoDependencies);
		}

		[Test]
		public void HasNoDependencies_PackageHasOneDependency_ReturnsFalse ()
		{
			CreateViewModel ();
			fakePackage.AddDependency ("Dependency");

			Assert.IsFalse (viewModel.HasNoDependencies);
		}

		[Test]
		public void HasDownloadCount_DownloadCountIsZero_ReturnsTrue ()
		{
			CreateViewModel ();
			fakePackage.DownloadCount = 0;

			Assert.IsTrue (viewModel.HasDownloadCount);
		}

		[Test]
		public void HasDownloadCount_DownloadCountIsMinusOne_ReturnsFalse ()
		{
			CreateViewModel ();
			fakePackage.DownloadCount = -1;

			Assert.IsFalse (viewModel.HasDownloadCount);
		}

		[Test]
		public void HasLastPublished_PackageHasPublishedDate_ReturnsTrue ()
		{
			CreateViewModel ();
			fakePackage.Published = new DateTime (2011, 1, 2);

			Assert.IsTrue (viewModel.HasLastPublished);
		}

		[Test]
		public void HasLastPublished_PackageHasNoPublishedDate_ReturnsFalse ()
		{
			CreateViewModel ();
			fakePackage.Published = null;

			Assert.IsFalse (viewModel.HasLastPublished);
		}

		[Test]
		public void LastPublished_PackageHasPublishedDate_ReturnsPackagePublishedDate ()
		{
			CreateViewModel ();
			fakePackage.Published = new DateTime (2011, 1, 2);

			Assert.AreEqual (fakePackage.Published, viewModel.LastPublished);
		}

		[Test]
		public void LastPublished_PackageHasNoPublishedDate_ReturnsNull ()
		{
			CreateViewModel ();
			fakePackage.Published = null;

			Assert.IsNull (viewModel.LastPublished);
		}

		[Test]
		public void AddPackage_CheckLoggerUsed_PackageViewModelLoggerUsedWhenResolvingPackageOperations ()
		{
			CreateViewModel ();
			viewModel.AddOneFakeInstallPackageOperationForViewModelPackage ();
			viewModel.AddPackage ();

			ILogger expectedLogger = viewModel.OperationLoggerCreated;
			ILogger actualLogger = fakeSolution.FakeProjectToReturnFromGetProject.Logger;
			Assert.AreEqual (expectedLogger, actualLogger);
		}

		[Test]
		public void AddPackage_PackageAddedSuccessfully_InstallingPackageMessageIsFirstMessageLogged ()
		{
			CreateViewModel ();
			viewModel.AddOneFakeInstallPackageOperationForViewModelPackage ();
			fakePackage.Id = "Test.Package";
			fakePackage.Version = new SemanticVersion (1, 2, 0, 55);
			viewModel.AddPackage ();

			string expectedMessage = "------- Installing...Test.Package 1.2.0.55 -------";
			string actualMessage = fakeLogger.FirstFormattedMessageLogged;

			Assert.AreEqual (expectedMessage, actualMessage);
		}

		[Test]
		public void AddPackage_PackageAddedSuccessfully_NextToLastMessageLoggedMarksEndOfInstallation ()
		{
			CreateViewModel ();
			viewModel.AddOneFakeInstallPackageOperationForViewModelPackage ();
			viewModel.AddPackage ();

			string expectedMessage = "==============================";
			string actualMessage = fakeLogger.NextToLastFormattedMessageLogged;

			Assert.AreEqual (expectedMessage, actualMessage);
		}

		[Test]
		public void AddPackage_PackageAddedSuccessfully_LastMessageLoggedIsEmptyLine ()
		{
			CreateViewModel ();
			viewModel.AddOneFakeInstallPackageOperationForViewModelPackage ();
			viewModel.AddPackage ();

			string expectedMessage = String.Empty;
			string actualMessage = fakeLogger.LastFormattedMessageLogged;

			Assert.AreEqual (expectedMessage, actualMessage);
		}

		[Test]
		public void RemovePackage_PackageRemovedSuccessfully_UninstallingPackageMessageIsFirstMessageLogged ()
		{
			CreateViewModel ();
			viewModel.AddOneFakeInstallPackageOperationForViewModelPackage ();
			fakePackage.Id = "Test.Package";
			fakePackage.Version = new SemanticVersion (1, 2, 0, 55);
			viewModel.RemovePackage ();

			string expectedMessage = "------- Uninstalling...Test.Package 1.2.0.55 -------";
			string actualMessage = fakeLogger.FirstFormattedMessageLogged;

			Assert.AreEqual (expectedMessage, actualMessage);
		}

		[Test]
		public void RemovePackage_PackageRemovedSuccessfully_NextToLastMessageLoggedMarksEndOfInstallation ()
		{
			CreateViewModel ();
			viewModel.AddOneFakeInstallPackageOperationForViewModelPackage ();
			viewModel.RemovePackage ();

			string expectedMessage = "==============================";
			string actualMessage = fakeLogger.NextToLastFormattedMessageLogged;

			Assert.AreEqual (expectedMessage, actualMessage);
		}

		[Test]
		public void RemovePackage_PackageRemovedSuccessfully_LastMessageLoggedIsEmptyLine ()
		{
			CreateViewModel ();
			viewModel.RemovePackage ();

			string expectedMessage = String.Empty;
			string actualMessage = fakeLogger.LastFormattedMessageLogged;

			Assert.AreEqual (expectedMessage, actualMessage);
		}

		[Test]
		public void AddPackage_PackagesInstalledSuccessfully_ViewModelPackageUsedWhenResolvingPackageOperations ()
		{
			CreateViewModel ();
			viewModel.AddOneFakeInstallPackageOperationForViewModelPackage ();
			viewModel.AddPackage ();

			FakePackage expectedPackage = fakePackage;
			IPackage actualPackage = fakeSolution
				.FakeProjectToReturnFromGetProject
				.PackagePassedToGetInstallPackageOperations;

			Assert.AreEqual (expectedPackage, actualPackage);
		}

		[Test]
		public void AddPackage_PackagesInstalledSuccessfully_PackageDependenciesNotIgnoredWhenCheckingForPackageOperations ()
		{
			CreateViewModel ();
			viewModel.AddOneFakeInstallPackageOperationForViewModelPackage ();
			viewModel.AddPackage ();

			bool result = fakeSolution
				.FakeProjectToReturnFromGetProject
				.IgnoreDependenciesPassedToGetInstallPackageOperations;

			Assert.IsFalse (result);
		}

		[Test]
		public void AddPackage_PackagesInstalledSuccessfully_PrereleaseVersionsNotAllowedWhenCheckingForPackageOperations ()
		{
			CreateViewModel ();
			viewModel.AddOneFakeInstallPackageOperationForViewModelPackage ();
			viewModel.AddPackage ();

			bool result = fakeSolution
				.FakeProjectToReturnFromGetProject
				.AllowPrereleaseVersionsPassedToGetInstallPackageOperations;

			Assert.IsFalse (result);
		}

		[Test]
		public void AddPackage_ParentHasIncludePrereleaseSetToTrueWhenInstalling_PrereleaseVersionsAllowedWhenCheckingForPackageOperations ()
		{
			CreateViewModel ();
			ParentAllowsPrereleasePackages ();
			viewModel.AddOneFakeInstallPackageOperationForViewModelPackage ();
			viewModel.AddPackage ();

			bool result = fakeSolution
				.FakeProjectToReturnFromGetProject
				.AllowPrereleaseVersionsPassedToGetInstallPackageOperations;

			Assert.IsTrue (result);
		}

		[Test]
		public void IsAdded_SolutionSelectedContainingOneProjectAndPackageIsInstalledInSolutionSharedRepository_ReturnsTrue ()
		{
			CreateFakeSolution ();
			AddProjectToSolution ();
			fakeSolution.NoProjectsSelected ();
			fakeSolution.FakeInstalledPackages.Add (fakePackage);
			CreateViewModel (fakeSolution);

			bool added = viewModel.IsAdded;

			Assert.IsTrue (added);
		}

		[Test]
		public void IsAdded_SolutionSelectedContainingOneProjectAndPackageIsNotInstalledInSolutionSharedRepository_ReturnsFalse ()
		{
			CreateViewModel ();
			AddProjectToSolution ();
			fakeSolution.NoProjectsSelected ();

			bool added = viewModel.IsAdded;

			Assert.IsFalse (added);
		}

		[Test]
		public void IsManaged_SolutionSelectedContainingTwoProjects_ReturnsTrue ()
		{
			CreateFakeSolution ();
			AddProjectToSolution ();
			AddProjectToSolution ();
			fakeSolution.NoProjectsSelected ();
			CreateViewModel (fakeSolution);

			bool managed = viewModel.IsManaged;

			Assert.IsTrue (managed);
		}

		[Test]
		public void IsManaged_SolutionSelectedContainingOneProject_ReturnsTrue ()
		{
			CreateFakeSolution ();
			AddProjectToSolution ();
			fakeSolution.NoProjectsSelected ();
			CreateViewModel (fakeSolution);

			bool managed = viewModel.IsManaged;

			Assert.IsTrue (managed);
		}

		[Test]
		public void IsManaged_SolutionWithOneProjectSelected_ReturnsFalse ()
		{
			CreateFakeSolution ();
			AddProjectToSolution ();
			fakeSolution.FakeActiveDotNetProject = fakeSolution.FakeDotNetProjects [0];
			CreateViewModel (fakeSolution);

			bool managed = viewModel.IsManaged;

			Assert.IsFalse (managed);
		}

		[Test]
		public void Summary_PackageHasSummary_PackageSummaryReturned ()
		{
			CreateViewModel ();
			fakePackage.Summary = "Expected summary";

			string summary = viewModel.Summary;

			Assert.AreEqual ("Expected summary", summary);
		}

		[Test]
		public void Summary_PackageHasDescriptionButNoSummary_PackageDescriptionReturned ()
		{
			CreateViewModel ();
			fakePackage.Summary = String.Empty;
			fakePackage.Description = "Expected description";

			string summary = viewModel.Summary;

			Assert.AreEqual ("Expected description", summary);
		}

		[Test]
		public void Name_PackageHasIdButNoTitle_ReturnsPackageId ()
		{
			CreateViewModel ();
			fakePackage.Id = "MyPackage";

			string name = viewModel.Name;

			Assert.AreEqual ("MyPackage", name);
		}

		[Test]
		public void Name_PackageHasIdAndTitle_ReturnsPackageId ()
		{
			CreateViewModel ();
			fakePackage.Id = "MyPackage";
			fakePackage.Title = "My Package Title";

			string name = viewModel.Name;

			Assert.AreEqual ("My Package Title", name);
		}

		[Test]
		public void GalleryUrl_PackageHasGalleryUrl_ReturnsUrl ()
		{
			CreateViewModel ();
			var expectedUrl = new Uri ("http://test.com/MyPackage");
			fakePackage.GalleryUrl = expectedUrl;

			Uri url = viewModel.GalleryUrl;

			Assert.AreEqual (expectedUrl, url);
		}

		[Test]
		public void HasGalleryUrl_PackageHasGalleryUrl_ReturnsTrue ()
		{
			CreateViewModel ();
			var expectedUrl = new Uri ("http://test.com/MyPackage");
			fakePackage.GalleryUrl = expectedUrl;

			bool result = viewModel.HasGalleryUrl;

			Assert.IsTrue (result);
		}

		[Test]
		public void HasNoGalleryUrl_PackageHasNoGalleryUrl_ReturnsFalse ()
		{
			CreateViewModel ();
			fakePackage.GalleryUrl = null;

			bool result = viewModel.HasGalleryUrl;

			Assert.IsFalse (result);
		}

		[Test]
		public void HasNoGalleryUrl_PackageHasGalleryUrl_ReturnsFalse ()
		{
			CreateViewModel ();
			var expectedUrl = new Uri ("http://test.com/MyPackage");
			fakePackage.GalleryUrl = expectedUrl;

			bool result = viewModel.HasNoGalleryUrl;

			Assert.IsFalse (result);
		}

		[Test]
		public void IsGalleryUrlMissing_PackageHasNoGalleryUrl_ReturnsTrue ()
		{
			CreateViewModel ();
			fakePackage.GalleryUrl = null;

			bool result = viewModel.HasNoGalleryUrl;

			Assert.IsTrue (result);
		}

		[Test]
		public void AddPackage_PackageRepositoryIsOperationAwareAndPackageAddedSuccessfully_InstallOperationStartedForPackage ()
		{
			CreateViewModel ();
			var operationAwareRepository = new FakeOperationAwarePackageRepository ();
			fakePackage.FakePackageRepository = operationAwareRepository;
			fakePackage.Id = "MyPackage";

			viewModel.AddPackage ();

			operationAwareRepository.AssertOperationWasStartedAndDisposed (RepositoryOperationNames.Install, "MyPackage");
		}

		[Test]
		public void ManagePackage_TwoProjectsNeitherSelectedAndSourceRepositoryIsOperationAware_InstallOperationStarted ()
		{
			CreateViewModelWithTwoProjectsSelected ("Project A", "Project B");
			UserAcceptsProjectSelection ();
			var operationAwareRepository = new FakeOperationAwarePackageRepository ();
			fakePackage.FakePackageRepository = operationAwareRepository;
			fakePackage.Id = "MyPackage";

			viewModel.ManagePackage ();

			operationAwareRepository.AssertOperationWasStartedAndDisposed (RepositoryOperationNames.Install, "MyPackage");
		}

		[Test]
		public void GetDownloadCountOrVersionDisplayText_PackageDownloadCountIsMinusOne_ReturnsEmptyString ()
		{
			CreateViewModel ();
			fakePackage.DownloadCount = -1;

			string result = viewModel.GetDownloadCountOrVersionDisplayText ();

			Assert.AreEqual (String.Empty, result);
		}

		[Test]
		public void GetDownloadCountOrVersionDisplayText_PackageHasTenThousandDownloads_ReturnsDownloadCountFormattedForLocale ()
		{
			CreateViewModel ();
			fakePackage.DownloadCount = 10000;

			string result = viewModel.GetDownloadCountOrVersionDisplayText ();

			string expectedResult = 10000.ToString ("N0");
			Assert.AreEqual (expectedResult, result);
		}

		[Test]
		public void GetDownloadCountOrVersionDisplayText_PackageWasPartOfGroupIncludingAllVersions_ReturnsVersionNumberInsteadOfDownloadCount ()
		{
			CreateViewModel ();
			viewModel.ShowVersionInsteadOfDownloadCount = true;
			fakePackage.DownloadCount = 10000;
			fakePackage.Version = new SemanticVersion ("1.2.3.4");

			string result = viewModel.GetDownloadCountOrVersionDisplayText ();

			Assert.AreEqual ("1.2.3.4", result);
		}
	}
}

