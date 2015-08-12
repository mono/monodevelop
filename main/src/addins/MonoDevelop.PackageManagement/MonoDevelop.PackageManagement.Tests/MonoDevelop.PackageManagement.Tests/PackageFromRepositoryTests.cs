//
// PackageFromRepositoryTests.cs
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
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using MonoDevelop.PackageManagement.Tests.Helpers;
using NuGet;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class PackageFromRepositoryTests
	{
		FakePackage fakePackage;
		TestablePackageFromRepository package;
		FakePackageRepository fakeRepository;

		void CreatePackage ()
		{
			package = new TestablePackageFromRepository ();
			fakePackage = package.FakePackagePassedToConstructor;
			fakeRepository = package.FakePackageRepositoryPassedToConstructor;
		}

		[Test]
		public void Repository_PackageCreatedWithSourceRepository_ReturnsSourceRepository ()
		{
			CreatePackage ();
			IPackageRepository repository = package.Repository;

			Assert.AreEqual (fakeRepository, repository);
		}

		[Test]
		public void AssemblyReferences_WrappedPackageHasOneAssemblyReference_ReturnsOneAssemblyReference ()
		{
			CreatePackage ();
			fakePackage.AssemblyReferenceList.Add (new FakePackageAssemblyReference ());

			IEnumerable<IPackageAssemblyReference> assemblyReferences = package.AssemblyReferences;
			List<IPackageAssemblyReference> expectedAssemblyReferences = fakePackage.AssemblyReferenceList;

			CollectionAssert.AreEqual (expectedAssemblyReferences, assemblyReferences);
		}

		[Test]
		public void Id_WrappedPackageIdIsTest_ReturnsTest ()
		{
			CreatePackage ();
			fakePackage.Id = "Test";
			string id = package.Id;

			Assert.AreEqual ("Test", id);
		}

		[Test]
		public void Version_WrappedPackageVersionIsOnePointOne_ReturnsOnePointOne ()
		{
			CreatePackage ();
			var expectedVersion = new SemanticVersion ("1.1");
			fakePackage.Version = expectedVersion;
			SemanticVersion version = package.Version;

			Assert.AreEqual (expectedVersion, version);
		}

		[Test]
		public void Title_WrappedPackageTitleIsTest_ReturnsTest ()
		{
			CreatePackage ();
			fakePackage.Title = "Test";
			string title = package.Title;

			Assert.AreEqual ("Test", title);
		}

		[Test]
		public void Authors_WrappedPackageHasOneAuthor_ReturnsOneAuthor ()
		{
			CreatePackage ();
			fakePackage.AuthorsList.Add ("Author1");

			IEnumerable<string> authors = package.Authors;
			List<string> expectedAuthors = fakePackage.AuthorsList;

			CollectionAssert.AreEqual (expectedAuthors, authors);
		}

		[Test]
		public void Owners_WrappedPackageHasOneOwner_ReturnsOneOwner ()
		{
			CreatePackage ();
			fakePackage.OwnersList.Add ("Owner1");

			IEnumerable<string> owners = package.Owners;
			List<string> expectedOwners = fakePackage.OwnersList;

			CollectionAssert.AreEqual (expectedOwners, owners);
		}

		[Test]
		public void IconUrl_WrappedPackageIconUrlIsHttpSharpDevelopNet_ReturnsHttpSharpDevelopNet ()
		{
			CreatePackage ();
			var expectedUrl = new Uri ("http://sharpdevelop.net");
			fakePackage.IconUrl = expectedUrl;
			Uri url = package.IconUrl;

			Assert.AreEqual (expectedUrl, url);
		}

		[Test]
		public void LicenseUrl_WrappedPackageLicenseUrlIsHttpSharpDevelopNet_ReturnsHttpSharpDevelopNet ()
		{
			CreatePackage ();
			var expectedUrl = new Uri ("http://sharpdevelop.net");
			fakePackage.LicenseUrl = expectedUrl;
			Uri url = package.LicenseUrl;

			Assert.AreEqual (expectedUrl, url);
		}

		[Test]
		public void ProjectUrl_WrappedPackageProjectUrlIsHttpSharpDevelopNet_ReturnsHttpSharpDevelopNet ()
		{
			CreatePackage ();
			var expectedUrl = new Uri ("http://sharpdevelop.net");
			fakePackage.ProjectUrl = expectedUrl;
			Uri url = package.ProjectUrl;

			Assert.AreEqual (expectedUrl, url);
		}

		[Test]
		public void ReportAbuseUrl_WrappedPackageReportAbuseUrlIsHttpSharpDevelopNet_ReturnsHttpSharpDevelopNet ()
		{
			CreatePackage ();
			var expectedUrl = new Uri ("http://sharpdevelop.net");
			fakePackage.ReportAbuseUrl = expectedUrl;
			Uri url = package.ReportAbuseUrl;

			Assert.AreEqual (expectedUrl, url);
		}

		[Test]
		public void RequiresLicenseAcceptance_WrappedPackageRequiresLicenseAcceptanceIsTrue_ReturnsTrue ()
		{
			CreatePackage ();
			fakePackage.RequireLicenseAcceptance = true;

			Assert.IsTrue (package.RequireLicenseAcceptance);
		}

		[Test]
		public void Description_WrappedPackageDescriptionIsTest_ReturnsTest ()
		{
			CreatePackage ();
			fakePackage.Description = "Test";
			string description = package.Description;

			Assert.AreEqual ("Test", description);
		}

		[Test]
		public void Summary_WrappedPackageSummaryIsTest_ReturnsTest ()
		{
			CreatePackage ();
			fakePackage.Summary = "Test";
			string summary = package.Summary;

			Assert.AreEqual ("Test", summary);
		}

		[Test]
		public void Language_WrappedPackageLanguageIsTest_ReturnsTest ()
		{
			CreatePackage ();
			fakePackage.Language = "Test";
			string language = package.Language;

			Assert.AreEqual ("Test", language);
		}

		[Test]
		public void Tags_WrappedPackageTagsIsTest_ReturnsTest ()
		{
			CreatePackage ();
			fakePackage.Tags = "Test";
			string tags = package.Tags;

			Assert.AreEqual ("Test", tags);
		}

		[Test]
		public void FrameworkAssemblies_WrappedPackageHasOneFrameworkAssembly_ReturnsOneFrameworkAssembly ()
		{
			CreatePackage ();
			fakePackage.FrameworkAssembliesList.Add (new FrameworkAssemblyReference ("System.Xml"));

			IEnumerable<FrameworkAssemblyReference> assemblies = package.FrameworkAssemblies;
			IEnumerable<FrameworkAssemblyReference> expectedAssemblies = fakePackage.FrameworkAssemblies;

			CollectionAssert.AreEqual (expectedAssemblies, assemblies);
		}

		[Test]
		public void Dependencies_WrappedPackageHasOneDependency_ReturnsOneDependency ()
		{
			CreatePackage ();
			fakePackage.AddDependency ("Test");

			IEnumerable<PackageDependency> dependencies = package.Dependencies;

			CollectionAssert.AreEqual (fakePackage.DependenciesList, dependencies);
		}

		[Test]
		public void GetFiles_WrappedPackageHasOneFile_ReturnsOneFile ()
		{
			CreatePackage ();
			fakePackage.FilesList.Add (new PhysicalPackageFile ());

			IEnumerable<IPackageFile> files = package.GetFiles ();
			IEnumerable<IPackageFile> expectedFiles = fakePackage.FilesList;

			CollectionAssert.AreEqual (expectedFiles, files);
		}

		[Test]
		public void DownloadCount_WrappedPackageDownloadCountIsTen_ReturnsTen ()
		{
			CreatePackage ();
			fakePackage.DownloadCount = 10;
			int count = package.DownloadCount;

			Assert.AreEqual (10, count);
		}

		[Test]
		public void GetStream_WrappedPackageHasStream_ReturnsWrappedPackageStream ()
		{
			CreatePackage ();
			var expectedStream = new MemoryStream ();
			fakePackage.Stream = expectedStream;

			Stream stream = package.GetStream ();

			Assert.AreEqual (expectedStream, stream);
		}

		[Test]
		public void HasDependencies_WrappedPackageHasNoDependencies_ReturnsFalse ()
		{
			CreatePackage ();
			fakePackage.DependenciesList.Clear ();
			bool result = package.HasDependencies;

			Assert.IsFalse (result);
		}

		[Test]
		public void HasDependencies_WrappedPackageHasOneDependency_ReturnsTrue ()
		{
			CreatePackage ();
			fakePackage.AddDependency ("Test");
			bool result = package.HasDependencies;

			Assert.IsTrue (result);
		}

		[Test]
		public void LastUpdated_PackageWrapsDataServicePackageThatHasLastUpdatedDateOffset_ReturnsDateFromDataServicePackage ()
		{
			CreatePackage ();
			var expectedDateTime = new DateTime (2011, 1, 2);
			package.DateTimeOffsetToReturnFromGetDataServicePackageLastUpdated = new DateTimeOffset (expectedDateTime);

			DateTime? lastUpdated = package.LastUpdated;

			Assert.AreEqual (expectedDateTime, lastUpdated.Value);
		}

		[Test]
		public void LastUpdated_PackageWrapsPackageThatDoesNotHaveLastUpdatedDateOffset_ReturnsNullDate ()
		{
			CreatePackage ();
			package.DateTimeOffsetToReturnFromGetDataServicePackageLastUpdated = null;

			DateTime? lastUpdated = package.LastUpdated;

			Assert.IsFalse (lastUpdated.HasValue);
		}

		[Test]
		public void IsLatestVersion_WrappedPackageIsNotLatestVersion_ReturnsFalse ()
		{
			CreatePackage ();
			fakePackage.IsLatestVersion = false;
			bool result = package.IsLatestVersion;

			Assert.IsFalse (result);
		}

		[Test]
		public void IsLatestVersion_WrappedPackageHasOneDependency_ReturnsTrue ()
		{
			CreatePackage ();
			fakePackage.IsLatestVersion = true;
			bool result = package.IsLatestVersion;

			Assert.IsTrue (result);
		}

		[Test]
		public void Published_PackageWrapsPackageThatHasPublishedDateOffset_ReturnsDateTimeOffsetFromWrappedPackage ()
		{
			CreatePackage ();
			var dateTime = new DateTime (2011, 1, 2);
			var expectedDateTimeOffset = new DateTimeOffset (dateTime);
			fakePackage.Published = expectedDateTimeOffset;

			DateTimeOffset? published = package.Published;

			Assert.AreEqual (expectedDateTimeOffset, published.Value);
		}

		[Test]
		public void Copyright_WrappedPackageCopyrightIsTest_ReturnsTest ()
		{
			CreatePackage ();
			fakePackage.Copyright = "Test";
			string copyright = package.Copyright;

			Assert.AreEqual ("Test", copyright);
		}

		[Test]
		public void ReleaseNotes_WrappedPackageReleaseNotesIsTest_ReturnsTest ()
		{
			CreatePackage ();
			fakePackage.ReleaseNotes = "Test";
			string releaseNotes = package.ReleaseNotes;

			Assert.AreEqual ("Test", releaseNotes);
		}

		[Test]
		public void IsAbsoluteLatestVersion_WrappedPackageIsAbsoluteLatestVersion_ReturnsTrue ()
		{
			CreatePackage ();
			fakePackage.IsAbsoluteLatestVersion = true;
			bool result = package.IsAbsoluteLatestVersion;

			Assert.IsTrue (result);
		}

		[Test]
		public void Listed_WrappedPackageIsListed_ReturnsTrue ()
		{
			CreatePackage ();
			fakePackage.Listed = true;
			bool result = package.Listed;

			Assert.IsTrue (result);
		}

		[Test]
		public void DependencySets_WrappedPackageIsListed_ReturnsTrue ()
		{
			CreatePackage ();
			fakePackage.AddDependency ("Test");
			List<PackageDependencySet> expectedDependencies = fakePackage.DependencySets.ToList ();

			List<PackageDependencySet> dependencies = package.DependencySets.ToList ();

			Assert.AreEqual (1, dependencies.Count);
			Assert.AreEqual (expectedDependencies [0].Dependencies, dependencies [0].Dependencies);
		}

		[Test]
		public void GetSupportedFrameworks_OneFramework_ReturnsOneFramework ()
		{
			CreatePackage ();
			FrameworkName expectedFramework = fakePackage.AddSupportedFramework (".NET Framework, Version=4.0");

			List<FrameworkName> supportedFrameworks = package.GetSupportedFrameworks ().ToList ();

			Assert.AreEqual (1, supportedFrameworks.Count);
			Assert.AreEqual (expectedFramework, supportedFrameworks [0]);
		}

		[Test]
		public void ToString_PackageHasIdAndVersion_ReturnsWrappedPackageToString ()
		{
			CreatePackage ();
			fakePackage.Id = "MyPackage";
			fakePackage.Version = new SemanticVersion ("1.1");

			string result = package.ToString ();

			Assert.AreEqual ("MyPackage 1.1", result);
		}

		[Test]
		public void MinClientVersion_PackageHasMinClientVersion_ReturnsWrappedPackageMinClientVersion ()
		{
			CreatePackage ();
			var expectedVersion = new Version ("1.1");
			fakePackage.MinClientVersion = expectedVersion;

			Version version = package.MinClientVersion;

			Assert.AreEqual (expectedVersion, version);
		}

		[Test]
		public void PackageAssemblyReferences_PackageHasOnePackageAssemblyReference_ReturnsWrappedPackagePackageAssemblyReferences ()
		{
			CreatePackage ();
			fakePackage.AddPackageReferences ("Test");
			List<PackageReferenceSet> expectedReferences = fakePackage.PackageAssemblyReferences.ToList ();

			List<PackageReferenceSet> result = package.PackageAssemblyReferences.ToList ();

			Assert.AreEqual (expectedReferences, result);
		}

		[Test]
		public void DevelopmentDependency_PackageHasDevelopmentDependencySetToTrue_ReturnsWrappedPackageDevelopmentDependency ()
		{
			CreatePackage ();
			fakePackage.DevelopmentDependency = true;

			bool dependency = package.DevelopmentDependency;

			Assert.IsTrue (dependency);
		}

		[Test]
		public void ExtractPath_WrappedPackage_WrappedPackageExtractContentsCalled ()
		{
			CreatePackage ();
			var expectedFileSystem = new FakeFileSystem ();
			string expectedPath = @"d:\projects\test\packages";

			package.ExtractContents (expectedFileSystem, expectedPath);

			Assert.AreEqual (expectedFileSystem, fakePackage.FileSystemPassedToExtractContents);
			Assert.AreEqual (expectedPath, fakePackage.ExtractPathPassedToExtractContents);
		}
	}
}

