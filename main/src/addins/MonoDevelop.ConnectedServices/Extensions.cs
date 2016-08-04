using System;
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
	}
}