//
// PackageDependencyNodeCache.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.PackageManagement;
using MonoDevelop.Projects;

namespace MonoDevelop.DotNetCore.NodeBuilders
{
	class PackageDependencyNodeCache
	{
		List<PackageDependencyInfo> frameworks = new List<PackageDependencyInfo> ();
		CancellationTokenSource cancellationTokenSource;

		public PackageDependencyNodeCache (DotNetProject project)
		{
			Project = project;
		}

		internal DotNetProject Project { get; private set; }

		public bool LoadedDependencies { get; private set; }

		public event EventHandler PackageDependenciesChanged;

		void OnPackageDependenciesChanged ()
		{
			var handler = PackageDependenciesChanged;
			if (handler != null) {
				handler (this, new EventArgs ());
			}
		}

		public void Refresh ()
		{
			try {
				CancelCurrentRefresh ();
				GetPackageDependencies ();
			} catch (Exception ex) {
				LoggingService.LogError ("Refresh packages folder error.", ex);
			}
		}

		void CancelCurrentRefresh ()
		{
			if (cancellationTokenSource != null) {
				cancellationTokenSource.Cancel ();
				cancellationTokenSource.Dispose ();
				cancellationTokenSource = null;
			}
		}

		void GetPackageDependencies ()
		{
			var tokenSource = new CancellationTokenSource ();
			cancellationTokenSource = tokenSource;
			GetPackageDependenciesAsync (tokenSource)
				.ContinueWith (task => OnPackageDependenciesRead (task, tokenSource), TaskScheduler.FromCurrentSynchronizationContext ());
		}

		Task<List<PackageDependencyInfo>> GetPackageDependenciesAsync (CancellationTokenSource tokenSource)
		{
			var configurationSelector = IdeApp.IsInitialized ? IdeApp.Workspace?.ActiveConfiguration ?? ConfigurationSelector.Default : ConfigurationSelector.Default;
			return Task.Run (async () => {
				var dependencies = await Project.GetPackageDependencies (configurationSelector, tokenSource.Token)
					.ConfigureAwait (false);

				if (!tokenSource.IsCancellationRequested)
					return LoadPackageDependencies (dependencies);

				return null;
			});
		}

		void OnPackageDependenciesRead (Task<List<PackageDependencyInfo>> task, CancellationTokenSource tokenSource)
		{
			try {
				if (task.IsFaulted) {
					// Access task.Exception to avoid 'An unhandled exception has occurred'
					// even if the exception is not being logged.
					Exception ex = task.Exception;
					if (!tokenSource.IsCancellationRequested) {
						LoggingService.LogError ("OnPackageDependenciesRead error.", ex);
					}
				} else if (!tokenSource.IsCancellationRequested) {
					frameworks = task.Result;
					LoadedDependencies = true;
					OnPackageDependenciesChanged ();
				}
			} catch (Exception ex) {
				LoggingService.LogError ("OnPackageDependenciesRead error.", ex);
			}
		}

		static List<PackageDependencyInfo> LoadPackageDependencies (IEnumerable<PackageDependency> dependencies)
		{
			var frameworks = new List<PackageDependencyInfo> ();
			var packageDependencies = new Dictionary<string, PackageDependencyInfo> ();

			foreach (PackageDependency dependency in dependencies) {
				if (dependency.IsTargetFramework) {
					frameworks.Add (new PackageDependencyInfo (dependency));
				} else if (dependency.IsPackage) {
					string key = dependency.Name + "/" + dependency.Version;
					packageDependencies [key] = new PackageDependencyInfo (dependency);
				} else if (dependency.IsDiagnostic) {
					string key = dependency.FrameworkName + "/" + dependency.Name + "/" + dependency.Version + "/" + dependency.DiagnosticCode;
					packageDependencies [key] = new PackageDependencyInfo (dependency);
				}
			}

			foreach (PackageDependencyInfo framework in frameworks) {
				BuildChildDependencies (packageDependencies, framework);
			}

			return frameworks;
		}

		public IEnumerable<TargetFrameworkNode> GetTargetFrameworkNodes (
			DependenciesNode dependenciesNode,
			bool sdkDependencies)
		{
			return frameworks.Select (dependency => new TargetFrameworkNode (dependenciesNode, dependency, sdkDependencies));
		}

		public IEnumerable<PackageDependencyNode> GetProjectPackageReferencesAsDependencyNodes (DependenciesNode dependenciesNode)
		{
			return Project.Items.OfType<ProjectPackageReference> ()
				.Select (reference => PackageDependencyNode.Create (dependenciesNode, reference));
		}

		static void BuildChildDependencies (Dictionary<string, PackageDependencyInfo> packageDependencies, PackageDependencyInfo dependency)
		{
			foreach (string childDependencyName in dependency.DependencyNames) {
				if (packageDependencies.TryGetValue (childDependencyName, out PackageDependencyInfo childDependency)) {
					if (!childDependency.IsBuilt) {
						BuildChildDependencies (packageDependencies, childDependency);
					}
					dependency.AddChild (childDependency);
				}
			}
			dependency.IsBuilt = true;
		}
	}
}
