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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using MonoDevelop.Core;
using MonoDevelop.PackageManagement;
using NuGet;

namespace ICSharpCode.PackageManagement
{
	public abstract class ProcessPackageAction : IPackageAction
	{
		IPackageManagementEvents packageManagementEvents;
		bool hasBeforeExecuteBeenRun;
		
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

		public FrameworkName ProjectTargetFramework {
			get { return Project.TargetFramework; }
		}

		public virtual bool HasPackageScriptsToRun()
		{
			return false;
		}

		protected void OnParentPackageUninstalled()
		{
			packageManagementEvents.OnParentPackageUninstalled(Package, Project);
		}
		
		public void Execute()
		{
			BeforeExecute();
			CheckForPowerShellScripts ();
			CheckLicenses ();
			//if (PackageScriptRunner != null) {
			//	ExecuteWithScriptRunner();
			//} else {
				ExecuteCore();
			//}
			LogEmptyLineForFinishedAction ();
		}
		
		protected virtual void BeforeExecute()
		{
			if (hasBeforeExecuteBeenRun)
				return;

			GetLoggerIfMissing();
			ConfigureProjectLogger();
			LogStartingMessage ();
			GetPackageIfMissing();

			hasBeforeExecuteBeenRun = true;
		}

		void LogStartingMessage ()
		{
			if (ShouldLogStartingMessage ()) {
				Logger.Log (MessageLevel.Info, GetStartingMessage ());
			}
		}

		protected virtual bool ShouldLogStartingMessage ()
		{
			return true;
		}

		string GetStartingMessage ()
		{
			return String.Format (
				GettextCatalog.GetString (StartingMessageFormat),
				GetPackageId ());
		}

		protected abstract string StartingMessageFormat { get; }

		void LogEmptyLineForFinishedAction ()
		{
			if (!ShouldLogEmptyLineForFinishedAction ())
				return;

			Logger.Log (MessageLevel.Info, String.Empty);
		}

		protected virtual bool ShouldLogEmptyLineForFinishedAction ()
		{
			return true;
		}

		void CheckForPowerShellScripts ()
		{
			if (HasPackageScriptsToRun ()) {
				ReportPowerShellScriptWarning ();
			}
		}

		void ReportPowerShellScriptWarning ()
		{
			string message = GettextCatalog.GetString ("{0} Package contains PowerShell scripts which will not be run.", GetPackageId ());
			packageManagementEvents.OnPackageOperationMessageLogged (MessageLevel.Warning, message);
		}

		void CheckLicenses ()
		{
			if (!AcceptLicenses ()) {
				string message = GettextCatalog.GetString ("Licenses not accepted.");
				throw new ApplicationException (message);
			}
		}

		bool AcceptLicenses ()
		{
			var packagesWithLicenses = new PackagesRequiringLicenseAcceptance (Project);
			var actions = new IPackageAction [] { this };
			List<IPackage> packages = packagesWithLicenses.GetPackagesRequiringLicenseAcceptance (actions).ToList ();
			if (packages.Any ()) {
				return packageManagementEvents.OnAcceptLicenses (packages);
			}

			return true;
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
				.FindPackage (
					PackageId,
					PackageVersion,
					Project.ConstraintProvider,
					AllowPrereleaseVersions,
					allowUnlisted: false);
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
		
		public string GetPackageId ()
		{
			if (Package != null) {
				return Package.Id;
			}
			return PackageId;
		}

		public SemanticVersion GetPackageVersion ()
		{
			if (Package != null) {
				return Package.Version;
			}
			return PackageVersion;
		}

		protected virtual IOpenPackageReadMeMonitor CreateOpenPackageReadMeMonitor (string packageId)
		{
			return new OpenPackageReadMeMonitor (packageId, Project, packageManagementEvents);
		}
	}
}
