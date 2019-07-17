//
// SelectProjectsViewModel.cs
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
using System.Linq;

namespace MonoDevelop.PackageManagement
{
	class SelectProjectsViewModel
	{
		List<SelectedProjectViewModel> projects;

		public SelectProjectsViewModel (
			IEnumerable<IDotNetProject> projects,
			int packagesCount,
			ManagePackagesPage page)
		{
			this.projects = projects
				.OrderBy (project => project.Name)
				.Select (project => new SelectedProjectViewModel (project))
				.ToList ();

			Init (packagesCount, page);
		}

		void Init (
			int packagesCount,
			ManagePackagesPage page)
		{
			if (projects.Count () == 1) {
				projects[0].IsSelected = true;
			}

			bool multiplePackages = packagesCount > 1;

			switch (page) {
				case ManagePackagesPage.Installed:
					IsRemovingMultiplePackages = multiplePackages;
					IsRemovingSinglePackage = !IsAddingMultiplePackages;
					break;
				case ManagePackagesPage.Browse:
					IsAddingMultiplePackages = multiplePackages;
					IsAddingSinglePackage = !IsAddingMultiplePackages;
					break;
				case ManagePackagesPage.Updates:
					IsUpdatingMultiplePackages = multiplePackages;
					IsUpdatingSinglePackage = !IsAddingMultiplePackages;
					SelectAllProjectsByDefault ();
					break;
				case ManagePackagesPage.Consolidate:
					IsConsolidatingMultiplePackages = multiplePackages;
					IsConsolidatingSinglePackage = !IsAddingMultiplePackages;
					SelectAllProjectsByDefault ();
					break;
			}
		}

		public IEnumerable<SelectedProjectViewModel> Projects {
			get { return projects; }
		}

		public bool IsAddingSinglePackage { get; private set; }
		public bool IsAddingMultiplePackages { get; private set; }
		public bool IsRemovingSinglePackage { get; private set; }
		public bool IsRemovingMultiplePackages { get; private set; }
		public bool IsUpdatingSinglePackage { get; private set; }
		public bool IsUpdatingMultiplePackages { get; private set; }
		public bool IsConsolidatingSinglePackage { get; private set; }
		public bool IsConsolidatingMultiplePackages { get; private set; }

		public IEnumerable<IDotNetProject> GetSelectedProjects ()
		{
			return projects
				.Where (viewModel => viewModel.IsSelected)
				.Select (viewModel => viewModel.Project);
		}

		void SelectAllProjectsByDefault ()
		{
			foreach (SelectedProjectViewModel project in projects) {
				project.IsSelected = true;
			}
		}
	}
}
