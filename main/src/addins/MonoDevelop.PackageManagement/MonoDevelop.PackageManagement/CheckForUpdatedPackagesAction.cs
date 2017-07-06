//
// CheckForUpdatedPackagesAction.cs
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

using System.Threading;
using MonoDevelop.Projects;

namespace MonoDevelop.PackageManagement
{
	internal class CheckForUpdatedPackagesAction : IPackageAction
	{
		IUpdatedNuGetPackagesInWorkspace updatedPackagesInWorkspace;
		ISolution solution;

		public CheckForUpdatedPackagesAction (Solution solution)
			: this (
				new SolutionProxy (solution),
				PackageManagementServices.UpdatedPackagesInWorkspace)
		{
		}

		public CheckForUpdatedPackagesAction (
			ISolution solution,
			IUpdatedNuGetPackagesInWorkspace updatedPackagesInWorkspace)
		{
			this.solution = solution;
			this.updatedPackagesInWorkspace = updatedPackagesInWorkspace;
		}

		public bool HasPackageScriptsToRun ()
		{
			return false;
		}

		public PackageActionType ActionType {
			get { return PackageActionType.None; }
		}

		public void Execute ()
		{
			Execute (CancellationToken.None);
		}

		public void Execute (CancellationToken cancellationToken)
		{
			// Queue the check for updates with the background dispatcher so
			// the NuGet addin does not create another separate Package Console.
			PackageManagementBackgroundDispatcher.Dispatch (() => {
				updatedPackagesInWorkspace.CheckForUpdates (solution);
			});
		}
	}
}

