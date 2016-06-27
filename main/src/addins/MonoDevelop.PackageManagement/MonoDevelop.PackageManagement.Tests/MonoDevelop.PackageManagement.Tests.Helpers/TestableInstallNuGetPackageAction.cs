//
// TestableInstallNuGetPackageAction.cs
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

using System.Collections.Generic;
using NuGet.Protocol.Core.Types;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	class TestableInstallNuGetPackageAction : InstallNuGetPackageAction
	{
		public FakeNuGetProjectContext ProjectContext;
		public FakeNuGetPackageManager PackageManager;
		public PackageManagementEvents PackageManagementEvents;
		public FakeLicenseAcceptanceService LicenseAcceptanceService = new FakeLicenseAcceptanceService ();
		public FakeFileRemover FileRemover = new FakeFileRemover ();

		public TestableInstallNuGetPackageAction (
			IEnumerable<SourceRepository> primarySources,
			FakeSolutionManager solutionManager,
			FakeDotNetProject project)
			: this (
				primarySources,
				solutionManager,
				project,
				new FakeNuGetProjectContext (),
				new FakeNuGetPackageManager (),
				new PackageManagementEvents ())
		{
		}
		public TestableInstallNuGetPackageAction (
			IEnumerable<SourceRepository> primarySources,
			FakeSolutionManager solutionManager,
			FakeDotNetProject dotNetProject,
			FakeNuGetProjectContext projectContext,
			FakeNuGetPackageManager packageManager,
			PackageManagementEvents packageManagementEvents)
			: base (
				primarySources,
				null,
				solutionManager,
				dotNetProject,
				projectContext,
				packageManager,
				packageManagementEvents)
		{
			ProjectContext = projectContext;
			PackageManager = packageManager;

			PackageManagementEvents = packageManagementEvents;
		}

		protected override ILicenseAcceptanceService GetLicenseAcceptanceService ()
		{
			return LicenseAcceptanceService;
		}

		protected override IFileRemover GetFileRemover ()
		{
			return FileRemover;
		}
	}
}

