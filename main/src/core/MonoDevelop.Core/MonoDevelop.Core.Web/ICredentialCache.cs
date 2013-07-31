using System;
using System.Net;

namespace MonoDevelop.Core.Web
{
	public interface ICredentialCache
	{
		void Add (Uri requestUri, Uri proxy, ICredentials credentials);

		ICredentials GetCredentials (Uri proxy, Uri uri);
	}
}
