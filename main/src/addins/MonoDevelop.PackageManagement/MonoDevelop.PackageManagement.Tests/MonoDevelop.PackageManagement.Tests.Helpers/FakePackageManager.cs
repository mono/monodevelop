//
// FakePackageManager.cs
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
using NuGet;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	public class FakePackageManager : ISharpDevelopPackageManager
	{
		public FakeProjectManager FakeProjectManager = new FakeProjectManager ();
		public FakePackageManagementProjectService FakeProjectService = new FakePackageManagementProjectService ();

		public IPackage PackagePassedToInstallPackage;
		public bool IgnoreDependenciesPassedToInstallPackage;
		public bool AllowPrereleaseVersionsPassedToInstallPackage;

		public IPackage PackagePassedToUninstallPackage;

		public UpdatePackagesAction UpdatePackagesActionsPassedToUpdatePackages;

		#pragma warning disable 67
		public event EventHandler<PackageOperationEventArgs> PackageInstalled;
		public event EventHandler<PackageOperationEventArgs> PackageInstalling;
		public event EventHandler<PackageOperationEventArgs> PackageUninstalled;
		public event EventHandler<PackageOperationEventArgs> PackageUninstalling;
		#pragma warning restore 67

		public IFileSystem FileSystem {
			get { return FakeFileSystem; }
			set { FakeFileSystem = value as FakeFileSystem; }
		}

		public FakeFileSystem FakeFileSystem = new FakeFileSystem ();

		public IPackageRepository LocalRepository { get; set; }

		public ILogger Logger { get; set; }

		public IPackageRepository SourceRepository { get; set; }

		public ISharpDevelopProjectManager ProjectManager { get; set; }

		public FakePackageRepository FakeSourceRepository = new FakePackageRepository ();

		public FakePackageManager ()
		{
			ProjectManager = FakeProjectManager;
			SourceRepository = FakeSourceRepository;
		}

		public bool ForceRemovePassedToUninstallPackage;
		public bool RemoveDependenciesPassedToUninstallPackage;

		public void UninstallPackage (IPackage package, UninstallPackageAction uninstallAction)
		{
			PackagePassedToUninstallPackage = package;
			ForceRemovePassedToUninstallPackage = uninstallAction.ForceRemove;
			RemoveDependenciesPassedToUninstallPackage = uninstallAction.RemoveDependencies;
		}

		public void UninstallPackage (IPackage package, bool forceRemove, bool removeDependencies)
		{
			throw new NotImplementedException ();
		}

		public IEnumerable<PackageOperation> PackageOperationsPassedToInstallPackage;

		public void InstallPackage (IPackage package, InstallPackageAction installAction)
		{
			PackagePassedToInstallPackage = package;

			IgnoreDependenciesPassedToInstallPackage = installAction.IgnoreDependencies;
			PackageOperationsPassedToInstallPackage = installAction.Operations;
			AllowPrereleaseVersionsPassedToInstallPackage = installAction.AllowPrereleaseVersions;
		}

		public List<PackageOperation> PackageOperationsToReturnFromGetInstallPackageOperations = new List<PackageOperation> ();
		public IPackage PackagePassedToGetInstallPackageOperations;
		public bool IgnoreDependenciesPassedToGetInstallPackageOperations;
		public bool AllowPrereleaseVersionsPassedToGetInstallPackageOperations;

		public IEnumerable<PackageOperation> GetInstallPackageOperations (IPackage package, InstallPackageAction installAction)
		{
			PackagePassedToGetInstallPackageOperations = package;
			IgnoreDependenciesPassedToGetInstallPackageOperations = installAction.IgnoreDependencies;
			AllowPrereleaseVersionsPassedToGetInstallPackageOperations = installAction.AllowPrereleaseVersions;
			return PackageOperationsToReturnFromGetInstallPackageOperations;
		}

		public IPackage PackagePassedToUpdatePackage;
		public IEnumerable<PackageOperation> PackageOperationsPassedToUpdatePackage;
		public bool UpdateDependenciesPassedToUpdatePackage;

		public void UpdatePackage (IPackage package, UpdatePackageAction updateAction)
		{
			PackagePassedToUpdatePackage = package;
			PackageOperationsPassedToUpdatePackage = updateAction.Operations;
			UpdateDependenciesPassedToUpdatePackage = updateAction.UpdateDependencies;
			AllowPrereleaseVersionsPassedToInstallPackage = updateAction.AllowPrereleaseVersions;
		}

		public void FirePackageInstalled (PackageOperationEventArgs e)
		{
			PackageInstalled (this, e);
		}

		public void FirePackageUninstalled (PackageOperationEventArgs e)
		{
			PackageUninstalled (this, e);
		}

		public IPackagePathResolver PathResolver {
			get {
				throw new NotImplementedException ();
			}
		}

		public void UpdatePackage (IPackage newPackage, bool updateDependencies)
		{
			throw new NotImplementedException ();
		}

		public void UpdatePackage (string packageId, IVersionSpec versionSpec, bool updateDependencies)
		{
			throw new NotImplementedException ();
		}

		public void InstallPackage (IPackage package, bool ignoreDependencies, bool allowPrereleaseVersions)
		{
			throw new NotImplementedException ();
		}

		public void InstallPackage (string packageId, SemanticVersion version, bool ignoreDependencies, bool allowPrereleaseVersions)
		{
			throw new NotImplementedException ();
		}

		public void UpdatePackage (IPackage newPackage, bool updateDependencies, bool allowPrereleaseVersions)
		{
			throw new NotImplementedException ();
		}

		public void UpdatePackage (string packageId, SemanticVersion version, bool updateDependencies, bool allowPrereleaseVersions)
		{
			throw new NotImplementedException ();
		}

		public void UpdatePackage (string packageId, IVersionSpec versionSpec, bool updateDependencies, bool allowPrereleaseVersions)
		{
			throw new NotImplementedException ();
		}

		public void UninstallPackage (string packageId, SemanticVersion version, bool forceRemove, bool removeDependencies)
		{
			throw new NotImplementedException ();
		}

		public void UpdatePackages (UpdatePackagesAction updateAction)
		{
			UpdatePackagesActionsPassedToUpdatePackages = updateAction;
		}

		public List<PackageOperation> PackageOperationsToReturnFromGetUpdatePackageOperations = new List<PackageOperation> ();
		public IUpdatePackageSettings SettingsPassedToGetUpdatePackageOperations;
		public IEnumerable<IPackage> PackagesPassedToGetUpdatePackageOperations;

		public IEnumerable<PackageOperation> GetUpdatePackageOperations (IEnumerable<IPackage> packages, IUpdatePackageSettings settings)
		{
			SettingsPassedToGetUpdatePackageOperations = settings;
			PackagesPassedToGetUpdatePackageOperations = packages;
			return PackageOperationsToReturnFromGetUpdatePackageOperations;
		}

		public List<PackageOperation> PackageOperationsPassedToRunPackageOperations;

		public void RunPackageOperations (IEnumerable<PackageOperation> operations)
		{
			PackageOperationsPassedToRunPackageOperations = operations.ToList ();
		}

		public IPackage PackagePassedToUpdatePackageReference;
		public IUpdatePackageSettings SettingsPassedToUpdatePackageReference;

		public void UpdatePackageReference (IPackage package, IUpdatePackageSettings settings)
		{
			PackagePassedToUpdatePackageReference = package;
			SettingsPassedToUpdatePackageReference = settings;
		}

		public bool IgnoreWalkInfoPassedToInstallPackage;
		public bool IsPackageInstalled;

		public void InstallPackage (IPackage package, bool ignoreDependencies, bool allowPrereleaseVersions, bool ignoreWalkInfo)
		{
			IsPackageInstalled = true;

			PackagePassedToInstallPackage = package;
			IgnoreDependenciesPassedToInstallPackage = ignoreDependencies;
			AllowPrereleaseVersionsPassedToInstallPackage = allowPrereleaseVersions;
			IgnoreWalkInfoPassedToInstallPackage = ignoreWalkInfo;

			PackagesInstalled.Add (package);
		}

		public List<IPackage> PackagesInstalled = new List<IPackage> ();

		public DependencyVersion DependencyVersion {
			get {
				throw new NotImplementedException ();
			}
			set {
				throw new NotImplementedException ();
			}
		}

		public bool WhatIf {
			get {
				throw new NotImplementedException ();
			}
			set {
				throw new NotImplementedException ();
			}
		}

		public ReinstallPackageOperations GetReinstallPackageOperations (IEnumerable<IPackage> packages)
		{
			throw new NotImplementedException ();
		}

		public void AddPackageReference (IPackage package, bool ignoreDependencies, bool allowPrereleaseVersions)
		{
		}
	}
}
