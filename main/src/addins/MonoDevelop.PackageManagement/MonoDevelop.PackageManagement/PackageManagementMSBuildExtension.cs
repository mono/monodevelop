//
// PackageManagementMSBuildExtension.cs
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
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.PackageManagement
{
	internal class PackageManagementMSBuildExtension : ProjectExtension
	{
		public static EnsureNuGetPackageBuildImportsTargetUpdater Updater;
		public static NuGetPackageNewImportsHandler NewImportsHandler;
		public static NuGetPackageForcedImportsRemover ForcedImportsRemover;
		public static Task PackageRestoreTask;

		protected override void OnWriteProject (ProgressMonitor monitor, MSBuildProject msproject)
		{
			UpdateProject (msproject);
			base.OnWriteProject (monitor, msproject);
		}

		public void UpdateProject (MSBuildProject msproject)
		{
			NuGetPackageForcedImportsRemover importsRemover = ForcedImportsRemover;
			if (importsRemover != null) {
				importsRemover.UpdateProject (msproject);
			}

			EnsureNuGetPackageBuildImportsTargetUpdater currentUpdater = Updater;
			if (currentUpdater != null) {
				currentUpdater.UpdateProject (msproject);
			}

			NuGetPackageNewImportsHandler importsHandler = NewImportsHandler;
			if (importsHandler != null) {
				importsHandler.UpdateProject (msproject);
			}
		}

		protected override Task<BuildResult> OnBuild (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
		{
			Task restoreTask = PackageRestoreTask;
			if (restoreTask != null) {
				return WaitForRestoreThenBuild (restoreTask, monitor, configuration, operationContext);
			}
			return base.OnBuild (monitor, configuration, operationContext);
		}

		async Task<BuildResult> WaitForRestoreThenBuild (Task restoreTask, ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
		{
			try {
				await restoreTask;
			} catch (Exception ex) {
				var result = new BuildResult ();
				result.AddError (GettextCatalog.GetString ("{0}. Please see the Package Console for more details.", ex.Message));
				return result;
			}
			return await base.OnBuild (monitor, configuration, operationContext);
		}
	}
}

