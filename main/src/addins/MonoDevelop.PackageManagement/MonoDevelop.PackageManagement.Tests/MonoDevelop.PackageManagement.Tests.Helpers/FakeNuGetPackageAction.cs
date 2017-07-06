//
// FakeNuGetPackageAction.cs
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
using System.Threading;
using NuGet.PackageManagement;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	class FakeNuGetPackageAction : INuGetPackageAction, INuGetProjectActionsProvider
	{
		public FakeNuGetPackageAction ()
		{
			PackageId = "Test";
		}

		public string PackageId { get; set; }

		public PackageActionType ActionType { get; set; }

		public void Execute ()
		{
		}

		public void Execute (CancellationToken cancellationToken)
		{
		}

		public List<NuGetProjectAction> ProjectActions = new List<NuGetProjectAction> ();

		public IEnumerable<NuGetProjectAction> GetNuGetProjectActions ()
		{
			return ProjectActions;
		}

		public bool HasPackageScriptsToRun ()
		{
			return false;
		}

		public void AddNuGetProjectInstallAction (string packageId, string packageVersion)
		{
			AddNuGetProjectAction (packageId, packageVersion, NuGetProjectActionType.Install);
		}

		public void AddNuGetProjectUninstallAction (string packageId, string packageVersion)
		{
			AddNuGetProjectAction (packageId, packageVersion, NuGetProjectActionType.Uninstall);
		}

		void AddNuGetProjectAction (string packageId, string packageVersion, NuGetProjectActionType actionType)
		{
			var projectAction = new FakeNuGetProjectAction (packageId, packageVersion, actionType);
			ProjectActions.Add (projectAction);
		}
	}
}

