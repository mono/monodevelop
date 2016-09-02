using System;

namespace MonoDevelop.ConnectedServices
{
	/// <summary>
	/// Defines the various states that a service or dependency can be in
	/// </summary>
	public enum Status
	{
		/// <summary>
		/// The service or dependency is not added to the project
		/// </summary>
		NotAdded,

		/// <summary>
		/// The service or dependency has been added to the project
		/// </summary>
		Added,

		/// <summary>
		/// The service or dependency is ciurrently being added to the project
		/// </summary>
		Adding,

		/// <summary>
		/// The service or dependency is currently being removed from the project
		/// </summary>
		Removing,
	}
}