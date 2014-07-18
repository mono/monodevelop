//
// TestablePackagesViewModel.cs
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
using System.Linq;

using ICSharpCode.PackageManagement;
using NuGet;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	public class TestablePackagesViewModel : PackagesViewModel
	{
		public FakeRegisteredPackageRepositories FakeRegisteredPackageRepositories;
		public FakeTaskFactory FakeTaskFactory;
		public List<FakePackage> FakePackages = new List<FakePackage> ();
		public int GetAllPackagesCallCount;
		public int PageCountBeforePackagesFiltered;
		public PackageSearchCriteria SearchCriteriaPassedToFilterPackagesBySearchCriteria;

		public TestablePackagesViewModel ()
			: this (new FakeRegisteredPackageRepositories ())
		{
		}

		public TestablePackagesViewModel (FakeRegisteredPackageRepositories registeredPackageRepositories)
			: this (registeredPackageRepositories, new FakeTaskFactory ())
		{
		}

		public TestablePackagesViewModel (
			FakeRegisteredPackageRepositories registeredPackageRepositories,
			FakeTaskFactory taskFactory)
			: base (
				registeredPackageRepositories,
				new FakePackageViewModelFactory (),
				taskFactory)
		{
			FakeRegisteredPackageRepositories = registeredPackageRepositories;
			FakeTaskFactory = taskFactory;
		}

		public void AddOneFakePackage ()
		{
			AddFakePackage ("Test");
		}

		public FakePackage AddFakePackage (string packageId)
		{
			FakePackage package = CreateFakePackage (packageId);
			FakePackages.Add (package);
			return package;
		}

		public FakePackage AddFakePackage (string packageId, string packageVersion)
		{
			FakePackage package = CreateFakePackage (packageId);
			package.Version = new SemanticVersion (packageVersion);
			FakePackages.Add (package);
			return package;
		}

		FakePackage CreateFakePackage (string packageId)
		{
			return new FakePackage () {
				Id = packageId,
				Description = ""
			};
		}

		public void AddSixFakePackages ()
		{
			AddFakePackages (howMany: 6);
		}

		void AddFakePackages (int howMany)
		{
			for (int i = 1; i <= howMany; ++i) {
				AddFakePackage ("Test " + i);
			}
		}

		protected override IQueryable<IPackage> GetPackages (PackageSearchCriteria search)
		{
			GetAllPackagesCallCount++;
			SearchCriteriaPassedToFilterPackagesBySearchCriteria = search;
			return FakePackages.AsQueryable ().Find (search.SearchText);
		}

		protected override IEnumerable<IPackage> GetFilteredPackagesBeforePagingResults (IQueryable<IPackage> allPackages, PackageSearchCriteria search)
		{
			PageCountBeforePackagesFiltered = Pages.Count;
			return base.GetFilteredPackagesBeforePagingResults (allPackages, search);
		}

		public void AddTenFakePackages ()
		{
			AddFakePackages (howMany: 10);
		}

		public void AddThreeFakePackages ()
		{
			AddFakePackages (howMany: 3);
		}
	}
}
