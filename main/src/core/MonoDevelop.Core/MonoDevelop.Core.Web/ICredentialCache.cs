using System;
using System.Net;

namespace MonoDevelop.Core.Web
{
	public interface ICredentialCache
	{
		void Add (Uri uri, ICredentials credentials);

		ICredentials GetCredentials (Uri uri);
	}
}
