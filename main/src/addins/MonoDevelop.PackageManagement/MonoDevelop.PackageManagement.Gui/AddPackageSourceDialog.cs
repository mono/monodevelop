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

namespace MonoDevelop.PackageManagement
{
	public partial class AddPackageSourceDialog
	{
		RegisteredPackageSourcesViewModel viewModel;

		public AddPackageSourceDialog (RegisteredPackageSourcesViewModel viewModel)
		{
			Build ();
			this.viewModel = viewModel;

			addPackageSourceButton.Clicked += AddPackageSourceButtonClicked;
			packageSourceNameTextEntry.Changed += PackageSourceNameTextBoxChanged;
			packageSourceNameTextEntry.Activated += TextEntryActivated;
			packageSourceUrlTextEntry.Changed += PackageSourceUrlTextBoxChanged;
			packageSourceUrlTextEntry.Activated += TextEntryActivated;
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

		void UpdateAddPackageSourceButton ()
		{
			addPackageSourceButton.Sensitive = viewModel.CanAddPackageSource;
		}

		void TextEntryActivated (object sender, EventArgs e)
		{
			AddPackageSourceAndCloseDialog ();
		}
	}
}

