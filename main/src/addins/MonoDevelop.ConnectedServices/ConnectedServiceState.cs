using System;

namespace MonoDevelop.ConnectedServices
{
	/// <summary>
	/// Object that is used to serialize the ConnectedService.json file
	/// </summary>
	public class ConnectedServiceState
	{
		/// <summary>
		/// Gets or sets the provider identifier.
		/// </summary>
		public string ProviderId { get; set; }

		/// <summary>
		/// Gets or sets the version of the provider
		/// </summary>
		public string Version { get; set; }

		/// <summary>
		/// Gets or sets the getting started document object
		/// </summary>
		public object GettingStartedDocument { get; set; }
	}
}