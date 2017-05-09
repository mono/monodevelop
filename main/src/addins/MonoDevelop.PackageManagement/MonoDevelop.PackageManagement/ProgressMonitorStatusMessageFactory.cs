//
// ProgressMonitorStatusMessagesFactory.cs
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

using System.Collections.Generic;
using MonoDevelop.Core;

namespace MonoDevelop.PackageManagement
{
	internal static class ProgressMonitorStatusMessageFactory
	{
		public static ProgressMonitorStatusMessage CreateInstallingSinglePackageMessage (string packageId)
		{
			return new ProgressMonitorStatusMessage (
				GettextCatalog.GetString ("Adding {0}...", packageId),
				GettextCatalog.GetString ("{0} successfully added.", packageId),
				GettextCatalog.GetString ("Could not add {0}.", packageId),
				GettextCatalog.GetString ("{0} added with warnings.", packageId)
			);
		}

		public static ProgressMonitorStatusMessage CreateInstallingProjectTemplatePackagesMessage ()
		{
			return new ProgressMonitorStatusMessage (
				GettextCatalog.GetString ("Adding packages..."),
				GettextCatalog.GetString ("Packages successfully added."),
				GettextCatalog.GetString ("Could not add packages."),
				GettextCatalog.GetString ("Packages added with warnings.")
			);
		}

		public static ProgressMonitorStatusMessage CreateInstallingMultiplePackagesMessage (int count)
		{
			return new ProgressMonitorStatusMessage (
				GettextCatalog.GetString ("Adding {0} packages...", count),
				GettextCatalog.GetString ("{0} packages successfully added.", count),
				GettextCatalog.GetString ("Could not add packages."),
				GettextCatalog.GetString ("{0} packages added with warnings.", count)
			);
		}

		public static ProgressMonitorStatusMessage CreateRemovingMultiplePackagesMessage (int count)
		{
			return new ProgressMonitorStatusMessage (
				GettextCatalog.GetString ("Removing {0} packages...", count),
				GettextCatalog.GetString ("{0} packages successfully removed.", count),
				GettextCatalog.GetString ("Could not remove packages."),
				GettextCatalog.GetString ("{0} packages removed with warnings.", count)
			);
		}

		public static ProgressMonitorStatusMessage CreateUpdatingPackagesInSolutionMessage ()
		{
			return new ProgressMonitorStatusMessage (
				GettextCatalog.GetString ("Updating packages in solution..."),
				GettextCatalog.GetString ("Packages successfully updated."),
				GettextCatalog.GetString ("Could not update packages."),
				GettextCatalog.GetString ("Packages updated with warnings.")
			);
		}

		public static ProgressMonitorStatusMessage CreateUpdatingPackagesInSolutionMessage (IEnumerable<IDotNetProject> projects)
		{
			ProgressMonitorStatusMessage message = CreateUpdatingPackagesInSolutionMessage ();
			return new UpdatePackagesProgressMonitorStatusMessage (
				projects,
				GettextCatalog.GetString ("Packages are up to date."),
				GettextCatalog.GetString ("No updates found but warnings were reported."),
				message);
		}

		public static ProgressMonitorStatusMessage CreateUpdatingPackagesInProjectMessage ()
		{
			return new ProgressMonitorStatusMessage (
				GettextCatalog.GetString ("Updating packages in project..."),
				GettextCatalog.GetString ("Packages successfully updated."),
				GettextCatalog.GetString ("Could not update packages."),
				GettextCatalog.GetString ("Packages updated with warnings.")
			);
		}

		public static ProgressMonitorStatusMessage CreateUpdatingPackagesInProjectMessage (IDotNetProject project)
		{
			ProgressMonitorStatusMessage message = CreateUpdatingPackagesInProjectMessage ();
			return new UpdatePackagesProgressMonitorStatusMessage (
				project,
				GettextCatalog.GetString ("Packages are up to date."),
				GettextCatalog.GetString ("No updates found but warnings were reported."),
				message);
		}

		public static ProgressMonitorStatusMessage CreateUpdatingSinglePackageMessage (string packageId)
		{
			return new ProgressMonitorStatusMessage (
				GettextCatalog.GetString ("Updating {0}...", packageId),
				GettextCatalog.GetString ("{0} successfully updated.", packageId),
				GettextCatalog.GetString ("Could not update {0}.", packageId),
				GettextCatalog.GetString ("{0} updated with warnings.", packageId)
			);
		}

		public static ProgressMonitorStatusMessage CreateUpdatingSinglePackageMessage (string packageId, IDotNetProject project)
		{
			ProgressMonitorStatusMessage message = CreateUpdatingSinglePackageMessage (packageId);
			return new UpdatePackagesProgressMonitorStatusMessage (
				project,
				GettextCatalog.GetString ("{0} is up to date.", packageId),
				GettextCatalog.GetString ("No update found but warnings were reported."),
				message);
		}

		public static ProgressMonitorStatusMessage CreateRemoveSinglePackageMessage (string packageId)
		{
			return new ProgressMonitorStatusMessage (
				GettextCatalog.GetString ("Removing {0}...", packageId),
				GettextCatalog.GetString ("{0} successfully removed.", packageId),
				GettextCatalog.GetString ("Could not remove {0}.", packageId),
				GettextCatalog.GetString ("{0} removed with warnings.", packageId)
			);
		}

		public static ProgressMonitorStatusMessage CreateRemovingPackagesFromProjectMessage (int count)
		{
			return new ProgressMonitorStatusMessage (
				GettextCatalog.GetString ("Removing {0} packages...", count),
				GettextCatalog.GetString ("{0} packages successfully removed.", count),
				GettextCatalog.GetString ("Could not remove packages."),
				GettextCatalog.GetString ("{0} packages removed with warnings.", count)
			);
		}

		public static ProgressMonitorStatusMessage CreateRestoringPackagesInSolutionMessage ()
		{
			return new ProgressMonitorStatusMessage (
				GettextCatalog.GetString ("Restoring packages for solution..."),
				GettextCatalog.GetString ("Packages successfully restored."),
				GettextCatalog.GetString ("Could not restore packages."),
				GettextCatalog.GetString ("Packages restored with warnings.")
			);
		}

		public static ProgressMonitorStatusMessage CreateRestoringPackagesBeforeUpdateMessage ()
		{
			return new ProgressMonitorStatusMessage (
				GettextCatalog.GetString ("Restoring packages before update..."),
				GettextCatalog.GetString ("Packages successfully restored."),
				GettextCatalog.GetString ("Could not restore packages."),
				GettextCatalog.GetString ("Packages restored with warnings.")
			);
		}

		public static ProgressMonitorStatusMessage CreateRestoringPackagesInProjectMessage ()
		{
			return new ProgressMonitorStatusMessage (
				GettextCatalog.GetString ("Restoring packages for project..."),
				GettextCatalog.GetString ("Packages successfully restored."),
				GettextCatalog.GetString ("Could not restore packages."),
				GettextCatalog.GetString ("Packages restored with warnings.")
			);
		}

		public static ProgressMonitorStatusMessage CreateCheckingPackageCompatibilityMessage ()
		{
			return new ProgressMonitorStatusMessage (
				GettextCatalog.GetString ("Checking package compatibility with new target framework..."),
				GettextCatalog.GetString ("Packages are compatible."),
				GettextCatalog.GetString ("Could not check package compatibility."),
				GettextCatalog.GetString ("Package retargeting required.")
			);
		}

		public static ProgressMonitorStatusMessage CreateRetargetingSinglePackageMessage (string packageId)
		{
			return new ProgressMonitorStatusMessage (
				GettextCatalog.GetString ("Retargeting {0}...", packageId),
				GettextCatalog.GetString ("{0} successfully retargeted.", packageId),
				GettextCatalog.GetString ("Could not retarget {0}.", packageId),
				GettextCatalog.GetString ("{0} retargeted with warnings.", packageId)
			);
		}

		public static ProgressMonitorStatusMessage CreateRetargetingPackagesInProjectMessage (int count)
		{
			return new ProgressMonitorStatusMessage (
				GettextCatalog.GetString ("Retargeting {0} packages...", count),
				GettextCatalog.GetString ("{0} packages successfully retargeted.", count),
				GettextCatalog.GetString ("Could not retarget packages."),
				GettextCatalog.GetString ("{0} packages retargeted with warnings.", count)
			);
		}

		public static ProgressMonitorStatusMessage CreateRetargetingPackagesInProjectMessage ()
		{
			return new ProgressMonitorStatusMessage (
				GettextCatalog.GetString ("Retargeting packages..."),
				GettextCatalog.GetString ("Packages successfully retargeted."),
				GettextCatalog.GetString ("Could not retarget packages."),
				GettextCatalog.GetString ("Packages retarget with warnings.")
			);
		}
	}
}

