using System;
using Gtk;

namespace MonoDevelop.ConnectedServices.Gui.ServicesTab
{
	/// <summary>
	/// Gtk host for the gallery and service details widgets
	/// </summary>
	class ConnectedServicesWidget : EventBox
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
			this.Build ();
		}

		/// <summary>
		/// Shows the services gallery and removes the details widget if it is visible
		/// </summary>
		public void ShowGallery(IConnectedService[] services)
		{
			if (this.details != null && this.details.Parent != null) {
				this.container.Remove (this.details);
			}

			if (this.gallery == null) {
				this.gallery = new ServicesGalleryWidget ();
			}

			if (this.gallery.Parent == null) {
				this.container.Add (this.gallery);
				this.container.ShowAll ();
			}

			this.gallery.LoadServices (services);
		}

		/// <summary>
		/// Shows the service details  and removes the gallery widget if it is visible
		/// </summary>
		public void ShowServiceDetails (IConnectedService service)
		{
			if (this.gallery != null && this.gallery.Parent != null) {
				this.container.Remove (this.gallery);
			}

			if (this.details == null) {
				this.details = new ServiceDetailsWidget ();
			}

			if (this.details.Parent == null) {
				this.container.Add (this.details);
				this.container.ShowAll ();
			}

			this.details.LoadService (service);
		}

		/// <summary>
		/// Builds the widget
		/// </summary>
		void Build()
		{
			this.container = new VBox ();

			this.Add (this.container);
		}
	}
}
