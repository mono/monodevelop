// 
// PackageViewModel.cs
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
using System.Text;
using System.Runtime.Versioning;
using MonoDevelop.PackageManagement;
using NuGet;

namespace ICSharpCode.PackageManagement
{
	public class PackageViewModel : ViewModelBase<PackageViewModel>
	{
		DelegateCommand addPackageCommand;
		DelegateCommand removePackageCommand;
		DelegateCommand managePackageCommand;
		
		PackageManagementSelectedProjects selectedProjects;
		IPackageManagementEvents packageManagementEvents;
		IPackageFromRepository package;
		IEnumerable<PackageOperation> packageOperations = new PackageOperation[0];
		PackageViewModelOperationLogger logger;
		IPackageActionRunner actionRunner;
		IPackageViewModelParent parent;
		string summary;
		List<PackageDependency> dependencies;
		bool isChecked;

		public PackageViewModel(
			IPackageViewModelParent parent,
			IPackageFromRepository package,
			PackageManagementSelectedProjects selectedProjects,
			IPackageManagementEvents packageManagementEvents,
			IPackageActionRunner actionRunner,
			ILogger logger)
		{
			this.parent = parent;
			this.package = package;
			this.selectedProjects = selectedProjects;
			this.packageManagementEvents = packageManagementEvents;
			this.actionRunner = actionRunner;
			this.logger = CreateLogger(logger);
			
			CreateCommands();
		}
		
		public IPackageViewModelParent GetParent()
		{
			return parent;
		}
		
		protected virtual PackageViewModelOperationLogger CreateLogger(ILogger logger)
		{
			return new PackageViewModelOperationLogger(logger, package);
		}
		
		void CreateCommands()
		{
			addPackageCommand = new DelegateCommand(param => AddPackage());
			removePackageCommand = new DelegateCommand(param => RemovePackage());
			managePackageCommand = new DelegateCommand(param => ManagePackage());
		}
	
		public ICommand AddPackageCommand {
			get { return addPackageCommand; }
		}
		
		public ICommand RemovePackageCommand {
			get { return removePackageCommand; }
		}
		
		public ICommand ManagePackageCommand {
			get { return managePackageCommand; }
		}
		
		public IPackage GetPackage()
		{
			return package;
		}
		
		public bool HasLicenseUrl {
			get { return LicenseUrl != null; }
		}
		
		public Uri LicenseUrl {
			get { return package.LicenseUrl; }
		}
		
		public bool HasProjectUrl {
			get { return ProjectUrl != null; }
		}
		
		public Uri ProjectUrl {
			get { return package.ProjectUrl; }
		}
		
		public bool HasReportAbuseUrl {
			get { return ReportAbuseUrl != null; }
		}
		
		public Uri ReportAbuseUrl {
			get { return package.ReportAbuseUrl; }
		}
		
		public bool IsAdded {
			get { return IsPackageInstalled(); }
		}
		
		bool IsPackageInstalled()
		{
			return selectedProjects.IsPackageInstalled(package);
		}
		
		public IEnumerable<PackageDependency> Dependencies {
			get {
				if (dependencies == null) {
					FrameworkName targetFramework = selectedProjects.GetTargetFramework ();
					dependencies = package
						.GetCompatiblePackageDependencies (targetFramework)
						.ToList ();
				}
				return dependencies;
			}
		}
		
		public bool HasDependencies {
			get { return Dependencies.Any (); }
		}
		
		public bool HasNoDependencies {
			get { return !HasDependencies; }
		}
		
		public IEnumerable<string> Authors {
			get { return package.Authors; }
		}
		
		public bool HasDownloadCount {
			get { return package.DownloadCount >= 0; }
		}
		
		public string Id {
			get { return package.Id; }
		}
		
		public string Name {
			get { return package.GetName(); }
		}
		
		public bool HasGalleryUrl {
			get { return GalleryUrl != null; }
		}
		
		public bool HasNoGalleryUrl {
			get { return !HasGalleryUrl; }
		}
		
		public Uri GalleryUrl {
			get { return package.GalleryUrl; }
		}
		
		public Uri IconUrl {
			get { return package.IconUrl; }
		}

		public bool HasIconUrl {
			get { return IconUrl != null; }
		}

		public string Summary {
			get {
				if (summary == null) {
					summary = StripNewLinesAndIndentation (package.SummaryOrDescription ());
				}
				return summary;
			}
		}

		string StripNewLinesAndIndentation (string text)
		{
			return PackageListViewTextFormatter.Format (text);
		}
		
		public SemanticVersion Version {
			get { return package.Version; }
		}
		
		public int DownloadCount {
			get { return package.DownloadCount; }
		}
		
		public string Description {
			get { return package.Description; }
		}
		
		public DateTimeOffset? LastPublished {
			get { return package.Published; }
		}
		
		public bool HasLastPublished {
			get { return package.Published.HasValue; }
		}
		
		public void AddPackage()
		{
			ClearReportedMessages();
			logger.LogAddingPackage();
			
			using (IDisposable operation = StartInstallOperation(package)) {
				TryInstallingPackage();
			}
			
			logger.LogAfterPackageOperationCompletes();
		}
		
		protected virtual IDisposable StartInstallOperation(IPackageFromRepository package)
		{
			return package.StartInstallOperation();
		}
		
		void ClearReportedMessages()
		{
			packageManagementEvents.OnPackageOperationsStarting();
		}
		
		void GetPackageOperations()
		{
			IPackageManagementProject project = GetSingleProjectSelected();
			project.Logger = logger;
			InstallPackageAction installAction = project.CreateInstallPackageAction();
			installAction.AllowPrereleaseVersions = parent.IncludePrerelease;
			packageOperations = project.GetInstallPackageOperations(package, installAction);
		}
		
		IPackageManagementProject GetSingleProjectSelected()
		{
			return selectedProjects.GetSingleProjectSelected(package.Repository);
		}
		
		bool CanInstallPackage()
		{
			IEnumerable<IPackage> packages = GetPackagesRequiringLicenseAcceptance();
			if (packages.Any()) {
				return packageManagementEvents.OnAcceptLicenses(packages);
			}
			return true;
		}
		
		IEnumerable<IPackage> GetPackagesRequiringLicenseAcceptance()
		{
			IList<IPackage> packagesToBeInstalled = GetPackagesToBeInstalled();
			return GetPackagesRequiringLicenseAcceptance(packagesToBeInstalled);
		}
		
		IEnumerable<IPackage> GetPackagesRequiringLicenseAcceptance(IList<IPackage> packagesToBeInstalled)
		{
			return packagesToBeInstalled.Where(package => PackageRequiresLicenseAcceptance(package));
		}
		
		IList<IPackage> GetPackagesToBeInstalled()
		{
			List<IPackage> packages = new List<IPackage>();
			foreach (PackageOperation operation in packageOperations) {
				if (operation.Action == PackageAction.Install) {
					packages.Add(operation.Package);
				}
			}
			return packages;
		}

		bool PackageRequiresLicenseAcceptance(IPackage package)
		{
			return package.RequireLicenseAcceptance && !IsPackageInstalledInSolution(package);
		}
		
		bool IsPackageInstalledInSolution(IPackage package)
		{
			return selectedProjects.IsPackageInstalledInSolution(package);
		}
		
		void TryInstallingPackage()
		{
			try {
				GetPackageOperations();
				if (CanInstallPackage()) {
					InstallPackage();
				}
			} catch (Exception ex) {
				ReportError(ex);
				logger.LogError(ex);
			}
		}
		
		void InstallPackage()
		{
			InstallPackage(packageOperations);
			OnPropertyChanged(model => model.IsAdded);
		}
		
		void InstallPackage(IEnumerable<PackageOperation> packageOperations)
		{
			var action = CreateInstallPackageAction () as ProcessPackageOperationsAction;
			action.Operations = packageOperations;
			actionRunner.Run(action);
		}

		public IPackageAction CreateInstallPackageAction ()
		{
			IPackageManagementProject project = GetSingleProjectSelected ();
			ProcessPackageOperationsAction action = CreateInstallPackageAction (project);
			action.AllowPrereleaseVersions = parent.IncludePrerelease;
			action.Package = package;
			return action;
		}
		
		protected virtual ProcessPackageOperationsAction CreateInstallPackageAction(
			IPackageManagementProject project)
		{
			return project.CreateInstallPackageAction();
		}
		
		void ReportError(Exception ex)
		{
			packageManagementEvents.OnPackageOperationError(ex);
		}
		
		public void RemovePackage()
		{
			ClearReportedMessages();
			logger.LogRemovingPackage();
			TryUninstallingPackage();
			logger.LogAfterPackageOperationCompletes();
			
			OnPropertyChanged(model => model.IsAdded);
		}
		
		void LogRemovingPackage()
		{
			logger.LogRemovingPackage();
		}
		
		void TryUninstallingPackage()
		{
			try {
				IPackageManagementProject project = GetSingleProjectSelected();
				UninstallPackageAction action = project.CreateUninstallPackageAction();
				action.Package = package;
				actionRunner.Run(action);
			} catch (Exception ex) {
				ReportError(ex);
				logger.LogError(ex);
			}
		}
		
		public bool IsManaged {
			get {
				if (selectedProjects.HasMultipleProjects()) {
					return true;
				}
				return !selectedProjects.HasSingleProjectSelected();
			}
		}
		
		public void ManagePackage()
		{
			List<IPackageManagementSelectedProject> projects = GetSelectedProjectsForPackage();
			if (packageManagementEvents.OnSelectProjects(projects)) {
				ManagePackagesForSelectedProjects(projects);
			}
		}
		
		List<IPackageManagementSelectedProject> GetSelectedProjectsForPackage()
		{
			return selectedProjects.GetProjects(package).ToList();
		}
		
		public void ManagePackagesForSelectedProjects(IEnumerable<IPackageManagementSelectedProject> projects)
		{
			ManagePackagesForSelectedProjects(projects.ToList());
		}
		
		void ManagePackagesForSelectedProjects(IList<IPackageManagementSelectedProject> projects)
		{
			ClearReportedMessages();
			logger.LogManagingPackage();
			
			using (IDisposable operation = StartInstallOperation(package)) {
				TryInstallingPackagesForSelectedProjects(projects);
			}
			
			logger.LogAfterPackageOperationCompletes();
			OnPropertyChanged(model => model.IsAdded);
		}
		
		void TryInstallingPackagesForSelectedProjects(IList<IPackageManagementSelectedProject> projects)
		{
			try {
				if (AnyProjectsSelected(projects)) {
					InstallPackagesForSelectedProjects(projects);
				}
			} catch (Exception ex) {
				ReportError(ex);
				logger.LogError(ex);
			}
		}
		
		protected virtual bool AnyProjectsSelected(IList<IPackageManagementSelectedProject> projects)
		{
			return projects.Any(project => project.IsSelected);
		}
		
		void InstallPackagesForSelectedProjects(IList<IPackageManagementSelectedProject> projects)
		{
			if (CanInstallPackage(projects)) {
				IList<ProcessPackageAction> actions = GetProcessPackageActionsForSelectedProjects(projects);
				RunActionsIfAnyExist(actions);
			}
		}
		
		public virtual IList<ProcessPackageAction> GetProcessPackageActionsForSelectedProjects(
			IList<IPackageManagementSelectedProject> selectedProjects)
		{
			var actions = new List<ProcessPackageAction>();
			foreach (IPackageManagementSelectedProject selectedProject in selectedProjects) {
				if (selectedProject.IsSelected) {
					ProcessPackageAction action = CreateInstallPackageAction(selectedProject);
					action.AllowPrereleaseVersions = parent.IncludePrerelease;
					actions.Add(action);
				}
			}
			return actions;
		}
		
		bool CanInstallPackage(IList<IPackageManagementSelectedProject> projects)
		{
			IPackageManagementSelectedProject project = projects.FirstOrDefault();
			if (project != null) {
				return CanInstallPackage(project);
			}
			return false;
		}
		
		bool CanInstallPackage(IPackageManagementSelectedProject selectedProject)
		{
			IEnumerable<IPackage> licensedPackages = GetPackagesRequiringLicenseAcceptance(selectedProject);
			if (licensedPackages.Any()) {
				return packageManagementEvents.OnAcceptLicenses(licensedPackages);
			}
			return true;
		}
		
		protected ProcessPackageAction CreateInstallPackageAction(IPackageManagementSelectedProject selectedProject)
		{
			IPackageManagementProject project = selectedProject.Project;
			project.Logger = logger;
			
			ProcessPackageOperationsAction action = CreateInstallPackageAction(project);
			action.Package = package;
			return action;
		}
		
		protected ProcessPackageAction CreateUninstallPackageAction(IPackageManagementSelectedProject selectedProject)
		{
			IPackageManagementProject project = selectedProject.Project;
			project.Logger = logger;
			
			ProcessPackageAction action = project.CreateUninstallPackageAction();
			action.Package = package;
			return action;
		}
		
		void RunActionsIfAnyExist(IList<ProcessPackageAction> actions)
		{
			if (actions.Any()) {
				actionRunner.Run(actions);
			}
		}
		
		IEnumerable<IPackage> GetPackagesRequiringLicenseAcceptance(IPackageManagementSelectedProject selectedProject)
		{
			IPackageManagementProject project = selectedProject.Project;
			project.Logger = logger;
			InstallPackageAction installAction = project.CreateInstallPackageAction();
			installAction.AllowPrereleaseVersions = parent.IncludePrerelease;
			IEnumerable<PackageOperation> operations = project.GetInstallPackageOperations(package, installAction);
			return GetPackagesRequiringLicenseAcceptance(operations);
		}
		
		IEnumerable<IPackage> GetPackagesRequiringLicenseAcceptance(IEnumerable<PackageOperation> operations)
		{
			foreach (PackageOperation operation in operations) {
				if (PackageOperationRequiresLicenseAcceptance(operation)) {
					yield return operation.Package;
				}
			}
		}
		
		bool PackageOperationRequiresLicenseAcceptance(PackageOperation operation)
		{
			return
				(operation.Action == PackageAction.Install) &&
				operation.Package.RequireLicenseAcceptance &&
				!IsPackageInstalledInSolution(operation.Package);
		}
		
		static readonly string DisplayTextMarkupFormat = "<span weight='bold'>{0}</span>\n{1}";
		
		public string GetDisplayTextMarkup ()
		{
			return MarkupString.Format (DisplayTextMarkupFormat, Name, Summary);
		}
		
		public string GetAuthors()
		{
			return String.Join (", ", Authors);
		}
		
		public string GetLastPublishedDisplayText()
		{
			if (HasLastPublished) {
				return LastPublished.Value.Date.ToShortDateString ();
			}
			return String.Empty;
		}
		
		public string GetDownloadCountOrVersionDisplayText ()
		{
			if (ShowVersionInsteadOfDownloadCount) {
				return Version.ToString ();
			}

			return GetDownloadCountDisplayText ();
		}

		public string GetDownloadCountDisplayText ()
		{
			if (HasDownloadCount) {
				return DownloadCount.ToString ("N0");
			}
			return String.Empty;
		}

		public string GetPackageDependenciesDisplayText ()
		{
			var displayText = new StringBuilder ();
			foreach (PackageDependency dependency in Dependencies) {
				displayText.AppendLine (dependency.ToString ());
			}
			return displayText.ToString ();
		}

		public string GetNameMarkup ()
		{
			return GetBoldText (Name);
		}
			
		static string GetBoldText (string text)
		{
			return String.Format ("<b>{0}</b>", text);
		}

		public bool IsOlderPackageInstalled ()
		{
			return selectedProjects.HasOlderPackageInstalled (package);
		}

		public bool IsChecked {
			get { return isChecked; }
			set {
				if (value != isChecked) {
					isChecked = value;
					parent.OnPackageCheckedChanged (this);
				}
			}
		}

		public bool ShowVersionInsteadOfDownloadCount { get; set; }

		public override bool Equals (object obj)
		{
			var other = obj as PackageViewModel;
			if (other == null)
				return false;

			var packageName = new PackageName (package.Id, package.Version);
			var otherPackageName = new PackageName (other.package.Id, other.package.Version);
			return packageName.Equals (otherPackageName);
		}

		public override int GetHashCode ()
		{
			return package.ToString ().GetHashCode ();
		}
	}
}
