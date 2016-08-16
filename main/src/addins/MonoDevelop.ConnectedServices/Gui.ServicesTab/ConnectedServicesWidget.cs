using System;
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
		ServicesGalleryWidget gallery;
		ServiceDetailsWidget details;

		public ConnectedServicesWidget ()
		{
			container = new VBox ();
			Content = container;
		}

		public IConnectedService ShowingService { get; private set; }

		/// <summary>
		/// Shows the services gallery and removes the details widget if it is visible
		/// </summary>
		public void ShowGallery(IConnectedService[] services)
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
			ShowingService = null;
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
			ShowingService = service;
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
