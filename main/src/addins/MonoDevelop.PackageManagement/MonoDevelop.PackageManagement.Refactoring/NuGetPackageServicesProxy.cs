//
// NuGetPackageServicesProxy.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2017 Microsoft Corporation
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
using MonoDevelop.Projects;
using MonoDevelop.Refactoring.PackageInstaller;
using System.Threading;
using MonoDevelop.Ide;
using System.Linq;
using MonoDevelop.Core;
using System.Threading.Tasks;
using System.Collections.Immutable;

namespace MonoDevelop.PackageManagement.Refactoring
{
	class NuGetPackageServicesProxy : PackageInstallerServiceFactory.IPackageServicesProxy
	{
		public event EventHandler SourcesChanged;

		public IEnumerable<PackageInstallerServiceFactory.PackageMetadata> GetInstalledPackages (Project project)
		{
			var solutionManager = PackageManagementServices.Workspace.GetSolutionManager (project.ParentSolution);
			var proxy = new DotNetProjectProxy ((DotNetProject)project);
			var np = solutionManager.GetNuGetProject (proxy);
			if (np == null)
				yield break;
			var packages = np.GetInstalledPackagesAsync (default (CancellationToken)).WaitAndGetResult (default (CancellationToken));
			if (packages == null)
				yield break;
			foreach (var p in packages) {
				yield return new PackageInstallerServiceFactory.PackageMetadata (p.PackageIdentity.Id, p.PackageIdentity.Version.ToFullString ());
			}
		}

		public IEnumerable<KeyValuePair<string, string>> GetSources (bool includeUnOfficial, bool includeDisabled)
		{
			return Runtime.RunInMainThread (() => {
				var result = new List<KeyValuePair<string, string>> ();
				var solutionManager = PackageManagementServices.Workspace.GetSolutionManager (IdeApp.ProjectOperations.CurrentSelectedSolution);

				var provider = solutionManager.CreateSourceRepositoryProvider ();
				var packageSourceProvider = provider.PackageSourceProvider;
				var repositories = provider.GetRepositories ().ToList ();

				foreach (var repository in repositories) {
					result.Add (new KeyValuePair<string, string> (
						repository.PackageSource.Name,
						repository.PackageSource.Source
					));
				}
				return result;
			}).WaitAndGetResult (default (CancellationToken));
		}

		public void InstallLatestPackage (string source, Project project, string packageId, bool includePrerelease, bool ignoreDependencies)
		{
			// TODO
			ShowManagePackagesDialog (packageId);
		}

		public void InstallPackage (string source, Project project, string packageId, string version, bool ignoreDependencies)
		{
			// TODO
			ShowManagePackagesDialog (packageId);
		}

		public bool IsPackageInstalled (Project project, string id)
		{
			return GetInstalledPackages (project).Any (p => p.Id == id);
		}

		public void UninstallPackage (Project project, string packageId, bool removeDependencies)
		{
			throw new NotImplementedException ();
		}

		public Task<IEnumerable<(string PackageName, string Version, int Rank)>> FindPackagesWithAssemblyAsync (string source, string assemblyName, CancellationToken cancellationToken)
		{
			var result = new List<(string PackageName, string Version, int Rank)> ();
			if (source == "nuget.org" && assemblyName == "System.ValueTuple") {
				result.Add (("System.ValueTuple", "4.3.0", 1)); 
			}
			return Task.FromResult ((IEnumerable<(string PackageName, string Version, int Rank)>)result);
		}

		public Task<IEnumerable<(string PackageName, string TypeName, string Version, int Rank, IReadOnlyList<string> ContainingNamespaceNames)>> FindPackagesWithTypeAsync (string source, string name, int arity, CancellationToken cancellationToken)
		{
			var result = new List<(string PackageName, string TypeName, string Version, int Rank, IReadOnlyList<string> ContainingNamespaceNames)> ();
			return Task.FromResult ((IEnumerable<(string PackageName, string TypeName, string Version, int Rank, IReadOnlyList<string> ContainingNamespaceNames)>)result);
		}

		public Task<IEnumerable<(string AssemblyName, string TypeName, IReadOnlyList<string> ContainingNamespaceNames)>> FindReferenceAssembliesWithTypeAsync (string name, int arity, CancellationToken cancellationToken)
		{
			var result = new List<(string AssemblyName, string TypeName, IReadOnlyList<string> ContainingNamespaceNames)> ();
			return Task.FromResult ((IEnumerable<(string AssemblyName, string TypeName, IReadOnlyList<string> ContainingNamespaceNames)>)result);
		}

		public ImmutableArray<string> GetInstalledVersions (string packageName)
		{
			return ImmutableArray<string>.Empty;
		}		

		public IEnumerable<MonoDevelop.Projects.Project> GetProjectsWithInstalledPackage (MonoDevelop.Projects.Solution solution, string packageName, string version)
		{
			var result = new List<MonoDevelop.Projects.Project> ();
			return result;
		}

		public void ShowManagePackagesDialog (string packageName)
		{
 			Runtime.RunInMainThread (delegate {
				var runner = new AddPackagesDialogRunner ();
				runner.Run ();
			});
		}
	}
}
