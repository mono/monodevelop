using System;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using Xwt;
using MonoDevelop.Ide.Gui.Documents;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;

namespace MonoDevelop.ConnectedServices.Gui.ServicesTab
{
	/// <summary>
	/// ViewContent host for the services gallery and service details widgets
	/// </summary>
	class ConnectedServicesViewContent : DocumentController, IProjectPadNodeSelector
	{
		ConnectedServicesWidget widget;

		public ConnectedServicesViewContent (DotNetProject project)
		{
			Owner = project;
			DocumentTitle = string.Format ("{0} \u2013 {1}", ConnectedServices.SolutionTreeNodeName, project.Name);

			widget = new ConnectedServicesWidget ();
			widget.GalleryShown += (sender, e) => {
				UpdateCurrentNode ();
			};
			widget.ServiceShown += (sender, e) => {
				UpdateCurrentNode ();
			};
		}

		protected override Control OnGetViewControl (DocumentViewContent view)
		{
			return new XwtControl (widget);
		}

		/// <summary>
		/// Updates the content of the view for the project. If a service id is given, opens the details view for that service
		/// </summary>
		public void UpdateContent(string serviceId)
		{
			var binding = ((DotNetProject)this.Owner).GetConnectedServicesBinding ();
			if (!string.IsNullOrEmpty (serviceId)) {
				var service = binding.SupportedServices.FirstOrDefault (x => x.Id == serviceId);
				if (service != null) {
					this.widget.ShowServiceDetails (service);
					return;
				}
				LoggingService.LogError ("Showing service details failed, service id {0} not found", serviceId);
			}

			var services = binding.SupportedServices;
			this.widget.ShowGallery (services, (Project)Owner);
		}

		object currentNodeObject;

		/// <summary>
		/// Tells the view content to update it's DocumentObject
		/// </summary>
		internal Task UpdateCurrentNode ()
		{
			return Runtime.RunInMainThread (() => {
				var node = ((DotNetProject)this.Owner).GetConnectedServicesBinding ()?.ServicesNode;
				if (node != null && widget.ShowingService != null) {
					var serviceNode = node.GetServiceNode (widget.ShowingService);
					if (serviceNode != null) {
						node.Expand ();
						serviceNode.Select ();
						currentNodeObject = serviceNode;
						return;
					}
				}
				node?.Select ();
				currentNodeObject = node;
			});
		}

		protected override void OnDispose ()
		{
			if (widget != null) {
				widget.Dispose ();
				widget = null;
			}
			base.OnDispose ();
		}

		public object GetNodeObjext ()
		{
			if (currentNodeObject == null)
				UpdateCurrentNode ().Wait ();
			return currentNodeObject;
		}
	}
}
