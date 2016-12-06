using System;

namespace MonoDevelop.ConnectedServices
{
	/// <summary>
	/// Defines the commands for Connected Services
	/// </summary>
	public enum Commands
	{
		/// <summary>
		/// Opens the services gallery tab
		/// </summary>
		OpenServicesGallery,
		OpenServicesGalleryFromServicesNode,

		/// <summary>
		/// Opens the service details tab for the given service
		/// </summary>
		OpenServiceDetails,

		/// <summary>
		/// Removes the selected service from the project
		/// </summary>
		RemoveService,

		/// <summary>
		/// Used to send telemetry
		/// </summary>
		AddServiceTelemetry
	}
}
