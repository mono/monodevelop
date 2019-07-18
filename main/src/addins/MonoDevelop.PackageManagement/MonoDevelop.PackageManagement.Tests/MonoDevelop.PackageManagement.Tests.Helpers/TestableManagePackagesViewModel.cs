//
// TestableManagePackagesViewModel.cs
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
using System.Threading;
using System.Threading.Tasks;
using NuGet.PackageManagement.UI;
using NuGet.Protocol.Core.Types;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	class TestableManagePackagesViewModel : ManagePackagesViewModel
	{
		public RecentManagedNuGetPackagesRepository RecentPackagesRepository;
		public FakeNuGetProjectContext FakeNuGetProjectContext;

		public TestableManagePackagesViewModel (
			IMonoDevelopSolutionManager solutionManager,
			IDotNetProject dotNetProject)
			: this (
				solutionManager,
				dotNetProject,
				new FakeNuGetProjectContext (),
				new RecentManagedNuGetPackagesRepository ())
		{
		}

		public TestableManagePackagesViewModel (
			IMonoDevelopSolutionManager solutionManager,
			IDotNetProject dotNetProject,
			FakeNuGetProjectContext projectContext,
			RecentManagedNuGetPackagesRepository recentPackagesRepository)
			: base (solutionManager, dotNetProject.ParentSolution, projectContext, recentPackagesRepository, dotNetProject)
		{
			FakeNuGetProjectContext = projectContext;
			RecentPackagesRepository = recentPackagesRepository;
		}

		public FakePackageFeed PackageFeed = new FakePackageFeed ();

		protected override IPackageFeed CreatePackageFeed (ManagePackagesLoadContext context)
		{
			return PackageFeed;
		}

		protected override Task CreateReadPackagesTask ()
		{
			ReadPackagesTask = base.CreateReadPackagesTask ();
			return ReadPackagesTask;
		}

		public Task ReadPackagesTask;

		protected override Task LoadPackagesAsync (PackageItemLoader loader, CancellationToken token)
		{
			if (LoadPackagesAsyncTask != null) {
				return LoadPackagesAsyncTask (loader, token);
			}
			return base.LoadPackagesAsync (loader, token);
		}

		public Func<PackageItemLoader, CancellationToken, Task> LoadPackagesAsyncTask;

		public Task CallBaseLoadPackagesAsyncTask (PackageItemLoader loader, CancellationToken token)
		{
			return base.LoadPackagesAsync (loader, token);
		}

		public Task GetPackagesInstalledInProjectTask;

		protected override Task GetPackagesInstalledInProjects ()
		{
			GetPackagesInstalledInProjectTask = base.GetPackagesInstalledInProjects ();
			return GetPackagesInstalledInProjectTask;
		}
	}
}

