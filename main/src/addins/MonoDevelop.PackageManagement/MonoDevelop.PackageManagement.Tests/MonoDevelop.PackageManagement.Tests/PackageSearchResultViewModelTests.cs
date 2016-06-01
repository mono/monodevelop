//
// PackageSearchResultViewModelTests.cs
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
using MonoDevelop.PackageManagement.Tests.Helpers;
using NuGet.PackageManagement.UI;
using NuGet.Versioning;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class PackageSearchResultViewModelTests
	{
		PackageSearchResultViewModel viewModel;
		PackageItemListViewModel packageItemListViewModel;
		TestableAllPackagesViewModel parent;

		void CreateViewModel ()
		{
			CreatePackageItemListViewModel ();
			CreateViewModel (packageItemListViewModel);
		}

		PackageItemListViewModel CreatePackageItemListViewModel ()
		{
			packageItemListViewModel = new PackageItemListViewModel ();
			return packageItemListViewModel;
		}

		void CreateViewModel (PackageItemListViewModel package)
		{
			var solutionManager = new FakeSolutionManager ();
			var project = new FakeDotNetProject ();
			parent = new TestableAllPackagesViewModel (solutionManager, project);
			viewModel = new PackageSearchResultViewModel (parent, packageItemListViewModel);
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
		/*
		[Test]
		public void HasDependencies_PackageHasNoDependencies_ReturnsFalse ()
		{
			CreateViewModel ();
			packageItemListViewModel.HasDependencies = false;

			Assert.IsFalse (viewModel.HasDependencies);
		}

		[Test]
		public void HasDependencies_PackageHasDependency_ReturnsTrue ()
		{
			CreateViewModel ();
			packageItemListViewModel.AddDependency ("Dependency");

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
			packageItemListViewModel.AddDependency ("Dependency");

			Assert.IsFalse (viewModel.HasNoDependencies);
		}*/

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
	}
}

