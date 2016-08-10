using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.PackageManagement;
using MonoDevelop.Projects;

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
		public static Task AddPackageDependency (this DotNetProject project, IPackageDependency dependency)
		{
			if (project == null)
				throw new ArgumentNullException (nameof (project));

			LoggingService.LogInfo ("Adding connected service dependencies");

			if (dependency.IsAdded) {
				LoggingService.LogInfo ("Skipped, all dependencies have already been added");
				return Task.FromResult (true);
			}

			try {
				var references = new List<PackageManagementPackageReference> ();
				references.Add (new PackageManagementPackageReference (dependency.PackageId, dependency.PackageVersion));

				var task = PackageManagementServices.ProjectOperations.InstallPackagesAsync (project, references);

				LoggingService.LogInfo ("Queued dependency for installation");
				return task;
			} catch (Exception ex) {
				LoggingService.LogInternalError ("Could not queue dependencies for installation", ex);
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