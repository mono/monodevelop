//
// PendingPackageActionsHandler.cs
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

using MonoDevelop.PackageManagement.Gui;
using MonoDevelop.Ide;
using MonoDevelop.Core;

namespace MonoDevelop.PackageManagement
{
	class PendingPackageActionsHandler
	{
		/// <summary>
		/// Returns true if the solution can be closed.
		/// </summary>
		public static bool OnSolutionClosing ()
		{
			if (!PackageManagementServices.BackgroundPackageActionRunner.IsRunning)
				return true;

			var pendingInfo = PackageManagementServices.BackgroundPackageActionRunner.GetPendingActionsInfo ();
			if (pendingInfo.IsInstallPending)
				return AskToStopCurrentPackageActions (true);
			else if (pendingInfo.IsUninstallPending)
				return AskToStopCurrentPackageActions (false);
			else if (pendingInfo.IsRestorePending)
				PackageManagementServices.BackgroundPackageActionRunner.Cancel ();

			return true;
		}

		static bool AskToStopCurrentPackageActions (bool installing)
		{
			using (var dialog = new SolutionClosingDialog (installing)) {
				dialog.ShowWithParent ();
				return !dialog.KeepSolutionOpen;
			}
		}
	}
}
