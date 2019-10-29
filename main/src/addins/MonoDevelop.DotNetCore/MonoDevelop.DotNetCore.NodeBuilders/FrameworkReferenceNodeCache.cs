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
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using NuGet.Frameworks;

namespace MonoDevelop.DotNetCore.NodeBuilders
{
	class FrameworkReferenceNodeCache
	{
		static DotNetCoreVersion Version30 = DotNetCoreVersion.Parse ("3.0");
		static DotNetCoreVersion Version21 = DotNetCoreVersion.Parse ("2.1");

		Dictionary<string, List<FrameworkReference>> referenceMappings =
			new Dictionary<string, List<FrameworkReference>> ();
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
				if (!CanGetFrameworkReferences (Project)) {
					LoadedReferences  = true;
					return;
				}

				CancelCurrentRefresh ();
				GetFrameworkReferences ();
			} catch (Exception ex) {
				LoggingService.LogError ("Refresh framework references error.", ex);
			}
		}

		public static bool CanGetFrameworkReferences (DotNetProject project)
		{
			foreach (TargetFrameworkMoniker targetFramework in project.TargetFrameworkMonikers) {
				if (CanGetFrameworkReferences (targetFramework)) {
					return true;
				}
			}
			return false;
		}

		public static bool CanGetFrameworkReferences (TargetFrameworkMoniker targetFramework)
		{
			return targetFramework.IsNetCoreAppOrHigher (Version30) ||
				targetFramework.IsNetStandardOrHigher (Version21);
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

		Task<Dictionary<string, List<FrameworkReference>>> GetFrameworkReferencesAsync (CancellationTokenSource tokenSource)
		{
			var config = IdeApp.IsInitialized ? IdeApp.Workspace.ActiveConfiguration : ConfigurationSelector.Default;
			return Task.Run (async () => {
				var referenceMappings = new Dictionary<string, List<FrameworkReference>> ();
				foreach (TargetFrameworkMoniker framework in Project.TargetFrameworkMonikers) {
					if (!CanGetFrameworkReferences (framework))
						continue;

					var frameworkConfig = new DotNetProjectFrameworkConfigurationSelector (config, framework.ShortName);
					var references = await Project.GetFrameworkReferences (frameworkConfig, tokenSource.Token)
						.ConfigureAwait (false);

					if (!tokenSource.IsCancellationRequested) {
						string key = GetMappingKey (framework);
						referenceMappings [key] = references.ToList ();
					}
				}
				return referenceMappings;
			});
		}

		void OnFrameworkReferencesRead (Task<Dictionary<string, List<FrameworkReference>>> task, CancellationTokenSource tokenSource)
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
					referenceMappings = task.Result;
					LoadedReferences = true;
					OnFrameworkReferencesChanged ();
				}
			} catch (Exception ex) {
				LoggingService.LogError ("OnFrameworkReferencesRead error.", ex);
			}
		}

		public IEnumerable<FrameworkReferenceNode> GetFrameworkReferenceNodes (TargetFrameworkMoniker framework)
		{
			string key = GetMappingKey (framework);
			if (referenceMappings.TryGetValue (key, out List<FrameworkReference> references)) {
				return references.Select (reference => new FrameworkReferenceNode (reference));
			}
			return Enumerable.Empty<FrameworkReferenceNode> ();
		}

		/// <summary>
		/// Target framework versions returned from ResolvePackageDependenciesDesignTime use four part version numbers
		/// so we cannot use TargetFrameworkMoniker as the dictionary key since this will not always match the
		/// project's target framework. Instead generate a NuGetFramework and use that as the key since it will
		/// normalize the version numbers.
		/// </summary>
		static string GetMappingKey (TargetFrameworkMoniker framework)
		{
			NuGetFramework nugetFramework = NuGetFramework.ParseFrameworkName (framework.ToString (), DefaultFrameworkNameProvider.Instance);
			return nugetFramework.ToString ();
		}
	}
}
