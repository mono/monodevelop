//
// AddPlatformImplementationDialog.cs
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
using MonoDevelop.Ide;
using Xwt;

namespace MonoDevelop.Packaging.Gui
{
	partial class AddPlatformImplementationDialog
	{
		AddPlatformImplementationViewModel viewModel;

		public AddPlatformImplementationDialog (AddPlatformImplementationViewModel viewModel)
		{
			this.viewModel = viewModel;

			Build ();

			androidCheckBox.Active = viewModel.CreateAndroidProject;
			androidCheckBox.Sensitive = viewModel.IsCreateAndroidProjectEnabled;

			iosCheckBox.Active = viewModel.CreateIOSProject;
			iosCheckBox.Sensitive = viewModel.IsCreateIOSProjectEnabled;

			useSharedProjectCheckBox.Active = viewModel.CreateSharedProject;
			useSharedProjectCheckBox.Sensitive = viewModel.IsCreateSharedProjectEnabled;

			UpdateOkButton ();

			androidCheckBox.Clicked += AndroidCheckBoxClicked;
			iosCheckBox.Clicked += IOSCheckBoxClicked;
			useSharedProjectCheckBox.Clicked += UseSharedProjectCheckBoxClicked;
			okButton.Clicked += OkButtonClicked;
		}

		public Command ShowWithParent ()
		{
			WindowFrame parent = Toolkit.CurrentEngine.WrapWindow (IdeApp.Workbench.RootWindow);
			return Run (parent);
		}

		void UpdateOkButton ()
		{
			okButton.Sensitive = viewModel.AnyItemsToCreate ();
		}

		void OkButtonClicked (object sender, EventArgs e)
		{
			Close ();
		}

		void AndroidCheckBoxClicked (object sender, EventArgs e)
		{
			viewModel.CreateAndroidProject = androidCheckBox.Active;
			UpdateOkButton ();
		}

		void IOSCheckBoxClicked (object sender, EventArgs e)
		{
			viewModel.CreateIOSProject = iosCheckBox.Active;
			UpdateOkButton ();
		}

		void UseSharedProjectCheckBoxClicked (object sender, EventArgs e)
		{
			viewModel.CreateSharedProject = useSharedProjectCheckBox.Active;
			UpdateOkButton ();
		}
	}
}
