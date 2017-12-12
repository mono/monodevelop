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
using NuGet.Protocol.Core.Types;

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

		public void Start (
			IEnumerable<IDotNetProject> projects,
			ISourceRepositoryProvider sourceRepositoryProvider)
		{
			Stop ();

			if (projects.Any ())
				CheckForUpdates (projects, sourceRepositoryProvider);
		}

		protected virtual Task CheckForUpdates (
			IEnumerable<IDotNetProject> projects,
			ISourceRepositoryProvider sourceRepositoryProvider)
		{
			cancellationTokenSource = new CancellationTokenSource ();

			// Create the source repositories here and use the same ones for all the projects.
			// This prevents the credential dialog being displayed for each project.
			ISolution solution = projects.FirstOrDefault ().ParentSolution;
			var solutionManager = GetSolutionManager (solution);
			if (sourceRepositoryProvider == null)
				sourceRepositoryProvider = solutionManager.CreateSourceRepositoryProvider ();

			var providers = projects.Select (project => CreateProvider (solutionManager, project, sourceRepositoryProvider)).ToList ();
			currentProviders = providers;

			return Task.Run (
				() => CheckForUpdates (currentProviders, cancellationTokenSource.Token),
				cancellationTokenSource.Token
			).ContinueWith (
				task => OnCheckForUpdatesCompleted (task, providers),
				TaskScheduler.FromCurrentSynchronizationContext ());
		}

		UpdatedNuGetPackagesProvider CreateProvider (
			IMonoDevelopSolutionManager solutionManager,
			IDotNetProject project,
			ISourceRepositoryProvider sourceRepositoryProvider)
		{
			var nugetProject = CreateNuGetProject (solutionManager, project);
			return new UpdatedNuGetPackagesProvider (
				project,
				sourceRepositoryProvider,
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

		static async Task<List<UpdatedNuGetPackagesInProject>> CheckForUpdates (List<UpdatedNuGetPackagesProvider> providers, CancellationToken cancellationToken)
		{
			var updatedPackages = new List<UpdatedNuGetPackagesInProject> ();
			foreach (UpdatedNuGetPackagesProvider provider in providers) {
				if (cancellationToken.IsCancellationRequested) {
					break;
				}

				await provider.FindUpdatedPackages ().ConfigureAwait (false);

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

