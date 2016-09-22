using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.PackageManagement;
using MonoDevelop.Projects;
using Microsoft.CodeAnalysis;

namespace MonoDevelop.ConnectedServices
{
	/// <summary>
	/// Extension methods and helpers
	/// </summary>
	static class Extensions
	{
		/// <summary>
		/// Returns the IConnectedServicesBinding instance that is attached to the project. 
		/// Use this to query state about connected services for a given project. 
		/// </summary>
		public static IConnectedServicesBinding GetConnectedServicesBinding(this DotNetProject project)
		{
			if (project == null) {
				return NullConnectedServicesBinding.Null;
			}

			// we should always have a binding for any given project because the extension loads for all projects
			return project.GetService<IConnectedServicesBinding> ();
		}

		/// <summary>
		/// Adds the dependencies to the project
		/// </summary>
		public static async Task<bool> AddPackageDependency (this DotNetProject project, IPackageDependency dependency)
		{
			if (project == null)
				throw new ArgumentNullException (nameof (project));

			LoggingService.LogInfo ("Adding package dependency '{0}' to project", dependency.DisplayName);

			if (dependency.Status == Status.Added) {
				LoggingService.LogInfo ("Skipped, the package dependency is already added to the project");
				return true;
			}

			try {
				var references = new List<PackageManagementPackageReference> ();
				references.Add (new PackageManagementPackageReference (dependency.PackageId, dependency.PackageVersion));

				var task = PackageManagementServices.ProjectOperations.InstallPackagesAsync (project, references);

				await task.ConfigureAwait (false);
				return true;
			} catch (InvalidOperationException) {
				// Nuget throws these and logs them, let's not pollute the log anymore than we need to
				// and assume that it was already added to the project
				throw;
			} catch (Exception ex) {
				LoggingService.LogInternalError ("Could not queue package for installation", ex);
				throw;
			}
		}

		/// <summary>
		/// Adds the dependencies to the project
		/// </summary>
		public static async Task<bool> AddPackageDependencies (this DotNetProject project, IList<IPackageDependency> dependencies)
		{
			if (project == null)
				throw new ArgumentNullException (nameof (project));

			foreach (var dependency in dependencies) {
				LoggingService.LogInfo ("Adding package dependency '{0}' to project", dependency.DisplayName);
				if (dependency.Status == Status.Added) {
					LoggingService.LogInfo ("Skipped, the package dependency is already added to the project");
				}
			}

			var dependenciesToAdd = dependencies.Where (x => x.Status != Status.Added).ToList ();

			try {
				var references = new List<PackageManagementPackageReference> ();
				foreach (var dependency in dependenciesToAdd) {
					references.Add (new PackageManagementPackageReference (dependency.PackageId, dependency.PackageVersion));
				}

				var task = PackageManagementServices.ProjectOperations.InstallPackagesAsync (project, references);

				await task.ConfigureAwait (false);
				return true;
			} catch (InvalidOperationException) {
				// Nuget throws these and logs them, let's not pollute the log anymore than we need to
				throw;
			} catch (Exception ex) {
				LoggingService.LogInternalError ("Could not queue packages for installation", ex);
				throw;
			}
		}

		/// <summary>
		/// Removes the dependency from the project
		/// </summary>
		public static async Task RemovePackageDependency(this DotNetProject project, IPackageDependency dependency)
		{
			if (project == null)
				throw new ArgumentNullException (nameof (project));

			LoggingService.LogInfo ("Removing package dependency '{0}' from project", dependency.DisplayName);

			if (dependency.Status == Status.NotAdded || !project.PackageAdded (dependency)) {
				LoggingService.LogInfo ("Skipped, the package dependency is not added to the project");
				return;
			}

			try {
				var references = new List<string> ();
				references.Add (dependency.PackageId);

				var task = PackageManagementServices.ProjectOperations.UninstallPackagesAsync (project, references, true);

				await task.ConfigureAwait (false);
			} catch (InvalidOperationException) {
				// Nuget throws these and logs them, let's not pollute the log anymore than we need to
				// and assume that it needs to be left in the project
			} catch (Exception ex) {
				LoggingService.LogInternalError ("Could not queue package for uninstallation", ex);
				throw;
			}
		}

		/// <summary>
		/// Removes the dependencies from the project
		/// </summary>
		public static async Task RemovePackageDependencies (this DotNetProject project, IList<IPackageDependency> dependencies)
		{
			if (project == null)
				throw new ArgumentNullException (nameof (project));

			foreach (var dependency in dependencies) {
				LoggingService.LogInfo ("Removing package dependency '{0}' from project", dependency.DisplayName);
				if (dependency.Status == Status.NotAdded || !project.PackageAdded (dependency)) {
					LoggingService.LogInfo ("Skipped, the package dependency is not added to the project");
				}
			}

			var dependenciesToRemove = dependencies.Where (x => x.Status != Status.NotAdded && project.PackageAdded (x)).ToList ();

			try {
				var references = new List<string> ();
				foreach (var dependency in dependencies) {
					references.Add (dependency.PackageId);
				}

				var task = PackageManagementServices.ProjectOperations.UninstallPackagesAsync (project, references, true);

				await task.ConfigureAwait (false);
			} catch (InvalidOperationException) {
				// Nuget throws these and logs them, let's not pollute the log anymore than we need to
				// and assume that it needs to be left in the project
			} catch (Exception ex) {
				LoggingService.LogInternalError ("Could not queue packages for uninstallation", ex);
				throw;
			}
		}

		/// <summary>
		/// Determines if the given package dependency has been added to the project or not
		/// </summary>
		public static bool PackageAdded(this DotNetProject project, IPackageDependency dependency)
		{
			return PackageManagementServices.ProjectOperations.GetInstalledPackages (project).Any (p => p.Id == dependency.PackageId);
		}
	}
}