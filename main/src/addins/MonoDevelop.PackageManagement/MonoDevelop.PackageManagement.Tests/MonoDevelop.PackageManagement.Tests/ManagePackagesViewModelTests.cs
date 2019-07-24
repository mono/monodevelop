//
// ManagePackagesViewModelTests.cs
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

using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.PackageManagement.Tests.Helpers;
using NuGet.Configuration;
using NuGet.PackageManagement.UI;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.Versioning;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class ManagePackagesViewModelTests
	{
		TestableManagePackagesViewModel viewModel;
		FakeSolutionManager solutionManager;
		FakeDotNetProject project;
		FakeSolution solution;
		FakePackageSourceProvider packageSourceProvider;

		void CreateProject ()
		{
			solution = new FakeSolution ();
			project = new FakeDotNetProject ();
			project.ParentSolution = solution;
			solution.Projects.Add (project);
			solutionManager = new FakeSolutionManager ();
			packageSourceProvider = solutionManager.SourceRepositoryProvider.FakePackageSourceProvider;
		}

		FakeDotNetProject AddProjectToSolution (string name)
		{
			var newProject = new FakeDotNetProject ();
			newProject.Name = name;
			solution.Projects.Add (newProject);
			return newProject;
		}

		void CreateViewModel ()
		{
			viewModel = new TestableManagePackagesViewModel (
				solutionManager,
				project);
			EnsurePackageSourcesLoaded ();
		}

		void CreateViewModelForSolution ()
		{
			viewModel = new TestableManagePackagesViewModel (solutionManager, solution);
			EnsurePackageSourcesLoaded ();
		}

		void EnsurePackageSourcesLoaded ()
		{
			viewModel.PackageSources.ToList ();
		}

		PackageSource[] AddTwoPackageSourcesToRegisteredSources ()
		{
			var sources = new [] {
				new PackageSource ("http://first.com", "First"),
				new PackageSource ("http://second.com", "Second")
			};
			solutionManager.SourceRepositoryProvider.AddRepositories (sources);

			return sources;
		}

		PackageSource AddOnePackageSourceToRegisteredSources ()
		{
			var source = new PackageSource ("http://monodevelop.com", "Test");
			solutionManager.SourceRepositoryProvider.AddRepository (source);
			return source;
		}

		Task SetUpTwoPackageSourcesAndViewModelHasReadPackages ()
		{
			CreateProject ();
			var sources = AddTwoPackageSourcesToRegisteredSources ().ToList ();
			packageSourceProvider.ActivePackageSourceName = sources[0].Name;
			CreateViewModel ();
			EnsurePackageSourcesLoaded ();
			viewModel.ReadPackages ();
			return viewModel.ReadPackagesTask;
		}

		void ChangeSelectedPackageSourceToFirstSourceNonAggregateSource ()
		{
			var firstPackageSource = viewModel.PackageSources.First (source => !source.IsAggregate);
			viewModel.SelectedPackageSource = firstPackageSource;
		}

		void ChangeSelectedPackageSourceToSecondNonAggregateSource ()
		{
			var secondPackageSource = viewModel
				.PackageSources
				.Where (source => !source.IsAggregate)
				.Skip (1)
				.First ();
			viewModel.SelectedPackageSource = secondPackageSource;
		}

		ManagePackagesSearchResultViewModel AddRecentPackage (string packageId, string packageVersion, string packageSource)
		{
			var searchResultViewModel = CreateRecentPackage (packageId, packageVersion, packageSource);
			viewModel.RecentPackagesRepository.AddPackage (searchResultViewModel, packageSource);
			return searchResultViewModel;
		}

		ManagePackagesSearchResultViewModel CreateRecentPackage (string packageId, string packageVersion, string packageSource)
		{
			var packagesViewModelForRecentPackages = new TestableManagePackagesViewModel (
				new FakeSolutionManager (),
				new FakeDotNetProject ());
			var recentPackage = new PackageItemListViewModel {
				Id = packageId,
				Version = new NuGetVersion (packageVersion)
			};
			return new ManagePackagesSearchResultViewModel (packagesViewModelForRecentPackages, recentPackage);
		}

		FakeNuGetProject CreateNuGetProjectForProject ()
		{
			return CreateNuGetProjectForProject (project);
		}

		FakeNuGetProject CreateNuGetProjectForProject (IDotNetProject dotNetProject)
		{
			var nugetProject = new FakeNuGetProject (dotNetProject);
			solutionManager.NuGetProjects [dotNetProject] = nugetProject;
			return nugetProject;
		}

		[Test]
		public void PackageSources_TwoPackageSourcesInOptions_ReturnsTwoPackageSourcesPlusAggregatePackageSource ()
		{
			CreateProject ();
			var expectedPackageSources = AddTwoPackageSourcesToRegisteredSources ().ToList ();
			CreateViewModel ();
			expectedPackageSources.Insert (0, AggregateSourceRepositoryViewModel.AggregatePackageSource);

			var packageSources = viewModel.PackageSources.Select (vm => vm.PackageSource).ToList ();

			PackageSourceCollectionAssert.AreEqual (expectedPackageSources, packageSources);
		}

		[Test]
		public void PackageSources_OnePackageSourceInOptions_ReturnsOnePackageSource ()
		{
			CreateProject ();
			var packageSource = AddOnePackageSourceToRegisteredSources ();
			var expectedPackageSources = new [] { packageSource };
			CreateViewModel ();

			var packageSources = viewModel.PackageSources.Select (vm => vm.PackageSource).ToList ();

			PackageSourceCollectionAssert.AreEqual (expectedPackageSources, packageSources);
		}

		[Test]
		public async Task ReadPackages_RepositoryHasOnePackage_PackageLoaded ()
		{
			CreateProject ();
			AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();
			viewModel.PackageFeed.AddPackage ("Test", "0.1");

			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;

			var package = viewModel.PackageViewModels.Single ();
			Assert.AreEqual ("Test", package.Id);
			Assert.AreEqual ("0.1", package.Version.ToString ());
		}

		[Test]
		public void SelectedPackageSource_TwoPackageSourcesInOptionsAndActivePackageSourceIsFirstSource_IsFirstPackageSource ()
		{
			CreateProject ();
			var sources = AddTwoPackageSourcesToRegisteredSources ().ToList ();
			var expectedPackageSource = sources[0];
			packageSourceProvider.ActivePackageSourceName = expectedPackageSource.Name;
			CreateViewModel ();

			Assert.AreEqual (expectedPackageSource, viewModel.SelectedPackageSource.PackageSource);
		}

		[Test]
		public void SelectedPackageSource_TwoPackageSourcesInOptionsAndActivePackageSourceIsSecondSource_IsSecondPackageSource ()
		{
			CreateProject ();
			var sources = AddTwoPackageSourcesToRegisteredSources ();
			var expectedPackageSource = sources[1];
			packageSourceProvider.ActivePackageSourceName = expectedPackageSource.Name;
			CreateViewModel ();

			Assert.AreEqual (expectedPackageSource, viewModel.SelectedPackageSource.PackageSource);
		}

		[Test]
		public void SelectedPackageSource_OnePackageSourceDefinedButUnknownActivePackageSource_PackageSourceIsMadeActiveSource ()
		{
			CreateProject ();
			var sources = AddTwoPackageSourcesToRegisteredSources ();
			var expectedPackageSource = sources[0];
			packageSourceProvider.ActivePackageSourceName = "Unknown";
			CreateViewModel ();

			Assert.AreEqual (expectedPackageSource, viewModel.SelectedPackageSource.PackageSource);
		}

		[Test]
		public void SelectedPackageSource_Changed_ActivePackageSourceChanged ()
		{
			CreateProject ();
			var packageSources = AddTwoPackageSourcesToRegisteredSources ().ToList ();
			packageSourceProvider.ActivePackageSourceName = packageSources[0].Name;
			CreateViewModel ();
			var expectedPackageSource = packageSources[1];
			var packageSourceViewModels = viewModel.PackageSources.ToList ();

			viewModel.SelectedPackageSource = packageSourceViewModels[2];

			Assert.AreEqual (expectedPackageSource, packageSourceProvider.ActivePackageSource);
		}

		[Test]
		public async Task SelectedPackageSource_PackageSourceChangedAfterReadingPackages_PackagesReadFromNewPackageSourceAndDisplayed ()
		{
			await SetUpTwoPackageSourcesAndViewModelHasReadPackages ();
			viewModel.ReadPackagesTask = null;
			viewModel.PackageFeed.AddPackage ("Test", "1.2");
			ChangeSelectedPackageSourceToSecondNonAggregateSource ();
			await viewModel.ReadPackagesTask;

			Assert.AreEqual (1, viewModel.PackageViewModels.Count);
			Assert.AreEqual ("Test", viewModel.PackageViewModels[0].Id);
			Assert.AreSame ("1.2", viewModel.PackageViewModels[0].Version.ToString ());
		}

		[Test]
		public async Task SelectedPackageSource_PackageSourceChangedAfterReadingPackages_PropertyChangedEventFiredAfterPackagesAreRead ()
		{
			await SetUpTwoPackageSourcesAndViewModelHasReadPackages ();

			int packageCountWhenPropertyChangedEventFired = -1;
			viewModel.PropertyChanged += (sender, e) => {
				packageCountWhenPropertyChangedEventFired = viewModel.PackageViewModels.Count;
			};
			viewModel.ReadPackagesTask = null;
			viewModel.PackageFeed.AddPackage ("Test", "1.0");
			ChangeSelectedPackageSourceToSecondNonAggregateSource ();
			await viewModel.ReadPackagesTask;

			Assert.AreEqual (1, packageCountWhenPropertyChangedEventFired);
		}

		[Test]
		public async Task SelectedPackageSource_PackageSourceChangedButToSameSelectedPackageSource_PackagesAreNotRead ()
		{
			await SetUpTwoPackageSourcesAndViewModelHasReadPackages ();
			viewModel.ReadPackagesTask = null;
			ChangeSelectedPackageSourceToFirstSourceNonAggregateSource ();

			Assert.IsNull (viewModel.ReadPackagesTask);
		}

		[Test]
		public async Task SelectedPackageSource_PackageSourceChangedButToSameSelectedPackageSource_PropertyChangedEventNotFired ()
		{
			await SetUpTwoPackageSourcesAndViewModelHasReadPackages ();

			bool fired = false;
			viewModel.PropertyChanged += (sender, e) => fired = true;
			ChangeSelectedPackageSourceToFirstSourceNonAggregateSource ();

			Assert.IsFalse (fired);
		}

		[Test]
		public async Task ReadPackages_SearchTextAndIncludePrereleaseIsTrue_SearchTextAndIncludePrereleaseUsedWhenQueryingPackageFeed ()
		{
			CreateProject ();
			AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();
			viewModel.IncludePrerelease = true;
			viewModel.SearchTerms = "Test";
			viewModel.PackageFeed.AddPackage ("Test", "1.1.0-alpha");
			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;

			Assert.AreEqual ("Test", viewModel.PackageFeed.SearchText);
			Assert.IsTrue (viewModel.PackageFeed.SearchFilter.IncludePrerelease);
		}

		[Test]
		public async Task ReadPackages_NoSearchTextAndIncludePrereleaseIsFalse_SearchTextAndIncludePrereleaseIsFalseWhenQueryingPackageFeed ()
		{
			CreateProject ();
			AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();
			viewModel.IncludePrerelease = false;
			viewModel.SearchTerms = null;
			viewModel.PackageFeed.AddPackage ("Test", "1.1");
			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;

			Assert.AreEqual (string.Empty, viewModel.PackageFeed.SearchText);
			Assert.IsFalse (viewModel.PackageFeed.SearchFilter.IncludePrerelease);
		}

		[Test]
		public async Task ReadPackages_ExceptionThrownDuringLoad_ExceptionIsShownAsErrorMessage ()
		{
			CreateProject ();
			AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();
			viewModel.LoadPackagesAsyncTask = (loader, token) => {
				return Task.Run (() => {
					throw new Exception ("Invalid url");
				});
			};
			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;

			Assert.IsTrue (viewModel.HasError);
			Assert.AreEqual ("Invalid url", viewModel.ErrorMessage);
		}

		[Test]
		public async Task ReadPackages_OneRecentPackageIsAvailable_RecentPackageIsDisplayedBeforeAnyOtherPackages ()
		{
			CreateProject ();
			var packageSource = AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();
			AddRecentPackage ("Recent", "2.1", packageSource.Name);
			viewModel.PackageFeed.AddPackage ("Test", "1.3");
			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;

			Assert.AreEqual (2, viewModel.PackageViewModels.Count);
			Assert.AreEqual ("Recent", viewModel.PackageViewModels[0].Id);
			Assert.AreEqual ("Test", viewModel.PackageViewModels[1].Id);
		}

		[Test]
		public async Task ReadPackages_OneRecentPackageIsAvailableWhichMatchesPackageFromActiveSource_DuplicatePackageWithSameVersionFromActivePackageSourceIsNotDisplayed ()
		{
			CreateProject ();
			var packageSource = AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();
			AddRecentPackage ("A", "2.1", packageSource.Name);
			viewModel.PackageFeed.AddPackage ("A", "2.5");
			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;

			Assert.AreEqual (1, viewModel.PackageViewModels.Count);
			Assert.IsTrue (viewModel.PackageViewModels[0].IsRecentPackage);
			Assert.AreEqual ("A", viewModel.PackageViewModels[0].Id);
		}

		[Test]
		public async Task ReadPackages_OneRecentPackageIsAvailableAndSearchTermEntered_RecentPackageIsNotDisplayed ()
		{
			CreateProject ();
			var packageSource = AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();
			AddRecentPackage ("A", "2.1", packageSource.Name);
			viewModel.SearchTerms = "A";
			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;

			Assert.AreEqual (0, viewModel.PackageViewModels.Count);
		}

		[Test]
		public async Task HasNextPage_TwoPagesOfData_HasNextPageIsFalseAfterSecondRead ()
		{
			CreateProject ();
			AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();
			viewModel.PackageFeed.AddPackage ("Test", "1.3");
			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;
			bool hasNextPageAfterFirstRead = viewModel.HasNextPage;
			viewModel.ReadPackagesTask = null;
			viewModel.ShowNextPage ();
			await viewModel.ReadPackagesTask;

			Assert.IsTrue (hasNextPageAfterFirstRead);
			Assert.IsFalse (viewModel.HasNextPage);
		}

		[Test]
		public void IsReadingPackages_ReadPackagesNotCalled_ReturnsFalse ()
		{
			CreateProject ();
			AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();

			Assert.IsFalse (viewModel.IsReadingPackages);
		}

		[Test]
		public async Task IsReadingPackages_ReadPackagesCalled_ReturnsTrue ()
		{
			CreateProject ();
			AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();
			bool readingPackages = false;
			viewModel.LoadPackagesAsyncTask = (loader, token) => {
				readingPackages = viewModel.IsReadingPackages;
				return Task.FromResult (0);
			};
			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;

			Assert.IsTrue (readingPackages);
		}

		[Test]
		public async Task ErrorMessage_BackgroundTaskHasAggregateExceptionWithNestedInnerAggregateException_ErrorMessageTakenFromInnerException ()
		{
			CreateProject ();
			AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();
			viewModel.LoadPackagesAsyncTask = (loader, token) => {
				return Task.Run (() => {
					var innerEx1 = new Exception ("Test1");
					var innerEx2 = new Exception ("Test2");
					var innerAggregateEx = new AggregateException (innerEx1, innerEx2);
					var aggregateEx = new AggregateException (innerAggregateEx);
					throw aggregateEx;
				});
			};
			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;

			string expectedErrorMessage = 
				"Test1" + Environment.NewLine +
				"Test2";

			Assert.AreEqual (expectedErrorMessage, viewModel.ErrorMessage);
		}

		[Test]
		public async Task HasError_ErrorMessageDisplayedAndSelectedPageChangedAfterFailure_ReturnsFalse ()
		{
			CreateProject ();
			AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();
			viewModel.LoadPackagesAsyncTask = (loader, token) => {
				return Task.Run (() => {
					throw new Exception ("Error");
				});
			};
			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;
			bool firstError = viewModel.HasError;
			viewModel.ReadPackagesTask = null;
			viewModel.LoadPackagesAsyncTask = null;
			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;

			Assert.IsFalse (viewModel.HasError);
			Assert.AreEqual (string.Empty, viewModel.ErrorMessage);
			Assert.IsTrue (firstError);
		}

		[Test]
		public void IsDisposed_DisposeMethodCalled_ReturnsTrue ()
		{
			CreateProject ();
			CreateViewModel ();
			viewModel.Dispose ();

			Assert.IsTrue (viewModel.IsDisposed);
		}

		[Test]
		public void IsDisposed_DisposeMethodNotCalled_ReturnsFalse ()
		{
			CreateProject ();
			CreateViewModel ();

			Assert.IsFalse (viewModel.IsDisposed);
		}

		[Test]
		public async Task IncludePrerelease_ChangedToTrue_PackagesAreReadAgain ()
		{
			CreateProject ();
			AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();
			viewModel.IncludePrerelease = false;
			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;
			viewModel.ReadPackagesTask = null;

			viewModel.IncludePrerelease = true;

			Assert.IsNotNull (viewModel.ReadPackagesTask);
		}

		[Test]
		public async Task IncludePrerelease_ChangedToFalse_PackagesAreReadAgain ()
		{
			CreateProject ();
			AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();
			viewModel.IncludePrerelease = true;
			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;
			viewModel.ReadPackagesTask = null;

			viewModel.IncludePrerelease = false;

			Assert.IsNotNull (viewModel.ReadPackagesTask);
		}

		[Test]
		public async Task IncludePrerelease_ChangedToTrue_PropertyChangedEventIsFired ()
		{
			CreateProject ();
			AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();
			viewModel.IncludePrerelease = false;
			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;
			viewModel.ReadPackagesTask = null;
			PropertyChangedEventArgs propertyChangedEvent = null;
			viewModel.PropertyChanged += (sender, e) => propertyChangedEvent = e;

			viewModel.IncludePrerelease = true;

			Assert.IsNull (propertyChangedEvent.PropertyName);
		}

		[Test]
		public async Task IncludePrerelease_SetToTrueWhenAlreadyTrue_PropertyChangedEventIsNotFired ()
		{
			CreateProject ();
			AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();
			viewModel.IncludePrerelease = true;
			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;
			viewModel.ReadPackagesTask = null;
			bool fired = false;
			viewModel.PropertyChanged += (sender, e) => fired = true;

			viewModel.IncludePrerelease = true;

			Assert.IsFalse (fired);
		}

		[Test]
		public async Task CheckedPackageViewModels_TwoPackagesAndOnePackageIsChecked_ReturnsOneCheckedPackage ()
		{
			CreateProject ();
			AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();
			viewModel.PackageFeed.AddPackage ("A", "1.2");
			viewModel.PackageFeed.AddPackage ("B", "1.3");
			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;

			viewModel.PackageViewModels[0].IsChecked = true;

			Assert.AreEqual (2, viewModel.PackageViewModels.Count);
			Assert.AreEqual (1, viewModel.CheckedPackageViewModels.Count);
			Assert.AreEqual ("A", viewModel.CheckedPackageViewModels[0].Id);
		}

		[Test]
		public async Task CheckedPackageViewModels_OnePackageIsCheckedThenNewSearchReturnsNoPackages_ReturnsCheckedPackageEvenWhenNotVisible ()
		{
			CreateProject ();
			AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();
			viewModel.PackageFeed.AddPackage ("MyPackage", "1.0");
			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;
			viewModel.PackageViewModels[0].IsChecked = true;
			viewModel.PackageFeed = new FakePackageFeed ();
			viewModel.PackageFeed.AddPackage ("AnotherPackage", "1.2");
			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;

			Assert.AreEqual (1, viewModel.CheckedPackageViewModels.Count);
			Assert.AreEqual ("MyPackage", viewModel.CheckedPackageViewModels[0].Id);
		}

		[Test]
		public async Task CheckedPackageViewModels_OnePackageIsCheckedAndThenUnchecked_ReturnsNoCheckedPackages ()
		{
			CreateProject ();
			AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();
			viewModel.PackageFeed.AddPackage ("MyPackage", "1.2");
			viewModel.PackageFeed.AddPackage ("Z-Package", "1.3");
			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;
			var packageViewModel = viewModel.PackageViewModels[0];
			packageViewModel.IsChecked = true;
			packageViewModel.IsChecked = false;

			Assert.AreEqual (0, viewModel.CheckedPackageViewModels.Count);
		}

		[Test]
		public async Task PackageViewModels_OnePackageIsCheckedAndNewSearchReturnsOriginalPackage_PackageViewHasIsCheckedSetToTrue ()
		{
			CreateProject ();
			AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();
			viewModel.PackageFeed.AddPackage ("MyPackage", "1.0");
			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;
			viewModel.PackageViewModels[0].IsChecked = true;
			viewModel.PackageFeed = new FakePackageFeed ();
			viewModel.PackageFeed.AddPackage ("MyPackage", "1.0");
			viewModel.Search ();
			await viewModel.ReadPackagesTask;

			var packageViewModel = viewModel.PackageViewModels[0];

			Assert.AreEqual ("MyPackage", packageViewModel.Id);
			Assert.IsTrue (packageViewModel.IsChecked);
		}

		[Test]
		public async Task PackageViewModels_OnePackageIsCheckedAndNewSearchReturnsOriginalPackageButWithDifferentVersion_PackageViewIsCheckedAndSameVersionIsSelectedAsTheFirstPackageChecked ()
		{
			CreateProject ();
			AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();
			viewModel.PackageFeed.AddPackage ("MyPackage", "1.0");
			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;
			viewModel.PackageViewModels[0].IsChecked = true;
			viewModel.PackageFeed = new FakePackageFeed ();
			viewModel.PackageFeed.AddPackage ("MyPackage", "1.1");
			viewModel.Search ();
			await viewModel.ReadPackagesTask;

			var packageViewModel = viewModel.PackageViewModels[0];

			Assert.AreEqual ("MyPackage", packageViewModel.Id);
			Assert.IsTrue (packageViewModel.IsChecked);
			Assert.AreEqual ("1.0", packageViewModel.SelectedVersion.ToString ());
			Assert.AreEqual (2, packageViewModel.Versions.Count);
			Assert.AreEqual ("1.1", packageViewModel.Versions[0].ToString ());
			Assert.AreEqual ("1.0", packageViewModel.Versions[1].ToString ());
		}

		[Test]
		public async Task CheckedPackageViewModels_OnePackageIsCheckedAndNewSearchReturnsMultipleVersionsOfOriginalPackage_OnlyPackageWithSameVersionIsChecked ()
		{
			CreateProject ();
			AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();
			viewModel.PackageFeed.AddPackage ("MyPackage", "1.0");
			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;
			viewModel.PackageViewModels[0].IsChecked = true;
			viewModel.PackageFeed = new FakePackageFeed ();
			viewModel.PackageFeed.AddPackage ("MyPackage", "1.1");
			viewModel.PackageFeed.AddPackage ("MyPackage", "1.2");
			viewModel.PackageFeed.AddPackage ("MyPackage", "1.0");
			viewModel.PackageFeed.AddPackage ("MyPackage", "1.3");
			viewModel.PackageFeed.AddPackage ("MyPackage", "1.4");
			viewModel.Search ();
			await viewModel.ReadPackagesTask;

			var packageViewModel = viewModel.CheckedPackageViewModels.FirstOrDefault ();

			Assert.AreEqual ("MyPackage", packageViewModel.Id);
			Assert.AreEqual ("1.4", packageViewModel.Version.ToString ());
			Assert.AreEqual ("1.0", packageViewModel.SelectedVersion.ToString ());
			Assert.AreEqual (1, viewModel.CheckedPackageViewModels.Count);
		}

		[Test]
		public async Task PackageViewModels_OnePackageIsCheckedAndNewSearchReturnsOriginalPackageWhichIsThenUncheckedByUser_NoCheckedPackageViewModels ()
		{
			CreateProject ();
			AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();
			viewModel.PackageFeed.AddPackage ("MyPackage", "1.0");
			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;
			viewModel.PackageViewModels [0].IsChecked = true;
			viewModel.PackageFeed = new FakePackageFeed ();
			viewModel.PackageFeed.AddPackage ("MyPackage", "1.0");
			viewModel.Search ();
			await viewModel.ReadPackagesTask;
			var packageViewModel = viewModel.PackageViewModels[0];

			viewModel.PackageViewModels[0].IsChecked = false;

			Assert.AreEqual (0, viewModel.CheckedPackageViewModels.Count);
		}

		[Test]
		public async Task CheckedPackageViewModels_OnePackageVersionIsCheckedThenDifferentVersionChecked_OldVersionIsUnchecked ()
		{
			CreateProject ();
			AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();
			viewModel.PackageFeed.AddPackage ("MyPackage", "1.1"); // Old
			viewModel.PackageFeed.AddPackage ("MyPackage", "1.2");
			viewModel.PackageFeed.AddPackage ("MyPackage", "1.0");
			viewModel.PackageFeed.AddPackage ("MyPackage", "1.3"); // New
			viewModel.PackageFeed.AddPackage ("MyPackage", "1.4");
			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;
			var oldPackageVersionViewModel = viewModel
				.PackageViewModels
				.First (item => item.Version.ToString () == "1.1");
			var newPackageVersionViewModel = viewModel
				.PackageViewModels
				.First (item => item.Version.ToString () == "1.3");
			oldPackageVersionViewModel.IsChecked = true;

			newPackageVersionViewModel.IsChecked = true;

			Assert.AreEqual (1, viewModel.CheckedPackageViewModels.Count);
			Assert.AreEqual ("MyPackage", viewModel.CheckedPackageViewModels[0].Id);
			Assert.AreEqual ("1.3", viewModel.CheckedPackageViewModels[0].Version.ToString ());
			Assert.IsFalse (oldPackageVersionViewModel.IsChecked);
		}

		[Test]
		public async Task Log_ErrorReportedForOnePackageSource_ErrorDisplayed ()
		{
			CreateProject ();
			AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();
			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;
			bool propertyChangedEventFired = false;
			viewModel.PropertyChanged += (sender, e) => propertyChangedEventFired = true;
			var logger = viewModel as INuGetUILogger;
			logger.Log (MessageLevel.Error, "Error");

			Assert.IsTrue (viewModel.HasError);
			Assert.AreEqual ("Error", viewModel.ErrorMessage);
			Assert.IsTrue (propertyChangedEventFired);
		}

		[Test]
		public async Task Log_TwoPackageSourcesOneReportsErrorWhenAggregateSourceSelected_OnePackageSourceCouldNotBeReachedErrorDisplayed ()
		{
			CreateProject ();
			AddTwoPackageSourcesToRegisteredSources ();
			CreateViewModel ();
			viewModel.SelectedPackageSource = viewModel.PackageSources.First ();
			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;
			var logger = viewModel as INuGetUILogger;
			logger.Log (MessageLevel.Error, "Test error occurred");

			Assert.IsTrue (viewModel.HasError);
			Assert.IsTrue (viewModel.ErrorMessage.Contains ("Test error occurred"));
			Assert.IsTrue (viewModel.ErrorMessage.Contains ("Some package sources could not be reached."));
		}

		[Test]
		public async Task Log_TwoPackageSourcesBothReportErrorWhenAggregateSourceSelected_AllPackageSourceCouldNotBeReachedErrorDisplayed ()
		{
			CreateProject ();
			AddTwoPackageSourcesToRegisteredSources ();
			CreateViewModel ();
			viewModel.SelectedPackageSource = viewModel.PackageSources.First ();
			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;
			var logger = viewModel as INuGetUILogger;
			logger.Log (MessageLevel.Error, "Test error1 occurred");
			logger.Log (MessageLevel.Error, "Test error2 occurred");

			Assert.IsTrue (viewModel.HasError);
			Assert.IsTrue (viewModel.ErrorMessage.Contains ("Test error1 occurred"));
			Assert.IsTrue (viewModel.ErrorMessage.Contains ("Test error2 occurred"));
			Assert.IsTrue (viewModel.ErrorMessage.Contains ("All package sources could not be reached."));
		}

		[Test]
		public async Task OnInstallingSelectedPackages_OnePackageSelected_PackageAddedToRecentPackages ()
		{
			CreateProject ();
			var source = AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();
			viewModel.PackageFeed.AddPackage ("Test", "1.0");
			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;
			viewModel.SelectedPackage = viewModel.PackageViewModels[0];

			viewModel.OnInstallingSelectedPackages ();

			var package = viewModel.RecentPackagesRepository.GetPackages (source.Name).Single ();
			Assert.AreEqual ("Test", package.Id);
		}

		[Test]
		public async Task OnInstallingSelectedPackages_OnePackageChecked_PackageAddedToRecentPackages ()
		{
			CreateProject ();
			var source = AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();
			viewModel.PackageFeed.AddPackage ("A", "1.0");
			viewModel.PackageFeed.AddPackage ("B", "1.2");
			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;
			viewModel.PackageViewModels[1].IsChecked = true;

			viewModel.OnInstallingSelectedPackages ();

			var package = viewModel.RecentPackagesRepository.GetPackages (source.Name).Single ();
			Assert.AreEqual ("B", package.Id);
		}

		[Test]
		public void IsOlderPackageInstalled_NoPackagesInstalledInProject_ReturnsFalse ()
		{
			CreateProject ();
			AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();

			bool result = viewModel.IsOlderPackageInstalled ("Test", new NuGetVersion ("1.0"));

			Assert.IsFalse (result);
		}

		[Test]
		public async Task IsOlderPackageInstalled_SamePackageVersionInstalledInProject_ReturnsFalse ()
		{
			CreateProject ();
			var nugetProject = CreateNuGetProjectForProject ();
			nugetProject.AddPackageReference ("Test", "1.0");
			AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();
			await viewModel.GetPackagesInstalledInProjectTask;

			bool result = viewModel.IsOlderPackageInstalled ("Test", new NuGetVersion ("1.0"));

			Assert.IsFalse (result);
		}

		[Test]
		public async Task IsOlderPackageInstalled_OlderPackageVersionInstalledInProject_ReturnsTrue ()
		{
			CreateProject ();
			var nugetProject = CreateNuGetProjectForProject ();
			nugetProject.AddPackageReference ("Test", "1.0");
			AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();
			await viewModel.GetPackagesInstalledInProjectTask;

			bool result = viewModel.IsOlderPackageInstalled ("Test", new NuGetVersion ("1.1"));

			Assert.IsTrue (result);
		}

		[Test]
		public async Task ReadPackages_RecentPackageWasCheckedWhenInstalled_RecentPackageIsNotCheckedWhenOpeningAddPackagesDialogAgain ()
		{
			CreateProject ();
			var packageSource = AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();
			var recentPackage = CreateRecentPackage ("Recent", "2.1", packageSource.Name);
			recentPackage.IsChecked = true;
			viewModel.RecentPackagesRepository.AddPackage (recentPackage, packageSource.Name);
			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;

			Assert.AreEqual (1, viewModel.PackageViewModels.Count);
			Assert.AreEqual ("Recent", viewModel.PackageViewModels[0].Id);
			Assert.IsFalse (recentPackage.IsChecked);
		}

		[Test]
		public async Task ReadPackages_OneRecentPackageIsChecked_RecentPackageIsInCheckedPackagesList ()
		{
			CreateProject ();
			var packageSource = AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();
			AddRecentPackage ("Recent", "2.1", packageSource.Name);
			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;
			var package = viewModel.PackageViewModels.Single ();
			package.IsChecked = true;

			var checkedPackage = viewModel.CheckedPackageViewModels.Single ();
			Assert.AreEqual ("Recent", package.Id);
			Assert.AreEqual ("Recent", checkedPackage.Id);
		}

		[Test]
		public async Task OnInstallingSelectedPackages_OnePackageChecked_RecentPackageParentIsCleared ()
		{
			CreateProject ();
			var source = AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();
			viewModel.PackageFeed.AddPackage ("A", "1.0");
			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;
			viewModel.PackageViewModels[0].IsChecked = true;

			viewModel.OnInstallingSelectedPackages ();

			var package = viewModel.RecentPackagesRepository.GetPackages (source.Name).Single ();
			Assert.AreEqual ("A", package.Id);
			Assert.IsNull (package.Parent);
		}

		[Test]
		public async Task OnInstallingSelectedPackages_OnePackageSelected_RecentPackageParentIsCleared ()
		{
			CreateProject ();
			var source = AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();
			viewModel.PackageFeed.AddPackage ("A", "1.0");
			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;
			viewModel.SelectedPackage = viewModel.PackageViewModels.Single ();

			viewModel.OnInstallingSelectedPackages ();

			var package = viewModel.RecentPackagesRepository.GetPackages (source.Name).Single ();
			Assert.AreEqual ("A", package.Id);
			Assert.IsNull (package.Parent);
		}

		[Test]
		public async Task ReadPackages_RecentPackageWasPrereleaseAndNowSearchingForNonPrerelease_RecentPackageIsNotDisplayed ()
		{
			CreateProject ();
			var packageSource = AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();
			var recentPackage = AddRecentPackage ("Recent1", "2.1", packageSource.Name);
			recentPackage.SelectedVersion = new NuGetVersion ("2.1-beta1");
			AddRecentPackage ("Recent2", "1.2", packageSource.Name);
			viewModel.IncludePrerelease = false;
			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;

			Assert.AreEqual (1, viewModel.PackageViewModels.Count);
			Assert.AreEqual ("Recent2", viewModel.PackageViewModels[0].Id);
		}

		[Test]
		public async Task ReadPackages_RecentPackageWasPrereleaseAndNowSearchingForPrerelease_RecentPackageIsDisplayed ()
		{
			CreateProject ();
			var packageSource = AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();
			var recentPackage = AddRecentPackage ("Recent1", "2.1", packageSource.Name);
			recentPackage.SelectedVersion = new NuGetVersion ("2.1-beta1");
			AddRecentPackage ("Recent2", "1.2", packageSource.Name);
			viewModel.IncludePrerelease = true;
			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;

			Assert.AreEqual (2, viewModel.PackageViewModels.Count);
			Assert.AreEqual ("Recent2", viewModel.PackageViewModels[0].Id);
			Assert.AreEqual ("Recent1", viewModel.PackageViewModels[1].Id);
		}

		[Test]
		public async Task ReadPackages_NonPrereleaseRecentPackageWasInstalledWhenSearchingForPrereleaseAndNowSearchingForNonPrerelease_RecentPackageIsDisplayedWithoutCachedPrereleaseVersionsDisplayed ()
		{
			CreateProject ();
			var packageSource = AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();
			var recentPackage = AddRecentPackage ("Recent1", "2.1", packageSource.Name);
			recentPackage.SelectedVersion = new NuGetVersion ("2.1");
			recentPackage.Versions.Add (new NuGetVersion ("2.1"));
			recentPackage.Versions.Add (new NuGetVersion ("2.0"));
			recentPackage.Versions.Add (new NuGetVersion ("2.0-beta1"));
			recentPackage.Versions.Add (new NuGetVersion ("2.0-beta2"));
			viewModel.IncludePrerelease = false;
			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;

			Assert.AreEqual (1, viewModel.PackageViewModels.Count);
			Assert.AreEqual ("Recent1", viewModel.PackageViewModels[0].Id);
			Assert.AreEqual (2, recentPackage.Versions.Count);
			Assert.AreEqual ("2.1", recentPackage.Versions[0].ToString ());
			Assert.AreEqual ("2.0", recentPackage.Versions[1].ToString ());
		}

		[Test]
		public async Task ReadPackages_NonPrereleaseRecentPackageWasInstalledWhenSearchingForPrereleaseAndSearchingForPrerelease_RecentPackageIsDisplayedWithCachedPrereleaseVersionsDisplayed ()
		{
			CreateProject ();
			var packageSource = AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();
			var recentPackage = AddRecentPackage ("Recent1", "2.1", packageSource.Name);
			recentPackage.SelectedVersion = new NuGetVersion ("2.1");
			recentPackage.Versions.Add (new NuGetVersion ("2.1"));
			recentPackage.Versions.Add (new NuGetVersion ("2.0"));
			recentPackage.Versions.Add (new NuGetVersion ("2.0-beta1"));
			recentPackage.Versions.Add (new NuGetVersion ("2.0-beta2"));
			viewModel.IncludePrerelease = true;
			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;

			Assert.AreEqual (1, viewModel.PackageViewModels.Count);
			Assert.AreEqual ("Recent1", viewModel.PackageViewModels[0].Id);
			Assert.AreEqual (4, recentPackage.Versions.Count);
		}

		[Test]
		public void PackageSources_AggregateSourceSelectedThenDialogClosedAndReopened_ReturnsTwoPackageSourcesPlusAggregatePackageSource ()
		{
			CreateProject ();
			AddTwoPackageSourcesToRegisteredSources ();
			CreateViewModel ();
			viewModel.SelectedPackageSource = viewModel.PackageSources.First ();
			Assert.IsTrue (viewModel.SelectedPackageSource.IsAggregate);
			CreateViewModel ();

			var selectedPackageSource = viewModel.SelectedPackageSource;

			Assert.AreEqual ("All Sources", selectedPackageSource.Name);
			Assert.IsTrue (selectedPackageSource.IsAggregate);
		}

		[Test]
		public async Task Consolidate_ThreeProjectsDifferentPackageVersionInTwoProjects_ProjectVersionAndSelectionInformationAvailable ()
		{
			CreateProject ();
			project.Name = "LibC";
			var nugetProject = CreateNuGetProjectForProject (project);
			nugetProject.AddPackageReference ("Test", "0.1");

			var project2 = AddProjectToSolution ("LibA");
			nugetProject = CreateNuGetProjectForProject (project2);

			var project3 = AddProjectToSolution ("LibB");
			nugetProject = CreateNuGetProjectForProject (project3);
			nugetProject.AddPackageReference ("Test", "0.2");

			AddOnePackageSourceToRegisteredSources ();
			CreateViewModelForSolution ();
			viewModel.PackageFeed.AddPackage ("Test", "0.2");
			viewModel.PageSelected = ManagePackagesPage.Consolidate;

			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;

			var package = viewModel.PackageViewModels.Single ();
			viewModel.SelectedPackage = package;
			Assert.AreEqual ("Test", package.Id);
			Assert.AreEqual ("0.2", package.Version.ToString ());

			Assert.AreEqual (3, viewModel.ProjectViewModels.Count);

			var projectViewModel = viewModel.ProjectViewModels [0];
			Assert.AreEqual ("LibA", projectViewModel.ProjectName);
			Assert.AreEqual ("", projectViewModel.PackageVersion);
			Assert.IsFalse (projectViewModel.IsChecked);

			projectViewModel = viewModel.ProjectViewModels [1];
			Assert.AreEqual ("LibB", projectViewModel.ProjectName);
			Assert.AreEqual ("0.2", projectViewModel.PackageVersion);
			Assert.IsTrue (projectViewModel.IsChecked);

			projectViewModel = viewModel.ProjectViewModels [2];
			Assert.AreEqual ("LibC", projectViewModel.ProjectName);
			Assert.AreEqual ("0.1", projectViewModel.PackageVersion);
			Assert.IsTrue (projectViewModel.IsChecked);

			Assert.IsTrue (viewModel.CanConsolidate ());

			// Expecting one action since Test 0.2 is already installed in LibB.
			var actions = viewModel.CreateConsolidatePackageActions (package).ToList ();
			Assert.AreEqual (1, actions.Count);
			var action = actions [0] as InstallNuGetPackageAction;
			Assert.AreEqual (PackageActionType.Install, action.ActionType);
			Assert.AreEqual ("Test", action.PackageId);
			Assert.AreEqual ("0.2", action.Version.ToString ());
			Assert.AreEqual ("LibC", action.Project.Name);

			// Check LibA, which does not have the package.
			viewModel.ProjectViewModels [0].IsChecked = true;

			// Uncheck LibC.
			viewModel.ProjectViewModels [2].IsChecked = false;

			Assert.IsTrue (viewModel.CanConsolidate ());

			actions = viewModel.CreateConsolidatePackageActions (package).ToList ();
			Assert.AreEqual (1, actions.Count);
			action = actions [0] as InstallNuGetPackageAction;
			Assert.AreEqual (PackageActionType.Install, action.ActionType);
			Assert.AreEqual ("Test", action.PackageId);
			Assert.AreEqual ("0.2", action.Version.ToString ());
			Assert.AreEqual ("LibA", action.Project.Name);

			// Uncheck all projects. No items checked so cannot consolidate.
			viewModel.ProjectViewModels [0].IsChecked = false;
			viewModel.ProjectViewModels [1].IsChecked = false;
			viewModel.ProjectViewModels [2].IsChecked = false;
			Assert.IsFalse (viewModel.CanConsolidate ());

			// Check LibB which has same package.
			viewModel.ProjectViewModels [1].IsChecked = true;
			Assert.IsFalse (viewModel.CanConsolidate ());

			viewModel.SelectedPackage = null;
			Assert.AreEqual (0, viewModel.ProjectViewModels.Count);
			Assert.IsFalse (viewModel.CanConsolidate ());
		}

		[Test]
		public async Task CheckedPackageViewModels_DifferentTabPageSelected_CheckedPackagesCleared ()
		{
			CreateProject ();
			AddOnePackageSourceToRegisteredSources ();
			CreateViewModel ();
			viewModel.PackageFeed.AddPackage ("A", "1.2");
			viewModel.PackageFeed.AddPackage ("B", "1.3");
			viewModel.ReadPackages ();
			await viewModel.ReadPackagesTask;

			viewModel.PackageViewModels [0].IsChecked = true;

			Assert.AreEqual (2, viewModel.PackageViewModels.Count);
			Assert.AreEqual (1, viewModel.CheckedPackageViewModels.Count);
			Assert.AreEqual ("A", viewModel.CheckedPackageViewModels [0].Id);

			viewModel.PageSelected = ManagePackagesPage.Consolidate;

			Assert.AreEqual (0, viewModel.CheckedPackageViewModels.Count);
		}
	}
}

