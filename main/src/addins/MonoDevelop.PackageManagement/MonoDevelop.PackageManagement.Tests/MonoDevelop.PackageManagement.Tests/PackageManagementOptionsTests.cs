//
// PackageManagementOptionsTests.cs
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
using System.Text;
using System.Xml;
using MonoDevelop.PackageManagement;
using MonoDevelop.Core;
using MonoDevelop.PackageManagement.Tests.Helpers;
using NuGet;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class PackageManagementOptionsTests
	{
		Properties properties;
		PackageManagementOptions options;
		FakeSettings fakeSettings;
		SettingsProvider settingsProvider;
		FakePackageManagementProjectService projectService;

		void CreateOptions ()
		{
			CreateProperties ();
			CreateSettings ();
			CreateOptions (properties, fakeSettings);
		}

		void CreateProperties ()
		{
			properties = new Properties ();
		}

		void CreateSettings ()
		{
			fakeSettings = new FakeSettings ();
		}

		void CreateOptions (Properties properties)
		{
			CreateSettings ();
			CreateOptions (properties, fakeSettings);
		}

		void CreateOptions (FakeSettings fakeSettings)
		{
			CreateProperties ();
			CreateSettingsProvider (fakeSettings);
			options = new PackageManagementOptions (properties, settingsProvider);
		}

		void CreateSettingsProvider (FakeSettings fakeSettings)
		{
			projectService = new FakePackageManagementProjectService ();
			settingsProvider = TestablePackageManagementOptions.CreateSettingsProvider (fakeSettings, projectService);
		}

		void ChangeSettingsReturnedBySettingsProvider ()
		{
			fakeSettings = new FakeSettings ();
			TestablePackageManagementOptions.ChangeSettingsReturnedBySettingsProvider (fakeSettings);
		}

		void CreateOptions (Properties properties, FakeSettings fakeSettings)
		{
			CreateSettingsProvider (fakeSettings);
			options = new PackageManagementOptions (properties, settingsProvider);
		}

		void SaveOptions ()
		{
			var builder = new StringBuilder ();
			var writer = new XmlTextWriter (new StringWriter (builder));
			properties.Write (writer);
		}

		void EnablePackageRestoreInSettings ()
		{
			fakeSettings.SetPackageRestoreSetting (true);
		}

		void OpenSolution ()
		{
			projectService.RaiseSolutionLoadedEvent ();
		}

		void CloseSolution ()
		{
			projectService.RaiseSolutionUnloadedEvent ();
		}

		[Test]
		public void PackageSources_OnePackageSourceInSettings_ContainsPackageSourceFromSettingsAndDefaultNuGetOrgFeed ()
		{
			CreateSettings ();
			var packageSource = new PackageSource ("http://codeplex.com", "Test");
			fakeSettings.AddFakePackageSource (packageSource);
			CreateOptions (fakeSettings);

			RegisteredPackageSources actualSources = options.PackageSources;

			List<PackageSource> expectedSources = new List<PackageSource> ();
			expectedSources.Add (packageSource);
			expectedSources.Add (RegisteredPackageSources.DefaultPackageSource);

			Assert.AreEqual (expectedSources, actualSources);
		}

		[Test]
		public void PackageSources_NoPackageSourceInSavedSettings_ContainsDefaultPackageSource ()
		{
			CreateSettings ();
			CreateOptions (fakeSettings);

			List<PackageSource> expectedSources = new List<PackageSource> ();
			expectedSources.Add (RegisteredPackageSources.DefaultPackageSource);

			RegisteredPackageSources actualPackageSources = options.PackageSources;

			CollectionAssert.AreEqual (expectedSources, actualPackageSources);
		}

		[Test]
		public void PackageSources_NoPackageSourceInSavedSettings_DefaultPackageSourceIsNotAddedToSettings ()
		{
			CreateSettings ();
			CreateOptions (fakeSettings);
			RegisteredPackageSources packageSources = options.PackageSources;

			PackageSource packageSource = RegisteredPackageSources.DefaultPackageSource;

			bool result = fakeSettings.SavedSectionValueLists.ContainsKey (FakeSettings.PackageSourcesSectionName);
			Assert.IsFalse (result);
			Assert.IsNotNull (packageSource);
		}

		[Test]
		public void PackageSources_OnePackageSourceAdded_PackageSourceSavedInSettings ()
		{
			CreateSettings ();
			CreateOptions (fakeSettings);
			RegisteredPackageSources registeredPackageSources = options.PackageSources;

			var packageSource = new PackageSource ("http://codeplex.com", "Test");
			registeredPackageSources.Clear ();
			registeredPackageSources.Add (packageSource);

			var expectedSavedPackageSourceSettings = new List<SettingValue> ();
			expectedSavedPackageSourceSettings.Add (new SettingValue ("Test", "http://codeplex.com", false));

			IList<SettingValue> actualSavedPackageSourceSettings = fakeSettings.GetValuesPassedToSetValuesForPackageSourcesSection ();

			Assert.AreEqual (expectedSavedPackageSourceSettings, actualSavedPackageSourceSettings);
		}

		[Test]
		public void PackageSources_OnePackageSourceAdded_PackageSourcesSectionUpdated ()
		{
			CreateSettings ();
			CreateOptions (fakeSettings);
			RegisteredPackageSources registeredPackageSources = options.PackageSources;	

			var packageSource = new PackageSource ("http://codeplex.com", "Test");
			registeredPackageSources.Clear ();
			registeredPackageSources.Add (packageSource);

			IList<SettingValue> settings = fakeSettings.SectionsUpdated [RegisteredPackageSourceSettings.PackageSourcesSectionName];

			Assert.AreEqual (1, settings.Count);
			Assert.AreEqual ("Test", settings[0].Key);
			Assert.AreEqual ("http://codeplex.com", settings[0].Value);
		}

		[Test]
		public void PackageSources_SettingsFilesDoesNotExistSoSettingsReturnsNullForPackageSourcesSection_DoesNotThrowException ()
		{
			CreateSettings ();
			fakeSettings.MakePackageSourceSectionsNull ();
			CreateOptions (fakeSettings);

			RegisteredPackageSources packageSources = null;
			Assert.DoesNotThrow (() => packageSources = options.PackageSources);
		}

		[Test]
		public void PackageSources_OneEnabledPackageSourceInSettings_ContainsSingleEnabledPackageSourceFromSettings ()
		{
			CreateSettings ();
			var packageSource = new PackageSource ("http://codeplex.com", "Test") { IsEnabled = true };
			fakeSettings.AddFakePackageSource (packageSource);
			CreateOptions (fakeSettings);

			RegisteredPackageSources actualSources = options.PackageSources;

			Assert.IsTrue (actualSources [0].IsEnabled);
		}

		[Test]
		public void PackageSources_OneDisabledPackageSourceInSettings_ContainsSingleDisabledPackageSourceFromSettings ()
		{
			CreateSettings ();
			var packageSource = new PackageSource ("http://codeplex.com", "Test") { IsEnabled = false };
			fakeSettings.AddFakePackageSource (packageSource);
			fakeSettings.AddDisabledPackageSource (packageSource);
			CreateOptions (fakeSettings);

			RegisteredPackageSources actualSources = options.PackageSources;

			Assert.IsFalse (actualSources [0].IsEnabled);
		}

		[Test]
		public void PackageSources_OnePackageSourceAdded_DisabledPackageSourcesSectionDeletedFromSettings ()
		{
			CreateSettings ();
			CreateOptions (fakeSettings);
			RegisteredPackageSources registeredPackageSources = options.PackageSources;	

			var packageSource = new PackageSource ("http://codeplex.com", "Test");
			registeredPackageSources.Clear ();
			registeredPackageSources.Add (packageSource);

			IList<SettingValue> settings = fakeSettings.SectionsUpdated[RegisteredPackageSourceSettings.DisabledPackageSourceSectionName];

			Assert.AreEqual (0, settings.Count);
		}

		[Test]
		public void PackageSources_OneDisabledPackageSourceAdded_DisabledPackageSourcesSectionSaved ()
		{
			CreateSettings ();
			CreateOptions (fakeSettings);
			RegisteredPackageSources registeredPackageSources = options.PackageSources;	

			var packageSource = new PackageSource ("http://codeplex.com", "Test") { IsEnabled = false };
			registeredPackageSources.Clear ();
			registeredPackageSources.Add (packageSource);

			var expectedSavedPackageSourceSettings = new List<SettingValue> ();
			expectedSavedPackageSourceSettings.Add (new SettingValue (packageSource.Name, "true", false));

			IList<SettingValue> actualSavedPackageSourceSettings = 
				fakeSettings.GetValuesPassedToSetValuesForDisabledPackageSourcesSection ();
			Assert.AreEqual (expectedSavedPackageSourceSettings, actualSavedPackageSourceSettings);
		}

		[Test]
		public void PackageSources_OneEnabledPackageSourceAdded_DisabledPackageSourcesSectionNotChanged ()
		{
			CreateSettings ();
			CreateOptions (fakeSettings);
			RegisteredPackageSources registeredPackageSources = options.PackageSources;	

			var packageSource = new PackageSource ("http://codeplex.com", "Test") { IsEnabled = true };
			registeredPackageSources.Clear ();
			registeredPackageSources.Add (packageSource);

			IList<SettingValue> actualSavedPackageSourceSettings = 
				fakeSettings.GetValuesPassedToSetValuesForDisabledPackageSourcesSection ();
			Assert.AreEqual (0, actualSavedPackageSourceSettings.Count);
		}

		[Test]
		public void PackageSources_SolutionOpenedAfterInitialPackageSourcesLoaded_ContainsPackageSourceFromSolutionSpecificSettings ()
		{
			CreateSettings ();
			var packageSource = new PackageSource ("https://www.nuget.org/api/v2/", "Official NuGet Gallery");
			fakeSettings.AddFakePackageSource (packageSource);
			CreateOptions (fakeSettings);
			RegisteredPackageSources initialSources = options.PackageSources;
			var expectedInitialSources = new List<PackageSource> ();
			expectedInitialSources.Add (packageSource);
			ChangeSettingsReturnedBySettingsProvider ();
			packageSource = new PackageSource ("https://www.nuget.org/api/v2/", "Official NuGet Gallery");
			fakeSettings.AddFakePackageSource (packageSource);
			var expectedSources = new List<PackageSource> ();
			expectedSources.Add (packageSource);
			packageSource = new PackageSource ("http://codeplex.com", "ProjectSource");
			fakeSettings.AddFakePackageSource (packageSource);
			expectedSources.Add (packageSource);
			OpenSolution ();

			RegisteredPackageSources actualSources = options.PackageSources;

			Assert.AreEqual (expectedInitialSources, initialSources);
			Assert.AreEqual (expectedSources, actualSources);
		}

		[Test]
		public void PackageSources_SolutionClosedAfterInitialPackageSourcesLoaded_PackageSourcesReloaded ()
		{
			CreateSettings ();
			var packageSource = new PackageSource ("https://www.nuget.org/api/v2/", "Official NuGet Gallery");
			fakeSettings.AddFakePackageSource (packageSource);
			var expectedInitialSources = new List<PackageSource> ();
			expectedInitialSources.Add (packageSource);
			packageSource = new PackageSource ("http://projectsource.org", "ProjectSource");
			fakeSettings.AddFakePackageSource (packageSource);
			expectedInitialSources.Add (packageSource);
			OpenSolution ();
			CreateOptions (fakeSettings);
			RegisteredPackageSources initialSources = options.PackageSources;
			ChangeSettingsReturnedBySettingsProvider ();
			packageSource = new PackageSource ("https://www.nuget.org/api/v2/", "Official NuGet Gallery");
			fakeSettings.AddFakePackageSource (packageSource);
			var expectedSources = new List<PackageSource> ();
			expectedSources.Add (packageSource);
			CloseSolution ();

			RegisteredPackageSources actualSources = options.PackageSources;

			Assert.AreEqual (expectedInitialSources, initialSources);
			Assert.AreEqual (expectedSources, actualSources);
		}
	}
}