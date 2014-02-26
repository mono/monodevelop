//
// AddPackageSourceDialog.cs
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
using MonoDevelop.Core;

namespace MonoDevelop.PackageManagement
{
	public partial class AddPackageSourceDialog
	{
		RegisteredPackageSourcesViewModel viewModel;

		public AddPackageSourceDialog (RegisteredPackageSourcesViewModel viewModel)
		{
			Build ();
			this.viewModel = viewModel;
			LoadViewModel ();

			addPackageSourceButton.Clicked += AddPackageSourceButtonClicked;
			savePackageSourceButton.Clicked += SavePackageSourceButtonClicked;

			packageSourceNameTextEntry.Changed += PackageSourceNameTextBoxChanged;
			packageSourceNameTextEntry.Activated += TextEntryActivated;
			packageSourceUrlTextEntry.Changed += PackageSourceUrlTextBoxChanged;
			packageSourceUrlTextEntry.Activated += TextEntryActivated;
			packageSourceUserNameTextEntry.Changed += PackageSourceUserNameTextBoxChanged;
			packageSourceUserNameTextEntry.Activated += TextEntryActivated;
			packageSourcePasswordTextEntry.Changed += PackageSourcePasswordTextBoxChanged;
			packageSourcePasswordTextEntry.Activated += TextEntryActivated;
		}

		void LoadViewModel ()
		{
			packageSourceNameTextEntry.Text = viewModel.NewPackageSourceName;
			packageSourceUrlTextEntry.Text = viewModel.NewPackageSourceUrl;
			packageSourceUserNameTextEntry.Text = viewModel.NewPackageSourceUserName;
			packageSourcePasswordTextEntry.Password = viewModel.NewPackageSourcePassword;

			addPackageSourceButton.Sensitive = viewModel.CanAddPackageSource;
			addPackageSourceButton.Visible = !viewModel.IsEditingSelectedPackageSource;
			savePackageSourceButton.Sensitive = viewModel.CanUpdatePackageSource;
			savePackageSourceButton.Visible = viewModel.IsEditingSelectedPackageSource;

			if (viewModel.IsEditingSelectedPackageSource) {
				Title = GettextCatalog.GetString ("Edit Package Source");
			}
		}

		void AddPackageSourceButtonClicked (object sender, EventArgs e)
		{
			AddPackageSourceAndCloseDialog ();
		}

		void AddPackageSourceAndCloseDialog ()
		{
			if (viewModel.CanAddPackageSource) {
				viewModel.AddPackageSource ();
				Close ();
			}
		}

		void PackageSourceNameTextBoxChanged (object sender, EventArgs e)
		{
			viewModel.NewPackageSourceName = packageSourceNameTextEntry.Text;
			UpdateAddPackageSourceButton ();
		}

		void PackageSourceUrlTextBoxChanged (object sender, EventArgs e)
		{
			viewModel.NewPackageSourceUrl = packageSourceUrlTextEntry.Text;
			UpdateAddPackageSourceButton ();
		}

		void PackageSourceUserNameTextBoxChanged (object sender, EventArgs e)
		{
			viewModel.NewPackageSourceUserName = packageSourceUserNameTextEntry.Text;
			UpdateAddPackageSourceButton ();
		}

		void PackageSourcePasswordTextBoxChanged (object sender, EventArgs e)
		{
			viewModel.NewPackageSourcePassword = packageSourcePasswordTextEntry.Password;
			UpdateAddPackageSourceButton ();
		}

		void UpdateAddPackageSourceButton ()
		{
			addPackageSourceButton.Sensitive = viewModel.CanAddPackageSource;
		}

		void TextEntryActivated (object sender, EventArgs e)
		{
			if (viewModel.IsEditingSelectedPackageSource) {
				UpdatePackageSourceAndCloseDialog ();
			} else {
				AddPackageSourceAndCloseDialog ();
			}
		}

		void SavePackageSourceButtonClicked (object sender, EventArgs e)
		{
			UpdatePackageSourceAndCloseDialog ();
		}

		void UpdatePackageSourceAndCloseDialog ()
		{
			if (viewModel.CanUpdatePackageSource) {
				viewModel.UpdatePackageSource ();
				Close ();
			}
		}
	}
}

