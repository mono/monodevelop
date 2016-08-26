using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
			new ConnectedServiceDependencyCategory (GettextCatalog.GetString ("Packages"), "md-folder-services");

		/// <summary>
		/// The category string for code, this will be localised to the user
		/// </summary>
		public readonly static ConnectedServiceDependencyCategory CodeDependencyCategory =
			new ConnectedServiceDependencyCategory (GettextCatalog.GetString ("Code"), "md-folder-code");

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
			var binding = project.GetConnectedServicesBinding ();
			var service = binding.SupportedServices.FirstOrDefault (x => x.Id == serviceId);
			if (service != null) {
				if (!ConfirmServiceRemoval (service))
					return;


				// TODO: progress monitor
				await service.RemoveFromProject ();
			}
		}

		static bool ConfirmServiceRemoval(IConnectedService service)
		{
			var msg1 = GettextCatalog.GetString ("Remove {0}", service.DisplayName);
			var msg2 = GettextCatalog.GetString ("Removing this service will result in the following changes to this project:\n\n{0}\n\nThis action does not remove any added or user code that uses the service. "+
			                                     "Removing the service will likely prevent the project from compiling until all usage of the service is removed.\n\n"+
			                                     "Are you sure you want to remove the service?", BuildRemovalInfo(service));
			
			if (service.Dependencies.Length == 0) {
				msg2 = GettextCatalog.GetString ("This action does not remove any added or user code that uses the service. " +
													 "Removing the service will likely prevent the project from compiling until all usage of the service is removed.\n\n" +
													 "Are you sure you want to remove the service?", BuildRemovalInfo (service));
			}

			return MessageService.Confirm (msg1, msg2, AlertButton.Remove);
		}

		static string BuildRemovalInfo(IConnectedService service)
		{
			var sb = new StringBuilder ();
			for (int i = 0; i < service.Dependencies.Length; i++) {
				if (i > 0) {
					sb.AppendLine ();
				}

				if (service.Dependencies [i].Category == ConnectedServices.PackageDependencyCategory) {
					sb.Append (GettextCatalog.GetString ("Remove {0} package and dependencies.", service.Dependencies [i].DisplayName));
				}
			}

			return sb.ToString ();
		}
	}
}
