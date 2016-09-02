using System;

namespace MonoDevelop.ConnectedServices
{
	/// <summary>
	/// Defines the various states that a service be in
	/// </summary>
	public enum ServiceStatus
	{
		/// <summary>
		/// The service is not added to the project
		/// </summary>
		NotAdded,

		/// <summary>
		/// The service has been added to the project
		/// </summary>
		Added,

		/// <summary>
		/// The service is ciurrently being added to the project
		/// </summary>
		Adding,

		/// <summary>
		/// The service is currently being removed from the project
		/// </summary>
		Removing,
	}
}