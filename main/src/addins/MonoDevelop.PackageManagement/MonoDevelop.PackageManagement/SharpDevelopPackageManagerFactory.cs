// 
// SharpDevelopPackageManagerFactory.cs
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

using MonoDevelop.PackageManagement;
using MonoDevelop.Projects;
using NuGet;

namespace ICSharpCode.PackageManagement
{
	public class SharpDevelopPackageManagerFactory : IPackageManagerFactory
	{
		PackageManagementOptions options;
		ISharpDevelopPackageRepositoryFactory packageRepositoryFactory;
		ISharpDevelopProjectSystemFactory projectSystemFactory;
		
		public SharpDevelopPackageManagerFactory()
			: this(
				new SharpDevelopPackageRepositoryFactory(),
				new SharpDevelopProjectSystemFactory(),
				PackageManagementServices.Options)
		{
		}
		
		public SharpDevelopPackageManagerFactory(
			ISharpDevelopPackageRepositoryFactory packageRepositoryFactory,
		    ISharpDevelopProjectSystemFactory projectSystemFactory,
		    PackageManagementOptions options)
		{
			this.packageRepositoryFactory = packageRepositoryFactory;
			this.projectSystemFactory = projectSystemFactory;
			this.options = options;
		}
		
		public ISharpDevelopPackageManager CreatePackageManager(
			IPackageRepository sourceRepository,
			IDotNetProject project)
		{
			SolutionPackageRepository solutionPackageRepository = CreateSolutionPackageRepository (project.ParentSolution);
			IProjectSystem projectSystem = CreateProjectSystem (project.DotNetProject);
			PackageOperationsResolverFactory packageOperationResolverFactory = new PackageOperationsResolverFactory();
			
			return new SharpDevelopPackageManager(
				sourceRepository,
				projectSystem,
				solutionPackageRepository,
				packageOperationResolverFactory);
		}
		
		SolutionPackageRepository CreateSolutionPackageRepository (ISolution solution)
		{
			return new SolutionPackageRepository (solution, packageRepositoryFactory, options);
		}
		
		IProjectSystem CreateProjectSystem(DotNetProject project)
		{
			return projectSystemFactory.CreateProjectSystem(project);
		}

		public IPackageManager CreatePackageManager (
			IPackageRepository sourceRepository,
			ISolutionPackageRepository solutionPackageRepository)
		{
			return new PackageManager (
				sourceRepository,
				solutionPackageRepository.PackagePathResolver,
				solutionPackageRepository.FileSystem,
				solutionPackageRepository.Repository);
		}
	}
}
