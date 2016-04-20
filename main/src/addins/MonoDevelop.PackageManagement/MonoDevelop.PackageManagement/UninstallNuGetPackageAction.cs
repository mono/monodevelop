//
// UninstallNuGetPackageAction.cs
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

using System.Threading;
using System.Threading.Tasks;
using NuGet.PackageManagement;
using NuGet.ProjectManagement;

namespace MonoDevelop.PackageManagement
{
	internal class UninstallNuGetPackageAction : INuGetPackageAction
	{
		NuGetPackageManager packageManager;
		NuGetProject project;
		CancellationToken cancellationToken;

		public UninstallNuGetPackageAction (
			IMonoDevelopSolutionManager solutionManager,
			NuGetProject project,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			this.project = project;
			this.cancellationToken = cancellationToken;

			var restartManager = new DeleteOnRestartManager ();

			packageManager = new NuGetPackageManager (
				SourceRepositoryProviderFactory.CreateSourceRepositoryProvider (),
				solutionManager.Settings,
				solutionManager,
				restartManager
			);
		}

		public string PackageId { get; set; }
		public bool ForceRemove { get; set; }

		public void Execute ()
		{
			ExecuteAsync ().Wait ();
		}

		async Task ExecuteAsync ()
		{
			INuGetProjectContext context = CreateProjectContext ();

			var actions = await packageManager.PreviewUninstallPackageAsync (
				project,
				PackageId,
				CreateUninstallationContext (),
				context,
				cancellationToken);

			await packageManager.ExecuteNuGetProjectActionsAsync (
				project,
				actions,
				context,
				cancellationToken);

			await project.RunPostProcessAsync (context, cancellationToken);
		}

		public bool HasPackageScriptsToRun ()
		{
			return false;
		}

		INuGetProjectContext CreateProjectContext ()
		{
			return new NuGetProjectContext (); 
		}

		UninstallationContext CreateUninstallationContext ()
		{
			return new UninstallationContext (false, ForceRemove);
		}
	}
}

