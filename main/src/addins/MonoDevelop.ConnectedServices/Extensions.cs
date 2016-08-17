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

			if (dependency.IsAdded) {
				LoggingService.LogInfo ("Skipped, the package dependency is already added to the project");
				return true;
			}

			try {
				var references = new List<PackageManagementPackageReference> ();
				references.Add (new PackageManagementPackageReference (dependency.PackageId, dependency.PackageVersion));

				var task = PackageManagementServices.ProjectOperations.InstallPackagesAsync (project, references);

				LoggingService.LogInfo ("Queued for installation");
				await task.ConfigureAwait (false);
				return true;
			} catch (Exception ex) {
				LoggingService.LogInternalError ("Could not queue package for installation", ex);
				throw;
			}
		}

		/// <summary>
		/// Removes the dependencies from the project
		/// </summary>
		public static async Task RemovePackageDependency(this DotNetProject project, IPackageDependency dependency)
		{
			if (project == null)
				throw new ArgumentNullException (nameof (project));

			LoggingService.LogInfo ("Removing package dependency '{0}' from project", dependency.DisplayName);

			if (!dependency.IsAdded) {
				LoggingService.LogInfo ("Skipped, the package dependency is not added to the project");
				return;
			}

			try {
				var references = new List<string> ();
				references.Add (dependency.PackageId);

				var task = PackageManagementServices.ProjectOperations.UninstallPackagesAsync (project, references, true);

				LoggingService.LogInfo ("Queued for uninstallation");
				await task.ConfigureAwait (false);
			} catch (Exception ex) {
				LoggingService.LogInternalError ("Could not queue package for uninstallation", ex);
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