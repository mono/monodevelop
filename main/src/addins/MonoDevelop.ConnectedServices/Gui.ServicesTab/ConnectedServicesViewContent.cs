using System;
using System.Linq;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;

namespace MonoDevelop.ConnectedServices.Gui.ServicesTab
{
	/// <summary>
	/// ViewContent host for the services gallery and service details widgets
	/// </summary>
	class ConnectedServicesViewContent : ViewContent
	{
		ConnectedServicesWidget widget;

		public ConnectedServicesViewContent (DotNetProject project)
		{
			this.Project = project;
			this.ContentName = string.Format ("{0} - {1}", GettextCatalog.GetString (ConnectedServices.SolutionTreeNodeName), project.Name); 
			widget = new ConnectedServicesWidget ();
			widget.Show ();
		}

		public override Control Control {
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
			var binding = ((DotNetProject)this.Project).GetConnectedServicesBinding ();
			if (string.IsNullOrEmpty (serviceId)) {
				var services = binding.SupportedServices;
				this.widget.ShowGallery (services);
			} else {
				var service = binding.SupportedServices.FirstOrDefault (x => x.Id == serviceId);
				this.widget.ShowServiceDetails (service);
			}
		}

		public override object GetDocumentObject ()
		{
			var binding = ((DotNetProject)this.Project).GetConnectedServicesBinding ();
			return binding?.ServicesNode;
		}
	}
}
