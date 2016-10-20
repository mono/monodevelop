//
// CheckForNuGetPackageUpdatesTaskRunner.cs
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using NuGet.ProjectManagement;

namespace MonoDevelop.PackageManagement
{
	internal class CheckForNuGetPackageUpdatesTaskRunner
	{
		UpdatedNuGetPackagesInWorkspace updatedNuGetPackagesInWorkspace;
		List<UpdatedNuGetPackagesProvider> currentProviders;
		CancellationTokenSource cancellationTokenSource;

		public CheckForNuGetPackageUpdatesTaskRunner (UpdatedNuGetPackagesInWorkspace updatedNuGetPackagesInWorkspace)
		{
			this.updatedNuGetPackagesInWorkspace = updatedNuGetPackagesInWorkspace;
		}

		public bool IsRunning {
			get { return cancellationTokenSource != null; }
		}

		public void Start (IEnumerable<IDotNetProject> projects)
		{
			Stop ();
			CheckForUpdates (projects);
		}

		protected virtual Task CheckForUpdates (IEnumerable<IDotNetProject> projects)
		{
			cancellationTokenSource = new CancellationTokenSource ();

			var providers = projects.Select (CreateProvider).ToList ();
			currentProviders = providers;

			return Task.Run (
				() => CheckForUpdates (currentProviders, cancellationTokenSource.Token),
				cancellationTokenSource.Token
			).ContinueWith (
				task => OnCheckForUpdatesCompleted (task, providers),
				TaskScheduler.FromCurrentSynchronizationContext ());
		}

		UpdatedNuGetPackagesProvider CreateProvider (IDotNetProject project)
		{
			var solutionManager = GetSolutionManager (project.ParentSolution);
			var nugetProject = CreateNuGetProject (solutionManager, project);
			return new UpdatedNuGetPackagesProvider (
				project,
				solutionManager,
				nugetProject,
				cancellationTokenSource.Token);
		}

		protected virtual IMonoDevelopSolutionManager GetSolutionManager (ISolution solution)
		{
			return PackageManagementServices.Workspace.GetSolutionManager (solution);
		}

		protected virtual NuGetProject CreateNuGetProject (
			IMonoDevelopSolutionManager solutionManager,
			IDotNetProject project)
		{
			return new MonoDevelopNuGetProjectFactory (solutionManager.Settings)
				.CreateNuGetProject (project);
		}

		List<UpdatedNuGetPackagesInProject> CheckForUpdates (List<UpdatedNuGetPackagesProvider> providers, CancellationToken cancellationToken)
		{
			var updatedPackages = new List<UpdatedNuGetPackagesInProject> ();
			foreach (UpdatedNuGetPackagesProvider provider in providers) {
				if (cancellationToken.IsCancellationRequested) {
					break;
				}

				provider.FindUpdatedPackages ().Wait ();

				if (provider.UpdatedPackagesInProject.AnyPackages ()) {
					updatedPackages.Add (provider.UpdatedPackagesInProject);
				}
			}

			return updatedPackages;
		}

		public void Dispose ()
		{
			Stop ();
		}

		public void Stop ()
		{
			if (cancellationTokenSource != null) {
				cancellationTokenSource.Cancel ();
				cancellationTokenSource.Dispose ();
				cancellationTokenSource = null;
			}

			currentProviders = null;
		}

		void OnCheckForUpdatesCompleted (Task<List<UpdatedNuGetPackagesInProject>> task, List<UpdatedNuGetPackagesProvider> providers)
		{
			if (task.IsFaulted) {
				if (IsCurrentTask (providers)) {
					LogError ("Current check for updates task error.", task.Exception);
				} else {
					LogError ("Check for updates task error.", task.Exception);
				}
			} else if (task.IsCanceled) {
				// Ignore.
				return;
			} else if (!IsCurrentTask (providers)) {
				return;
			} else {
				currentProviders = null;
				if (cancellationTokenSource != null) {
					cancellationTokenSource.Dispose ();
					cancellationTokenSource = null;
				}
				updatedNuGetPackagesInWorkspace.CheckForUpdatesCompleted (task.Result);
			}
		}

		bool IsCurrentTask (List<UpdatedNuGetPackagesProvider> providers)
		{
			return currentProviders == providers;
		}

		protected virtual void LogError (string message, Exception ex)
		{
			LoggingService.LogError (message, ex);
		}

		protected virtual void GuiBackgroundDispatch (Action action)
		{
			PackageManagementBackgroundDispatcher.Dispatch (action);
		}
	}
}

