//
// ManagePackagesSearchResultViewModelTests.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.PackageManagement.Tests.Helpers;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.PackageManagement.UI;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class ManagePackagesSearchResultViewModelTests
	{
		TestableManagePackagesSearchResultViewModel viewModel;
		PackageItemListViewModel packageItemListViewModel;
		TestableManagePackagesViewModel parent;
		FakePackageMetadataProvider metadataProvider;
		List<VersionInfo> packageVersions;
		FakePackageSearchMetadata packageSearchMetadata;
		FakePackageMetadataResource packageMetadataResource;

		void CreateViewModel ()
		{
			CreatePackageItemListViewModel ();
			CreateViewModel (packageItemListViewModel);
		}

		PackageItemListViewModel CreatePackageItemListViewModel ()
		{
			packageVersions = new List<VersionInfo> ();
			packageItemListViewModel = new PackageItemListViewModel {
				Id = "TestPackage",
				Version = new NuGetVersion ("1.2.3"),
				Versions = AsyncLazy.New (() => {
					return Task.FromResult (packageVersions.AsEnumerable ());
				})
			};
			return packageItemListViewModel;
		}

		void CreateViewModel (PackageItemListViewModel package)
		{
			metadataProvider = new FakePackageMetadataProvider ();
			packageSearchMetadata = metadataProvider.AddPackageMetadata (package.Id, package.Version.ToString ());
			var solutionManager = new FakeSolutionManager ();
			var project = new FakeDotNetProject ();
			parent = new TestableManagePackagesViewModel (solutionManager, project);
			viewModel = new TestableManagePackagesSearchResultViewModel (parent, packageItemListViewModel);
		}

		Task LoadPackageMetadata ()
		{
			viewModel.LoadPackageMetadata (metadataProvider, CancellationToken.None);
			return viewModel.LoadPackageMetadataTask;
		}

		void AddSinglePackageDependencyToSearchMetadata ()
		{
			var dependencies = new [] {
				new PackageDependency ("Test")
			};
			AddDependenciesToSearchMetadata (dependencies);
		}

		void AddDependenciesToSearchMetadata (params PackageDependency [] dependencies)
		{
			var dependencyGroup = new PackageDependencyGroup (
				new NuGetFramework ("any"),
				dependencies);
			packageSearchMetadata.DependencySetsList.Add (dependencyGroup);
		}

		Task ReadVersions ()
		{
			viewModel.LoadPackageMetadata (metadataProvider, CancellationToken.None);
			return viewModel.ReadVersionsTask;
		}

		void AddVersionsToPackageItemListViewModel (params string[] versions)
		{
			var versionInfos = versions
				.Select (version => new NuGetVersion (version))
				.Select (version => new VersionInfo (version));
			packageVersions.AddRange (versionInfos);
		}

		void SelectPackageSourceInParentViewModel ()
		{
			var metadataResourceProvider = new FakePackageMetadataResourceProvider ();
			packageMetadataResource = metadataResourceProvider.PackageMetadataResource;
			var source = new PackageSource ("http://test.com");
			var providers = new INuGetResourceProvider[] {
				metadataResourceProvider
			};
			var sourceRepository = new SourceRepository (source, providers);
			parent.SelectedPackageSource = new SourceRepositoryViewModel (sourceRepository);
		}

		async Task WaitForLoadPackageMetadataTask ()
		{
			const int maximumDelayCount = 10;
			int count = 0;
			while (viewModel.LoadPackageMetadataTask == null && count < maximumDelayCount) {
				await Task.Delay (10);
				count++;
			}

			if (viewModel.LoadPackageMetadataTask != null) {
				await viewModel.LoadPackageMetadataTask;
			} else {
				throw new ApplicationException ("LoadPackageMetadataTask never started.");
			}
		}

		[Test]
		public void HasLicenseUrl_PackageHasLicenseUrl_ReturnsTrue ()
		{
			CreateViewModel ();
			packageItemListViewModel.LicenseUrl = new Uri ("http://monodevelop.com");

			Assert.IsTrue (viewModel.HasLicenseUrl);
		}

		[Test]
		public void HasLicenseUrl_PackageHasNoLicenseUrl_ReturnsFalse ()
		{
			CreateViewModel ();
			packageItemListViewModel.LicenseUrl = null;

			Assert.IsFalse (viewModel.HasLicenseUrl);
		}

		[Test]
		public void HasProjectUrl_PackageHasProjectUrl_ReturnsTrue ()
		{
			CreateViewModel ();
			packageItemListViewModel.ProjectUrl = new Uri ("http://monodevelop.com");

			Assert.IsTrue (viewModel.HasProjectUrl);
		}

		[Test]
		public void HasProjectUrl_PackageHasNoProjectUrl_ReturnsFalse ()
		{
			CreateViewModel ();
			packageItemListViewModel.ProjectUrl = null;

			Assert.IsFalse (viewModel.HasProjectUrl);
		}

		[Test]
		public void IsDependencyInformationAvailable_DependenciesNotRead_ReturnsFalse ()
		{
			CreateViewModel ();

			Assert.IsFalse (viewModel.IsDependencyInformationAvailable);
		}

		[Test]
		public async Task HasDependencies_PackageHasNoDependencies_ReturnsFalse ()
		{
			CreateViewModel ();
			await LoadPackageMetadata ();

			Assert.IsFalse (viewModel.HasDependencies);
		}

		[Test]
		public async Task HasDependencies_PackageHasDependency_ReturnsTrue ()
		{
			CreateViewModel ();
			AddSinglePackageDependencyToSearchMetadata ();
			await LoadPackageMetadata ();

			Assert.IsTrue (viewModel.HasDependencies);
		}

		[Test]
		public async Task HasNoDependencies_PackageHasNoDependencies_ReturnsTrue ()
		{
			CreateViewModel ();
			await LoadPackageMetadata ();

			Assert.IsTrue (viewModel.HasNoDependencies);
		}

		[Test]
		public async Task HasNoDependencies_PackageHasOneDependency_ReturnsFalse ()
		{
			CreateViewModel ();
			AddSinglePackageDependencyToSearchMetadata ();
			await LoadPackageMetadata ();

			Assert.IsFalse (viewModel.HasNoDependencies);
		}

		[Test]
		public void HasDownloadCount_DownloadCountIsZero_ReturnsTrue ()
		{
			CreateViewModel ();
			packageItemListViewModel.DownloadCount = 0;

			Assert.IsTrue (viewModel.HasDownloadCount);
		}

		[Test]
		public void HasDownloadCount_DownloadCountIsMinusOne_ReturnsFalse ()
		{
			CreateViewModel ();
			packageItemListViewModel.DownloadCount = -1;

			Assert.IsFalse (viewModel.HasDownloadCount);
		}

		[Test]
		public void HasLastPublished_PackageHasPublishedDate_ReturnsTrue ()
		{
			CreateViewModel ();
			packageItemListViewModel.Published = new DateTime (2011, 1, 2);

			Assert.IsTrue (viewModel.HasLastPublished);
		}

		[Test]
		public void HasLastPublished_PackageHasNoPublishedDate_ReturnsFalse ()
		{
			CreateViewModel ();
			packageItemListViewModel.Published = null;

			Assert.IsFalse (viewModel.HasLastPublished);
		}

		[Test]
		public void LastPublished_PackageHasPublishedDate_ReturnsPackagePublishedDate ()
		{
			CreateViewModel ();
			packageItemListViewModel.Published = new DateTime (2011, 1, 2);

			Assert.AreEqual (packageItemListViewModel.Published, viewModel.LastPublished);
		}

		[Test]
		public void LastPublished_PackageHasNoPublishedDate_ReturnsNull ()
		{
			CreateViewModel ();
			packageItemListViewModel.Published = null;

			Assert.IsNull (viewModel.LastPublished);
		}

		[Test]
		public void Summary_PackageHasSummary_PackageSummaryReturned ()
		{
			CreateViewModel ();
			packageItemListViewModel.Summary = "Expected summary";

			string summary = viewModel.Summary;

			Assert.AreEqual ("Expected summary", summary);
		}

		[Test]
		public void Summary_PackageHasDescriptionButNoSummary_PackageDescriptionReturned ()
		{
			CreateViewModel ();
			packageItemListViewModel.Summary = String.Empty;
			packageItemListViewModel.Description = "Expected description";

			string summary = viewModel.Summary;

			Assert.AreEqual ("Expected description", summary);
		}

		[Test]
		public void Summary_PackageSummaryHasNewLinesAndExtraWhitespaceAtStartOfLine_SummaryHasNoNewLinesAndWhitespaceIsMinimized ()
		{
			CreateViewModel ();
			packageItemListViewModel.Summary = "First.\n    Second.\n    Third.\n";

			string summary = viewModel.Summary;

			Assert.AreEqual ("First. Second. Third.", summary);
		}

		[Test]
		public void Name_PackageHasIdButNoTitle_ReturnsPackageId ()
		{
			CreateViewModel ();
			packageItemListViewModel.Id = "MyPackage";

			string name = viewModel.Name;

			Assert.AreEqual ("MyPackage", name);
		}

		[Test]
		public void Name_PackageHasIdAndTitle_ReturnsPackageId ()
		{
			CreateViewModel ();
			packageItemListViewModel.Id = "MyPackage";
			packageItemListViewModel.Title = "My Package Title";

			string name = viewModel.Name;

			Assert.AreEqual ("My Package Title", name);
		}

		[Test]
		public void GetNameMarkup_PackageHasId_ReturnsBoldPackageId ()
		{
			CreateViewModel ();
			packageItemListViewModel.Id = "MyPackage";

			string name = viewModel.GetNameMarkup ();

			Assert.AreEqual ("<b>MyPackage</b>", name);
		}

		[Test]
		public void HasNoGalleryUrl_PackageHasNoGalleryUrl_ReturnsFalse ()
		{
			CreateViewModel ();

			bool result = viewModel.HasGalleryUrl;

			Assert.IsFalse (result);
		}

		[Test]
		public void IsGalleryUrlMissing_PackageHasNoGalleryUrl_ReturnsTrue ()
		{
			CreateViewModel ();

			bool result = viewModel.HasNoGalleryUrl;

			Assert.IsTrue (result);
		}

		[Test]
		public void GetDownloadCountOrVersionDisplayText_PackageDownloadCountIsMinusOne_ReturnsEmptyString ()
		{
			CreateViewModel ();
			packageItemListViewModel.DownloadCount = -1;

			string result = viewModel.GetDownloadCountOrVersionDisplayText ();

			Assert.AreEqual (String.Empty, result);
		}

		[Test]
		public void GetDownloadCountOrVersionDisplayText_PackageHasTenThousandDownloads_ReturnsDownloadCountFormattedForLocale ()
		{
			CreateViewModel ();
			packageItemListViewModel.DownloadCount = 10000;

			string result = viewModel.GetDownloadCountOrVersionDisplayText ();

			string expectedResult = 10000.ToString ("N0");
			Assert.AreEqual (expectedResult, result);
		}

		[Test]
		public void GetDownloadCountOrVersionDisplayText_PackageWasPartOfGroupIncludingAllVersions_ReturnsVersionNumberInsteadOfDownloadCount ()
		{
			CreateViewModel ();
			viewModel.ShowVersionInsteadOfDownloadCount = true;
			packageItemListViewModel.DownloadCount = 10000;
			packageItemListViewModel.Version = new NuGetVersion ("1.2.3.4");

			string result = viewModel.GetDownloadCountOrVersionDisplayText ();

			Assert.AreEqual ("1.2.3.4", result);
		}

		[Test]
		public void SelectedVersion_PackageHasVersion_UsesLatestPackageVersionByDefault ()
		{
			var package = CreatePackageItemListViewModel ();
			package.Version = new NuGetVersion ("1.2.3");
			CreateViewModel (package);

			Assert.AreEqual ("1.2.3", viewModel.SelectedVersion.ToString ());
		}

		[Test]
		public void UpdateFromPreviouslyCheckedViewModel_VersionSelectedIsLatestVersion_VersionsIsEmpty ()
		{
			var package = CreatePackageItemListViewModel ();
			package.Version = new NuGetVersion ("1.2.3");
			CreateViewModel (package);
			var checkedViewModel = viewModel;
			checkedViewModel.IsChecked = true;
			package = CreatePackageItemListViewModel ();
			package.Version = new NuGetVersion ("1.2.3");
			CreateViewModel (package);

			viewModel.UpdateFromPreviouslyCheckedViewModel (checkedViewModel);

			Assert.IsTrue (viewModel.IsChecked);
			Assert.AreEqual ("1.2.3", viewModel.SelectedVersion.ToString ());
			Assert.AreEqual (0, viewModel.Versions.Count);
		}

		/// <summary>
		/// Here we ensure that the latest and selected version is available from the Versions
		/// property otherwise the selected version cannot be selected in the Versions drop down
		/// in the UI.
		/// </summary>
		[Test]
		public void UpdateFromPreviouslyCheckedViewModel_VersionSelectedIsDifferentToLatestVersion_VersionsHasLatestAndSelectedVersion ()
		{
			var package = CreatePackageItemListViewModel ();
			package.Version = new NuGetVersion ("1.2.3");
			CreateViewModel (package);
			var checkedViewModel = viewModel;
			checkedViewModel.IsChecked = true;
			checkedViewModel.SelectedVersion = new NuGetVersion ("1.0.2");
			package = CreatePackageItemListViewModel ();
			package.Version = new NuGetVersion ("1.2.3");
			CreateViewModel (package);

			viewModel.UpdateFromPreviouslyCheckedViewModel (checkedViewModel);

			Assert.IsTrue (viewModel.IsChecked);
			Assert.AreEqual ("1.0.2", viewModel.SelectedVersion.ToString ());
			Assert.AreEqual (2, viewModel.Versions.Count);
			Assert.AreEqual ("1.2.3", viewModel.Versions[0].ToString ());
			Assert.AreEqual ("1.0.2", viewModel.Versions[1].ToString ());
		}

		/// <summary>
		/// NuGet v3 does not have a published date until the metadata is read separately.
		/// </summary>
		[Test]
		public async Task LoadPackageMetadata_ViewModelDidNotHavePublishedDate_PublishedDateReadFromLoadedMetadata ()
		{
			CreateViewModel ();
			packageItemListViewModel.Published = null;
			packageSearchMetadata.Published = new DateTimeOffset (2000, 1, 30, 10, 58, 30, new TimeSpan ());
			await LoadPackageMetadata ();

			Assert.IsTrue (viewModel.IsDependencyInformationAvailable);
			Assert.AreEqual (viewModel.LastPublished, packageSearchMetadata.Published);
		}

		[Test]
		public async Task LoadPackageMetadata_NoDependencies_PropertyChangedForDependencies ()
		{
			CreateViewModel ();
			bool dependenciesChanged = false;
			viewModel.PropertyChanged += (sender, e) => {
				if (e.PropertyName == "Dependencies")
					dependenciesChanged = true;
			};
			await LoadPackageMetadata ();

			Assert.IsTrue (viewModel.IsDependencyInformationAvailable);
			Assert.IsTrue (dependenciesChanged);
		}

		[Test]
		public async Task GetPackageDependenciesDisplayText_TwoDependenciesWithVersionRange_DependenciesReturnedInText ()
		{
			CreateViewModel ();
			var dependencies = new [] {
				new PackageDependency ("jQuery", new VersionRange (new NuGetVersion ("1.2.3"))),
				new PackageDependency ("bootstrap")
			};
			AddDependenciesToSearchMetadata (dependencies);
			await LoadPackageMetadata ();

			string result = viewModel.GetPackageDependenciesDisplayText ();

			Assert.That (result, Contains.Substring ("jQuery (>= 1.2.3)"));
			Assert.That (result, Contains.Substring ("bootstrap"));
		}

		[Test]
		public async Task ReadVersions_PackageSearchMetadataHasMultipleVersions_ViewModelHasVersionsSortedByNewestFirst ()
		{
			CreateViewModel ();
			AddVersionsToPackageItemListViewModel ("0.1", "0.2", "1.0");

			await ReadVersions ();

			Assert.AreEqual (3, viewModel.Versions.Count);
			Assert.AreEqual ("1.0", viewModel.Versions[0].ToString ());
			Assert.AreEqual ("0.2", viewModel.Versions[1].ToString ());
			Assert.AreEqual ("0.1", viewModel.Versions[2].ToString ());
		}

		[Test]
		public async Task ReadVersions_PackageSearchMetadataHasMultipleVersions_PropertyChangedEventFiredForVersions ()
		{
			CreateViewModel ();
			AddVersionsToPackageItemListViewModel ("0.1", "0.2");
			bool propertyChanged = false;
			viewModel.PropertyChanged += (sender, e) => {
				if (e.PropertyName == "Versions")
					propertyChanged = true;
			};
			await ReadVersions ();

			Assert.IsTrue (propertyChanged);
		}

		[Test]
		public async Task ReadVersions_ViewModelHadVersionsAlready_VersionsClearedBeforePopulating ()
		{
			CreateViewModel ();
			viewModel.Versions.Add (new NuGetVersion ("9.99"));
			AddVersionsToPackageItemListViewModel ("0.1");

			await ReadVersions ();

			Assert.AreEqual (1, viewModel.Versions.Count);
			Assert.AreEqual ("0.1", viewModel.Versions[0].ToString ());
		}

		[Test]
		public async Task ReadVersions_RecentPackage_VersionInfoRequestedAgainFromSourceRepository ()
		{
			CreateViewModel ();
			packageItemListViewModel.Id = "Test";
			packageItemListViewModel.Version = new NuGetVersion ("1.2");
			viewModel.IsRecentPackage = true;
			SelectPackageSourceInParentViewModel ();
			packageMetadataResource.AddPackageMetadata ("Test", "1.1");
			packageMetadataResource.AddPackageMetadata ("Test", "1.0");
			packageMetadataResource.AddPackageMetadata ("Test", "1.2");

			await ReadVersions ();

			Assert.AreEqual (3, viewModel.Versions.Count);
			Assert.AreEqual ("1.2", viewModel.Versions[0].ToString ());
			Assert.AreEqual ("1.1", viewModel.Versions[1].ToString ());
			Assert.AreEqual ("1.0", viewModel.Versions[2].ToString ());
		}

		[Test]
		public async Task LoadPackageMetadata_RecentPackage_PackageDependencyInformationIsLoaded ()
		{
			CreateViewModel ();
			packageItemListViewModel.Id = "Test";
			packageItemListViewModel.Version = new NuGetVersion ("1.2");
			viewModel.IsRecentPackage = true;
			SelectPackageSourceInParentViewModel ();
			packageMetadataResource.AddPackageMetadata ("Test", "1.1");
			packageMetadataResource.AddPackageMetadata ("Test", "1.2");
			var dependencies = new [] {
				new PackageDependency ("jQuery")
			};
			AddDependenciesToSearchMetadata (dependencies);
			await ReadVersions ();
			await WaitForLoadPackageMetadataTask ();

			string result = viewModel.GetPackageDependenciesDisplayText ();

			Assert.That (result, Contains.Substring ("jQuery"));
		}

		[Test]
		public async Task ReadVersions_RecentPackageAndParentIsNotSearchingForPrereleases_NonPrereleaseVersionInfoRequestedAgainFromSourceRepository ()
		{
			CreateViewModel ();
			packageItemListViewModel.Id = "Test";
			packageItemListViewModel.Version = new NuGetVersion ("1.2-beta1");
			viewModel.IsRecentPackage = true;
			parent.IncludePrerelease = false;
			SelectPackageSourceInParentViewModel ();
			packageMetadataResource.AddPackageMetadata ("Test", "1.1");
			packageMetadataResource.AddPackageMetadata ("Test", "1.0-beta2");
			packageMetadataResource.AddPackageMetadata ("Test", "1.2-beta1");

			await ReadVersions ();

			Assert.AreEqual (1, viewModel.Versions.Count);
			Assert.AreEqual ("1.1", viewModel.Versions[0].ToString ());
		}

		[Test]
		public async Task ReadVersions_RecentPackageAndParentIsSearchingForPrereleases_PrereleaseVersionInfoRequestedAgainFromSourceRepository ()
		{
			CreateViewModel ();
			packageItemListViewModel.Id = "Test";
			packageItemListViewModel.Version = new NuGetVersion ("1.2");
			viewModel.IsRecentPackage = true;
			parent.IncludePrerelease = true;
			SelectPackageSourceInParentViewModel ();
			packageMetadataResource.AddPackageMetadata ("Test", "1.0-beta2");
			packageMetadataResource.AddPackageMetadata ("Test", "1.2-beta1");

			await ReadVersions ();

			Assert.AreEqual (2, viewModel.Versions.Count);
			Assert.AreEqual ("1.2-beta1", viewModel.Versions[0].ToString ());
			Assert.AreEqual ("1.0-beta2", viewModel.Versions[1].ToString ());
		}
	}
}