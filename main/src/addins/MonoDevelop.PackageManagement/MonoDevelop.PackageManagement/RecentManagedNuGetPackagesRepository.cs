//
// RecentManagedNuGetPackagesRepository.cs
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

namespace MonoDevelop.PackageManagement
{
	internal class RecentManagedNuGetPackagesRepository
	{
		public const int DefaultMaximumPackagesCount = 20;

		int maximumPackagesCount = DefaultMaximumPackagesCount;

		List<RecentPackage> packages = new List<RecentPackage> ();

		public int MaximumPackagesCount {
			get { return maximumPackagesCount; }
			set { maximumPackagesCount = value; }
		}

		public IEnumerable<ManagePackagesSearchResultViewModel> GetPackages (string source)
		{
			return packages
				.Where (package => String.Equals (package.Source, source, StringComparison.OrdinalIgnoreCase))
				.Select (package => package.PackageViewModel);
		}

		public void AddPackage (ManagePackagesSearchResultViewModel viewModel, string source)
		{
			var package = new RecentPackage (viewModel, source);
			viewModel.IsRecentPackage = true;
			RemovePackageIfAlreadyAdded (package);
			AddPackageAtBeginning (package);
			RemoveLastPackageIfCurrentPackageCountExceedsMaximum ();
		}

		void RemovePackageIfAlreadyAdded (RecentPackage package)
		{
			int index = FindPackage (package);
			if (index >= 0) {
				packages.RemoveAt (index);
			}
		}

		int FindPackage (RecentPackage package)
		{
			return packages.FindIndex (p => IsMatch (p, package));
		}

		bool IsMatch (RecentPackage x, RecentPackage y)
		{
			return ManagedPackagesSearchResultViewModelComparer.Instance.Equals (x.PackageViewModel, y.PackageViewModel);
		}

		void AddPackageAtBeginning (RecentPackage package)
		{
			package.PackageViewModel.Parent = null;
			packages.Insert (0, package);
		}

		void RemoveLastPackageIfCurrentPackageCountExceedsMaximum()
		{
			if (packages.Count > maximumPackagesCount) {
				RemoveLastPackage ();
			}
		}

		void RemoveLastPackage ()
		{
			packages.RemoveAt (packages.Count - 1);
		}

		class RecentPackage
		{
			public RecentPackage (ManagePackagesSearchResultViewModel viewModel, string source)
			{
				PackageViewModel = viewModel;
				Source = source;
			}

			public ManagePackagesSearchResultViewModel PackageViewModel { get; set; }
			public string Source { get; set; }
		}
	}
}

