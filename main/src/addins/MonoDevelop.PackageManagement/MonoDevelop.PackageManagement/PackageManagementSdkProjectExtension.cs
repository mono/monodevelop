//
// PackageManagementSdkProjectExtension.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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

using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.PackageManagement.Commands;
using MonoDevelop.Projects;

namespace MonoDevelop.PackageManagement
{
	class PackageManagementSdkProjectExtension : SdkProjectExtension
	{
		bool itemReady;

		public PackageManagementSdkProjectExtension ()
		{
			SdkProjectReloadMonitor.Initialize ();
		}

		protected override void OnItemReady ()
		{
			base.OnItemReady ();
			itemReady = true;
		}

		protected override void OnModified (SolutionItemModifiedEventArgs args)
		{
			base.OnModified (args);

			if (Project.Loading || !itemReady)
				return;

			var fileNameChange = args.LastOrDefault (arg => arg.Hint == "FileName");
			if (fileNameChange != null) {
				SdkProjectFileRenamedHandler.OnProjectFileRenamed (Project);
			}
		}

		/// <summary>
		/// Handle a new project being created and added to a new solution. In this case
		/// the NuGet packages should be restored. Need to avoid running a restore when
		/// a solution is being opened so check that project's parent solution is open in
		/// the IDE.
		/// </summary>
		protected override void OnBoundToSolution ()
		{
			base.OnBoundToSolution ();

			if (Project.Loading)
				return;

			if (!IdeApp.IsInitialized)
				return;

			if (IdeApp.ProjectOperations.CurrentSelectedSolution != Project.ParentSolution)
				return;

			if (!Project.NuGetAssetsFileExists ())
				RestorePackagesInProjectHandler.Run (Project);
		}

		BuildResult CreateNuGetRestoreRequiredBuildResult ()
		{
			return CreateBuildError (GettextCatalog.GetString ("NuGet packages need to be restored before building. NuGet MSBuild targets are missing and are needed for building. The NuGet MSBuild targets are generated when the NuGet packages are restored."));
		}

		BuildResult CreateBuildError (string message)
		{
			var result = new BuildResult ();
			result.SourceTarget = Project;
			result.AddError (message);
			return result;
		}

		protected override Task<BuildResult> OnClean (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
		{
			BuildResult result = CheckCanRunCleanOrBuild ();
			if (result != null) {
				return Task.FromResult (result);
			}
			return base.OnClean (monitor, configuration, operationContext);
		}

		protected override Task<BuildResult> OnBuild (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
		{
			BuildResult result = CheckCanRunCleanOrBuild ();
			if (result != null) {
				return Task.FromResult (result);
			}
			return base.OnBuild (monitor, configuration, operationContext);
		}

		BuildResult CheckCanRunCleanOrBuild ()
		{
			if (!Project.NuGetAssetsFileExists ()) {
				return CreateNuGetRestoreRequiredBuildResult ();
			}
			return null;
		}

		/// <summary>
		/// Shared projects can trigger a reference change during re-evaluation so do not
		/// restore if the project is being re-evaluated. Otherwise this could cause the
		/// restore to be run repeatedly.
		/// </summary>
		protected override void OnReferenceAddedToProject (ProjectReferenceEventArgs e)
		{
			base.OnReferenceAddedToProject (e);

			if (!IsLoadingOrReevaluating ())
				RestoreNuGetPackages ();
		}

		/// <summary>
		/// Shared projects can trigger a reference change during re-evaluation so do not
		/// restore if the project is being re-evaluated. Otherwise this could cause the
		/// restore to be run repeatedly.
		/// </summary>
		protected override void OnReferenceRemovedFromProject (ProjectReferenceEventArgs e)
		{
			base.OnReferenceRemovedFromProject (e);

			if (!IsLoadingOrReevaluating ())
				RestoreNuGetPackages ();
		}

		bool IsLoadingOrReevaluating ()
		{
			return Project.Loading || Project.IsReevaluating;
		}

		void RestoreNuGetPackages ()
		{
			Runtime.AssertMainThread ();
			RestorePackagesInProjectHandler.Run (Project);
		}
	}
}
