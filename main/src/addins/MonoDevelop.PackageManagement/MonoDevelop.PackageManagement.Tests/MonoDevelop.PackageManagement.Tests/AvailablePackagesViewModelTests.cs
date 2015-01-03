//
// AvailablePackagesViewModelTests.cs
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

// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

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
	public class AvailablePackagesViewModelTests
	{
		AvailablePackagesViewModel viewModel;
		FakeRegisteredPackageRepositories registeredPackageRepositories;
		ExceptionThrowingRegisteredPackageRepositories exceptionThrowingRegisteredPackageRepositories;
		FakeTaskFactory taskFactory;
		FakeRecentPackageRepository recentPackageRepository;
		FakePackageManagementSolution solution;

		void CreateViewModel ()
		{
			CreateRegisteredPackageRepositories ();
			CreateViewModel (registeredPackageRepositories);
		}

		void CreateRegisteredPackageRepositories ()
		{
			registeredPackageRepositories = new FakeRegisteredPackageRepositories ();
		}

		void CreateViewModel (FakeRegisteredPackageRepositories registeredPackageRepositories)
		{
			CreateSolution ();
			CreateViewModel (registeredPackageRepositories, solution);
		}

		void CreateViewModel (
			FakeRegisteredPackageRepositories registeredPackageRepositories,
			FakePackageManagementSolution solution)
		{
			taskFactory = new FakeTaskFactory ();
			var packageViewModelFactory = new FakePackageViewModelFactory ();
			recentPackageRepository = new FakeRecentPackageRepository ();


			viewModel = new AvailablePackagesViewModel (
				solution,
				registeredPackageRepositories,
				recentPackageRepository,
				packageViewModelFactory,
				taskFactory);
		}

		void CreateSolution ()
		{
			solution = new FakePackageManagementSolution ();
		}

		void CreateExceptionThrowingRegisteredPackageRepositories ()
		{
			exceptionThrowingRegisteredPackageRepositories = new ExceptionThrowingRegisteredPackageRepositories ();
		}

		void CompleteReadPackagesTask ()
		{
			taskFactory.ExecuteAllFakeTasks ();
		}

		void ClearReadPackagesTasks ()
		{
			taskFactory.ClearAllFakeTasks ();
		}

		void AddOnePackageSourceToRegisteredSources ()
		{
			registeredPackageRepositories.ClearPackageSources ();
			registeredPackageRepositories.AddOnePackageSource ();
			registeredPackageRepositories.HasMultiplePackageSources = false;
			registeredPackageRepositories.ActivePackageSource = registeredPackageRepositories.PackageSources [0];
		}

		void AddTwoPackageSourcesToRegisteredSources ()
		{
			var expectedPackageSources = new PackageSource[] {
				new PackageSource ("http://first.com", "First"),
				new PackageSource ("http://second.com", "Second")
			};
			AddPackageSourcesToRegisteredSources (expectedPackageSources);
			registeredPackageRepositories.HasMultiplePackageSources = true;
			registeredPackageRepositories.ActivePackageSource = expectedPackageSources [0];
		}

		void AddPackageSourcesToRegisteredSources (PackageSource[] sources)
		{
			registeredPackageRepositories.ClearPackageSources ();
			registeredPackageRepositories.AddPackageSources (sources);
		}

		PackageSource AddTwoPackageSourcesToRegisteredSourcesWithFirstOneDisabled ()
		{
			var expectedPackageSources = new PackageSource[] {
				new PackageSource ("http://first.com", "First") { IsEnabled = false },
				new PackageSource ("http://second.com", "Second") { IsEnabled = true }
			};
			AddPackageSourcesToRegisteredSources (expectedPackageSources);
			registeredPackageRepositories.HasMultiplePackageSources = true;
			return expectedPackageSources [0];
		}

		void CreateNewActiveRepositoryWithDifferentPackages ()
		{
			var package = new FakePackage ("NewRepositoryPackageId");
			var newRepository = new FakePackageRepository ();
			newRepository.FakePackages.Add (package);
			registeredPackageRepositories.FakeActiveRepository = newRepository;
		}

		void SetUpTwoPackageSourcesAndViewModelHasReadPackages ()
		{
			CreateRegisteredPackageRepositories ();
			AddTwoPackageSourcesToRegisteredSources ();
			CreateViewModel (registeredPackageRepositories);
			registeredPackageRepositories.ActivePackageSource = registeredPackageRepositories.PackageSources [0];
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			CreateNewActiveRepositoryWithDifferentPackages ();
		}

		void ChangeSelectedPackageSourceToSecondSource ()
		{
			var secondPackageSource = registeredPackageRepositories.PackageSources [1];
			viewModel.SelectedPackageSource = secondPackageSource;
		}

		void ChangeSelectedPackageSourceToFirstSource ()
		{
			var firstPackageSource = registeredPackageRepositories.PackageSources [0];
			viewModel.SelectedPackageSource = firstPackageSource;
		}

		void SearchForAllPackageVersions (string packageId, string versions = "")
		{
			viewModel.SearchTerms = string.Format ("{0} version:{1}", packageId, versions).TrimEnd ();
		}

		void AddAggregateRepositoryWithOneFailingRepository ()
		{
			AddAggregateRepository (new ExceptionThrowingPackageRepository (), new FakePackageRepository ());
		}

		void AddAggregateRepository (params IPackageRepository[] repositories)
		{
			var repository = new MonoDevelopAggregateRepository (repositories);
			registeredPackageRepositories.ActivePackageSource = registeredPackageRepositories.PackageSources [0];
			registeredPackageRepositories.GetActiveRepositoryAction = () => {
				return repository;
			};
		}

		void AddAggregateRepositoryWithJustFailingRepositories ()
		{
			AddAggregateRepository (new ExceptionThrowingPackageRepository (), new ExceptionThrowingPackageRepository ());
		}

		void AddAggregateRepositoryWithTwoFailingRepositories (Exception exception1, Exception exception2)
		{
			AddAggregateRepository (
				new ExceptionThrowingPackageRepository (exception1),
				new ExceptionThrowingPackageRepository (exception2));
		}

		FakePackage AddPackageToSolution (string packageId, string packageVersion)
		{
			var package = FakePackage.CreatePackageWithVersion (packageId, packageVersion);
			solution.SolutionPackageRepository.FakePackages.Add (package);
			return package;
		}

		[Test]
		public void ReadPackages_RepositoryHasThreePackagesWithSameIdButDifferentVersions_HasLatestPackageVersionOnly ()
		{
			CreateViewModel ();
			AddOnePackageSourceToRegisteredSources ();

			var package1 = new FakePackage ("Test", "0.1.0.0");
			var package2 = new FakePackage ("Test", "0.2.0.0");
			var package3 = new FakePackage ("Test", "0.3.0.0");

			var packages = new FakePackage[] {
				package1, package2, package3
			};

			registeredPackageRepositories.FakeActiveRepository.FakePackages.AddRange (packages);

			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();

			var expectedPackages = new FakePackage[] {
				package3
			};

			PackageCollectionAssert.AreEqual (expectedPackages, viewModel.PackageViewModels);
		}

		[Test]
		public void IsSearchable_ByDefault_ReturnsTrue ()
		{
			CreateViewModel ();
			Assert.IsTrue (viewModel.IsSearchable);
		}

		[Test]
		public void Search_RepositoryHasThreePackagesWithSameIdButSearchTermsMatchNoPackageIds_ReturnsNoPackages ()
		{
			CreateViewModel ();

			var package1 = new FakePackage ("Test", "0.1.0.0");
			var package2 = new FakePackage ("Test", "0.2.0.0");
			var package3 = new FakePackage ("Test", "0.3.0.0");

			var packages = new FakePackage[] {
				package1, package2, package3
			};

			registeredPackageRepositories.FakeActiveRepository.FakePackages.AddRange (packages);

			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();

			ClearReadPackagesTasks ();
			viewModel.SearchTerms = "NotAMatch";
			viewModel.Search ();
			CompleteReadPackagesTask ();

			Assert.AreEqual (0, viewModel.PackageViewModels.Count);
		}

		[Test]
		public void ShowNextPage_TwoObjectsWatchingForPagesCollectionChangedEventAndUserMovesToPageTwoAndFilteredPackagesReturnsLessThanExpectedPackagesDueToMatchingVersions_InvalidOperationExceptionNotThrownWhen ()
		{
			CreateViewModel ();
			AddOnePackageSourceToRegisteredSources ();
			viewModel.PageSize = 2;

			var package1 = new FakePackage ("First", "0.1.0.0");
			var package2 = new FakePackage ("Second", "0.2.0.0");
			var package3 = new FakePackage ("Test", "0.3.0.0");
			var package4 = new FakePackage ("Test", "0.4.0.0");

			var packages = new FakePackage[] {
				package1, package2, package3, package4
			};

			registeredPackageRepositories.FakeActiveRepository.FakePackages.AddRange (packages);

			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();

			ClearReadPackagesTasks ();
			bool collectionChangedEventFired = false;
			viewModel.Pages.CollectionChanged += (sender, e) => collectionChangedEventFired = true;
			viewModel.ShowNextPage ();
			CompleteReadPackagesTask ();

			var expectedPackages = new FakePackage[] {
				package4
			};

			PackageCollectionAssert.AreEqual (expectedPackages, viewModel.PackageViewModels);
			Assert.IsTrue (collectionChangedEventFired);
		}

		[Test]
		public void ShowSources_TwoPackageSources_ReturnsTrue ()
		{
			CreateRegisteredPackageRepositories ();
			AddTwoPackageSourcesToRegisteredSources ();
			CreateViewModel (registeredPackageRepositories);

			Assert.IsTrue (viewModel.ShowPackageSources);
		}

		[Test]
		public void ShowPackageSources_OnePackageSources_ReturnsTrue ()
		{
			CreateRegisteredPackageRepositories ();
			AddOnePackageSourceToRegisteredSources ();
			CreateViewModel (registeredPackageRepositories);

			Assert.IsTrue (viewModel.ShowPackageSources);
		}

		[Test]
		public void PackageSources_TwoPackageSourcesInOptions_ReturnsTwoPackageSourcesPlusAggregatePackageSource ()
		{
			CreateRegisteredPackageRepositories ();
			AddTwoPackageSourcesToRegisteredSources ();
			CreateViewModel (registeredPackageRepositories);

			var expectedPackageSources = new List<PackageSource> (registeredPackageRepositories.PackageSources);
			expectedPackageSources.Insert (0, RegisteredPackageSourceSettings.AggregatePackageSource);

			PackageSourceCollectionAssert.AreEqual (expectedPackageSources, viewModel.PackageSources);
		}

		[Test]
		public void PackageSources_OnePackageSourceInOptions_ReturnsOnePackageSource ()
		{
			CreateRegisteredPackageRepositories ();
			AddOnePackageSourceToRegisteredSources ();
			CreateViewModel (registeredPackageRepositories);

			var expectedPackageSources = new List<PackageSource> (registeredPackageRepositories.PackageSources);

			PackageSourceCollectionAssert.AreEqual (expectedPackageSources, viewModel.PackageSources);
		}

		[Test]
		public void SelectedPackageSource_TwoPackageSourcesInOptionsAndActivePackageSourceIsFirstSource_IsFirstPackageSource ()
		{
			CreateRegisteredPackageRepositories ();
			AddTwoPackageSourcesToRegisteredSources ();
			CreateViewModel (registeredPackageRepositories);

			var expectedPackageSource = registeredPackageRepositories.PackageSources [0];
			registeredPackageRepositories.ActivePackageSource = expectedPackageSource;

			Assert.AreEqual (expectedPackageSource, viewModel.SelectedPackageSource);
		}

		[Test]
		public void SelectedPackageSource_TwoPackageSourcesInOptionsAndActivePackageSourceIsSecondSource_IsSecondPackageSource ()
		{
			CreateRegisteredPackageRepositories ();
			AddTwoPackageSourcesToRegisteredSources ();
			CreateViewModel (registeredPackageRepositories);

			var expectedPackageSource = registeredPackageRepositories.PackageSources [1];
			registeredPackageRepositories.ActivePackageSource = expectedPackageSource;

			Assert.AreEqual (expectedPackageSource, viewModel.SelectedPackageSource);
		}

		[Test]
		public void SelectedPackageSource_Changed_ActivePackageSourceChanged ()
		{
			CreateRegisteredPackageRepositories ();
			AddTwoPackageSourcesToRegisteredSources ();
			CreateViewModel (registeredPackageRepositories);

			registeredPackageRepositories.ActivePackageSource = registeredPackageRepositories.PackageSources [0];
			var expectedPackageSource = registeredPackageRepositories.PackageSources [1];
			viewModel.SelectedPackageSource = expectedPackageSource;

			Assert.AreEqual (expectedPackageSource, registeredPackageRepositories.ActivePackageSource);
		}

		[Test]
		public void SelectedPackageSource_PackageSourceChangedAfterReadingPackages_PackagesReadFromNewPackageSourceAndDisplayed ()
		{
			SetUpTwoPackageSourcesAndViewModelHasReadPackages ();
			ClearReadPackagesTasks ();
			ChangeSelectedPackageSourceToSecondSource ();
			CompleteReadPackagesTask ();

			var expectedPackages = registeredPackageRepositories.FakeActiveRepository.FakePackages;

			PackageCollectionAssert.AreEqual (expectedPackages, viewModel.PackageViewModels);
		}

		[Test]
		public void SelectedPackageSource_PackageSourceChangedAfterReadingPackages_PropertyChangedEventFiredAfterPackagesAreRead ()
		{
			SetUpTwoPackageSourcesAndViewModelHasReadPackages ();

			int packageCountWhenPropertyChangedEventFired = -1;
			viewModel.PropertyChanged += (sender, e) => packageCountWhenPropertyChangedEventFired = viewModel.PackageViewModels.Count;
			ClearReadPackagesTasks ();
			ChangeSelectedPackageSourceToSecondSource ();
			CompleteReadPackagesTask ();

			Assert.AreEqual (1, packageCountWhenPropertyChangedEventFired);
		}

		[Test]
		public void SelectedPackageSource_PackageSourceChangedButToSameSelectedPackageSource_PackagesAreNotRead ()
		{
			SetUpTwoPackageSourcesAndViewModelHasReadPackages ();
			ChangeSelectedPackageSourceToFirstSource ();

			Assert.AreEqual (0, viewModel.PackageViewModels.Count);
		}

		[Test]
		public void SelectedPackageSource_PackageSourceChangedButToSameSelectedPackageSource_PropertyChangedEventNotFired ()
		{
			SetUpTwoPackageSourcesAndViewModelHasReadPackages ();

			bool fired = false;
			viewModel.PropertyChanged += (sender, e) => fired = true;
			ChangeSelectedPackageSourceToFirstSource ();

			Assert.IsFalse (fired);
		}

		[Test]
		public void GetAllPackages_OnePackageInRepository_RepositoryNotCreatedByBackgroundThread ()
		{
			CreateRegisteredPackageRepositories ();
			AddOnePackageSourceToRegisteredSources ();
			registeredPackageRepositories.FakeActiveRepository.FakePackages.Add (new FakePackage ());
			CreateViewModel (registeredPackageRepositories);
			viewModel.ReadPackages ();

			registeredPackageRepositories.FakeActiveRepository = null;
			CompleteReadPackagesTask ();

			Assert.AreEqual (1, viewModel.PackageViewModels.Count);
		}

		[Test]
		public void ReadPackages_ExceptionThrownWhenAccessingActiveRepository_ErrorMessageFromExceptionNotOverriddenByReadPackagesCall ()
		{
			CreateExceptionThrowingRegisteredPackageRepositories ();
			exceptionThrowingRegisteredPackageRepositories.ExceptionToThrowWhenActiveRepositoryAccessed = 
				new Exception ("Test");
			CreateViewModel (exceptionThrowingRegisteredPackageRepositories);
			exceptionThrowingRegisteredPackageRepositories.ActivePackageSource = new PackageSource ("Test");
			viewModel.ReadPackages ();

			ApplicationException ex = Assert.Throws<ApplicationException> (CompleteReadPackagesTask);
			Assert.AreEqual ("Test", ex.Message);
		}

		[Test]
		public void ReadPackages_RepositoryHasPrereleaseAndReleasePackage_HasReleasePackageOnly ()
		{
			CreateViewModel ();
			AddOnePackageSourceToRegisteredSources ();
			var releasePackage = new FakePackage ("Test", "1.1.0.0");
			var prereleasePackage = new FakePackage ("Test", "1.1.0-alpha");

			var packages = new FakePackage[] {
				releasePackage, prereleasePackage
			};

			registeredPackageRepositories.FakeActiveRepository.FakePackages.AddRange (packages);

			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();

			var expectedPackages = new FakePackage[] {
				releasePackage
			};

			PackageCollectionAssert.AreEqual (expectedPackages, viewModel.PackageViewModels);
		}

		[Test]
		public void ReadPackages_TwoPackagesWithDifferentDownloadCounts_HighestDownloadCountShownFirst ()
		{
			CreateViewModel ();
			AddOnePackageSourceToRegisteredSources ();

			var package1 = new FakePackage ("A", "0.1.0.0") { DownloadCount = 1 };
			var package2 = new FakePackage ("Z", "0.1.0.0") { DownloadCount = 1000 };

			var packages = new FakePackage[] {
				package1, package2
			};

			registeredPackageRepositories.FakeActiveRepository.FakePackages.AddRange (packages);

			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();

			var expectedPackages = new FakePackage[] {
				package2, package1
			};

			PackageCollectionAssert.AreEqual (expectedPackages, viewModel.PackageViewModels);
		}

		[Test]
		public void GetPackagesFromPackageSource_RepositoryHasThreePackagesWithSameIdButDifferentVersions_LatestPackageVersionOnlyRequestedFromPackageSource ()
		{
			CreateViewModel ();
			AddOnePackageSourceToRegisteredSources ();
			var package1 = new FakePackage ("Test", "0.1.0.0") { IsLatestVersion = false };
			var package2 = new FakePackage ("Test", "0.2.0.0") { IsLatestVersion = false };
			var package3 = new FakePackage ("Test", "0.3.0.0") { IsLatestVersion = true };
			var packages = new FakePackage[] {
				package1, package2, package3
			};
			registeredPackageRepositories.FakeActiveRepository.FakePackages.AddRange (packages);
			viewModel.ReadPackages ();

			IList<IPackage> allPackages = viewModel.GetPackagesFromPackageSource ().ToList ();

			var expectedPackages = new FakePackage[] {
				package3
			};
			PackageCollectionAssert.AreEqual (expectedPackages, allPackages);
		}

		[Test]
		public void PackageSources_TwoPackageSourcesButFirstIsDisabled_DoesNotReturnDisabledPackageSource ()
		{
			CreateRegisteredPackageRepositories ();
			AddTwoPackageSourcesToRegisteredSourcesWithFirstOneDisabled ();
			CreateViewModel (registeredPackageRepositories);

			IEnumerable<PackageSource> packageSources = viewModel.PackageSources;

			bool containsDisabledPackageSource = packageSources.Contains (registeredPackageRepositories.PackageSources [0]);
			bool containsEnabledPackageSource = packageSources.Contains (registeredPackageRepositories.PackageSources [1]);
			Assert.IsFalse (containsDisabledPackageSource);
			Assert.IsTrue (containsEnabledPackageSource);
		}

		[Test]
		public void IsInstallAllPackagesEnabled_RepositoryHasTwoPackages_ReturnsFalse ()
		{
			CreateViewModel ();
			var package1 = new FakePackage ("Test", "0.1.0.0");
			var package2 = new FakePackage ("Test", "0.2.0.0");
			var packages = new FakePackage[] {
				package1, package2
			};
			registeredPackageRepositories.FakeActiveRepository.FakePackages.AddRange (packages);
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();

			bool enabled = viewModel.IsUpdateAllPackagesEnabled;

			Assert.IsFalse (enabled);
		}

		[Test]
		public void ShowPrerelease_ByDefault_ReturnsTrue ()
		{
			CreateViewModel ();

			bool show = viewModel.ShowPrerelease;

			Assert.IsTrue (show);
		}

		[Test]
		public void ReadPackages_RepositoryHasPrereleasePackageAndIncludePrereleaseIsTrue_HasPrereleasePackageInList ()
		{
			CreateViewModel ();
			AddOnePackageSourceToRegisteredSources ();
			viewModel.IncludePrerelease = true;
			var prereleasePackage = new FakePackage ("Test", "1.1.0-alpha") {
				IsLatestVersion = false,
				IsAbsoluteLatestVersion = true
			};
			var expectedPackages = new FakePackage[] { prereleasePackage };
			registeredPackageRepositories.FakeActiveRepository.FakePackages.AddRange (expectedPackages);

			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();

			PackageCollectionAssert.AreEqual (expectedPackages, viewModel.PackageViewModels);
		}

		[Test]
		public void ReadPackages_RepositoryHasThreePrereleasePackagesWithSameIdButDifferentVersionsAndIncludePrereleaseIsTrue_HasLatestPreleasePackageVersionOnly ()
		{
			CreateViewModel ();
			AddOnePackageSourceToRegisteredSources ();
			viewModel.IncludePrerelease = true;
			var package1 = new FakePackage ("Test", "0.1.0.0-alpha") { IsLatestVersion = false };
			var package2 = new FakePackage ("Test", "0.2.0.0-alpha") { IsLatestVersion = false };
			var package3 = new FakePackage ("Test", "0.3.0.0-alpha") { IsLatestVersion = false, IsAbsoluteLatestVersion = true };
			var packages = new FakePackage[] {
				package1, package2, package3
			};
			registeredPackageRepositories.FakeActiveRepository.FakePackages.AddRange (packages);

			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();

			var expectedPackages = new FakePackage[] {
				package3
			};
			PackageCollectionAssert.AreEqual (expectedPackages, viewModel.PackageViewModels);
		}

		[Test]
		public void PackageViewModels_GetParentOfPackageViewModel_ReturnsAvailablePackagesViewModel ()
		{
			CreateViewModel ();
			AddOnePackageSourceToRegisteredSources ();
			var package1 = new FakePackage ("Test", "0.1.0.0");
			var package2 = new FakePackage ("Test", "0.2.0.0");
			var packages = new FakePackage[] {
				package1, package2
			};
			registeredPackageRepositories.FakeActiveRepository.FakePackages.AddRange (packages);
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			PackageViewModel childViewModel = viewModel.PackageViewModels.First ();

			IPackageViewModelParent parent = childViewModel.GetParent ();

			Assert.AreEqual (viewModel, parent);
		}

		[Test]
		public void GetPackagesFromPackageSource_RepositoryIsServiceBasedRepository_ServiceBasedRepositorySearchUsed ()
		{
			CreateViewModel ();
			AddOnePackageSourceToRegisteredSources ();
			var package = FakePackage.CreatePackageWithVersion ("Test", "0.1.0.0");
			var packages = new FakePackage[] { package };
			var repository = new FakeServiceBasedRepository ();
			repository.PackagesToReturnForSearch ("id:test", false, packages);
			registeredPackageRepositories.FakeActiveRepository = repository;
			viewModel.SearchTerms = "id:test";
			viewModel.IncludePrerelease = false;
			viewModel.ReadPackages ();

			IList<IPackage> allPackages = viewModel.GetPackagesFromPackageSource ().ToList ();

			var expectedPackages = new FakePackage[] { package };
			PackageCollectionAssert.AreEqual (expectedPackages, allPackages);
		}

		[Test]
		public void GetPackagesFromPackageSource_RepositoryIsServiceBasedRepositoryAndPrereleaseIncluded_ServiceBasedRepositorySearchUsed ()
		{
			CreateViewModel ();
			AddOnePackageSourceToRegisteredSources ();
			var package = FakePackage.CreatePackageWithVersion ("Test", "0.1.0.0");
			package.IsAbsoluteLatestVersion = true;
			var packages = new FakePackage[] { package };
			var repository = new FakeServiceBasedRepository ();
			repository.PackagesToReturnForSearch ("id:test", true, packages);
			registeredPackageRepositories.FakeActiveRepository = repository;
			viewModel.SearchTerms = "id:test";
			viewModel.IncludePrerelease = true;
			viewModel.ReadPackages ();

			IList<IPackage> allPackages = viewModel.GetPackagesFromPackageSource ().ToList ();

			var expectedPackages = new FakePackage[] { package };
			PackageCollectionAssert.AreEqual (expectedPackages, allPackages);
		}

		[Test]
		public void GetPackagesFromPackageSource_RepositoryHasThreePackagesWithSameIdButDifferentVersionsAndSearchIncludesPrerelease_AbsoluteLatestPackageVersionOnlyRequestedFromPackageSource ()
		{
			CreateViewModel ();
			AddOnePackageSourceToRegisteredSources ();
			var package1 = new FakePackage ("Test", "0.1.0.0") { IsAbsoluteLatestVersion = false };
			var package2 = new FakePackage ("Test", "0.2.0.0") { IsAbsoluteLatestVersion = false };
			var package3 = new FakePackage ("Test", "0.3.0.0") { IsAbsoluteLatestVersion = true };
			var packages = new FakePackage[] {
				package1, package2, package3
			};
			registeredPackageRepositories.FakeActiveRepository.FakePackages.AddRange (packages);
			viewModel.IncludePrerelease = true;
			viewModel.ReadPackages ();

			IList<IPackage> allPackages = viewModel.GetPackagesFromPackageSource ().ToList ();

			var expectedPackages = new FakePackage[] {
				package3
			};
			PackageCollectionAssert.AreEqual (expectedPackages, allPackages);
		}

		[Test]
		public void ReadPackages_ActiveRepositoryChangedWhichUsesInvalidUrl_InvalidUrlExceptionIsShownAsErrorMessage ()
		{
			CreateExceptionThrowingRegisteredPackageRepositories ();
			CreateViewModel (exceptionThrowingRegisteredPackageRepositories);
			exceptionThrowingRegisteredPackageRepositories.ActivePackageSource = new PackageSource ("test");
			var package = new FakePackage ("Test", "0.1.0.0");
			exceptionThrowingRegisteredPackageRepositories
				.FakeActiveRepository
				.FakePackages
				.Add (package);
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			taskFactory.ClearAllFakeTasks ();
			exceptionThrowingRegisteredPackageRepositories.ExceptionToThrowWhenActiveRepositoryAccessed =
				new Exception ("Invalid url");

			viewModel.ReadPackages ();
			FakeTask<PackagesForSelectedPageResult> task = taskFactory.FirstFakeTaskCreated;
			ApplicationException ex = Assert.Throws<ApplicationException> (() => task.ExecuteTaskButNotContinueWith ());
			task.Exception = new AggregateException (ex);
			task.IsFaulted = true;
			task.ExecuteContinueWith ();

			Assert.AreEqual ("Invalid url", ex.Message);
			Assert.IsTrue (viewModel.HasError);
			Assert.AreEqual ("Invalid url", viewModel.ErrorMessage);
		}

		[Test]
		public void ReadPackages_SearchForAllPackageVersionsWhenThreePackageVersionsAvailable_ShowsOnlyPackagesWithSameIdAndAllVersionsWithHighestVersionFirst ()
		{
			CreateViewModel ();
			AddOnePackageSourceToRegisteredSources ();
			var package1 = new FakePackage ("A", "0.1.0.0") { IsLatestVersion = false };
			var package2 = new FakePackage ("A", "0.3.0.0");
			var package3 = new FakePackage ("A", "0.2.0.0") { IsLatestVersion = false };
			var package4 = new FakePackage ("AA", "0.1.0.0");
			var packages = new FakePackage[] {
				package1, package2, package3, package4
			};
			registeredPackageRepositories.FakeActiveRepository.FakePackages.AddRange (packages);
			SearchForAllPackageVersions ("a");

			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();

			var expectedPackages = new FakePackage[] {
				package2, package3, package1
			};
			PackageCollectionAssert.AreEqual (expectedPackages, viewModel.PackageViewModels);
		}

		[Test]
		public void ReadPackages_SearchForAllPackageVersionsWhenThreePackageVersionsAvailableButOneIsPrerelease_ShowsOnlyNonPrereleasePackages ()
		{
			CreateViewModel ();
			AddOnePackageSourceToRegisteredSources ();
			var package1 = new FakePackage ("A", "0.1.0.0") { IsLatestVersion = false };
			var package2 = new FakePackage ("A", "0.2.0.0");
			var package3 = new FakePackage ("A", "0.2.0-alpha") { IsLatestVersion = false };
			var packages = new FakePackage[] {
				package1, package2, package3
			};
			registeredPackageRepositories.FakeActiveRepository.FakePackages.AddRange (packages);
			SearchForAllPackageVersions ("a");

			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();

			var expectedPackages = new FakePackage[] {
				package2, package1
			};
			PackageCollectionAssert.AreEqual (expectedPackages, viewModel.PackageViewModels);
		}

		[Test]
		public void ReadPackages_SearchForAllPackageVersionsIncludePrereleaseWhenOneIsPrereleaseAndOneIsRelease_ShowsAllPackages ()
		{
			CreateViewModel ();
			AddOnePackageSourceToRegisteredSources ();
			var package1 = new FakePackage ("A", "0.2.0.0");
			var package2 = new FakePackage ("A", "0.2.0-alpha") { IsLatestVersion = false };
			var packages = new FakePackage[] {
				 package1, package2
			};
			registeredPackageRepositories.FakeActiveRepository.FakePackages.AddRange (packages);
			SearchForAllPackageVersions ("a");
			viewModel.IncludePrerelease = true;

			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();

			var expectedPackages = new FakePackage[] {
				package1, package2
			};
			PackageCollectionAssert.AreEqual (expectedPackages, viewModel.PackageViewModels);
		}

		[Test]
		public void ReadPackages_SearchForAllPackageVersionsWhenOneRecentPackageIsAvailable_RecentPackagesNotDisplayed ()
		{
			CreateViewModel ();
			AddOnePackageSourceToRegisteredSources ();
			var package1 = new FakePackage ("A", "0.1.0.0") { IsLatestVersion = false };
			var package2 = new FakePackage ("A", "0.2.0.0");
			var packages = new FakePackage[] {
				package1, package2
			};
			registeredPackageRepositories.FakeActiveRepository.FakePackages.AddRange (packages);
			var recentPackage = new FakePackage ("A", "0.2.0.0") {
				Description = "A -version"
			};
			recentPackageRepository.AddPackage (recentPackage);
			SearchForAllPackageVersions ("a");

			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();

			var expectedPackages = new FakePackage[] {
				package2, package1
			};
			PackageCollectionAssert.AreEqual (expectedPackages, viewModel.PackageViewModels);
		}

		[Test]
		public void ReadPackages_SearchForAllPackageVersions_ShowVersionInsteadOfDownloadCountIsTrueForEachPackageViewModel ()
		{
			CreateViewModel ();
			AddOnePackageSourceToRegisteredSources ();
			var package1 = new FakePackage ("A", "0.1.0.0") { IsLatestVersion = false };
			var package2 = new FakePackage ("A", "0.2.0.0");
			var packages = new FakePackage[] {
				package1, package2
			};
			registeredPackageRepositories.FakeActiveRepository.FakePackages.AddRange (packages);
			SearchForAllPackageVersions ("a");

			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();

			Assert.IsTrue (viewModel.PackageViewModels [0].ShowVersionInsteadOfDownloadCount);
			Assert.IsTrue (viewModel.PackageViewModels [1].ShowVersionInsteadOfDownloadCount);
		}

		[Test]
		public void ReadPackages_SearchForPackage_ShowVersionInsteadOfDownloadCountIsFalseForEachPackageViewModel ()
		{
			CreateViewModel ();
			AddOnePackageSourceToRegisteredSources ();
			var package1 = new FakePackage ("A", "0.1.0.0");
			var package2 = new FakePackage ("B", "0.2.0.0");
			var packages = new FakePackage[] {
				package1, package2
			};
			registeredPackageRepositories.FakeActiveRepository.FakePackages.AddRange (packages);

			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();

			Assert.IsFalse (viewModel.PackageViewModels [0].ShowVersionInsteadOfDownloadCount);
			Assert.IsFalse (viewModel.PackageViewModels [1].ShowVersionInsteadOfDownloadCount);
		}

		[Test]
		public void ReadPackages_SearchForSinglePackageVersionWhenThreePackageVersionsAvailable_ShowsSinglePackagesWithSameIdAndSameVersion ()
		{
			CreateViewModel ();
			AddOnePackageSourceToRegisteredSources ();
			var package1 = new FakePackage ("A", "0.1.0.0") { IsLatestVersion = false };
			var package2 = new FakePackage ("A", "0.3.0.0");
			var package3 = new FakePackage ("A", "0.2.0.0") { IsLatestVersion = false };
			var packages = new FakePackage[] {
				package1, package2, package3
			};
			registeredPackageRepositories.FakeActiveRepository.FakePackages.AddRange (packages);
			SearchForAllPackageVersions ("a", "0.2");

			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();

			var expectedPackages = new FakePackage[] {
				package3
			};
			PackageCollectionAssert.AreEqual (expectedPackages, viewModel.PackageViewModels);
		}

		[Test]
		public void ReadPackages_SearchForAllPackageVersionsUsingAsteriskWhenThreePackageVersionsAvailable_HasAllPackageVersions ()
		{
			CreateViewModel ();
			AddOnePackageSourceToRegisteredSources ();
			var package1 = new FakePackage ("A", "0.1.0.0") { IsLatestVersion = false };
			var package2 = new FakePackage ("A", "0.3.0.0");
			var package3 = new FakePackage ("A", "0.2.0.0") { IsLatestVersion = false };
			var packages = new FakePackage[] {
				package1, package2, package3
			};
			registeredPackageRepositories.FakeActiveRepository.FakePackages.AddRange (packages);
			SearchForAllPackageVersions ("a", "*");

			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();

			var expectedPackages = new FakePackage[] {
				package2, package3, package1
			};
			PackageCollectionAssert.AreEqual (expectedPackages, viewModel.PackageViewModels);
		}

		[Test]
		public void ReadPackages_SearchForAllOnePointZeroPackageVersionsUsingVersionOne_ReturnsAllOnePointZeroVersions ()
		{
			CreateViewModel ();
			AddOnePackageSourceToRegisteredSources ();
			var package1 = new FakePackage ("A", "1.0.0.0") { IsLatestVersion = false };
			var package2 = new FakePackage ("A", "2.1.0.0");
			var package3 = new FakePackage ("A", "2.0.0.0") { IsLatestVersion = false };
			var package4 = new FakePackage ("A", "1.1.0.0") { IsLatestVersion = false };
			var package5 = new FakePackage ("A", "1.9.0.0") { IsLatestVersion = false };
			var package6 = new FakePackage ("A", "1.2.0.0") { IsLatestVersion = false };
			var package7 = new FakePackage ("A", "1.3.0.0") { IsLatestVersion = false };
			var package8 = new FakePackage ("A", "0.1.0.0") { IsLatestVersion = false };
			var packages = new FakePackage[] {
				package1, package2, package3, package4, package5, package6, package7, package8
			};
			registeredPackageRepositories.FakeActiveRepository.FakePackages.AddRange (packages);
			SearchForAllPackageVersions ("a", "1");

			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();

			var expectedPackages = new FakePackage[] {
				package5, package7, package6, package4, package1
			};
			PackageCollectionAssert.AreEqual (expectedPackages, viewModel.PackageViewModels);
		}

		[Test]
		public void ReadPackages_ActivePackageSourceIsAggregatePackageRepositoryWithOneFailingRepository_HasErrorIsTrueAndErrorMessageHasWarning ()
		{
			CreateRegisteredPackageRepositories ();
			AddAggregateRepositoryWithOneFailingRepository ();
			CreateViewModel (registeredPackageRepositories);

			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();

			Assert.IsTrue (viewModel.HasError);
			Assert.That (viewModel.ErrorMessage, Contains.Substring ("Some package sources could not be reached."));
		}

		[Test]
		public void ReadPackages_ActivePackageSourceIsAggregatePackageRepositoryWithAllRepositoriesFailing_HasErrorIsTrueAndErrorMessageHasWarning ()
		{
			CreateRegisteredPackageRepositories ();
			AddAggregateRepositoryWithJustFailingRepositories ();
			CreateViewModel (registeredPackageRepositories);

			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();

			Assert.IsTrue (viewModel.HasError);
			Assert.That (viewModel.ErrorMessage, Contains.Substring ("All package sources could not be reached."));
		}

		[Test]
		public void ReadPackages_ActivePackageSourceIsAggregatePackageRepositoryWithAllRepositoriesFailing_RepositoryErrorIsDisplayed ()
		{
			CreateRegisteredPackageRepositories ();
			var exception1 = new Exception ("Error1");
			var exception2 = new Exception ("Error2");
			AddAggregateRepositoryWithTwoFailingRepositories (exception1, exception2);
			CreateViewModel (registeredPackageRepositories);

			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();

			Assert.IsTrue (viewModel.HasError);
			Assert.That (viewModel.ErrorMessage, Contains.Substring ("Error1"));
			Assert.That (viewModel.ErrorMessage, Contains.Substring ("Error2"));
		}

		[Test]
		public void ReadPackages_ActivePackageSourceIsAggregatePackageRepositoryWithOneFailingRepository_RepositoryErrorIsDisplayed ()
		{
			CreateRegisteredPackageRepositories ();
			var repository = new ExceptionThrowingPackageRepository (new Exception ("Error1"));
			AddAggregateRepository (new FakePackageRepository (), repository);
			CreateViewModel (registeredPackageRepositories);

			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();

			Assert.IsTrue (viewModel.HasError);
			Assert.That (viewModel.ErrorMessage, Contains.Substring ("Error1"));
		}

		[Test]
		public void ReadPackages_OneRecentPackageIsAvailable_RecentPackageIsDisplayedBeforeAnyOtherPackages ()
		{
			CreateViewModel ();
			AddOnePackageSourceToRegisteredSources ();
			var package1 = new FakePackage ("A", "0.2.0.0");
			var package2 = new FakePackage ("Aa", "0.3.0.0");
			var packages = new [] {
				package1, package2
			};
			registeredPackageRepositories.FakeActiveRepository.FakePackages.AddRange (packages);
			var recentPackage = new FakePackage ("B", "1.0.0.0");
			recentPackageRepository.AddPackage (recentPackage);

			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();

			var expectedPackages = new [] {
				recentPackage, package1, package2
			};
			PackageCollectionAssert.AreEqual (expectedPackages, viewModel.PackageViewModels);
		}

		[Test]
		public void ReadPackages_OneRecentPackageIsAvailableWhichMatchesPackageFromActiveSource_DuplicatePackageWithSameVersionFromActivePackageSourceIsNotDisplayed ()
		{
			CreateViewModel ();
			AddOnePackageSourceToRegisteredSources ();
			var package1 = new FakePackage ("A", "1.0.0.0");
			var package2 = new FakePackage ("B", "0.3.0.0");
			var packages = new [] {
				package1, package2
			};
			registeredPackageRepositories.FakeActiveRepository.FakePackages.AddRange (packages);
			var recentPackage = new FakePackage ("A", "1.0.0.0");
			recentPackageRepository.AddPackage (recentPackage);

			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();

			var expectedPackages = new [] {
				recentPackage,  package2
			};
			PackageCollectionAssert.AreEqual (expectedPackages, viewModel.PackageViewModels);
		}

		[Test]
		public void ReadPackages_TwoRecentPackageAndSearchTextEntered_RecentPackagesAreFilteredBySearch ()
		{
			CreateViewModel ();
			AddOnePackageSourceToRegisteredSources ();
			var package1 = new FakePackage ("A", "1.0.0.0");
			var package2 = new FakePackage ("B", "0.3.0.0");
			var packages = new [] {
				package1, package2
			};
			registeredPackageRepositories.FakeActiveRepository.FakePackages.AddRange (packages);
			var recentPackage = new FakePackage ("Aa", "1.0.0.0");
			recentPackageRepository.AddPackage (recentPackage);
			recentPackageRepository.AddPackage (new FakePackage ("Bb", "1.0.0.0"));
			viewModel.SearchTerms = "a";

			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();

			var expectedPackages = new [] {
				recentPackage,  package1
			};
			PackageCollectionAssert.AreEqual (expectedPackages, viewModel.PackageViewModels);
		}

		[Test]
		public void ReadPackages_SolutionHasOnePackageInstalled_SolutionPackageDisplayedBeforeActivePackageSourcePackages ()
		{
			CreateViewModel ();
			AddOnePackageSourceToRegisteredSources ();
			var package1 = new FakePackage ("A", "1.0.0.0");
			var package2 = new FakePackage ("B", "0.3.0.0");
			var packages = new [] {
				package1, package2
			};
			registeredPackageRepositories.FakeActiveRepository.FakePackages.AddRange (packages);
			FakePackage installedPackage = AddPackageToSolution ("ZZ", "1.0.0.0");

			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();

			var expectedPackages = new [] {
				installedPackage,  package1, package2
			};
			PackageCollectionAssert.AreEqual (expectedPackages, viewModel.PackageViewModels);
		}

		[Test]
		public void ReadPackages_SolutionHasOnePackageInstalledAndSecondPageOfPackagesIsReadWhenInfiniteScrollIsEnabled_SolutionPackageIsNotAddedTwice ()
		{
			CreateViewModel ();
			AddOnePackageSourceToRegisteredSources ();
			var package1 = new FakePackage ("A", "1.0.0.0");
			var package2 = new FakePackage ("B", "0.3.0.0");
			var package3 = new FakePackage ("C", "0.1.0.0");
			var package4 = new FakePackage ("D", "0.1.0.0");
			var packages = new [] {
				package1, package2, package3, package4
			};
			registeredPackageRepositories.FakeActiveRepository.FakePackages.AddRange (packages);
			FakePackage installedPackage = AddPackageToSolution ("ZZ", "1.0.0.0");
			viewModel.PageSize = 2;
			viewModel.ClearPackagesOnPaging = false;
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			viewModel.ShowNextPage ();
			CompleteReadPackagesTask ();

			var expectedPackages = new [] {
				installedPackage,  package1, package2, package3, package4
			};
			PackageCollectionAssert.AreEqual (expectedPackages, viewModel.PackageViewModels);
		}

		[Test]
		public void ReadPackages_OneSolutionPackageMatchesPackageFromActiveSource_DuplicatePackageWithSameVersionFromActiveSourceIsNotDisplayed ()
		{
			CreateViewModel ();
			AddOnePackageSourceToRegisteredSources ();
			var package1 = new FakePackage ("A", "1.0.0.0");
			var package2 = new FakePackage ("B", "0.3.0.0");
			var packages = new [] {
				package1, package2
			};
			registeredPackageRepositories.FakeActiveRepository.FakePackages.AddRange (packages);
			FakePackage installedPackage = AddPackageToSolution ("A", "1.0.0.0");

			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();

			var expectedPackages = new [] {
				installedPackage,  package2
			};
			PackageCollectionAssert.AreEqual (expectedPackages, viewModel.PackageViewModels);
		}

		[Test]
		public void ReadPackages_OneSolutionPackageMatchesRecentPackage_DuplicateSolutionPackageWithSameVersionIsNotDisplayed ()
		{
			CreateViewModel ();
			AddOnePackageSourceToRegisteredSources ();
			var package1 = new FakePackage ("A", "1.0.0.0");
			var package2 = new FakePackage ("B", "0.3.0.0");
			var packages = new [] {
				package1, package2
			};
			var recentPackage = new FakePackage ("A", "1.0.0.0");
			recentPackageRepository.AddPackage (recentPackage);
			registeredPackageRepositories.FakeActiveRepository.FakePackages.AddRange (packages);
			FakePackage installedPackage = AddPackageToSolution ("A", "1.0.0.0");

			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();

			var expectedPackages = new [] {
				recentPackage,  package2
			};
			PackageCollectionAssert.AreEqual (expectedPackages, viewModel.PackageViewModels);
		}

		[Test]
		public void ReadPackages_SearchForAllPackageVersionsWhenOneSolutionPackageAvailable_SolutionPackageIsNotDisplayed ()
		{
			CreateViewModel ();
			AddOnePackageSourceToRegisteredSources ();
			var package1 = new FakePackage ("A", "0.1.0.0") { IsLatestVersion = false };
			var package2 = new FakePackage ("A", "0.2.0.0");
			FakePackage installedPackage = AddPackageToSolution ("A", "1.0.0.0");
			var packages = new [] { package1, package2 };
			registeredPackageRepositories.FakeActiveRepository.FakePackages.AddRange (packages);
			SearchForAllPackageVersions ("A");

			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();

			var expectedPackages = new FakePackage[] {
				package2, package1
			};
			PackageCollectionAssert.AreEqual (expectedPackages, viewModel.PackageViewModels);
		}

		[Test]
		public void ReadPackages_TwoSolutionPackagesAndSearchTextEntered_SolutionPackagesAreFilteredBySearch ()
		{
			CreateViewModel ();
			AddOnePackageSourceToRegisteredSources ();
			var package1 = new FakePackage ("A", "1.0.0.0");
			var package2 = new FakePackage ("B", "0.3.0.0");
			var packages = new [] {
				package1, package2
			};
			registeredPackageRepositories.FakeActiveRepository.FakePackages.AddRange (packages);
			FakePackage installedPackage = AddPackageToSolution ("Aa", "1.0.0.0");
			AddPackageToSolution ("Bb", "1.0.0.0");
			viewModel.SearchTerms = "a";

			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();

			var expectedPackages = new [] {
				installedPackage,  package1
			};
			PackageCollectionAssert.AreEqual (expectedPackages, viewModel.PackageViewModels);
		}

		[Test]
		public void ReadPackages_TwoSolutionPackagesAndSecondOneThrowsExceptionWhenBeingReturned_ExceptionHandled ()
		{
			CreateSolution ();
			var solutionPackageRepository = new ExceptionThrowingSolutionPackageRepository {
				ThrowExceptionOnIteration = 1
			};
			solution.SolutionPackageRepository = solutionPackageRepository;
			CreateRegisteredPackageRepositories ();
			CreateViewModel (registeredPackageRepositories, solution);
			AddOnePackageSourceToRegisteredSources ();
			var package1 = new FakePackage ("A", "1.0.0.0");
			var package2 = new FakePackage ("B", "0.3.0.0");
			var packages = new [] {
				package1, package2
			};
			registeredPackageRepositories.FakeActiveRepository.FakePackages.AddRange (packages);
			AddPackageToSolution ("Aa", "1.0.0.0");
			AddPackageToSolution ("Bb", "1.0.0.0");

			viewModel.ReadPackages ();
			Assert.DoesNotThrow (() => CompleteReadPackagesTask ());

			PackageCollectionAssert.AreEqual (packages, viewModel.PackageViewModels);
		}
	}
}
