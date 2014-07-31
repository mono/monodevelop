// 
// PackageManagementSolution.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
// 
// Copyright (C) 2012 Matthew Ward
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

using System;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.PackageManagement;
using NuGet;

namespace ICSharpCode.PackageManagement
{
	public class PackageManagementSolution : IPackageManagementSolution
	{
		IRegisteredPackageRepositories registeredPackageRepositories;
		IPackageManagementProjectService projectService;
		IPackageManagementProjectFactory projectFactory;
		ISolutionPackageRepositoryFactory solutionPackageRepositoryFactory;
		
		public PackageManagementSolution(
			IRegisteredPackageRepositories registeredPackageRepositories,
			IPackageManagementProjectService projectService,
			IPackageManagementEvents packageManagementEvents)
			: this(
				registeredPackageRepositories,
				projectService,
				new PackageManagementProjectFactory(packageManagementEvents),
				new SolutionPackageRepositoryFactory())
		{
		}
		
		public PackageManagementSolution(
			IRegisteredPackageRepositories registeredPackageRepositories,
			IPackageManagementProjectService projectService,
			IPackageManagementProjectFactory projectFactory,
			ISolutionPackageRepositoryFactory solutionPackageRepositoryFactory)
		{
			this.registeredPackageRepositories = registeredPackageRepositories;
			this.projectFactory = projectFactory;
			this.projectService = projectService;
			this.solutionPackageRepositoryFactory = solutionPackageRepositoryFactory;
		}
		
		public string FileName {
			get { return OpenSolution.FileName; }
		}
		
		ISolution OpenSolution {
			get { return projectService.OpenSolution; }
		}

		public IPackageManagementProject GetActiveProject()
		{
			if (HasActiveProject()) {
				return GetActiveProject(registeredPackageRepositories.CreateAggregateRepository());
			}
			return null;
		}
		
		bool HasActiveProject()
		{
			return GetActiveDotNetProject() != null;
		}
		
		public IDotNetProject GetActiveDotNetProject ()
		{
			if (projectService.CurrentProject != null) {
				return projectService.CurrentProject as IDotNetProject;
			}
			return null;
		}
		
		IPackageRepository ActivePackageRepository {
			get { return registeredPackageRepositories.ActiveRepository; }
		}
		
		public IPackageManagementProject GetActiveProject(IPackageRepository sourceRepository)
		{
			IDotNetProject activeProject = GetActiveDotNetProject ();
			if (activeProject != null) {
				return CreateProject (sourceRepository, activeProject);
			}
			return null;
		}

		IPackageManagementProject CreateProject (IPackageRepository sourceRepository, IDotNetProject project)
		{
			if (!(sourceRepository is AggregateRepository)) {
				sourceRepository = CreateFallbackRepository (sourceRepository);
			}
			return projectFactory.CreateProject (sourceRepository, project);
		}

		IPackageRepository CreateFallbackRepository (IPackageRepository repository)
		{
			return new FallbackRepository (repository, registeredPackageRepositories.CreateAggregateRepository ());
		}

		IPackageRepository CreatePackageRepository(PackageSource source)
		{
			return registeredPackageRepositories.CreateRepository(source);
		}
		
		public IPackageManagementProject GetProject(PackageSource source, string projectName)
		{
			IDotNetProject project = GetDotNetProject (projectName);
			return CreateProject(source, project);
		}
		
		IDotNetProject GetDotNetProject (string name)
		{
			var openProjects = new OpenDotNetProjects(projectService);
			return openProjects.FindProject(name);
		}
		
		IPackageManagementProject CreateProject (PackageSource source, IDotNetProject project)
		{
			IPackageRepository sourceRepository = CreatePackageRepository(source);
			return CreateProject(sourceRepository, project);
		}
		
		public IPackageManagementProject GetProject(IPackageRepository sourceRepository, string projectName)
		{
			IDotNetProject project = GetDotNetProject (projectName);
			return CreateProject(sourceRepository, project);
		}
		
		public IPackageManagementProject GetProject (IPackageRepository sourceRepository, IDotNetProject project)
		{
			return CreateProject (sourceRepository, project);
		}
		
		public IEnumerable<IDotNetProject> GetDotNetProjects ()
		{
			return projectService.GetOpenProjects ();
		}
		
		public bool IsOpen {
			get { return OpenSolution != null; }
		}
		
		public bool HasMultipleProjects()
		{
			return projectService.GetOpenProjects().Count() > 1;
		}
		
		public bool IsPackageInstalled(IPackage package)
		{
			ISolutionPackageRepository repository = CreateSolutionPackageRepository();
			return repository.IsInstalled(package);
		}
		
		ISolutionPackageRepository CreateSolutionPackageRepository()
		{
			return solutionPackageRepositoryFactory.CreateSolutionPackageRepository (OpenSolution);
		}
		
		public IQueryable<IPackage> GetPackages()
		{
			ISolutionPackageRepository repository = CreateSolutionPackageRepository();
			List<IPackageManagementProject> projects = GetProjects(ActivePackageRepository).ToList();
			return repository
				.GetPackages()
				.Where(package => IsPackageInstalledInSolutionOrAnyProject(projects, package));
		}
		
		bool IsPackageInstalledInSolutionOrAnyProject(IList<IPackageManagementProject> projects, IPackage package)
		{
			if (projects.Any(project => project.IsPackageInstalled(package))) {
				return true;
			}
			return false;
		}
		
		public string GetInstallPath(IPackage package)
		{
			ISolutionPackageRepository repository = CreateSolutionPackageRepository();
			return repository.GetInstallPath(package);
		}
		
		public IEnumerable<IPackage> GetPackagesInReverseDependencyOrder()
		{
			ISolutionPackageRepository repository = CreateSolutionPackageRepository();
			return repository.GetPackagesByReverseDependencyOrder();
		}
		
		public IEnumerable<IPackageManagementProject> GetProjects(IPackageRepository sourceRepository)
		{
			foreach (IDotNetProject dotNetProject in GetDotNetProjects ()) {
				yield return CreateProject (sourceRepository, dotNetProject);
			}
		}

		public ISolutionPackageRepository GetRepository ()
		{
			return CreateSolutionPackageRepository ();
		}
	}
}
