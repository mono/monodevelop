using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Addins;
using MonoDevelop.ConnectedServices.Gui.ServicesTab;
using MonoDevelop.ConnectedServices.Gui.SolutionPad;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Projects;

namespace MonoDevelop.ConnectedServices
{
	/// <summary>
	/// Defines a set of constants for the Connected Services addin
	/// </summary>
	public static class ConnectedServices
	{
		/// <summary>
		/// The extension point for service providers
		/// </summary>
		static readonly string ServiceProvidersExtensionPoint = "/MonoDevelop/ConnectedServices/ServiceProviders";

		/// <summary>
		/// The name of the node to display in the solution tree
		/// </summary>
		internal static string SolutionTreeNodeName = GettextCatalog.GetString ("Connected Services");

		/// <summary>
		/// The name of the folder that is used to store state about each connected service
		/// that has been added to the project
		/// </summary>
		internal const string ProjectStateFolderName = "Connected Services";

		/// <summary>
		/// The name of the .json file that is stored in the ProjectStateFolderName/&lt;ServiceId&gt; folder.
		/// </summary>
		internal const string ConnectedServicesJsonFileName = "ConnectedService.json";

		/// <summary>
		/// The name of the Getting Started section that is displayed to the user
		/// </summary>
		internal static string GettingStartedSectionDisplayName = GettextCatalog.GetString ("Getting Started");

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
		internal static void OpenServicesTab(DotNetProject project, string serviceId)
		{
			if (project == null)
				project = IdeApp.ProjectOperations.CurrentSelectedProject as DotNetProject;

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
		/// Displays the service details tab for the given service
		/// </summary>
		public static Task OpenServicesTab (this IConnectedService service)
		{
			if (service == null)
				throw new ArgumentNullException (nameof (service));

			return Runtime.RunInMainThread (() => OpenServicesTab (service.Project, service.Id));
		}

		/// <summary>
		/// Displays the services gallery tab for the given project
		/// </summary>
		public static Task OpenServicesTab (this DotNetProject project)
		{
			if (project == null)
				throw new ArgumentNullException (nameof (project));

			return Runtime.RunInMainThread (() => OpenServicesTab (project, null));
		}

		/// <summary>
		/// Removes the given service from the given project
		/// </summary>
		public static async Task RemoveServiceFromProject (DotNetProject project, string serviceId)
		{
			var binding = project.GetConnectedServicesBinding ();
			var service = binding.SupportedServices.FirstOrDefault (x => x.Id == serviceId);
			if (service != null) {
				if (! (await ConfirmServiceRemoval (service).ConfigureAwait (false)))
					return;
				
				await Runtime.RunInMainThread (() => EnsureServiceDetailTabIsClosed (project, serviceId));

				await service.RemoveFromProject ();
			}
		}

		/// <summary>
		/// Looks for open documents that are showing the detail for the service that is being removed and updates the content to show the gallery instead
		/// </summary>
		static void EnsureServiceDetailTabIsClosed (DotNetProject project, string serviceId)
		{
			Ide.Gui.Document view = null;
			var servicesView = LocateServiceView(project, out view);
			if (servicesView != null) {
				var docObject = view.GetDocumentObject ();
				var serviceNode = docObject as ConnectedServiceNode;
				if (serviceNode != null && serviceNode.Id == serviceId) {
					servicesView.UpdateContent (null);
					view.Window.SelectWindow ();
				}
			}
		}

		/// <summary>
		/// Searches for open documents and locates the ConnectedServicesViewContent for the given project
		/// </summary>
		internal static ConnectedServicesViewContent LocateServiceView(DotNetProject project)
		{
			Ide.Gui.Document view = null;
			return LocateServiceView (project, out view);
		}

		/// <summary>
		/// Searches for open documents and locates the ConnectedServicesViewContent for the given project
		/// </summary>
		internal static ConnectedServicesViewContent LocateServiceView (DotNetProject project, out Ide.Gui.Document documentView)
		{
			documentView = null;
			foreach (var view in IdeApp.Workbench.Documents) {
				var servicesView = view.PrimaryView.GetContent<ConnectedServicesViewContent> ();
				if (servicesView != null && servicesView.Project == project) {
					documentView = view;
					return servicesView;
				}
			}

			return null;
		}

		/// <summary>
		/// Confirms with the user about removing the specified service
		/// </summary>
		static Task<bool> ConfirmServiceRemoval(IConnectedService service)
		{
			var msg1 = GettextCatalog.GetString ("Remove {0}", service.DisplayName);
			var msg2 = GettextCatalog.GetString ("{0}" +
			                                     "References in your code need to be removed manually. " +
			                                     "Are you sure you want to remove the service from project {1}?", BuildRemovalInfo(service), service.Project.Name);

			var result = new TaskCompletionSource<bool> ();
			Xwt.Toolkit.NativeEngine.Invoke (delegate {
				result.SetResult (Xwt.MessageDialog.Confirm (msg1, msg2, Xwt.Command.Remove));
			});
			return result.Task;
		}

		/// <summary>
		/// Builds up the text describing what will happen when the service is removed. This just lists
		/// package dependencies
		/// </summary>
		static string BuildRemovalInfo(IConnectedService service)
		{
			var sb = new StringBuilder ();

			if (service.Dependencies.Length > 0) {
				sb.AppendLine (GettextCatalog.GetString ("The following packages and their dependencies will be removed:"));
				sb.AppendLine ();
				for (int i = 0; i < service.Dependencies.Length; i++) {

					if (service.Dependencies [i].Category == ConnectedServiceDependency.PackageDependencyCategory) {
						if (i > 0)
							sb.AppendLine ();
						sb.Append ("   • ").Append (service.Dependencies [i].DisplayName);
					}
				}
				sb.Append ("\n\n");
			}

			return sb.ToString ();
		}
	}
}
