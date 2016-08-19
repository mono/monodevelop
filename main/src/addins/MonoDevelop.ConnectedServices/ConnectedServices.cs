using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mono.Addins;
using MonoDevelop.ConnectedServices.Gui.ServicesTab;
using MonoDevelop.Core;
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
		/// The category string for packages, this will be localised to the user
		/// </summary>
		public readonly static ConnectedServiceDependencyCategory PackageDependencyCategory =
			new ConnectedServiceDependencyCategory (GettextCatalog.GetString ("Packages"), Ide.Gui.Stock.OpenReferenceFolder);

		/// <summary>
		/// The category string for code, this will be localised to the user
		/// </summary>
		public readonly static ConnectedServiceDependencyCategory CodeDependencyCategory =
			new ConnectedServiceDependencyCategory (GettextCatalog.GetString ("Code"), "md-file-source");

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

		/// <summary>
		/// Displays the service details tab for the given service in the given project
		/// </summary>
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

		/// <summary>
		/// Removes the given service from the given project
		/// </summary>
		public static async Task RemoveServiceFromProject (DotNetProject project, string serviceId)
		{
			// TODO: show the remove dialog


			var binding = project.GetConnectedServicesBinding ();
			var service = binding.SupportedServices.FirstOrDefault (x => x.Id == serviceId);
			if (service != null) {
				// TODO: progress monitor
				await service.RemoveFromProject ();
			}
		}
	}
}
