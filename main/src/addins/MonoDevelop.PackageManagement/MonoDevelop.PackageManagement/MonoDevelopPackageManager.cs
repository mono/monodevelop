// 
// MonoDevelopPackageManager.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
// 
// Copyright (C) 2012-2013 Matthew Ward
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.Collections.Generic;
using NuGet;

namespace MonoDevelop.PackageManagement
{
	internal class MonoDevelopPackageManager : PackageManager, IMonoDevelopPackageManager
	{
		IProjectSystem projectSystem;

		public MonoDevelopPackageManager(
			IPackageRepository sourceRepository,
			IProjectSystem projectSystem,
			ISolutionPackageRepository solutionPackageRepository)
			: base(
				sourceRepository,
				solutionPackageRepository.PackagePathResolver,
				solutionPackageRepository.FileSystem,
				solutionPackageRepository.Repository)
		{
			this.projectSystem = projectSystem;
			CreateProjectManager();
		}
		
		/// <summary>
		/// project manager should be created with:
		/// 	local repo = PackageReferenceRepository(projectSystem, sharedRepo)
		///     packageRefRepo should have its RegisterIfNecessary() method called before creating the project manager.
		/// 	source repo = sharedRepository
		/// </summary>
		void CreateProjectManager()
		{
			var packageRefRepository = CreatePackageReferenceRepository();
			ProjectManager = CreateProjectManager(packageRefRepository);
		}
		
		PackageReferenceRepository CreatePackageReferenceRepository()
		{
			var sharedRepository = LocalRepository as ISharedPackageRepository;
			var packageRefRepository = new PackageReferenceRepository(projectSystem, projectSystem.ProjectName, sharedRepository);
			packageRefRepository.RegisterIfNecessary();
			return packageRefRepository;
		}
		
		public IMonoDevelopProjectManager ProjectManager { get; set; }
		
		MonoDevelopProjectManager CreateProjectManager(PackageReferenceRepository packageRefRepository)
		{
			IPackageRepository sourceRepository = CreateProjectManagerSourceRepository ();
			return new MonoDevelopProjectManager (sourceRepository, PathResolver, projectSystem, packageRefRepository);
		}

		IPackageRepository CreateProjectManagerSourceRepository ()
		{
			var fallbackRepository = SourceRepository as FallbackRepository;
			if (fallbackRepository != null) {
				var primaryRepositories = new [] {
					LocalRepository,
					fallbackRepository.SourceRepository.Clone () }; 

				return new FallbackRepository (
					new AggregateRepository (primaryRepositories),
					fallbackRepository.DependencyResolver);
			}
			return new AggregateRepository (new [] { LocalRepository, SourceRepository.Clone () });
		}

		public void InstallPackage(IPackage package)
		{
			bool ignoreDependencies = false;
			bool allowPreleaseVersions = false;
			InstallPackage(package, ignoreDependencies, allowPreleaseVersions);
		}
		
		public void AddPackageReference (IPackage package, bool ignoreDependencies, bool allowPrereleaseVersions)
		{
			var monitor = new PackageReferenceMonitor (ProjectManager, this);
			using (monitor) {
				ProjectManager.AddPackageReference(package.Id, package.Version, ignoreDependencies, allowPrereleaseVersions);
			}
			
			monitor.PackagesRemoved.ForEach(packageRemoved => UninstallPackageFromSolutionRepository(packageRemoved));
		}
		
		public override void InstallPackage(IPackage package, bool ignoreDependencies, bool allowPrereleaseVersions)
		{
			base.InstallPackage(package, ignoreDependencies, allowPrereleaseVersions);
			AddPackageReference(package, ignoreDependencies, allowPrereleaseVersions);
		}
		
		public override void UninstallPackage(IPackage package, bool forceRemove, bool removeDependencies)
		{
			ProjectManager.RemovePackageReference(package.Id, forceRemove, removeDependencies);
			if (!IsPackageReferencedByOtherProjects(package)) {
				base.UninstallPackage(package, forceRemove, removeDependencies);
			}
		}
		
		public void UninstallPackageFromSolutionRepository(IPackage package)
		{
			if (!IsPackageReferencedByOtherProjects(package)) {
				ExecuteUninstall(package);
			}
		}

		public void InstallPackageIntoSolutionRepository (IPackage package)
		{
			if (!LocalRepository.Exists (package)) {
				ExecuteInstall (package);
			}
		}
		
		bool IsPackageReferencedByOtherProjects(IPackage package)
		{
			var sharedRepository = LocalRepository as ISharedPackageRepository;
			return sharedRepository.IsReferenced(package.Id, package.Version);
		}
	}
}