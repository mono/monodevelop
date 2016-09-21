using System;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using Xwt;

namespace MonoDevelop.ConnectedServices.Gui.ServicesTab
{
	/// <summary>
	/// Xwt host for the gallery and service details widgets
	/// </summary>
	class ConnectedServicesWidget : Widget
	{
		/*
		 * This widget has a container to hold the current widget (services gallery or service details)
		 * 
		 */

		ScrollView scrollContainer;
		ExtendedHeaderBox header;
		ServicesGalleryWidget gallery;
		ServiceDetailsWidget details;

		public ConnectedServicesWidget ()
		{
			header = new ExtendedHeaderBox (GettextCatalog.GetString (ConnectedServices.SolutionTreeNodeName));
			header.Image = ImageService.GetIcon ("md-service");
			header.BackButtonClicked += (sender, e) => {
				if (ShowingService != null) {
					var project = (DotNetProject)ShowingService.Project;
					ShowGallery (project.GetConnectedServicesBinding ().SupportedServices, project);
				}
			};
			header.BackButtonTooltip = GettextCatalog.GetString ("Back to Service Gallery");

			scrollContainer = new ScrollView ();
			scrollContainer.BorderVisible = false;

			var container = new VBox ();
			container.Spacing = 0;
			container.PackStart (header);
			container.PackStart (scrollContainer, true, true);
			Content = container;
		}

		public IConnectedService ShowingService { get; private set; }

		public event EventHandler GalleryShown;
		public event EventHandler<ServiceEventArgs> ServiceShown;

		/// <summary>
		/// Shows the services gallery and removes the details widget if it is visible
		/// </summary>
		public void ShowGallery(IConnectedService[] services, Project project)
		{
			if (gallery == null) {
				gallery = new ServicesGalleryWidget ();
				gallery.ServiceSelected += HandleServiceSelected;
			}

			if (gallery.Parent == null) {
				scrollContainer.Content = gallery;
			}

			gallery.LoadServices (services);

			header.Image = ImageService.GetIcon ("md-service");
			header.Subtitle = project?.Name;
			header.BackButtonVisible = false;

			ShowingService = null;
			GalleryShown?.Invoke (this, EventArgs.Empty);
		}

		void HandleServiceSelected (object sender, ServiceEventArgs e)
		{
			ShowServiceDetails (e.Service);
		}

		/// <summary>
		/// Shows the service details  and removes the gallery widget if it is visible
		/// </summary>
		public void ShowServiceDetails (IConnectedService service)
		{
			if (details == null)
				details = new ServiceDetailsWidget ();

			if (details.Parent == null)
				scrollContainer.Content = details;

			details.LoadService (service);

			header.Subtitle = service?.Project?.Name;
			header.BackButtonVisible = true;

			ShowingService = service;
			ServiceShown?.Invoke (this, new ServiceEventArgs (service));
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing) {
				if (gallery != null)
					gallery.ServiceSelected -= HandleServiceSelected;
			}
			base.Dispose (disposing);
		}
	}
}
