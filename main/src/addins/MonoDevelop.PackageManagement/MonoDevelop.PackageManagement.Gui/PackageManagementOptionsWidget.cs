// 
// PackageManagementOptionsWidget.cs
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
using ICSharpCode.PackageManagement;

namespace MonoDevelop.PackageManagement
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class PackageManagementOptionsWidget : Gtk.Bin
	{
		PackageManagementOptionsViewModel viewModel;

		public PackageManagementOptionsWidget (PackageManagementOptionsViewModel viewModel)
		{
			this.Build ();
			this.viewModel = viewModel;
			this.InitializeWidget ();
		}

		void InitializeWidget ()
		{
			this.automaticPackageRestoreOnOpeningSolutionCheckBox.Active = viewModel.IsAutomaticPackageRestoreOnOpeningSolutionEnabled;
			this.automaticPackageRestoreOnOpeningSolutionCheckBox.Toggled += AutomaticPackageRestoreOnOpeningSolutionCheckBoxToggled;

			this.checkForPackageUpdatesOnOpeningSolutionCheckBox.Active = viewModel.IsCheckForPackageUpdatesOnOpeningSolutionEnabled;
			this.checkForPackageUpdatesOnOpeningSolutionCheckBox.Toggled += CheckForPackageUpdatesOnOpeningSolutionCheckBoxCheckBoxToggled;
		}

		protected void AutomaticPackageRestoreOnOpeningSolutionCheckBoxToggled (object sender, EventArgs e)
		{
			viewModel.IsAutomaticPackageRestoreOnOpeningSolutionEnabled = this.automaticPackageRestoreOnOpeningSolutionCheckBox.Active;
		}

		protected void CheckForPackageUpdatesOnOpeningSolutionCheckBoxCheckBoxToggled (object sender, EventArgs e)
		{
			viewModel.IsCheckForPackageUpdatesOnOpeningSolutionEnabled = this.checkForPackageUpdatesOnOpeningSolutionCheckBox.Active;
		}
	}
}

