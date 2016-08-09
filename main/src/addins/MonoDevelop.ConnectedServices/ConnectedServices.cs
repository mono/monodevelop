using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mono.Addins;
using MonoDevelop.ConnectedServices.Gui.ServicesTab;
using MonoDevelop.Ide;
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
		internal const string SolutionTreeNodeName = "Service Capabilities";

		/// <summary>
		/// The name of the folder that is used to store state about each connected service
		/// that has been added to the project
		/// </summary>
		internal const string ProjectStateFolderName = "Service Capabilities";

		/// <summary>
		/// The name of the .json file that is stored in the ProjectStateFolderName/&lt;ServiceId&gt; folder.
		/// </summary>
		internal const string ConnectedServicesJsonFileName = "ConnectedService.json";

		internal const string GettingStartedSectionDisplayName = "Getting Started";

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

		public static void OpenServicesTab(DotNetProject project, string serviceId = null)
		{
			ConnectedServicesViewContent servicesView = null;

			foreach (var view in IdeApp.Workbench.Documents) {
				servicesView = view.PrimaryView.GetContent<ConnectedServicesViewContent> ();
				if (servicesView != null && servicesView.Project == project) {
					servicesView.UpdateContent(serviceId);
					view.Window.SelectWindow ();
					return;
				}
			}

			servicesView = new ConnectedServicesViewContent (project);
			servicesView.UpdateContent (serviceId);
			IdeApp.Workbench.OpenDocument (servicesView, true);
		}
	}
}
