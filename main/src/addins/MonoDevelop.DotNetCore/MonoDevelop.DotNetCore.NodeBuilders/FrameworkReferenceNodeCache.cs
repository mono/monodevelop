//
// PackageDependencyNodeCache.cs
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Projects;

namespace MonoDevelop.DotNetCore.NodeBuilders
{
	class FrameworkReferenceNodeCache
	{
		static DotNetCoreVersion Version30 = DotNetCoreVersion.Parse ("3.0");
		static DotNetCoreVersion Version21 = DotNetCoreVersion.Parse ("2.1");

		List<FrameworkReference> references = new List<FrameworkReference> ();
		CancellationTokenSource cancellationTokenSource;

		public FrameworkReferenceNodeCache (DotNetProject project)
		{
			Project = project;
		}

		internal DotNetProject Project { get; private set; }

		public bool LoadedReferences { get; private set; }

		public event EventHandler FrameworkReferencesChanged;

		void OnFrameworkReferencesChanged ()
		{
			FrameworkReferencesChanged?.Invoke (this, EventArgs.Empty);
		}

		public void Refresh ()
		{
			try {
				if (!CanGetFrameworkReferences ()) {
					LoadedReferences  = true;
					return;
				}

				CancelCurrentRefresh ();
				GetFrameworkReferences ();
			} catch (Exception ex) {
				LoggingService.LogError ("Refresh framework references error.", ex);
			}
		}

		bool CanGetFrameworkReferences ()
		{
			return Project.TargetFramework.IsNetCoreAppOrHigher (Version30) ||
				Project.TargetFramework.IsNetStandardOrHigher (Version21);
		}

		void CancelCurrentRefresh ()
		{
			if (cancellationTokenSource != null) {
				cancellationTokenSource.Cancel ();
				cancellationTokenSource.Dispose ();
				cancellationTokenSource = null;
			}
		}

		void GetFrameworkReferences ()
		{
			var tokenSource = new CancellationTokenSource ();
			cancellationTokenSource = tokenSource;
			GetFrameworkReferencesAsync (tokenSource)
				.ContinueWith (task => OnFrameworkReferencesRead (task, tokenSource), TaskScheduler.FromCurrentSynchronizationContext ());
		}

		Task<List<FrameworkReference>> GetFrameworkReferencesAsync (CancellationTokenSource tokenSource)
		{
			var configurationSelector = IdeApp.IsInitialized ? IdeApp.Workspace?.ActiveConfiguration ?? ConfigurationSelector.Default : ConfigurationSelector.Default;
			return Task.Run (async () => {
				var references = await Project.GetFrameworkReferences (configurationSelector, tokenSource.Token)
					.ConfigureAwait (false);

				if (!tokenSource.IsCancellationRequested)
					return references.ToList ();

				return null;
			});
		}

		void OnFrameworkReferencesRead (Task<List<FrameworkReference>> task, CancellationTokenSource tokenSource)
		{
			try {
				if (task.IsFaulted) {
					// Access task.Exception to avoid 'An unhandled exception has occurred'
					// even if the exception is not being logged.
					Exception ex = task.Exception;
					if (!tokenSource.IsCancellationRequested) {
						LoggingService.LogError ("OnFrameworkReferencesRead error.", ex);
					}
				} else if (!tokenSource.IsCancellationRequested) {
					references = task.Result;
					LoadedReferences = true;
					OnFrameworkReferencesChanged ();
				}
			} catch (Exception ex) {
				LoggingService.LogError ("OnFrameworkReferencesRead error.", ex);
			}
		}

		public IEnumerable<FrameworkReferenceNode> GetFrameworkReferenceNodes ()
		{
			return references.Select (reference => new FrameworkReferenceNode (reference));
		}
	}
}
