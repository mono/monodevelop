using System;
using System.Net;

namespace MonoDevelop.WebReferences
{
	
	/// <summary>Adds an authentication type to the standard NetworkCredential class.</summary>
	public class DiscoveryNetworkCredential : NetworkCredential
	{
		#region Properties
		public string AuthenticationType 
		{
			get { return authenticationType; }
		}
		
		public bool IsDefaultAuthenticationType 
		{
			get { return String.Compare (authenticationType, DefaultAuthenticationType, StringComparison.OrdinalIgnoreCase) == 0; }
		}
		#endregion
		
		#region Constants
		public const string DefaultAuthenticationType = "Default";
		#endregion
		
		#region Member Variables
		readonly string authenticationType = String.Empty;
		#endregion
		
		public DiscoveryNetworkCredential(string userName, string password, string domain, string authenticationType) : base(userName, password, domain)
		{
			this.authenticationType = authenticationType;
		}
		
		public DiscoveryNetworkCredential(NetworkCredential credential, string authenticationType) : this(credential.UserName, credential.Password, credential.Domain, authenticationType)
		{
		}
		
		
	}
}
