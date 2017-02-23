//
// PackageDependenciesNode.cs
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
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.PackageManagement;
using MonoDevelop.Projects;

namespace MonoDevelop.DotNetCore.NodeBuilders
{
	class PackageDependenciesNode
	{
		public static readonly string NodeName = "PackageDependencies";

		DotNetProject project;
		List<PackageDependency> frameworks = new List<PackageDependency> ();
		Dictionary<string, PackageDependency> packageDependencies = new Dictionary<string, PackageDependency> ();
		CancellationTokenSource cancellationTokenSource;

		public PackageDependenciesNode (DependenciesNode dependenciesNode)
		{
			project = dependenciesNode.Project;
		}

		internal DotNetProject Project {
			get { return project; }
		}

		public string GetLabel ()
		{
			return "NuGet";
		}

		public string GetSecondaryLabel ()
		{
			return string.Empty;
		}

		public IconId Icon {
			get { return Stock.OpenReferenceFolder; }
		}

		public IconId ClosedIcon {
			get { return Stock.ClosedReferenceFolder; }
		}

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

		Task<IEnumerable<PackageDependency>> GetPackageDependenciesAsync (CancellationTokenSource tokenSource)
		{
			var configurationSelector = IdeApp.Workspace?.ActiveConfiguration ?? ConfigurationSelector.Default;
			return Task.Run (() => project.GetPackageDependencies (configurationSelector, tokenSource.Token));
		}

		void OnPackageDependenciesRead (Task<IEnumerable<PackageDependency>> task, CancellationTokenSource tokenSource)
		{
			try {
				if (task.IsFaulted) {
					// Access task.Exception to avoid 'An unhandled exception has occured'
					// even if the exception is not being logged.
					Exception ex = task.Exception;
					if (!tokenSource.IsCancellationRequested) {
						LoggingService.LogError ("OnPackageDependenciesRead error.", ex);
					}
				} else if (!tokenSource.IsCancellationRequested) {
					LoadPackageDependencies (task.Result);
					LoadedDependencies = true;
					OnPackageDependenciesChanged ();
				}
			} catch (Exception ex) {
				LoggingService.LogError ("OnPackageDependenciesRead error.", ex);
			}
		}

		void LoadPackageDependencies (IEnumerable<PackageDependency> dependencies)
		{
			frameworks.Clear ();
			packageDependencies.Clear ();

			foreach (PackageDependency dependency in dependencies) {
				if (dependency.IsTargetFramework) {
					frameworks.Add (dependency);
				} else if (dependency.IsPackage) {
					string key = dependency.Name + "/" + dependency.Version;
					packageDependencies[key] = dependency;
				}
			}
		}

		public IEnumerable<TargetFrameworkNode> GetTargetFrameworkNodes ()
		{
			return frameworks.Select (dependency => new TargetFrameworkNode (this, dependency));
		}

		public PackageDependency GetDependency (string dependency)
		{
			PackageDependency matchedDependency = null;
			if (packageDependencies.TryGetValue (dependency, out matchedDependency))
				return matchedDependency;

			return null;
		}

		public IEnumerable<PackageDependencyNode> GetProjectPackageReferencesAsDependencyNodes ()
		{
			return project.Items.OfType<ProjectPackageReference> ()
				.Select (reference => PackageDependencyNode.Create (this, reference));
		}
	}
}
