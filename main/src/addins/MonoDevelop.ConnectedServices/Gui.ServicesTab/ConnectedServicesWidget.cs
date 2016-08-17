using System;
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

		VBox container;
		ImageView headerImage;
		Label headerTitle, headerSubtitle;
		ServicesGalleryWidget gallery;
		ServiceDetailsWidget details;

		public ConnectedServicesWidget ()
		{

			var headerBox = new HBox ();

			headerImage = new ImageView (ImageService.GetIcon ("connected-services").WithSize (IconSize.Medium));
			headerImage.ButtonReleased += (sender, e) => {
				if (ShowingService != null) {
					var project = (DotNetProject)ShowingService.Project;
					ShowGallery (project.GetConnectedServicesBinding ().SupportedServices, project);
				}
			};
			headerTitle = new Label {
				Markup = "<b>" + GettextCatalog.GetString (ConnectedServices.SolutionTreeNodeName) + "</b>"
			};
			headerTitle.Font = headerTitle.Font.WithSize (16);
			headerSubtitle = new Label {
				TextColor = Styles.SecondaryTextColor
			};
			headerSubtitle.Font = headerTitle.Font.WithSize (14);


			headerBox.PackStart (headerImage);
			headerBox.PackStart (headerTitle);
			headerBox.PackStart (headerSubtitle);

			var headerFrame = new FrameBox {
				BackgroundColor = Styles.BaseBackgroundColor,
				BorderColor = Styles.ThinSplitterColor,
				BorderWidthBottom = 1,
				Padding = 20,
				Content = headerBox,
			};

			container = new VBox ();
			container.PackStart (headerFrame);
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
			if (details?.Parent == container)
				container.Remove (details);

			if (gallery == null) {
				gallery = new ServicesGalleryWidget ();
				gallery.ServiceSelected += HandleServiceSelected;
			}

			if (gallery.Parent == null) {
				container.PackStart (gallery);
			}

			gallery.LoadServices (services);

			headerImage.Image = ImageService.GetIcon ("connected-services").WithSize (IconSize.Medium);
			if (!string.IsNullOrEmpty (project?.Name))
				headerSubtitle.Text = " - " + project.Name;
			else
				headerSubtitle.Text = String.Empty;

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
			if (gallery?.Parent == container)
				container.Remove (gallery);

			if (details == null)
				details = new ServiceDetailsWidget ();

			if (details.Parent == null)
				container.PackStart (details);

			details.LoadService (service);

			headerSubtitle.Text = String.Empty;
			headerImage.Image = ImageService.GetIcon ("md-navigate-back").WithSize (IconSize.Medium);

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
