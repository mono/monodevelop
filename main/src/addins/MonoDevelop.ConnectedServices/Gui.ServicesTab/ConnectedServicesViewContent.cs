using System;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using Xwt;

namespace MonoDevelop.ConnectedServices.Gui.ServicesTab
{
	/// <summary>
	/// ViewContent host for the services gallery and service details widgets
	/// </summary>
	class ConnectedServicesViewContent : AbstractXwtViewContent
	{
		ConnectedServicesWidget widget;

		public ConnectedServicesViewContent (DotNetProject project)
		{
			this.Owner = project;
			this.ContentName = string.Format ("{0} \u2013 {1}", ConnectedServices.SolutionTreeNodeName, project.Name);

			widget = new ConnectedServicesWidget ();
			widget.GalleryShown += (sender, e) => {
				UpdateCurrentNode ();
			};
			widget.ServiceShown += (sender, e) => {
				UpdateCurrentNode ();
			};
		}

		public override Widget Widget {
			get {
				return widget;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this ViewContent represents a file or not.
		/// </summary>
		public override bool IsFile {
			get {
				return false;
			}
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
			this.widget.ShowGallery (services, Owner);
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

		public override object GetDocumentObject ()
		{
			if (currentNodeObject == null)
				UpdateCurrentNode ().Wait ();
			return currentNodeObject;
		}

		public override void Dispose ()
		{
			if (widget != null) {
				widget.Dispose ();
				widget = null;
			}
			base.Dispose ();
		}
	}
}
