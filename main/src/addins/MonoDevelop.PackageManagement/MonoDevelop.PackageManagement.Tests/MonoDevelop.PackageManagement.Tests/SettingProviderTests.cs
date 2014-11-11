//
// SettingProviderTests.cs
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
using ICSharpCode.PackageManagement;
using NuGet;
using NUnit.Framework;
using MonoDevelop.PackageManagement.Tests.Helpers;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class SettingsProviderTests
	{
		SettingsProvider settingsProvider;
		FakeSettings fakeSettings;
		FakePackageManagementProjectService projectService;
		IFileSystem fileSystemUsedToLoadSettings;
		string configFileUsedToLoadSettings;
		IMachineWideSettings machinesettingsUsedToLoadSettings;

		[SetUp]
		public void SetUp ()
		{
			fakeSettings = new FakeSettings ();
			projectService = new FakePackageManagementProjectService ();
			SettingsProvider.LoadDefaultSettings = LoadDefaultSettings;
			settingsProvider = new SettingsProvider (projectService);
		}

		ISettings LoadDefaultSettings (IFileSystem fileSystem, string configFile, IMachineWideSettings machineSettings)
		{
			fileSystemUsedToLoadSettings = fileSystem;
			configFileUsedToLoadSettings = configFile;
			machinesettingsUsedToLoadSettings = machineSettings;

			return fakeSettings;
		}

		void OpenSolution (string fileName)
		{
			var solution = new FakeSolution (fileName);
			projectService.OpenSolution = solution;
		}

		[TearDown]
		public void TearDown ()
		{
			// This resets SettingsProvider.LoadDefaultSettings.
			TestablePackageManagementOptions.CreateSettingsProvider (fakeSettings, projectService);
		}

		[Test]
		public void LoadSettings_NoSolutionOpen_NullFileSystemAndNullConfigFileAndNullMachineSettingsUsed ()
		{
			fileSystemUsedToLoadSettings = new FakeFileSystem ();
			configFileUsedToLoadSettings = "configFile";

			ISettings settings = settingsProvider.LoadSettings ();

			Assert.IsNull (fileSystemUsedToLoadSettings);
			Assert.IsNull (configFileUsedToLoadSettings);
			Assert.IsNull (machinesettingsUsedToLoadSettings);
			Assert.AreEqual (fakeSettings, settings);
		}

		[Test]
		public void LoadSettings_SolutionOpen_FileSystemWithRootSetToSolutionDotNuGetDirectoryUsedToLoadSettings ()
		{
			string fileName = @"d:\projects\MyProject\MyProject.sln";
			OpenSolution (fileName);

			ISettings settings = settingsProvider.LoadSettings ();

			Assert.AreEqual (@"d:\projects\MyProject\.nuget".ToNativePath (), fileSystemUsedToLoadSettings.Root);
			Assert.AreEqual (fakeSettings, settings);
		}

		[Test]
		public void LoadSettings_NuGetSettingsThrowsUnauthorizedAccessException_ExceptionHandledAndSettingsNullObjectReturned ()
		{
			fileSystemUsedToLoadSettings = new FakeFileSystem ();
			configFileUsedToLoadSettings = "configFile";
			SettingsProvider.LoadDefaultSettings = (fileSystem, configFile, machineSettings) => {
				throw new UnauthorizedAccessException ();
			};

			ISettings settings = settingsProvider.LoadSettings ();

			Assert.IsInstanceOf<NullSettings> (settings);
		}
	}
}
