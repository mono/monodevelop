//
// PackageManagementOptionsViewModelTests.cs
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
using ICSharpCode.PackageManagement;
using NUnit.Framework;
using MonoDevelop.Core;
using MonoDevelop.PackageManagement.Tests.Helpers;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class PackageManagementOptionsViewModelTests
	{
		PackageManagementOptionsViewModel viewModel;
		FakeRecentPackageRepository fakeRecentRepository;
		FakeMachinePackageCache fakeMachineCache;
		FakeProcess fakeProcess;
		List<string> propertiesChanged;
		PackageManagementOptions options;
		FakeSettings fakeSettings;

		void CreateRecentRepository ()
		{
			fakeRecentRepository = new FakeRecentPackageRepository ();
		}

		void CreateMachineCache ()
		{
			fakeMachineCache = new FakeMachinePackageCache ();
		}

		void CreateOptions ()
		{
			var properties = new Properties ();
			var projectService = new FakePackageManagementProjectService ();
			fakeSettings = new FakeSettings ();
			SettingsProvider settingsProvider = TestablePackageManagementOptions.CreateSettingsProvider (fakeSettings, projectService);
			options = new PackageManagementOptions (properties, settingsProvider);
		}

		void EnablePackageRestoreInOptions ()
		{
			fakeSettings.SetPackageRestoreSetting (true);
		}

		void DisablePackageRestoreInOptions ()
		{
			fakeSettings.SetPackageRestoreSetting (false);
		}

		void CreateViewModelUsingCreatedMachineCache ()
		{
			CreateRecentRepository ();
			CreateOptions ();
			fakeProcess = new FakeProcess ();
			CreateViewModel (options);
		}

		void CreateViewModelUsingCreatedRecentRepository ()
		{
			CreateMachineCache ();
			CreateOptions ();
			fakeProcess = new FakeProcess ();
			CreateViewModel (options);
		}

		void CreateViewModel (PackageManagementOptions options)
		{
			viewModel = new PackageManagementOptionsViewModel (options, fakeRecentRepository, fakeMachineCache, fakeProcess);			
		}

		void AddPackageToRecentRepository ()
		{
			fakeRecentRepository.FakePackages.Add (new FakePackage ());
			fakeRecentRepository.HasRecentPackages = true;
		}

		void AddPackageToMachineCache ()
		{
			fakeMachineCache.FakePackages.Add (new FakePackage ());
		}

		void RecordPropertyChanges ()
		{
			propertiesChanged = new List<string> ();
			viewModel.PropertyChanged += (sender, e) => propertiesChanged.Add (e.PropertyName);
		}

		[Test]
		public void HasNoRecentPackages_RecentPackageRepositoryHasNoPackages_ReturnsTrue ()
		{
			CreateRecentRepository ();
			CreateViewModelUsingCreatedRecentRepository ();
			fakeRecentRepository.HasRecentPackages = false;

			bool hasPackages = viewModel.HasNoRecentPackages;

			Assert.IsTrue (hasPackages);
		}

		[Test]
		public void HasNoRecentPackages_RecentPackageRepositoryHasOnePackage_ReturnsFalse ()
		{
			CreateRecentRepository ();
			fakeRecentRepository.HasRecentPackages = true;
			AddPackageToRecentRepository ();
			CreateViewModelUsingCreatedRecentRepository ();

			bool hasPackages = viewModel.HasNoRecentPackages;

			Assert.IsFalse (hasPackages);
		}

		[Test]
		public void HasNoCachedPackages_MachinePackageCacheHasNoPackages_ReturnsTrue ()
		{
			CreateMachineCache ();
			CreateViewModelUsingCreatedMachineCache ();

			bool hasPackages = viewModel.HasNoCachedPackages;

			Assert.IsTrue (hasPackages);
		}

		[Test]
		public void HasNoCachedPackages_MachinePackageCacheHasOnePackage_ReturnsFalse ()
		{
			CreateMachineCache ();
			AddPackageToMachineCache ();
			CreateViewModelUsingCreatedMachineCache ();

			bool hasPackages = viewModel.HasNoCachedPackages;

			Assert.IsFalse (hasPackages);
		}

		[Test]
		public void ClearRecentPackagesCommandCanExecute_OneRecentPackage_CanExecuteReturnsTrue ()
		{
			CreateRecentRepository ();
			AddPackageToRecentRepository ();
			CreateViewModelUsingCreatedRecentRepository ();

			bool canExecute = viewModel.ClearRecentPackagesCommand.CanExecute (null);

			Assert.IsTrue (canExecute);
		}

		[Test]
		public void ClearRecentPackagesCommandCanExecute_NoRecentPackages_CanExecuteReturnsFalse ()
		{
			CreateRecentRepository ();
			CreateViewModelUsingCreatedRecentRepository ();

			bool canExecute = viewModel.ClearRecentPackagesCommand.CanExecute (null);

			Assert.IsFalse (canExecute);
		}

		[Test]
		public void ClearCachedPackagesCommandCanExecute_OneCachedPackage_CanExecuteReturnsTrue ()
		{
			CreateMachineCache ();
			AddPackageToMachineCache ();
			CreateViewModelUsingCreatedMachineCache ();

			bool canExecute = viewModel.ClearCachedPackagesCommand.CanExecute (null);

			Assert.IsTrue (canExecute);
		}

		[Test]
		public void ClearCachedPackagesCommandCanExecute_NoCachedPackages_CanExecuteReturnsFalse ()
		{
			CreateMachineCache ();
			CreateViewModelUsingCreatedMachineCache ();

			bool canExecute = viewModel.ClearCachedPackagesCommand.CanExecute (null);

			Assert.IsFalse (canExecute);
		}

		[Test]
		public void ClearCachedPackagesCommandExecute_OneCachedPackage_ClearsPackagesFromCache ()
		{
			CreateMachineCache ();
			AddPackageToMachineCache ();
			CreateViewModelUsingCreatedMachineCache ();

			viewModel.ClearCachedPackagesCommand.Execute (null);

			Assert.IsTrue (fakeMachineCache.IsClearCalled);
		}

		[Test]
		public void ClearRecentPackagesCommandExecute_OneRecentPackage_ClearsPackages ()
		{
			CreateMachineCache ();
			AddPackageToRecentRepository ();
			CreateViewModelUsingCreatedRecentRepository ();

			viewModel.ClearRecentPackagesCommand.Execute (null);

			Assert.IsTrue (fakeRecentRepository.IsClearCalled);
		}

		[Test]
		public void ClearRecentPackages_OneRecentPackage_HasNoRecentPackagesIsTrue ()
		{
			CreateRecentRepository ();
			AddPackageToRecentRepository ();
			CreateViewModelUsingCreatedRecentRepository ();

			RecordPropertyChanges ();
			viewModel.ClearRecentPackages ();
			fakeRecentRepository.HasRecentPackages = false;

			bool hasPackages = viewModel.HasNoRecentPackages;

			Assert.IsTrue (hasPackages);
		}

		[Test]
		public void ClearCachedPackages_OneCachedPackage_HasNoCachedPackagesReturnsTrue ()
		{
			CreateMachineCache ();
			AddPackageToMachineCache ();
			CreateViewModelUsingCreatedMachineCache ();

			RecordPropertyChanges ();
			viewModel.ClearCachedPackages ();

			bool hasPackages = viewModel.HasNoCachedPackages;

			Assert.IsTrue (hasPackages);
		}

		[Test]
		public void ClearRecentPackages_OneRecentPackage_HasNoRecentPackagesPropertyChangedEventFired ()
		{
			CreateRecentRepository ();
			AddPackageToRecentRepository ();
			CreateViewModelUsingCreatedMachineCache ();

			RecordPropertyChanges ();
			viewModel.ClearRecentPackages ();

			bool fired = propertiesChanged.Contains ("HasNoRecentPackages");

			Assert.IsTrue (fired);
		}

		[Test]
		public void ClearCachedPackages_OneCachedPackage_HasNoCachedPackagesPropertyChangedEventFired ()
		{
			CreateMachineCache ();
			AddPackageToMachineCache ();
			CreateViewModelUsingCreatedMachineCache ();

			RecordPropertyChanges ();
			viewModel.ClearCachedPackages ();

			bool fired = propertiesChanged.Contains ("HasNoCachedPackages");

			Assert.IsTrue (fired);
		}

		[Test]
		public void BrowseCachedPackagesCommandCanExecute_OneCachedPackage_ReturnsTrue ()
		{
			CreateMachineCache ();
			AddPackageToMachineCache ();
			CreateViewModelUsingCreatedMachineCache ();

			bool canExecute = viewModel.BrowseCachedPackagesCommand.CanExecute (null);

			Assert.IsTrue (canExecute);
		}

		[Test]
		public void BrowseCachedPackagesCommandCanExecute_NoCachedPackages_ReturnsFalse ()
		{
			CreateMachineCache ();
			CreateViewModelUsingCreatedMachineCache ();

			bool canExecute = viewModel.BrowseCachedPackagesCommand.CanExecute (null);

			Assert.IsFalse (canExecute);
		}

		[Test]
		public void BrowseCachedPackagesCommandExecute_OneCachedPackage_StartsProcessToOpenMachineCacheFolder ()
		{
			CreateMachineCache ();
			CreateViewModelUsingCreatedMachineCache ();

			string expectedFileName = @"d:\projects\nugetpackages";
			fakeMachineCache.Source = expectedFileName;

			viewModel.BrowseCachedPackagesCommand.Execute (null);

			string fileName = fakeProcess.FileNamePassedToStart;

			Assert.AreEqual (expectedFileName, fileName);
		}
	}
}
