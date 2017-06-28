//
// SolutionClosingDialog.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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
using Xwt;

namespace MonoDevelop.PackageManagement.Gui
{
	partial class SolutionClosingDialog
	{
		const int timeout = 250; // ms
		IDisposable timer;

		public SolutionClosingDialog ()
		{
			Build ();

			yesButton.Clicked += StopButtonClicked;
		}

		public bool KeepSolutionOpen { get; private set; } = true;

		void StopButtonClicked (object sender, EventArgs e)
		{
			yesButton.Sensitive = false;

			CloseIfNotRunningNuGetActions ();

			PackageManagementServices.BackgroundPackageActionRunner.Cancel ();

			CloseIfNotRunningNuGetActions ();

			spinner.Visible = true;
			spinner.Animate = true;

			timer = CreateTimer ();
		}

		void CloseIfNotRunningNuGetActions ()
		{
			if (!PackageManagementServices.BackgroundPackageActionRunner.IsRunning) {
				KeepSolutionOpen = false;
				Close ();
			}
		}

		IDisposable CreateTimer ()
		{
			return Application.TimeoutInvoke (timeout, CheckNuGetActionsStillRunning);
		}

		bool CheckNuGetActionsStillRunning ()
		{
			CloseIfNotRunningNuGetActions ();
			return true;
		}

		protected override void Dispose (bool disposing)
		{
			base.Dispose (disposing);

			if (timer != null) {
				timer.Dispose ();
				timer = null;
			}
		}
	}
}
