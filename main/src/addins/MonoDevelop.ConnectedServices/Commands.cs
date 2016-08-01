using System;

namespace MonoDevelop.ConnectedServices
{
	/// <summary>
	/// Defines the commands for Connected Services
	/// </summary>
	public enum Commands
	{
		/// <summary>
		/// Adds a new Connected Service to the project by taking the user to the gallery of services
		/// </summary>
		AddService,

		/// <summary>
		/// Opens the service details tab for the given service
		/// </summary>
		OpenServiceDetails,
	}
}
