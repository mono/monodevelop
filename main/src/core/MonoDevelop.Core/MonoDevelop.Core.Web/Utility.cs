using System;
using System.Net;

namespace MonoDevelop.Core.Web
{
	public static class Utility
	{
		static readonly string[] AuthenticationSchemes = { "Basic", "NTLM", "Negotiate" };

		public static NetworkCredential GetCredentialsForUriFromICredentials (Uri uri, ICredentials credentials)
		{
			if (credentials == null)
				return null;

			NetworkCredential cred = null;
			foreach (var scheme in AuthenticationSchemes) {
				cred = credentials.GetCredential (uri, scheme);
				if (cred != null)
					break;
			}

			return cred;
		}
	}
}