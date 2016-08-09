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
		public static Task AddDependencies (this DotNetProject project, IConnectedServiceDependency[] dependencies)
		{
			if (project == null)
				throw new ArgumentNullException (nameof (project));

			LoggingService.LogInfo ("Adding connected service dependencies");
			if (dependencies.Length == 0) {
				LoggingService.LogInfo ("Skipped, there were no dependencies to add");
				return Task.FromResult (true);
			}

			if (dependencies.Count(x => !x.IsAdded) == 0) {
				LoggingService.LogInfo ("Skipped, all dependencies have already been added");
				return Task.FromResult (true);
			}

			try {
				var references = new List<PackageManagementPackageReference> ();
				foreach (var dependency in dependencies.Where (x => !x.IsAdded)) {
					references.Add (new PackageManagementPackageReference (dependency.PackageId, dependency.PackageVersion));
				}

				var task = PackageManagementServices.ProjectOperations.InstallPackagesAsync (project, references);

				LoggingService.LogInfo ("Queued dependencies for installation");
				return task;
			} catch (Exception ex) {
				LoggingService.LogInternalError ("Could not queue dependencies for installation", ex);
				throw;
			}
		}
	}
}