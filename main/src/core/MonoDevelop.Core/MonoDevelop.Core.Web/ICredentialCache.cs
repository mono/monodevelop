using System;
using System.Net;

namespace MonoDevelop.Core.Web
{
	public interface ICredentialCache
	{
		void Add (Uri uri, ICredentials credentials, CredentialType credentialType);

		ICredentials GetCredentials (Uri uri, CredentialType credentialType);
	}
}
