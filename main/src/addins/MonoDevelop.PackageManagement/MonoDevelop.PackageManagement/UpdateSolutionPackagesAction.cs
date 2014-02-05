// 
// UpdateSolutionPackagesAction.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
// 
// Copyright (C) 2013 Matthew Ward
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
using NuGet;

namespace ICSharpCode.PackageManagement
{
	public class UpdateSolutionPackagesAction : IUpdatePackagesAction
	{
		List<IPackageFromRepository> packages = new List<IPackageFromRepository>();
		List<PackageOperation> operations = new List<PackageOperation>();
		List<IPackageManagementProject> projects;
		IPackageManagementEvents packageManagementEvents;
		
		public UpdateSolutionPackagesAction(
			IPackageManagementSolution solution,
			IPackageManagementEvents packageManagementEvents)
		{
			this.Solution = solution;
			this.UpdateDependencies = true;
			this.packageManagementEvents = packageManagementEvents;
		}
		
		public IPackageManagementSolution Solution { get; private set; }
		//public IPackageScriptRunner PackageScriptRunner { get; set; }
		public bool UpdateDependencies { get; set; }
		public bool AllowPrereleaseVersions { get; set; }
		public ILogger Logger { get; set; }
		
		public IEnumerable<PackageOperation> Operations {
			get { return operations; }
		}
		
		public IEnumerable<IPackageFromRepository> Packages {
			get { return packages; }
		}
		
		public bool HasPackageScriptsToRun()
		{
			var files = new PackageFilesForOperations(Operations);
			return files.HasAnyPackageScripts();
		}
		
		public void AddOperations(IEnumerable<PackageOperation> operations)
		{
			this.operations.AddRange(operations);
		}
		
		public void AddPackages(IEnumerable<IPackageFromRepository> packages)
		{
			this.packages.AddRange(packages);
		}
		
		public void Execute()
		{
			//if (PackageScriptRunner != null) {
			//	ExecuteWithScriptRunner();
			//} else {
				ExecuteCore();
			//}
		}
		
//		void ExecuteWithScriptRunner()
//		{
//			using (RunAllProjectPackageScriptsAction runScriptsAction = CreateRunPackageScriptsAction()) {
//				ExecuteCore();
//			}
//		}
//		
//		RunAllProjectPackageScriptsAction CreateRunPackageScriptsAction()
//		{
//			return CreateRunPackageScriptsAction(PackageScriptRunner, GetProjects());
//		}
		
		void ExecuteCore()
		{
			RunPackageOperations();
			UpdatePackageReferences();
			packageManagementEvents.OnParentPackagesUpdated(Packages);
		}
		
		void RunPackageOperations()
		{
			IPackageManagementProject project = GetProjects().First();
			project.RunPackageOperations(operations);
		}
		
		IEnumerable<IPackageManagementProject> GetProjects()
		{
			if (projects == null) {
				IPackageFromRepository package = packages.First();
				projects = Solution
					.GetProjects(package.Repository)
					.Select(project => {
						project.Logger = Logger;
						return project;
					})
					.ToList();
			}
			return projects;
		}
		
		void UpdatePackageReferences()
		{
			foreach (IPackageManagementProject project in GetProjects()) {
				foreach (IPackageFromRepository package in packages) {
					if (project.HasOlderPackageInstalled(package)) {
						project.UpdatePackageReference(package, this);
					}
				}
			}
		}
		
//		protected virtual RunAllProjectPackageScriptsAction CreateRunPackageScriptsAction(
//			IPackageScriptRunner scriptRunner,
//			IEnumerable<IPackageManagementProject> projects)
//		{
//			return new RunAllProjectPackageScriptsAction(scriptRunner, projects);
//		}
	}
}