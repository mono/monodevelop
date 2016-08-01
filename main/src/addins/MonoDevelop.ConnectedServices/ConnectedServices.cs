using System;
using System.Collections.Generic;
using Mono.Addins;
using MonoDevelop.Projects;

namespace MonoDevelop.ConnectedServices
{
	/// <summary>
	/// Defines a set of constants for the Connected Services addin
	/// </summary>
	static class ConnectedServices
	{
		/// <summary>
		/// The extension point for service providers
		/// </summary>
		static readonly string ServiceProvidersExtensionPoint = "/MonoDevelop/ConnectedServices/ServiceProviders";

		/// <summary>
		/// The name of the node to display in the solution tree
		/// </summary>
		internal const string SolutionTreeNodeName = "Connected Services";

		/// <summary>
		/// The name of the folder that is used to store state about each connected service
		/// that has been added to the project
		/// </summary>
		internal const string ProjectStateFolderName = "Connected Services";

		/// <summary>
		/// Gets the list of IConnectedService instances that support project
		/// </summary>
		public static IConnectedService[] GetServices (DotNetProject project)
		{
			var result = new List<IConnectedService> ();
			var providers = AddinManager.GetExtensionObjects<IConnectedServiceProvider> (ServiceProvidersExtensionPoint);

			foreach (var provider in providers) {
				var service = provider.GetConnectedService (project);
				if (service != null) {
					result.Add (service);
				}
			}

			return result.ToArray ();
		}
	}
}
