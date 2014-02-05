// 
// ProcessPackageActions.cs
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
using NuGet;

namespace ICSharpCode.PackageManagement
{
	public abstract class ProcessPackageAction : IPackageAction
	{
		IPackageManagementEvents packageManagementEvents;
		
		public ProcessPackageAction(
			IPackageManagementProject project,
			IPackageManagementEvents packageManagementEvents)
		{
			this.Project = project;
			this.packageManagementEvents = packageManagementEvents;
		}
		
		public IPackageManagementProject Project { get; set; }
		public ILogger Logger { get; set; }
		public IPackage Package { get; set; }
		public SemanticVersion PackageVersion { get; set; }
		public string PackageId { get; set; }
		//public IPackageScriptRunner PackageScriptRunner { get; set; }
		public bool AllowPrereleaseVersions { get; set; }
		
		public virtual bool HasPackageScriptsToRun()
		{
			return false;
		}
		
		protected void OnParentPackageInstalled()
		{
			packageManagementEvents.OnParentPackageInstalled(Package);
		}
		
		protected void OnParentPackageUninstalled()
		{
			packageManagementEvents.OnParentPackageUninstalled(Package);
		}
		
		public void Execute()
		{
			BeforeExecute();
			//if (PackageScriptRunner != null) {
			//	ExecuteWithScriptRunner();
			//} else {
				ExecuteCore();
			//}
		}
		
		protected virtual void BeforeExecute()
		{
			GetLoggerIfMissing();
			ConfigureProjectLogger();
			GetPackageIfMissing();
		}
		
		void ExecuteWithScriptRunner()
		{
//			using (RunPackageScriptsAction runScriptsAction = CreateRunPackageScriptsAction()) {
//				ExecuteCore();
//			}
		}
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
		
		protected virtual void ExecuteCore()
		{
		}
		
		void GetLoggerIfMissing()
		{
			if (Logger == null) {
				Logger = new PackageManagementLogger(packageManagementEvents);
			}
		}
		
		void ConfigureProjectLogger()
		{
			Project.Logger = Logger;
		}
		
		void GetPackageIfMissing()
		{
			if (Package == null) {
				FindPackage();
			}
			if (Package == null) {
				ThrowPackageNotFoundError(PackageId);
			}
		}
		
		void FindPackage()
		{
			Package = Project
				.SourceRepository
				.FindPackage(PackageId, PackageVersion, AllowPrereleaseVersions, allowUnlisted: true);
		}
		
		void ThrowPackageNotFoundError(string packageId)
		{
			string message = String.Format("Unable to find package '{0}'.", packageId);
			throw new ApplicationException(message);
		}
		
		protected bool PackageIdExistsInProject()
		{
			string id = GetPackageId();
			return Project.IsPackageInstalled(id);
		}
		
		string GetPackageId()
		{
			if (Package != null) {
				return Package.Id;
			}
			return PackageId;
		}
	}
}
