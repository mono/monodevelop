using System;
using Gtk;

namespace MonoDevelop.ConnectedServices.Gui.ServicesTab
{
	/// <summary>
	/// Widget that displays the service details
	/// </summary>
	class ServiceDetailsWidget : VBox
	{
		public ServiceDetailsWidget ()
		{
			this.Build ();
		}

		/// <summary>
		/// Loads the service details for the given service
		/// </summary>
		public void LoadService (IConnectedService service)
		{
			// test code
			var label = new Label ("service: " + service.DisplayName);
			this.PackStart (label, false, false, 0);

			this.ShowAll ();
		}

		/// <summary>
		/// Builds the widget
		/// </summary>
		void Build()
		{
			var label = new Label ("details");
			this.PackStart (label, false, false, 0);
		}
	}
}
