// 
// UpdatePackagesAction.cs
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
using NuGet;

namespace ICSharpCode.PackageManagement
{
	public class UpdatePackagesAction : IUpdatePackagesAction
	{
		List<IPackage> packages = new List<IPackage>();
		List<PackageOperation> operations = new List<PackageOperation>();
		IPackageManagementEvents packageManagementEvents;
		
		public UpdatePackagesAction(
			IPackageManagementProject project,
			IPackageManagementEvents packageManagementEvents)
		{
			Project = project;
			this.packageManagementEvents = packageManagementEvents;
			UpdateDependencies = true;
		}
		
		public IPackageManagementProject Project { get; private set; }
		
		public IEnumerable<IPackage> Packages {
			get { return packages; }
		}
		
		public IEnumerable<PackageOperation> Operations {
			get { return operations; }
		}
		
		public bool UpdateDependencies { get; set; }
		public bool AllowPrereleaseVersions { get; set; }
		public ILogger Logger { get; set; }
		
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
		
		protected virtual void ExecuteCore()
		{
			Project.UpdatePackages(this);
			packageManagementEvents.OnParentPackagesUpdated(Packages);
		}
		
//		void ExecuteWithScriptRunner()
//		{
//			using (RunPackageScriptsAction runScriptsAction = CreateRunPackageScriptsAction()) {
//				ExecuteCore();
//			}
//		}
//		
//		RunPackageScriptsAction CreateRunPackageScriptsAction()
//		{
//			return CreateRunPackageScriptsAction(PackageScriptRunner, Project);
//		}
//		
//		protected virtual RunPackageScriptsAction CreateRunPackageScriptsAction(
//			IPackageScriptRunner scriptRunner,
//			IPackageManagementProject project)
//		{
//			return new RunPackageScriptsAction(scriptRunner, project);
//		}
	}
}