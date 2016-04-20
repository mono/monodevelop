// 
// LicenseAcceptanceDialog.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
// 
// Copyright (C) 2013 Matthew Ward
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.PackageManagement
{
	internal partial class LicenseAcceptanceDialog : Gtk.Dialog
	{
		LicenseAcceptanceViewModel viewModel;

		public LicenseAcceptanceDialog (LicenseAcceptanceViewModel viewModel)
		{
			this.Build ();
			this.viewModel = viewModel;
			this.subTitleHBoxForSinglePackage.Visible = viewModel.HasOnePackage;
			this.subTitleHBoxForMultiplePackages.Visible = viewModel.HasMultiplePackages;

			AddPackages ();
		}

		void AddPackages ()
		{
			foreach (PackageLicenseViewModel package in viewModel.Packages) {
				AddPackage (package);
			}
			this.packagesVBox.ShowAll ();
		}

		void AddPackage (PackageLicenseViewModel package)
		{
			var label = new Label () {
				Xalign = 0,
				Yalign = 0,
				Xpad = 5,
				Ypad = 5,
				Wrap = true,
				Markup = CreatePackageMarkup (package)
			};

			GtkWorkarounds.SetLinkHandler (label, DesktopService.ShowUrl);

			this.packagesVBox.PackStart (label, false, false, 0);
		}

		string CreatePackageMarkup (PackageLicenseViewModel package)
		{
			return String.Format (
				"<span weight='bold'>{0}</span>\t{1}\n<a href='{2}'>{3}</a>",
				package.Id,
				package.Author,
				package.LicenseUrl,
				GettextCatalog.GetString ("View License")
			);
		}
	}
}

