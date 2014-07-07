//
// ReinstallProjectPackagesAction.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.PackageManagement;
using MonoDevelop.Core;
using NuGet;

namespace MonoDevelop.PackageManagement
{
	public class ReinstallProjectPackagesAction : IPackageAction
	{
		IPackageManagementProject project;
		IPackageManagementEvents packageManagementEvents;
		List<IPackage> packagesFromSourceRepository;
		List<IPackage> projectPackages;
		ReinstallPackageOperations operations;

		public ReinstallProjectPackagesAction (
			IPackageManagementProject project,
			IPackageManagementEvents packageManagementEvents)
		{
			this.project = project;
			this.packageManagementEvents = packageManagementEvents;
		}

		public IEnumerable<IPackage> Packages {
			get {
				BeforeExecute ();
				return projectPackages;
			}
		}

		public bool UpdateDependencies { get; set; }
		public bool AllowPrereleaseVersions { get; set; }

		public bool HasPackageScriptsToRun ()
		{
			return false;
		}

		void BeforeExecute ()
		{
			if (projectPackages != null)
				return;

			GetProjectPackagesToReinstall ();
		}

		public void Execute ()
		{
			BeforeExecute ();
			//if (PackageScriptRunner != null) {
			//	ExecuteWithScriptRunner();
			//} else {
				ExecuteCore ();
			//}
		}

		void ExecuteCore ()
		{
			LogReinstallingPackagesMessage ();
			GetPackagesFromSourceRepository ();
			UninstallPackages ();
			GetReinstallPackageOperations ();
			RunPackageOperations ();
			AddPackageReferences ();
		}

		void GetProjectPackagesToReinstall ()
		{
			projectPackages = project.GetPackages ().ToList ();
		}

		void LogReinstallingPackagesMessage ()
		{
			string message = GettextCatalog.GetString ("Retargeting packages...{0}", Environment.NewLine);
			packageManagementEvents.OnPackageOperationMessageLogged (MessageLevel.Info, message);
		}

		void GetPackagesFromSourceRepository ()
		{
			packagesFromSourceRepository = new List<IPackage> ();
			foreach (IPackage package in projectPackages) {
				packagesFromSourceRepository.Add (GetPackageFromSourceRepository (package));
			}
		}

		IPackage GetPackageFromSourceRepository (IPackage package)
		{
			IPackage foundPackage = project.SourceRepository.FindPackage (package.Id, package.Version);
			if (foundPackage == null) {
				ThrowPackageNotFoundInSourceRepository (package);
			}
			return foundPackage;
		}

		void ThrowPackageNotFoundInSourceRepository (IPackage package)
		{
			string message = GettextCatalog.GetString (
				"Package {0} {1} not found in package source.",
				package.Id,
				package.Version);
			throw new ApplicationException (message);
		}

		void UninstallPackages ()
		{
			foreach (IPackage package in projectPackages) {
				var uninstallAction = new UninstallPackageAction (project, packageManagementEvents) {
					ForceRemove = true,
					Package = package,
					RemoveDependencies = false,
					AllowPrereleaseVersions = false
				};
				uninstallAction.Execute ();
			}
		}

		void GetReinstallPackageOperations ()
		{
			operations = project.GetReinstallPackageOperations (packagesFromSourceRepository);
		}

		void RunPackageOperations ()
		{
			project.RunPackageOperations (operations.Operations);
		}

		void AddPackageReferences ()
		{
			foreach (IPackage package in operations.PackagesInDependencyOrder) {
				project.AddPackageReference (package);
			}
		}
	}
}

