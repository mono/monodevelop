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

using System;
using System.Collections.Generic;
using ICSharpCode.PackageManagement;
using MonoDevelop.Core;

namespace MonoDevelop.PackageManagement
{
	public static class ProgressMonitorStatusMessageFactory
	{
		public static ProgressMonitorStatusMessage CreateInstallingSinglePackageMessage (string packageId)
		{
			return new ProgressMonitorStatusMessage (
				GetString ("Adding {0}...", packageId),
				GetString ("{0} successfully added.", packageId),
				GetString ("Could not add {0}.", packageId),
				GetString ("{0} added with warnings.", packageId)
			);
		}

		public static ProgressMonitorStatusMessage CreateInstallingProjectTemplatePackagesMessage ()
		{
			return new ProgressMonitorStatusMessage (
				GetString ("Adding packages..."),
				GetString ("Packages successfully added."),
				GetString ("Could not add packages."),
				GetString ("Packages added with warnings.")
			);
		}

		public static ProgressMonitorStatusMessage CreateInstallingMultiplePackagesMessage (int count)
		{
			return new ProgressMonitorStatusMessage (
				GetString ("Adding {0} packages...", count),
				GetString ("{0} packages successfully added.", count),
				GetString ("Could not add packages."),
				GetString ("{0} packages added with warnings.", count)
			);
		}

		public static ProgressMonitorStatusMessage CreateUpdatingPackagesInSolutionMessage ()
		{
			return new ProgressMonitorStatusMessage (
				GetString ("Updating packages in solution..."),
				GetString ("Packages successfully updated."),
				GetString ("Could not update packages."),
				GetString ("Packages updated with warnings.")
			);
		}

		public static ProgressMonitorStatusMessage CreateUpdatingPackagesInSolutionMessage (IEnumerable<IPackageManagementProject> projects)
		{
			ProgressMonitorStatusMessage message = CreateUpdatingPackagesInSolutionMessage ();
			return new UpdatePackagesProgressMonitorStatusMessage (
				projects,
				GetString ("Packages are up to date."),
				GetString ("No updates found but warnings were reported."),
				message);
		}

		public static ProgressMonitorStatusMessage CreateUpdatingPackagesInProjectMessage (int count)
		{
			return new ProgressMonitorStatusMessage (
				GetString ("Updating {0} packages in project...", count),
				GetString ("{0} packages successfully updated.", count),
				GetString ("Could not update packages."),
				GetString ("{0} packages updated with warnings.", count)
			);
		}

		public static ProgressMonitorStatusMessage CreateUpdatingPackagesInProjectMessage (int count, IPackageManagementProject project)
		{
			ProgressMonitorStatusMessage message = CreateUpdatingPackagesInProjectMessage (count);
			return new UpdatePackagesProgressMonitorStatusMessage (
				project,
				GetString ("Packages are up to date."),
				GetString ("No updates found but warnings were reported."),
				message);
		}

		public static ProgressMonitorStatusMessage CreateUpdatingPackagesInProjectMessage ()
		{
			return new ProgressMonitorStatusMessage (
				GetString ("Updating packages in project..."),
				GetString ("Packages successfully updated."),
				GetString ("Could not update packages."),
				GetString ("Packages updated with warnings.")
			);
		}

		public static ProgressMonitorStatusMessage CreateUpdatingSinglePackageMessage (string packageId)
		{
			return new ProgressMonitorStatusMessage (
				GetString ("Updating {0}...", packageId),
				GetString ("{0} successfully updated.", packageId),
				GetString ("Could not update {0}.", packageId),
				GetString ("{0} updated with warnings.", packageId)
			);
		}

		public static ProgressMonitorStatusMessage CreateUpdatingSinglePackageMessage (string packageId, IPackageManagementProject project)
		{
			ProgressMonitorStatusMessage message = CreateUpdatingSinglePackageMessage (packageId);
			return new UpdatePackagesProgressMonitorStatusMessage (
				project,
				GetString ("{0} is up to date.", packageId),
				GetString ("No update found but warnings were reported."),
				message);
		}

		public static ProgressMonitorStatusMessage CreateRemoveSinglePackageMessage (string packageId)
		{
			return new ProgressMonitorStatusMessage (
				GetString ("Removing {0}...", packageId),
				GetString ("{0} successfully removed.", packageId),
				GetString ("Could not remove {0}.", packageId),
				GetString ("{0} removed with warnings.", packageId)
			);
		}

		public static ProgressMonitorStatusMessage CreateRestoringPackagesInSolutionMessage ()
		{
			return new ProgressMonitorStatusMessage (
				GetString ("Restoring packages for solution..."),
				GetString ("Packages successfully restored."),
				GetString ("Could not restore packages."),
				GetString ("Packages restored with warnings.")
			);
		}

		public static ProgressMonitorStatusMessage CreateRestoringPackagesBeforeUpdateMessage ()
		{
			return new ProgressMonitorStatusMessage (
				GetString ("Restoring packages before update..."),
				GetString ("Packages successfully restored."),
				GetString ("Could not restore packages."),
				GetString ("Packages restored with warnings.")
			);
		}

		public static ProgressMonitorStatusMessage CreateRestoringPackagesInProjectMessage ()
		{
			return new ProgressMonitorStatusMessage (
				GetString ("Restoring packages for project..."),
				GetString ("Packages successfully restored."),
				GetString ("Could not restore packages."),
				GetString ("Packages restored with warnings.")
			);
		}

		public static ProgressMonitorStatusMessage CreateCheckingPackageCompatibilityMessage ()
		{
			return new ProgressMonitorStatusMessage (
				GetString ("Checking package compatibility with new target framework..."),
				GetString ("Packages are compatible."),
				GetString ("Could not check package compatibility."),
				GetString ("Package retargeting required.")
			);
		}

		public static ProgressMonitorStatusMessage CreateRetargetingSinglePackageMessage (string packageId)
		{
			return new ProgressMonitorStatusMessage (
				GetString ("Retargeting {0}...", packageId),
				GetString ("{0} successfully retargeted.", packageId),
				GetString ("Could not retarget {0}.", packageId),
				GetString ("{0} retargeted with warnings.", packageId)
			);
		}

		public static ProgressMonitorStatusMessage CreateRetargetingPackagesInProjectMessage (int count)
		{
			return new ProgressMonitorStatusMessage (
				GetString ("Retargeting {0} packages...", count),
				GetString ("{0} packages successfully retargeted.", count),
				GetString ("Could not retarget packages."),
				GetString ("{0} packages retargeted with warnings.", count)
			);
		}

		public static ProgressMonitorStatusMessage CreateRetargetingPackagesInProjectMessage ()
		{
			return new ProgressMonitorStatusMessage (
				GetString ("Retargeting packages..."),
				GetString ("Packages successfully retargeted."),
				GetString ("Could not retarget packages."),
				GetString ("Packages retarget with warnings.")
			);
		}

		static string GetString (string phrase)
		{
			return GettextCatalog.GetString (phrase);
		}

		static string GetString (string phrase, object arg0)
		{
			return GettextCatalog.GetString (phrase, arg0);
		}
	}
}

