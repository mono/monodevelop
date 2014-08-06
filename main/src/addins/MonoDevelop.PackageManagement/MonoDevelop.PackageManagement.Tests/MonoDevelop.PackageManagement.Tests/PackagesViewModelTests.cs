//
// PackagesViewModelTests.cs
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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using ICSharpCode.PackageManagement;
using MonoDevelop.PackageManagement.Tests.Helpers;
using NuGet;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class PackagesViewModelTests
	{
		TestablePackagesViewModel viewModel;
		FakeTaskFactory taskFactory;
		FakeRegisteredPackageRepositories registeredPackageRepositories;

		void CreateViewModel (FakeRegisteredPackageRepositories registeredPackageRepositories)
		{
			viewModel = new TestablePackagesViewModel (registeredPackageRepositories);
			registeredPackageRepositories.ActivePackageSource = registeredPackageRepositories.PackageSources [0];
			this.registeredPackageRepositories = registeredPackageRepositories;
			taskFactory = viewModel.FakeTaskFactory;
		}

		void CreateViewModel ()
		{
			CreateRegisteredRepositoriesService ();
			CreateViewModel (registeredPackageRepositories);
		}

		void CreateRegisteredRepositoriesService ()
		{
			registeredPackageRepositories = new FakeRegisteredPackageRepositories ();
		}

		void CompleteReadPackagesTask ()
		{
			taskFactory.ExecuteAllFakeTasks ();
		}

		void ClearReadPackagesTasks ()
		{
			taskFactory.ClearAllFakeTasks ();
		}

		[Test]
		public void IsPaged_OnePackageAndPageSizeIsFive_ReturnsFalse ()
		{
			CreateViewModel ();
			viewModel.PageSize = 5;
			viewModel.AddOneFakePackage ();
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			
			bool paged = viewModel.IsPaged;
			
			Assert.IsFalse (paged);
		}

		[Test]
		public void IsPaged_SixPackagesAndPageSizeIsFive_ReturnsTrue ()
		{
			CreateViewModel ();
			viewModel.PageSize = 5;
			viewModel.AddSixFakePackages ();
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			
			bool paged = viewModel.IsPaged;
			
			Assert.IsTrue (paged);
		}

		[Test]
		public void SelectedPageNumber_ByDefault_ReturnsOne ()
		{
			CreateViewModel ();
			
			int pageNumber = viewModel.SelectedPageNumber;
			
			Assert.AreEqual (1, pageNumber);
		}

		[Test]
		public void HasPreviousPage_SixPackagesSelectedPageNumberIsOneAndPageSizeIsFive_ReturnsFalse ()
		{
			CreateViewModel ();
			viewModel.PageSize = 5;
			viewModel.AddSixFakePackages ();
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			viewModel.SelectedPageNumber = 1;
			
			Assert.IsFalse (viewModel.HasPreviousPage);
		}

		[Test]
		public void HasPreviousPage_SixPackagesSelectedPageNumberIsTwoAndPageSizeIsFive_ReturnsTrue ()
		{
			CreateViewModel ();
			viewModel.PageSize = 5;
			viewModel.AddSixFakePackages ();
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			viewModel.SelectedPageNumber = 2;
			
			Assert.IsTrue (viewModel.HasPreviousPage);
		}

		[Test]
		public void HasPreviousPage_SelectedPagesChangesFromFirstPageToSecond_PropertyChangedEventFiredForAllProperties ()
		{
			CreateViewModel ();
			viewModel.PageSize = 5;
			viewModel.AddSixFakePackages ();
			viewModel.SelectedPageNumber = 1;
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			
			PropertyChangedEventArgs propertyChangedEvent = null;
			viewModel.PropertyChanged += (sender, e) => propertyChangedEvent = e;
			viewModel.SelectedPageNumber = 2;
			
			string propertyName = propertyChangedEvent.PropertyName;
			
			Assert.IsNull (propertyName);
		}

		[Test]
		public void HasNextPage_SixPackagesSelectedPageNumberIsOneAndPageSizeIsFive_ReturnsTrue ()
		{
			CreateViewModel ();
			viewModel.PageSize = 5;
			viewModel.AddSixFakePackages ();
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			viewModel.SelectedPageNumber = 1;
			
			Assert.IsTrue (viewModel.HasNextPage);
		}

		[Test]
		public void HasNextPage_SixPackagesSelectedPageNumberIsTwoAndPageSizeIsFive_ReturnsFalse ()
		{
			CreateViewModel ();
			viewModel.PageSize = 5;
			viewModel.AddSixFakePackages ();
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			viewModel.SelectedPageNumber = 2;
			
			Assert.IsFalse (viewModel.HasNextPage);
		}

		[Test]
		public void HasNextPage_SixPackagesSelectedPageNumberIsTwoAndPageSizeIsTwo_ReturnsTrue ()
		{
			CreateViewModel ();
			viewModel.PageSize = 2;
			viewModel.AddSixFakePackages ();
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			viewModel.SelectedPageNumber = 2;
			
			Assert.IsTrue (viewModel.HasNextPage);
		}

		[Test]
		public void Pages_SixPackagesSelectedPageNumberIsTwoAndPageSizeIsFive_ReturnsTwoPagesWithSecondOneSelected ()
		{
			CreateViewModel ();
			viewModel.PageSize = 5;
			viewModel.AddSixFakePackages ();
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			viewModel.SelectedPageNumber = 2;
			
			Page[] expectedPages = new Page[] {
				new Page () { Number = 1 },
				new Page () { Number = 2, IsSelected = true }
			};
			
			var actualPages = viewModel.Pages;
			
			PageCollectionAssert.AreEqual (expectedPages, actualPages);
		}

		[Test]
		public void Pages_SixPackagesSelectedPageNumberIsOneAndPageSizeIsFive_ReturnsTwoPagesWithFirstOneSelected ()
		{
			CreateViewModel ();
			viewModel.PageSize = 5;
			viewModel.AddSixFakePackages ();
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			viewModel.SelectedPageNumber = 1;
			
			Page[] expectedPages = new Page[] {
				new Page () { Number = 1, IsSelected = true },
				new Page () { Number = 2 }
			};
			
			var actualPages = viewModel.Pages;
			
			PageCollectionAssert.AreEqual (expectedPages, actualPages);
		}

		[Test]
		public void Pages_SixPackagesSelectedPageNumberIsOneAndPageSizeIsTwo_ReturnsThreePagesWithFirstOneSelected ()
		{
			CreateViewModel ();
			viewModel.PageSize = 2;
			viewModel.AddSixFakePackages ();
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			viewModel.SelectedPageNumber = 1;
			
			Page[] expectedPages = new Page[] {
				new Page () { Number = 1, IsSelected = true },
				new Page () { Number = 2 },
				new Page () { Number = 3 }
			};
			
			var actualPages = viewModel.Pages;
			
			PageCollectionAssert.AreEqual (expectedPages, actualPages);
		}

		[Test]
		public void Pages_SixPackagesSelectedPageNumberIsOneAndPageSizeIsTwoAndMaximumSelectablePagesIsTwo_ReturnsTwoPagesWithFirstOneSelected ()
		{
			CreateViewModel ();
			viewModel.PageSize = 2;
			viewModel.AddSixFakePackages ();
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			viewModel.SelectedPageNumber = 1;
			viewModel.MaximumSelectablePages = 2;
			
			Page[] expectedPages = new Page[] {
				new Page () { Number = 1, IsSelected = true },
				new Page () { Number = 2 }
			};
			
			var actualPages = viewModel.Pages;
			
			PageCollectionAssert.AreEqual (expectedPages, actualPages);
		}

		[Test]
		public void Pages_SixPackagesSelectedPageNumberIsOneAndPageSizeIsFiveGetPagesTwice_ReturnsTwoPagesWithFirstOneSelected ()
		{
			CreateViewModel ();
			viewModel.PageSize = 5;
			viewModel.AddSixFakePackages ();
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			viewModel.SelectedPageNumber = 1;
			
			Page[] expectedPages = new Page[] {
				new Page () { Number = 1, IsSelected = true },
				new Page () { Number = 2 }
			};
			
			var actualPages = viewModel.Pages;
			actualPages = viewModel.Pages;
			
			PageCollectionAssert.AreEqual (expectedPages, actualPages);
		}

		[Test]
		public void Pages_SixPackagesSelectedPageNumberIsThreeAndPageSizeIsTwoAndMaximumSelectablePagesIsTwo_ReturnsPagesTwoAndThreeWithPageThreeSelected ()
		{
			CreateViewModel ();
			viewModel.PageSize = 2;
			viewModel.AddSixFakePackages ();
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			viewModel.SelectedPageNumber = 3;
			viewModel.MaximumSelectablePages = 2;
			
			Page[] expectedPages = new Page[] {
				new Page () { Number = 2 },
				new Page () { Number = 3, IsSelected = true }
			};
			
			var actualPages = viewModel.Pages;
			
			PageCollectionAssert.AreEqual (expectedPages, actualPages);
		}

		[Test]
		public void Pages_TenPackagesSelectedPageNumberIsFiveAndPageSizeIsTwoAndMaximumSelectablePagesIsThree_ReturnsPagesThreeAndFourAndFive ()
		{
			CreateViewModel ();
			viewModel.PageSize = 2;
			viewModel.AddTenFakePackages ();
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			viewModel.SelectedPageNumber = 5;
			viewModel.MaximumSelectablePages = 3;
			
			Page[] expectedPages = new Page[] {
				new Page () { Number = 3 },
				new Page () { Number = 4 },
				new Page () { Number = 5, IsSelected = true }
			};
			
			var actualPages = viewModel.Pages;
			
			PageCollectionAssert.AreEqual (expectedPages, actualPages);
		}

		[Test]
		public void ReadPackages_SecondQueryFinishesBeforeFirst_PackagesInViewModelAreForSecondQuery ()
		{
			CreateViewModel ();
			viewModel.AddThreeFakePackages ();
			FakePackage package = viewModel.AddFakePackage ("MyTest");
			viewModel.ReadPackages ();
			viewModel.SearchTerms = "MyTest";

			var expectedPackages = new FakePackage [] { package };
			
			viewModel.ReadPackages ();
			taskFactory.ExecuteTask (1);
			taskFactory.ExecuteTask (0);
			ClearReadPackagesTasks ();
			
			PackageCollectionAssert.AreEqual (expectedPackages, viewModel.PackageViewModels);
		}

		[Test]
		public void ReadPackages_RepositoryHasSixPackagesWhenSelectedPageIsOneAndPageSizeIsThree_ThreePackageViewModelsCreatedForFirstThreePackages ()
		{
			CreateViewModel ();
			viewModel.PageSize = 3;
			viewModel.SelectedPageNumber = 1;
			viewModel.AddSixFakePackages ();
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			
			var expectedPackages = new List<FakePackage> ();
			expectedPackages.Add (viewModel.FakePackages [0]);
			expectedPackages.Add (viewModel.FakePackages [1]);
			expectedPackages.Add (viewModel.FakePackages [2]);

			ClearReadPackagesTasks ();
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			
			PackageCollectionAssert.AreEqual (expectedPackages, viewModel.PackageViewModels);
		}

		[Test]
		public void Pages_PageSizeChanged_PagesRecalcuatedBasedOnNewPageSize ()
		{
			CreateViewModel ();
			viewModel.PageSize = 10;
			viewModel.AddSixFakePackages ();
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			
			int oldPageCount = viewModel.Pages.Count;
			viewModel.PageSize = 5;
			int newPageCount = viewModel.Pages.Count;
			
			Assert.AreEqual (2, newPageCount);
			Assert.AreEqual (1, oldPageCount);
		}

		[Test]
		public void Pages_SelectedPageNumberChanged_PagesRecalculatedBasedOnNewSelectedPage ()
		{
			CreateViewModel ();
			viewModel.PageSize = 3;
			viewModel.SelectedPageNumber = 1;
			viewModel.AddSixFakePackages ();
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			
			ClearReadPackagesTasks ();
			var oldPages = viewModel.Pages;
			viewModel.SelectedPageNumber = 2;
			CompleteReadPackagesTask ();
			var newPages = viewModel.Pages;
			
			Page[] expectedPages = new Page[] {
				new Page () { Number = 1 },
				new Page () { Number = 2, IsSelected = true }
			};
			
			PageCollectionAssert.AreEqual (expectedPages, newPages);
		}

		[Test]
		public void ShowNextPageCommand_TwoPagesAndFirstPageSelectedWhenCommandExecuted_PageTwoIsSelected ()
		{
			CreateViewModel ();
			viewModel.PageSize = 3;
			viewModel.SelectedPageNumber = 1;
			viewModel.AddSixFakePackages ();
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			
			viewModel.ShowNextPageCommand.Execute (null);
			
			int selectedPage = viewModel.SelectedPageNumber;
			
			Assert.AreEqual (2, selectedPage);
		}

		[Test]
		public void ShowNextPageCommand_TwoPagesAndFirstPageSelectedWhenCommandExecuted_SecondPageOfPackagesDisplayed ()
		{
			CreateViewModel ();
			viewModel.AddThreeFakePackages ();
			viewModel.PageSize = 2;
			viewModel.SelectedPageNumber = 1;
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			
			ClearReadPackagesTasks ();
			viewModel.ShowNextPageCommand.Execute (null);
			CompleteReadPackagesTask ();
			
			var expectedPackages = new List<FakePackage> ();
			expectedPackages.Add (viewModel.FakePackages [2]);
			
			PackageCollectionAssert.AreEqual (expectedPackages, viewModel.PackageViewModels);
		}

		[Test]
		public void ShowPreviousPageCommand_TwoPagesAndSecondPageSelectedWhenCommandExecuted_PageOneIsSelected ()
		{
			CreateViewModel ();
			viewModel.PageSize = 3;
			viewModel.AddSixFakePackages ();
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			viewModel.SelectedPageNumber = 2;
			
			viewModel.ShowPreviousPageCommand.Execute (null);
			
			int selectedPage = viewModel.SelectedPageNumber;
			
			Assert.AreEqual (1, selectedPage);
		}

		[Test]
		public void ShowPreviousPageCommand_TwoPagesAndSecondPageSelectedWhenCommandExecuted_FirstPageOfPackagesDisplayed ()
		{
			CreateViewModel ();
			viewModel.AddThreeFakePackages ();
			viewModel.PageSize = 2;
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			viewModel.SelectedPageNumber = 2;
			ClearReadPackagesTasks ();
			
			viewModel.ShowPreviousPageCommand.Execute (null);
			CompleteReadPackagesTask ();
			
			var expectedPackages = new List<FakePackage> ();
			expectedPackages.Add (viewModel.FakePackages [0]);
			expectedPackages.Add (viewModel.FakePackages [1]);
			
			PackageCollectionAssert.AreEqual (expectedPackages, viewModel.PackageViewModels);
		}

		[Test]
		public void ShowPageCommand_PageNumberOneToBeShownWhenCurrentlySelectedPageIsTwo_PageOneIsSelected ()
		{
			CreateViewModel ();
			viewModel.PageSize = 2;
			viewModel.AddSixFakePackages ();
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			viewModel.SelectedPageNumber = 2;
			
			int pageNumber = 1;
			viewModel.ShowPageCommand.Execute (pageNumber);
			
			int selectedPage = viewModel.SelectedPageNumber;
			
			Assert.AreEqual (1, selectedPage);
		}

		[Test]
		public void Pages_ReadPackagesAndIsPagedCalled_PackagesReadFromRepositoryOnlyOnce ()
		{
			CreateViewModel ();
			viewModel.AddSixFakePackages ();
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			bool result = viewModel.IsPaged;
			int count = viewModel.Pages.Count;
			
			Assert.AreEqual (1, viewModel.GetAllPackagesCallCount);
		}

		[Test]
		public void ReadPackages_CalledThreeTimesAndThenSelectedPageChanged_ViewModelPropertiesChangedEventFiresOnceWhenSelectedPageChanged ()
		{
			CreateViewModel ();
			viewModel.PageSize = 3;
			viewModel.AddSixFakePackages ();

			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();

			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();

			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			
			int count = 0;
			viewModel.PropertyChanged += (sender, e) => count++;
			viewModel.SelectedPageNumber = 2;
			
			Assert.AreEqual (1, count);
		}

		[Test]
		public void IsSearchable_ByDefault_ReturnsFalse ()
		{
			CreateViewModel ();
			
			Assert.IsFalse (viewModel.IsSearchable);
		}

		[Test]
		public void SearchCommand_SearchTextEntered_PackageViewModelsFilteredBySearchCriteria ()
		{
			CreateViewModel ();
			viewModel.IsSearchable = true;
			viewModel.AddSixFakePackages ();
			
			var package = new FakePackage () {
				Id = "SearchedForId",
				Description = "Test"
			};
			viewModel.FakePackages.Add (package);
			
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			
			ClearReadPackagesTasks ();
			viewModel.SearchTerms = "SearchedForId";
			viewModel.SearchCommand.Execute (null);
			CompleteReadPackagesTask ();
			
			var expectedPackages = new FakePackage[] {
				package
			};
			
			PackageCollectionAssert.AreEqual (expectedPackages, viewModel.PackageViewModels);
		}

		[Test]
		public void PackageExtensionsFind_TwoPackagesInCollection_FindsOnePackageId ()
		{
			List<IPackage> packages = new List<IPackage> ();
			var package1 = new FakePackage () {
				Id = "Test"
			};
			var package2 = new FakePackage () {
				Id = "Another"
			};
			packages.Add (package1);
			packages.Add (package2);
			
			IQueryable<IPackage> query = packages.AsQueryable ();
			
			IQueryable<IPackage> filteredResults = query.Find ("Test");
			
			IPackage foundPackage = filteredResults.First ();
			
			Assert.AreEqual ("Test", foundPackage.Id);
		}

		[Test]
		public void PackageExtensionsFind_TwoPackagesInCollectionAndQueryableResultsPutInBufferedEnumerable_OnePackageInBufferedEnumerable ()
		{
			List<IPackage> packages = new List<IPackage> ();
			
			// Need to add descriptiosn otherwise we get a null reference when enumerating results 
			// in BufferedEnumerable
			var package1 = new FakePackage () {
				Id = "Test", Description = "b"
			};
			var package2 = new FakePackage () {
				Id = "Another", Description = "a"
			};
			packages.Add (package1);
			packages.Add (package2);
			
			IQueryable<IPackage> query = packages.AsQueryable ();
			
			IQueryable<IPackage> filteredResults = query.Find ("Test");
			
			var collection = new BufferedEnumerable<IPackage> (filteredResults, 10);
			IPackage foundPackage = collection.First ();
			
			Assert.AreEqual ("Test", foundPackage.Id);
		}

		[Test]
		public void Search_SearchTextChangedAndPackagesWerePagedBeforeSearch_PagesUpdatedAfterFilteringBySearchCriteria ()
		{
			CreateViewModel ();
			viewModel.IsSearchable = true;
			viewModel.PageSize = 2;
			viewModel.MaximumSelectablePages = 5;
			viewModel.AddSixFakePackages ();
			
			var package = new FakePackage () {
				Id = "SearchedForId",
				Description = "Test"
			};
			viewModel.FakePackages.Add (package);
			
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			
			ObservableCollection<Page> pages = viewModel.Pages;
			
			ClearReadPackagesTasks ();
			viewModel.SearchTerms = "SearchedForId";
			viewModel.Search ();
			CompleteReadPackagesTask ();
			
			var expectedPages = new Page[] {
				new Page () { Number = 1, IsSelected = true }
			};
			
			PageCollectionAssert.AreEqual (expectedPages, pages);
		}

		[Test]
		public void Pages_SixPackagesButPackagesNotRead_HasNoPages ()
		{
			CreateViewModel ();
			viewModel.PageSize = 2;
			viewModel.AddSixFakePackages ();
			
			Assert.AreEqual (0, viewModel.Pages.Count);
		}

		[Test]
		public void HasPreviousPage_SixPackagesAndSecondPageSelectedButPackagesNotRead_ReturnsFalse ()
		{
			CreateViewModel ();
			viewModel.PageSize = 2;
			viewModel.SelectedPageNumber = 2;
			viewModel.AddSixFakePackages ();
			
			Assert.IsFalse (viewModel.HasPreviousPage);
		}

		[Test]
		public void HasNextPage_SixPackagesAndFirstPageSelectedButPackagesNotRead_ReturnsFalse ()
		{
			CreateViewModel ();
			viewModel.PageSize = 2;
			viewModel.SelectedPageNumber = 1;
			viewModel.AddSixFakePackages ();
			
			Assert.IsFalse (viewModel.HasNextPage);
		}

		[Test]
		public void IsPaged_SixPackagesAndFirstPageSelectedButPackagesNotRead_ReturnsFalse ()
		{
			CreateViewModel ();
			viewModel.PageSize = 2;
			viewModel.SelectedPageNumber = 1;
			viewModel.AddSixFakePackages ();
			
			Assert.IsFalse (viewModel.IsPaged);
		}

		[Test]
		public void Search_SelectedPageInitiallyIsPageTwoAndThenUserSearches_SelectedPageNumberIsSetToPageOne ()
		{
			CreateViewModel ();
			viewModel.PageSize = 2;
			viewModel.AddSixFakePackages ();
			
			var package = new FakePackage () {
				Id = "SearchedForId",
				Description = "Test"
			};
			viewModel.FakePackages.Add (package);
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();

			viewModel.SelectedPageNumber = 2;
			
			ClearReadPackagesTasks ();
			viewModel.SearchTerms = "SearchedForId";
			viewModel.Search ();
			CompleteReadPackagesTask ();
			
			Assert.AreEqual (1, viewModel.SelectedPageNumber);
		}

		/// <summary>
		/// Ensures that the total number of packages is determined from all packages and not
		/// the filtered set. All packages will be retrieved from the repository
		/// if this is not done when we only want 30 retrieved in one go.
		/// </summary>
		[Test]
		public void ReadPackages_SixPackagesInRepository_TotalItemsSetBeforePackagesFiltered ()
		{
			CreateViewModel ();
			viewModel.PageSize = 2;
			viewModel.AddSixFakePackages ();
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			
			int expectedTotal = 6;
			Assert.AreEqual (expectedTotal, viewModel.TotalItems);
		}

		[Test]
		public void Search_ThreePagesOfPackagesBeforeSearchReturnsNoPackages_IsPagedIsFalseWhenPropertyChangedEventFired ()
		{
			CreateViewModel ();
			viewModel.PageSize = 2;
			viewModel.AddSixFakePackages ();
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			
			viewModel.SearchTerms = "SearchedForId";
			
			ClearReadPackagesTasks ();
			bool paged = true;
			viewModel.PropertyChanged += (sender, e) => paged = viewModel.IsPaged;
			viewModel.Search ();
			CompleteReadPackagesTask ();
			
			Assert.IsFalse (paged);
		}

		[Test]
		public void Search_BeforeSearchFivePagesOfPackagesShownAndSearchReturnsTwoPages_TwoPagesShownAfterSearch ()
		{
			CreateViewModel ();
			viewModel.IsSearchable = true;
			viewModel.PageSize = 2;
			viewModel.MaximumSelectablePages = 5;
			viewModel.AddSixFakePackages ();
			
			viewModel.FakePackages.Add (new FakePackage ("SearchedForId1"));
			viewModel.FakePackages.Add (new FakePackage ("SearchedForId2"));
			viewModel.FakePackages.Add (new FakePackage ("SearchedForId3"));
			
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			
			ObservableCollection<Page> pages = viewModel.Pages;
			
			ClearReadPackagesTasks ();
			viewModel.SearchTerms = "SearchedForId";
			viewModel.Search ();
			CompleteReadPackagesTask ();
			
			var expectedPages = new Page[] {
				new Page () { Number = 1, IsSelected = true },
				new Page () { Number = 2 }
			};
			
			PageCollectionAssert.AreEqual (expectedPages, pages);
		}

		[Test]
		public void ShowPackageSources_ByDefault_ReturnsFalse ()
		{
			CreateViewModel ();
			
			Assert.IsFalse (viewModel.ShowPackageSources);
		}

		[Test]
		public void ReadPackages_OnePackageInRepository_CreatesTask ()
		{
			CreateViewModel ();
			viewModel.AddOneFakePackage ();
			
			viewModel.ReadPackages ();
			
			Assert.IsTrue (taskFactory.IsCreateTaskCalled);
		}

		[Test]
		public void ReadPackages_OnePackageInRepository_TaskStartMethodCalled ()
		{
			CreateViewModel ();
			viewModel.AddOneFakePackage ();
			
			viewModel.ReadPackages ();
			
			Assert.IsTrue (taskFactory.FirstFakeTaskCreated.IsStartCalled);
		}

		[Test]
		public void IsReadingPackages_ReadPackagesNotCalled_ReturnsFalse ()
		{
			CreateViewModel ();
			
			Assert.IsFalse (viewModel.IsReadingPackages);
		}

		[Test]
		public void IsReadingPackages_ReadPackagesCalled_ReturnsTrue ()
		{
			CreateViewModel ();
			viewModel.ReadPackages ();
			
			Assert.IsTrue (viewModel.IsReadingPackages);
		}

		[Test]
		public void ReadPackages_OnePackageInRepositoryWhenBackgroundTaskExecuted_ReadsOnePackage ()
		{
			CreateViewModel ();
			viewModel.AddOneFakePackage ();
			
			viewModel.ReadPackages ();
			
			PackagesForSelectedPageResult result = taskFactory.FirstFakeTaskCreated.ExecuteTaskButNotContinueWith ();
			
			CollectionAssert.AreEqual (viewModel.FakePackages, result.Packages);
		}

		[Test]
		public void ReadPackages_OnePackageInRepositoryWhenFirstPartOfBackgroundTaskExecuted_PackageCountReadInBackgroundTask ()
		{
			CreateViewModel ();
			viewModel.AddOneFakePackage ();
			
			viewModel.ReadPackages ();
			
			PackagesForSelectedPageResult result = taskFactory.FirstFakeTaskCreated.ExecuteTaskButNotContinueWith ();
			
			Assert.AreEqual (1, result.TotalPackagesOnPage);
		}

		[Test]
		public void ReadPackages_OnePackageInRepositoryWhenBackgroundTaskExecutedAndResultsReturned_PackagesUpdatedInViewModel ()
		{
			CreateViewModel ();
			viewModel.AddOneFakePackage ();
			
			viewModel.ReadPackages ();
			
			CompleteReadPackagesTask ();
			
			PackageCollectionAssert.AreEqual (viewModel.FakePackages, viewModel.PackageViewModels);
		}

		[Test]
		public void IsReadingPackages_OnePackageInRepositoryWhenBackgroundTaskExecutedAndResultsReturned_SetToFalseAfterPackagesRead ()
		{
			CreateViewModel ();
			viewModel.AddOneFakePackage ();
			
			viewModel.ReadPackages ();
			
			CompleteReadPackagesTask ();
			
			Assert.IsFalse (viewModel.IsReadingPackages);
		}

		[Test]
		public void IsReadingPackages_OnePackageInRepositoryWhenBackgroundTaskExecutedAndResultsReturned_NotifyPropertyChangedFiredAfterIsReadingPackagesSetToFalse ()
		{
			CreateViewModel ();
			viewModel.AddOneFakePackage ();
			
			viewModel.ReadPackages ();
			
			bool readingPackages = true;
			viewModel.PropertyChanged += (sender, e) => readingPackages = viewModel.IsReadingPackages;
			CompleteReadPackagesTask ();
			
			Assert.IsFalse (readingPackages);
		}

		[Test]
		public void ReadPackages_SixPackagesInRepositoryAndPageSizeIsTwoWhenFirstPartOfBackgroundTaskExecuted_PackageCountReadInBackgroundTask ()
		{
			CreateViewModel ();
			viewModel.PageSize = 2;
			viewModel.AddSixFakePackages ();
			
			viewModel.ReadPackages ();
			
			PackagesForSelectedPageResult result = taskFactory.FirstFakeTaskCreated.ExecuteTaskButNotContinueWith ();
			
			Assert.AreEqual (6, result.TotalPackages);
		}

		[Test]
		public void ReadPackages_SixPackagesInRepositoryAndPageSizeIsTwoWhenFirstPartOfBackgroundTaskExecuted_PageSizeNotChangedDuringBackgroundTaskExecution ()
		{
			CreateViewModel ();
			viewModel.PageSize = 2;
			viewModel.AddSixFakePackages ();
			
			viewModel.ReadPackages ();
			
			taskFactory.FirstFakeTaskCreated.ExecuteTaskButNotContinueWith ();
			
			Assert.IsFalse (viewModel.IsPaged);
		}

		[Test]
		public void ReadPackages_SixPackagesInRepositoryAndPageSizeIsTwoWhenBackgroundTaskExecutedAndResultsReturned_ResultsArePaged ()
		{
			CreateViewModel ();
			viewModel.PageSize = 2;
			viewModel.AddSixFakePackages ();
			
			viewModel.ReadPackages ();
			
			CompleteReadPackagesTask ();
			
			Assert.IsTrue (viewModel.IsPaged);
		}

		[Test]
		public void ReadPackages_CalledSecondTimeBeforeFirstReadPackagesTaskCompletes_FirstReadPackagesTaskIsCancelled ()
		{
			CreateViewModel ();
			viewModel.AddOneFakePackage ();
			
			viewModel.ReadPackages ();
			viewModel.ReadPackages ();
			
			Assert.IsTrue (taskFactory.FirstFakeTaskCreated.IsCancelCalled);
		}

		[Test]
		public void ReadPackages_FirstReadPackagesTaskCompletesAfterBeingCancelled_PackagesNotUpdated ()
		{
			CreateViewModel ();
			viewModel.AddOneFakePackage ();
			
			viewModel.ReadPackages ();
			taskFactory.FirstFakeTaskCreated.IsCancelled = true;
			viewModel.ReadPackages ();
			taskFactory.FirstFakeTaskCreated.ExecuteTaskCompletely ();
			
			Assert.AreEqual (0, viewModel.PackageViewModels.Count);
		}

		[Test]
		public void SelectedPage_ChangedTwoPageTwo_IsReadingPackagesReturnsTrue ()
		{
			CreateViewModel ();
			viewModel.AddSixFakePackages ();
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			ClearReadPackagesTasks ();
			
			viewModel.SelectedPageNumber = 2;
			
			Assert.IsTrue (viewModel.IsReadingPackages);
		}

		[Test]
		public void ReadPackages_SixPackagesDisplayedWhenReadPackagesCalledAgain_DisplayedPackagesAreRemoved ()
		{
			CreateViewModel ();
			viewModel.AddSixFakePackages ();
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			
			viewModel.ReadPackages ();
			
			Assert.AreEqual (0, viewModel.PackageViewModels.Count);
		}

		[Test]
		public void SelectedPage_ChangedTwoPageTwo_DisplayedPackagesAreRemoved ()
		{
			CreateViewModel ();
			viewModel.AddSixFakePackages ();
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			ClearReadPackagesTasks ();
			
			viewModel.SelectedPageNumber = 2;
			
			Assert.AreEqual (0, viewModel.PackageViewModels.Count);
		}

		[Test]
		public void HasError_BackgroundTaskHasExceptionWhenItFinishes_ReturnsTrue ()
		{
			CreateViewModel ();
			viewModel.ReadPackages ();
			taskFactory.FirstFakeTaskCreated.IsFaulted = true;
			CompleteReadPackagesTask ();
			
			Assert.IsTrue (viewModel.HasError);
		}

		[Test]
		public void HasError_ByDefault_ReturnsFalse ()
		{
			CreateViewModel ();
			
			Assert.IsFalse (viewModel.HasError);
		}

		[Test]
		public void IsReadingPackages_BackgroundTaskHasExceptionWhenItFinishes_ReturnsFalse ()
		{
			CreateViewModel ();
			viewModel.ReadPackages ();
			taskFactory.FirstFakeTaskCreated.IsFaulted = true;
			CompleteReadPackagesTask ();
			
			Assert.IsFalse (viewModel.IsReadingPackages);
		}

		[Test]
		public void PropertyChanged_BackgroundTaskHasExceptionWhenItFinishes_PropertyChangedEventFiredWhenTaskCompletes ()
		{
			CreateViewModel ();
			viewModel.ReadPackages ();
			taskFactory.FirstFakeTaskCreated.IsFaulted = true;
			taskFactory.FirstFakeTaskCreated.ExecuteTaskButNotContinueWith ();
			
			string propertyName = "Nothing";
			viewModel.PropertyChanged += (sender, e) => propertyName = e.PropertyName;
			taskFactory.FirstFakeTaskCreated.ExecuteContinueWith ();
			
			Assert.IsNull (propertyName);
		}

		[Test]
		public void ReadPackages_BackgroundTaskHasExceptionWhenItFinishes_PackagesNotUpdated ()
		{
			CreateViewModel ();
			viewModel.AddSixFakePackages ();
			viewModel.ReadPackages ();
			var query = new PackagesForSelectedPageQuery (viewModel, null, null);
			taskFactory.FirstFakeTaskCreated.Result = new PackagesForSelectedPageResult (viewModel.FakePackages, query);
			taskFactory.FirstFakeTaskCreated.IsFaulted = true;
			CompleteReadPackagesTask ();
			
			Assert.AreEqual (0, viewModel.PackageViewModels.Count);
		}

		[Test]
		public void ErrorMessage_BackgroundTaskHasExceptionWhenItFinishes_ErrorMessageTakenFromException ()
		{
			CreateViewModel ();
			viewModel.AddSixFakePackages ();
			viewModel.ReadPackages ();
			
			Exception ex = new Exception ("Test");
			AggregateException aggregateEx = new AggregateException (ex);
			taskFactory.FirstFakeTaskCreated.Exception = aggregateEx;
			taskFactory.FirstFakeTaskCreated.IsFaulted = true;
			CompleteReadPackagesTask ();
			
			Assert.AreEqual ("Test", viewModel.ErrorMessage);
		}

		[Test]
		public void ErrorMessage_BackgroundTaskHasAggregateExceptionWithNestedInnerAggregateException_ErrorMessageTakenFromInnerException ()
		{
			CreateViewModel ();
			viewModel.AddSixFakePackages ();
			viewModel.ReadPackages ();

			Exception innerEx1 = new Exception ("Test1");
			Exception innerEx2 = new Exception ("Test2");
			AggregateException innerAggregateEx = new AggregateException (innerEx1, innerEx2);
			AggregateException aggregateEx = new AggregateException (innerAggregateEx);
			taskFactory.FirstFakeTaskCreated.Exception = aggregateEx;
			taskFactory.FirstFakeTaskCreated.IsFaulted = true;
			CompleteReadPackagesTask ();
			
			string expectedErrorMessage = 
				"Test1" + Environment.NewLine +
				"Test2";
			
			Assert.AreEqual (expectedErrorMessage, viewModel.ErrorMessage);
		}

		[Test]
		public void ErrorMessage_BackgroundTaskHasAggregateExceptionWithTwoInnerExceptionsWhenItFinishes_ErrorMessageTakenFromAllInnerExceptions ()
		{
			CreateViewModel ();
			viewModel.AddSixFakePackages ();
			viewModel.ReadPackages ();
			
			Exception innerEx1 = new Exception ("Test1");
			Exception innerEx2 = new Exception ("Test2");
			Exception innerEx3 = new Exception ("Test3");
			AggregateException aggregateEx = new AggregateException (innerEx1, innerEx2, innerEx3);
			taskFactory.FirstFakeTaskCreated.Exception = aggregateEx;
			taskFactory.FirstFakeTaskCreated.IsFaulted = true;
			CompleteReadPackagesTask ();
			
			string expectedErrorMessage = 
				"Test1" + Environment.NewLine +
				"Test2" + Environment.NewLine +
				"Test3";
			
			Assert.AreEqual (expectedErrorMessage, viewModel.ErrorMessage);
		}

		[Test]
		public void HasError_ErrorMessageDisplayedAndReadPackagesRetriedAfterFailure_ReturnsFalse ()
		{
			CreateViewModel ();
			viewModel.ReadPackages ();
			taskFactory.FirstFakeTaskCreated.IsFaulted = true;
			CompleteReadPackagesTask ();
			viewModel.ReadPackages ();
			
			Assert.IsFalse (viewModel.HasError);
		}

		[Test]
		public void HasError_ErrorMessageDisplayedAndSelectedPageChangedAfterFailure_ReturnsFalse ()
		{
			CreateViewModel ();
			viewModel.PageSize = 2;
			viewModel.AddSixFakePackages ();
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			ClearReadPackagesTasks ();
			
			viewModel.SelectedPageNumber = 2;
			taskFactory.FirstFakeTaskCreated.IsFaulted = true;
			CompleteReadPackagesTask ();
			
			viewModel.SelectedPageNumber = 3;
			
			Assert.IsFalse (viewModel.HasError);
		}

		[Test]
		public void ReadPackages_PackagesReturnedNotSortedFromRepository_PackagesDisplayedSortedById ()
		{
			CreateViewModel ();
			viewModel.AddFakePackage ("Z");
			viewModel.AddFakePackage ("C");
			viewModel.AddFakePackage ("A");
			viewModel.AddFakePackage ("B");
			viewModel.ReadPackages ();
			
			CompleteReadPackagesTask ();
			
			var expectedPackages = new FakePackage[] {
				viewModel.FakePackages [2],
				viewModel.FakePackages [3],
				viewModel.FakePackages [1],
				viewModel.FakePackages [0]
			};
			
			PackageCollectionAssert.AreEqual (expectedPackages, viewModel.PackageViewModels);
		}

		[Test]
		public void SelectedPageNumber_SixPackagesAndPageSizeIsFiveAndSelectedPageNumberIsChangedToTwo_OneReadPackagesTaskCreated ()
		{
			CreateViewModel ();
			viewModel.PageSize = 5;
			viewModel.AddSixFakePackages ();
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			ClearReadPackagesTasks ();
			
			viewModel.SelectedPageNumber = 2;
			
			Assert.AreEqual (1, taskFactory.FakeTasksCreated.Count);
		}

		[Test]
		public void SelectedPageNumber_SixPackagesAndSelectedPageNumberIsSetToPageOneButUnchanged_NoReadPackagesTaskCreated ()
		{
			CreateViewModel ();
			viewModel.PageSize = 5;
			viewModel.AddSixFakePackages ();
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			ClearReadPackagesTasks ();
			
			viewModel.SelectedPageNumber = 1;
			
			Assert.AreEqual (0, taskFactory.FakeTasksCreated.Count);
		}

		[Test]
		public void SelectedPageNumber_SixPackagesAndPageSizeIsFiveAndSelectedPageNumberIsChangedToTwo_PropertyChangedEventFiredAfterSelectedPageNumberChanged ()
		{
			CreateViewModel ();
			viewModel.PageSize = 5;
			viewModel.AddSixFakePackages ();
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			ClearReadPackagesTasks ();
			
			int selectedPageNumber = 0;
			viewModel.PropertyChanged += (source, e) => selectedPageNumber = viewModel.SelectedPageNumber;
			viewModel.SelectedPageNumber = 2;
			
			Assert.AreEqual (2, selectedPageNumber);
		}

		[Test]
		public void SelectedPageNumber_SixPackagesAndPageSizeIsFiveAndSelectedPageNumberIsChangedToTwo_SelectedPageNumberChangedBeforeReadPackagesTaskStarted ()
		{
			CreateViewModel ();
			viewModel.PageSize = 5;
			viewModel.AddSixFakePackages ();
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			ClearReadPackagesTasks ();
			
			taskFactory.RunTasksSynchronously = true;
			viewModel.SelectedPageNumber = 2;
			
			Assert.AreEqual (1, viewModel.PackageViewModels.Count);
		}

		[Test]
		public void Search_RepositoryHasPackageWithIdOfEmptyString_SearchCriteriaUsedIsNull ()
		{
			CreateViewModel ();
			viewModel.PageSize = 2;
			viewModel.FakePackages.Add (new FakePackage () {
				Id = "",
				Description = "abc"
			});
			taskFactory.RunTasksSynchronously = true;
			viewModel.ReadPackages ();
			
			ClearReadPackagesTasks ();
			
			viewModel.SearchTerms = "";
			viewModel.Search ();
			
			Assert.IsNull (viewModel.SearchCriteriaPassedToFilterPackagesBySearchCriteria.SearchText);
		}

		[Test]
		public void Search_RepositoryHasPackageWithIdOfWhitespaceString_SearchCriteriaUsedIsNull ()
		{
			CreateViewModel ();
			viewModel.PageSize = 2;
			viewModel.FakePackages.Add (new FakePackage () {
				Id = "",
				Description = "abc"
			});
			taskFactory.RunTasksSynchronously = true;
			viewModel.ReadPackages ();
			
			ClearReadPackagesTasks ();
			
			viewModel.SearchTerms = "   ";
			viewModel.Search ();
			
			Assert.IsNull (viewModel.SearchCriteriaPassedToFilterPackagesBySearchCriteria.SearchText);
		}

		[Test]
		public void IsDisposed_DisposeMethodCalled_ReturnsTrue ()
		{
			CreateViewModel ();
			viewModel.Dispose ();
			
			Assert.IsTrue (viewModel.IsDisposed);
		}

		[Test]
		public void IsDisposed_DisposeMethodNotCalled_ReturnsFalse ()
		{
			CreateViewModel ();
			
			Assert.IsFalse (viewModel.IsDisposed);
		}

		[Test]
		public void IncludePrerelease_ChangedToTrue_PackagesAreReadAgain ()
		{
			CreateViewModel ();
			viewModel.IncludePrerelease = false;
			viewModel.AddSixFakePackages ();
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			ClearReadPackagesTasks ();
			
			viewModel.IncludePrerelease = true;
			
			Assert.IsTrue (viewModel.IsReadingPackages);
		}

		[Test]
		public void IncludePrerelease_ChangedToFalse_PackagesAreReadAgain ()
		{
			CreateViewModel ();
			viewModel.IncludePrerelease = true;
			viewModel.AddSixFakePackages ();
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			ClearReadPackagesTasks ();
			
			viewModel.IncludePrerelease = false;
			
			Assert.IsTrue (viewModel.IsReadingPackages);
		}

		[Test]
		public void IncludePrerelease_ChangedToTrue_PropertyChangedEventIsFired ()
		{
			CreateViewModel ();
			viewModel.IncludePrerelease = false;
			viewModel.AddSixFakePackages ();
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			ClearReadPackagesTasks ();
			PropertyChangedEventArgs propertyChangedEvent = null;
			viewModel.PropertyChanged += (sender, e) => propertyChangedEvent = e;
			
			viewModel.IncludePrerelease = true;
			
			Assert.IsNull (propertyChangedEvent.PropertyName);
		}

		[Test]
		public void IncludePrerelease_SetToTrueWhenAlreadyTrue_PropertyChangedEventIsNotFired ()
		{
			CreateViewModel ();
			viewModel.IncludePrerelease = true;
			viewModel.AddSixFakePackages ();
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			ClearReadPackagesTasks ();
			bool fired = false;
			viewModel.PropertyChanged += (sender, e) => fired = true;
			
			viewModel.IncludePrerelease = true;
			
			Assert.IsFalse (fired);
		}

		[Test]
		public void CheckedPackageViewModels_TwoPackagesAndOnePackageIsChecked_ReturnsOneCheckedPackage ()
		{
			CreateViewModel ();
			FakePackage package = viewModel.AddFakePackage ("MyPackage");
			var expectedPackages = new FakePackage [] { package };
			viewModel.AddFakePackage ("Z-Package");
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();

			viewModel.PackageViewModels [0].IsChecked = true;

			PackageCollectionAssert.AreEqual (expectedPackages, viewModel.CheckedPackageViewModels);
		}

		[Test]
		public void CheckedPackageViewModels_OnePackageIsCheckedThenNewSearchReturnsNoPackages_ReturnsCheckedPackageEvenWhenNotVisible ()
		{
			CreateViewModel ();
			FakePackage package = viewModel.AddFakePackage ("MyPackage");
			var expectedPackages = new FakePackage [] { package };
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			viewModel.PackageViewModels [0].IsChecked = true;
			viewModel.FakePackages.Clear ();
			viewModel.AddFakePackage ("AnotherPackage");
			viewModel.Search ();
			CompleteReadPackagesTask ();

			PackageCollectionAssert.AreEqual (expectedPackages, viewModel.CheckedPackageViewModels);
		}

		[Test]
		public void CheckedPackageViewModels_OnePackageIsCheckedAndThenUnchecked_ReturnsNoCheckedPackages ()
		{
			CreateViewModel ();
			viewModel.AddFakePackage ("MyPackage");
			viewModel.AddFakePackage ("Z-Package");
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			PackageViewModel packageViewModel = viewModel.PackageViewModels [0];
			packageViewModel.IsChecked = true;
			packageViewModel.IsChecked = false;

			PackageCollectionAssert.AreEqual (new FakePackage [0], viewModel.CheckedPackageViewModels);
		}

		[Test]
		public void PackageViewModels_OnePackageIsCheckedAndNewSearchReturnsOriginalPackage_PackageViewHasIsCheckedSetToTrue ()
		{
			CreateViewModel ();
			viewModel.AddFakePackage ("MyPackage", "1.0");
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			viewModel.PackageViewModels [0].IsChecked = true;
			viewModel.FakePackages.Clear ();
			viewModel.AddFakePackage ("MyPackage", "1.0");
			viewModel.Search ();
			CompleteReadPackagesTask ();

			PackageViewModel packageViewModel = viewModel.PackageViewModels [0];

			Assert.AreEqual ("MyPackage", packageViewModel.Id);
			Assert.IsTrue (packageViewModel.IsChecked);
		}

		[Test]
		public void PackageViewModels_OnePackageIsCheckedAndNewSearchReturnsOriginalPackageButWithDifferentVersion_PackageViewHasIsCheckedSetToFalse ()
		{
			CreateViewModel ();
			viewModel.AddFakePackage ("MyPackage", "1.0");
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			viewModel.PackageViewModels [0].IsChecked = true;
			viewModel.FakePackages.Clear ();
			viewModel.AddFakePackage ("MyPackage", "1.1");
			viewModel.Search ();
			CompleteReadPackagesTask ();

			PackageViewModel packageViewModel = viewModel.PackageViewModels [0];

			Assert.AreEqual ("MyPackage", packageViewModel.Id);
			Assert.IsFalse (packageViewModel.IsChecked);
		}

		[Test]
		public void CheckedPackageViewModels_OnePackageIsCheckedAndNewSearchReturnsMultipleVersionsOfOriginalPackage_OnlyPackageWithSameVersionIsChecked ()
		{
			CreateViewModel ();
			viewModel.AddFakePackage ("MyPackage", "1.0");
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			viewModel.PackageViewModels [0].IsChecked = true;
			viewModel.FakePackages.Clear ();
			viewModel.AddFakePackage ("MyPackage", "1.1");
			viewModel.AddFakePackage ("MyPackage", "1.2");
			viewModel.AddFakePackage ("MyPackage", "1.0");
			viewModel.AddFakePackage ("MyPackage", "1.3");
			viewModel.AddFakePackage ("MyPackage", "1.4");
			viewModel.Search ();
			CompleteReadPackagesTask ();

			PackageViewModel packageViewModel = viewModel.CheckedPackageViewModels.FirstOrDefault ();

			Assert.AreEqual ("MyPackage", packageViewModel.Id);
			Assert.AreEqual ("1.0", packageViewModel.Version.ToString ());
			Assert.AreEqual (1, viewModel.CheckedPackageViewModels.Count);
		}

		[Test]
		public void PackageViewModels_OnePackageIsCheckedAndNewSearchReturnsOriginalPackageWhichIsThenUncheckedByUser_NoCheckedPackageViewModels ()
		{
			CreateViewModel ();
			viewModel.AddFakePackage ("MyPackage", "1.0");
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			viewModel.PackageViewModels [0].IsChecked = true;
			viewModel.FakePackages.Clear ();
			viewModel.AddFakePackage ("MyPackage", "1.0");
			viewModel.Search ();
			CompleteReadPackagesTask ();
			PackageViewModel packageViewModel = viewModel.PackageViewModels [0];

			viewModel.PackageViewModels [0].IsChecked = false;

			PackageCollectionAssert.AreEqual (new FakePackage [0], viewModel.CheckedPackageViewModels);
		}

		[Test]
		public void CheckedPackageViewModels_OnePackageVersionIsCheckedThenDifferentVersionChecked_OldVersionIsUnchecked ()
		{
			CreateViewModel ();
			FakePackage oldPackage = viewModel.AddFakePackage ("MyPackage", "1.1");
			viewModel.AddFakePackage ("MyPackage", "1.2");
			viewModel.AddFakePackage ("MyPackage", "1.0");
			FakePackage newPackage = viewModel.AddFakePackage ("MyPackage", "1.3");
			viewModel.AddFakePackage ("MyPackage", "1.4");
			var expectedPackages = new FakePackage [] { newPackage };
			viewModel.ReadPackages ();
			CompleteReadPackagesTask ();
			PackageViewModel oldPackageVersionViewModel = viewModel
				.PackageViewModels
				.First (item => item.Version == oldPackage.Version);
			PackageViewModel newPackageVersionViewModel = viewModel
				.PackageViewModels
				.First (item => item.Version == newPackage.Version);
			oldPackageVersionViewModel.IsChecked = true;

			newPackageVersionViewModel.IsChecked = true;

			PackageCollectionAssert.AreEqual (expectedPackages, viewModel.CheckedPackageViewModels);
			Assert.IsFalse (oldPackageVersionViewModel.IsChecked);
		}

		[Test]
		public void ReadPackages_ReadPackagesCalledAgainAfterFirstOneFailed_ErrorIsCleared ()
		{
			CreateViewModel ();
			viewModel.ReadPackages ();
			var ex = new Exception ("Test");
			var aggregateEx = new AggregateException (ex);
			taskFactory.FirstFakeTaskCreated.Exception = aggregateEx;
			taskFactory.FirstFakeTaskCreated.IsFaulted = true;
			CompleteReadPackagesTask ();
			bool hasErrorAfterFirstRead = viewModel.HasError;

			viewModel.ReadPackages ();

			Assert.IsTrue (hasErrorAfterFirstRead);
			Assert.IsFalse (viewModel.HasError);
			Assert.AreEqual (String.Empty, viewModel.ErrorMessage);
		}
	}
}
