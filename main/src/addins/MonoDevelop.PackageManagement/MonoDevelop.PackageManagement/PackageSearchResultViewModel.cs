//
// PackageSearchResultViewModel.cs
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
using NuGet.PackageManagement.UI;
using NuGet.Versioning;

namespace MonoDevelop.PackageManagement
{
	internal class PackageSearchResultViewModel : ViewModelBase<PackageSearchResultViewModel>
	{
		AllPackagesViewModel parent;
		PackageItemListViewModel viewModel;
		bool isChecked;

		public PackageSearchResultViewModel (
			AllPackagesViewModel parent,
			PackageItemListViewModel viewModel)
		{
			this.parent = parent;
			this.viewModel = viewModel;
		}

		public string Id {
			get { return viewModel.Id; }
		}

		public NuGetVersion Version {
			get { return viewModel.Version; }
		}

		// TODO: This should return package Title if it is available.
		public string Name {
			get { return Id; }
		}

		public bool IsChecked {
			get { return isChecked; }
			set {
				if (value != isChecked) {
					isChecked = value;
					parent.OnPackageCheckedChanged (this);
				}
			}
		}

		public bool HasLicenseUrl {
			get { return LicenseUrl != null; }
		}

		public Uri LicenseUrl {
			get { return null; }
		//	get { return viewModel.LicenseUrl; }
		}

		public bool HasProjectUrl {
			get { return ProjectUrl != null; }
		}

		public Uri ProjectUrl {
			get { return null; }
		//	get { return viewModel.ProjectUrl; }
		}

		public bool HasGalleryUrl {
			get { return GalleryUrl != null; }
		}

		public bool HasNoGalleryUrl {
			get { return !HasGalleryUrl; }
		}

		public Uri GalleryUrl {
			get { return null; }
			//get { return viewModel.GalleryUrl; }
		}

		public Uri IconUrl {
			get { return viewModel.IconUrl; }
		}

		public bool HasIconUrl {
			get { return IconUrl != null; }
		}

		public string Author {
			get { return viewModel.Author; }
		}

		public string Summary {
			get { return viewModel.Summary; }
		}

		public bool HasDownloadCount {
			get { return viewModel.DownloadCount.HasValue; }
		}

		public string GetNameMarkup ()
		{
			return GetBoldText (Name);
		}

		static string GetBoldText (string text)
		{
			return String.Format ("<b>{0}</b>", text);
		}

		public string GetDownloadCountOrVersionDisplayText ()
		{
			if (ShowVersionInsteadOfDownloadCount) {
				return Version.ToString ();
			}

			return GetDownloadCountDisplayText ();
		}

		public string GetDownloadCountDisplayText ()
		{
			if (HasDownloadCount) {
				return viewModel.DownloadCount.Value.ToString ("N0");
			}
			return String.Empty;
		}

		public bool ShowVersionInsteadOfDownloadCount { get; set; }
	}
}

