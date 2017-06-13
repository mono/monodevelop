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
		HBox contentContainer;
		ScrollView galleryScrollContainer;
		ScrollView detailsScrollContainer;
		ExtendedHeaderBox header;
		ServicesGalleryWidget gallery;
		ServiceDetailsWidget details;

		public ConnectedServicesWidget ()
		{
			header = new ExtendedHeaderBox (ConnectedServices.SolutionTreeNodeName);
			header.Image = ImageService.GetIcon ("md-service");
			header.BackButtonClicked += (sender, e) => {
				if (ShowingService != null) {
					var project = (DotNetProject)ShowingService.Project;
					ShowGallery (project.GetConnectedServicesBinding ().SupportedServices, project);
				}
			};
			header.BackButtonTooltip = GettextCatalog.GetString ("Back to Service Gallery");

			contentContainer = new HBox ();
			contentContainer.Spacing = 0;
			galleryScrollContainer = new ScrollView () { Visible = false };
			detailsScrollContainer = new ScrollView () { Visible = false };
			galleryScrollContainer.BorderVisible = false;
			detailsScrollContainer.BorderVisible = false;
			contentContainer.PackStart (galleryScrollContainer, true);
			contentContainer.PackStart (detailsScrollContainer, true);

			var container = new VBox ();
			container.Spacing = 0;
			container.PackStart (header);
			container.PackStart (contentContainer, true, true);
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
				galleryScrollContainer.Content = gallery;
				gallery.ServiceSelected += HandleServiceSelected;
			}

			gallery.LoadServices (services);

			header.Image = ImageService.GetIcon ("md-service");
			header.Subtitle = project?.Name;
			header.BackButtonVisible = false;

			ShowingService = null;
			detailsScrollContainer.Visible = false;
			galleryScrollContainer.Visible = true;
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
			if (details == null) {
				details = new ServiceDetailsWidget ();
				detailsScrollContainer.Content = details;
			}

			details.LoadService (service);

			header.Subtitle = service?.Project?.Name;
			header.BackButtonVisible = true;

			ShowingService = service;
			detailsScrollContainer.Visible = true;
			galleryScrollContainer.Visible = false;
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
