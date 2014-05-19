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
			taskFactory = new FakeTaskFactory ();
			var packageViewModelFactory = new FakePackageViewModelFactory ();
			recentPackageRepository = new FakeRecentPackageRepository ();

			viewModel = new AvailablePackagesViewModel (
				registeredPackageRepositories,
				recentPackageRepository,
				packageViewModelFactory,
				taskFactory);
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
	}
}
