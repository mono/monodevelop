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
				GetString ("Could not add {0}. Please see Package Console for details.", packageId),
				GetString ("{0} added with warnings. Please see Package Console for details.", packageId)
			);
		}

		public static ProgressMonitorStatusMessage CreateInstallingProjectTemplatePackagesMessage ()
		{
			return new ProgressMonitorStatusMessage (
				GetString ("Adding packages..."),
				GetString ("Packages successfully added."),
				GetString ("Could not add packages. Please see Package Console for details."),
				GetString ("Packages added with warnings. Please see Package Console for details.")
			);
		}

		public static ProgressMonitorStatusMessage CreateInstallingMultiplePackagesMessage (int count)
		{
			return new ProgressMonitorStatusMessage (
				GetString ("Adding {0} packages...", count),
				GetString ("{0} packages successfully added.", count),
				GetString ("Could not add packages. Please see Package Console for details."),
				GetString ("{0} packages added with warnings. Please see Package Console for details.", count)
			);
		}

		public static ProgressMonitorStatusMessage CreateUpdatingPackagesInSolutionMessage ()
		{
			return new ProgressMonitorStatusMessage (
				GetString ("Updating packages in solution..."),
				GetString ("Packages successfully updated."),
				GetString ("Could not update packages. Please see Package Console for details."),
				GetString ("Packages updated with warnings. Please see Package Console for details.")
			);
		}

		public static ProgressMonitorStatusMessage CreateUpdatingPackagesInProjectMessage (int count)
		{
			return new ProgressMonitorStatusMessage (
				GetString ("Updating {0} packages in project...", count),
				GetString ("{0} packages successfully updated.", count),
				GetString ("Could not update packages. Please see Package Console for details."),
				GetString ("{0} packages updated with warnings. Please see Package Console for details.", count)
			);
		}

		public static ProgressMonitorStatusMessage CreateUpdatingPackagesInProjectMessage ()
		{
			return new ProgressMonitorStatusMessage (
				GetString ("Updating packages in project..."),
				GetString ("Packages successfully updated."),
				GetString ("Could not update packages. Please see Package Console for details."),
				GetString ("Packages updated with warnings. Please see Package Console for details.")
			);
		}

		public static ProgressMonitorStatusMessage CreateUpdatingSinglePackageMessage (string packageId)
		{
			return new ProgressMonitorStatusMessage (
				GetString ("Updating {0}...", packageId),
				GetString ("{0} successfully updated.", packageId),
				GetString ("Could not update {0}. Please see Package Console for details.", packageId),
				GetString ("{0} updated with warnings. Please see Package Console for details.", packageId)
			);
		}

		public static ProgressMonitorStatusMessage CreateRemoveSinglePackageMessage (string packageId)
		{
			return new ProgressMonitorStatusMessage (
				GetString ("Removing {0}...", packageId),
				GetString ("{0} successfully removed.", packageId),
				GetString ("Could not remove {0}. Please see Package Console for details.", packageId),
				GetString ("{0} removed with warnings. Please see Package Console for details.", packageId)
			);
		}

		public static ProgressMonitorStatusMessage CreateRestoringPackagesInSolutionMessage ()
		{
			return new ProgressMonitorStatusMessage (
				GetString ("Restoring packages for solution..."),
				GetString ("Packages successfully restored."),
				GetString ("Could not restore packages. Please see Package Console for details."),
				GetString ("Packages restored with warnings. Please see Package Console for details.")
			);
		}

		public static ProgressMonitorStatusMessage CreateCheckingPackageCompatibilityMessage ()
		{
			return new ProgressMonitorStatusMessage (
				GetString ("Checking package compatibility with new target framework..."),
				GetString ("Packages are compatible."),
				GetString ("Could not check package compatibility. Please see Package Console for details."),
				GetString ("Package reinstallation required. Please see Package Console for details.")
			);
		}

		public static ProgressMonitorStatusMessage CreateReinstallingSinglePackageMessage (string packageId)
		{
			return new ProgressMonitorStatusMessage (
				GetString ("Reinstalling {0}...", packageId),
				GetString ("{0} successfully reinstalled.", packageId),
				GetString ("Could not reinstall {0}. Please see Package Console for details.", packageId),
				GetString ("{0} reinstalled with warnings. Please see Package Console for details.", packageId)
			);
		}

		public static ProgressMonitorStatusMessage CreateReinstallingPackagesInProjectMessage (int count)
		{
			return new ProgressMonitorStatusMessage (
				GetString ("Reinstalling {0} packages...", count),
				GetString ("{0} packages successfully reinstalled.", count),
				GetString ("Could not reinstall packages. Please see Package Console for details."),
				GetString ("{0} packages reinstalled with warnings. Please see Package Console for details.", count)
			);
		}

		public static ProgressMonitorStatusMessage CreateReinstallingPackagesInProjectMessage ()
		{
			return new ProgressMonitorStatusMessage (
				GetString ("Reinstalling packages..."),
				GetString ("Packages successfully reinstalled."),
				GetString ("Could not reinstall packages. Please see Package Console for details."),
				GetString ("Packages reinstalled with warnings. Please see Package Console for details.")
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

