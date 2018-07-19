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
using System.Linq;
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

		protected override Task<TargetEvaluationResult> OnRunTarget (ProgressMonitor monitor, string target, ConfigurationSelector configuration, TargetEvaluationContext context)
		{
			if (Project is DotNetProject dotNetProject && IsCoreCompileDependsOn (target, context)) {
				if (DotNetCoreNuGetProject.CanCreate (dotNetProject)) {
					return OnRunDotNetCoreProjectTarget (monitor, target, configuration, context);
				} else if (PackageReferenceNuGetProject.CanCreate (dotNetProject)) {
					return OnRunPackageReferenceProjectTarget (monitor, target, configuration, context);
				}
			}
			return base.OnRunTarget (monitor, target, configuration, context);
		}

		/// <summary>
		/// Ensures any NuGet package content files are included when CoreCompileDependsOn is evaluated.
		/// Visual Studio 2017 does not run the RunProductContentAssets target directly but runs a set of
		/// targets which indirectly run RunProductContentAssets. 
		/// </summary>
		Task<TargetEvaluationResult> OnRunDotNetCoreProjectTarget (ProgressMonitor monitor, string target, ConfigurationSelector configuration, TargetEvaluationContext context)
		{
			target += ";RunProduceContentAssets";
			return base.OnRunTarget (monitor, target, configuration, context);
		}

		/// <summary>
		/// Ensures any NuGet package content files are included when CoreCompileDependsOn is evaluated.
		/// Visual Studio 2017 does not run the ResolveNuGetPackageAssets target directly but seems to
		/// run the Compile target for the project which indirectly runs ResolveNuGetPackageAssets.
		/// </summary>
		Task<TargetEvaluationResult> OnRunPackageReferenceProjectTarget (ProgressMonitor monitor, string target, ConfigurationSelector configuration, TargetEvaluationContext context)
		{
			target += ";ResolveNuGetPackageAssets";
			context.GlobalProperties.SetValue ("ResolveNuGetPackages", true);
			return base.OnRunTarget (monitor, target, configuration, context);
		}

		bool IsCoreCompileDependsOn (string target, TargetEvaluationContext context)
		{
			if (!context.ItemsToEvaluate.Contains ("Compile"))
				return false;

			// The following is based on Project.GetCompileItemsFromCoreCompileDependenciesAsync
			// and determines whether the CoreCompileDependsOn is being run.
			var coreCompileDependsOn = Project.MSBuildProject.EvaluatedProperties.GetValue<string> ("CoreCompileDependsOn");
			if (string.IsNullOrEmpty (coreCompileDependsOn))
				return false;

			var dependsList = string.Join (";", coreCompileDependsOn.Split (new [] { ";" }, StringSplitOptions.RemoveEmptyEntries).Select (s => s.Trim ()).Where (s => s.Length > 0));
			return target == dependsList;
		}
	}
}

