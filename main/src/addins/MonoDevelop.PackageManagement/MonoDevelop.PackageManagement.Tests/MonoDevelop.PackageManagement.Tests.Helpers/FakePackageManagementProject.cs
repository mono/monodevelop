//
// FakePackageManagementProject.cs
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
using System.Runtime.Versioning;
using ICSharpCode.PackageManagement;
using MonoDevelop.Projects;
using NuGet;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	public class FakePackageManagementProject : IPackageManagementProject
	{
		public FakePackageManagementProject ()
			: this ("Test")
		{
		}

		public FakePackageManagementProject (string name)
		{
			FakeUninstallPackageAction = new FakeUninstallPackageAction (this) {
				Logger = new FakeLogger ()
			};

			FindPackageAction = packageId => {
				return FakePackages.FirstOrDefault (package => package.Id == packageId);
			};

			InstallPackageAction = (package, installAction) => {
				PackagePassedToInstallPackage = package;
				PackageOperationsPassedToInstallPackage = installAction.Operations;
				IgnoreDependenciesPassedToInstallPackage = installAction.IgnoreDependencies;
				AllowPrereleaseVersionsPassedToInstallPackage = installAction.AllowPrereleaseVersions;
			};

			UpdatePackageAction = (package, updateAction) => {
				PackagePassedToUpdatePackage = package;
				PackageOperationsPassedToUpdatePackage = updateAction.Operations;
				UpdateDependenciesPassedToUpdatePackage = updateAction.UpdateDependencies;
				AllowPrereleaseVersionsPassedToUpdatePackage = updateAction.AllowPrereleaseVersions;
				IsUpdatePackageCalled = true;
			};

			this.Name = name;

			ConstraintProvider = NullConstraintProvider.Instance;
		}

		public FakeUninstallPackageAction FakeUninstallPackageAction;

		public FakeUpdatePackageAction FirstFakeUpdatePackageActionCreated {
			get { return FakeUpdatePackageActionsCreated [0]; }
		}

		public FakeUpdatePackageAction SecondFakeUpdatePackageActionCreated {
			get { return FakeUpdatePackageActionsCreated [1]; }
		}

		public List<FakeUpdatePackageAction> FakeUpdatePackageActionsCreated = 
			new List<FakeUpdatePackageAction> ();

		public string Name { get; set; }

		public bool IsPackageInstalled (string packageId)
		{
			return FakePackages.Any (p => p.Id == packageId);
		}

		public bool IsPackageInstalled (IPackage package)
		{
			return FakePackages.Contains (package);
		}

		public List<FakePackage> FakePackages = new List<FakePackage> ();

		public IQueryable<IPackage> GetPackages ()
		{
			return FakePackages.AsQueryable ();
		}

		public List<FakePackageOperation> FakeInstallOperations = new List<FakePackageOperation> ();
		public IPackage PackagePassedToGetInstallPackageOperations;
		public bool IgnoreDependenciesPassedToGetInstallPackageOperations;
		public bool AllowPrereleaseVersionsPassedToGetInstallPackageOperations;

		public virtual IEnumerable<PackageOperation> GetInstallPackageOperations (IPackage package, InstallPackageAction installAction)
		{
			PackagePassedToGetInstallPackageOperations = package;
			IgnoreDependenciesPassedToGetInstallPackageOperations = installAction.IgnoreDependencies;
			AllowPrereleaseVersionsPassedToGetInstallPackageOperations = installAction.AllowPrereleaseVersions;

			return FakeInstallOperations;
		}

		public ILogger Logger { get; set; }

		public IPackage PackagePassedToInstallPackage;
		public IEnumerable<PackageOperation> PackageOperationsPassedToInstallPackage;
		public bool IgnoreDependenciesPassedToInstallPackage;
		public bool AllowPrereleaseVersionsPassedToInstallPackage;

		public Action<IPackage, InstallPackageAction> InstallPackageAction;

		public void InstallPackage (IPackage package, InstallPackageAction installAction)
		{
			InstallPackageAction (package, installAction);
		}

		public FakePackageOperation AddFakeInstallOperation ()
		{
			var package = new FakePackage ("MyPackage");
			var operation = new FakePackageOperation (package, PackageAction.Install);
			FakeInstallOperations.Add (operation);
			return operation;
		}

		public FakePackageOperation AddFakeUninstallOperation ()
		{
			var package = new FakePackage ("MyPackage");
			var operation = new FakePackageOperation (package, PackageAction.Uninstall);
			FakeInstallOperations.Add (operation);
			return operation;
		}

		public FakePackageRepository FakeSourceRepository = new FakePackageRepository ();

		public IPackageRepository SourceRepository {
			get { return FakeSourceRepository; }
		}

		public IPackage PackagePassedToUninstallPackage;
		public bool ForceRemovePassedToUninstallPackage;
		public bool RemoveDependenciesPassedToUninstallPackage;

		public void UninstallPackage (IPackage package, UninstallPackageAction uninstallAction)
		{
			PackagePassedToUninstallPackage = package;
			ForceRemovePassedToUninstallPackage = uninstallAction.ForceRemove;
			RemoveDependenciesPassedToUninstallPackage = uninstallAction.RemoveDependencies;
		}

		public IPackage PackagePassedToUpdatePackage;
		public IEnumerable<PackageOperation> PackageOperationsPassedToUpdatePackage;
		public bool UpdateDependenciesPassedToUpdatePackage;
		public bool AllowPrereleaseVersionsPassedToUpdatePackage;
		public bool IsUpdatePackageCalled;

		public void UpdatePackage (IPackage package, UpdatePackageAction updateAction)
		{
			UpdatePackageAction (package, updateAction);
		}

		public Action<IPackage, UpdatePackageAction> UpdatePackageAction;

		public FakeInstallPackageAction LastInstallPackageCreated;

		public virtual InstallPackageAction CreateInstallPackageAction ()
		{
			LastInstallPackageCreated = new FakeInstallPackageAction (this);
			return LastInstallPackageCreated;
		}

		public virtual UninstallPackageAction CreateUninstallPackageAction ()
		{
			return FakeUninstallPackageAction;
		}

		public UpdatePackageAction CreateUpdatePackageAction ()
		{
			var action = new FakeUpdatePackageAction (this);
			FakeUpdatePackageActionsCreated.Add (action);
			return action;
		}

		public event EventHandler<PackageOperationEventArgs> PackageInstalled;

		public void FirePackageInstalledEvent (PackageOperationEventArgs e)
		{
			if (PackageInstalled != null) {
				PackageInstalled (this, e);
			}
		}

		public event EventHandler<PackageOperationEventArgs> PackageUninstalled;

		public void FirePackageUninstalledEvent (PackageOperationEventArgs e)
		{
			if (PackageUninstalled != null) {
				PackageUninstalled (this, e);
			}
		}

		public event EventHandler<PackageOperationEventArgs> PackageReferenceAdded;

		public void FirePackageReferenceAddedEvent (PackageOperationEventArgs e)
		{
			if (PackageReferenceAdded != null) {
				PackageReferenceAdded (this, e);
			}
		}

		public event EventHandler<PackageOperationEventArgs> PackageReferenceRemoving;

		public void FirePackageReferenceRemovingEvent (PackageOperationEventArgs e)
		{
			if (PackageReferenceRemoving != null) {
				PackageReferenceRemoving (this, e);
			}
		}

		public List<FakePackage> FakePackagesInReverseDependencyOrder = 
			new List<FakePackage> ();

		public IEnumerable<IPackage> GetPackagesInReverseDependencyOrder ()
		{
			return FakePackagesInReverseDependencyOrder;
		}

		public void AddFakePackage (string id)
		{
			FakePackages.Add (new FakePackage (id));
		}

		public FakePackage AddFakePackage (string id, string version)
		{
			return AddFakePackage (new FakePackage (id, version));
		}

		public FakePackage AddFakePackage (FakePackage package)
		{
			FakePackages.Add (package);
			return package;
		}

		public void AddFakePackageToSourceRepository (string packageId)
		{
			FakeSourceRepository.AddFakePackage (packageId);
		}

		public FakePackage AddFakePackageToSourceRepository (string packageId, string version)
		{
			return FakeSourceRepository.AddFakePackageWithVersion (packageId, version);
		}

		public void UpdatePackages (UpdatePackagesAction action)
		{
		}

		public List<UpdatePackagesAction> UpdatePackagesActionsCreated = 
			new List<UpdatePackagesAction> ();

		public UpdatePackagesAction CreateUpdatePackagesAction ()
		{
			var action = new UpdatePackagesAction (this, null);
			UpdatePackagesActionsCreated.Add (action);
			return action;
		}

		public UpdatePackagesAction UpdatePackagesActionPassedToGetUpdatePackagesOperations;
		public IUpdatePackageSettings SettingsPassedToGetUpdatePackagesOperations;
		public List<IPackage> PackagesOnUpdatePackagesActionPassedToGetUpdatePackagesOperations;
		public List<PackageOperation> PackageOperationsToReturnFromGetUpdatePackagesOperations =
			new List<PackageOperation> ();

		public IEnumerable<PackageOperation> GetUpdatePackagesOperations (
			IEnumerable<IPackage> packages,
			IUpdatePackageSettings settings)
		{
			SettingsPassedToGetUpdatePackagesOperations = settings;
			PackagesOnUpdatePackagesActionPassedToGetUpdatePackagesOperations = packages.ToList ();
			return PackageOperationsToReturnFromGetUpdatePackagesOperations;
		}

		public ReinstallPackageOperations ReinstallOperations;
		public IEnumerable<IPackage> PackagesPassedToGetReinstallPackageOperations;

		public ReinstallPackageOperations GetReinstallPackageOperations (IEnumerable<IPackage> packages)
		{
			PackagesPassedToGetReinstallPackageOperations = packages;
			return ReinstallOperations;
		}

		public IEnumerable<PackageOperation> PackageOperationsRun;

		public void RunPackageOperations (IEnumerable<PackageOperation> operations)
		{
			PackageOperationsRun = operations;
		}

		public bool HasOlderPackageInstalled (IPackage package)
		{
			IPackage matchedPackage = FakePackages.FirstOrDefault (p => p.Id == package.Id);
			if (matchedPackage != null) {
				return matchedPackage.Version < package.Version;
			}
			return false;
		}

		public void UpdatePackageReference (IPackage package, IUpdatePackageSettings settings)
		{
			throw new NotImplementedException ();
		}

		#pragma warning disable 0067
		public event EventHandler<PackageOperationEventArgs> PackageReferenceRemoved;
		#pragma warning restore 0067

		public Func<string, IPackage> FindPackageAction;

		public IPackage FindPackage (string packageId)
		{
			return FindPackageAction (packageId);
		}

		public FrameworkName TargetFramework { get; set; }

		public DotNetProject DotNetProject {
			get {
				throw new NotImplementedException ();
			}
		}

		public ReinstallPackageAction CreateReinstallPackageAction ()
		{
			var action = new ReinstallPackageAction (this, new PackageManagementEvents ());
			ReinstallPackageActionsCreated.Add (action);
			return action;
		}

		public List<ReinstallPackageAction> ReinstallPackageActionsCreated = new List<ReinstallPackageAction> ();

		public List<IPackage> PackageReferencesAdded = new List<IPackage> ();

		public void AddPackageReference (IPackage package)
		{
			PackageReferencesAdded.Add (package);
		}

		public FakeDotNetProject FakeDotNetProject = new FakeDotNetProject (@"d:\projects\MyProject\MyProject.csproj");

		public IDotNetProject Project {
			get { return FakeDotNetProject; }
		}

		FakeProjectManager projectManager = new FakeProjectManager ();

		public List<PackageReference> PackageReferences {
			get { return projectManager.PackageReferences; }
		}

		public PackageReference AddPackageReference (string packageId, string packageVersion)
		{
			return projectManager.AddPackageReference (packageId, packageVersion);
		}

		public IEnumerable<PackageReference> GetPackageReferences ()
		{
			return PackageReferences;
		}

		public bool AnyUnrestoredPackages ()
		{
			throw new NotImplementedException ();
		}

		public IPackageConstraintProvider ConstraintProvider { get; set; }
	}
}

