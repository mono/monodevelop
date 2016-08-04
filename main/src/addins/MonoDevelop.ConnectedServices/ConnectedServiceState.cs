using System;

namespace MonoDevelop.ConnectedServices
{
	/// <summary>
	/// Object that is used to serialize the ConnectedService.json file
	/// </summary>
	public class ConnectedServiceState
	{
		public string ProviderId { get; set; }
		public string Version { get; set; }
		public string GettingStartedDocument { get; set; }
	}
}